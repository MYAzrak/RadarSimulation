using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CameraTest : InputTestFixture
{
    GameObject cameraObject;
    CameraController cameraController;

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator CameraControllerMovementTest()
    {
        SceneManager.LoadScene("Assets/Scenes/OceanMain.unity");

        yield return null;
        
        cameraObject = GameObject.FindWithTag("MainCamera");
        Assert.IsNotNull(cameraObject, "Main Camera should be present in the scene.");

        cameraController = cameraObject.GetComponent<CameraController>();
        Assert.IsNotNull(cameraController, "Main Camera should have CameraController script.");

        // Start at the origin
        cameraObject.transform.position = Vector3.zero;
        cameraObject.transform.rotation = Quaternion.identity;

        // Set speed to 1 
        cameraController.movementSpeed = 1;

        // Hold right mouse button
        var mouse = InputSystem.AddDevice<Mouse>();
        Press(mouse.rightButton);

        InputSystem.Update();

        var keyboard = InputSystem. AddDevice<Keyboard>();

        // Move forward
        Press(keyboard.wKey);
        InputSystem.Update();
        
        cameraController.Move();

        Release(keyboard.wKey);
        InputSystem.Update();
        
        Assert.AreEqual(new Vector3(0, 0, 1), cameraObject.transform.position);

        // Move right
        Press(keyboard.dKey);
        InputSystem.Update();

        cameraController.Move();
        
        Release(keyboard.dKey);
        InputSystem.Update();

        Assert.AreEqual(new Vector3(1, 0, 1), cameraObject.transform.position);

        // Move backward
        Press(keyboard.sKey);
        InputSystem.Update();

        cameraController.Move();

        Release(keyboard.sKey);
        InputSystem.Update();
        
        Assert.AreEqual(new Vector3(1, 0, 0), cameraObject.transform.position);

        // Move left
        Press(keyboard.aKey);
        InputSystem.Update();

        cameraController.Move();
        
        Release(keyboard.aKey);
        InputSystem.Update();
        
        Assert.AreEqual(new Vector3(0, 0, 0), cameraObject.transform.position);

        Release(mouse.rightButton);

        yield return null;
    }
}
