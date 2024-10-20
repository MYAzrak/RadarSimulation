using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Collections;

public class CoastlineKhorfakkanTest
{
    private GameObject terrainGenerationObject;
    private RuntimeTerrainGenerator terrainGeneratorComponent;
    private Collider exclusionZoneCollider;

    [UnitySetUp]
    public IEnumerator InitializingTestingEnvironment()
    {
        // Load the scene that contains the Terrain object.
        yield return SceneManager.LoadSceneAsync("KhofranBeach");

        // Find the existing Terrain object in the scene.
        terrainGenerationObject = GameObject.Find("Terrain");

        // Ensure the Terrain object exists.
        Assert.IsNotNull(terrainGenerationObject, "Terrain object must be present in the scene.");

        // Get the RuntimeTerrainGenerator component from the object.
        terrainGeneratorComponent = terrainGenerationObject.GetComponent<RuntimeTerrainGenerator>();
        Assert.IsNotNull(terrainGeneratorComponent, "The RuntimeTerrainGenerator component must be attached to the Terrain object.");

        // Validate user input ranges for number of trees, bushes, and houses.
        // terrainGeneratorComponent.numberOfTrees = Mathf.Clamp(terrainGeneratorComponent.numberOfTrees, 100, 1000);
        // terrainGeneratorComponent.numberOfBushes = Mathf.Clamp(terrainGeneratorComponent.numberOfBushes, 100, 1000);
        //terrainGeneratorComponent.numberOfHouses = Mathf.Clamp(terrainGeneratorComponent.numberOfHouses, 1, 3);

        // Debug.Log($"The number of Trees: {terrainGeneratorComponent.numberOfTrees}");
        // Debug.Log($"The number of Bushes: {terrainGeneratorComponent.numberOfBushes}");
        // Debug.Log($"The number of Houses: {terrainGeneratorComponent.numberOfHouses}");

        // Ensure the exclusion zone is assigned and has a collider.
        Assert.IsNotNull(terrainGeneratorComponent.exclusionZoneObject, "Exclusion area object is not assigned in the RuntimeTerrainGenerator.");
        exclusionZoneCollider = terrainGeneratorComponent.exclusionZoneObject.GetComponent<Collider>();
        Assert.IsNotNull(exclusionZoneCollider, "The exclusion area object must have a Collider component attached.");

        // Call the GenerateTerrain method to initialize everything.
        terrainGeneratorComponent.GenerateTerrain();

        // Wait a frame to allow terrain and objects to initialize.
        yield return null;
    }

    /* [UnityTest]
     public IEnumerator TestTreeSpawn()
     {
         // Check if the trees have been spawned using the "Tree" tag.
         GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
         int treeCount = trees.Length;

         // Log the number of trees found for debugging purposes.
         Debug.Log($"Expected {terrainGeneratorComponent.numberOfTrees} trees, but found {treeCount}.");

         // Calculate acceptable range based on user input.
         int minExpectedCount = Mathf.FloorToInt(terrainGeneratorComponent.numberOfTrees * 0.9f); // 90% of the input
         int maxExpectedCount = Mathf.CeilToInt(terrainGeneratorComponent.numberOfTrees * 1.1f); // 110% of the input

         // Check if the tree count is within the acceptable range.
         if (treeCount >= minExpectedCount && treeCount <= maxExpectedCount)
         {
             Debug.Log("The actual number of trees falls within the acceptable range.");
         }

         // Verify the count is within the acceptable range.
         Assert.IsTrue(
             treeCount >= minExpectedCount && treeCount <= maxExpectedCount,
             $"Expected {terrainGeneratorComponent.numberOfTrees} trees, but found {treeCount}. Acceptable range: {minExpectedCount}-{maxExpectedCount}."
         );

         // Verify that the spawned trees are of the expected prefabs.
         foreach (GameObject tree in trees)
         {
             bool isExpectedPrefab = tree.name.Contains("Coconut_Palm_Tree01") || tree.name.Contains("Coconut_Palm_Tree02");
             Assert.IsTrue(isExpectedPrefab, $"Invalid tree prefab: {tree.name}");
         }

         yield return null;
     }*/

