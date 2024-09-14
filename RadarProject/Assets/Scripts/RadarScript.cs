using UnityEngine;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Collections;

public class RadarScript : MonoBehaviour
{
    public int radarID;
    [SerializeField] string path = "radar";

    [SerializeField] private int HeightRes = 2048;

    [SerializeField] private int WidthRes = 5;
    [Range(0.0f, 1f)] private float resolution = 0.5f;
    [Range(5f, 5000f)] public float MaxDistance = 100F;
    [Range(0.01f, 2f)] public float MinDistance = 0.5F;
    [Range(5.0f, 90f)] public float VerticalAngle = 30f;
    [HideInInspector] public Camera radarCamera;
    [Range(0.0f, 5f)] public float noise = 10.0f;
    [Range(200, 2000)] public int ImageRadius = 1000;

    [SerializeField] private Shader normalDepthShader;
    [Range(0.0f, 0.99f)] public float parallelThreshold = 0.45f; // Threshold for considering a surface parallel

    private RenderTexture radarTexture;
    private float currentRotation = 0f; // Track current rotation
    private GameObject cameraObject;
    private WebSocketServer server;

    [SerializeField] private ComputeShader radarComputeShader;
    private ComputeBuffer radarBuffer;
    public RenderTexture inputTexture;
    private int[] tempBuffer;

    public DebugSpoke debugSpoke;

    public int[,] radarPPI;

    void Start()
    {
        path += radarID;

        server = Server.serverInstance.server;
        server.AddWebSocketService<DataService>($"/{path}");

        radarPPI = new int[Mathf.RoundToInt(360 / resolution), ImageRadius];
        if (normalDepthShader == null)
        {
            normalDepthShader = Shader.Find("Custom/NormalDepthShader");
        }

        if (radarComputeShader == null)
        {
            radarComputeShader = (ComputeShader)Resources.Load("ProcessRadarData");
        }
        cameraObject = SpawnCameras("DepthCamera", WidthRes, HeightRes, VerticalAngle, RenderTextureFormat.ARGBFloat);
        radarCamera = cameraObject.GetComponent<Camera>();

        // Initialize compute shader resources
        inputTexture = new RenderTexture(WidthRes, HeightRes, 24, RenderTextureFormat.ARGBFloat);
        inputTexture.enableRandomWrite = true;
        inputTexture.Create();

        // Set up the radar camera
        radarCamera.targetTexture = inputTexture;
        radarCamera.depthTextureMode = DepthTextureMode.Depth;

        // Create a buffer to hold a single rotation
        radarBuffer = new ComputeBuffer(ImageRadius, sizeof(int));
        tempBuffer = new int[ImageRadius];

        StartCoroutine(ProcessRadar());
    }

    IEnumerator ProcessRadar()
    {

        int kernelIndex = radarComputeShader.FindKernel("ProcessRadarData");

        while (Application.isPlaying)
        {
            if (currentRotation == 0)
            {
                var task = CollectData();

                yield return new WaitUntil(() => task.IsCompleted);

                // If server is not listening then do not process the radar
                if (!server.IsListening)
                {
                    continue;
                }

                server.WebSocketServices[$"/{path}"].Sessions.Broadcast(task.Result);
                Debug.Log($"Sent Data: {task.Result}");
            }

            ProcessRadarDataGPU(kernelIndex);
            RotateCamera();

            yield return new WaitForFixedUpdate();
        }
    }

    async Task<string> CollectData()
    {
        var dataObject = new
        {
            id = radarID,
            timestamp = 55,
            range = MaxDistance,
            PPI = radarPPI,
        };

        JsonSerializer serializer = new();

        using StringWriter sw = new();
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            await Task.Run(() => serializer.Serialize(writer, dataObject));
        }

        return sw.ToString();
    }

    void OnApplicationQuit()
    {
        server.Stop();
        Debug.Log("Stopped Server");
    }

    private GameObject SpawnCameras(string name, int Width, int Height, float verticalAngle, RenderTextureFormat format)
    {
        GameObject CameraObject = new GameObject();
        CameraObject.name = name;
        CameraObject.transform.SetParent(transform);
        CameraObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        CameraObject.transform.localPosition = new Vector3(0, 1, 0);
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


    private void ProcessRadarDataGPU(int kernelIndex)
    {
        // Clear the buffer for the new rotation
        radarBuffer.SetData(new int[ImageRadius]);

        // Render the camera to the input texture
        radarCamera.targetTexture = inputTexture;
        radarCamera.Render();

        // Set compute shader parameters
        radarComputeShader.SetTexture(kernelIndex, "InputTexture", inputTexture);
        radarComputeShader.SetBuffer(kernelIndex, "RadarBuffer", radarBuffer);
        radarComputeShader.SetFloat("MaxDistance", MaxDistance);
        radarComputeShader.SetFloat("MinDistance", MinDistance);
        radarComputeShader.SetFloat("ParallelThreshold", parallelThreshold);
        radarComputeShader.SetFloat("Noise", noise);
        radarComputeShader.SetFloat("Resolution", resolution);
        radarComputeShader.SetInt("ImageRadius", ImageRadius);
        radarComputeShader.SetFloat("CurrentRotation", currentRotation);

        // Dispatch the compute shader
        int threadGroupsX = Mathf.CeilToInt(WidthRes / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(HeightRes / 8.0f);
        radarComputeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

        // Read back the results into the temporary buffer
        radarBuffer.GetData(tempBuffer);

        // Copy the data back into the 2D radarPPI array for the current rotation
        int rotationIndex = Mathf.RoundToInt(currentRotation / resolution);

        for (int i = 0; i < ImageRadius; i++)
        {
            radarPPI[rotationIndex, i] = tempBuffer[i];
        }

    }

    private void UpdateDebugSpoke()
    {
        debugSpoke.tex.Reinitialize(1, Mathf.RoundToInt(MaxDistance), TextureFormat.RGB24, false);
        int rotationIndex = Mathf.RoundToInt(currentRotation / resolution);
        for (int i = 0; i < ImageRadius; i++)
        {
            if (radarPPI[rotationIndex, i] > 0)
            {
                debugSpoke.tex.SetPixel(0, i, Color.red);
            }
        }
        debugSpoke.tex.Apply();
    }

    void OnDestroy()
    {
        if (radarBuffer != null)
        {
            radarBuffer.Release();
        }
        if (inputTexture != null)
        {
            inputTexture.Release();
        }
    }
    private void RotateCamera()
    {
        currentRotation += resolution; // Increase rotation by 1 degree
        if (currentRotation >= 360f) currentRotation = 0f; // Wrap around at 360 degrees
        cameraObject.transform.localRotation = Quaternion.Euler(0, currentRotation, 0);
    }

}
