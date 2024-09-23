using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;   

public class ScenarioMenuUI
{
    VisualElement ui;
    MainMenuController mainMenuController;
    ScenarioController scenarioController;
    CSVController csvController;

    public ScenarioMenuUI(VisualElement ui, MainMenuController mainMenuController, ScenarioController scenarioController, CSVController csvController)
    {
        this.ui = ui;
        this.mainMenuController = mainMenuController;
        this.scenarioController = scenarioController;
        this.csvController = csvController;
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

        List<string> files = scenarioController.ReadScenarioFiles();
        int numOfScenarios = files.Count;

        numOfScenariosLabel.text = $"Found {numOfScenarios} Scenarios";
    }

    public void SetLoadScenariosBtn()
    {
        Button runScenariosBtn = ui.Q("RunScenariosBtn") as Button;

        runScenariosBtn.RegisterCallback((ClickEvent clickEvent) => {
            
            Debug.Log("Running scenarios");
            scenarioController.LoadAllScenarios();
        });
    }

    public void SetGenerateBtn()
    {
        Button generateScenariosBtn = ui.Q("GenerateScenariosBtn") as Button;

        generateScenariosBtn.RegisterCallback((ClickEvent clickEvent) => {
            
            // Get the number of scenarios from the UI
            IntegerField generateScenariosInt = ui.Q("GenerateScenariosInt") as IntegerField;
            int numOfScenarios = int.Parse(generateScenariosInt.text);

            // Generate the scenarios
            csvController.GenerateScenarios(numOfScenarios);
        });
    }
}
