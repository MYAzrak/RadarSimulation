using UnityEngine;
using UnityEditor;
using EditorGUITable;

[CustomEditor(typeof(CustomTerrain))]
[CanEditMultipleObjects]

public class CustomTerrainEditor : Editor
{

    // Properties
    SerializedProperty randomHeightRange;
    SerializedProperty heightMapScale;
    SerializedProperty heightMapImage;

    // Fold Outs
    bool showRandom = false;
    bool showLoadHeights = false;

    private void OnEnable()
    {

        randomHeightRange = serializedObject.FindProperty("randomHeightRange");
        heightMapScale = serializedObject.FindProperty("heightMapScale");
        heightMapImage = serializedObject.FindProperty("heightMapImage");
    }

    public override void OnInspectorGUI()
    {

        serializedObject.Update();

        CustomTerrain terrain = (CustomTerrain)target;

        showRandom = EditorGUILayout.Foldout(showRandom, "Random");
        if (showRandom)
        {

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Set Heights Between Random Values", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(randomHeightRange);
            if (GUILayout.Button("Random Heights"))
            {

                terrain.RandomTerrain();
            }
        }

        showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");
        if (showLoadHeights)
        {

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(heightMapImage);
            EditorGUILayout.PropertyField(heightMapScale);
            if (GUILayout.Button("Load Texture"))
            {

                terrain.LoadTexture();
            }
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (GUILayout.Button("Reset Terrain"))
        {

            terrain.ResetTerrain();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
