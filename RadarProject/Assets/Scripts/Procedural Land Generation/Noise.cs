using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Static since 1 instance only, and no MonoBehaviour since we are not applying this 
// to any object in our scene
public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        if (scale <= 0) scale = 0.0001f; // Avoid div by zero

        // Go over each pixel and assign a value based on Perlin noise
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float sampleX = x / scale; // we do not want to use int values since perlin noise 
                float sampleY = y / scale; // will give us the same value each time

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = perlinValue;
            }
        }

        return noiseMap;
    }
}
