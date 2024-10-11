using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

public class ProcTerrainController : MonoBehaviour
{
    public GameObject terrainGeneratorPrefab = null;
    public int seed;
    public Vector3 position = new();

    private GameObject terrainInstance;
    private MapGenerator mapGenerator;
    private ObjectAreaSpawner objectAreaSpawner;

    void Start()
    {
        objectAreaSpawner = FindObjectOfType<ObjectAreaSpawner>();
        //GenerateTerrain();
    }
    public async Task GenerateTerrain()
    {
        terrainInstance = Instantiate(terrainGeneratorPrefab, position, Quaternion.identity);
        PositionAllChildren(terrainInstance.transform, position);

        mapGenerator = terrainInstance.GetComponentInChildren<MapGenerator>();

        if (mapGenerator != null)
        {
            mapGenerator.seed = seed;
            await mapGenerator.GenerateMapAsync();
        }
        else
        {
            Logger.Log("MapGenerator component not found on the instantiated prefab.");
        }

        objectAreaSpawner.GenerateObjects(terrainInstance.transform);
    }

    private void PositionAllChildren(Transform parent, Vector3 newPosition)
    {
        foreach (Transform child in parent)
        {
            child.position = newPosition;
        }
    }

    public void UnloadLandObjects()
    {
        objectAreaSpawner.UnloadAllObjects();

        if (terrainInstance != null)
            Destroy(terrainInstance);
    }
}
