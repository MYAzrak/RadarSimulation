# Maritime Vessel Detection Using a Network of Marine Radars via a Simulation in Unity

This is our senior project where we explore the development of a maritime vessel detection system using a network of marine radars simulated through Unity. The objective is to enhance maritime surveillance capabilities in the UAE by addressing limitations in traditional radar systems such as range and coverage constraints and adding an extra data layer for maritime surveillance. By integrating deep learning with a network of buoy-mounted radars, this project aims to provide un-manned, real-time maritime situational awareness. We propose a cost-effective, scalable solution that utilizes a simulated environment to train and test radars and marine vessels detection, which is a proof of concept that, if implemented in real-life, would improve national security and economic stability in maritime domains. Our trained model, CenterNet, achieved an F1-score of 0.938. Additionally, our network of radars sends the detected ships' locations to the database, which are then retrieved for visualization. For detailed technical information, refer to the accompanying `Project's Report.pdf`.

## Table of Contents

1. [Introduction](#maritime-vessel-detection-using-a-network-of-marine-radars-via-a-simulation)
2. [Project's Subsystems](#projects-subsystems)
3. [Installation](#installation)
4. [Usage](#usage)
   - [Generate a Dataset](#1-generate-a-dataset)
        - [Simulation Configuration Parameters](#simulation-configuration-parameters)
   - [Train the DL Model](#2-train-the-dl-model)
   - [Run the Entire System](#3-run-the-entire-system)
5. [Project Structure](#project-structure)
6. [Configuration Files](#configuration-files)
7. [Future Work](#future-work)
8. [Senior Project Team](#senior-project-team)

## Project's Subsystems

This block diagram shows the various subsystems of our project. The user begins the simulation by inputting the simulation settings, radar settings, simulation scenario, and radar locations. The simulation system creates the needed radar and vessels according to the settings and scenario provided and stores the radar locations in the database. The simulated radar will detect the simulated vessels and export its output as a PPI image, which is communicated to the onboard software and stored in the database. The radar onboard software detects the vessels in the PPI images, converts the vessel locations from the relative position of the radar to the absolute position on a map and stores the vessel locations in the database. These vessel locations are then sent to the visualization platform for plotting onto a map, where the user can input specific map parameters such as scale and orientation.

![BlockDiagram](https://github.com/user-attachments/assets/194fa881-f83c-4250-ad58-da6448344240)

## Installation

### System Requirements

- **Unity**: Version 2022.3.40f1  
- **Docker**  
- **Python**
- **Conda**

### Steps to Install

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/yal77/RadarSimulation.git
   ```

2. **Navigate to the Cloned Directory**:

   ```bash
   cd RadarSimulation
   ```

3. **Create and Activate Conda Environment**:

   ```bash
   conda create --name deep_learning python
   conda activate deep_learning
   ```

4. **Install Dependencies**:

   ```bash
   pip install -r requirements.txt
   ```

5. **Set Up Unity**:

   - Add project from disk.
   - Choose `RadarSimulation -> RadarProject`.
   - Use version `2022.3.40f1`.

6. **Set Up Visualization Platform**:

   ```bash
   cd Visualization
   npm install
   ```

## Usage

This project can be used to:

