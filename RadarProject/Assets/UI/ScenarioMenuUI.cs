using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;   

public class ScenarioMenuUI
{
    VisualElement ui;
    MainMenuController mainMenuController;
    ScenarioManager scenarioManager;
    CSVManager csvManager;

    MinMaxValue<int> randomShipNumber = new(1, 2);
    MinMaxValue<int> locationsToCreate = new(2, 4);
    MinMaxValue<float> startingCoordinates = new(-1000, 1000);
    MinMaxValue<float> randomCoordinates = new(-1000, 1000);
    MinMaxValue<int> speed = new(11, 20);

    public ScenarioMenuUI(VisualElement ui, MainMenuController mainMenuController, ScenarioManager scenarioManager, CSVManager csvManager)
    {
        this.ui = ui;
        this.mainMenuController = mainMenuController;
        this.scenarioManager = scenarioManager;
        this.csvManager = csvManager;
    }

    public void SetBtnEvents()
    {
        ReadScenarios();

        SetLoadScenariosBtn();
        SetGenerateBtn();
    }

    public void ReadScenarios()
    {
        Label numOfScenariosLabel = ui.Q("NumOfScenariosLabel") as Label;

        List<string> files = scenarioManager.ReadScenarioFiles();
        int numOfScenarios = files.Count;

        numOfScenariosLabel.text = $"Found {numOfScenarios} Scenarios";
    }

    public void SetLoadScenariosBtn()
    {
        Button runScenariosBtn = ui.Q("RunScenariosBtn") as Button;

        runScenariosBtn.RegisterCallback((ClickEvent clickEvent) => {
            
            Debug.Log("Running scenarios");

            scenarioManager.loadAllScenarios = true;
            scenarioManager.loadScenario = true;
        });
    }

    public void SetGenerateBtn()
    {
        Button generateScenariosBtn = ui.Q("GenerateScenariosBtn") as Button;

        generateScenariosBtn.RegisterCallback((ClickEvent clickEvent) => {

            IntegerField generateScenariosInt = ui.Q("GenerateScenariosInt") as IntegerField;
            int numOfScenarios = int.Parse(generateScenariosInt.text);

            // TODO: Add error messages
            if (numOfScenarios < 0)
            {
                return;
            }
            
            string filePath = csvManager.GetFilePath();

            // Delete the file path and all scenarios in it
            if (Directory.Exists(filePath)) { Directory.Delete(filePath, true); }
                Directory.CreateDirectory(filePath);

            for(int i = 0; i < numOfScenarios; i++)
            {
                string file = filePath + "Scenario" + i;

                csvManager.numberOfShips = Random.Range(randomShipNumber.Min, randomShipNumber.Max);
                csvManager.locationsToCreate = Random.Range(locationsToCreate.Min, locationsToCreate.Max);
                csvManager.minStartingCoordinates = startingCoordinates.Min;
                csvManager.maxStartingCoordinates = startingCoordinates.Max;
                csvManager.randomCoordinates = Random.Range(randomCoordinates.Min, randomCoordinates.Max);
                csvManager.minSpeed = speed.Min;
                csvManager.maxSpeed = speed.Max;
                
                csvManager.GenerateCSV(file);
            }

            Debug.Log("All scenarios have been generated.");

            ReadScenarios();
        });
    }

    public struct MinMaxValue<T>
    {
        public T Min { get; set; }
        public T Max { get; set; }

        public MinMaxValue(T min, T max)
        {
            Min = min;
            Max = max;
        }
    }
}
