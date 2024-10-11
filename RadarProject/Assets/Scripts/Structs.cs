
using System;
using UnityEngine;

[Serializable]
public struct ShipPrefab
{
    public ShipType shipType;
    public GameObject prefab;

    public ShipPrefab(ShipType shipType, GameObject prefab)
    {
        this.shipType = shipType;
        this.prefab = prefab;
    }
}

[Serializable]
public struct WavePrefab
{
    public Waves waves;
    public GameObject prefab;

    public WavePrefab(Waves waves, GameObject prefab)
    {
        this.waves = waves;
        this.prefab = prefab;
    }
}

[Serializable]
public struct WeatherPrefab
{
    public Weather weather;
    public GameObject prefab;
    public Material skybox;
    public Material oceanMaterial; // For reflecting light from sun

    public WeatherPrefab(Weather weather, GameObject prefab, Material skybox, Material oceanMaterial)
    {
        this.weather = weather;
        this.prefab = prefab;
        this.skybox = skybox;
        this.oceanMaterial = oceanMaterial;
    }
}