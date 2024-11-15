using UnityEngine;

[System.Serializable]
public class ScenarioSettings
{
    public Waves waves;
    public Weather weather;

    // Procedural land settings
    public bool hasProceduralLand;
    public int proceduralLandSeed;
    public Vector3 proceduralLandLocation;
    public RadarGenerationDirection directionToSpawnRadars;
}
