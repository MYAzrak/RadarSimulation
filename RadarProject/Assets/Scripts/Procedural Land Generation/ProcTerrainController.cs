using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProcTerrainController : MonoBehaviour
{
    public GameObject terrainGeneratorPrefab = null;
    public int seed;
    public Vector3 position = new Vector3(0, 0, 0);

    private GameObject terrainInstance;
    private MapGenerator mapGenerator;
    void Start()
    {
        GenerateTerrain();
    }
    public void GenerateTerrain()
    {
        terrainInstance = Instantiate(terrainGeneratorPrefab, position, Quaternion.identity);
        PositionAllChildren(terrainInstance.transform, position);

        mapGenerator = terrainInstance.GetComponentInChildren<MapGenerator>();

        if (mapGenerator != null)
        {
            mapGenerator.seed = seed;
            mapGenerator.GenerateMap();
        }
        else
        {
            Logger.Log("MapGenerator component not found on the instantiated prefab.");
        }
    }

    private void PositionAllChildren(Transform parent, Vector3 newPosition)
    {
        foreach (Transform child in parent)
        {
            child.position = newPosition;
        }


    }

}
