using UnityEngine;
using System;
using System.Linq;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class RadarScript : MonoBehaviour
{
    public int radarID;
    [SerializeField] string path = "radar";

    [SerializeField] public int HeightRes = 1024;
    [SerializeField] public int WidthRes = 10;
    [Range(0.0f, 1f)] private float resolution = 0.5f;
    [Range(5f, 5000f)] public float MaxDistance = 100F;
    [Range(0.01f, 2f)] public float MinDistance = 0.5F;
    [Range(5.0f, 90f)] public float VerticalAngle = 30f;
    [Range(0.5f, 50f)] public float BeamWidth = 2f;
    [HideInInspector] public Camera radarCamera;
    [Range(0.0f, 5f)] public float noise = 10.0f;
    [Range(200, 2000)] public int ImageRadius = 1000;

    [SerializeField] private Shader normalDepthShader;
    [Range(0.0f, 0.99f)] public float parallelThreshold = 0.45f; // Threshold for considering a surface parallel

    private RenderTexture radarTexture;
    private float currentRotation = 1f; // Track current rotation
    private GameObject cameraObject;
    private WebSocketServer server;

    [SerializeField] private ComputeShader radarComputeShader;
    private ComputeBuffer radarBuffer;
    private RenderTexture inputTexture;
    private int[] tempBuffer;

    [Header("Radar Equation Parameters")]
    public float transmittedPowerW = 1000f; // Watts
    public float antennaGainDBi = 30f; // dBi
    public float wavelengthM = 0.03f; // meters (for 10 GHz)
    public float systemLossesDB = 3f; // dB

    private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

    private ComputeBuffer rcsBuffer;
    private ComputeBuffer depthNormalBuffer;
    private RenderTexture depthNormalTexture;
    private ComputeShader rcsComputeShader;

    [SerializeField] private LayerMask radarLayerMask;
    [SerializeField] private float defaultReflectivity = 1f;

    public int[,] radarPPI;
    private ScenarioController scenario;

    [Header("Rain Simulation")]
    [Range(0f, 1f)] public float RainProbability = 0.1f;
    public float RainRCS = 0.001f; // Typical RCS for rain
    [Range(1, 100)] public int RainIntensity = 10; // Number of rain drops per angle

    private ComputeBuffer rainBuffer;
    private int[] tempRainBuffer;

    void Start()
    {
        path += radarID;

        server = Server.serverInstance.server;

        scenario = FindObjectOfType<ScenarioController>();

        // If service not found then add it 
        // Allows us to reuse it for different radars when loading and unloading them
        if (!server.WebSocketServices.TryGetServiceHost($"/{path}", out _))
        {
            server.AddWebSocketService<DataService>($"/{path}");
        }

        radarPPI = new int[Mathf.RoundToInt(360 / resolution), ImageRadius];
        if (normalDepthShader == null)
        {
            normalDepthShader = Shader.Find("Custom/NormalDepthShader");
        }

        if (radarComputeShader == null)
        {
            radarComputeShader = (ComputeShader)Resources.Load("ProcessRadarData");
        }
        cameraObject = SpawnCameras("DepthCamera", WidthRes, HeightRes, VerticalAngle, BeamWidth, RenderTextureFormat.ARGBFloat);
        radarCamera = cameraObject.GetComponent<Camera>();

        // Initialize compute shader resources
        inputTexture = new RenderTexture(WidthRes, HeightRes, 24, RenderTextureFormat.ARGBFloat);
        inputTexture.enableRandomWrite = true;
        inputTexture.Create();


        // Set up the radar camera
        radarCamera.targetTexture = inputTexture;
        // radarCamera.depthTextureMode = DepthTextureMode.Depth;

        // Create a buffer to hold a single rotation
        radarBuffer = new ComputeBuffer(ImageRadius, sizeof(int));
        tempBuffer = new int[ImageRadius];

        depthNormalTexture = new RenderTexture(WidthRes, HeightRes, 0, RenderTextureFormat.ARGBFloat);
        depthNormalTexture.enableRandomWrite = true;
        depthNormalTexture.Create();

        depthNormalBuffer = new ComputeBuffer(WidthRes * HeightRes, sizeof(float) * 4);
        rcsBuffer = new ComputeBuffer(WidthRes * HeightRes, sizeof(float));

        // Set up the radar camera to render to the depth-normal texture
        // radarCamera.targetTexture = depthNormalTexture;
        radarCamera.depthTextureMode = DepthTextureMode.Depth;

        rainBuffer = new ComputeBuffer(ImageRadius, sizeof(int));
        tempRainBuffer = new int[ImageRadius];

        rcsComputeShader = Resources.Load<ComputeShader>("RCSCalculation");
        StartCoroutine(ProcessRadar());
    }
    void Update()
    {
        // Execute any queued actions on the main thread
        while (_mainThreadActions.TryDequeue(out var action))
        {
            action();
        }
    }


    void UpdateRCSArray()
    {
        // Render the depth-normal texture
        // radarCamera.depthTextureMode = DepthTextureMode.DepthNormals;
        // radarCamera.targetTexture = depthNormalTexture;
        radarCamera.Render();

        // Copy the depth-normal texture to the buffer
        ComputeShader copyShader = Resources.Load<ComputeShader>("CopyTextureToBuffer");
        int copyKernel = copyShader.FindKernel("CSMain");
        copyShader.SetTexture(copyKernel, "InputTexture", depthNormalTexture);
        copyShader.SetBuffer(copyKernel, "OutputBuffer", depthNormalBuffer);

        int dispatchX = Mathf.Max(1, WidthRes / 8);
        int dispatchY = Mathf.Max(1, HeightRes / 8);
        copyShader.Dispatch(copyKernel, dispatchX, dispatchY, 1);

        // Calculate RCS using compute shader
        int rcsKernel = rcsComputeShader.FindKernel("CSMain");
        rcsComputeShader.SetBuffer(rcsKernel, "DepthNormalBuffer", depthNormalBuffer);
        rcsComputeShader.SetBuffer(rcsKernel, "RCSBuffer", rcsBuffer);
        rcsComputeShader.SetMatrix("InverseViewProjectionMatrix", radarCamera.projectionMatrix.inverse * radarCamera.worldToCameraMatrix.inverse);
        rcsComputeShader.SetVector("CameraPosition", radarCamera.transform.position);
        rcsComputeShader.SetFloat("MaxDistance", MaxDistance);
        rcsComputeShader.SetFloat("DefaultReflectivity", defaultReflectivity);
        rcsComputeShader.SetInt("Width", WidthRes);
        rcsComputeShader.SetInt("Height", HeightRes);

        rcsComputeShader.Dispatch(rcsKernel, dispatchX, dispatchY, 1);
    }
    float GetReflectivity(GameObject obj)
    {
        //TODO: Get reflectivity from object property
        return defaultReflectivity;
    }

    IEnumerator ProcessRadar()
    {

        int kernelIndex = radarComputeShader.FindKernel("ProcessRadarData");
        int rainKernelIndex = radarComputeShader.FindKernel("GenerateRain");

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

            UpdateRCSArray();
            ProcessRadarDataGPU(kernelIndex);
            GenerateRainGPU(rainKernelIndex);
            CombineRadarAndRain();

            RotateCamera();

            yield return new WaitForFixedUpdate();
        }
    }

    async Task<string> CollectData()
    {
        Vector3 radarPosition = Vector3.zero;
        List<ShipData> shipDataList = new List<ShipData>();

        // Queue the transform access to be executed on the main thread
        _mainThreadActions.Enqueue(() =>
        {
            radarPosition = cameraObject.transform.position;
            shipDataList = scenario.generatedShips.Select(ship => new ShipData
            {
                Id = ship.GetInstanceID(),
                Position = ship.transform.position
            }).ToList();
        });

        // Wait for a frame to ensure the queued action is processed
        await Task.Yield();

        var dataObject = new
        {
            id = radarID,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            range = MaxDistance,
            PPI = radarPPI,
            ships = shipDataList,
            radarLocation = radarPosition
        };

        JsonSerializer serializer = new JsonSerializer();
        serializer.Converters.Add(new Vector3Converter());

        using StringWriter sw = new StringWriter();
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            await Task.Run(() => serializer.Serialize(writer, dataObject));
        }

        return sw.ToString();
    }

    private GameObject SpawnCameras(string name, int Width, int Height, float verticalAngle, float beamWidth, RenderTextureFormat format)
    {
        GameObject CameraObject = new GameObject();
        CameraObject.name = name;
        CameraObject.transform.SetParent(transform);
        CameraObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        CameraObject.transform.localPosition = new Vector3(0, 0.1f, 0);
        CameraObject.AddComponent<Camera>();
        Camera cam = CameraObject.GetComponent<Camera>();

        radarTexture = new RenderTexture(Width, Height, 32, format);
        cam.targetTexture = radarTexture;

        cam.usePhysicalProperties = false;

        // Set the vertical field of view to the specified vertical angle
        cam.fieldOfView = verticalAngle;

        // Calculate the horizontal field of view based on the beam width
        float horizontalFOV = beamWidth;

        // Set the aspect ratio to maintain the desired horizontal FOV
        cam.aspect = Mathf.Tan(horizontalFOV * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(verticalAngle * 0.5f * Mathf.Deg2Rad);

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
        tempBuffer = new int[ImageRadius];

        // Render the camera to the input texture
        // radarCamera.depthTextureMode = DepthTextureMode.Depth;
        // radarCamera.targetTexture = inputTexture;
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
        radarComputeShader.SetFloat("TransmittedPower", transmittedPowerW);
        radarComputeShader.SetFloat("AntennaGain", antennaGainDBi);
        radarComputeShader.SetFloat("Wavelength", wavelengthM);
        radarComputeShader.SetFloat("SystemLosses", systemLossesDB);
        radarComputeShader.SetBuffer(kernelIndex, "RCSBuffer", rcsBuffer);

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
    private void GenerateRainGPU(int kernelIndex)
    {
        // Clear the rain buffer for the new rotation
        rainBuffer.SetData(new int[ImageRadius]);
        tempRainBuffer = new int[ImageRadius];

        // Set compute shader parameters for rain
        radarComputeShader.SetBuffer(kernelIndex, "RainBuffer", rainBuffer);
        radarComputeShader.SetFloat("RainProbability", RainProbability);
        radarComputeShader.SetFloat("RainRCS", RainRCS);
        radarComputeShader.SetInt("RainIntensity", RainIntensity);
        radarComputeShader.SetFloat("MaxDistance", MaxDistance);
        radarComputeShader.SetInt("ImageRadius", ImageRadius);
        radarComputeShader.SetFloat("CurrentRotation", currentRotation);

        // Generate random seed for rain simulation
        uint randomSeed = (uint)System.DateTime.Now.Ticks;
        radarComputeShader.SetInt("RandomSeed", (int)randomSeed);

        // Dispatch the compute shader for rain generation
        radarComputeShader.Dispatch(kernelIndex, 1, 1, 1);

        // Read back the results into the temporary rain buffer
        rainBuffer.GetData(tempRainBuffer);
    }

    private void CombineRadarAndRain()
    {
        int rotationIndex = Mathf.RoundToInt(currentRotation / resolution);

        for (int i = 0; i < ImageRadius; i++)
        {
            radarPPI[rotationIndex, i] = tempBuffer[i] + tempRainBuffer[i];
        }
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
        if (rcsBuffer != null)
        {
            rcsBuffer.Release();
        }
        if (rainBuffer != null)
        {
            rainBuffer.Release();
        }
    }
    private void RotateCamera()
    {
        currentRotation += resolution; // Increase rotation by 1 degree
        if (currentRotation >= 360f) currentRotation = 0f; // Wrap around at 360 degrees
        cameraObject.transform.localRotation = Quaternion.Euler(0, currentRotation, 0);
    }

    public class ShipData
    {
        public int Id { get; set; }
        public Vector3 Position { get; set; }
    }
}
public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.y);
        writer.WritePropertyName("z");
        writer.WriteValue(value.z);
        writer.WriteEndObject();
    }
}
