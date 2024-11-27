using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WebSocketSharp;

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

        /*
        string[] args = System.Environment.GetCommandLineArgs();

        Debug.Log("CLI Arg Length :" + args.Length);
        foreach (var x in args)
        {
            Debug.Log(x);
        }
        */

        string sceneName = GetArg("-sceneName");
        if (!sceneName.IsNullOrEmpty() && SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        StartCoroutine(RunArguments());
    }

    IEnumerator RunArguments()
    {
        yield return null;

        //CSV Controller Params
        if (SetIntListArg("-nships", out int minShips, out int maxShips))
        {
            minShips = Mathf.Clamp(minShips, 0, 120);
            maxShips = Mathf.Clamp(maxShips, 0, 120);
            csvController.numOfShips = new MinMax<int>(minShips, maxShips);
        }
           

        if (SetIntListArg("-nLocations", out int minLocation, out int maxLocation))
        {
            minLocation = Mathf.Clamp(minLocation, 5, 30);
            maxLocation = Mathf.Clamp(maxLocation, 5, 30);
            csvController.locationsToVisit = new MinMax<int>(minLocation, maxLocation);
        }

        SetFloatArg("-coordinateSquareWidth", ref csvController.coordinateSquareWidth);

        if (SetIntListArg("-speed", out int minSpeed, out int maxSpeed))
        {
            minSpeed = Mathf.Clamp(minSpeed, 1, 30);
            maxSpeed = Mathf.Clamp(maxSpeed, 1, 30);
            csvController.speedAtLocations = new MinMax<int>(minSpeed, maxSpeed);
        }

        SetEnumListArg("-weather", ref csvController.weathers);
        SetEnumListArg("-waves", ref csvController.waves);
        SetBoolListArg("-proceduralLand", ref csvController.generateProceduralLand);

        if (SceneManager.GetActiveScene().name == "KhorfakkanCoastline")
        {
            csvController.generateProceduralLand = new( new bool[] { false } );
            radarController.direction = RadarGenerationDirection.Right;
        }

        //Radar Params
        SetIntArg("-radarRows", ref radarController.rows);
        SetFloatArg("-radarPower", ref radarController.transmittedPowerW);
        SetFloatArg("-radarGain", ref radarController.antennaGainDBi);
        SetFloatArg("-waveLength", ref radarController.waveLength);
        SetFloatArg("-radarLoss", ref radarController.systemLossesDB);
        SetIntArg("-radarImageRadius", ref radarController.ImageRadius);
        SetFloatArg("-antennaVerticalBeamWidth", ref radarController.antennaVerticalBeamWidth);
        SetFloatArg("-antennaHorizontalBeamWidth", ref radarController.antennaHorizontalBeamWidth);
        SetFloatArg("-rainRCS", ref radarController.rainRCS);

        //Scenario Params
        int nScenarios = 0;
        if (SetIntArg("-nScenarios", ref nScenarios))
        {
            csvController.GenerateScenarios(nScenarios);
            scenarioController.LoadAllScenarios();
        }
        SetFloatArg("-scenarioTimeLimit", ref scenarioController.timeLimit);

        int nRadars = 0;
        if (SetIntArg("-nRadars", ref nRadars))
        {
            radarController.GenerateRadars(nRadars);
        }

        if (SceneManager.GetActiveScene().name == "KhorfakkanCoastline")
        {
            GameObject terrain = GameObject.Find("Terrain");
            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();

            if (terrainCollider != null)
            {
                Bounds bounds = terrainCollider.bounds;

                float z = bounds.center.z;
                float depth = bounds.size.z;

                radarController.parentEmptyObject.transform.position = new Vector3(0, 0, z + (depth / 2));
            }
        }
    }

    private bool SetIntArg(string argName, ref int parameter)
    {
        string arg = GetArg(argName);
        if (arg != null && int.TryParse(arg, out int value))
        {
            parameter = value;
            return true;
        }
        return false;
    }

    private bool SetFloatArg(string argName, ref float parameter)
    {
        string arg = GetArg(argName);
        if (arg != null && float.TryParse(arg, out float value))
        {
            parameter = value;
            return true;
        }
        return false;
    }

    private bool SetIntListArg(string argName, out int min, out int max)
    {
        string arg = GetArg(argName);
        if (!string.IsNullOrEmpty(arg))
        {
            arg = arg.Trim('[', ']');
            string[] values = arg.Split(',');

            if (values.Length == 2)
            {
                if (int.TryParse(values[0].Trim(), out min) && int.TryParse(values[1].Trim(), out max))
                {
                    return true; 
                }
            }
        }
        
        min = 0;
        max = 0;
        return false;
    }

    private bool SetEnumListArg<TEnum>(string argName, ref LoopArray<TEnum> loopArray) where TEnum : struct, Enum
    {
        string arg = GetArg(argName);

        if (!string.IsNullOrEmpty(arg)) 
        {
            arg = arg.Trim('[', ']'); 
            string[] values = arg.Split(',');

            List<TEnum> list = new();

            foreach (string value in values)
            {
                string valueTemp = value.Trim();
                if (Enum.TryParse(valueTemp.Trim('\''), true, out TEnum enumValue))
                {
                    list.Add(enumValue);
                }
            }

            if (list.Count == 0) return false;
            
            loopArray = new LoopArray<TEnum>(list.ToArray());
            return true;
        }

        return false;
    }

    private bool SetBoolListArg(string argName, ref LoopArray<bool> loopArray)
    {
        string arg = GetArg(argName);

        if (!string.IsNullOrEmpty(arg)) 
        {
            arg = arg.Trim('[', ']'); 
            string[] values = arg.Split(',');

            List<bool> list = new();

            foreach (string value in values)
            {
                string valueTemp = value.Trim();
                valueTemp = valueTemp.Trim('\'');
                valueTemp = valueTemp.ToLower();
                if (valueTemp.Equals("true"))
                {
                    list.Add(true);
                }
                else if (valueTemp.Equals("false"))
                {
                    list.Add(false);
                }
            }

            if (list.Count == 0) return false;
            
            loopArray = new LoopArray<bool>(list.ToArray());
            return true;
        }

        return false;
    }

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
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
