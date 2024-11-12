using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    public int movementSpeed = 1;

    [Header("Camera Rotation")]
    public float rotationSpeed = 0.4f;

    GameObject weather;

    // Update is called once per frame
    void Update()
    {
        Move();

        if (weather != null)
        {
            weather.transform.position = transform.position;
        }
    }

    public void Move()
    {
        // Only move or rotate the camera if the user is holding the right button
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            // Get the mouse delta, and add eulerAngles to save the last rotated values
            float h = transform.eulerAngles.y + rotationSpeed * Mouse.current.delta.x.ReadValue();
            float v = transform.eulerAngles.x + rotationSpeed * Mouse.current.delta.y.ReadValue() * -1; // -1 to invert movement

            transform.rotation = Quaternion.Euler(v, h, 0);

            // Camera movement
            int forward = Keyboard.current.wKey.isPressed ? 1 : 0;
            int backward = Keyboard.current.sKey.isPressed ? -1 : 0;

            int right = Keyboard.current.dKey.isPressed ? 1 : 0;
            int left = Keyboard.current.aKey.isPressed ? -1 : 0;

            float verticalDirection = (forward + backward) * movementSpeed;
            float horizontalDirection = (left + right) * movementSpeed;

            transform.position += (transform.forward * verticalDirection) + (transform.right * horizontalDirection);
        }
    }

    public void SetWeatherOverCamera(GameObject weather)
    {
        this.weather = weather;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CameraController))]
    public class CameraButton : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            CameraController script = (CameraController)target;
            if (GUILayout.Button("Change to depth"))
            {
                try
                {
                    Shader normalDepthShader = Shader.Find("Custom/NormalDepthShader");
                    script.GetComponentInParent<Camera>().SetReplacementShader(normalDepthShader, "");
                }
                catch (UnityException e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }
#endif

    public void SetSpeed(int speed)
    {
        movementSpeed = speed;
    }
}
