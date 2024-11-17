using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement; // Import this for scene management
using UnityEngine.TestTools;

public class CoastlineKhorfakkanTest
{
    private GameObject terrainObject;
    private RuntimeTerrainGenerator terrainGenerator;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("KhorfakkanCoastline", LoadSceneMode.Single);

        terrainObject = GameObject.FindWithTag("Terrain");
        Assert.IsNotNull(terrainObject, "Terrain object with tag 'Terrain' not found.");
        terrainGenerator = terrainObject.GetComponent<RuntimeTerrainGenerator>();
        Assert.IsNotNull(terrainGenerator, "RuntimeTerrainGenerator component not found on Terrain object.");

        terrainGenerator.GenerateTerrain();
        yield return null;
    }

    [UnityTest]
    public IEnumerator TerrainGenerationTest()
    {
        yield return new WaitForSeconds(1f); // Wait for terrain generation to complete
        Assert.IsNotNull(terrainObject, "Terrain not generated.");
        Debug.Log("Terrain generated successfully.");

        // Validate terrain dimensions
        float generatedHeight = terrainObject.GetComponent<Terrain>().terrainData.size.y;
        float generatedWidth = terrainObject.GetComponent<Terrain>().terrainData.size.x;
        float generatedLength = terrainObject.GetComponent<Terrain>().terrainData.size.z;

        Debug.Log($"Expected Height: {terrainGenerator.heightScale}, Generated Height: {generatedHeight}");
        Debug.Log($"Expected Width: {terrainGenerator.terrainWidth}, Generated Width: {generatedWidth}");
        Debug.Log($"Expected Length: {terrainGenerator.terrainLength}, Generated Length: {generatedLength}");

        Assert.AreEqual(terrainGenerator.heightScale, generatedHeight, $"Expected height: {terrainGenerator.heightScale}, but got: {generatedHeight}.");
        Assert.AreEqual(terrainGenerator.terrainWidth, generatedWidth, $"Expected width: {terrainGenerator.terrainWidth}, but got: {generatedWidth}.");
        Assert.AreEqual(terrainGenerator.terrainLength, generatedLength, $"Expected length: {terrainGenerator.terrainLength}, but got: {generatedLength}.");

        Debug.Log("Terrain generation test passed with correct dimensions.");
        yield return null;
    }


    [UnityTest]
    public IEnumerator TreeSpawnTest()
    {
        int expectedTreeCount = terrainGenerator.numberOfTrees;
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
        Assert.AreEqual(expectedTreeCount, trees.Length, $"Expected {expectedTreeCount} trees but found {trees.Length}.");
        Debug.Log($"Tree spawn test passed. Expected {expectedTreeCount} trees but found {trees.Length}.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator BushSpawnTest()
    {
        int expectedBushCount = terrainGenerator.numberOfBushes;
        GameObject[] bushes = GameObject.FindGameObjectsWithTag("Bush");
        Assert.AreEqual(expectedBushCount, bushes.Length, $"Expected {expectedBushCount} bushes but found {bushes.Length}.");
        Debug.Log($"Bush spawn test passed. Expected {expectedBushCount} bushes but found {bushes.Length}.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator HouseSpawnTest()
    {
        int expectedHouseCount = terrainGenerator.numberOfHouses;
        GameObject[] houses = GameObject.FindGameObjectsWithTag("House");
        Assert.AreEqual(expectedHouseCount, houses.Length, $"Expected {expectedHouseCount} houses but found {houses.Length}.");
        Debug.Log($"House spawn test passed.Expected {expectedHouseCount} houses but found {houses.Length}.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ExclusionZoneTest()
    {
        // Find all exclusion zone objects in the scene
        var exclusionZoneObjects = GameObject.FindGameObjectsWithTag("ExclusiveZone");
        Assert.IsTrue(exclusionZoneObjects.Length > 0, "No exclusion zone objects found in the scene.");

        // Find all objects that could potentially violate the exclusion zones
        var objectsToCheck = new List<GameObject>();
        objectsToCheck.AddRange(GameObject.FindGameObjectsWithTag("Tree"));
        objectsToCheck.AddRange(GameObject.FindGameObjectsWithTag("Bush"));
        objectsToCheck.AddRange(GameObject.FindGameObjectsWithTag("House"));

        Assert.IsTrue(objectsToCheck.Count > 0, "No objects found to check against exclusion zones.");

        foreach (var exclusionZoneObject in exclusionZoneObjects)
        {
            var exclusionZoneCollider = exclusionZoneObject.GetComponent<Collider>();
            Assert.IsNotNull(exclusionZoneCollider, $"{exclusionZoneObject.name} does not have a collider.");

            // Use the collider's bounds as the exclusion zone
            var exclusionZoneBounds = exclusionZoneCollider.bounds;

            foreach (var obj in objectsToCheck)
            {
                // Assert that the object's position is NOT within the exclusion zone
                Assert.IsFalse(
                    exclusionZoneBounds.Contains(obj.transform.position),
                    $"{obj.name} is in the exclusion zone defined by {exclusionZoneObject.name}."
                );
            }
        }

        Debug.Log("Exclusion Zone Test passed.");
        yield return null;
    }



    [UnityTest]
    public IEnumerator SlopeConstraintTest()
    {
        float slopeThreshold = 60f; // Maximum allowed slope in degrees
        var terrain = GameObject.FindWithTag("Terrain");
        Assert.IsNotNull(terrain, "Terrain object with tag 'Terrain' not found.");

        // Assuming you have defined tags for trees, bushes, and houses
        string[] objectTags = { "Tree", "Bush", "House" };

        foreach (var tag in objectTags)
        {
            var spawnedObjects = GameObject.FindGameObjectsWithTag(tag);
            Assert.IsNotNull(spawnedObjects, $"No spawned objects found with the tag '{tag}'.");

            foreach (var obj in spawnedObjects)
            {
                var position = obj.transform.position;
                var slope = terrain.GetComponent<Terrain>().terrainData.GetSteepness(position.x / terrain.GetComponent<Terrain>().terrainData.size.x, position.z / terrain.GetComponent<Terrain>().terrainData.size.z);

                Assert.LessOrEqual(slope, slopeThreshold, $"{obj.name} is placed on a slope steeper than {slopeThreshold} degrees.");
            }
        }

        Debug.Log("Slope Constraint Test passed.");
        yield return null;
    }



    [UnityTest]
    public IEnumerator HeightmapApplicationTest()
    {
        // Find the terrain GameObject
        var terrainObject = GameObject.FindWithTag("Terrain");
        Assert.IsNotNull(terrainObject, "Terrain not found for heightmap application test.");

        // Get the Terrain component
        var terrain = terrainObject.GetComponent<Terrain>();
        Assert.IsNotNull(terrain, "Terrain component not found on the GameObject.");

        // Retrieve the heightmap data
        var heightmap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);

        // Check for height variations
        bool hasHeightVariations = heightmap.Cast<float>().Any(height => height != 0);

        // Assert that there are height variations in the heightmap
        Assert.IsTrue(hasHeightVariations, "Heightmap application is incorrect, no height variations found.");

        // Log success message
        Debug.Log("Heightmap Application Test passed with correct height variations.");

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Clean up the test scene by destroying the terrain generator object.
        Object.Destroy(terrainGenerator);
        yield return null;
    }

}

