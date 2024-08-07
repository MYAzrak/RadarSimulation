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
    
    Dictionary<int, string[]> shipsInformation = new();                 // <Ship id, array of ship info>
    Dictionary<int, List<(float, float, float)>> shipLocations = new(); // <Ship id, (x coordinates, z coordinates, speed)>
    List<GameObject> ships = new();                                     // Keep track of generated ships
    string filePath = Application.dataPath + "/Scenarios/";
    bool result;                                                        // The result of ReadScenarioCSV
    string filePattern = @"^Scenario\d+\.csv$";                         // ScenarioX.csv where X is any number
    
    void Start()
    {
        result = ReadScenarioCSV();
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
            result = ReadScenarioCSV();
            reloadCSV = false;
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
            result = ReadScenarioCSV();
            if (!result) return;
        }

        // Destroy all generated ships
        foreach (var ship in ships) 
        {
            Destroy(ship);
        }
        
        GenerateShips();
        Debug.Log("Scenario has been reset.");
    }

    void GenerateShips()
    {
        foreach (var ship in shipLocations)
        {
            // The first location is the starting position of the ship
            float x = ship.Value[0].Item1;
            float z = ship.Value[0].Item2;
            Vector3 shipLocation = new(x, 0, z);
            
            // Get ship information
            string[] information = shipsInformation[ship.Key];

            int id = ship.Key;
            string name = information[0];
            string type = information[1];

            // TODO: Instantiate the prefab based on the type of the ship
            GameObject instance;

            // If there are more than one location then rotate the generated ship to face the direction of the next location
            if (ship.Value.Count > 1) 
            {
                Vector3 heading = new Vector3(ship.Value[1].Item1, 0, ship.Value[1].Item2) - shipLocation;
                float distance = heading.magnitude;
                Vector3 direction = heading / distance;
                
                instance = Instantiate(shipPrefab, shipLocation, Quaternion.LookRotation(direction));
            }
            else
                instance = Instantiate(shipPrefab, shipLocation, Quaternion.identity);

            ShipController shipController = instance.GetComponent<ShipController>();
            shipController.shipInformation = new ShipInformation(id, name, type); // Initialize the ship information

            ships.Add(instance);
            
            // Start from index 1 since index 0 is the starting position
            for (int i = 1; i < ship.Value.Count; i++)
            {
                x = ship.Value[i].Item1;
                z = ship.Value[i].Item2;
                float speed = ship.Value[i].Item3;
                
                // Add the location and speed to ship controller lists
                shipController.locationsToVisit.Add(new Vector3(x, 0, z));
                shipController.speedAtEachLocation.Add(speed);
            }
        }
    }

    bool ReadScenarioCSV()
    {
        shipsInformation.Clear();
        shipLocations.Clear();

        try
        {
            // Read ship list information
            StreamReader streamReader = new(filePath + scenarioFileName + "ShipList.csv");

            _ = streamReader.ReadLine(); // Ignore the first line which is the headings
            string data = streamReader.ReadLine();
            while (data != null)
            {
                string[] value = data.Split(',');

                // Ensure all rows do not have empty or null cells
                if (value.Any(s => string.IsNullOrEmpty(s)))
                {
                    Debug.Log("Error: Invalid number of columns");
                    shipsInformation.Clear();
                    return false;
                }
                
                int id = int.Parse(value[0]);

                // In the dictionary, the key is the id and array from index 1 (without the id) is the value
                string[] array = { value[1], value[2]};

                // Keep track of ship IDs in case there are duplicate IDs in the csv
                if (shipsInformation.ContainsKey(id))
                {
                    Debug.Log("Error: Ship list csv contains duplicate ID");
                    shipsInformation.Clear();
                    return false;
                }
                else
                {
                    shipsInformation[id] = array;
                }

                data = streamReader.ReadLine();
            }

            // Read each ship locations and speed
            streamReader = new(filePath + scenarioFileName + ".csv");
            _ = streamReader.ReadLine(); // Ignore the first line which is the headings
            data = streamReader.ReadLine();
            while (data != null)
            {
                string[] value = data.Split(',');

                // Ensure all rows do not have empty or null cells
                if (value.Any(s => string.IsNullOrEmpty(s)))
                {
                    Debug.Log("Error: Invalid number of columns");
                    shipLocations.Clear();
                    return false;
                }
                
                int id = int.Parse(value[0]);

                // x, z, speed
                (float, float, float) list = (float.Parse(value[1]), float.Parse(value[2]), float.Parse(value[3]));
                
                // Save all locations for each ship in a dictionary
                if (shipLocations.ContainsKey(id))
                    shipLocations[id].Add(list);
                else
                    shipLocations[id] = new List<(float, float, float)> { list };

                data = streamReader.ReadLine();
            }

            Debug.Log("csv has been successfully parsed.");
            return true;
        }
        catch (FileNotFoundException e)
        {
            Debug.Log($"File not found: {e.Message}");
            return false;
        }
    }
}
