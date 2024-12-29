# Maritime Vessel Detection Using a Network of Marine Radars via a Simulation

This is our senior project where we explore the development of a maritime vessel detection system using a network of marine radars simulated through Unity. The objective is to enhance maritime surveillance capabilities in the UAE by addressing limitations in traditional radar systems such as range and coverage constraints and adding an extra data layer for maritime surveillance. By integrating deep learning with a network of buoy-mounted radars, this project aims to provide un-manned, real-time maritime situational awareness. We propose a cost-effective, scalable solution that utilizes a simulated environment to train and test radars and marine vessels detection, which is a proof of concept that, if implemented in real-life, would improve national security and economic stability in maritime domains. Our trained model achieved an F1-score of 0.938. Additionally, our network of radars sends the detected ships' locations to the database, which are then retrieved for visualization.

## Table of Contents

## Project's Subsystems

This block diagram shows the various subsystems of our project. The user begins the simulation by inputting the simulation settings, radar settings, simulation scenario, and radar locations. The simulation system creates the needed radar and vessels according to the settings and scenario provided and stores the radar locations in the database. The simulated radar will detect the simulated vessels and export its output as a PPI image, which is communicated to the onboard software and stored in the database. The radar onboard software detects the vessels in the PPI images, converts the vessel locations from the relative position of the radar to the absolute position on a map and stores the vessel locations in the database. These vessel locations are then sent to the visualization platform for plotting onto a map, where the user can input specific map parameters such as scale and orientation.

![BlockDiagram](https://github.com/user-attachments/assets/20220313-1b22-448a-b863-1957eff8b442)



## Installation

## Usage

## Project Structure

## Configuration Files

Here is a brief overview of each configuration file role:


## Collaborators

This project was done by [arcarum](https://github.com/arcarum), [Yousif Alhosani](https://github.com/yal77), [Mohammad Yaser Azrak](https://github.com/MYAzrak) and [Ibrahim Baig](https://github.com/darkwing-30).

## Clone Repo

1. Add project from disk.
2. Choose `RadarSimulation -> RadarProject`.
3. Use version `2022.3.40f1`.

## Generate Dataset

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

## Run the Test Scene

1. Change the config.yaml to have use the test scene
2. Run `python run.py ./path/to/config`
3. Create a copy of service_config.yaml with your own paths
4. Run `python start_services.py service_config.yaml`
