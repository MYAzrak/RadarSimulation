using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugSpoke : MonoBehaviour
{
    RawImage rawImage;
    public Texture2D tex;

    // Start is called before the first frame update
    void Start()
    {
        rawImage = GetComponent<RawImage>();
        tex = new Texture2D(500, 500, TextureFormat.RGB24, false);
    }

    // Update is called once per frame
    void Update()
    {
        rawImage.texture = tex;
    }
}
