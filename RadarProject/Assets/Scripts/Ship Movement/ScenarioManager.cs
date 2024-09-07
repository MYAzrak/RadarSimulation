using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Crest;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario and Ship Prefabs")]
    public string scenarioFileName;
    [SerializeField] List<ShipPrefab> shipPrefabs = new();

    [Header("Time options")]
    public int timeScale = 1;
    public bool updateTimeScale = false;

    [Header("Scenario Options")]
    public bool loadScenario = false;
    bool scenarioCurrentlyRunning = false;
    public bool resetScenario = false;

    [Header("Debug")]
    public bool logMessages = false;
    
    string filePath = Application.dataPath + "/Scenarios/";
    string filePattern = @"^Scenario\d+\.csv$";                         // ScenarioX.csv where X is any number
    Dictionary<int, ShipInformation> shipsInformation = new();          // <Ship id, list of ship info>
    Dictionary<int, List<ShipCoordinates>> shipLocations = new();       // <Ship id, list of ship coordinates>
    List<GameObject> generatedShips = new();
    int completedShips = 0;                                             // Ships that have completed their path
    bool csvReadResult;
    bool previousLogMessageBool = false;                                // Allows the log messages to be enabled or disabled using the same if statement

    CSVManager csvManager;
    MainMenuController mainMenuController;
    OceanRenderer oceanRenderer;
    TimeProviderCustom timeProviderCustom;
    float timeSinceScenarioStart;
    public bool loadAllScenarios = false;
    int currentScenarioIndex = 0;
    List<string> scenarios = new();

    public const float METERS_PER_SECOND_TO_KNOTS = 1.943844f;          // 1 Meter/second = 1.943844 Knot
    public const float KNOTS_TO_METERS_PER_SECOND = 0.5144444f;         // 1 Knot = 0.5144444 Meter/second
    
    void Start()
    {
        timeSinceScenarioStart = Time.time;

        oceanRenderer = FindObjectOfType<OceanRenderer>();
        timeProviderCustom = FindObjectOfType<TimeProviderCustom>();
        timeProviderCustom._overrideTime = true;

        oceanRenderer.PushTimeProvider(timeProviderCustom);

        csvManager = GetComponent<CSVManager>();
        mainMenuController = FindObjectOfType<MainMenuController>();
        Time.timeScale = 2f;

        StartCoroutine(UpdateScenarioLabelAnimation());
        StartCoroutine(RunNextScenario());
    }

    void Update()
    {
        if (loadScenario)
        {
            LoadScenario(scenarios[currentScenarioIndex]);
            loadScenario = false;

            mainMenuController.SetscenarioRunningLabelVisibility(true);
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

        timeSinceScenarioStart += Time.deltaTime;
        timeProviderCustom._time = timeSinceScenarioStart;
    }

    // Read all files in filePath and store the scenarios that match filePattern for the Unity inspector 
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
        csvReadResult = csvManager.ReadScenarioCSV(ref shipsInformation, ref shipLocations, scenario);
        if (!csvReadResult) return;

        // Destroy all generated ships
        foreach (var ship in generatedShips) 
        {
            Destroy(ship);
        }

        generatedShips.Clear();
        completedShips = 0;
        
        GenerateShips();

        logMessages = previousLogMessageBool = false;
        timeSinceScenarioStart = 0;
        scenarioCurrentlyRunning = true;

        mainMenuController.SetScenarioLabel(scenario);
        mainMenuController.SetShipsLabel(generatedShips.Count);

        Debug.Log($"{scenario} has been loaded");
    }

    void GenerateShips()
    {
        foreach (var ship in shipLocations)
        {
            // The first location is the starting position of the ship
            float x = ship.Value[0].x_coordinates;
            float z = ship.Value[0].z_coordinates;
            Vector3 shipLocation = new(x, 10, z);
            
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


    IEnumerator UpdateScenarioLabelAnimation()
    {
        string[] scenarioLabels = {".", "..", "..."};
        while (Application.isPlaying) 
        {
            yield return new WaitForSeconds(1);
            if (scenarioCurrentlyRunning)
            {
                for (int i = 0; i < scenarioLabels.Length; i++)
                {
                    mainMenuController.SetScenarioRunningLabel($"Scenario Running{scenarioLabels[i]}");
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
            if (scenarioCurrentlyRunning && completedShips == generatedShips.Count)
            {
                scenarioCurrentlyRunning = false;
                Debug.Log("Scenario has finished");

                for (int i = 5; i > 0; i--)
                {
                    mainMenuController.SetScenarioRunningLabel($"Scenario has completed.\nRunning next scenario in {i}.");
                    yield return new WaitForSeconds(1);
                }

                if (loadAllScenarios)
                {
                    currentScenarioIndex++;

                    if (currentScenarioIndex < scenarios.Count)
                    {
                        string scenario = scenarios[currentScenarioIndex];
                        LoadScenario(scenario);
                    }
                    else
                    {
                        loadAllScenarios = false;
                        currentScenarioIndex = 0;
                    }
                }

                mainMenuController.SetScenarioRunningLabel("Scenario Running.");
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

