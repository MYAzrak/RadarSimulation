# RadarSimulation

CMP Senior Project - Simulation of maritime radar detection

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

| Parameter                           | Description                                                |
| ----------------------------------- | ---------------------------------------------------------- |
| sceneName                           | Scene to start simulation in ("OceanMain" or "KhorfakkanCoastline") |
| nships                              | Number of ships in a scenario defined by a range [minAmount, maxAmount] |
| nLocations                          | Number of locations a ship visits during a scenario defined by a range [minAmount, maxAmount]) |
| coordinateSquareWidth               | Width of ship generation space                             |
| speed                               | Ship movement speed (in knots) defined by a range [minSpeed, maxSpeed]|
| radarRows                           | Number of rows in radar lattice network                    |
| radarPower                          | Power transmitted in W                                     |
| radarGain                           | Gain of the radar in dB                                    |
| waveLength                          | Wavelength of radar in m                                   |
| radarImageRadius                    | Width of the output data array (pixels)                    |
| verticalAngle                       | Vertical angle of radar beam                               |
| beamWidth                           | Horizontal angle of radar beam                             |
| rainRCS                             | RCS value for a rain drop                                  |
| nRadars                             | Number of radars in a scenario                             |
| nScenarios                          | Number of scenarios to generate                            |
| scenarioTimeLimit                   | Time limit for a scenario before ending and moving to next |
| weather                             | List of Weather conditions to cycle through for each scenario      |
| waves                               | List of Wave conditions to cycle through for each scenario |
| proceduralLand                      | List of bool to cycle through (whether procedural land is generated or not) |
| generateDataset                     | Flag to generate a dataset                                 |
| unityBuildDirectory                 | Directory for Unity build                                  |
| outputDirectory                     | Directory for output files                                 |
