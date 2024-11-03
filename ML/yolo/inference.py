import numpy as np
import os
import random
import json
from PIL import Image
from ultralytics import YOLO

def run_model_on_image(model, ppi_array):
    
    # Create an image from the array
    image = Image.fromarray(ppi_array)
    output = model.predict(image, save=True)
    
    return output

def load_random_json(directory):
    # List all JSON files in the specified directory
    json_files = [f for f in os.listdir(directory) if f.endswith('.json')]
    
    # Check if there are any JSON files in the directory
    if not json_files:
        raise FileNotFoundError("No JSON files found in the specified directory.")

    # Select a random JSON file
    random_file = random.choice(json_files)
    
    # Construct the full file path
    file_path = os.path.join(directory, random_file)
    
    # Load and return the JSON data
    with open(file_path, 'r') as file:
        data = json.load(file)
    
    ppi_array = np.array(data['PPI'], dtype=np.float32)
    
    return ppi_array

model = YOLO("/home/kkp/RadarSimulation/ML/yolo/best.pt")

result = run_model_on_image(model, load_random_json('/home/kkp/Downloads/output'))
