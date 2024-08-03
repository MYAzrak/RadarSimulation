using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    [SerializeField] int movementSpeed = 75;


    [Header("Camera Rotation")]
    [SerializeField] int horizontalSpeed = 75;
    [SerializeField] int verticalSpeed = 75;
    [SerializeField] bool invertedVerticalMovement = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Only rotate the camera if the user is holding the right button
        if (Input.GetButton("Fire2")) 
        {
            // Get the mouse delta, and add eulerAngles to save the last rotated values
            float h = transform.eulerAngles.y + horizontalSpeed * Input.GetAxis("Mouse X") * Time.deltaTime;
            float v = transform.eulerAngles.x + verticalSpeed * Input.GetAxis("Mouse Y") * (invertedVerticalMovement ? 1 : -1) * Time.deltaTime;

            transform.rotation = Quaternion.Euler(v, h, 0);
        }

        // Camera movement
        float verticalDirection = Input.GetAxis("Vertical") * Time.deltaTime * movementSpeed;
        float horizontalDirection = Input.GetAxis("Horizontal") * Time.deltaTime * movementSpeed;
        
        transform.position += (transform.forward * verticalDirection) + (transform.right * horizontalDirection);
    }
}
