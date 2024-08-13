using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuController : MonoBehaviour
{
    VisualElement ui;
    ShipManager shipManager;

    bool enabled = false;
    List<TabViews> tabBtns = new();

    void Awake()
    {
        shipManager = FindObjectOfType<ShipManager>();
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
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
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
                tabView.visualElement.visible = true;
            }
            else
            {
                tabView.visualElement.visible = false;
            }
        }
    }

    void SetBtnEvents()
    {
        Button resetBtn = ui.Q("ResetBtn") as Button;
        resetBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.resetScenario = !shipManager.resetScenario);
        resetBtn.focusable = false;

        Button reloadBtn = ui.Q("ReloadBtn") as Button;
        resetBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.reloadCSV = !shipManager.reloadCSV);
        resetBtn.focusable = false;

        Button loadBtn = ui.Q("LoadBtn") as Button;
        loadBtn.RegisterCallback((ClickEvent clickEvent) => shipManager.loadScenario = !shipManager.loadScenario);
        loadBtn.focusable = false;

        Toggle logToggle = ui.Q("LogToggle") as Toggle;
        logToggle.RegisterCallback((ClickEvent clickEvent) => shipManager.logMessages = !shipManager.logMessages);
        logToggle.focusable = false;
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
