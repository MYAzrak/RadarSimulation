using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ShipInformation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // Fishing boat, cargo, etc

    public float currentSpeed = 0f;
    public Vector3 currentLocation;
    public float2 currentLocationLatLon; // TODO

    public List<float> speedHistory = new();
    public List<Vector3> locationHistory = new();
    
    public const float METERS_PER_SECOND_TO_KNOTS = 1.943844f; // 1 Meter/second = 1.943844 Knot
    public const float KNOTS_TO_METERS_PER_SECOND = 0.5144444f; // 1 Knot = 0.5144444 Meter/second

    public ShipInformation(int id, string name, string type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    public void SetInformation(float speed, Vector3 location)
    {
        currentSpeed = speed;
        currentLocation = location;
    }

    public void AddToHistory(float speed, Vector3 location)
    {
        speedHistory.Add(speed);
        locationHistory.Add(location);
    }

    public float GetSpeedInMetersPerSecond(float knot)
    {
        return knot * KNOTS_TO_METERS_PER_SECOND;
    }
}