import numpy as np
import os
import random
import json
import matplotlib.pyplot as plt
from PIL import Image
from ultralytics import YOLO

def run_model(ppi_array, model):
    
    # Create an image from the array
    image = Image.fromarray(ppi_array)
    output = model.predict(image, conf=0.7)
    
    boxes = output[0].boxes
    image = np.zeros((720, 1000), dtype=np.uint8)

    xy = boxes.xywh[:, :2].detach().cpu().numpy()

    # Make the "dot" bigger
    dot_size = 7

    # Mark the points on the image array
    for point in xy:
        x, y = point
        x, y = int(x), int(y)
        # Ensure the square fits within the bounds
        for i in range(max(0, y - dot_size // 2), min(720, y + dot_size // 2 + 1)):
            for j in range(max(0, x - dot_size // 2), min(1000, x + dot_size // 2 + 1)):
                image[i, j] = 255
    
    # output[0].show()

    return image