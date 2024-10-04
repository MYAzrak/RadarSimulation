using Crest;
using UnityEngine;

public class WavesController : MonoBehaviour
{
    public GameObject currentWave;

    ScenarioController scenarioController;
    OceanRenderer oceanRenderer;
    TimeProviderCustom timeProviderCustom;

    // Start is called before the first frame update
    void Start()
    {
        oceanRenderer = GetComponent<OceanRenderer>();
        timeProviderCustom = GetComponent<TimeProviderCustom>();
        timeProviderCustom._overrideTime = true;

        oceanRenderer.PushTimeProvider(timeProviderCustom);

        scenarioController = FindObjectOfType<ScenarioController>();

        // Set default wave
        currentWave = Instantiate(scenarioController.wavePrefabs[0].prefab, Vector3.zero, Quaternion.identity);
    }

    public void SetTimeProvider(float time)
    {
        timeProviderCustom._time = time;
    }
    
    public void GenerateWaves(Waves scenarioWave)
    {
        GameObject prefab = null;

        foreach (ScenarioController.WavePrefab wavePrefab in scenarioController.wavePrefabs)
        {
            if (wavePrefab.waves == scenarioWave)
            {
                prefab = wavePrefab.prefab;
            }
        }

        Destroy(currentWave);
        currentWave = Instantiate(prefab, Vector3.zero, Quaternion.identity);
    }

    public void ResetToDefaultWave()
    {
        Destroy(currentWave);
        currentWave = Instantiate(scenarioController.wavePrefabs[0].prefab, Vector3.zero, Quaternion.identity);
    }
}
