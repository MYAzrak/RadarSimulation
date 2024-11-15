using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class RadarTests
{

    RadarController radarController;

    [SetUp]
    public void SetUp()
    {
        string sceneName = "OceanTests";

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene("Assets/Scenes/OceanTests.unity");
        }

        radarController = Object.FindObjectOfType<RadarController>();
        Assert.IsNotNull(radarController, "Radar controller not found in scene.");

        radarController.GenerateRadar();
    }

    [UnityTest]
    public IEnumerator RadarPPIisScanning()
    {
        radarController.GenerateRadar();
        foreach (KeyValuePair<int, GameObject> entry in radarController.radars)
        {
            RadarScript script = entry.Value.GetComponentInChildren<RadarScript>();
            bool allZeros = true;
            yield return new WaitUntil(() => script.nRotations > 0);
            foreach (int num in script.radarPPI)
            {
                if (num != 0)
                {
                    allZeros = false;
                    break;
                }
            }
            Assert.IsFalse(allZeros, "PPI image should not be empty");
        }
        radarController.UnloadRadars();
    }

    [UnityTest]
    public IEnumerator RadarDetectsSphere()
    {
        yield return new WaitForFixedUpdate();
        radarController.GenerateRadar();
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = new Vector3(0, 10000, 2500);
        sphere.transform.localScale = new Vector3(300, 300, 300);
        radarController.SetWeather(Weather.Clear);

        foreach (KeyValuePair<int, GameObject> entry in radarController.radars)
        {
            RadarScript script = entry.Value.GetComponentInChildren<RadarScript>();
            entry.Value.GetComponent<Rigidbody>().useGravity = false;
            entry.Value.transform.position = new Vector3(0, 10000, 0);
            yield return new WaitUntil(() => script.nRotations > 0);
            int azimuth = (int)(90 / script.resolution);
            bool allZeros = true;

            foreach (int num in script.radarPPI)
            {
                if (num != 0)
                {
                    allZeros = false;
                    break;
                }
            }
            Assert.IsFalse(allZeros, "Radar didn't detect a sphere");

        }

        Object.Destroy(sphere);
        radarController.UnloadRadars();
    }

    [UnityTest]
    public IEnumerator RadarControllerSetsVars()
    {
        radarController.antennaGainDBi = 30f;
        radarController.ImageRadius = 1000;
        radarController.WidthRes = 30;
        radarController.HeightRes = 720;
        radarController.GenerateRadars(3);
        yield return new WaitForFixedUpdate();

        foreach (KeyValuePair<int, GameObject> entry in radarController.radars)
        {
            RadarScript script = entry.Value.GetComponentInChildren<RadarScript>();
            Assert.AreEqual(radarController.antennaGainDBi, script.antennaGainDBi, "Antenna gain not set");
            Assert.AreEqual(radarController.ImageRadius, script.ImageRadius, "ImageRadius not set");
            Assert.AreEqual(radarController.WidthRes, script.WidthRes, "WidthRes not set");
            Assert.AreEqual(radarController.HeightRes, script.HeightRes, "HeightRes not set");
        }
        radarController.UnloadRadars();
    }




}
