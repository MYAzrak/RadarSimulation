using System.Collections.Generic;
using System.Linq;
using Crest;
using UnityEngine;

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
    SampleHeightHelper sampleHeightHelper = new();

    [Header("Radar Equation Parameters")]
    public float transmittedPowerW = 1000f; // Watts
    public float antennaGainDBi = 30f; // dBi
    public float wavelengthM = 0.03f; // meters (for 10 GHz)
    public float systemLossesDB = 3f; // dB

    public int ImageRadius = 1000;
    public float VerticalAngle = 30f;
    public float BeamWidth = 2f;

    public int HeightRes = 1024;
    public int WidthRes = 10;

    public RadarGenerationDirection direction = RadarGenerationDirection.Right;

    // Start is called before the first frame update
    void Start()
    {
        parentEmptyObject = new("Radars");

        numOfRadarsPerRow = new();

        for (int i = 0; i < rows; i++)
        {
            numOfRadarsPerRow[i] = 0;
        }

        mainMenuController = FindObjectOfType<MainMenuController>();
    }

    public void SetWeather(Weather weather, bool isFoggy)
    {
        if (weather == Weather.Clear)
        {
            SetRadarWeather(0f, 0);
        }
        else if (weather == Weather.LightRain)
        {
            SetRadarWeather(UnityEngine.Random.Range(0.01f, 0.07f), 15);

        }
        else if (weather == Weather.HeavyRain && !isFoggy)
        {
            SetRadarWeather(UnityEngine.Random.Range(0.1f, 0.2f), 20);
        }

        else if (weather == Weather.HeavyRain && isFoggy)
        {
            SetRadarWeather(UnityEngine.Random.Range(0.3f, 0.4f), 25);
        }
    }

    private void SetRadarWeather(float RainProbability, int RainIntensity)
    {
        foreach (KeyValuePair<int, GameObject> entry in radars)
        {
            RadarScript script = entry.Value.GetComponentInChildren<RadarScript>();
            script.RainProbability = RainProbability;
            script.RainIntensity = RainIntensity;
        }
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
        radarScript.wavelengthM = wavelengthM;
        radarScript.systemLossesDB = systemLossesDB;
        radarScript.ImageRadius = ImageRadius;
        radarScript.VerticalAngle = VerticalAngle;
        radarScript.BeamWidth = BeamWidth;
        radarScript.HeightRes = HeightRes;
        radarScript.WidthRes = WidthRes;
        radarScript.MaxDistance = maxDistance;

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

        newRadarID++; // Update for the next radar generated to use

        mainMenuController.SetRadarsLabel(newRadarID);
    }

    public void GenerateRadars(int numOfRadars = 1, RadarGenerationDirection direction = RadarGenerationDirection.Left)
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

    public void UnloadRadars()
    {
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
