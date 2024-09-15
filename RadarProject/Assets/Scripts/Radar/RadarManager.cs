using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class RadarManager : MonoBehaviour
{
    [Header("Radars Information")]
    [SerializeField] GameObject radarPrefab;
    [SerializeField] int rows = 1;
    [SerializeField] bool generateRadars = false;

    [Header("Debug")]
    [SerializeField] int newRadarID = 0;

    GameObject parentEmptyObject;                   // Parent Object of the radars to easily rotate and move them
    List<List<int>> radarIDAtRow;
    Dictionary<int, GameObject> radars = new();

    // Start is called before the first frame update
    void Start()
    {
        parentEmptyObject = new("Radars");

        radarIDAtRow = new();
        
        for(int i = 0; i < rows; i++)
        {
            radarIDAtRow.Add(new());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (generateRadars)
        {
            GenerateRadar();
            generateRadars = false;
        }
    }

    void GenerateRadar()
    {
        if (radarPrefab != null)
        {
            // Create Radar
            GameObject instance = Instantiate(radarPrefab);

            // Update Radar ID for the radar
            RadarScript radarScript = instance.GetComponent<RadarScript>();
            radarScript.radarID = newRadarID;

            float diameter = radarScript.MaxDistance * 2;

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

                // TODO: Replace 20 with diameter
                if (min == 0)
                    instance.transform.position = new Vector3(latestRadarPosition.x, 0, latestRadarPosition.z + (20 * index));
                else
                    instance.transform.position = new Vector3(latestRadarPosition.x + 20, 0, latestRadarPosition.z);
            }

            radarIDAtRow[index].Add(newRadarID);

            // Make the new radar a child of parentEmptyObject
            instance.transform.parent = parentEmptyObject.transform;

            // Keep track of created radars
            radars[newRadarID] = instance;

            newRadarID++; // Update for the next radar generated to use
        }
    }
}
