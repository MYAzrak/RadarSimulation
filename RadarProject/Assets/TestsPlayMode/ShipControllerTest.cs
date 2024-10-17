using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class ShipControllerTests
{
    private GameObject shipGameObject;
    private ShipController shipController; // Replace with your actual class name
    private Transform shipTransform;

    [SetUp]
    public void SetUp()
    {
        string sceneName = "OceanTests";
        
        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene("Assets/Scenes/OceanTests.unity");
        }

        // Get ship game object
        shipGameObject = GameObject.Find("Gas Carrier Ship");
        Assert.IsNotNull(shipGameObject, "Gas Carrier Ship should be present in the test scene.");

        shipTransform = shipGameObject.transform;
        shipController = shipGameObject.GetComponent<ShipController>();

        // Initialize values for testing
        shipController.locationsToVisit = new List<Vector3>
        {
            new(0, 0, 60), // Directly in front of the ship
            new(50, 0, 150)  // To the right of the ship
        };
        
        shipController.speedAtEachLocation = new List<float>
        {
            15,
            15
        };
        shipController.forwardSpeed = 15f;
        shipController.distanceThreshold = 50;
        shipController.turnSpeedMultiplier = 0.020f;
        shipController.indexOfLocationToVisit = 0;

        shipController.shipInformation = new ShipInformation(1, "TestShip", ShipType.GasCarrier);
    }

    [UnityTest]
    public IEnumerator ShipMovementAndRotationTest()
    {
        // Speed up the events
        Time.timeScale = 2f;

        // Set initial position of the ship
        shipTransform.position = new Vector3(0, 0, 0);

        // Call FixedUpdate enough times to move
        for (int i = 0; i < 200; i++)
        {
            shipController.Move();
            yield return new WaitForFixedUpdate();
        }

        // Assert that the ship's position has moved towards the first location
        Assert.AreEqual(shipController.indexOfLocationToVisit, 1);

        yield return null;

        // Call FixedUpdate enough times to rotate
        for (int i = 0; i < 1000; i++)
        {
            shipController.Move();
            yield return new WaitForFixedUpdate();
        }

        // Assert that the ship has rotated to face the next location
        Assert.AreEqual(shipController.indexOfLocationToVisit, 2);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        Object.DestroyImmediate(shipGameObject);
    }
}
