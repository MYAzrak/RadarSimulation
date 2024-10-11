using Crest;
using UnityEngine;

public class WavesController : MonoBehaviour
{
    public GameObject currentWave;
    GameObject defaultWavePrefab;

    MainMenuController mainMenuController;
    OceanRenderer oceanRenderer;
    TimeProviderCustom timeProviderCustom;

    // Start is called before the first frame update
    void Start()
    {
        oceanRenderer = GetComponent<OceanRenderer>();
        timeProviderCustom = GetComponent<TimeProviderCustom>();
        timeProviderCustom._overrideTime = true;

        oceanRenderer.PushTimeProvider(timeProviderCustom);

        mainMenuController = FindObjectOfType<MainMenuController>();

        currentWave = GameObject.Find("WavesCalm");
    }

    public void SetTimeProvider(float time)
    {
        timeProviderCustom._time = time;
    }
    
    public void GenerateWaves(Waves scenarioWave, GameObject prefab)
    {
        if (currentWave != null)
            Destroy(currentWave);
        
        currentWave = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        mainMenuController.SetWaveLabel(scenarioWave.ToString());
    }

    public void ResetToDefaultWave(GameObject prefab = null)
    {
        if (currentWave != null)
            Destroy(currentWave);
        
        if (prefab != null)
        {
            currentWave = Instantiate(prefab, Vector3.zero, Quaternion.identity);

            if (defaultWavePrefab == null)
                defaultWavePrefab = prefab;
        }
    }
}
