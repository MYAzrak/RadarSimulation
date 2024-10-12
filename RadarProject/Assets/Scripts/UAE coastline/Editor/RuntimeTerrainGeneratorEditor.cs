using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RuntimeTerrainGenerator))]
public class RuntimeTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector layout
        DrawDefaultInspector();

        // Add some space before the button
        GUILayout.Space(20);

        // Add a larger button to generate the terrain
        RuntimeTerrainGenerator generator = (RuntimeTerrainGenerator)target;
        if (GUILayout.Button("Generate Terrain", GUILayout.Height(40))) // Increase button height
        {
            generator.GenerateTerrain();  // Call the terrain generation method
        }

        // Add some space after the button
        GUILayout.Space(20);
    }
}
