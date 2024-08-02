using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] public List<Vector3> locationsToVisit;
    [SerializeField] public List<float> speedAtEachLocation;

    // The acceleration is in knots but it is converted to m/s for Unity
    [SerializeField] public float steerPower = 4f;
    [SerializeField] public float shipSpeed = 5f;
    [SerializeField] Transform motor;
    
    new Rigidbody rigidbody;
    public ShipInformation shipInformation;

    [Header("Debug")]
    [SerializeField] int indexOfLocationToVisit;
    [SerializeField] int distanceThreshold = 50;
    [SerializeField] float timeToWaitBeforeLogging = 5f;

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
            
            float speed = rigidbody.velocity.magnitude * ShipInformation.METERS_PER_SECOND_TO_KNOTS;
            Vector3 position = rigidbody.position;

            shipInformation.AddToHistory(speed, position);
            Debug.Log($"{shipInformation.GetName()} with ID {shipInformation.GetID()} has speed: {speed} Knots at position: {position}");
        }
    }
}