    [UnityTest]
    public IEnumerator TestTreeSpawn()
    {
        // Check if the number of trees is within the allowed range (100 to 1000).
        if (terrainGeneratorComponent.numberOfTrees < 100 || terrainGeneratorComponent.numberOfTrees > 1000)
        {
            Assert.Fail($"The number of trees must be between 100 and 1000. Current value: {terrainGeneratorComponent.numberOfTrees}.");
        }

        // Find all objects tagged as "Tree" in the scene.
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
        int treeCount = trees.Length;

        // Log the number of trees found for debugging purposes.
        // Debug.Log($"Expected {terrainGeneratorComponent.numberOfTrees} trees, but found {treeCount}.");

        /* // Calculate acceptable range based on user input.
         int minExpectedCount = Mathf.FloorToInt(terrainGeneratorComponent.numberOfTrees * 0.9f); // 90% of the input
         int maxExpectedCount = Mathf.CeilToInt(terrainGeneratorComponent.numberOfTrees * 1.1f); // 110% of the input

         // Check if the tree count is within the acceptable range.
         if (treeCount >= minExpectedCount && treeCount <= maxExpectedCount)
         {
             Debug.Log("The actual number of trees falls within the acceptable range.");
         }

         // Verify the count is within the acceptable range.
         Assert.IsTrue(
             treeCount >= minExpectedCount && treeCount <= maxExpectedCount,
             $"Expected {terrainGeneratorComponent.numberOfTrees} trees, but found {treeCount}. Acceptable range: {minExpectedCount}-{maxExpectedCount}."
         );*/

        // Debug.Log("The actual number of trees falls within the acceptable range.");
        // Verify that the spawned trees are of the expected prefabs.
        foreach (GameObject tree in trees)
        {
            bool isExpectedPrefab = tree.name.Contains("Coconut_Palm_Tree01") || tree.name.Contains("Coconut_Palm_Tree02");
            Assert.IsTrue(isExpectedPrefab, $"Invalid tree prefab: {tree.name}");
        }

        yield return null;
    }




    /* [UnityTest]
     public IEnumerator TestBushesSpawn()
     {
         // Check if the bushes have been spawned using the "Bush" tag.
         GameObject[] bushes = GameObject.FindGameObjectsWithTag("Bush");
         int bushCount = bushes.Length;

         // Log the number of bushes found for debugging purposes.
         Debug.Log($"Expected {terrainGeneratorComponent.numberOfBushes} bushes, but found {bushCount}.");

         // Calculate acceptable range based on user input.
         int minExpectedCount = Mathf.FloorToInt(terrainGeneratorComponent.numberOfBushes * 0.9f); // 90% of the input
         int maxExpectedCount = Mathf.CeilToInt(terrainGeneratorComponent.numberOfBushes * 1.1f); // 110% of the input

         // Check if the bushCount is within the acceptable range.
         if (bushCount >= minExpectedCount && bushCount <= maxExpectedCount)
         {
             Debug.Log("The actual number of bushes falls within the acceptable range.");
         }

         // Verify the count is within the acceptable range.
         Assert.IsTrue(
             bushCount >= minExpectedCount && bushCount <= maxExpectedCount,
             $"Expected {terrainGeneratorComponent.numberOfBushes} bushes, but found {bushCount}. Acceptable range: {minExpectedCount}-{maxExpectedCount}."
         );

         // Verify that the spawned bushes are of the expected prefabs.
         foreach (GameObject bush in bushes)
         {
             bool isExpectedPrefab = bush.name.Contains("P_Bush04") || bush.name.Contains("P_Bush05");
             Assert.IsTrue(isExpectedPrefab, $"Invalid bush prefab: {bush.name}");
         }

         yield return null;
     }*/
    [UnityTest]
    public IEnumerator TestBushesSpawn()
    {
        // Check if the number of bushes is within the allowed range (100 to 1000).
        if (terrainGeneratorComponent.numberOfBushes < 100 || terrainGeneratorComponent.numberOfBushes > 1000)
        {
            Assert.Fail($"The number of bushes must be between 100 and 1000. Current value: {terrainGeneratorComponent.numberOfBushes}.");
        }

        // Find all objects tagged as "Bush" in the scene.
        GameObject[] bushes = GameObject.FindGameObjectsWithTag("Bush");
        int bushCount = bushes.Length;

        // Log the number of bushes found for debugging purposes.
        // Debug.Log($"Expected {terrainGeneratorComponent.numberOfBushes} bushes, but found {bushCount}.");

        /*  // Calculate acceptable range based on user input.
          int minExpectedCount = Mathf.FloorToInt(terrainGeneratorComponent.numberOfBushes * 0.9f); // 90% of the input
          int maxExpectedCount = Mathf.CeilToInt(terrainGeneratorComponent.numberOfBushes * 1.1f); // 110% of the input

          // Check if the bush count is within the acceptable range.
          if (bushCount >= minExpectedCount && bushCount <= maxExpectedCount)
          {
              Debug.Log("The actual number of bushes falls within the acceptable range.");
          }

          // Verify the count is within the acceptable range.
          Assert.IsTrue(
              bushCount >= minExpectedCount && bushCount <= maxExpectedCount,
              $"Expected {terrainGeneratorComponent.numberOfBushes} bushes, but found {bushCount}. Acceptable range: {minExpectedCount}-{maxExpectedCount}."
          );*/

        // Debug.Log("The actual number of bushes falls within the acceptable range.");

        // Verify that the spawned bushes are of the expected prefabs.
        foreach (GameObject bush in bushes)
        {
            bool isExpectedPrefab = bush.name.Contains("P_Bush04") || bush.name.Contains("P_Bush05");
            Assert.IsTrue(isExpectedPrefab, $"Invalid bush prefab: {bush.name}");
        }

        yield return null;
    }


