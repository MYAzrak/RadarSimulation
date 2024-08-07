using System.IO;
using UnityEngine;

public class CSVGenerator : MonoBehaviour
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
    string shipListEndName = "ShipList";                 // The ship list csv ends with ShipList.csv  
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
}