# centernet.py
import torch
import torch.nn as nn
import torch.nn.functional as F
import numpy as np
from sklearn.cluster import DBSCAN
from math import sqrt, atan2, pi


def convert_to_polar(point, image_size):
    """Convert (x, y) coordinates to (azimuth, distance) in polar coordinates"""
    center_x = image_size[1] / 2
    center_y = image_size[0] / 2

    # Translate point to origin at center
    x = point[0] - center_x
    y = point[1] - center_y

    # Calculate distance
    distance = sqrt(x**2 + y**2)

    # Calculate azimuth (in degrees)
    azimuth = atan2(y, x) * 180 / pi
    if azimuth < 0:
        azimuth += 360

    return np.array([azimuth, distance])


def detect_points(heatmap, threshold=0.3, nms_kernel_size=3, eps=20, min_samples=2):
    """
    Extract points from a heatmap using non-maximum suppression and DBSCAN clustering

    Args:
        heatmap: The prediction heatmap
        threshold: Detection threshold for the heatmap
        nms_kernel_size: Kernel size for non-maximum suppression
        eps: The maximum distance between two samples for DBSCAN clustering
        min_samples: The minimum number of samples in a neighborhood for a point to be considered a core point
    """
    with torch.no_grad():
        # Apply NMS
        pad = (nms_kernel_size - 1) // 2
        hmax = F.max_pool2d(heatmap.unsqueeze(0).unsqueeze(0),
                            kernel_size=nms_kernel_size,
                            stride=1,
                            padding=pad)
        hmax = hmax[0, 0]

        # Find peaks
        keep = (heatmap == hmax) & (heatmap > threshold)
        ys, xs = torch.where(keep)

        points = np.array([(x.item(), y.item()) for x, y in zip(xs, ys)])

        if len(points) == 0:
            return []

        # Convert points to polar coordinates for clustering
        image_size = heatmap.shape
        polar_points = np.array([convert_to_polar(p, image_size) for p in points])

        # Scale the coordinates to handle the circular nature of azimuth
        # This ensures that points at 359° and 1° are considered close
        X = np.column_stack(
            [
                np.sin(polar_points[:, 0] * pi / 180) * polar_points[:, 1],
                np.cos(polar_points[:, 0] * pi / 180) * polar_points[:, 1],
            ]
        )

        # Apply DBSCAN clustering
        db = DBSCAN(eps=eps, min_samples=min_samples).fit(X)
        labels = db.labels_

        # Calculate cluster centers
        final_points = []
        unique_labels = np.unique(labels)
        for label in unique_labels:
            if label == -1:  # Noise points
                noise_points = points[labels == label]
                final_points.extend([(x, y) for x, y in noise_points])
            else:
                # Get all points in the cluster
                cluster_points = points[labels == label]
                # Take the point with highest heatmap value as the representative
                heatmap_values = [
                    heatmap[int(y), int(x)].item() for x, y in cluster_points
                ]
                best_point = cluster_points[np.argmax(heatmap_values)]
                final_points.append((best_point[0], best_point[1]))

        return final_points


class ResNetBlock(nn.Module):
    def __init__(self, in_channels, out_channels, stride=1):
        super().__init__()
        self.conv1 = nn.Conv2d(in_channels, out_channels, kernel_size=3, stride=stride, padding=1, bias=False)
        self.bn1 = nn.BatchNorm2d(out_channels)
        self.conv2 = nn.Conv2d(out_channels, out_channels, kernel_size=3, stride=1, padding=1, bias=False)
        self.bn2 = nn.BatchNorm2d(out_channels)
        
        self.shortcut = nn.Sequential()
        if stride != 1 or in_channels != out_channels:
            self.shortcut = nn.Sequential(
                nn.Conv2d(in_channels, out_channels, kernel_size=1, stride=stride, bias=False),
                nn.BatchNorm2d(out_channels)
            )

    def forward(self, x):
        out = F.relu(self.bn1(self.conv1(x)))
        out = self.bn2(self.conv2(out))
        out += self.shortcut(x)
        out = F.relu(out)
        return out

class CenterNetBackbone(nn.Module):
    def __init__(self, in_channels=1):
        super().__init__()
        
        # Initial convolution to handle single channel input
        self.initial = nn.Sequential(
            nn.Conv2d(in_channels, 64, kernel_size=7, stride=2, padding=3, bias=False),
            nn.BatchNorm2d(64),
            nn.ReLU(inplace=True)
        )
        
        # ResNet layers
        self.layer1 = nn.Sequential(
            ResNetBlock(64, 64),
            ResNetBlock(64, 64)
        )
        
        self.layer2 = nn.Sequential(
            ResNetBlock(64, 128, stride=2),
            ResNetBlock(128, 128)
        )
        
        self.layer3 = nn.Sequential(
            ResNetBlock(128, 256, stride=2),
            ResNetBlock(256, 256)
        )
        
        # Upsampling layers
        self.deconv1 = nn.Sequential(
            nn.ConvTranspose2d(256, 128, kernel_size=4, stride=2, padding=1),
            nn.BatchNorm2d(128),
            nn.ReLU(inplace=True)
        )
        
        self.deconv2 = nn.Sequential(
            nn.ConvTranspose2d(128, 64, kernel_size=4, stride=2, padding=1),
            nn.BatchNorm2d(64),
            nn.ReLU(inplace=True)
        )
        
        # Final layers to match input dimensions
        self.head = nn.Sequential(
            nn.Conv2d(64, 32, kernel_size=3, padding=1),
            nn.BatchNorm2d(32),
            nn.ReLU(inplace=True),
            nn.Conv2d(32, 1, kernel_size=1),
            nn.UpsamplingBilinear2d(scale_factor=2)
        )

    def forward(self, x):
        # Initial convolution
        x = self.initial(x)  # [8, 64, 360, 500]
        
        # ResNet blocks
        x = self.layer1(x)   # [8, 64, 360, 500]
        x = self.layer2(x)   # [8, 128, 180, 250]
        x = self.layer3(x)   # [8, 256, 90, 125]
        
        # Upsampling
        x = self.deconv1(x)  # [8, 128, 180, 250]
        x = self.deconv2(x)  # [8, 64, 360, 500]
        
        # Final prediction
        heatmap = torch.sigmoid(self.head(x))  # [8, 1, 720, 1000]
        
        return heatmap
class FocalLoss(nn.Module):
    def __init__(self, alpha=2, beta=4):
        super().__init__()
        self.alpha = alpha
        self.beta = beta

    def forward(self, pred, target):
        # Ensure pred and target have the same size
        if pred.shape != target.shape:
            target = F.interpolate(
                target, size=pred.shape[2:], mode='bilinear', align_corners=True)

        pos_inds = (target > 0.5).float()
        neg_inds = (target <= 0.5).float()

        # Add small epsilon to prevent log(0)
        eps = 1e-6
        pred = pred.clamp(eps, 1.0 - eps)

        pos_loss = (torch.log(pred) * torch.pow(1 -
                    pred, self.alpha) * pos_inds).sum()
        neg_loss = (torch.log(1 - pred) * torch.pow(pred, self.alpha) *
                    torch.pow(1 - target, self.beta) * neg_inds).sum()

        num_pos = pos_inds.sum()

        if num_pos == 0:
            loss = -neg_loss
        else:
            loss = -(pos_loss + neg_loss) / num_pos

        return loss
