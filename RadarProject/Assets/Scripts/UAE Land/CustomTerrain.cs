using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour
{
    
    public Vector2 randomHeightRange = new Vector2(0.0f, 0.1f);
    public Texture2D heightMapImage;
    public Vector3 heightMapScale = new Vector3(1.0f, 1.0f, 1.0f);

    public Terrain terrain;
    public TerrainData terrainData;

    public void RandomTerrain()
    {

        int hmr = terrainData.heightmapResolution;
        float[,] heightMap = terrainData.GetHeights(0, 0, hmr, hmr);

        for (int x = 0; x < hmr; ++x)
        {
            for (int z = 0; z < hmr; ++z)
            {

                heightMap[x, z] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void LoadTexture()
    {

        int hmr = terrainData.heightmapResolution;
        float[,] heightMap = new float[hmr, hmr]; //terrainData.GetHeights(0, 0, hmr, hmr);

        for (int x = 0; x < hmr; ++x)
        {
            for (int z = 0; z < hmr; ++z)
            {

                heightMap[x, z] += heightMapImage.GetPixel((int)(x * heightMapScale.x),
                    (int)(z * heightMapScale.z)).grayscale * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void ResetTerrain()
    {

        int hmr = terrainData.heightmapResolution;
        float[,] heightMap = new float[hmr, hmr];
        //for (int x = 0; x < hmr; ++x) {

        //    for (int z = 0; z < hmr; ++z) {

        //        heightMap[x, z] = 0.0f;
        //    }
        //}
        terrainData.SetHeights(0, 0, heightMap);
    }

    private void OnEnable()
    {

        Debug.Log("Initialising Tertain Data");
        terrain = this.GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData;
    }

    public enum TagType { Tag, Layer };
    [SerializeField]
    int terrainLayer = 0;

    private void Start()
    {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTag(tagsProp, "Terrain", TagType.Tag);
        AddTag(tagsProp, "Cloud", TagType.Tag);
        AddTag(tagsProp, "Shore", TagType.Tag);

        // Apply tag changes to tag database
        tagManager.ApplyModifiedProperties();

        SerializedProperty layerProp = tagManager.FindProperty("layers");
        terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
        tagManager.ApplyModifiedProperties();

        // Tag this object
        this.gameObject.tag = "Terrain";
        this.gameObject.layer = terrainLayer;
    }

    int AddTag(SerializedProperty tagsProp, string newTag, TagType tType)
    {

        bool found = false;

        // Ensure the tag doesn't already exist
        for (int i = 0; i < tagsProp.arraySize; ++i)
        {

            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag))
            {

                found = true;
                return i;
            }
        }

        // Add your new tag
        if (!found && tType == TagType.Tag)
        {

            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }

        // Add new layer
        else if (!found && tType == TagType.Layer)
        {

            for (int j = 8; j < tagsProp.arraySize; ++j)
            {

                SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);
                // Add layer in next slot
                if (newLayer.stringValue == "")
                {

                    Debug.Log("Adding New Layer: " + newTag);
                    newLayer.stringValue = newTag;
                    return j;
                }
            }
        }
        return -1;
    }
}
