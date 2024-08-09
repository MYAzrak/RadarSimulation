using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ShipManager : MonoBehaviour
{
    [Header("Scenario and Ship Prefabs")]
    [SerializeField] string scenarioFileName = "Scenario1";
    // [SerializeField] bool readScenarioFiles = false;                    // Rereads the files stored in filePath
    [SerializeField] GameObject shipPrefab; // TODO: Create prefabs for each of the ships

    [Header("Scenario Options")]
    [SerializeField] bool resetScenario = false;
    [SerializeField] bool reloadCSV = false;
    [SerializeField] bool logMessages = false;                          // Enables/Disables log messages of all ships (Results in a worse performance) 
    
    string filePath = Application.dataPath + "/Scenarios/";
    string filePattern = @"^Scenario\d+\.csv$";                         // ScenarioX.csv where X is any number
    Dictionary<int, ShipInformation> shipsInformation = new();    // <Ship id, list of ship info>
    Dictionary<int, List<ShipCoordinates>> shipLocations = new();       // <Ship id, list of ship coordinates>
    List<GameObject> ships = new();                                     // Keep track of generated ships
    bool result;                                                        // The result of ReadScenarioCSV

    bool previousLogMessageBool = false;                                // Allows the log messages to be enabled or disabled using the same if statement

    CSVManager csvManager;
    
    void Start()
    {
        csvManager = GetComponent<CSVManager>();
        result = csvManager.ReadScenarioCSV(ref shipsInformation, ref shipLocations, scenarioFileName);
        if (!result) return;
        GenerateShips();
    }

    void FixedUpdate()
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
    }

    // Read all files in filePath and store the scenarios that match filePattern for the Unity inspector 
    /*
    void ReadScenarioFiles()
    {
        Regex myRegExp = new(filePattern);

        var info = new DirectoryInfo(filePath);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo) 
        {
            if (myRegExp.Match(file.Name).Success)
            {
                // scenarios.Add(file.Name[..^4]); // remove .csv
            }
        }

        Debug.Log("Scenario files have been read.")
    }
    */

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
            

            // TODO: Instantiate the prefab based on the type of the ship
            GameObject instance;

            // If there are more than one location then rotate the generated ship to face the direction of the next location
            if (ship.Value.Count > 1) 
            {
                Vector3 heading = new Vector3(ship.Value[1].x_coordinates, 0, ship.Value[1].z_coordinates) - shipLocation;
                float distance = heading.magnitude;
                Vector3 direction = heading / distance;
                
                instance = Instantiate(shipPrefab, shipLocation, Quaternion.LookRotation(direction));
            }
            else
                instance = Instantiate(shipPrefab, shipLocation, Quaternion.identity);

            ShipController shipController = instance.GetComponent<ShipController>();
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
}

