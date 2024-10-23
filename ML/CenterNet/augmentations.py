import torch
import random
import numpy as np
from torch.utils.data import Dataset
from itertools import product


class AugmentedRadarDataset(Dataset):
    """
    Dataset wrapper that creates augmented versions of the original data
    """

    def __init__(self, base_dataset, flip_prob=0.5, num_shifts=2, shift_fraction=0.15):
        """
        Args:
            base_dataset: Original radar dataset
            flip_prob: Probability of flipping (0.5 means half the augmented samples will be flipped)
            num_shifts: Number of shifted versions to create for each sample
            shift_fraction: Maximum fraction of image width to shift
        """
        self.base_dataset = base_dataset
        self.flip_prob = flip_prob
        self.num_shifts = num_shifts
        self.shift_fraction = shift_fraction

        # Calculate total number of augmented samples
        self.num_base_samples = len(base_dataset)
        self.augmentation_types = []

        # Original samples
        self.augmentation_types.append(('original', None))

        # Flipped samples
        if flip_prob > 0:
            self.augmentation_types.append(('flip', None))

        # Shifted samples
        if num_shifts > 0:
            height = base_dataset[0][0].shape[1]  # Get azimuth dimension size
            max_shift = int(height * shift_fraction)
            shifts = np.linspace(-max_shift, max_shift, num_shifts, dtype=int)
            for shift in shifts:
                if shift != 0:  # Don't include zero shift as it's same as original
                    self.augmentation_types.append(('shift', shift))
                    # Also add flipped versions of shifted samples if flipping is enabled
                    if flip_prob > 0:
                        self.augmentation_types.append(('flip_shift', shift))

        # Log the augmentation setup
        print(f"Dataset size will increase from {self.num_base_samples} to "
              f"{self.num_base_samples * len(self.augmentation_types)} samples")

    def __len__(self):
        return self.num_base_samples * len(self.augmentation_types)

    def __getitem__(self, idx):
        # Get base sample index and augmentation type
        base_idx = idx // len(self.augmentation_types)
        aug_idx = idx % len(self.augmentation_types)
        aug_type, param = self.augmentation_types[aug_idx]

        # Get base sample
        image, heatmap = self.base_dataset[base_idx]

        # Apply augmentations based on type
        if aug_type == 'original':
            return image, heatmap

        if aug_type == 'flip':
            return self._flip(image, heatmap)

        if aug_type == 'shift':
            return self._shift(image, heatmap, param)

        if aug_type == 'flip_shift':
            image, heatmap = self._flip(image, heatmap)
            return self._shift(image, heatmap, param)

        return image, heatmap

    def _flip(self, image, heatmap):
        """Flip along azimuth dimension"""
        image = image.clone()
        heatmap = heatmap.clone()
        return torch.flip(image, [1]), torch.flip(heatmap, [1])

    def _shift(self, image, heatmap, shift_amount):
        """Circular shift along azimuth dimension"""
        image = image.clone()
        heatmap = heatmap.clone()
        return (torch.roll(image, shifts=shift_amount, dims=1),
                torch.roll(heatmap, shifts=shift_amount, dims=1))
