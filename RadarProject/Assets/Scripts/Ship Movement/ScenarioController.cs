using System;
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

    [Header("Wave Prefabs")]
    public List<WavePrefab> wavePrefabs = new();

    [Header("Time options")]
    public int timeScale = 1;
    public bool updateTimeScale = false;

    [Header("Scenario Options")]
    public bool loadScenario = false;
    bool scenarioCurrentlyRunning = false;
    public bool resetScenario = false;

    [Header("Debug")]
    public bool logMessages = false;
    bool previousLogMessageBool = false;                                // Allows the log messages to be enabled or disabled using the same if statement
    public float timeSinceScenarioStart;
    public float timeLimit = 300;                                       // A time limit for the scenario (in seconds)
    public int completedShips = 0;                                      // Ships that have completed their path
    public bool loadAllScenarios = false;

    // -------------------------------------------------
    // --------- Scenario Files Path and Names ---------
    // -------------------------------------------------
    string filePath;
    string filePattern = @"^Scenario\d+\.csv$";                         // ScenarioX.csv where X is any number

    // -------------------------------------------------
    // ------- Stored Scenario File Information --------
    // -------------------------------------------------
    Dictionary<int, ShipInformation> shipsInformation = new();          // <Ship id, list of ship info>
    Dictionary<int, List<ShipCoordinates>> shipLocations = new();       // <Ship id, list of ship coordinates>
    public List<GameObject> generatedShips = new();
    ScenarioSettings scenarioSettings;
    bool csvReadResult;

    // -------------------------------------------------
    // ----- Other Classes the Script Makes Use of -----
    // -------------------------------------------------
    RadarController radarController;
    CSVController csvController;
    MainMenuController mainMenuController;
    WavesTracker wavesTracker;

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
        filePath = Application.persistentDataPath + "/Scenarios/";
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);
    }

    void Start()
    {
        timeSinceScenarioStart = Time.time;

        wavesTracker = FindObjectOfType<WavesTracker>();

        csvController = GetComponent<CSVController>();
        radarController = GetComponent<RadarController>();
        mainMenuController = FindObjectOfType<MainMenuController>();
        Time.timeScale = 1f;

        StartCoroutine(UpdateScenarioLabelAnimation());
        StartCoroutine(RunNextScenario());
    }

    void Update()
    {
        if (loadScenario)
        {
            LoadScenario(scenarios[currentScenarioIndex]);
            loadScenario = false;
        }
        /*
        else if (resetScenario) 
        {
            LoadScenario();
            resetScenario = false;
            scenarioCurrentlyRunning = true;
            timeSinceScenarioStart = 0;
        }
        */
        else if (logMessages != previousLogMessageBool)
        {
            foreach (var ship in generatedShips)
            {
                ship.GetComponent<ShipController>().logMessages = logMessages;
            }

            previousLogMessageBool = logMessages;
        }
        else if (updateTimeScale)
        {
            Time.timeScale = timeScale;
            updateTimeScale = false;
        }
        
        if (timeSinceScenarioStart > timeLimit)
            endScenario = true;

        timeSinceScenarioStart += Time.deltaTime;
        wavesTracker.SetTimeProvider(timeSinceScenarioStart);
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

        Debug.Log("Scenario files have been read.");

        scenarios = files;

        return files;
    }

    void LoadScenario(string scenario)
    {
        // Read csv
        csvReadResult = csvController.ReadScenarioCSV(ref shipsInformation, ref shipLocations, ref scenarioSettings, scenario);
        if (!csvReadResult) return;

        // set scenario name
        this.scenario = scenario;
        endScenario = false;

        UnloadAllObjects();

        // Reset ships that completed their path
        completedShips = 0;

        wavesTracker.GenerateWaves(scenarioSettings.waves);
        mainMenuController.SetWaveLabel(scenarioSettings.waves.ToString());

        radarController.UpdateRadarsPositions();
        GenerateShips();

        mainMenuController.SetShipsLabel(generatedShips.Count);

        // Set the label for the animation
        scenarioLabels[0] = $"{scenario} Running.";
        scenarioLabels[1] = $"{scenario} Running..";
        scenarioLabels[2] = $"{scenario} Running...";

        // Reset variables and start scenario
        logMessages = previousLogMessageBool = false;
        timeSinceScenarioStart = 0;
        scenarioCurrentlyRunning = true;

        //Debug.Log($"{scenario} has been loaded");
    }

    public void LoadAllScenarios()
    {
        loadAllScenarios = true;
        loadScenario = true;
    }

    void UnloadAllObjects()
    {
        // Destroy all generated ships
        foreach (var ship in generatedShips)
        {
            Destroy(ship);
        }

        // Reset Wave
        wavesTracker.ResetToDefaultWave();

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
                    //Debug.Log($"No prefabs found. Skipping ship with ID {shipsInformation[ship.Key].Id}");
                    break;
                }

                prefab = shipPrefabs[0].prefab;

                if (prefab == null)
                {
                    //Debug.Log($"No prefabs found at the first index of Ship Prefabs. Skipping ship with ID {shipsInformation[ship.Key].Id}");
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
                //Debug.Log($"Unable to find ship controller component. Ship with ID: {shipsInformation[ship.Key].Id} is uninitialized.");
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

    public void ReportCompletion()
    {
        completedShips += 1;
    }

    public void EndScenario()
    {
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
            if (scenarioCurrentlyRunning)
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
            if (scenarioCurrentlyRunning && (completedShips >= generatedShips.Count || endScenario))
            {
                scenarioCurrentlyRunning = false;
                //Debug.Log("Scenario has finished");

                if (!loadAllScenarios)
                {
                    endScenario = false;
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
                    LoadScenario(scenario);
                }
                else
                {
                    EndAllScenarios();
                    currentScenarioIndex = 0;
                }
            }
        }
    }

    [Serializable]
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

    [Serializable]
    public struct WavePrefab
    {
        public Waves waves;
        public GameObject prefab;

        public WavePrefab(Waves waves, GameObject prefab)
        {
            this.waves = waves;
            this.prefab = prefab;
        }
    }
}

