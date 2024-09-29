using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int mapWidth, int mapHeight)
    {
        float[,] falloffMap = new float[mapWidth, mapHeight];

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                float x = i / (float)mapWidth * 2 - 1; // [-1,1]
                float y = j / (float)mapHeight * 2 - 1; // [-1,1]

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)); // Which is closer to the edge
                falloffMap[i, j] = Evaluate(value);
            }
        }

        return falloffMap;
    }

    // Controls the effect of the falloff map
    static float Evaluate(float value)
    {
        float a = 3;
        float b = 10f; // Increase the black (more height) region 
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