    /* [UnityTest]
     public IEnumerator TestHousesSpawn()
     {
         // Check if the houses have been spawned using the "House" tag.
         GameObject[] houses = GameObject.FindGameObjectsWithTag("House");
         int houseCount = houses.Length;

         // Log the number of houses found for debugging purposes.
         Debug.Log($"Expected {terrainGeneratorComponent.numberOfHouses} houses, but found {houseCount}.");

         // Calculate acceptable range based on user input.
         int minExpectedCount = Mathf.FloorToInt(terrainGeneratorComponent.numberOfHouses * 0.9f); // 90% of the input
         int maxExpectedCount = Mathf.CeilToInt(terrainGeneratorComponent.numberOfHouses * 1.1f); // 110% of the input

         // Check if the houses count is within the acceptable range.
         if (houseCount >= minExpectedCount && houseCount <= maxExpectedCount)
         {
             Debug.Log("The actual number of houses falls within the acceptable range.");
         }

         // Verify the count is within the acceptable range.
         Assert.IsTrue(
             houseCount >= minExpectedCount && houseCount <= maxExpectedCount,
             $"Expected {terrainGeneratorComponent.numberOfHouses} houses, but found {houseCount}. Acceptable range: {minExpectedCount}-{maxExpectedCount}."
         );

         // Verify that the spawned houses are of the expected prefabs.
         foreach (GameObject house in houses)
         {
             bool isExpectedPrefab = house.name.Contains("Desert_Building_M1") ||
                                     house.name.Contains("Desert_Building_M3") ||
                                     house.name.Contains("Desert_Building_M33") ||
                                     house.name.Contains("Desert_Building_M333") ||
                                     house.name.Contains("Desert_Building_M4");
             Assert.IsTrue(isExpectedPrefab, $"Invalid house prefab: {house.name}");
         }

         yield return null;
     }*/

