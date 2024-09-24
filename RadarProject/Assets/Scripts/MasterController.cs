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
        setIntArg("-nships", ref csvController.numberOfShips);
        setIntArg("-nLocations", ref csvController.locationsToCreate);
        setFloatArg("-minStartingCoords", ref csvController.minStartingCoordinates);
        setFloatArg("-maxStartingCoords", ref csvController.maxStartingCoordinates);
        setFloatArg("-randomCoords", ref csvController.randomCoordinates);
        setIntArg("-minSpeed", ref csvController.minSpeed);
        setIntArg("-maxSpeed", ref csvController.maxSpeed);

        //Radar Params
        setIntArg("-radarRows", ref radarController.rows);

        int nRadars = 0;
        if (setIntArg("-nRadars", ref nRadars))
        {
            radarController.GenerateRadars(nRadars);
        }

        //Scenario Params
        int nScenarios = 0;
        if (setIntArg("-nScenarios", ref nScenarios))
        {
            //TODO: refactor scenario controller UI code to seperate method, callable from here
            //TODO: after generating scenarios automatically load them
            csvController.GenerateScenarios(nScenarios);
            scenarioController.LoadAllScenarios();
        }

        //TODO: Start the simulation
    }

    void Update()
    {

    }

    private bool setIntArg(string argName, ref int parameter)
    {
        string arg = getArg(argName);
        if (arg != null && int.TryParse(arg, out int value))
        {
            parameter = value;
            return true;
        }
        return false;
    }

    private bool setFloatArg(string argName, ref float parameter)
    {
        string arg = getArg(argName);
        if (arg != null && float.TryParse(arg, out float value))
        {
            parameter = value;
            return true;
        }
        return false;
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
