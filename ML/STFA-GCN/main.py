import argparse
import os
from torch.utils.data import DataLoader
import torch
import torch.nn as nn
import torch.nn.functional as F
import numpy as np
import json
import matplotlib.pyplot as plt
from scipy.ndimage import measurements
from torch_geometric.data import Data, Batch
import networkx as nx


class FENet(nn.Module):
    """Feature Extraction Network as described in the paper"""

    def __init__(self):
        super(FENet, self).__init__()
        # Two convolutional layers and two pooling layers
        self.conv1 = nn.Conv2d(1, 16, kernel_size=3, padding=1)
        self.pool1 = nn.MaxPool2d(2, 2)
        self.conv2 = nn.Conv2d(16, 32, kernel_size=3, padding=1)
        self.pool2 = nn.MaxPool2d(2, 2)
        self.fc = nn.Linear(
            32 * 2 * 2, 32
        )  # Final output dimension of 32 as mentioned in paper

    def forward(self, x):
        # Input shape: (batch_size, 1, 11, 11) as per paper
        x = self.pool1(F.relu(self.conv1(x)))
        x = self.pool2(F.relu(self.conv2(x)))
        x = x.view(x.size(0), -1)
        x = self.fc(x)
        return x


class RadarDataProcessor:
    """Process raw radar data into graph format"""

    def __init__(self, threshold=0.01, window_size=11):
        self.threshold = threshold
        self.window_size = window_size
        self.max_velocity = 10  # 10 m/s as mentioned in paper

    def load_json_data(self, json_file):
        """Load and parse JSON radar data"""
        with open(json_file, "r") as f:
            data = json.load(f)
        return np.array(data["PPI"])

    def detect_connected_components(self, frame):
        """Perform connected component detection on binary detection result"""
        binary_frame = frame > self.threshold
        labeled_array, num_features = measurements.label(binary_frame)
        return labeled_array, num_features

    def extract_node_features(self, frame, center_x, center_y):
        """Extract 11x11 window around detection point"""
        half_window = self.window_size // 2
        window = frame[
            max(0, center_x - half_window) : min(
                frame.shape[0], center_x + half_window + 1
            ),
            max(0, center_y - half_window) : min(
                frame.shape[1], center_y + half_window + 1
            ),
        ]
        # Pad if necessary
        if window.shape != (self.window_size, self.window_size):
            padded_window = np.zeros((self.window_size, self.window_size))
            padded_window[: window.shape[0], : window.shape[1]] = window
            window = padded_window
        return window

    def calculate_auxiliary_features(self, component, frame_idx):
        """Calculate auxiliary features (az, r, k, c) for each node"""
        coords = np.where(component)
        mean_azimuth = np.mean(coords[0])
        mean_range = np.mean(coords[1])
        component_size = len(coords[0])

        return {
            "azimuth": mean_azimuth,
            "range": mean_range,
            "frame": frame_idx,
            "size": component_size,
        }

    def build_graph(self, frames):
        """Convert radar frames into graph data with nodes and edges."""
        nodes = []
        edges = []
        node_features = []
        auxiliary_features = []

        for frame_idx, frame in enumerate(frames):
            labeled_array, num_components = self.detect_connected_components(frame)

            for component_idx in range(1, num_components + 1):
                component = labeled_array == component_idx
                center_x, center_y = np.mean(np.where(component), axis=1)

                window = self.extract_node_features(frame, int(center_x), int(center_y))
                aux_features = self.calculate_auxiliary_features(component, frame_idx)

                nodes.append((center_x, center_y, frame_idx))
                node_features.append(window)
                auxiliary_features.append(aux_features)

        # Add edges based on spatial-temporal relationships
        for i, (x1, y1, f1) in enumerate(nodes):
            for j, (x2, y2, f2) in enumerate(nodes):
                if abs(f1 - f2) == 1:  # Connecting nodes in adjacent frames
                    distance = np.sqrt((x1 - x2) ** 2 + (y1 - y2) ** 2)
                    if distance <= self.max_velocity:
                        edges.append((i, j))

        return nodes, edges, node_features, auxiliary_features


