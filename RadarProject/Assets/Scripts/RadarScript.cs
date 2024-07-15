using UnityEngine;


public class RadarScript : MonoBehaviour
{
    [SerializeField] private int HeightRes = 500;
    [SerializeField] private int WidthRes = 500;
    // [Range(0.0f, 15f)] private float resolution = 1f;
    [Range(5f, 5000f)] public float MaxDistance = 100F;
    [Range(0.01f, 2f)] public float MinDistance = 0.1F;
    [Range(5.0f, 90f)] public float VerticalAngle = 60f;
    [Range(0.5f, 50f)] public float HoritontalAngle = 1f;

    public string RadarLayer = "Radar";
    public Shader normalDepthShader;

    [HideInInspector] public Camera radarCamera;

    void Start()
    {
        radarCamera = SpawnCameras("DepthCamera", WidthRes, HeightRes, VerticalAngle, RenderTextureFormat.ARGBFloat);
        if (normalDepthShader == null)
        {
            normalDepthShader = Shader.Find("Custom/NormalDepthShader");
        }
        radarCamera.SetReplacementShader(normalDepthShader, "RenderType");
    }
    void LateUpdate()
    {
        RenderTexture.active = radarCamera.targetTexture;
        Texture2D texture = new Texture2D(radarCamera.targetTexture.width, radarCamera.targetTexture.height, TextureFormat.RGBAFloat, false);
        texture.ReadPixels(new Rect(0, 0, radarCamera.targetTexture.width, radarCamera.targetTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        // Process the texture data...
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
        cam.aspect = HoritontalAngle / verticalAngle;
        cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(HoritontalAngle, cam.aspect);
        cam.farClipPlane = MaxDistance;
        cam.nearClipPlane = MinDistance;
        cam.enabled = true;
        // cam.cullingMask = LayerMask.GetMask(RadarLayer);
        Debug.Log("Created Camera");
        return cam;
    }
}

