using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Header("Location and Speed Information")]
    public List<Vector3> locationsToVisit;
    public List<float> speedAtEachLocation;

    [Header("Ship Power")]
    [Range(0.001f, 0.020f)] public float turnSpeedMultiplier = 0.005f;
    public float forwardSpeed = 5f; // The speed is in knots but it is converted to m/s for Unity
    [SerializeField] Transform motor;

    [Header("Debug")]
    public int indexOfLocationToVisit;
    public int distanceThreshold = 50;
    public float timeToWaitBeforeLogging = 5f;
    public bool logMessages = false;

    ScenarioController scenarioController;
    new Rigidbody rigidbody;
    Transform shipTransform;
    public ShipInformation shipInformation;
    bool hitLand = false;

    void Start()
    {
        rigidbody = GetComponentInChildren<Rigidbody>() ?? GetComponent<Rigidbody>();
        shipTransform = transform;

        scenarioController = FindObjectOfType<ScenarioController>();

        StartCoroutine(LogShipEvents());
    }

    void FixedUpdate()
    {
        Move();
    }

    public void Move()
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
            Logger.Log("Error: shipInformation is not initialized");
            yield break;
        }

        while (Application.isPlaying) 
        {
            yield return new WaitForSeconds(timeToWaitBeforeLogging);

            // If ship hits land then exit
            if (hitLand) break;

            float speed = rigidbody.velocity.magnitude * ScenarioController.METERS_PER_SECOND_TO_KNOTS;
            Vector3 position = rigidbody.position;

            shipInformation.AddToHistory(speed, position);

            if (logMessages)
                Debug.Log($"Ship with ID {shipInformation.Id} has speed: {speed} Knots at position: {position}");
        }

        if (hitLand)
        {
            scenarioController.RemoveShip(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Contains("Terrain"))
        {
            hitLand = true;
        }
        else
        {
            hitLand = false;
        }
    }
}
