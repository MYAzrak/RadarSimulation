# Maritime Vessel Detection Using a Network of Marine Radars via a Simulation

This is our senior project where we explore the development of a maritime vessel detection system using a network of marine radars simulated through Unity. The objective is to enhance maritime surveillance capabilities in the UAE by addressing limitations in traditional radar systems such as range and coverage constraints and adding an extra data layer for maritime surveillance. By integrating deep learning with a network of buoy-mounted radars, this project aims to provide un-manned, real-time maritime situational awareness. We propose a cost-effective, scalable solution that utilizes a simulated environment to train and test radars and marine vessels detection, which is a proof of concept that, if implemented in real-life, would improve national security and economic stability in maritime domains. Our trained model, CenterNet, achieved an F1-score of 0.938. Additionally, our network of radars sends the detected ships' locations to the database, which are then retrieved for visualization.

## Table of Contents

## Project's Subsystems

This block diagram shows the various subsystems of our project. The user begins the simulation by inputting the simulation settings, radar settings, simulation scenario, and radar locations. The simulation system creates the needed radar and vessels according to the settings and scenario provided and stores the radar locations in the database. The simulated radar will detect the simulated vessels and export its output as a PPI image, which is communicated to the onboard software and stored in the database. The radar onboard software detects the vessels in the PPI images, converts the vessel locations from the relative position of the radar to the absolute position on a map and stores the vessel locations in the database. These vessel locations are then sent to the visualization platform for plotting onto a map, where the user can input specific map parameters such as scale and orientation.

![BlockDiagram](https://github.com/user-attachments/assets/20220313-1b22-448a-b863-1957eff8b442)

## Installation

System Requirements: Unity (version 2022.3.40f1), Docker, and Python.
1- Clone repo (git clone https://github.com/yal77/RadarSimulation.git)
2- Install dependacies: (pip install -r requirements.txt) 
3- In Unity:
a- Add project from disk.
b- Choose `RadarSimulation -> RadarProject`.
c-  Use version `2022.3.40f1`.

## Usage
The project can be used to generate a dataset of radar PPI images, train the DL model, or test the model on a test scence of a simulated Khorfakkan scene. You can generate your own dataset through the steps below, or get a dataset we generated that can be found in this [repository](https://github.com/yal77/radar_dataset).

### Generate Dataset

1. Create a config file in json format
2. Include the relevant parameters
3. Run `python ML/datasetGen/generateDataset.py path/to/config.json path/to/unity/executable path/to/output/directory`

Where the unity executable is the build executable of the project (you have to generate it on your own).
And output directory is the directory where you would like to store the dataset.

### Config Parameters

| Parameter                  | Description                                                                                    |
| -------------------------- | ---------------------------------------------------------------------------------------------- |
| sceneName                  | Scene to start simulation in ("OceanMain" or "KhorfakkanCoastline")                            |
| nships                     | Number of ships in a scenario defined by a range [minAmount, maxAmount]                        |
| nLocations                 | Number of locations a ship visits during a scenario defined by a range [minAmount, maxAmount]) |
| coordinateSquareWidth      | Width of ship generation space                                                                 |
| speed                      | Ship movement speed (in knots) defined by a range [minSpeed, maxSpeed]                         |
| radarRows                  | Number of rows in radar lattice network                                                        |
| radarPower                 | Power transmitted in W                                                                         |
| radarGain                  | Gain of the radar in dB                                                                        |
| waveLength                 | Wavelength of radar in m                                                                       |
| radarImageRadius           | Width of the output data array (pixels)                                                        |
| antennaVerticalBeamWidth   | Vertical angle of radar beam                                                                   |
| antennaHorizontalBeamWidth | Horizontal angle of radar beam                                                                 |
| rainRCS                    | RCS value for a rain drop                                                                      |
| nRadars                    | Number of radars in a scenario                                                                 |
| nScenarios                 | Number of scenarios to generate                                                                |
| scenarioTimeLimit          | Time limit for a scenario before ending and moving to next                                     |
| weather                    | List of Weather conditions to cycle through for each scenario                                  |
| waves                      | List of Wave conditions to cycle through for each scenario                                     |
| proceduralLand             | List of bool to cycle through (whether procedural land is generated or not)                    |
| generateDataset            | Flag to generate a dataset                                                                     |
| unityBuildDirectory        | Directory for Unity build                                                                      |
| outputDirectory            | Directory for output files                                                                     |

### Train the DL Model

### Run the Test Scene

1. Change the config.yaml to have use the test scene
2. Run `python run.py ./path/to/config`
3. Create a copy of service_config.yaml with your own paths
4. Run `python start_services.py service_config.yaml`

## Project Structure

Below is an overview of the key folders and their purposes:

### **ML/**
Contains machine learning models and scripts for dataset generation, training, and inference.
- **`CenterNet/`**: Implements the CenterNet model for vessel detection.
- **`datasetGen/`**: Script for generating datasets.
- **`yolo/`**: YOLO-based model implementation.

### **OnboardSoftware/**
Handles radar image processing and vessel detection onboard.
- **Key scripts**:
  - **`centernet-infer.py`**: Performs inference using the CenterNet model.
  - **`onboard-yolo.py`**: Handles onboard YOLO model operations.
  - **`radar.py`**: Core radar processing logic.
  - **`radarWebSocketVisualizer.py`**: Visualizes radar data via WebSockets.
  - **`yolo_infer.py`**: Inference script for YOLO.

### **RadarProject/**
Unity project for radar simulation.
- **`Assets/`**:
  - **`Materials/`**: Contains material configurations for land and ocean.
  - **`Models/`**: Includes 3D models for ships, buoys, and other objects.
  - **`Oceans/`**: Ocean generation and environmental settings.
  - **`Scenes/`**: Unity scenes for simulation:
    - **`KhorfakkanCoastline.unity`**: Scene representing the Khorfakkan coastline used for testing.
    - **`OceanMain.unity`**: Radnomized scene used for training.
  - **`Scripts/`**: Contains Unity C# scripts for simulation behavior.

### **Visualization/**
Web-based visualization platform for plotting detected vessels on a map.
- **`DB_API/`**: Backend API for database interactions.
- **`src/`**: Frontend source code for the visualization interface.

## Configuration Files

Here is a brief overview of each configuration file role:


## Collaborators

This project was done by [arcarum](https://github.com/arcarum), [Yousif Alhosani](https://github.com/yal77), [Mohammad Yaser Azrak](https://github.com/MYAzrak) and [Ibrahim Baig](https://github.com/darkwing-30).
