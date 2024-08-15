using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ShipInformation
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public ShipType Type { get; private set; }

    public List<float> speedHistory = new();
    public List<Vector3> locationHistory = new();
    public List<float2> LocationLatLonHistory = new(); // TODO

    public ShipInformation(int id, string name, ShipType type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    public void AddToHistory(float speed, Vector3 location)
    {
        speedHistory.Add(speed);
        locationHistory.Add(location);
    }

    public float GetSpeedInMetersPerSecond(float knot)
    {
        return knot * ScenarioManager.KNOTS_TO_METERS_PER_SECOND;
    }
}