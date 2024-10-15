using System;
using Crest;
using UnityEngine;

public class WeatherController : MonoBehaviour
{
    public GameObject currentWeather;

    CameraController cameraController;
    Transform cameraTransform;
    MainMenuController mainMenuController;

    // Start is called before the first frame update
    void Start()
    {
        cameraController = FindObjectOfType<CameraController>();
        
        GameObject camera = GameObject.FindWithTag("MainCamera");
        if (camera == null)
        {
            Logger.Log("Main camera not present in the scene");
        }
        cameraTransform = camera.transform;

        mainMenuController = FindObjectOfType<MainMenuController>();

        // Set default wave
        currentWeather = null;
    }

    public void GenerateWeather(
        Weather scenarioWeather,
        GameObject prefab,
        Material skybox,
        Material oceanMaterial
        )
    {
        ClearWeather();

        if (skybox == null || oceanMaterial == null)
            return;

        RenderSettings.skybox = skybox;
        OceanRenderer.Instance.OceanMaterial = oceanMaterial;
        mainMenuController.SetWeatherLabel(scenarioWeather.ToString());

        if (scenarioWeather != Weather.Clear && prefab != null)
        {
            currentWeather = Instantiate(prefab, prefab.transform.position + cameraTransform.position, Quaternion.identity);
            cameraController.SetWeatherOverCamera(currentWeather);
        }
    }

    public void ClearWeather()
    {
        if (currentWeather != null)
            Destroy(currentWeather);
    }
}
