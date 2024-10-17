using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ScenarioControllerTests
{
    ScenarioController scenarioController;
    CSVController CSVController;
    WeatherController weatherController;
    WavesController wavesController;

    string filePath;

    [OneTimeSetUp]
    public void SetUp()
    {
        string sceneName = "OceanTests";
        
        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene("Assets/Scenes/OceanTests.unity");
        }

        scenarioController = Object.FindObjectOfType<ScenarioController>();
        Assert.IsNotNull(scenarioController, "ScenarioController not found in scene.");

        CSVController = Object.FindObjectOfType<CSVController>();
        Assert.IsNotNull(CSVController, "CSVController not found in scene.");

        weatherController = Object.FindObjectOfType<WeatherController>();
        Assert.IsNotNull(weatherController, "WeatherController not found in scene.");

        wavesController = Object.FindObjectOfType<WavesController>();
        Assert.IsNotNull(wavesController, "WavesController not found in scene.");

        filePath = Path.Combine(CSVController.GetFilePath(), "Tests");
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);
    }
    
    [UnityTest]
    public IEnumerator LoadScenarioTest()
    {
        yield return null;

        // Test calm waves and clear weather
        string test1FileName = "TestScenario1";
        GenerateTestScenario(test1FileName, false, Weather.Clear, Waves.Calm);
        scenarioController.LoadScenario(test1FileName);

        yield return null;

        // Check ships were created as required
        Assert.AreEqual(scenarioController.shipsInformation.Count, scenarioController.generatedShips.Count, "Not all ships were created.");

        Assert.AreEqual(wavesController.defaultWave.activeInHierarchy, true, "Calm wave is not set");

        Assert.IsNull(weatherController.currentWeather, "Current weather should be clear (null)");

        scenarioController.EndScenario();
        yield return null;

        // Test moderate waves and light rain weather
        string test2FileName = "TestScenario2";
        GenerateTestScenario(test2FileName, false, Weather.LightRain, Waves.Moderate);
        scenarioController.LoadScenario(test2FileName);

        yield return null;

        Assert.AreEqual(wavesController.currentWave.name, "WavesModerate(Clone)", "Moderate waves was not created");

        Assert.AreEqual(weatherController.currentWeather.name, "Light Rain(Clone)", "Current weather should be light rain");

        scenarioController.EndScenario();
        yield return null;

        // Test heavy rain weather
        string test3FileName = "TestScenario3";
        GenerateTestScenario(test3FileName, false, Weather.HeavyRain, Waves.Calm);
        scenarioController.LoadScenario(test3FileName);

        yield return null;

        Assert.AreEqual(weatherController.currentWeather.name, "Heavy Rain(Clone)", "Current weather should be light rain");

        scenarioController.EndScenario();
        yield return null;
    }

    void GenerateTestScenario(string fileName, bool proceduralLand = false, Weather weather = Weather.Clear, Waves waves = Waves.Calm)
    {
        CSVController.GenerateRandomParameters();
        CSVController.numberOfShips = 2; // Small number of ships for testing
        CSVController.hasProceduralLand = proceduralLand;
        CSVController.weather = weather;
        CSVController.waves = waves;
        CSVController.GenerateScenario(Path.Combine(filePath, fileName));
    }
}
