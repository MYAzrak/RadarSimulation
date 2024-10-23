import os
import json
import numpy as np
import torch
from torch.utils.data import Dataset
from torchvision import transforms
from PIL import Image
import matplotlib.pyplot as plt


class PPIDataset(Dataset):
    def __init__(self, json_dir, transform=None):
        self.json_dir = json_dir
        self.transform = transform
        self.json_files = [file for file in os.listdir(
            json_dir) if file.endswith('.json')]

    def __len__(self):
        return len(self.json_files)

    def __getitem__(self, idx):
        json_path = os.path.join(self.json_dir, self.json_files[idx])

        with open(json_path, 'r') as file:
            data = json.load(file)

        ppi_array = np.array(data['PPI'], dtype=np.float32)

        # Convert to PIL Image (Assuming PPI is in a format that can be converted)
        image = Image.fromarray(ppi_array)
        ships = data['ships']

        # Apply any transformations
        if self.transform:
            image = self.transform(image)

        return image, ships


# Usage example
json_directory = os.path.expanduser('~/Downloads/output')
transform = transforms.Compose([
    transforms.ToTensor(),
])

dataset = PPIDataset(json_directory, transform=transform)

'''
for i in range(len(dataset)):
    img = dataset[i]
    plt.imshow(img.permute(1, 2, 0), cmap='gray')
    plt.axis('off')  # Hide axes
    plt.show()  # Display the image
'''

img = dataset[3]
plt.imshow(img.permute(2, 1, 0), cmap='gray')
plt.axis('off')  # Hide axes
plt.show()  # Display the image
