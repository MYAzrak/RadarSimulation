using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RadarControllerTests
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

        radarController.rows = 3;
    }

    [Test]
    public void CreateRadarsTest()
    {
        int numOfRadars = 5;
        radarController.GenerateRadars(numOfRadars);
        Assert.AreEqual(radarController.radars.Count, numOfRadars, $"Number of generated radars is {radarController.radars.Count} and not {numOfRadars}.");

        Assert.AreEqual(radarController.radarIDAtRow[0].Count, 2, "2 Radars should have been created in the first row.");
        Assert.AreEqual(radarController.radarIDAtRow[1].Count, 2, "2 Radars should have been created in the second row.");
        Assert.AreEqual(radarController.radarIDAtRow[2].Count, 1, "1 Radars should have been created in the third row.");
    }

    [UnityTest]
    public IEnumerator UnloadCreatedRadarsTest()
    {
        radarController.UnloadRadars();

        yield return null;

        GameObject emptyRadarParent = GameObject.Find("Radars");
        Assert.AreEqual(emptyRadarParent.transform.childCount, 0, "Radars were not destroyed.");

        Assert.AreEqual(radarController.radars.Count, 0, "Radars were not destroyed.");
    }
}
