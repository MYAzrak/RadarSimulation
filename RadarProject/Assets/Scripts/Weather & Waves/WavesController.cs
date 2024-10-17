using Crest;
using UnityEngine;

public class WavesController : MonoBehaviour
{
    public GameObject currentWave;
    public GameObject defaultWave;

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

        defaultWave = GameObject.Find("WavesCalm");
    }

    public void SetTimeProvider(float time)
    {
        timeProviderCustom._time = time;
    }
    
    public void GenerateWaves(Waves scenarioWave, GameObject prefab)
    {   
        ResetToDefaultWave();
        if (scenarioWave != Waves.Calm)
        {
            defaultWave.SetActive(false);        
            currentWave = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            mainMenuController.SetWaveLabel(scenarioWave.ToString());
        }
    }

    public void ResetToDefaultWave()
    {
        if (currentWave != null)
            Destroy(currentWave);
        
        if (defaultWave != null)
            defaultWave.SetActive(true);
    }
}
