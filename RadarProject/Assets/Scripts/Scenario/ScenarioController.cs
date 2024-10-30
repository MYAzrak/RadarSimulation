using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ScenarioController : MonoBehaviour
{
    [Header("Scenario and Ship Prefabs")]
    public string scenarioFileName;
    [SerializeField] List<ShipPrefab> shipPrefabs = new();
    Dictionary<ShipType, ShipPrefab> shipDict = new();

    [Header("Wave Prefabs")]
    public List<WavePrefab> wavePrefabs = new();
    Dictionary<Waves, WavePrefab> waveDict = new();

    [Header("Weather Prefabs")]
    public List<WeatherPrefab> weatherPrefabs = new();
    Dictionary<Weather, WeatherPrefab> weatherDict = new();

    [Header("Time options")]
    public int timeScale = 1;

    [Header("Scenario Options")]
    public bool loadScenario = false;
    bool scenarioCurrentlyRunning = false;

    [Header("Debug")]
    float timeSinceScenarioStart;
    public float timeLimit = 300;                                       // A time limit for the scenario (in seconds)
    public bool loadAllScenarios = false;

    // -------------------------------------------------
    // --------- Scenario Files Path and Names ---------
    // -------------------------------------------------
    string filePath;
    string filePattern = @"^Scenario\d+\.csv$";                         // ScenarioX.csv where X is any number

    // -------------------------------------------------
    // ------- Stored Scenario File Information --------
    // -------------------------------------------------
    public Dictionary<int, ShipInformation> shipsInformation = new();          // <Ship id, list of ship info>
    public Dictionary<int, List<ShipCoordinates>> shipLocations = new();       // <Ship id, list of ship coordinates>
    public List<GameObject> generatedShips = new();
    public ScenarioSettings scenarioSettings;
    bool csvReadResult;

    // -------------------------------------------------
    // ----- Other Classes the Script Makes Use of -----
    // -------------------------------------------------
    RadarController radarController;
    CSVController csvController;
    MainMenuController mainMenuController;
    WavesController wavesController;
    WeatherController weatherController;
    ProcTerrainController procTerrainController;

    // -------------------------------------------------
    // -------- Current Scenario Information -----------
    // -------------------------------------------------
    string scenario = "Scenario";                                       // Scenario File name
    int currentScenarioIndex = 0;
    List<string> scenarios = new();                                     // All scenarios loaded
    bool endScenario = false;                                           // Forcefully ends a scenario
    string[] scenarioLabels = new string[3];                            // Use to animate the scenario label

    // -------------------------------------------------
    // --------------- Speed Conversions ---------------
    // -------------------------------------------------
    public const float METERS_PER_SECOND_TO_KNOTS = 1.943844f;          // 1 Meter/second = 1.943844 Knot
    public const float KNOTS_TO_METERS_PER_SECOND = 0.5144444f;         // 1 Knot = 0.5144444 Meter/second

    void Awake()
    {
        Logger.SetFilePath("Simulation Started");

        filePath = Application.persistentDataPath + "/Scenarios/";
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);
    }

    void Start()
    {
        timeSinceScenarioStart = Time.time;

        wavesController = FindObjectOfType<WavesController>();
        weatherController = GetComponent<WeatherController>();
        procTerrainController = GetComponent<ProcTerrainController>();

        csvController = GetComponent<CSVController>();
        radarController = GetComponent<RadarController>();
        mainMenuController = FindObjectOfType<MainMenuController>();
        Time.timeScale = 1f;

        // Converting the list to dictionaries for faster and cleaner access
        InitDict();

        StartCoroutine(UpdateScenarioLabelAnimation());
        StartCoroutine(RunNextScenario());
    }

    void Update()
    {
        if (scenarioCurrentlyRunning && timeSinceScenarioStart > timeLimit)
            endScenario = true;

        timeSinceScenarioStart += Time.deltaTime;
        wavesController.SetTimeProvider(timeSinceScenarioStart);
    }

    void InitDict()
    {
        foreach (var shipPrefab in shipPrefabs)
        {
            shipDict[shipPrefab.shipType] = shipPrefab;
        }

        foreach (var wavePrefab in wavePrefabs)
        {
            waveDict[wavePrefab.waves] = wavePrefab;
        }

        foreach (var weatherPrefab in weatherPrefabs)
        {
            weatherDict[weatherPrefab.weather] = weatherPrefab;
        }
    }

    // Read all files in filePath 
    public List<string> ReadScenarioFiles()
    {
        List<string> files = new();
        Regex myRegExp = new(filePattern);

        DirectoryInfo info = new(filePath);
        FileInfo[] fileInfo = info.GetFiles()
                                    .Where(file => myRegExp.Match(file.Name).Success)
                                    .OrderBy(file => int.Parse(Path.GetFileNameWithoutExtension(file.Name[8..])))
                                    .ToArray();
        for (int i = 0; i < fileInfo.Length; i++)
        {
            var file = fileInfo[i];
            files.Add(Path.GetFileNameWithoutExtension(file.Name[..^4]));
        }

        Logger.Log("Scenario files have been read.");

        scenarios = files;

        return files;
    }

    public void LoadScenario(string scenario)
    {
        // Read csv
        csvReadResult = csvController.ReadScenarioCSV(ref shipsInformation, ref shipLocations, ref scenarioSettings, scenario);
        if (!csvReadResult) return;

        // set scenario name
        this.scenario = Path.GetFileName(scenario);

        UnloadAllObjects();

        // Set procedural land
        if (scenarioSettings.hasProceduralLand)
        {
            procTerrainController.seed = scenarioSettings.proceduralLandSeed;
            procTerrainController.position = scenarioSettings.proceduralLandLocation;
            procTerrainController.GenerateTerrain();
            
            // Set radars direction
            radarController.direction = scenarioSettings.directionToSpawnRadars;
            
            // Set radars position to be close to the land
            float x = procTerrainController.position.x;
            float y = 0;
            float z = procTerrainController.position.z - 10000;

            switch (scenarioSettings.directionToSpawnRadars)
            {
                case RadarGenerationDirection.Right:
                    x += 15000;
                    break;
                case RadarGenerationDirection.Left:
                    x -= 15000;
                    break;
                default:
                    break;
            }

            radarController.parentEmptyObject.transform.position = new Vector3(x, y, z);
        }
        else
        {
            radarController.direction = RadarGenerationDirection.Right;
            radarController.parentEmptyObject.transform.position = Vector3.zero;
        }

        int numOfRadars = radarController.radars.Count;
        
        if (numOfRadars > 0)
        {
            radarController.UnloadRadars();
            radarController.GenerateRadars(numOfRadars);
        }

        // Set waves
        SetWaves(scenarioSettings.waves);

        // Set weather
        SetWeather(scenarioSettings.weather, scenarioSettings.isFoggy);

        //radarController.UpdateRadarsPositions();
        GenerateShips();

        mainMenuController.SetShipsLabel(generatedShips.Count);

        // Set the label for the animation
        scenarioLabels[0] = $"{this.scenario} Running.";
        scenarioLabels[1] = $"{this.scenario} Running..";
        scenarioLabels[2] = $"{this.scenario} Running...";

        // Reset variables and start scenario
        timeSinceScenarioStart = 0;
        scenarioCurrentlyRunning = true;

        Logger.Log($"{this.scenario} has been loaded");
    }

    public void LoadAllScenarios(string filePath = null)
    {
        if (filePath == null)
            filePath = this.filePath;

        currentScenarioIndex = 0;
        loadAllScenarios = true;

        if (currentScenarioIndex < scenarios.Count)
        {
            scenario = scenarios[currentScenarioIndex];
            LoadScenario(filePath + scenario);
        }
    }

    void UnloadAllObjects()
    {
        // Destroy all generated ships
        foreach (var ship in generatedShips)
        {
            Destroy(ship);
        }

        // Reset Wave to calm
        wavesController.ResetToDefaultWave();

        // Reset Weather
        weatherController.ClearWeather();

        // Delete the land
        procTerrainController.UnloadLandObjects();

        generatedShips.Clear();
    }

    void GenerateShips()
    {
        foreach (var ship in shipLocations)
        {
            // The first location is the starting position of the ship
            float x = ship.Value[0].x_coordinates;
            float z = ship.Value[0].z_coordinates;

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
                if (shipPrefabs.Count == 0)
                {
                    Logger.Log($"No prefabs found. Skipping ship with ID {shipsInformation[ship.Key].Id}");
                    break;
                }

                prefab = shipPrefabs[0].prefab;

                if (prefab == null)
                {
                    Logger.Log($"No prefabs found at the first index of Ship Prefabs. Skipping ship with ID {shipsInformation[ship.Key].Id}");
                    break;
                }

                //Debug.Log($"Unable to find ship prefab for ship type {shipsInformation[ship.Key].Type}. " +
                //$"Defaulting to the first ship prefab for ship with ID {shipsInformation[ship.Key].Id}");
            }

            float shipHeight = prefab.transform.position.y;
            Vector3 shipLocation = new(x, shipHeight, z);

            // If there are more than one location then rotate the generated ship to face the direction of the next location
            if (ship.Value.Count > 1)
            {
                Vector3 heading = new Vector3(ship.Value[1].x_coordinates, shipHeight, ship.Value[1].z_coordinates) - shipLocation;
                float distance = heading.magnitude;
                Vector3 direction = heading / distance;

                instance = Instantiate(prefab, shipLocation, Quaternion.LookRotation(direction));
            }
            else
                instance = Instantiate(prefab, shipLocation, Quaternion.identity);

            ShipController shipController = instance.GetComponent<ShipController>() ?? instance.GetComponentInChildren<ShipController>();
            if (shipController == null)
            {
                Logger.Log($"Unable to find ship controller component. Ship with ID: {shipsInformation[ship.Key].Id} is uninitialized.");
                continue;
            }
            shipController.shipInformation = shipsInformation[ship.Key]; // Initialize the ship information

            generatedShips.Add(instance);

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

    public void RemoveShip(GameObject ship)
    {
        if (generatedShips.Contains(ship))
        {
            generatedShips.Remove(ship);
            Destroy(ship);
        }
    }

    public void EndScenario()
    {
        if (!scenarioCurrentlyRunning) return;

        UnloadAllObjects();
        endScenario = true;
        mainMenuController.SetDefaultSimulationInfoPanel();
    }

    public void EndAllScenarios()
    {
        loadAllScenarios = false;
        EndScenario();
    }

    IEnumerator UpdateScenarioLabelAnimation()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForSeconds(1);
            while (scenarioCurrentlyRunning)
            {
                for (int i = 0; i < scenarioLabels.Length; i++)
                {
                    mainMenuController.SetTimeRemainingLabel(timeLimit - timeSinceScenarioStart);

                    // TODO: Find a better solution since it is possible for SetDefaultSimulationInfoPanel() to be replaced
                    if (!scenarioCurrentlyRunning)
                    {
                        mainMenuController.SetDefaultSimulationInfoPanel();
                        break;
                    }

                    mainMenuController.SetScenarioRunningLabel(scenarioLabels[i]);
                    yield return new WaitForSeconds(1);
                }
            }
        }
    }

    IEnumerator RunNextScenario()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForSeconds(1);
            if (scenarioCurrentlyRunning && endScenario)
            {
                EndScenario();
                scenarioCurrentlyRunning = false;
                endScenario = false;
                Logger.Log("Scenario has finished");

                if (!loadAllScenarios)
                {
                    mainMenuController.SetDefaultSimulationInfoPanel();
                    continue;
                }

                currentScenarioIndex++;

                if (currentScenarioIndex < scenarios.Count)
                {
                    for (int i = 3; i > 0; i--)
                    {
                        mainMenuController.SetScenarioRunningLabel($"Scenario has completed.\nRunning next scenario in {i}.");
                        yield return new WaitForSeconds(1);
                    }

                    scenario = scenarios[currentScenarioIndex];
                    LoadScenario(filePath + scenario);
                }
                else
                {
                    EndAllScenarios();
                    currentScenarioIndex = 0;
                }
            }
            else if (!scenarioCurrentlyRunning && generatedShips.Count > 0)
            {
                UnloadAllObjects();
            }
        }
    }

    public void SetTimeScale(int speed)
    {
        Time.timeScale = speed;
    }

    public void SetWeather(Weather weather, bool isFoggy)
    {
        GameObject prefab = weatherDict[weather].prefab;
        Material skybox = weatherDict[weather].skybox;
        Material oceanMaterial = weatherDict[weather].oceanMaterial;
        weatherController.GenerateWeather(weather, prefab, skybox, oceanMaterial);
        radarController.SetWeather(weather, isFoggy);
    }

    public void SetWaves(Waves waves)
    {
        wavesController.GenerateWaves(waves, waveDict[waves].prefab);
    }
}