class GraphVisualizer:
    """Visualize graph structure and attention weights"""

    def __init__(self):
        self.colors = plt.cm.viridis

    def visualize_graph(self, nodes, edges, attention_weights=None):
        """Create visualization of the graph structure"""
        G = nx.Graph()

        # Add nodes
        for i, (x, y, f) in enumerate(nodes):
            G.add_node(i, pos=(x, y), frame=f)

        # Add edges
        for i, j in edges:
            weight = attention_weights[i][j] if attention_weights is not None else 1.0
            G.add_edge(i, j, weight=weight)

        # Draw graph
        plt.figure(figsize=(12, 8))
        pos = nx.get_node_attributes(G, "pos")

        # Draw nodes colored by frame number
        frame_numbers = [G.nodes[n]["frame"] for n in G.nodes()]
        nx.draw_networkx_nodes(
            G, pos, node_color=frame_numbers, cmap=self.colors, node_size=100
        )

        # Draw edges with width proportional to attention weights
        if attention_weights is not None:
            edge_weights = [G[u][v]["weight"] for (u, v) in G.edges()]
            nx.draw_networkx_edges(G, pos, width=edge_weights)
        else:
            nx.draw_networkx_edges(G, pos)

        plt.colorbar(plt.cm.ScalarMappable(cmap=self.colors))
        plt.title("Radar Detection Graph Structure")
        plt.axis("equal")
        return plt.gcf()


class RadarDataset(torch.utils.data.Dataset):
    """Dataset class for radar data"""

    def __init__(self, nodes, edges, node_features, auxiliary_features, labels=None):
        self.nodes = nodes
        self.edges = edges
        self.node_features = torch.FloatTensor(node_features)
        self.auxiliary_features = auxiliary_features
        self.labels = torch.LongTensor(labels) if labels is not None else None

    def __len__(self):
        return len(self.nodes)

    def __getitem__(self, idx):
        data = Data(
            x=self.node_features[idx],
            edge_index=torch.LongTensor(self.edges).t().contiguous(),
            aux_features=self.auxiliary_features[idx],
        )
        if self.labels is not None:
            data.y = self.labels[idx]
        return data


def train_and_visualize(model, train_loader, test_loader, optimizer, epochs=1):
    """Training function with visualization"""
    model.train()
    losses = []
    accuracies = []

    for epoch in range(epochs):
        epoch_loss = 0
        correct = 0
        total = 0

        for batch in train_loader:
            optimizer.zero_grad()
            out = model(batch)
            loss = F.nll_loss(out, batch.y)
            loss.backward()
            optimizer.step()

            epoch_loss += loss.item()
            pred = out.max(1)[1]
            correct += pred.eq(batch.y).sum().item()
            total += batch.y.size(0)

        epoch_loss /= len(train_loader)
        epoch_acc = correct / total
        losses.append(epoch_loss)
        accuracies.append(epoch_acc)

        if (epoch + 1) % 100 == 0:
            print(f"Epoch {epoch+1}/{epochs}:")
            print(f"  Training Loss: {epoch_loss:.4f}")
            print(f"  Training Accuracy: {epoch_acc:.4f}")

            # Evaluate on test set
            model.eval()
            test_correct = 0
            test_total = 0
            with torch.no_grad():
                for batch in test_loader:
                    out = model(batch)
                    pred = out.max(1)[1]
                    test_correct += pred.eq(batch.y).sum().item()
                    test_total += batch.y.size(0)
            test_acc = test_correct / test_total
            print(f"  Test Accuracy: {test_acc:.4f}")
            model.train()

    # Plot training curves
    plt.figure(figsize=(12, 4))
    plt.subplot(1, 2, 1)
    plt.plot(losses)
    plt.title("Training Loss")
    plt.xlabel("Epoch")
    plt.ylabel("Loss")

    plt.subplot(1, 2, 2)
    plt.plot(accuracies)
    plt.title("Training Accuracy")
    plt.xlabel("Epoch")
    plt.ylabel("Accuracy")

    return plt.gcf()


