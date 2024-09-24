using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class RadarController : MonoBehaviour
{
    [Header("Radars Information")]
    [SerializeField] GameObject radarPrefab;
    public int rows = 1;
    public int cols = 1;
    public int distanceBetweenRadars = 50;

    [Header("Generate Radars")]
    [SerializeField] bool generateRadars = false;
    [SerializeField] bool generateOneRadar = false;

    [Header("Debug")]
    [SerializeField] int newRadarID = 0;

    GameObject parentEmptyObject;                   // Parent Object of the radars to easily rotate and move them
    List<List<int>> radarIDAtRow;
    Dictionary<int, GameObject> radars = new();

    MainMenuController mainMenuController;

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
            int numOfRadars = rows * cols;
            GenerateRadars(numOfRadars);
            generateRadars = false;
        }
    }

    public void GenerateRadar()
    {
        if (radarPrefab == null) return;
        
        // Create Radar
        GameObject instance = Instantiate(radarPrefab);

        // Update Radar ID for the radar
        RadarScript radarScript = instance.GetComponent<RadarScript>();
        radarScript.radarID = newRadarID;

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

    public void UnloadRadars()
    {
        foreach (KeyValuePair<int, GameObject> entry in radars)
        {
            Destroy(entry.Value);
        }

        newRadarID = 0;
    }
}
