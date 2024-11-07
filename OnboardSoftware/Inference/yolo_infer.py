import numpy as np
from PIL import Image

def run_model(ppi_array, model):

    # Clip the values
    mean = np.mean(ppi_array)
    std = np.std(ppi_array)
    ppi_array = np.clip(ppi_array, 0, min(5000, mean + (2/3) * std))
    
    # Create a normalized image from the array
    ppi_array_normalized = (ppi_array - ppi_array.min()) / (ppi_array.max() - ppi_array.min() + 1e-8)
    image = Image.fromarray((ppi_array_normalized * 255).astype(np.uint8))

    output = model.predict(image, conf=0.7)
    
    boxes = output[0].boxes
    # output[0].show()

    # 2D array of [distance, azimuth]
    xy_coordinates = boxes.xywh[:, :2].detach().cpu().numpy()
    print(xy_coordinates.tolist())

    return xy_coordinates

