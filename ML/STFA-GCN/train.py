import json
import glob
import os
import torch
from torch_geometric.loader import DataLoader
from sklearn.model_selection import train_test_split
from sklearn.metrics import precision_score, recall_score, f1_score, accuracy_score
import numpy as np
import torch.nn as nn
import torch.nn.functional as F
import numpy as np
from scipy.ndimage import label
from torch_geometric.nn import GCNConv
from torch_geometric.data import Data, Batch
from typing import List, Tuple, Dict


class RadarDataProcessor:
    def __init__(
        self,
        threshold_pfa: float = 1e-2,
        max_velocity: float = 8.5,
        scan_interval: float = 1.0,
        connection_threshold: float = None,
    ):
        """
        Initialize the radar data processor
        Args:
            threshold_pfa: False alarm rate for threshold detection
            max_velocity: Maximum expected target velocity in m/s
            scan_interval: Time interval between radar scans
            connection_threshold: Maximum distance for node connection
        """
        self.threshold_pfa = threshold_pfa
        self.max_velocity = max_velocity
        self.scan_interval = scan_interval
        self.connection_threshold = connection_threshold or (
            max_velocity * scan_interval
        )

    def process_connected_components(
        self, binary_map: np.ndarray
    ) -> Tuple[np.ndarray, List[Dict]]:
        """
        Detect connected components and extract their properties
        """
        # Use 8-connectivity for component labeling
        structure = np.ones((3, 3))
        labeled_array, num_features = label(binary_map, structure)

        components = []
        for i in range(1, num_features + 1):
            # Get component coordinates
            coords = np.where(labeled_array == i)

            # Calculate component properties
            component = {
                "mean_azimuth": np.mean(coords[0]),
                "mean_range": np.mean(coords[1]),
                "size": len(coords[0]),
                "coords": list(zip(coords[0], coords[1])),
            }
            components.append(component)

        return labeled_array, components

    def extract_signal_features(
        self, ppi_data: np.ndarray, component: Dict, window_size: int = 3
    ) -> np.ndarray:
        """
        Extract signal features for a component using a sliding window
        """
        az, r = int(component["mean_azimuth"]), int(component["mean_range"])
        half_win = window_size // 2

        # Extract window around component center
        window = np.zeros((window_size, window_size))
        for i in range(-half_win, half_win + 1):
            for j in range(-half_win, half_win + 1):
                az_idx = (az + i) % ppi_data.shape[0]  # Handle azimuth wrap-around
                r_idx = r + j
                if 0 <= r_idx < ppi_data.shape[1]:
                    window[i + half_win, j + half_win] = ppi_data[az_idx, r_idx]

        return window

    def calculate_edge_connections(
        self, components: List[Dict], frame_idx: int
    ) -> Tuple[List[Tuple[int, int]], List[float]]:
        """
        Calculate edge connections between components based on spatial-temporal proximity
        """
        edges = []
        edge_weights = []

        for i, comp1 in enumerate(components):
            for j, comp2 in enumerate(components):
                # Only connect components in adjacent frames
                if abs(frame_idx - j) != 1:
                    continue

                # Calculate Euclidean distance
                az1, r1 = comp1["mean_azimuth"], comp1["mean_range"]
                az2, r2 = comp2["mean_azimuth"], comp2["mean_range"]

                # Convert to Cartesian coordinates
                x1 = r1 * np.cos(np.radians(az1))
                y1 = r1 * np.sin(np.radians(az1))
                x2 = r2 * np.cos(np.radians(az2))
                y2 = r2 * np.sin(np.radians(az2))

                distance = np.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2)

                print(self.connection_threshold)
                if distance <= self.connection_threshold:
                    edges.append((i, j))
                    edge_weights.append(1.0 - distance / self.connection_threshold)

        return edges, edge_weights

    def process_radar_frame(
        self, ppi_data: np.ndarray, frame_idx: int
    ) -> Tuple[List[Dict], np.ndarray]:
        """
        Process a single radar frame
        """
        # 1. Threshold detection
        threshold = np.sort(ppi_data.flatten())[int(ppi_data.size * self.threshold_pfa)]
        binary_map = (ppi_data >= threshold).astype(np.float32)

        # 2. Connected component detection
        labeled_map, components = self.process_connected_components(binary_map)

        # 3. Extract features for each component
        for component in components:
            component["signal_features"] = self.extract_signal_features(
                ppi_data, component
            )
            component["frame_idx"] = frame_idx

        return components, labeled_map


