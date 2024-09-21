using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Takes the noiseMap and returns it into a texture then applies that texture into a plane in out scene
public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer; // A ref to the renderer of the plane we created

    public void DrawNoiseMap(float[,] noiseMap)
    {
        int width = noiseMap.GetLength(0); // First dimension
        int height = noiseMap.GetLength(1); // Second dimension

        Texture2D texture = new Texture2D(width, height);

        Color[] colorMap = new Color[width * height]; // 1D array of size width * height
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]); // noiseMap[x, y] is a % here
            }
        }

        // Coloring all pixels at the same time is faster than one by one
        texture.SetPixels(colorMap);
        texture.Apply();

        // textureRenderer.material // instantiated at runtime
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(width, 1, height); // Set the size of the plane = size of the map
    }
}
