# dataset.py
import os
import json
import numpy as np
import torch
from torch.utils.data import Dataset
from torchvision import transforms
from PIL import Image


class PPIDataset(Dataset):
    def __init__(self, json_dir, output_size=None, sigma=2, transform=None):
        """
        Args:
            json_dir: Directory containing JSON files
            output_size: Optional tuple (height, width) for resizing.
                        If None, uses original image size
            sigma: Standard deviation for Gaussian kernel
            transform: Optional transform to be applied on the image
        """
        self.json_dir = json_dir
        self.transform = transform
        self.sigma = sigma
        self.json_files = [file for file in os.listdir(
            json_dir) if file.endswith('.json')]

        # Determine output size from first image if not specified
        if output_size is None:
            first_json = os.path.join(json_dir, self.json_files[0])
            with open(first_json, 'r') as file:
                data = json.load(file)
                ppi_array = np.array(data['PPI'])
                self.output_size = ppi_array.shape
        else:
            self.output_size = output_size

    def generate_heatmap(self, ships, original_size, radar_range):
        heatmap = np.zeros(self.output_size)

        for ship in ships:
            # Get original coordinates
            x, y = ship['Azimuth'], ship['Distance']

            # Scale coordinates if output size is different from original size
            if self.output_size != original_size:
                x = x * (self.output_size[1] / original_size[1])
                y = y * (self.output_size[0] / original_size[0])

            # Convert normalized coordinates (if they are normalized)
            # x_scaled = int(x * self.output_size[1] if x <= 1 else x)
            # y_scaled = int(y * self.output_size[0] if y <= 1 else y)
            x_scaled = int(x / 360 * self.output_size[0])
            y_scaled = int(y / radar_range * self.output_size[1])

            # Ensure coordinates are within bounds
            x_scaled = min(max(0, x_scaled), self.output_size[0] - 1)
            y_scaled = min(max(0, y_scaled), self.output_size[1] - 1)

            # Generate 2D Gaussian
            tmp_size = 6 * self.sigma + 1
            g = np.zeros((tmp_size, tmp_size))
            center = tmp_size // 2
            for i in range(tmp_size):
                for j in range(tmp_size):
                    g[i, j] = np.exp(-((i-center)**2 + (j-center)
                                     ** 2) / (2*self.sigma**2))

            # Add Gaussian to heatmap
            x_left = max(0, x_scaled - center)
            x_right = min(self.output_size[0], x_scaled + center + 1)
            y_left = max(0, y_scaled - center)
            y_right = min(self.output_size[1], y_scaled + center + 1)

            g_x_left = max(0, center-(x_scaled-x_left))
            g_x_right = min(tmp_size, center+(x_right-x_scaled))
            g_y_left = max(0, center-(y_scaled-y_left))
            g_y_right = min(tmp_size, center+(y_right-y_scaled))

            heatmap[x_left:x_right, y_left:y_right] = np.maximum(
                heatmap[x_left:x_right, y_left:y_right],
                g[g_x_left:g_x_right, g_y_left:g_y_right]
            )

        return heatmap

    def __len__(self):
        return len(self.json_files)

   

    def __getitem__(self, idx):
        json_path = os.path.join(self.json_dir, self.json_files[idx])

        with open(json_path, 'r') as file:
            data = json.load(file)

        # Load PPI array
        ppi_array = np.array(data['PPI'], dtype=np.float32)
        original_size = ppi_array.shape

        # Resize if necessary
        if self.output_size != original_size:
            ppi_array = Image.fromarray(ppi_array).resize(
                (self.output_size[1], self.output_size[0]),
                Image.BILINEAR
            )
            ppi_array = np.array(ppi_array, dtype=np.float32)

        # Normalize image to [0, 1]
        ppi_array = (ppi_array - ppi_array.min()) / \
            (ppi_array.max() - ppi_array.min() + 1e-8)

        # Convert to tensor and add channel dimension
        image = torch.FloatTensor(ppi_array).unsqueeze(0)

        # Generate heatmap from ship coordinates
        heatmap = self.generate_heatmap(
            data['ships'], original_size, data['range'])
        heatmap = torch.FloatTensor(heatmap).unsqueeze(0)

        return image, heatmap

    def get_image_size(self):
        """Returns the size of the images in the dataset"""
        return self.output_size
