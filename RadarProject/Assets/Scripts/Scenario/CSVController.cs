using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CSVController : MonoBehaviour
{
    [Header("Random CSV Generator")]
    public string fileName;
    public bool generateRandomCSV = false;
    public bool generateRandomParameters = true;

    [Header("Master Controller Parameters")]
    public MinMax<int> numOfShips = new(30, 120);
    public MinMax<int> locationsToVisit = new(5, 9);
    public MinMax<int> speedAtLocations = new(12, 18);
    public LoopArray<Weather> weathers = new( new Weather[]{ Weather.Clear, Weather.LightRain, Weather.HeavyRain } );
    public LoopArray<Waves> waves = new( new Waves[] {Waves.Calm, Waves.Moderate} );
    public LoopArray<bool> generateProceduralLand = new( new bool[] { true, false, false } );

    [Header("Random Ship Parameters")]
    public int numberOfShips;                       // Number of ships to generate
    public int locationsToCreate;                   // Number of locations the ship will visit
    public float coordinateSquareWidth = 30000f;
    public Vector3 centerPoint = Vector3.zero;
    public float randomCoordinates;                 // The range added to the previous location the ship will visit

    [Header("Random Procedural land Parameters")]
    bool hasProceduralLand = false;
    public int proceduralLandSeed;
    public Vector3 proceduralLandLocation;
    RadarGenerationDirection direction;

    string filePath;
    const string fileExtension = ".csv";
    const string shipListEndName = "ShipList";            // The ship list csv ends with ShipList.csv
    const string scenarioSettingsEndName = "Settings.json";

    MainMenuController mainMenuController;

    void Awake()
    {
        filePath = Application.persistentDataPath + "/Scenarios/";
    }

    void Start()
    {
        mainMenuController = FindObjectOfType<MainMenuController>();
    }

    public void GenerateRandomParameters()
    {
        // Initialize ship parameters with random values
        numberOfShips = Random.Range(numOfShips.Min, numOfShips.Max);
        locationsToCreate = Random.Range(locationsToVisit.Min, locationsToVisit.Max);
        randomCoordinates = Random.Range(-1500, 1500);
        float halfWidth = coordinateSquareWidth / 2f;

        // Initialize procedural land parameters with random values
        hasProceduralLand = generateProceduralLand.GetCurrentElement();

        if (!hasProceduralLand)
            return;

        proceduralLandSeed = Random.Range(0, 10_000_000);

        // Create a point on the boundary of the ship spawn area
        Vector3 pointOutside = GetRandomPointOnBoundary(Vector3.zero, new Vector2(centerPoint.x + halfWidth, centerPoint.z + halfWidth), ref direction);

        if (direction == RadarGenerationDirection.Left)
            centerPoint.x -= halfWidth;
        else if (direction == RadarGenerationDirection.Right)
            centerPoint.x += halfWidth;

        proceduralLandLocation = pointOutside;
    }

    Vector3 GetRandomPointOnBoundary(Vector3 center, Vector2 size, ref RadarGenerationDirection direction)
    {
        // Calculate half width and half height
        float width = size.x;
        float Height = size.y;
        float yValue = 0f;

        // Randomly choose a side
        int side = Random.Range(0, 2); // 0 = right, 1 = left for land

        // Direction of radar is opposite to direction of land
        switch (side)
        {
            case 0:
                direction = RadarGenerationDirection.Left;
                break;
            case 1:
                direction = RadarGenerationDirection.Right;
                break;
            default:
                break;
        }

        return side switch
        {
            // Right
            0 => new Vector3(
                                center.x + width,
                                yValue,
                                center.z + Random.Range(-Height, Height)
                            ),
            // Left
            1 => new Vector3(
                                center.x - width,
                                yValue,
                                center.z + Random.Range(-Height, Height)
                            ),
            _ => Vector3.zero,
        };
    }

    void Update()
    {
        if (generateRandomCSV)
        {
            // .csv file extension is added in the function
            GenerateScenario(filePath + fileName);
            generateRandomCSV = false;
        }
    }

    public Vector3[] GeneratePath(out int[] speed)
    {
        Vector3[] points = new Vector3[locationsToCreate];

        speed = new int[locationsToCreate];
        speed[0] = Random.Range(speedAtLocations.Min, speedAtLocations.Max);
        
        float width = coordinateSquareWidth / 2;
        float x = centerPoint.x + Random.Range(-width, width);
        float z = centerPoint.z + Random.Range(-width, width);
        points[0] = new Vector3(x, 0, z);

        for (int i = 1; i < locationsToCreate; i++)
        {
            x = points[i - 1].x + Random.Range(-randomCoordinates, randomCoordinates);
            z = points[i - 1].z + Random.Range(-randomCoordinates, randomCoordinates);
            points[i] = new Vector3(x, 0, z);

            speed[i] = Random.Range(speedAtLocations.Min, speedAtLocations.Max);
        }

        return points;
    }

    public void GenerateScenario(string file)
    {
        if (File.Exists(file + fileExtension) || File.Exists(file + shipListEndName + fileExtension))
        {
            Logger.Log($"{file + fileExtension} or {file + shipListEndName + fileExtension} already exists.");
            return;
        }

        using TextWriter scenarioWriter = new StreamWriter(file + fileExtension, true);
        using TextWriter shipListWriter = new StreamWriter(file + shipListEndName + fileExtension, true);

        scenarioWriter.WriteLine("ID, X Coordinate, Z Coordinate, Speed");
        shipListWriter.WriteLine("ID, Type");

        int shipTypeEnumLength = System.Enum.GetNames(typeof(ShipType)).Length;

        for (int i = 0; i < numberOfShips; i++)
        {
            Vector3[] locations = GeneratePath(out int[] speed);

            for (int x = 0; x < locations.Length; x++)
            {
                scenarioWriter.WriteLine($"{i + 1}, {locations[x].x}, {locations[x].z}, {speed[x]}");
            }

            shipListWriter.WriteLine($"{i + 1}, {(ShipType)Random.Range(0, shipTypeEnumLength)}");
        }

        // Save the settings to a json file
        ScenarioSettings settings = new()
        {
            waves = waves.GetCurrentElement(),
            weather = weathers.GetCurrentElement(),

            // Procedural land settings
            hasProceduralLand = hasProceduralLand,
            proceduralLandSeed = proceduralLandSeed,
            proceduralLandLocation = proceduralLandLocation,
            directionToSpawnRadars = direction,
        };

        string json = JsonUtility.ToJson(settings, true);
        File.WriteAllText(file + scenarioSettingsEndName, json);

        // Debug.Log("csv has been generated.");
    }

    public void GenerateScenarios(int numOfScenarios = 1, string filePath = null)
    {
        // TODO: Add error messages
        if (numOfScenarios < 0)
        {
            Logger.Log("Invalid number of scenarios inputted.");
            return;
        }

        if (filePath == null)
        {
            filePath = this.filePath;
        }

        // Delete the file path and all scenarios in it
        if (Directory.Exists(filePath))
            Directory.Delete(filePath, true);

        Directory.CreateDirectory(filePath);

        for (int i = 0; i < numOfScenarios; i++)
        {
            string file = filePath + "Scenario" + i;
            GenerateRandomParameters();
            GenerateScenario(file);
        }

        Logger.Log("All scenarios have been generated.");

        mainMenuController.ScenarioMenuUI.ReadScenarios();
    }

    public bool ReadScenarioCSV(
        ref Dictionary<int, ShipInformation> shipsInformation,
        ref Dictionary<int, List<ShipCoordinates>> shipLocations,
        ref ScenarioSettings scenarioSettings,
        string scenarioFileName)
    {
        shipsInformation.Clear();
        shipLocations.Clear();

        try
        {
            // Read ship list information
            using (StreamReader streamReader = new(scenarioFileName + shipListEndName + fileExtension))
            {
                _ = streamReader.ReadLine(); // Ignore the first line which is the headings
                string data = streamReader.ReadLine();
                while (data != null)
                {
                    string[] value = data.Split(',');

                    // Ensure all rows do not have empty or null cells
                    if (value.Any(s => string.IsNullOrEmpty(s)))
                    {
                        //Debug.Log("Error: Invalid number of columns");
                        shipsInformation.Clear();
                        return false;
                    }

                    int id = int.Parse(value[0]);

                    // Keep track of ship IDs in case there are duplicate IDs in the csv
                    if (shipsInformation.ContainsKey(id))
                    {
                        //Debug.Log("Error: Ship list csv contains duplicate ID");
                        shipsInformation.Clear();
                        return false;
                    }
                    else
                    {
                        // If ship type is not found default to 0
                        if (!System.Enum.TryParse(typeof(ShipType), value[1], false, out object result))
                        {
                            Logger.Log($"{value[1]} is not a valid ship type. Defaulting to {System.Enum.Parse(typeof(ShipType), "0")}");
                            shipsInformation[id] = new ShipInformation(id, 0);
                        }
                        else
                            shipsInformation[id] = new ShipInformation(id, (ShipType)result);
                    }

                    data = streamReader.ReadLine();
                }
            }

            // Read each ship locations and speed
            using (StreamReader streamReader = new(scenarioFileName + fileExtension))
            {
                _ = streamReader.ReadLine(); // Ignore the first line which is the headings
                string data = streamReader.ReadLine();
                while (data != null)
                {
                    string[] value = data.Split(',');

                    // Ensure all rows do not have empty or null cells
                    if (value.Any(s => string.IsNullOrEmpty(s)))
                    {
                        //Debug.Log("Error: Invalid number of columns");
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
            }

            // Read scenario settings
            using (StreamReader streamReader = new(scenarioFileName + scenarioSettingsEndName))
            {
                string json = streamReader.ReadToEnd();
                scenarioSettings = JsonUtility.FromJson<ScenarioSettings>(json);
            }

            //Debug.Log("csv has been successfully parsed.");
            return true;
        }
        catch (FileNotFoundException e)
        {
            Logger.Log($"File not found: {e.Message}");
            return false;
        }
    }

    public string GetFilePath()
    {
        return filePath;
    }
}

public class MinMax<T> 
{
    public T Min { get; set; }
    public T Max { get; set; }

    public MinMax(T min, T max)
    {
        Min = min;
        Max = max;
    }
}


public class LoopArray<T>
{
    readonly T[] array;
    int currentIndex = 0;

    public LoopArray(T[] array)
    {
        if (array == null || array.Length == 0) 
            Logger.Log("Weather or Waves is empty");

        this.array = array;
    }

    public T GetCurrentElement()
    {
        T temp = array[currentIndex];
        currentIndex = (currentIndex + 1) % array.Length;
        return temp;
    }
}