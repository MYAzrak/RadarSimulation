using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ShipInformation
{
    int id;
    string name;
    string type; // Fishing boat, cargo, etc

    public float currentSpeed = 0f;
    public Vector3 currentLocation;
    public float2 currentLocationLatLon; // TODO

    public List<float> speedHistory = new();
    public List<Vector3> locationHistory = new();
    
    public const float METERS_PER_SECOND_TO_KNOTS = 1.943844f; // 1 Meter/second = 1.943844 Knot
    public const float KNOTS_TO_METERS_PER_SECOND = 0.5144444f; // 1 Knot = 0.5144444 Meter/second
    public const int EARTH_RADIUS = 6371000; // 6,371 km

    public ShipInformation(int id, string name, string type)
    {
        this.id = id;
        this.name = name;
        this.type = type;
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

    public int GetID()
    {
        return id;
    }

    public string GetName()
    {
        return name;
    }
}