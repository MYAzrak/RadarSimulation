using System.Collections;
using System.Collections.Generic;
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
    ProcTerrainController procTerrainController;

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

        procTerrainController = Object.FindObjectOfType<ProcTerrainController>();
        Assert.IsNotNull(procTerrainController, "ProcTerrainController not found in scene.");

        filePath = Path.Combine(CSVController.GetFilePath(), "Tests/");

        if (Directory.Exists(filePath)) 
            Directory.Delete(filePath, true);

        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);
    }

    [UnityTest]
    public IEnumerator LoadScenarioTest()
    {
        // Test calm waves and clear weather
        string test1FileName = "TestScenario4";
        GenerateTestScenario(test1FileName, false, Weather.Clear, Waves.Calm);
        scenarioController.LoadScenario(filePath + test1FileName);

        yield return new WaitForSeconds(1f);

        // Check ships were created as required
        Assert.Greater(scenarioController.shipsInformation.Count, 0, "Not all ships were created.");
        Assert.AreEqual(scenarioController.shipsInformation.Count, scenarioController.generatedShips.Count, "Not all ships were created.");

        // Get the ship's locations and speeds
        List<Vector3> shipLocations = scenarioController.generatedShips[0].GetComponent<ShipController>().locationsToVisit;
        List<float> shipSpeedAtEachLocation = scenarioController.generatedShips[0].GetComponent<ShipController>().speedAtEachLocation;

        // Get the ship's coordinates 
        List<ShipCoordinates> expectedCoordinates = scenarioController.shipLocations[1]; // Ship ID 1

        List<Vector3> expectedLocations = new();
        for (int i = 1; i < expectedCoordinates.Count; i++)
        {
            expectedLocations.Add(new Vector3(expectedCoordinates[i].x_coordinates, 0, expectedCoordinates[i].z_coordinates));
        }

        float xGenerated = scenarioController.generatedShips[0].transform.position.x;
        float zGenerated = scenarioController.generatedShips[0].transform.position.z;
        
        // Check ship spawned in first location
        // Checking with tolerance due to physics and floating point errors
        float tolerance = 5f;
        Vector3 expected = new(expectedCoordinates[0].x_coordinates, 0, expectedCoordinates[0].z_coordinates);
        Vector3 actual = new(xGenerated, 0, zGenerated);
        Assert.IsTrue(
            Mathf.Abs(expected.x - actual.x) < tolerance &&
            Mathf.Abs(expected.y - actual.y) < tolerance &&
            Mathf.Abs(expected.z - actual.z) < tolerance,
            $"Expected: {expected}, but was: {actual}"
        );
        Assert.AreEqual(expectedLocations, shipLocations); // Check the remaining locations to visit
        
        // Get the ship's speed
        List<float> expectedSpeeds = new();

        // First ship speed is ignored since it is the starting position of the ship
        for (int i = 1; i < expectedCoordinates.Count; i++)
        {
            expectedSpeeds.Add(expectedCoordinates[i].speed);
        }

        Assert.AreEqual(expectedSpeeds, shipSpeedAtEachLocation);
    }

    [UnityTest]
    public IEnumerator EndScenariosTest()
    {
        scenarioController.LoadAllScenarios(filePath);

        yield return null;
        
        scenarioController.EndAllScenarios();

        yield return null;

        Assert.AreEqual(scenarioController.generatedShips.Count, 0);
    }

    [UnityTest]
    public IEnumerator WeatherAndWavesTest()
    {
        yield return null;

        // Test calm waves and clear weather
        string test1FileName = "TestScenario1";
        GenerateTestScenario(test1FileName, false, Weather.Clear, Waves.Calm);
        scenarioController.LoadScenario(filePath + test1FileName);

        yield return null;

        // Check ships were created as required
        Assert.Greater(scenarioController.shipsInformation.Count, 0, "Not all ships were created.");
        Assert.AreEqual(scenarioController.shipsInformation.Count, scenarioController.generatedShips.Count, "Not all ships were created.");

        Assert.AreEqual(wavesController.currentWaveCondition, Waves.Calm, "Calm wave is not set");
        Assert.IsNull(weatherController.currentWeather, "Current weather should be clear (null)");

        scenarioController.EndScenario();
        yield return null;

        // Test moderate waves and light rain weather
        string test2FileName = "TestScenario2";
        GenerateTestScenario(test2FileName, false, Weather.LightRain, Waves.Moderate);
        scenarioController.LoadScenario(filePath + test2FileName);

        yield return null;

        Assert.AreEqual(wavesController.currentWaveCondition, Waves.Moderate, "Moderate waves was not created");
        Assert.AreEqual(weatherController.currentWeather.name, "Light Rain(Clone)", "Current weather should be light rain");

        scenarioController.EndScenario();
        yield return null;

        // Test heavy rain weather
        string test3FileName = "TestScenario3";
        GenerateTestScenario(test3FileName, false, Weather.HeavyRain, Waves.Calm);
        scenarioController.LoadScenario(filePath + test3FileName);

        yield return null;

        Assert.AreEqual(weatherController.currentWeather.name, "Heavy Rain(Clone)", "Current weather should be light rain");

        scenarioController.EndScenario();
        yield return null;
    }

    void GenerateTestScenario(string fileName, bool proceduralLand = false, Weather weather = Weather.Clear, Waves waves = Waves.Calm)
    {
        CSVController.GenerateRandomParameters();
        CSVController.numberOfShips = 5; // Small number of ships for testing
        CSVController.generateProceduralLand = new(new bool[] {proceduralLand});
        CSVController.weathers = new LoopArray<Weather> ( new Weather[] {weather} );
        CSVController.waves = new LoopArray<Waves> ( new Waves[] {waves} );
        CSVController.GenerateScenario(Path.Combine(filePath, fileName));
    }
}
