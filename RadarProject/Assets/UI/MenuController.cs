using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuController : MonoBehaviour
{
    VisualElement ui;
    ShipManager shipManager;
    CSVManager csvManager;

    bool enabled = false;
    List<TabViews> tabBtns = new();

    void Awake()
    {
        shipManager = FindObjectOfType<ShipManager>();
        csvManager = FindObjectOfType<CSVManager>();
        ui = GetComponent<UIDocument>().rootVisualElement;

        Visibility visibility = enabled ? Visibility.Visible : Visibility.Hidden;
        ui.style.visibility = visibility;
        enabled = !enabled;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        AddTabBtnsToList();
        SetTabs();
        SetBtnEvents();
        SetDropdownField();
        ViewToEnable(tabBtns[0].button);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            Visibility visibility = enabled ? Visibility.Visible : Visibility.Hidden;
            ui.style.visibility = visibility;
            enabled = !enabled;
        }
    }

    void SetTabs()
    {
        foreach (TabViews tabView in tabBtns)
        {
            tabView.button.RegisterCallback((ClickEvent ClickEvent) => ViewToEnable(tabView.button));
        }
    }

    void AddTabBtnsToList()
    {
        tabBtns.Add(new TabViews(ui.Q("ShipBtn") as Button, ui.Q("ShipsView")));
        tabBtns.Add(new TabViews(ui.Q("CSVBtn") as Button, ui.Q("CsvView")));
    }

    void ViewToEnable(Button button)
    {
        foreach (TabViews tabView in tabBtns)
        {
            if (tabView.button.name.Equals(button.name))
            {
                tabView.visualElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                tabView.visualElement.style.display = DisplayStyle.None;
            }
        }
    }

    void SetBtnEvents()
    {
        // Ship Mangaer
        Button resetBtn = ui.Q("ResetBtn") as Button;
        resetBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.resetScenario = !shipManager.resetScenario);

        Button reloadBtn = ui.Q("ReloadBtn") as Button;
        resetBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.reloadCSV = !shipManager.reloadCSV);

        Button loadBtn = ui.Q("LoadBtn") as Button;
        loadBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.loadScenario = !shipManager.loadScenario);

        // TODO: Reset toggle after resetting a scenario to be inline with ship manager
        Toggle logToggle = ui.Q("LogToggle") as Toggle;
        logToggle.RegisterCallback((ClickEvent clickEvent) => shipManager.logMessages = !shipManager.logMessages);

        // CSV manager
        TextField fileNameTxtField = ui.Q("FileNameTxtField") as TextField;
        csvManager.fileName = fileNameTxtField.text;
        fileNameTxtField.RegisterValueChangedCallback(evt => {
            csvManager.fileName = evt.newValue;
        });

        SliderInt numOfShipsSlider = ui.Q("NumOfShipsSlider") as SliderInt;
        csvManager.numberOfShips = numOfShipsSlider.value;
        numOfShipsSlider.RegisterValueChangedCallback(evt => {
            csvManager.numberOfShips = evt.newValue;
        });

        SliderInt numOfLocationsSlider = ui.Q("NumOfLocationsSlider") as SliderInt;
        csvManager.locationsToCreate = numOfLocationsSlider.value;
        numOfLocationsSlider.RegisterValueChangedCallback(evt => {
            csvManager.locationsToCreate = evt.newValue;
        });

        Label minMaxLabel = ui.Q("MinMaxLabel") as Label;

        MinMaxSlider minMaxSlider = ui.Q("MinMaxSlider") as MinMaxSlider;
        csvManager.minStartingCoordinates = minMaxSlider.value.x;
        csvManager.maxStartingCoordinates = minMaxSlider.value.y;
        minMaxLabel.text = $"Coordinate Range:\nMin Value: {minMaxSlider.value.x}\nMax Value: {minMaxSlider.value.y}";
        minMaxSlider.RegisterValueChangedCallback(evt => {
            
            minMaxLabel.text = $"Coordinate Range:\nMin Value: {evt.newValue.x}\nMax Value: {evt.newValue.y}";

            csvManager.minStartingCoordinates = evt.newValue.x;
            csvManager.maxStartingCoordinates = evt.newValue.y;
        });

        Button generateRandomCSVBtn = ui.Q("GenerateCSVBtn") as Button;
        generateRandomCSVBtn.RegisterCallback((ClickEvent clickEvent) => {
            csvManager.generateRandomCSV = !csvManager.generateRandomCSV;
            DropdownField dropdownField = ui.Q("ScenarioDropdown") as DropdownField;
            dropdownField.choices.Add(fileNameTxtField.text);
        });
    }

    void SetDropdownField()
    {
        DropdownField dropdownField = ui.Q("ScenarioDropdown") as DropdownField;
        
        List<string> files = shipManager.ReadScenarioFiles();

        if (files.Count == 0)
        {
            Debug.Log("No scenario files found.");
            return;
        }

        dropdownField.choices = files;
        dropdownField.value = shipManager.scenarioFileName = files[0];
        dropdownField.focusable = false;
        dropdownField.RegisterValueChangedCallback(evt => {
            shipManager.scenarioFileName = evt.newValue;
        });
    }

    struct TabViews
    {
        public Button button;
        public VisualElement visualElement;

        public TabViews(Button button, VisualElement visualElement)
        {
            this.button = button;
            this.visualElement = visualElement;
        }
    }
}
