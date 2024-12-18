using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class RuntimeTerrainGenerator : MonoBehaviour
{
    private Terrain terrain;               
    public Texture2D heightmapTexture;      
    public float heightScale = 600f;
    public int smoothingIterations = 2;
    //public GameObject waterPrefab;
    float waterHeight = 0f;
    public GameObject[] treePrefabs;
    public int numberOfTrees = 20;

    public GameObject[] bushPrefabs;
    public int numberOfBushes = 10;

    public GameObject[] housePrefabs;
    public Transform[] houseSpawnPositions;
    public int numberOfHouses = 5;


    [Range(0, 1)] public float maxSlope = 0.2f;
    // Terrain width and length
    public float terrainWidth = 1000f;
    public float terrainLength = 1000f;

    public TerrainLayer sandLayer;

    public Material skyboxMaterial;
    private List<Vector3> occupiedPositions = new List<Vector3>();
    public float minimumDistance = 5f;
    public float safeDistanceFromHouses = 10f; //Minimum distance from house positions
    public List<GameObject> exclusionZoneObjects;
    public float maxSpawnHeight = 100f;



    float BilinearSample(Texture2D texture, float x, float y)
    {
        int xMin = Mathf.FloorToInt(x);
        int xMax = Mathf.Min(texture.width - 1, Mathf.CeilToInt(x));
        int yMin = Mathf.FloorToInt(y);
        int yMax = Mathf.Min(texture.height - 1, Mathf.CeilToInt(y));

        float xLerp = x - xMin;
        float yLerp = y - yMin;

        float bottomLeft = texture.GetPixel(xMin, yMin).grayscale;
        float bottomRight = texture.GetPixel(xMax, yMin).grayscale;
        float topLeft = texture.GetPixel(xMin, yMax).grayscale;
        float topRight = texture.GetPixel(xMax, yMax).grayscale;

        float bottom = Mathf.Lerp(bottomLeft, bottomRight, xLerp);
        float top = Mathf.Lerp(topLeft, topRight, xLerp);

        return Mathf.Lerp(bottom, top, yLerp);
    }

    float[,] SmoothHeights(float[,] heights)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        float[,] smoothHeights = new float[width, height];

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                float avgHeight = (
                    heights[x, y] +
                    heights[x - 1, y] + heights[x + 1, y] +  // Left and right
                    heights[x, y - 1] + heights[x, y + 1] +  // Top and bottom
                    heights[x - 1, y - 1] + heights[x + 1, y + 1] +  // Diagonals
                    heights[x - 1, y + 1] + heights[x + 1, y - 1]
                ) / 9.0f;

                smoothHeights[x, y] = avgHeight;
            }
        }

        return smoothHeights;
    }

    void Awake()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found on this GameObject.");
        }
    }


    void Start()
    {
        if (terrain == null || heightmapTexture == null)  // Check if height map is assigned
        {
            Debug.LogError("Terrain or HeightmapTexture is not assigned.");
            return;
        }
        // Terrain size
        terrain.terrainData.size = new Vector3(terrainWidth, heightScale, terrainLength);

        ApplyHeightmap();
        ApplyTerrainLayers();
        //SpawnWater();
        PlantTrees();
        PlantBushes();
        SpawnHouses();
        RenderSettings.skybox = skyboxMaterial;
    }

    void ApplyTerrainLayers()
    {
        TerrainData terrainData = terrain.terrainData;

        // Set only the grass layer
        terrainData.terrainLayers = new TerrainLayer[] { sandLayer };

        // Check Terrain Layers are assigned
        if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
        {
            Debug.LogError("No terrain layers assigned!");
            return;
        }

        // Splatmap data array
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.terrainLayers.Length];

        // Fill splatmap with the grass layer
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Set the grass layer to full weight
                splatmapData[x, y, 0] = 1f; // Full grass
            }
        }

        // Splatmap to the terrain
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }



    void PlantTrees()
    {
        List<Transform> treesToDestroy = new List<Transform>();

        // Find and destroy all the trees before new spawn
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Tree"))
            {
                treesToDestroy.Add(child);
            }
        }

        foreach (Transform tree in treesToDestroy)
        {
            DestroyImmediate(tree.gameObject);
            // Debug.Log("Tree destroyed");
        }



        for (int i = 0; i < numberOfTrees; i++)
        {
            Vector3 position = GetRandomFlatPositionOnTerrain();
            float height = terrain.SampleHeight(position) + terrain.GetPosition().y;
                position.y = height;
                GameObject treePrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                Instantiate(treePrefab, position, Quaternion.identity, transform).tag = "Tree";
                occupiedPositions.Add(position); // Mark position as occupied
        }
    }


    void PlantBushes()
    {
        List<Transform> bushesToDestroy = new List<Transform>();

        // Doing same that previously done for trees
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Bush"))
            {
                bushesToDestroy.Add(child);
            }
        }

        foreach (Transform bush in bushesToDestroy)
        {
            DestroyImmediate(bush.gameObject);
            // Debug.Log("Bush destroyed");
        }


        for (int i = 0; i < numberOfBushes; i++)
        {
            Vector3 position = GetRandomFlatPositionOnTerrain();
            float height = terrain.SampleHeight(position) + terrain.GetPosition().y;
                position.y = height;
                GameObject bushPrefab = bushPrefabs[Random.Range(0, bushPrefabs.Length)];
                Instantiate(bushPrefab, position, Quaternion.identity, transform).tag = "Bush";
                occupiedPositions.Add(position); // Mark position as occupied
        }
    }



    void SpawnHouses()
    {

        List<Transform> bushesToDestroy = new List<Transform>();

        // Collect all bush objects into the list
        foreach (Transform child in transform)
        {
            if (child.CompareTag("House"))
            {
                bushesToDestroy.Add(child);
            }
        }

        // Now destroy all collected bushes
        foreach (Transform bush in bushesToDestroy)
        {
            DestroyImmediate(bush.gameObject);
        }

        // Ensure there are enough predefined positions for the number of houses to spawn
        if (houseSpawnPositions.Length < numberOfHouses)
        {
            Debug.LogError("Not enough predefined house spawn positions for the desired number of houses.");
            return;
        }

        List<int> availablePositions = Enumerable.Range(0, houseSpawnPositions.Length).ToList();

        for (int i = 0; i < numberOfHouses; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Transform spawnPoint = houseSpawnPositions[availablePositions[randomIndex]];

            float height = terrain.SampleHeight(spawnPoint.position) + terrain.GetPosition().y;
            Vector3 spawnPosition = new Vector3(spawnPoint.position.x, height, spawnPoint.position.z);

            GameObject housePrefab = housePrefabs[Random.Range(0, housePrefabs.Length)];
            Instantiate(housePrefab, spawnPosition, Quaternion.identity, transform).tag = "House";

            availablePositions.RemoveAt(randomIndex); // Remove used position
        }
    }



    //void SpawnWater()
    //{
    //    // Find any existing water object in the scene that was instantiated as a child of the terrain
    //    Transform existingWater = transform.Find(waterPrefab.name + "(Clone)");

    //    // If the water object already exists, destroy it
    //    if (existingWater != null)
    //    {
    //        DestroyImmediate(existingWater.gameObject);
    //    }

    //    // Spawn new water prefab at the specified height
    //    if (waterPrefab != null)
    //    {
    //        Vector3 waterPosition = new Vector3(
    //            terrain.transform.position.x + terrain.terrainData.size.x / 2,
    //            waterHeight,
    //            terrain.transform.position.z + terrain.terrainData.size.z / 2
    //        );

    //        Instantiate(waterPrefab, waterPosition, Quaternion.identity, transform);
    //    }
    //    else
    //    {
    //        Debug.LogError("Water prefab is not assigned.");
    //    }
    //}


    public void GenerateTerrain()
    {
        if (terrain == null)
        {
            terrain = GetComponent<Terrain>();
        }

        if (terrain == null || heightmapTexture == null)
        {
            Debug.LogError("Terrain or HeightmapTexture is not assigned.");
            return;
        }

        terrain.terrainData.size = new Vector3(terrainWidth, heightScale, terrainLength);
        ApplyHeightmap();
        ApplyTerrainLayers();
        //SpawnWater();
        PlantTrees();
        PlantBushes();
        SpawnHouses();
        RenderSettings.skybox = skyboxMaterial;
    }


    void ApplyHeightmap()
    {
        TerrainData terrainData = terrain.terrainData;
        int width = heightmapTexture.width;
        int height = heightmapTexture.height;

        terrainData.heightmapResolution = width + 1;
        float[,] heights = new float[width, height];

        // Bilinear interpolation and applying grayscale height data
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Inverting the y-axis by sampling from the opposite row
                float pixelHeight = BilinearSample(heightmapTexture, x, height - y - 1);
                heights[x, y] = pixelHeight * heightScale / terrainData.size.y;
            }
        }

        // Smoothing the terrain
        for (int i = 0; i < smoothingIterations; i++)
        {
            heights = SmoothHeights(heights);
        }

        terrainData.SetHeights(0, 0, heights);
    }


    bool IsFlatArea(Vector3 position)
    {
        Vector3 terrainNormal = terrain.terrainData.GetInterpolatedNormal(position.x / terrain.terrainData.size.x, position.z / terrain.terrainData.size.z);
        float slope = Vector3.Angle(terrainNormal, Vector3.up) / 90.0f;
        return slope <= maxSlope;
    }
    bool IsInExcludedArea(Vector3 position)
    {
        if (exclusionZoneObjects == null || exclusionZoneObjects.Count == 0)
        {
            return false; // No exclusion zones defined, so no area is excluded
        }

        // Iterate over each exclusion zone object and check if the position is within any exclusion bounds
        foreach (GameObject exclusionZoneObject in exclusionZoneObjects)
        {
            if (exclusionZoneObject == null) continue; // Skip null entries in the list

            Bounds exclusionBounds = exclusionZoneObject.GetComponent<Renderer>().bounds;
            if (exclusionBounds.Contains(position))
            {
                return true; // Position is within one of the exclusion areas
            }
        }

        return false; // Position is outside all exclusion areas
    }

    Vector3 GetRandomFlatPositionOnTerrain()
    {
        Vector3 position;
        int attempts = 0;
        float terrainHeight;

        do
        {
            position = GetRandomPositionOnTerrain();
            terrainHeight = terrain.SampleHeight(position);
            position.y = terrainHeight;
            // Increment attempts
            attempts++;

        } while ((terrainHeight > maxSpawnHeight || !IsFlatArea(position) || !IsFarEnough(position) || IsNearHousePosition(position) || IsInExcludedArea(position)) && attempts < 100);

        // If the maximum number of attempts is reached, handle it accordingly (e.g., return a default position or log a warning)
        if (attempts >= 200)
        {
            Debug.LogWarning("Could not find a valid flat position after 100 attempts.");
            return Vector3.zero;
        }
        return position;
    }

    Vector3 GetRandomPositionOnTerrain()
    {
        float terrainWidth = terrain.terrainData.size.x;
        float terrainLength = terrain.terrainData.size.z;

        float randomX = Random.Range(0, terrainWidth);
        float randomZ = Random.Range(0, terrainLength);

        // Return the random position on the terrain
        return new Vector3(randomX, 0, randomZ) + terrain.GetPosition();
    }

    bool IsNearHousePosition(Vector3 position)
    {
        foreach (Transform housePosition in houseSpawnPositions)
        {
            if (Vector3.Distance(position, housePosition.position) < safeDistanceFromHouses)
            {
                return true; // Too close to a house position
            }
        }
        return false; // Far enough from all house positions
    }


    bool IsFarEnough(Vector3 position)
    {
        foreach (var occupied in occupiedPositions)
        {
            if (Vector3.Distance(position, occupied) < minimumDistance)
            {
                return false; // Too close to another object
            }
        }
        return true; // Far enough from all other objects
    }



}

