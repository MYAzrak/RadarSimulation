using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.Mathematics;
using UnityEngine;

// Values that define our map
public class MapGenerator : MonoBehaviour
{
    public DrawMode drawMode;

    public int mapWidth;
    public int mapHeight;
    public float noiseScale;
    public int octaves;
    [Range(0, 1)]
    public float persistance; // Usually equals 0.5
    public float lacunarity; // Usually equals 2
    public int seed;
    public Vector2 offset;
    public bool autoUpdate;
    public TerrainType[] terrainTypes;
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

        // Loop over the pixels and assign the terrain type color based on the height
        Color[] colorMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < terrainTypes.Length; i++)
                {
                    if (currentHeight <= terrainTypes[i].height)
                    {
                        colorMap[y * mapWidth + x] = terrainTypes[i].color; // Colors 
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap) display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColorMap) display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        else if (drawMode == DrawMode.Mesh) display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap), TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
    }

    // Called whenever any script variables changes in the inspector
    void OnValidate()
    {
        if (mapWidth < 1) mapWidth = 1;
        if (mapHeight < 1) mapHeight = 1;
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
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