class SpatialTemporalAttention(nn.Module):
    """Spatial-Temporal Feature Attention mechanism for GCN."""

    def __init__(self):
        super(SpatialTemporalAttention, self).__init__()

    def calculate_attention_1(self, nodes_i, neighbors):
        # Basic attention based on node count (equal attention weights)
        return torch.ones(len(neighbors)) / len(neighbors)

    def calculate_attention_2(self, node_i, node_j, node_k):
        """Attention mechanism for second-order neighbors based on displacement."""
        # Logic for calculating second-order neighbor attention
        d_jk = self.calculate_displacement(node_j, node_k)
        d_ki = self.calculate_displacement(node_k, node_i)

        # Calculating attention weight based on spatial displacement vectors
        return torch.dot(d_jk, d_ki) / (torch.norm(d_jk) * torch.norm(d_ki))

    def calculate_displacement(self, node1, node2):
        # Calculate displacement between two nodes (based on range and azimuth)
        r1, az1 = node1["range"], node1["azimuth"]
        r2, az2 = node2["range"], node2["azimuth"]

        x1, y1 = r1 * torch.cos(az1), r1 * torch.sin(az1)
        x2, y2 = r2 * torch.cos(az2), r2 * torch.sin(az2)

        return torch.tensor([x2 - x1, y2 - y1])


class STFAGCN(nn.Module):
    """Spatial-Temporal Feature Attention Graph Convolutional Network."""

    def __init__(self, input_dim=32, hidden_dim=64, output_dim=2):
        super(STFAGCN, self).__init__()
        self.fe_net = FENet()
        self.attention = SpatialTemporalAttention()

        self.gc1 = nn.Linear(input_dim, hidden_dim)
        self.gc2 = nn.Linear(hidden_dim, hidden_dim)

        self.fc = nn.Linear(hidden_dim, hidden_dim // 2)
        self.output = nn.Linear(hidden_dim // 2, output_dim)

    def normalize_adjacency(self, adj):
        """Normalize adjacency matrix with self-loops."""
        adj = adj + torch.eye(adj.size(0))
        degree = torch.sum(adj, dim=1)
        return (degree ** (-0.5)).unsqueeze(-1) * adj * (degree ** (-0.5)).unsqueeze(-2)

    def forward(self, data):
        x = self.fe_net(data.x)
        adj1 = self.normalize_adjacency(data.adj1)
        att1 = self.attention.calculate_attention_1(data.nodes, data.neighbors1)

        h1 = F.relu(self.gc1(torch.mm(adj1 * att1, x)))

        adj2 = self.normalize_adjacency(data.adj2)
        att2 = self.attention.calculate_attention_2(
            data.nodes, data.neighbors2, data.neighbors1
        )

        h2 = F.relu(self.gc2(torch.mm(adj2 * att2, h1)))

        out = F.relu(self.fc(h2))
        return F.log_softmax(self.output(out), dim=1)


def main():
    # Parse command-line arguments
    parser = argparse.ArgumentParser(description="Train and Evaluate STFA-GCN Model")
    parser.add_argument(
        "data_dir", type=str, help="Directory path containing radar JSON files"
    )
    args = parser.parse_args()

    # Initialize processor and load data
    processor = RadarDataProcessor()
    raw_data_files = [
        os.path.join(args.data_dir, f)
        for f in os.listdir(args.data_dir)
        if f.endswith(".json")
    ]

    # Load all JSON data files
    radar_frames = []
    for file_path in raw_data_files:
        radar_frames.append(processor.load_json_data(file_path))

    # Process data into graph format
    nodes, edges, node_features, aux_features = processor.build_graph(radar_frames)

    # Create dataset and data loaders
    dataset = RadarDataset(nodes, edges, node_features, aux_features)
    train_size = int(0.8 * len(dataset))
    test_size = len(dataset) - train_size
    train_dataset, test_dataset = torch.utils.data.random_split(
        dataset, [train_size, test_size]
    )

    train_loader = DataLoader(train_dataset, batch_size=256, shuffle=True)
    test_loader = DataLoader(test_dataset, batch_size=256, shuffle=False)

    # Initialize model and optimizer
    model = STFAGCN()
    optimizer = torch.optim.Adam(model.parameters(), lr=1e-3)

    # Train model and visualize results
    training_fig = train_and_visualize(model, train_loader, test_loader, optimizer)
    plt.show()


if __name__ == "__main__":
    main()