class FeatureExtractionNet(nn.Module):
    def __init__(self, input_size: int = 3):
        super(FeatureExtractionNet, self).__init__()

        # Modified architecture to handle smaller input sizes
        self.layers = nn.ModuleList()

        # First convolution layer without pooling
        self.layers.append(nn.Conv2d(1, 32, kernel_size=3, padding=1))
        self.layers.append(nn.ReLU())

        # Second convolution layer with smaller kernel
        self.layers.append(nn.Conv2d(32, 64, kernel_size=2, padding=1))
        self.layers.append(nn.ReLU())

        # Adaptive pooling to ensure fixed output size
        self.adaptive_pool = nn.AdaptiveAvgPool2d((2, 2))

        # Calculate final feature size
        self.feature_size = 64 * 2 * 2  # channels * height * width
        self.fc = nn.Linear(self.feature_size, 32)

    def _calculate_feature_size(self, input_size: int, num_pools: int) -> int:
        size = input_size
        for _ in range(num_pools):
            size = size // 2
        return size * size * 32

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        # Print input shape for debugging
        # print("Input shape:", x.shape)

        # Ensure proper input dimensions
        if len(x.shape) == 3:
            x = x.unsqueeze(1)  # Add channel dimension
        elif len(x.shape) == 2:
            x = x.unsqueeze(0).unsqueeze(0)  # Add batch and channel dimensions

        # Ensure float32
        x = x.float()

        # Apply convolutional layers
        for layer in self.layers:
            x = layer(x)
            # print("After layer:", layer.__class__.__name__, "shape:", x.shape)

        # Apply adaptive pooling
        x = self.adaptive_pool(x)
        # print("After adaptive pooling shape:", x.shape)

        # Flatten and pass through fully connected layer
        x = x.view(x.size(0), -1)
        # print("After flatten shape:", x.shape)
        x = self.fc(x)
        # print("Final output shape:", x.shape)

        return x


class STFAGCNLayer(nn.Module):
    def __init__(self, in_channels: int, out_channels: int):
        super(STFAGCNLayer, self).__init__()
        self.gcn = GCNConv(in_channels, out_channels)

    def forward(
        self, x: torch.Tensor, edge_index: torch.Tensor, attention_weights: torch.Tensor
    ) -> torch.Tensor:
        # Apply GCN with attention weights
        out = self.gcn(x, edge_index)
        if attention_weights is not None:
            out = out * attention_weights.unsqueeze(-1)
        return out


