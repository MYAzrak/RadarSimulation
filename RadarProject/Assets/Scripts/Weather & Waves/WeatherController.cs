using Crest;
using UnityEngine;

public class WeatherController : MonoBehaviour
{
    public GameObject currentWeather;

    ScenarioController scenarioController;
    CameraController cameraController;

    // Start is called before the first frame update
    void Start()
    {
        scenarioController = FindObjectOfType<ScenarioController>();
        cameraController = FindObjectOfType<CameraController>();

        // Set default wave
        currentWeather = null;
    }
    
    public void GenerateWeather(Weather scenarioWeather)
    {
        if (scenarioWeather == Weather.Clear)
        {
            ClearWeather();
            return;
        }
            
        GameObject prefab = null;

        foreach (ScenarioController.WeatherPrefab weatherPrefab in scenarioController.weatherPrefabs)
        {
            if (weatherPrefab.weather == scenarioWeather)
            {
                prefab = weatherPrefab.prefab;
            }
        }

        ClearWeather();

        if (prefab != null)
        {
            currentWeather = Instantiate(prefab, prefab.transform.position + cameraController.GetTransformPosition(), Quaternion.identity);
            cameraController.SetWeatherOverCamera(currentWeather);
        }
    }

    public void ClearWeather()
    {
        if (currentWeather != null)
            Destroy(currentWeather);
    }
}
