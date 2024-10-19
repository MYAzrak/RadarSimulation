using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ProcTerrainControllerTests
{
    ProcTerrainController procTerrainController;
    GameObject terrainGeneratorPrefab;
    GameObject managerObject;

    [OneTimeSetUp]
    public void SetUp()
    {
        string sceneName = "OceanTests";

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene("Assets/Scenes/OceanTests.unity");
        }

        OnSceneLoaded();
    }

    private void OnSceneLoaded()
    {
        // Find the Manager object specifically
        managerObject = GameObject.Find("Manager");
        Assert.IsNotNull(managerObject, "Manager GameObject not found in scene.");

        // Get the ProcTerrainController from the Manager
        procTerrainController = managerObject.GetComponent<ProcTerrainController>();
        Assert.IsNotNull(procTerrainController, "ProcTerrainController not found on Manager GameObject.");

        // Get the reference to your existing prefab
        terrainGeneratorPrefab = procTerrainController.terrainGeneratorPrefab;
        Assert.IsNotNull(terrainGeneratorPrefab, "TerrainGenerator prefab not assigned in ProcTerrainController.");
    }

    [TearDown]
    public void TearDown()
    {
        if (procTerrainController != null && procTerrainController.terrainInstance != null)
        {
            procTerrainController.UnloadLandObjects();
        }
    }

    [UnityTest]
    public IEnumerator TerrainPositionTest()
    {
        // Wait for scene and setup to complete
        yield return new WaitForSeconds(0.5f);

        // Test initial position
        Vector3 testPosition = new(100f, 0f, 100f);
        procTerrainController.position = testPosition;

        // Generate terrain and wait for it to complete
        procTerrainController.GenerateTerrain();
        yield return new WaitForSeconds(0.1f);

        // Check terrain instance was created
        Assert.IsNotNull(procTerrainController.terrainInstance, "Terrain instance was not created");

        // Check main terrain position
        float tolerance = 0.1f;
        Vector3 expected = testPosition;
        Vector3 actual = procTerrainController.terrainInstance.transform.position;
        Assert.IsTrue(
            Mathf.Abs(expected.x - actual.x) < tolerance &&
            Mathf.Abs(expected.y - actual.y) < tolerance &&
            Mathf.Abs(expected.z - actual.z) < tolerance,
            $"Expected terrain position: {expected}, but was: {actual}"
        );
    }

    [UnityTest]
    public IEnumerator TerrainSeedTest()
    {
        // Wait for scene and setup to complete
        yield return new WaitForSeconds(0.5f);

        // Test first generation with specific seed
        int testSeed = 12345;
        procTerrainController.seed = testSeed;
        procTerrainController.GenerateTerrain();
        yield return new WaitForSeconds(0.1f);

        // Get the MapGenerator component
        MapGenerator firstMapGen = procTerrainController.terrainInstance.GetComponentInChildren<MapGenerator>();
        Assert.IsNotNull(firstMapGen, "MapGenerator component not found");
        Assert.AreEqual(testSeed, firstMapGen.seed, "Seed was not set correctly in MapGenerator");

        // Clean up first generation
        procTerrainController.UnloadLandObjects();
        yield return null;

        // Generate second terrain with same seed
        procTerrainController.GenerateTerrain();
        yield return new WaitForSeconds(0.1f);

        // Check second generation
        MapGenerator secondMapGen = procTerrainController.terrainInstance.GetComponentInChildren<MapGenerator>();
        Assert.IsNotNull(secondMapGen, "Second MapGenerator component not found");
        Assert.AreEqual(testSeed, secondMapGen.seed, "Seed changed in second generation");
    }
}