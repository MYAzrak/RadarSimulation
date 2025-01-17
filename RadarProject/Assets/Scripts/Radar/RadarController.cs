using System.Collections.Generic;
using System.Linq;
using System;
using Crest;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RadarController : MonoBehaviour
{
    [Header("Radars Information")]
    [SerializeField] GameObject radarPrefab;
    public int rows = 1;
    public int cols = 1;
    int distanceBetweenRadars;
    public int maxDistance = 5000;
    public Vector3 locationToCreateRadar = Vector3.zero;
    public int numOfRadars;

    [Header("Debug")]
    [SerializeField] int newRadarID = 0;

    public GameObject parentEmptyObject;                   // Parent Object of the radars to easily rotate and move them
    public Dictionary<int, int> numOfRadarsPerRow;
    public Dictionary<int, GameObject> radars = new();

    MainMenuController mainMenuController;
    WavesController wavesController;

    [Header("Radar Equation Parameters")]
    public float transmittedPowerW = 50f; // Watts
    public float antennaGainDBi = 30f; // dBi
    public float waveLength = 0.0031228381f; // meters (for 9.5 GHz)
    public float systemLossesDB = 3f; // dB
    public float antennaVerticalBeamWidth = 22f;
    public float antennaHorizontalBeamWidth = 1.2f;
    
    public int ImageRadius = 1000;
    public int HeightRes = 1024;
    public int WidthRes = 10;

    private Weather currentWeather = Weather.Clear;
    public float rainRCS = 0.001f;

    public RadarGenerationDirection direction = RadarGenerationDirection.Right;
    
    // Used for a more realistic ocean normal detection by the radar
    [Header("Ocean Community")]
    public GameObject oceanCalm;
    public GameObject oceanModerate;
    List<GameObject> radarOceansGenerated = new();

    // Start is called before the first frame update
    void Start()
    {
        parentEmptyObject = new("Radars");

        // Set radars to a different position in demo scene
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name == "KhorfakkanCoastline")
        {
            GameObject terrain = GameObject.Find("Terrain");
            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();

            if (terrainCollider != null)
            {
                Bounds bounds = terrainCollider.bounds;

                float z = bounds.center.z;
                float depth = bounds.size.z;

                parentEmptyObject.transform.position = new Vector3(0, 0, z + (depth / 2));
                Logger.Log($"Demo scene detected. Setting radar position to {parentEmptyObject.transform.position}");
            }
            else
            {
                Logger.Log("TerrainCollider not found on terrain.");
            }
        }

        numOfRadarsPerRow = new();

        for (int i = 0; i < rows; i++)
        {
            numOfRadarsPerRow[i] = 0;
        }

        mainMenuController = FindObjectOfType<MainMenuController>();
        wavesController = FindObjectOfType<WavesController>();
    }

    public void SetWeather(Weather weather)
    {
        int RainIntensity = GetRainIntensity(weather);
        SetRadarWeather(RainIntensity);
        currentWeather = weather;
    }

    private void SetRadarWeather(int RainIntensity)
    {
        foreach (KeyValuePair<int, GameObject> entry in radars)
        {
            RadarScript script = entry.Value.GetComponentInChildren<RadarScript>();
            script.RainIntensity = RainIntensity;
        }
    }

    private int GetRainIntensity(Weather weather)
    {
        int rainMultiplier = 15;
        if (weather == Weather.Clear)
        {
            return 0;
        }
        else if (weather == Weather.ModerateRain)
        {
            return UnityEngine.Random.Range(2,7) * rainMultiplier;
        }
        else if (weather == Weather.HeavyRain)
        {
            return UnityEngine.Random.Range(6,11) * rainMultiplier;
        }
        else if (weather == Weather.VeryHeavyRain)
        {
            return UnityEngine.Random.Range(10,19) * rainMultiplier;
        }
        else if (weather == Weather.Shower)
        {
            return UnityEngine.Random.Range(18, 31) * rainMultiplier;
        }
        else if (weather == Weather.CloudBurst)
        {
            return UnityEngine.Random.Range(30, 36) * rainMultiplier;
        }

        return 0;
    }

    public void GenerateRadar()
    {
        if (radarPrefab == null) return;

        // Create Radar
        GameObject instance = Instantiate(radarPrefab);        

        // Update Radar ID for the radar
        RadarScript radarScript = instance.GetComponentInChildren<RadarScript>();
        radarScript.radarID = newRadarID;
        radarScript.transmittedPowerW = transmittedPowerW;
        radarScript.antennaGainDBi = antennaGainDBi;
        radarScript.waveLength = waveLength;
        radarScript.systemLossesDB = systemLossesDB;
        radarScript.ImageRadius = ImageRadius;
        radarScript.antennaVerticalBeamWidth = antennaVerticalBeamWidth;
        radarScript.antennaHorizontalBeamWidth = antennaHorizontalBeamWidth;
        radarScript.HeightRes = HeightRes;
        radarScript.WidthRes = WidthRes;
        radarScript.MaxDistance = maxDistance;
        radarScript.RainRCS = rainRCS;

        radarScript.RainIntensity = GetRainIntensity(currentWeather);

        // Keep track of created radars
        radars[newRadarID] = instance;

        if (locationToCreateRadar == Vector3.zero)
        {
            // Get the row with the least radars and its key
            var min = numOfRadarsPerRow.First();
            foreach (var pair in numOfRadarsPerRow)
            {
                if (pair.Value < min.Value)
                {
                    min = pair;
                }
            }

            distanceBetweenRadars = maxDistance * 2;

            int key = min.Key; // 0 first row, 1 second row, etc

            int xDistance = min.Value * distanceBetweenRadars;

            int radarToSpawnAt = (min.Value * rows) + key;
            Vector3 latestRadarPosition;

            // Create radar at the row with least radars
            if (radars.Keys.Contains(radarToSpawnAt))
                latestRadarPosition = radars[radarToSpawnAt].transform.position;
            else
                latestRadarPosition = radars[radarToSpawnAt + 1].transform.position;

            float x = latestRadarPosition.x;
            float y = 0;
            float z = latestRadarPosition.z;

            float zAdd = z + (distanceBetweenRadars * key);
            float xAdd = x + xDistance;

            instance.transform.position = parentEmptyObject.transform.position;
            if (direction == RadarGenerationDirection.Right || direction == RadarGenerationDirection.Up)
            {
                instance.transform.position += new Vector3(xAdd, y, zAdd);
            }
            else if (direction == RadarGenerationDirection.Left)
            {
                instance.transform.position += new Vector3(-xAdd, y, zAdd);
            }
            else if (direction == RadarGenerationDirection.Down)
            {
                instance.transform.position += new Vector3(xAdd, y, -zAdd);
            }

            numOfRadarsPerRow[key] += 1;
        }
        else
        {
            instance.transform.position = locationToCreateRadar;
        }


        // Make the new radar a child of parentEmptyObject
        instance.transform.parent = parentEmptyObject.transform;
        radarScript.Init();

        Waves wave = wavesController.currentWaveCondition;
        SetRadarWaveCondition(wave, radarScript);

        newRadarID++; // Update for the next radar generated to use

        mainMenuController.SetRadarsLabel(newRadarID);
    }

    public void GenerateRadars(int numOfRadars = 1, RadarGenerationDirection direction = RadarGenerationDirection.Right)
    {
        UnloadRadars();

        this.direction = direction;

        // Generate the new radars
        for (int i = 0; i < numOfRadars; i++)
        {
            GenerateRadar();
        }

        mainMenuController.SetRadarsLabel(numOfRadars);
        //Debug.Log($"{numOfRadars} radars have been generated");
    }

    // Radar is making use of a different more realistic ocean
    void SetRadarWaveCondition(Waves wave, RadarScript radarScript)
    {
        GameObject oceanInstance = null;
        if (wave == Waves.Calm)
            oceanInstance = Instantiate(oceanCalm);
        else if (wave == Waves.Moderate)
            oceanInstance = Instantiate(oceanModerate);

        if (oceanInstance == null)
        {
            Logger.Log("Unable to generate radar ocean");
        }

        radarOceansGenerated.Add(oceanInstance);

        Ocean o = oceanInstance.GetComponent<Ocean>();
        o.AssignFolowTarget(radarScript.radarCamera.transform);
        o.followMainCamera = true;
    }

    public void UnloadRadars()
    {
        // Delete radar ocean
        foreach (GameObject ocean in radarOceansGenerated)
        {
            Destroy(ocean);
        }
        radarOceansGenerated.Clear();

        // Delete the radar objects
        foreach (KeyValuePair<int, GameObject> entry in radars)
        {
            Destroy(entry.Value);
        }

        // Clear the arrays
        radars.Clear();
        numOfRadarsPerRow.Clear();

        for (int i = 0; i < rows; i++)
        {
            numOfRadarsPerRow[i] = 0;
        }

        newRadarID = 0;
    }
}
