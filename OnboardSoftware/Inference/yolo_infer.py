import numpy as np
from PIL import Image
import cv2

def run_model(ppi_array, model):

    # Clip the values
    mean = np.mean(ppi_array)
    std = np.std(ppi_array)
    ppi_array = np.clip(ppi_array, 0, min(5000, mean + (2/3) * std))
    
    # Create a normalized image from the array
    ppi_array_normalized = (ppi_array - ppi_array.min()) / (ppi_array.max() - ppi_array.min() + 1e-8)
    image = Image.fromarray((ppi_array_normalized * 255).astype(np.uint8))

    output = model.predict(image, imgsz=640, conf=0.3)
    
    boxes = output[0].boxes
    # Instead of output[0].show(), create custom visualization
    img_array = np.array(image)
    for box in boxes.xyxy:  # Use xyxy format for drawing rectangles
        x1, y1, x2, y2 = map(int, box[:4].tolist())
        cv2.rectangle(img_array, (x1, y1), (x2, y2), (255, 0, 0), 2)  # Draw rectangle without labels
    
    # Display the image in a larger window
    cv2.namedWindow('Detection Result', cv2.WINDOW_NORMAL)
    cv2.imshow('Detection Result', img_array)
    cv2.waitKey(1)  # Update the window
    # output[0].show()

    # 2D array of [distance, azimuth]
    xy_coordinates = boxes.xywh[:, :2].detach().cpu().numpy()
    print(xy_coordinates.tolist())

    return xy_coordinates

