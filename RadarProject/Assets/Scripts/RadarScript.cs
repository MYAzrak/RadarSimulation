using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarScript : MonoBehaviour
{

    [Range(3, 16)] public int NrOfCameras = 8;

    [Space]
    [Header("Radar Parameters")]
    public Texture2D RadiationPattern;
    public float AntennaGainDbi = 1;
    [SerializeField] private int HeightRes = 32;
    [SerializeField] private int WidthRes = 2048;
    [SerializeField] private int radarSweepResolution = 512;
    [SerializeField] private int radarSpokeResolution = 1024;
    public float PowerW = 20;
    public float RadarFrequencyGhz = 5.4f;
    [Range(5f, 5000f)] public float MaxDistance = 1000F;
    [Range(0.01f, 2f)] public float MinDistance = 0.1F;
    [Range(5.0f, 90f)] public float VerticalAngle = 30f;


    [Space(10)]
    [Header("Debugging Options")]
    public RenderTexture RadarPlotExternalImage;
    public RenderTexture RadarSpokeExternalImage;
    [Range(1f, 30000)] public float Sensitivity = 500f;
    [Range(0, 360)] public float SpokeAngle = 0;

    [HideInInspector] public Camera[] radarCameras;
    private RenderTexture RadarPlotImage;
    private RenderTexture RadarSpokeImage;
    // private NativeArray<int> RadarSpokesInt;
    // private NativeArray<float> RadarSpokesFloat;

    public string RadarLayer = "Radar";
    // Start is called before the first frame update
    void Start()
    {

        radarCameras = SpawnCameras("DepthCam", NrOfCameras, WidthRes, HeightRes, VerticalAngle, RenderTextureFormat.Depth);

    }

    // Update is called once per frame
    void Update()
    {

    }

    private Camera[] SpawnCameras(string name, int numbers, int Width, int Height, float verticalAngle, RenderTextureFormat format)
    {

        Camera[] Cameras = new Camera[numbers];
        for (int i = 0; i < numbers; i++)
        {
            GameObject CameraObject = new GameObject();
            CameraObject.name = name + i;
            CameraObject.transform.SetParent(transform);
            CameraObject.transform.localRotation = Quaternion.Euler(0, 180 + (1 / 2 + i) * 360.0f / numbers, 0);
            CameraObject.transform.localPosition = new Vector3(0, 0, 0);

            CameraObject.AddComponent<Camera>();
            Camera cam = CameraObject.GetComponent<Camera>();

            if (cam.targetTexture == null)
            {
                cam.targetTexture = new RenderTexture(Width, Height, 32, format);
            }

            cam.usePhysicalProperties = false;
            cam.aspect = (360.0f / numbers) / verticalAngle;
            cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(360.0f / numbers, cam.aspect);
            cam.farClipPlane = MaxDistance;
            cam.nearClipPlane = MinDistance;
            cam.enabled = false;
            cam.cullingMask = LayerMask.GetMask(RadarLayer);
            Cameras[i] = cam;
        }
        return Cameras;
    }
}
