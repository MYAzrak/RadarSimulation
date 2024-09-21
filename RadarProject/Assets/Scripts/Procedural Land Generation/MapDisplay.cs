using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Takes the noiseMap and returns it into a texture then applies that texture into a plane in out scene
public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer; // A ref to the renderer of the plane we created
    public MeshFilter meshFilter; // Link b/2 Mesh and Renderer
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture)
    {
        // textureRenderer.material // instantiated at runtime
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height); // Set the size of the plane = size of the map
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh(); // Shared since we could generate the mesh outside game mode
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
