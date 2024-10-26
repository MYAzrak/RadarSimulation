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
| nships                              | Number of ships in a scenario (Default: randomly chosen)   |
| nLocations                          | Number of locations a ship visits during scenario          |
| minStartingCoords/maxStartingCoords | Min/Max starting coordinates for a ship to visit           |
| randomCoords                        | Width of coordinate generation space                       |
| minSpeed/maxSpeed                   | Min/Max speeds for ships to move at                        |
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
