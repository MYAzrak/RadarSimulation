using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    public int movementSpeed = 1;

    [Header("Camera Rotation")]
    [SerializeField] int rotationSpeed = 1;
    [SerializeField] bool invertedVerticalMovement = false;

    GameObject weather;

    // Update is called once per frame
    void Update()
    {
        // Only move or rotate the camera if the user is holding the right button
        if (Input.GetButton("Fire2")) 
        {
            // Get the mouse delta, and add eulerAngles to save the last rotated values
            float h = transform.eulerAngles.y + rotationSpeed * Input.GetAxis("Mouse X");
            float v = transform.eulerAngles.x + rotationSpeed * Input.GetAxis("Mouse Y") * (invertedVerticalMovement ? 1 : -1);

            transform.rotation = Quaternion.Euler(v, h, 0);

            // Camera movement
            float verticalDirection = Input.GetAxisRaw("Vertical") * movementSpeed;
            float horizontalDirection = Input.GetAxisRaw("Horizontal") * movementSpeed;
            
            transform.position += (transform.forward * verticalDirection) + (transform.right * horizontalDirection);
        }

        if (weather != null) 
        {
            weather.transform.position = transform.position; 
        }
    }

    public void SetWeatherOverCamera(GameObject weather)
    {
        this.weather = weather;
    }

    public Vector3 GetTransformPosition()
    {
        return transform.position;
    }

    public void SetSpeed(int speed)
    {
        movementSpeed = speed;
    }
}
