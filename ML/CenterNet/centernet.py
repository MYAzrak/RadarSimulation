# centernet.py
import torch
import torch.nn as nn
import torch.nn.functional as F
import numpy as np


def detect_points(heatmap, threshold=0.3, nms_kernel_size=3):
    """
    Extract points from a heatmap using non-maximum suppression
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

        points = [(x.item(), y.item()) for x, y in zip(xs, ys)]

        return points


class CenterNetBackbone(nn.Module):
    def __init__(self, in_channels=1):  # Default 1 channel for grayscale radar images
        super().__init__()
        # Downsampling layers
        self.conv1 = nn.Conv2d(
            in_channels, 64, kernel_size=7, stride=2, padding=3)
        self.bn1 = nn.BatchNorm2d(64)
        self.conv2 = nn.Conv2d(64, 128, kernel_size=3, stride=2, padding=1)
        self.bn2 = nn.BatchNorm2d(128)
        self.conv3 = nn.Conv2d(128, 256, kernel_size=3, stride=2, padding=1)
        self.bn3 = nn.BatchNorm2d(256)

        # Upsampling layers
        self.deconv1 = nn.ConvTranspose2d(
            256, 128, kernel_size=4, stride=2, padding=1)
        self.deconv2 = nn.ConvTranspose2d(
            128, 64, kernel_size=4, stride=2, padding=1)

        # Final prediction layer
        # 1 channel output for heatmap
        self.head = nn.Conv2d(64, 1, kernel_size=1)

    def forward(self, x):
        # Downsampling path
        x = F.relu(self.bn1(self.conv1(x)))
        x = F.relu(self.bn2(self.conv2(x)))
        x = F.relu(self.bn3(self.conv3(x)))

        # Upsampling path
        x = F.relu(self.deconv1(x))
        x = F.relu(self.deconv2(x))

        # Prediction head
        heatmap = torch.sigmoid(self.head(x))
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
