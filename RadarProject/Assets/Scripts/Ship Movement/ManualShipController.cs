using Unity.Mathematics;
using UnityEngine;

public class ManualShipController : MonoBehaviour
{
    [SerializeField] Transform motor;
    [SerializeField] float steerPower = 1f;
    [SerializeField] float shipSpeed = 5f; // The acceleration is in knots but it is converted to m/s for Unity
    
    new Rigidbody rigidbody;
    ShipInformation shipInformation;

    [Header("Debug")]
    [SerializeField] float currentSpeed;
    [SerializeField] Vector3 currentLocation;
    [SerializeField] float2 currentLocationLatLon; // TODO

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        shipInformation = new ShipInformation(1 , "test", "test");
    }

    void FixedUpdate()
    {   
        // Add force depending on user input
        float movementDirection = Input.GetAxis("Vertical");
        float force = rigidbody.mass * shipInformation.GetSpeedInMetersPerSecond(shipSpeed); // f = m a
        rigidbody.AddForce(movementDirection * transform.right * force, ForceMode.Force);

        float steerDirection = Input.GetAxis("Horizontal");
        float steerForce = rigidbody.mass * shipInformation.GetSpeedInMetersPerSecond(steerPower);
        rigidbody.AddForceAtPosition(steerDirection * transform.forward * steerForce, motor.position, ForceMode.Force);

        // Set ship information
        float speed = rigidbody.velocity.magnitude * ShipInformation.METERS_PER_SECOND_TO_KNOTS; // The speed stored is in knots
        Vector3 location = rigidbody.position;
        shipInformation.SetInformation(speed, location);

        // Set debug information
        currentSpeed = shipInformation.currentSpeed;
        currentLocation = shipInformation.currentLocation;
    }
}
