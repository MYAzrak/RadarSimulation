using UnityEngine;

public class ReflectivityComponent : MonoBehaviour
{
    public float reflectivity = 1.0f;
    private int objectId;

    void Start()
    {
        objectId = gameObject.GetInstanceID();
        ReflectivityManager.Instance.RegisterReflectivity(objectId, reflectivity);
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ReflectivityManager.Instance.RegisterReflectivity(objectId, reflectivity);
        }
    }

    void OnDestroy()
    {
        ReflectivityManager.Instance.RemoveReflectivity(objectId);
    }
}
