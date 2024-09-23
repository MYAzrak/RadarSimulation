using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MasterController : MonoBehaviour
{
    RadarController radarController;
    CSVController csvController;
    ScenarioController scenarioController;

    void Start()
    {
        radarController = GetComponent<RadarController>();
        csvController = GetComponent<CSVController>();
        scenarioController = GetComponent<ScenarioController>();


        string[] args = System.Environment.GetCommandLineArgs();
        Debug.Log("CLI Arg Length :" + args.Length);
        foreach (var x in args)
        {
            Debug.Log(x);
        }

        csvController.GenerateParameters();

        //CSV Controller Params
        int.TryParse(getArg("-nships"), out csvController.numberOfShips);
        int.TryParse(getArg("-nLocations"), out csvController.locationsToCreate);
        float.TryParse(getArg("-minStartingCoords"), out csvController.minStartingCoordinates);
        float.TryParse(getArg("-maxStartingCoords"), out csvController.maxStartingCoordinates);
        float.TryParse(getArg("-randomCoords"), out csvController.randomCoordinates);
        int.TryParse(getArg("-minSpeed"), out csvController.minSpeed);
        int.TryParse(getArg("-maxSpeed"), out csvController.maxSpeed);


        //Radar Params
        int.TryParse(getArg("-radarRows"), out radarController.rows);

        int nRadars = 0;
        if (int.TryParse(getArg("-nRadars"), out nRadars))
        {
            for (var x = 0; x < nRadars; x++)
            {
                radarController.GenerateRadar();
            }
        }

        //Scenario Params
        int nScenarios = 0;
        if (int.TryParse(getArg("-nScenarios"), out nScenarios))
        {
            //TODO: refactor scenario controller UI code to seperate method, callable from here
            //TODO: after generating scenarios automatically load them
        }

        //TODO: Start the simulation


    }

    void Update()
    {

    }

    private static string getArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                if (args[i + 1].StartsWith("-") || args[i + 1].StartsWith("--")) return null;
                return args[i + 1];
            }
        }
        return null;
    }
}
