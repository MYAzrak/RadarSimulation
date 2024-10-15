using UnityEngine;

// Values that define our map
public class MapGenerator : MonoBehaviour
{
    public DrawMode drawMode;

    const int mapChunkSize = 2521;
    [Range(0, 7)]
    public int levelOfDetail = 7;
    public float noiseScale;
    public int octaves;
    [Range(0, 1)]
    public float persistance; // Usually equals 0.5
    public float lacunarity; // Usually equals 2
    public int seed;
    public Vector2 offset;

    public bool useFalloff;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public bool autoUpdate;
    public TerrainType[] terrainTypes;

    float[,] fallOffMap;

    void Awake()
    {
        fallOffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize, mapChunkSize);
    }

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        // Loop over the pixels and assign the terrain type color based on the height
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFalloff) noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - fallOffMap[x, y]);
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < terrainTypes.Length; i++)
                {
                    if (currentHeight <= terrainTypes[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = terrainTypes[i].color; // Colors 
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColorMap)
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail),
                            TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.FalloffMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize, mapChunkSize)));
    }

    // Called whenever any script variables changes in the inspector
    void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
        fallOffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize, mapChunkSize);
    }
}

// Serializable to make it visible in the Inspector
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height; // e.g. name = Water & height = 0.4 then [0,0.4] is the Water terrain
    public Color color;
}