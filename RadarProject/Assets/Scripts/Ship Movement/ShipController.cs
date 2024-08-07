using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Header("Location and Speed Information")]
    public List<Vector3> locationsToVisit;
    public List<float> speedAtEachLocation;

    [Header("Ship Power")]
    [Range(0.001f, 0.010f)] public float turnSpeedMultiplier = 0.005f; // The ship rotates very fast so keep this number low
    public float forwardSpeed = 5f; // The speed is in knots but it is converted to m/s for Unity
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
            forwardSpeed = speedAtEachLocation[indexOfLocationToVisit];
        
        Vector3 heading = locationsToVisit[indexOfLocationToVisit] - transform.position;
        heading.y = 0; // Ignore elevation
        
        float dot = Vector3.Dot(transform.forward, heading.normalized);

        // Not facing the next location
        if  (dot < 0.999f) {
            var newRotation = Quaternion.LookRotation (heading, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, turnSpeedMultiplier * Time.deltaTime);
        }

        if (heading.sqrMagnitude < distanceThreshold * distanceThreshold)
        {
            // Move to the next location
            indexOfLocationToVisit += 1;
            return;
        }

        float counterDrag = (rigidbody.drag == 0) ? 1 : rigidbody.drag; // Counter the drag to maintain the inputted speed
        float force = counterDrag * rigidbody.mass * shipInformation.GetSpeedInMetersPerSecond(forwardSpeed); // f = m a
        Vector3 forwardForce = Vector3.Scale(new Vector3(1, 0, 1), transform.forward) * force; // Ignore y axis
        rigidbody.AddForce(forwardForce, ForceMode.Force); // Always move forward for now
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

            if (logMessages)
                Debug.Log($"{shipInformation.GetName()} with ID {shipInformation.GetID()} has speed: {speed} Knots at position: {position}");
        }
    }
}
