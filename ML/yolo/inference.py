import numpy as np
from PIL import Image
from ultralytics import YOLO

def run_model(ppi_array, model):
    
    # Create an image from the array
    image = Image.fromarray(ppi_array)
    output = model.predict(image, conf=0.7)
    
    boxes = output[0].boxes
    #output[0].show()

    # 2D array of [distance, azimuth]
    xy_coordinates = boxes.xywh[:, :2].detach().cpu().numpy()

    return xy_coordinates
