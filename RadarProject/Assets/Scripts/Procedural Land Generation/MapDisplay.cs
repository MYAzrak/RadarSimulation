using UnityEngine;

// Takes the noiseMap and returns it into a texture then applies that texture into a plane in our scene
public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer; // A ref to the renderer of the plane we created
    public MeshFilter meshFilter; // Link b/w Mesh and Renderer
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public void DrawTexture(Texture2D texture)
    {
        // textureRenderer.material // instantiated at runtime
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height); // Set the size of the plane = size of the map
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        Mesh mesh = meshData.CreateMesh();
        meshFilter.sharedMesh = mesh; // Shared since we could generate the mesh outside game mode
        meshRenderer.sharedMaterial.mainTexture = texture;
        
        // Add a box collider around the land to trigger events
        BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
        boxCollider.size = mesh.bounds.size * 9.5f;
        boxCollider.center = mesh.bounds.center;
    }
}
