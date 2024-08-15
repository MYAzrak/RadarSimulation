using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario and Ship Prefabs")]
    public string scenarioFileName;
    [SerializeField] List<ShipPrefab> shipPrefabs = new();

    [Header("Scenario Options")]
    public int timeScale = 1;
    public bool updateTimeScale = false;
    public bool loadScenario = false;
    public bool resetScenario = false;
    public bool reloadCSV = false;
    public bool logMessages = false;
    
    string filePath = Application.dataPath + "/Scenarios/";
    string filePattern = @"^Scenario\d+\.csv$";                         // ScenarioX.csv where X is any number
    Dictionary<int, ShipInformation> shipsInformation = new();          // <Ship id, list of ship info>
    Dictionary<int, List<ShipCoordinates>> shipLocations = new();       // <Ship id, list of ship coordinates>
    List<GameObject> ships = new();                                     // Keep track of generated ships
    bool result;                                                        // The result of ReadScenarioCSV
    bool previousLogMessageBool = false;                                // Allows the log messages to be enabled or disabled using the same if statement

    CSVManager csvManager;

    public const float METERS_PER_SECOND_TO_KNOTS = 1.943844f;          // 1 Meter/second = 1.943844 Knot
    public const float KNOTS_TO_METERS_PER_SECOND = 0.5144444f;         // 1 Knot = 0.5144444 Meter/second
    
    void Start()
    {
        csvManager = GetComponent<CSVManager>();
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (resetScenario) 
        {
            ResetScenario();
            resetScenario = false;
        }
        else if (reloadCSV)
        {
            result = csvManager.ReadScenarioCSV(ref shipsInformation, ref shipLocations, scenarioFileName);
            reloadCSV = false;
        }
        else if (logMessages != previousLogMessageBool)
        {
            foreach (var ship in ships) 
            {
                ship.GetComponent<ShipController>().logMessages = logMessages;
            }

            previousLogMessageBool = logMessages;
        }
        else if (loadScenario)
        {
            result = false;
            ResetScenario();
            loadScenario = false;
        }
        else if (updateTimeScale)
        {
            Time.timeScale = timeScale;
            updateTimeScale = false;
        }
    }

    // Read all files in filePath and store the scenarios that match filePattern for the Unity inspector 
    public List<string> ReadScenarioFiles(out int numberOfNextScenario)
    {
        List<string> files = new();
        Regex myRegExp = new(filePattern);

        numberOfNextScenario = -1;

        DirectoryInfo info = new(filePath);
        FileInfo[] fileInfo = info.GetFiles()
                                    .Where(file => myRegExp.Match(file.Name).Success)
                                    .OrderBy(file => int.Parse(Path.GetFileNameWithoutExtension(file.Name[8..])))
                                    .ToArray();
        for (int i = 0; i < fileInfo.Length; i++)
        {
            var file = fileInfo[i];
            files.Add(Path.GetFileNameWithoutExtension(file.Name[..^4]));

            if (i == fileInfo.Length - 1)
                numberOfNextScenario = int.Parse(Path.GetFileNameWithoutExtension(file.Name[8..])); // "Scenario" has a length of 8
        }
        numberOfNextScenario += 1;

        Debug.Log("Scenario files have been read.");

        return files;
    }

    void ResetScenario()
    {
        // Generate ships if the csv was valid else read the csv again
        if (!result) {
            result = csvManager.ReadScenarioCSV(ref shipsInformation, ref shipLocations, scenarioFileName);
            if (!result) return;
        }

        // Destroy all generated ships
        foreach (var ship in ships) 
        {
            Destroy(ship);
        }

        ships.Clear();
        
        GenerateShips();
        Debug.Log("Scenario has been reset.");

        logMessages = previousLogMessageBool = false;
    }

    void GenerateShips()
    {
        foreach (var ship in shipLocations)
        {
            // The first location is the starting position of the ship
            float x = ship.Value[0].x_coordinates;
            float z = ship.Value[0].z_coordinates;
            Vector3 shipLocation = new(x, 0, z);
            
            GameObject instance;
            GameObject prefab = null;

            foreach (ShipPrefab shipPrefab in shipPrefabs)
            {
                if (shipPrefab.shipType == shipsInformation[ship.Key].Type)
                {
                    prefab = shipPrefab.prefab;
                }
            }

            if (prefab == null)
            {
                if (shipPrefabs.Count == 0){
                    Debug.Log($"No prefabs found. Skipping ship with ID {shipsInformation[ship.Key].Id}");
                    break;
                }

                prefab = shipPrefabs[0].prefab;

                if (prefab == null)
                {
                    Debug.Log($"No prefabs found at the first index of Ship Prefabs. Skipping ship with ID {shipsInformation[ship.Key].Id}");
                    break;
                }

                Debug.Log($"Unable to find ship prefab for ship type {shipsInformation[ship.Key].Type}. " + 
                $"Defaulting to the first ship prefab for ship with ID {shipsInformation[ship.Key].Id}");
            }

            // If there are more than one location then rotate the generated ship to face the direction of the next location
            if (ship.Value.Count > 1) 
            {
                Vector3 heading = new Vector3(ship.Value[1].x_coordinates, 0, ship.Value[1].z_coordinates) - shipLocation;
                float distance = heading.magnitude;
                Vector3 direction = heading / distance;
                
                instance = Instantiate(prefab, shipLocation, Quaternion.LookRotation(direction));
            }
            else
                instance = Instantiate(prefab, shipLocation, Quaternion.identity);

            ShipController shipController = instance.GetComponent<ShipController>() ?? instance.GetComponentInChildren<ShipController>();
            if (shipController == null)
            {
                Debug.Log($"Unable to find ship controller component. Ship with ID: {shipsInformation[ship.Key].Id} is uninitialized.");
                continue;
            }
            shipController.shipInformation = shipsInformation[ship.Key]; // Initialize the ship information

            ships.Add(instance);
            
            // Start from index 1 since index 0 is the starting position
            for (int i = 1; i < ship.Value.Count; i++)
            {
                x = ship.Value[i].x_coordinates;
                z = ship.Value[i].z_coordinates;
                float speed = ship.Value[i].speed;
                
                // Add the location and speed to ship controller lists
                shipController.locationsToVisit.Add(new Vector3(x, 0, z));
                shipController.speedAtEachLocation.Add(speed);
            }
        }
    }

    [System.Serializable]
    public struct ShipPrefab
    {
        public ShipType shipType;
        public GameObject prefab;

        public ShipPrefab(ShipType shipType, GameObject prefab)
        {
            this.shipType = shipType;
            this.prefab = prefab;
        }
    }
}

