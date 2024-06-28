
using UnityEngine;
using UnityEngine.UI;

public class DisplayRadarTexture : MonoBehaviour
{
    public RadarScript radarScript; // Reference to your RadarScript or the camera rendering the texture
    RawImage rawImage;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        if (radarScript != null && radarScript.radarCamera != null && radarScript.radarCamera.targetTexture != null)
        {
            rawImage.texture = radarScript.radarCamera.targetTexture;
        }
    }
}

