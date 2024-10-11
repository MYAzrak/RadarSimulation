#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorUI : Editor
{
    public override async void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target; // Ref to our map generator

        // Auto updates w/o clicking the "Generate" btn
        if (DrawDefaultInspector()) // If any value changed
        {
            if (mapGen.autoUpdate) await mapGen.GenerateMapAsync();
        }

        if (GUILayout.Button("Generate"))
        {
            await mapGen.GenerateMapAsync();
        }
    }
}
#endif
