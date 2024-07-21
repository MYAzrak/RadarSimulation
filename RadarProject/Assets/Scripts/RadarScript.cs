using UnityEngine;

public class RadarScript : MonoBehaviour
{
    [SerializeField] private int HeightRes = 2048;
    [SerializeField] private int WidthRes = 5;
    [Range(0.0f, 15f)] private float resolution = 1f;
    [Range(5f, 5000f)] public float MaxDistance = 100F;
    [Range(0.01f, 2f)] public float MinDistance = 0.1F;
    [Range(5.0f, 90f)] public float VerticalAngle = 30f;
    public string RadarLayer = "Radar";
    [HideInInspector] public Camera radarCamera;

    [SerializeField] private Shader normalDepthShader;
    [SerializeField] private float parallelThreshold = 0.8f; // Threshold for considering a surface parallel

    private RenderTexture radarTexture;
    private float currentRotation = 0f; // Track current rotation
    private GameObject cameraObject;

    public DebugSpoke debugSpoke;

    void Start()
    {
        if (normalDepthShader == null)
        {
            normalDepthShader = Shader.Find("Custom/NormalDepthShader");
        }
        cameraObject = SpawnCameras("DepthCamera", WidthRes, HeightRes, VerticalAngle, RenderTextureFormat.ARGBFloat);
        radarCamera = cameraObject.GetComponent<Camera>();
        ProcessRadarData();
    }

    void Update()
    {
        ProcessRadarData();
        // RotateCamera();
    }

    private GameObject SpawnCameras(string name, int Width, int Height, float verticalAngle, RenderTextureFormat format)
    {
        GameObject CameraObject = new GameObject();
        CameraObject.name = name;
        CameraObject.transform.SetParent(transform);
        CameraObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        CameraObject.transform.localPosition = new Vector3(0, 0, 0);
        CameraObject.AddComponent<Camera>();
        Camera cam = CameraObject.GetComponent<Camera>();

        radarTexture = new RenderTexture(Width, 5, 32, format);
        cam.targetTexture = radarTexture;

        cam.usePhysicalProperties = false;
        cam.aspect = (1) / verticalAngle;
        cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(1, cam.aspect);
        cam.farClipPlane = MaxDistance;
        cam.nearClipPlane = MinDistance;
        cam.enabled = true;

        // Set the custom shader
        cam.SetReplacementShader(normalDepthShader, "");

        return CameraObject;
    }

    private void ProcessRadarData()
    {

        if (debugSpoke != null)
        {
            debugSpoke.tex.Reinitialize(1, Mathf.RoundToInt(MaxDistance), TextureFormat.RGB24, false);
        }
        RenderTexture.active = radarTexture;
        Texture2D tex = new Texture2D(radarTexture.width, radarTexture.height, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(0, 0, radarTexture.width, radarTexture.height), 0, 0);
        tex.Apply();


        for (int x = 0; x < tex.width; x++)
        {
            for (int y = 0; y < tex.height; y++)
            {
                Color pixel = tex.GetPixel(x, y);
                Vector3 normal = new Vector3(pixel.r, pixel.g, pixel.b);
                float viewSpaceDepth = pixel.a;
                viewSpaceDepth = Mathf.Clamp(viewSpaceDepth, MinDistance, MaxDistance);

                int distance = Mathf.RoundToInt(viewSpaceDepth);

                // Check if the surface is parallel enough (blue channel > threshold)
                if (normal.z > parallelThreshold)
                {
                    // This is a radar detection
                    // distance = Mathf.Clamp(distance, MinDistance, MaxDistance);
                    // Use this distance for your radar logic
                    // For example, you could store it in an array or use it to spawn objects
                    // Debug.Log($"Radar detection at ({x}, {y}) with distance {distance}, rgb {normal.x}, {normal.y}, {normal.z}");
                    if (debugSpoke != null)
                    {
                    
                        debugSpoke.tex.SetPixel(x, distance, Color.red);
                    }
                }
            }

        }
        if (debugSpoke != null)
        {
            debugSpoke.tex.Apply();
        }

        Destroy(tex);
    }

    private void RotateCamera()
    {
        currentRotation += 1f; // Increase rotation by 1 degree
        if (currentRotation >= 360f) currentRotation -= 360f; // Wrap around at 360 degrees
        cameraObject.transform.localRotation = Quaternion.Euler(0, currentRotation, 0);
    }
}
