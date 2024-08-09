using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CSVManager : MonoBehaviour
{
    [Header("Random CSV Generator")]
    [SerializeField] string fileName = "Scenario";
    [SerializeField] bool generateRandomCSV = false;

    [Header("Random CSV Parameters")]
    [SerializeField] int numberOfShips = 5;               // Number of ships to generate
    [SerializeField] int locationsToCreate = 10;          // Number of locations the ship will visit
    [SerializeField] float distanceBetweenPoints = 400f;  // Ensures the distance between the points is at least a bit apart
    [SerializeField] float initialCoordinates = 2000f;    // The range the ships will initially generate at
    [SerializeField] float randomCoordinates = 400f;      // The range added to the previous location the ship will visit
    [SerializeField] int minSpeed = 6;                    // The min value in the speed range
    [SerializeField] int maxSpeed = 11;                   // The max value in the speed range
    [SerializeField] string[] typesOfShips = { "Fishing boat", "Cargo", "Tanker" };

    string filePath = Application.dataPath + "/Scenarios/";
    string fileExtension = ".csv";
    string shipListEndName = "ShipList";                  // The ship list csv ends with ShipList.csv  
    int[] speed;
    
    void Update()
    {
        if (generateRandomCSV)
        {
            // .csv file extension is added in the function
            GenerateCSV(numberOfShips, filePath + fileName);
            generateRandomCSV = false;
        }
    }
    
    public Vector3[] GeneratePath()
    {
        Vector3[] points = new Vector3[locationsToCreate];

        speed = new int[locationsToCreate];
        speed[0] = Random.Range(minSpeed, maxSpeed);

        float x = Random.Range(-initialCoordinates, initialCoordinates);
        float z = Random.Range(-initialCoordinates, initialCoordinates);
        points[0] = new Vector3(x, 0, z);

        for (int i = 1; i < locationsToCreate; i++)
        {
            x = points[i - 1].x + Random.Range(-randomCoordinates, randomCoordinates) + distanceBetweenPoints;
            z = points[i - 1].z + Random.Range(-randomCoordinates, randomCoordinates) + distanceBetweenPoints;
            points[i] = new Vector3(x, 0, z);
            
            speed[i] = Random.Range(minSpeed, maxSpeed);
        }

        return points;
    }

    public void GenerateCSV(int numberOfShips, string file)
    {
        if (File.Exists(file + fileExtension) || File.Exists(file + fileExtension + shipListEndName)) {
            Debug.Log($"{file + fileExtension} or {file + fileExtension + shipListEndName} already exists.");
            return;
        }

        using TextWriter textWriter = new StreamWriter(file + fileExtension, true);
        using TextWriter shipListWriter = new StreamWriter(file + fileExtension + shipListEndName, true);

        textWriter.WriteLine("ID, X Coordinate, Z Coordinate, Speed");
        shipListWriter.WriteLine("ID, Name, Type");

        for (int i = 0; i < numberOfShips; i++)
        {
            Vector3[] locations = GeneratePath();

            for (int x = 0; x < locations.Length; x++)
            {
                textWriter.WriteLine($"{i + 1}, {locations[x].x}, {locations[x].z}, {speed[x]}");
            }

            shipListWriter.WriteLine($"{i + 1}, TestShip{i + 1}, {typesOfShips[Random.Range(0, typesOfShips.Length)]}");
        }

        Debug.Log("csv has been generated.");
    }

    public bool ReadScenarioCSV(
        ref Dictionary<int, ShipInformation> shipsInformation, 
        ref Dictionary<int, List<ShipCoordinates>> shipLocations, 
        string scenarioFileName)
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

                // Keep track of ship IDs in case there are duplicate IDs in the csv
                if (shipsInformation.ContainsKey(id))
                {
                    Debug.Log("Error: Ship list csv contains duplicate ID");
                    shipsInformation.Clear();
                    return false;
                }
                else
                {
                    shipsInformation[id] = new ShipInformation(id, value[1], value[2]);
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

                ShipCoordinates shipCoordinates = new(float.Parse(value[1]), float.Parse(value[2]), float.Parse(value[3]));
                
                // Save all locations for each ship in a dictionary
                if (shipLocations.ContainsKey(id))
                    shipLocations[id].Add(shipCoordinates);
                else
                    shipLocations[id] = new List<ShipCoordinates>() { shipCoordinates };

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