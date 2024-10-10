using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class CameraControllerTest
{
    GameObject cameraObject;
    CameraController cameraController;

    [SetUp]
    public void Setup()
    {
        cameraObject = new GameObject("Camera");
        cameraController = cameraObject.AddComponent<CameraController>();

        // Start at the origin
        cameraObject.transform.position = Vector3.zero;
        cameraObject.transform.rotation = Quaternion.identity;
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator CameraControllerMovementTest()
    {
        InputTestFixture inputTestFixture = new();
        var mouse = InputSystem.AddDevice<Mouse>();
        inputTestFixture.Press(mouse.rightButton);

        // move forward
        float verticalDirection = 1 * cameraController.movementSpeed;
        cameraObject.transform.position = Vector3.zero;
        cameraObject.transform.position += cameraObject.transform.forward * verticalDirection;

        Assert.AreEqual(new Vector3(0, 0, 1), cameraObject.transform.position);

        // move backward
        verticalDirection = -1 * cameraController.movementSpeed;
        cameraObject.transform.position = Vector3.zero;
        cameraObject.transform.position += cameraObject.transform.forward * verticalDirection;

        Assert.AreEqual(new Vector3(0, 0, -1), cameraObject.transform.position);

        // move right
        float horizontalDirection = 1 * cameraController.movementSpeed;
        cameraObject.transform.position = Vector3.zero;
        cameraObject.transform.position += cameraObject.transform.right * horizontalDirection;

        Assert.AreEqual(new Vector3(1, 0, 0), cameraObject.transform.position);

        // move right
        horizontalDirection = -1 * cameraController.movementSpeed;
        cameraObject.transform.position = Vector3.zero;
        cameraObject.transform.position += cameraObject.transform.right * horizontalDirection;

        Assert.AreEqual(new Vector3(-1, 0, 0), cameraObject.transform.position);

        yield return null;
    }
}
