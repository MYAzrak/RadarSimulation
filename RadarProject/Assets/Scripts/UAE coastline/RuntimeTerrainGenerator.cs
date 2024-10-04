using UnityEngine;
using System.Linq;

public class RuntimeTerrainGenerator : MonoBehaviour
{
    private Terrain terrain;                // Automatically detect the Terrain object
    public Texture2D heightmapTexture;      // Reference to the heightmap image
    public float heightScale = 600f;
    public int smoothingIterations = 2;
    public GameObject waterPrefab;          // Assign the water prefab in the Inspector
    public float waterHeight = 0.5f;        // Scale for height adjustments
    public GameObject[] treePrefabs;
    public Transform[] treeSpawnTransforms;    // Array of spawn points for trees
    public int numberOfTrees = 20;

    public GameObject[] bushPrefabs;
    public Transform[] bushSpawnTransforms;    // Array of spawn points for bushes
    public int numberOfBushes = 10;

    // Expose terrain width in the Inspector
    public float terrainWidth = 1000f;
    public float terrainLength = 1000f;     // You may also want to expose the terrain length

    public TerrainLayer sandLayer;
    public TerrainLayer grassLayer;
    public TerrainLayer rockLayer;

    // Public variables for height thresholds (adjustable in Inspector)
    [Range(0, 2000)] public float sandHeight = 30f; // Sand applied below this height
    [Range(0, 2000)] public float grassHeight = 90f; // Grass applied below this height
    [Range(0, 2000)] public float rockHeight = 150f; // Rock applied above this height

    public Material skyboxMaterial;


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
        if (terrain == null || heightmapTexture == null)
        {
            Debug.LogError("Terrain or HeightmapTexture is not assigned.");
            return;
        }

        // Apply custom terrain size based on inspector values
        terrain.terrainData.size = new Vector3(terrainWidth, heightScale, terrainLength);

