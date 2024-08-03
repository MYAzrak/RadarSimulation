using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Header("Location and Speed Attributes")]
    public List<Vector3> locationsToVisit;
    public List<float> speedAtEachLocation;

    [Header("Ship Information")]
    // The acceleration is in knots but it is converted to m/s for Unity
    public float steerPower = 4f;
    public float shipSpeed = 5f;
    [SerializeField] Transform motor;

    [Header("Debug")]
    [SerializeField] int indexOfLocationToVisit;
    [SerializeField] int distanceThreshold = 50;
    [SerializeField] float timeToWaitBeforeLogging = 5f;
    public bool logMessages = false;

    new Rigidbody rigidbody;
    public ShipInformation shipInformation;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        StartCoroutine(LogShipEvents());
    }

    void FixedUpdate()
    {
        if (indexOfLocationToVisit == locationsToVisit.Count) return;

        if (speedAtEachLocation != null)
            shipSpeed = speedAtEachLocation[indexOfLocationToVisit];
        
        Vector3 distanceToLocation = locationsToVisit[indexOfLocationToVisit] - transform.position;
        distanceToLocation.y = 0; // Ignore elevation
        
        float dot = Vector3.Dot(transform.forward, distanceToLocation.normalized);

        // Not facing the next location
        if  (dot < 0.999f) {
            float steerDirection = - Vector3.Dot(transform.right, distanceToLocation.normalized); // Negative to rotate it in the correct direction
            float steerForce = rigidbody.mass * shipInformation.GetSpeedInMetersPerSecond(steerPower);
            rigidbody.AddForceAtPosition(transform.right * steerForce * steerDirection, motor.position, ForceMode.Force);
        }

        if (distanceToLocation.magnitude < distanceThreshold)
        {
            // Move to the next location
            indexOfLocationToVisit += 1;
            return;
        }

        // Vector3 movementDirection = directionToLocation.normalized;
        float force = rigidbody.mass * shipInformation.GetSpeedInMetersPerSecond(shipSpeed); // f = m a
        rigidbody.AddForce(transform.forward * force, ForceMode.Force); // Always move forward for now
    }
    
    IEnumerator LogShipEvents()
    {
        if (shipInformation == null) {
            Debug.Log("Error: shipInformation is not initialized");
            yield break;
        }

        while (Application.isPlaying) 
        {
            yield return new WaitForSeconds(timeToWaitBeforeLogging);
            
            float speed = rigidbody.velocity.magnitude;
            Vector3 position = rigidbody.position;

            shipInformation.AddToHistory(speed, position);

            if (logMessages)
                Debug.Log($"{shipInformation.GetName()} with ID {shipInformation.GetID()} has speed: {speed} Knots at position: {position}");
        }
    }
}
