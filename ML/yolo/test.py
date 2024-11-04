import numpy as np
import os
import random
import json
import matplotlib.pyplot as plt
from PIL import Image
from ultralytics import YOLO
from inference import run_model

def load_random_json(directory):

    directory = os.path.expanduser(directory)
    
    # List all JSON files in the specified directory
    json_files = [f for f in os.listdir(directory) if f.endswith('.json')]

    # Select a random JSON file
    random_file = random.choice(json_files)
    
    # Construct the full file path
    file_path = os.path.join(directory, random_file)
    
    # Load and return the JSON data
    with open(file_path, 'r') as file:
        data = json.load(file)
    
    ppi_array = np.array(data['PPI'], dtype=np.float32)
    
    return data

model = YOLO("./best.pt")

test = load_random_json('~/Downloads/output')
test = np.array(test['PPI'], dtype=np.float32)

ships = run_model(test, model)
print(ships)
