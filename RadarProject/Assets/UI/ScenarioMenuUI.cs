using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;   

public class ScenarioMenuUI
{
    VisualElement ui;
    MainMenuController mainMenuController;
    ShipManager shipManager;

    public ScenarioMenuUI(VisualElement ui, MainMenuController mainMenuController, ShipManager shipManager)
    {
        this.ui = ui;
        this.mainMenuController = mainMenuController;
        this.shipManager = shipManager;
    }

    public void SetBtnEvents()
    {
        Button resetBtn = ui.Q("ResetBtn") as Button;
        resetBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.resetScenario = !shipManager.resetScenario);

        Button reloadBtn = ui.Q("ReloadBtn") as Button;
        resetBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.reloadCSV = !shipManager.reloadCSV);

        Button loadBtn = ui.Q("LoadBtn") as Button;
        loadBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.loadScenario = !shipManager.loadScenario);

        // TODO: Reset toggle after resetting a scenario to be inline with ship manager
        Toggle logToggle = ui.Q("LogToggle") as Toggle;
        logToggle.RegisterCallback((ClickEvent clickEvent) => shipManager.logMessages = !shipManager.logMessages);

        SetDropdownField();
    }

    public void SetDropdownField(bool reset = false)
    {
        DropdownField dropdownField = ui.Q("ScenarioDropdown") as DropdownField;
        
        List<string> files = shipManager.ReadScenarioFiles(out int numberOfNextScenario);

        if (files.Count == 0)
        {
            Debug.Log("No scenario files found.");
            return; 
        }

        Debug.Log(numberOfNextScenario);
        mainMenuController.PassNextScenarioNumber(numberOfNextScenario);

        dropdownField.choices = files;
        dropdownField.value = shipManager.scenarioFileName = files[0];

        if (!reset)
        {
            dropdownField.RegisterValueChangedCallback(evt => {
                shipManager.scenarioFileName = evt.newValue;
            });
        }
    }
}
