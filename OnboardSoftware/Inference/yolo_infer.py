from PIL import Image

def run_model(ppi_array, model):
    
    # Create an image from the array
    image = Image.fromarray(ppi_array)
    output = model.predict(image, conf=0.7)
    
    boxes = output[0].boxes
    #output[0].show()

    # 2D array of [distance, azimuth]
    xy_coordinates = boxes.xywh[:, :2].detach().cpu().numpy()
    print(xy_coordinates.tolist())

    return xy_coordinates

