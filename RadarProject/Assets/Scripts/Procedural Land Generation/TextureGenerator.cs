using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class TextureGenerator
{
    // Create a texture out of a 1D color map
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; // To remove the blurriness 
        texture.wrapMode = TextureWrapMode.Clamp; // Instead of repeating the edges
        texture.SetPixels(colorMap); // Coloring all pixels at the same time is faster than one by one
        texture.Apply();
        return texture;
    }

    // Create a texture out of a 2D height map
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0); // First dimension
        int height = heightMap.GetLength(1); // Second dimension

        Color[] colorMap = new Color[width * height]; // 1D array of size width * height
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]); // noiseMap[x, y] is a % here
            }
        }
        return TextureFromColorMap(colorMap, width, height);
    }
}
