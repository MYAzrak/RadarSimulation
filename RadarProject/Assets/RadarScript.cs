using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarScript : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private int HeightRes = 2048;
    [SerializeField] private int WidthRes = 5;
    [Range(0.0f, 15f)] private float resolution = 1f;
    [Range(5f, 5000f)] public float MaxDistance = 100F;
    [Range(0.01f, 2f)] public float MinDistance = 0.1F;
    [Range(5.0f, 90f)] public float VerticalAngle = 30f;

    public string RadarLayer = "Radar";

    [HideInInspector] public Camera radarCamera;
    void Start()
    {
        radarCamera = SpawnCameras("DepthCamera", WidthRes, HeightRes, VerticalAngle, RenderTextureFormat.Depth);

    }

    // Update is called once per frame
    void Update()
    {

    }
    private Camera SpawnCameras(string name, int Width, int Height, float verticalAngle, RenderTextureFormat format)
    {

        GameObject CameraObject = new GameObject();
        CameraObject.name = name;
        CameraObject.transform.SetParent(transform);
        CameraObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        CameraObject.transform.localPosition = new Vector3(0, 0, 0);

        CameraObject.AddComponent<Camera>();
        Camera cam = CameraObject.GetComponent<Camera>();

        if (cam.targetTexture == null)
        {
            cam.targetTexture = new RenderTexture(Width, Height, 32, format);
        }

        cam.usePhysicalProperties = false;
        cam.aspect = (1) / verticalAngle;
        cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(1, cam.aspect);
        cam.farClipPlane = MaxDistance;
        cam.nearClipPlane = MinDistance;
        cam.enabled = true;
        cam.cullingMask = LayerMask.GetMask(RadarLayer);
        return cam;
    }
}
