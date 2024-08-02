using System.IO;
using UnityEngine;

public class CSVGenerator
{
    public int locationsToCreate = 10; // Number of locations the ship will visit
    public float distanceBetweenPoints = 400f;
    public float initialCoordinates = 2000f; // The range the ships will initially generate at
    public float randomCoordinates = 400f; // The range added to the previous location the ship will visit

    int[] speed;
    public int minSpeed = 6;
    public int maxSpeed = 11;
    public string[] typesOfShips = { "Fishing boat", "Cargo", "Tanker" };
    
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
        if (File.Exists(file + ".csv") || File.Exists(file + "ShipList.csv")) {
            Debug.Log("csv file or shiplist.csv already exists.");
            return;
        }

        using TextWriter textWriter = new StreamWriter(file + ".csv", true);
        using TextWriter shipListWriter = new StreamWriter(file + "ShipList.csv", true);

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
    }
}