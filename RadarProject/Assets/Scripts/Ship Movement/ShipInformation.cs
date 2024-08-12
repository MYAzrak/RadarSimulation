using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ShipInformation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ShipType Type { get; set; } // Fishing boat, cargo, etc

    public float currentSpeed = 0f;
    public Vector3 currentLocation;
    public float2 currentLocationLatLon; // TODO

    public List<float> speedHistory = new();
    public List<Vector3> locationHistory = new();

    public ShipInformation(int id, string name, ShipType type)
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
        return knot * ShipManager.KNOTS_TO_METERS_PER_SECOND;
    }
}