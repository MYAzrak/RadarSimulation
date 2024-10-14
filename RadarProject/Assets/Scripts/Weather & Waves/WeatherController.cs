using Crest;
using UnityEngine;

public class WeatherController : MonoBehaviour
{
    public GameObject currentWeather;

    ScenarioController scenarioController;
    CameraController cameraController;
    MainMenuController mainMenuController;

    // Start is called before the first frame update
    void Start()
    {
        scenarioController = FindObjectOfType<ScenarioController>();
        cameraController = FindObjectOfType<CameraController>();
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

        if (scenarioWeather == Weather.Clear)
        {
            RenderSettings.skybox = skybox;
            OceanRenderer.Instance.OceanMaterial = oceanMaterial;
            mainMenuController.SetWeatherLabel(scenarioWeather.ToString());
            return;
        }

        if (prefab != null && skybox != null)
        {
            currentWeather = Instantiate(prefab, prefab.transform.position + cameraController.GetTransformPosition(), Quaternion.identity);
            cameraController.SetWeatherOverCamera(currentWeather);
            mainMenuController.SetWeatherLabel(scenarioWeather.ToString());
            RenderSettings.skybox = skybox;
            OceanRenderer.Instance.OceanMaterial = oceanMaterial;
        }
    }

    public void ClearWeather()
    {
        if (currentWeather != null)
            Destroy(currentWeather);
    }
}
