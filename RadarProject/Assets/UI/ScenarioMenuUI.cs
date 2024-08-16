using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;   

public class ScenarioMenuUI
{
    VisualElement ui;
    MainMenuController mainMenuController;
    ScenarioManager scenarioManager;

    public ScenarioMenuUI(VisualElement ui, MainMenuController mainMenuController, ScenarioManager scenarioManager)
    {
        this.ui = ui;
        this.mainMenuController = mainMenuController;
        this.scenarioManager = scenarioManager;
    }

    public void SetBtnEvents()
    {
        SliderInt timeScaleSlider = ui.Q("TimeScaleSlider") as SliderInt;
        timeScaleSlider.value = 1;
        timeScaleSlider.RegisterCallback((ClickEvent clickEvent) => {
            scenarioManager.timeScale = timeScaleSlider.value;
            scenarioManager.updateTimeScale = !scenarioManager.updateTimeScale;
        });

        Button resetBtn = ui.Q("ResetBtn") as Button;
        resetBtn.RegisterCallback((ClickEvent clickEvent) => scenarioManager.resetScenario = !scenarioManager.resetScenario);

        Button reloadBtn = ui.Q("ReloadBtn") as Button;
        resetBtn.RegisterCallback((ClickEvent clickEvent) => scenarioManager.reloadCSV = !scenarioManager.reloadCSV);

        Button loadBtn = ui.Q("LoadBtn") as Button;
        loadBtn.RegisterCallback((ClickEvent clickEvent) => scenarioManager.loadScenario = !scenarioManager.loadScenario);

        SetDropdownField();
    }

    public void SetDropdownField(bool reset = false)
    {
        DropdownField dropdownField = ui.Q("ScenarioDropdown") as DropdownField;
        
        List<string> files = scenarioManager.ReadScenarioFiles(out int numberOfNextScenario);

        if (files.Count == 0)
        {
            Debug.Log("No scenario files found.");
            return; 
        }

        mainMenuController.PassNextScenarioNumber(numberOfNextScenario);

        dropdownField.choices = files;
        dropdownField.value = scenarioManager.scenarioFileName = files[0];

        if (!reset)
        {
            dropdownField.RegisterValueChangedCallback(evt => {
                scenarioManager.scenarioFileName = evt.newValue;
            });
        }
    }
}