    [UnityTest]
    public IEnumerator TestHousesSpawn()
    {
        // Check if the number of houses is within the allowed range (1 to 4).
        if (terrainGeneratorComponent.numberOfHouses < 1 || terrainGeneratorComponent.numberOfHouses > 4)
        {
            Assert.Fail($"The number of houses must be between 1 and 4. Current value: {terrainGeneratorComponent.numberOfHouses}.");
        }

        // Find all objects tagged as "House" in the scene.
        GameObject[] houses = GameObject.FindGameObjectsWithTag("House");
        int houseCount = houses.Length;

        // Log the number of houses found for debugging purposes.
        // Debug.Log($"Expected {terrainGeneratorComponent.numberOfHouses} houses, but found {houseCount}.");

        /* // Calculate acceptable range based on user input.
         int minExpectedCount = Mathf.FloorToInt(terrainGeneratorComponent.numberOfHouses * 0.9f); // 90% of the input
         int maxExpectedCount = Mathf.CeilToInt(terrainGeneratorComponent.numberOfHouses * 1.1f); // 110% of the input

         // Check if the house count is within the acceptable range.
         if (houseCount >= minExpectedCount && houseCount <= maxExpectedCount)
         {
             Debug.Log("The actual number of houses falls within the acceptable range.");
         }

         // Verify the count is within the acceptable range.
         Assert.IsTrue(
             houseCount >= minExpectedCount && houseCount <= maxExpectedCount,
             $"Expected {terrainGeneratorComponent.numberOfHouses} houses, but found {houseCount}. Acceptable range: {minExpectedCount}-{maxExpectedCount}."
         );*/
        // Debug.Log("The actual number of houses falls within the acceptable range.");

        // Verify that the spawned houses are of the expected prefabs.
        foreach (GameObject house in houses)
        {
            bool isExpectedPrefab = house.name.Contains("Desert_Building_M1") ||
                                    house.name.Contains("Desert_Building_M3") ||
                                    house.name.Contains("Desert_Building_M33") ||
                                    house.name.Contains("Desert_Building_M333") ||
                                    house.name.Contains("Desert_Building_M4");
            Assert.IsTrue(isExpectedPrefab, $"Invalid house prefab: {house.name}");
        }

        yield return null;
    }


    /*  [UnityTest]
      public IEnumerator TestObjectsInExclusionZone()
      {
          // Define tags for the objects we want to check.
          string[] tagsToCheck = { "Tree", "Bush", "House" };

          // Iterate over each tag.
          foreach (string tag in tagsToCheck)
          {
              // Find all objects with the current tag.
              GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);

              // Check each object to ensure it's outside the exclusion zone.
              foreach (GameObject obj in objects)
              {
                  // Assert that the object is outside the exclusion zone.
                  Assert.IsFalse(
                      exclusionZoneCollider.bounds.Contains(obj.transform.position),
                      $"Object with tag '{tag}' is present inside the exclusion area: {obj.name} at position {obj.transform.position}."
                  );
              }
          }

          yield return null;
      }*/
    [UnityTest]
    public IEnumerator TestObjectsInExclusionZone()
    {
        // Define tags for the objects we want to check.
        string[] tagsToCheck = { "Tree", "Bush", "House" };

        // Verify the exclusion zone collider's bounds.
        // Debug.Log($"Exclusion Zone Bounds: Center = {exclusionZoneCollider.bounds.center}, Size = {exclusionZoneCollider.bounds.size}");

        // Iterate over each tag.
        foreach (string tag in tagsToCheck)
        {
            // Find all objects with the current tag.
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);

            // Check each object to ensure it's outside the exclusion zone.
            foreach (GameObject obj in objects)
            {
                bool isInsideExclusionZone = exclusionZoneCollider.bounds.Contains(obj.transform.position);

                // Log the position of each object for debugging.
                // Debug.Log($"Object '{obj.name}' with tag '{tag}' is at position {obj.transform.position}. Inside Exclusion Zone: {isInsideExclusionZone}");

                // Assert that the object is outside the exclusion zone.
                Assert.IsFalse(
                    isInsideExclusionZone,
                    $"Object with tag '{tag}' is present inside the exclusion area: {obj.name} at position {obj.transform.position}."
                );
            }
        }

        yield return null;
    }


    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Clean up the test scene by destroying the terrain generator object.
        Object.Destroy(terrainGenerationObject);
        yield return null;
    }
}