class STFAGCN(nn.Module):
    def __init__(self, feature_dim: int = 32, hidden_dim: int = 64):
        super(STFAGCN, self).__init__()
        self.fe_net = FeatureExtractionNet()
        self.gcn1 = STFAGCNLayer(feature_dim, hidden_dim)
        self.gcn2 = STFAGCNLayer(hidden_dim, hidden_dim)
        self.fc = nn.Linear(hidden_dim, hidden_dim // 2)
        self.out = nn.Linear(hidden_dim // 2, 1)

    def calculate_attention1(
        self, aux_features: torch.Tensor, edge_index: torch.Tensor
    ) -> torch.Tensor:
        """
        Calculate first-order attention based on spatial-temporal information
        """
        attention = torch.ones(edge_index.size(1), device=edge_index.device)

        for i in range(edge_index.size(1)):
            src, dst = edge_index[:, i]
            src_frame = aux_features[src, 2]  # frame index
            dst_frame = aux_features[dst, 2]

            # Only assign attention to nodes in adjacent frames
            if abs(src_frame - dst_frame) == 1:
                attention[i] = 1.0 / len(torch.where(edge_index[0] == src)[0])
            else:
                attention[i] = 0.0

        return attention

    def calculate_attention2(
        self, aux_features: torch.Tensor, edge_index: torch.Tensor
    ) -> torch.Tensor:
        """
        Calculate second-order attention based on path information
        """
        attention = torch.ones(edge_index.size(1), device=edge_index.device)

        for i in range(edge_index.size(1)):
            src, dst = edge_index[:, i]

            # Extract position information
            src_pos = aux_features[src, :2]  # azimuth, range
            dst_pos = aux_features[dst, :2]
            frame_diff = abs(aux_features[src, 2] - aux_features[dst, 2])

            if frame_diff == 2:  # Second-order neighbors
                # Calculate displacement vector
                src_cart = torch.tensor(
                    [
                        src_pos[1] * torch.cos(src_pos[0]),
                        src_pos[1] * torch.sin(src_pos[0]),
                    ],
                    device=edge_index.device,  # Added device specification
                )
                dst_cart = torch.tensor(
                    [
                        dst_pos[1] * torch.cos(dst_pos[0]),
                        dst_pos[1] * torch.sin(dst_pos[0]),
                    ],
                    device=edge_index.device,  # Added device specification
                )
                displacement = dst_cart - src_cart

                # Calculate attention based on displacement consistency
                velocity = torch.norm(displacement) / frame_diff
                attention[i] = torch.exp(-velocity / 8.5)  # 8.5 m/s is our max velocity
            else:
                attention[i] = 0.0

        return attention

    def forward(self, data: Data) -> torch.Tensor:
        # Extract features using FE-Net
        signal_features = self.fe_net(data.x)

        # First GCN layer with first-order attention
        attention1 = self.calculate_attention1(data.aux_features, data.edge_index)
        x1 = self.gcn1(signal_features, data.edge_index, attention1)
        x1 = F.relu(x1)

        # Second GCN layer with second-order attention
        attention2 = self.calculate_attention2(data.aux_features, data.edge_index)
        x2 = self.gcn2(x1, data.edge_index, attention2)
        x2 = F.relu(x2)

        # Classification
        x = F.relu(self.fc(x2))
        out = torch.sigmoid(self.out(x))
        return out.squeeze(-1).unsqueeze(-1)  # Ensure output shape is [batch_size, 1]


def create_graph_dataset(json_data: List[Dict]) -> Data:
    """
    Create a PyTorch Geometric dataset from processed radar data
    """
    processor = RadarDataProcessor()
    all_components = []
    edge_list = []
    edge_weights = []

    # Process each frame
    for frame_idx, frame_data in enumerate(json_data):
        ppi_data = np.array(frame_data["PPI"])
        components, _ = processor.process_radar_frame(ppi_data, frame_idx)

        # Get ground truth ship positions
        ships = frame_data.get("ships", [])

        # Label components based on proximity to ground truth ships
        for comp in components:
            # Convert component position to polar coordinates
            comp_az = comp["mean_azimuth"]  # Already in degrees
            comp_range = comp["mean_range"] * (
                frame_data["range"] / ppi_data.shape[1]
            )  # Scale to actual range

            # Check if component corresponds to any ground truth ship
            is_ship = False
            for ship in ships:
                ship_az = ship["Azimuth"]
                ship_range = ship["Distance"]

                # Calculate distance between component and ship
                # Convert to Cartesian coordinates for accurate distance calculation
                comp_x = comp_range * np.cos(np.radians(comp_az))
                comp_y = comp_range * np.sin(np.radians(comp_az))
                ship_x = ship_range * np.cos(np.radians(ship_az))
                ship_y = ship_range * np.sin(np.radians(ship_az))

                distance = np.sqrt((comp_x - ship_x) ** 2 + (comp_y - ship_y) ** 2)

                # If component is within threshold distance of ship, mark as true target
                if distance < 2000:  # 100m threshold, adjust as needed
                    is_ship = True
                    break

            comp["is_ship"] = is_ship

        # Store components
        start_idx = len(all_components)
        all_components.extend(components)

        # Calculate edges for this frame
        edges, weights = processor.calculate_edge_connections(components, frame_idx)

        # Adjust edge indices
        edges = [(i + start_idx, j + start_idx) for i, j in edges]
        edge_list.extend(edges)
        edge_weights.extend(weights)

    if not all_components:
        raise ValueError("No components found in the data")

    # Create feature matrices
    features = [comp["signal_features"] for comp in all_components]
    max_shape = max(f.shape for f in features)
    padded_features = []
    for f in features:
        if f.shape != max_shape:
            pad_width = [(0, max_shape[i] - f.shape[i]) for i in range(len(f.shape))]
            padded_f = np.pad(f, pad_width, mode="constant")
            padded_features.append(padded_f)
        else:
            padded_features.append(f)

    x = torch.tensor(np.array(padded_features, dtype=np.float32))

    aux_features = torch.tensor(
        [
            [comp["mean_azimuth"], comp["mean_range"], comp["frame_idx"]]
            for comp in all_components
        ],
        dtype=torch.float32,
    )

    if not edge_list:
        edge_list = [(i, i) for i in range(len(all_components))]
        edge_weights = [1.0] * len(all_components)

    edge_index = torch.tensor(edge_list, dtype=torch.long).t().contiguous()
    edge_weight = torch.tensor(edge_weights, dtype=torch.float32)

    # Create labels based on the is_ship flag
    y = torch.tensor(
        [[1.0] if comp["is_ship"] else [0.0] for comp in all_components],
        dtype=torch.float32,
    )

    return Data(
        x=x,
        edge_index=edge_index,
        edge_weight=edge_weight,
        aux_features=aux_features,
        y=y,
    )


def train_stfagcn(
    model: STFAGCN,
    train_loader: DataLoader,
    optimizer: torch.optim.Optimizer,
    epochs: int = 100,
):
    """
    Training function for STFA-GCN
    """
    model.train()
    device = next(model.parameters()).device

    for epoch in range(epochs):
        total_loss = 0
        for batch in train_loader:
            # Move batch to device and ensure float32
            batch = batch.to(device)
            batch.x = batch.x.float()
            batch.edge_weight = batch.edge_weight.float()
            batch.aux_features = batch.aux_features.float()
            batch.y = batch.y.float()  # Ensure target is float32

            optimizer.zero_grad()
            out = model(batch)

            # Ensure shapes match for loss calculation
            loss = F.binary_cross_entropy(out, batch.y)
            loss.backward()
            optimizer.step()
            total_loss += loss.item()

        print(f"Epoch {epoch+1}, Loss: {total_loss/len(train_loader):.4f}")


def evaluate_stfagcn(model: STFAGCN, test_loader: DataLoader) -> Dict[str, float]:
    """
    Evaluation function for STFA-GCN
    """
    model.eval()
    device = next(model.parameters()).device
    predictions = []
    targets = []

    with torch.no_grad():
        for batch in test_loader:
            batch = batch.to(device)
            batch.x = batch.x.float()
            batch.edge_weight = batch.edge_weight.float()
            batch.aux_features = batch.aux_features.float()
            batch.y = batch.y.float()

            out = model(batch)
            pred = (out.cpu().numpy() > 0.5).astype(
                float
            )  # Convert probabilities to binary predictions
            predictions.extend(pred)
            targets.extend(batch.y.cpu().numpy())

    predictions = np.array(predictions)
    targets = np.array(targets)

    # Handle potential warning by setting zero_division parameter
    metrics = {
        "accuracy": accuracy_score(targets, predictions),
        "precision": precision_score(targets, predictions),
        "recall": recall_score(targets, predictions),
        "f1": f1_score(targets, predictions),
    }

    return metrics


def load_json_files(dataset_path):
    """Load all JSON files from the dataset directory"""
    json_files = glob.glob(os.path.join(dataset_path, "*.json"))
    all_data = []

    for file_path in json_files:
        with open(file_path, "r") as f:
            data = json.load(f)
            all_data.append(data)

    print(f"Loaded {len(all_data)} JSON files")
    return all_data


def main():
    # Set device
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Using device: {device}")

    # Set default dtype
    torch.set_default_dtype(torch.float32)

    # Load dataset
    dataset_path = os.path.expanduser(
        r"D:\MYA\AUS\Bachelor\Senior 2\CMP 491\NewestDataset"
    )
    json_data = load_json_files(dataset_path)

    # Create graph datasets
    print("Creating graph datasets...")
    graph_data = []
    for data in json_data:
        try:
            graph = create_graph_dataset([data])
            graph_data.append(graph)
        except Exception as e:
            print(f"Skipping problematic data file: {e}")

    if not graph_data:
        raise ValueError("No valid graph data created")

    print(f"Created {len(graph_data)} valid graph datasets")

    # Split dataset into train and test
    train_graphs, test_graphs = train_test_split(graph_data, test_size=0.2)

    # Create data loaders with proper device placement
    train_loader = DataLoader(train_graphs, batch_size=32, shuffle=True)
    test_loader = DataLoader(test_graphs, batch_size=32)

    # Initialize model and move to device
    model = STFAGCN().to(device)
    optimizer = torch.optim.Adam(
        model.parameters(), lr=0.001
    )  # ADAM & lr = 10^-3 were used in the paper

    # Train model
    print("Starting training...")
    train_stfagcn(
        model, train_loader, optimizer, epochs=50
    )  # 3000 epochs were used in the paper

    # Evaluate model
    print("\nEvaluating model...")
    metrics = evaluate_stfagcn(model, test_loader)

    # Print results
    print("\nResults:")
    for metric, value in metrics.items():
        print(f"{metric.capitalize()}: {value:.4f}")

    # Save model
    torch.save(model.state_dict(), r"ML\STFA-GCN\stfagcn_model.pth")
    print("\nModel saved as 'stfagcn_model.pth'")


if __name__ == "__main__":
    main()
