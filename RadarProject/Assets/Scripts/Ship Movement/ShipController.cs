using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Header("Location and Speed Information")]
    public List<Vector3> locationsToVisit;
    public List<float> speedAtEachLocation;

    [Header("Ship Power")]
    [SerializeField, Range(0.001f, 0.010f)] float turnSpeedMultiplier = 0.005f;
    public float forwardSpeed = 5f; // The speed is in knots but it is converted to m/s for Unity
    [SerializeField] Transform motor;

    [Header("Debug")]
    [SerializeField] int indexOfLocationToVisit;
    [SerializeField] int distanceThreshold = 50;
    [SerializeField] float timeToWaitBeforeLogging = 5f;
    public bool logMessages = false;

    new Rigidbody rigidbody;
    Transform shipTransform;
    public ShipInformation shipInformation;
    ScenarioManager scenarioManager;
    bool reportedCompletion = false;

    void Start()
    {
        rigidbody = GetComponentInChildren<Rigidbody>() ?? GetComponent<Rigidbody>();
        shipTransform = transform;
        scenarioManager = FindObjectOfType<ScenarioManager>();
        StartCoroutine(LogShipEvents());
    }

    void FixedUpdate()
    {
        if (indexOfLocationToVisit == locationsToVisit.Count) return;

        if (speedAtEachLocation != null)
            forwardSpeed = speedAtEachLocation[indexOfLocationToVisit];
        
        Vector3 heading = locationsToVisit[indexOfLocationToVisit] - shipTransform.position;
        heading.y = 0; // Ignore elevation
        
        float dot = Vector3.Dot(shipTransform.forward, heading.normalized);

        // Not facing the next location
        if  (dot < 0.999f) {
            var newRotation = Quaternion.LookRotation (heading, Vector3.up);
            shipTransform.rotation = Quaternion.Slerp(shipTransform.rotation, newRotation, turnSpeedMultiplier * Time.deltaTime);
        }

        if (heading.sqrMagnitude < distanceThreshold * distanceThreshold)
        {
            // Move to the next location
            indexOfLocationToVisit += 1;
            return;
        }

        Vector3 force = shipTransform.forward * rigidbody.mass * shipInformation.GetSpeedInMetersPerSecond(forwardSpeed); // f = m a
        rigidbody.AddForce(force, ForceMode.Force); // Always move forward for now
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
            
            float speed = rigidbody.velocity.magnitude * ScenarioManager.METERS_PER_SECOND_TO_KNOTS;
            Vector3 position = rigidbody.position;

            shipInformation.AddToHistory(speed, position);

            if (logMessages)
                Debug.Log($"{shipInformation.Name} with ID {shipInformation.Id} has speed: {speed} Knots at position: {position}");

            // Report to scenario manager that it has completed
            if (!reportedCompletion && indexOfLocationToVisit >= locationsToVisit.Count)
            {
                scenarioManager.ReportCompletion();
                reportedCompletion = true;
            }
        }
    }
}