- Generate a dataset of radar PPI images.
- Train deep learning models for vessel detection.
- Run the entire system (Simulation System, Onboard Software, and Visualization Platform) to simulate the Khorfakkan scene, predict vessel locations, and visualize them in the web application.
Follow the steps below to generate your own dataset or use the pre-generated dataset available in this [repository](https://github.com/yal77/radar_dataset).

### 1. Generate a Dataset

1. **Create a Configuration File**:
Create a YAML file specifying the desired simulation parameters (refer to the table below). You can use the example file `sim-config-example.yaml` as a template, but ensure that the `sceneName` is set to `OceanMain` when generating a dataset.

2. **Run the Dataset Generation Script**:

   ```bash
   python ML/datasetGen/generateDataset.py path/to/config.yaml path/to/unity/executable path/to/output/directory
   ```

- Replace `path/to/config.yaml` with the path to your configuration file.
- Replace `path/to/unity/executable` with the path to the Unity build executable of the project (you need to create this executable).
- Replace `path/to/output/directory` with the directory where the dataset will be saved.

#### Simulation Configuration Parameters

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

### 2. Train the DL Model

We implemented two models that you can train, [CenterNet](https://arxiv.org/abs/1904.08189) and YOLO from [ultraytics](https://docs.ultralytics.com/).

1. Train CenterNet:
   - Change `json_directory` to your dataset's location in `ML/CenterNet/main.py` and run `main.py`.

2. Train YOLO:
   - Change the dataset path directory in `ppi_dataset.yaml`. This is the directory with the images the model will train on.
   - Modify `json_directory` and `save_directory` in `ML/yolo/train.py` (`save_directory` should match the dataset directory in `ppi_dataset.yaml`) and run `train.py`.
   - The training script will convert the JSON files into the format expected by YOLO, place them in the dataset directory, and train the model.

### 3. Run the Entire System

1. Change `sim-config-example.yaml` (or your version of it) to have use `KhorfakkanCoastline` scene.
2. Add your Conda path to `possible_paths` in `start_services.py`.
3. Ensure Docker is running.
4. Create a copy of `service_config-example.yaml` with your own paths.
5. Run `python run.py sim-config-example.yaml`.
6. Run `python start_services.py service_config-example.yaml`.

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
  - **`yolo_infer.py`**: Inference script for YOLO.

### **RadarProject/**

Unity project for radar simulation.

- **`Assets/`**:
  - **`Materials/`**: Contains material configurations for land and ocean.
  - **`Models/`**: Includes 3D models for ships, buoys, and other objects.
  - **`Oceans/`**: Ocean generation and environmental settings.
  - **`Scenes/`**: Unity scenes for simulation:
    - **`KhorfakkanCoastline.unity`**: Scene representing the Khorfakkan coastline used for testing.
    - **`OceanMain.unity`**: Randomized scene used for training.
  - **`Scripts/`**: Contains Unity C# scripts for simulation behavior.
    - **`Buoyancy/`**: Implements ship buoyancy physics by simulating interactions with water surfaces.
    - **`Camera/`**: Manages camera control and perspective for navigating the simulation environment.
    - **`Khorfakkan Coastline/`**: Generates and manages terrain data for the Khorfakkan Coastline simulation scene.
    - **`Procedural Land Generation/`**: Creates dynamic, procedurally generated terrain for simulation scenarios. Implemented following parts of [Sebastian Lague's tutorial](https://www.youtube.com/watch?v=wbpMiKiSKm8&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3).
    - **`Radar/`**: Handles core radar operations.
    - **`Scenario/`**: Manages simulation scenarios, including settings, configurations, and data import/export.
    - **`Shaders/`**: Defines GPU-accelerated operations for radar data processing and visualization.
    - **`Ship Movement/`**: Controls ship movements and manages their positions within the simulation.
    - **`Weather & Waves/`**: Simulates environmental conditions such as weather and ocean wave dynamics.

### **Visualization/**

Web-based visualization platform for plotting detected vessels on a map.

- **`DB_API/`**: Backend API for database interactions.
- **`src/`**: Frontend source code for the visualization interface.

## Configuration Files

Here is a brief overview of each configuration file role:

1. `ppi_dataset.yaml`: Defines the dataset structure for training the YOLO model, specifying the paths to training and validation data along with class labels (e.g., ship). It ensures the model correctly locates and processes the data during training.
2. `sim-config-example.yaml`: A template for defining the simulation configuration parameters mentioned in the earlier table allowing users to customize scenarios for generating radar PPI datasets or testing.
3. `service_config-example.yaml`: Defines the paths, environment, and settings required to initialize and manage the various components of the entire system, including the database, API, onboard software, and visualization platform.

## Future Work

- Enhance system security with encrypted data transmission and storage.  
- Address the limitation of reflectivity by integrating material-specific radar reflections instead of treating all materials uniformly.  
- Expand the simulation system to include more diverse weather scenarios.  
- Focus on the following aspects for real-world deployment:
  - Hardware integration.  
  - Power solutions.  
  - Reducing radar interference.  
  - Ensuring long-term system durability.  

## Senior Project Team

This project was done by [arcarum](https://github.com/arcarum), [Yousif Alhosani](https://github.com/yal77), [Mohammad Yaser Azrak](https://github.com/MYAzrak) and [Ibrahim Baig](https://github.com/darkwing-30).
