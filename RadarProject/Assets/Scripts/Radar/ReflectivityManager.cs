using System.Collections.Generic;
using UnityEngine;

public struct ReflectivityData
{
  public int objectId;
  public float reflectivity;
}

public class ReflectivityManager : MonoBehaviour
{
  private static ReflectivityManager _instance;
  public static ReflectivityManager Instance
  {
    get
    {
      if (_instance == null)
      {
        _instance = FindObjectOfType<ReflectivityManager>();
        if (_instance == null)
        {
          GameObject go = new GameObject("ReflectivityManager");
          _instance = go.AddComponent<ReflectivityManager>();
        }
      }
      return _instance;
    }
  }

  private Dictionary<int, float> reflectivityMap = new Dictionary<int, float>();

  public void RegisterReflectivity(int objectId, float reflectivity)
  {
    reflectivityMap[objectId] = reflectivity;
  }

  public float GetReflectivity(int objectId)
  {
    if (reflectivityMap.TryGetValue(objectId, out float reflectivity))
    {
      return reflectivity;
    }
    return 1.0f; // Default reflectivity if not found
  }

  public ReflectivityData[] GetReflectivityDataArray()
  {
    ReflectivityData[] dataArray = new ReflectivityData[reflectivityMap.Count];
    int index = 0;
    foreach (var kvp in reflectivityMap)
    {
      dataArray[index++] = new ReflectivityData { objectId = kvp.Key, reflectivity = kvp.Value };
    }
    return dataArray;
  }
}
