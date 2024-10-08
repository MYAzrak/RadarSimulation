using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crest;
using Unity.Mathematics;
using UnityEngine;

public class RadarController : MonoBehaviour
{
    [Header("Radars Information")]
    [SerializeField] GameObject radarPrefab;
    public int rows = 1;
    public int cols = 1;
    public int distanceBetweenRadars = 50;
    public int numOfRadars;

    [Header("Generate Radars")]
    [SerializeField] bool generateRadars = false;
    [SerializeField] bool generateOneRadar = false;

    [Header("Debug")]
    [SerializeField] int newRadarID = 0;

    GameObject parentEmptyObject;                   // Parent Object of the radars to easily rotate and move them
    List<List<int>> radarIDAtRow;
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


    // Start is called before the first frame update
    void Start()
    {
        parentEmptyObject = new("Radars");

        radarIDAtRow = new();

        for (int i = 0; i < rows; i++)
        {
            radarIDAtRow.Add(new());
        }

        mainMenuController = FindObjectOfType<MainMenuController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (generateOneRadar)
        {
            GenerateRadar();
            generateOneRadar = false;
        }
        else if (generateRadars)
        {
            if (numOfRadars == 0)
                numOfRadars = rows * cols;
            GenerateRadars(numOfRadars);
            generateRadars = false;
        }
    }
    public void SetWeather(Weather weather)
    {
        if (weather == Weather.Clear)
        {
            SetRadarWeather(0f, 0);
        }
        else if (weather == Weather.LightRain)
        {
            SetRadarWeather(UnityEngine.Random.Range(0.01f, 0.07f), 20);

        }
        else if (weather == Weather.HeavyRain)
        {
            SetRadarWeather(UnityEngine.Random.Range(0.1f, 0.2f), 15);
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

        // Get the row with the least radars and its index
        float min = math.INFINITY;
        int index = 0;
        for (int k = 0; k < radarIDAtRow.Count; k++)
        {
            if (radarIDAtRow[k].Count < min)
            {
                min = radarIDAtRow[k].Count;
                index = k;
            }
        }

        // Create radar at the row with least radars
        int latestRadarID = radarIDAtRow[index].LastOrDefault();
        if (radars.Keys.Contains(latestRadarID))
        {
            Vector3 latestRadarPosition = radars[latestRadarID].transform.position;

            // If min == 0 then the radar is on the same row
            if (min == 0)
                instance.transform.position = new Vector3(latestRadarPosition.x, 0, latestRadarPosition.z + (distanceBetweenRadars * index));
            else
                instance.transform.position = new Vector3(latestRadarPosition.x + distanceBetweenRadars, 0, latestRadarPosition.z);
        }

        radarIDAtRow[index].Add(newRadarID);

        // Make the new radar a child of parentEmptyObject
        instance.transform.parent = parentEmptyObject.transform;

        // Keep track of created radars
        radars[newRadarID] = instance;

        newRadarID++; // Update for the next radar generated to use
    }

    public void GenerateRadars(int numOfRadars = 1)
    {
        UnloadRadars();

        // Generate the new radars
        for (int i = 0; i < numOfRadars; i++)
        {
            GenerateRadar();
        }

        mainMenuController.SetRadarsLabel(numOfRadars);
        //Debug.Log($"{numOfRadars} radars have been generated");
    }

    public async void UpdateRadarsPositions()
    {
        foreach (KeyValuePair<int, GameObject> entry in radars)
        {
            // Disable kinematic to avoid unwanted forces being applied
            Rigidbody rb = entry.Value.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            Vector3 position = entry.Value.transform.position;
            sampleHeightHelper.Init(position, 0, true);

            float o_height = await WaitForSampleAsync();

            entry.Value.transform.position = new Vector3(position.x, o_height, position.z);

            rb.isKinematic = false;
        }

        mainMenuController.SetRadarsLabel(numOfRadars);
    }

    async Task<float> WaitForSampleAsync()
    {
        float o_height;

        // Wait until we get a valid sample height
        while (!sampleHeightHelper.Sample(out o_height))
        {
            await Task.Yield();
        }

        return o_height;
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
        radarIDAtRow.Clear();

        for (int i = 0; i < rows; i++)
        {
            radarIDAtRow.Add(new());
        }

        newRadarID = 0;
    }
}
