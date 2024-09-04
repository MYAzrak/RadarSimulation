using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    MainMenuController instance;
    VisualElement ui;
    ScenarioManager scenarioManager;
    CSVManager csvManager;

    ScenarioMenuUI ScenarioMenuUI;

    List<TabViews> tabBtns = new();

    VisualElement menuPanel;
    VisualElement simulationInfoPanel;
    
    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        scenarioManager = FindObjectOfType<ScenarioManager>();
        csvManager = FindObjectOfType<CSVManager>();
        ui = GetComponent<UIDocument>().rootVisualElement;

        menuPanel = ui.Q("Panel");
        menuPanel.visible = false;  

        simulationInfoPanel = ui.Q("SimulationInfoPanel");
        simulationInfoPanel.visible = false;

        ScenarioMenuUI = new(ui, instance, scenarioManager, csvManager);

        SetTabs();
        ViewToEnable(tabBtns[0].button);

        ScenarioMenuUI.SetBtnEvents();

        SetDefaultSimulationInfoPanel();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            menuPanel.visible = !menuPanel.visible;
            simulationInfoPanel.visible = !simulationInfoPanel.visible;
        }
    }

    void SetTabs()
    {
        AddTabBtnsToList();

        foreach (TabViews tabView in tabBtns)
        {
            tabView.button.RegisterCallback((ClickEvent ClickEvent) => ViewToEnable(tabView.button));
        }
    }

    void AddTabBtnsToList()
    {
        tabBtns.Add(new TabViews(ui.Q("ShipBtn") as Button, ui.Q("ShipsView")));
        //tabBtns.Add(new TabViews(ui.Q("CSVBtn") as Button, ui.Q("CsvView")));
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

    public void SetDefaultSimulationInfoPanel()
    {
        Label currentScenarioLabel = ui.Q("CurrentScenarioLabel") as Label;
        currentScenarioLabel.text = "No Scenario Loaded";

        Label numOfShipsLabel = ui.Q("NumOfShipsLabel") as Label;
        numOfShipsLabel.text = "0 Ships";

        Label numOfRadarsLabel = ui.Q("NumOfRadarsLabel") as Label;
        numOfRadarsLabel.text = "0 Radars";
    }

    public void SetScenarioLabel(string scenarioName)
    {
        Label currentScenarioLabel = ui.Q("CurrentScenarioLabel") as Label;
        currentScenarioLabel.text = "Currently Running: " + scenarioName;
    }

    public void SetShipsLabel(int numOfShips)
    {
        Label numOfShipsLabel = ui.Q("NumOfShipsLabel") as Label;
        numOfShipsLabel.text = numOfShips + " Ships";
    }

    public void SetRadarsLabel(int numOfRadars)
    {
        Label numOfRadarsLabel = ui.Q("NumOfRadarsLabel") as Label;
        numOfRadarsLabel.text = numOfRadars + " Radars";
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