        ApplyHeightmap();
        //ApplyTextures();
        ApplyTerrainLayers();
        SpawnWater();
        PlantTrees();
        PlantBushes();
        RenderSettings.skybox = skyboxMaterial;
    }

    void ApplyTerrainLayers()
    {
        TerrainData terrainData = terrain.terrainData;

        // Assign the terrain layers
        terrainData.terrainLayers = new TerrainLayer[] { sandLayer, grassLayer, rockLayer };

        // Check if terrain layers are properly assigned
        if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
        {
            Debug.LogError("No terrain layers assigned!");
            return;
        }

        // Create a splatmap data array
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.terrainLayers.Length];



        // Loop through each point on the terrain and blend layers based on height
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Get the corresponding height at this point
                float normalizedX = (float)x / (float)terrainData.alphamapWidth;
                float normalizedY = (float)y / (float)terrainData.alphamapHeight;

                float heightAtPoint = terrainData.GetHeight(
                    Mathf.RoundToInt(normalizedX * terrainData.heightmapResolution),
                    Mathf.RoundToInt(normalizedY * terrainData.heightmapResolution)
                ) / terrainData.size.y;

                // Debugging to verify height values
                //if (x % 100 == 0 && y % 100 == 0) // Print debug info every 100 points to avoid spamming
                //{
                //    Debug.Log($"Height at Point ({x}, {y}): {heightAtPoint}");
                //}

                // Initialize splat weights for each terrain layer
                float[] splatWeights = new float[terrainData.terrainLayers.Length];

                // Apply sand layer for low elevations
                if (heightAtPoint < sandHeight / terrainData.size.y)
                {
                    splatWeights[0] = 1f; // Full sand
                }
                // Blend grass layer between sand and rock
                else if (heightAtPoint < grassHeight / terrainData.size.y)
                {
                    float blendFactor = Mathf.InverseLerp(sandHeight / terrainData.size.y, grassHeight / terrainData.size.y, heightAtPoint);
                    splatWeights[0] = 1f - blendFactor; // Sand fading out
                    splatWeights[1] = blendFactor;      // Grass fading in
                }
                // Apply rock layer for high elevations
                else if (heightAtPoint < rockHeight / terrainData.size.y)
                {
                    float blendFactor = Mathf.InverseLerp(grassHeight / terrainData.size.y, rockHeight / terrainData.size.y, heightAtPoint);
                    splatWeights[1] = 1f - blendFactor; // Grass fading out
                    splatWeights[2] = blendFactor;      // Rock fading in
                }
                else
                {
                    splatWeights[2] = 1f; // Full rock
                }

                // Normalize the splat weights to ensure they sum to 1
                float totalWeight = splatWeights.Sum();
                for (int i = 0; i < splatWeights.Length; i++)
                {
                    splatWeights[i] /= totalWeight;
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Apply the splatmap to the terrain
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }


    void PlantTrees()
    {
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Tree"))
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Plant trees using the Transform positions from treeSpawnTransforms
        for (int i = 0; i < Mathf.Min(numberOfTrees, treeSpawnTransforms.Length); i++)
        {
            Transform treeTransform = treeSpawnTransforms[i]; // Get the Transform
            Vector3 position = treeTransform.position;
            Quaternion rotation = treeTransform.rotation;

            float height = terrain.SampleHeight(position) + terrain.GetPosition().y;

            if (height > waterHeight) // Ensure trees are planted above water level
            {
                position.y = height;  // Adjust Y to match terrain height
                GameObject treePrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                Instantiate(treePrefab, position, rotation, transform);  // Use position and rotation from Transform
            }
        }
    }

    void PlantBushes()
    {
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Bush"))
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Plant bushes using the Transform positions from bushSpawnTransforms
        for (int i = 0; i < Mathf.Min(numberOfBushes, bushSpawnTransforms.Length); i++)
        {
            Transform bushTransform = bushSpawnTransforms[i]; // Get the Transform
            Vector3 position = bushTransform.position;
            Quaternion rotation = bushTransform.rotation;

            float height = terrain.SampleHeight(position) + terrain.GetPosition().y;

            if (height > waterHeight) // Ensure bushes are placed above water level
            {
                position.y = height;  // Adjust Y to match terrain height
                GameObject bushPrefab = bushPrefabs[Random.Range(0, bushPrefabs.Length)];
                Instantiate(bushPrefab, position, rotation, transform);  // Use position and rotation from Transform
            }
        }
    }

    void SpawnWater()
    {
        // Find any existing water object in the scene that was instantiated as a child of the terrain
        Transform existingWater = transform.Find(waterPrefab.name + "(Clone)");

        // If the water object already exists, destroy it
        if (existingWater != null)
        {
            DestroyImmediate(existingWater.gameObject);
        }

        // Spawn new water prefab at the specified height
        if (waterPrefab != null)
        {
            Vector3 waterPosition = new Vector3(
                terrain.transform.position.x + terrain.terrainData.size.x / 2,
                waterHeight,
                terrain.transform.position.z + terrain.terrainData.size.z / 2
            );

            Instantiate(waterPrefab, waterPosition, Quaternion.identity, transform);
        }
        else
        {
            Debug.LogError("Water prefab is not assigned.");
        }
    }


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
        //ApplyTextures();
        ApplyTerrainLayers();
        SpawnWater();
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
                float pixelHeight = BilinearSample(heightmapTexture, x, y);
                //Debug.Log($"Heightmap pixel at ({x}, {y}): {pixelHeight}");
                heights[x, y] = pixelHeight * heightScale / terrainData.size.y;
            }
        }

        // Smoothing the terrain
        for (int i = 0; i < smoothingIterations; i++)
        {
            heights = SmoothHeights(heights);
        }

        terrainData.SetHeights(0, 0, heights);


        Debug.Log($"Terrain Scale: {terrain.terrainData.size}");
        Debug.Log($"Height Scale: {heightScale}");



    }


   
}

