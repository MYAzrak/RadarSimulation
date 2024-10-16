using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    MainMenuController instance;
    VisualElement ui;

    // -------------------------------
    // --------- Controllers ---------
    // -------------------------------
    [SerializeField] GameObject manager; // Manager objects that contains most of the controllers 
    ScenarioController scenarioController;
    CSVController csvController;
    WeatherController weatherController;
    RadarController radarController;
    WavesController wavesController;
    CameraController cameraController;

    // ------------------------------------
    // --------- UI panel classes ---------
    // ------------------------------------
    public ScenarioMenuUI ScenarioMenuUI;
    DynamicMenuUI dynamicMenuUI;

    List<TabViews> tabBtns = new();

    VisualElement menuPanel;
    VisualElement simulationInfoPanel;
    VisualElement helpPanel;

    bool paused = false;

    // Labels
    Label currentScenarioLabel;
    Label numOfShipsLabel;
    Label numOfRadarsLabel;
    Label waveConditionLabel;
    Label weatherConditionLabel;
    Label timeLimitLabel;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        // Get controllers
        scenarioController = manager.GetComponent<ScenarioController>();
        weatherController = manager.GetComponent<WeatherController>();
        radarController = manager.GetComponent<RadarController>();
        csvController = manager.GetComponent<CSVController>();

        wavesController = FindObjectOfType<WavesController>();
        cameraController = FindObjectOfType<CameraController>();

        ui = GetComponent<UIDocument>().rootVisualElement;

        // Hide panel by default
        menuPanel = ui.Q("Panel");
        menuPanel.visible = false;

        simulationInfoPanel = ui.Q("SimulationInfoPanel");
        simulationInfoPanel.visible = false;

        // Show help panel by default
        helpPanel = ui.Q("HelpPanel");
        helpPanel.visible = true;

        // Initialize the events
        ScenarioMenuUI = new(ui, instance, scenarioController, csvController);
        dynamicMenuUI = new(ui, instance, scenarioController, weatherController, wavesController, radarController, cameraController);

        InitializeLabels();

        SetTabs();
        ViewToEnable(tabBtns[0].button);

        ScenarioMenuUI.SetBtnEvents();
        dynamicMenuUI.SetBtnEvents();

        SetPauseBtn();
        SetEndBtns();

        SetDefaultSimulationInfoPanel();

        // Weird bug where the first visual element remains visible
        // Ignoring the parents visibility
        tabBtns[0].visualElement.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuPanel.visible = !menuPanel.visible;
            simulationInfoPanel.visible = !simulationInfoPanel.visible;

            helpPanel.visible = !helpPanel.visible;

            tabBtns[0].visualElement.visible = !tabBtns[0].visualElement.visible;
        }
    }

    void InitializeLabels()
    {
        currentScenarioLabel = ui.Q("ScenarioRunningLabel") as Label;
        numOfShipsLabel = ui.Q("NumOfShipsLabel") as Label;
        numOfRadarsLabel = ui.Q("NumOfRadarsLabel") as Label;
        waveConditionLabel = ui.Q("WaveConditionLabel") as Label;
        weatherConditionLabel = ui.Q("WeatherConditionLabel") as Label;
        timeLimitLabel = ui.Q("TimeLimitLabel") as Label;
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
        tabBtns.Add(new TabViews(ui.Q("DynamicBtn") as Button, ui.Q("DynamicView")));
        tabBtns.Add(new TabViews(ui.Q("CameraBtn") as Button, ui.Q("CameraView")));
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
        currentScenarioLabel.text = "No Scenario Loaded";
        numOfShipsLabel.text = "0 Ships";
        numOfRadarsLabel.text = "0 Radars";
        waveConditionLabel.text = $"Waves: {Waves.Calm}";
        weatherConditionLabel.text = $"Weather: {Weather.Clear}";
        timeLimitLabel.text = "Time Remaining: None";
    }

    public void SetShipsLabel(int numOfShips)
    {
        numOfShipsLabel.text = numOfShips + " Ships";
    }

    public void SetRadarsLabel(int numOfRadars)
    {
        numOfRadarsLabel.text = numOfRadars + " Radars";
    }

    public void SetScenarioRunningLabel(string label)
    {
        Label scenarioRunningLabel = ui.Q("ScenarioRunningLabel") as Label;
        scenarioRunningLabel.text = label;
    }

    public void SetWaveLabel(string waveCondition)
    {
        waveConditionLabel.text = "Waves: " + waveCondition;
    }

    public void SetWeatherLabel(string weatherCondition)
    {
        weatherConditionLabel.text = "Weather: " + weatherCondition;
    }

    public void SetTimeRemainingLabel(float timeRemaining)
    {
        float minutes = (int)(timeRemaining / 60);
        float seconds = (int)(timeRemaining % 60);

        timeLimitLabel.text = $"Time Remaining: {minutes}:{seconds}";
    }

    public void SetPauseBtn()
    {
        Button pauseSimulation = ui.Q("PauseSimulation") as Button;

        pauseSimulation.RegisterCallback((ClickEvent clickEvent) =>
        {

            if (paused)
            {
                scenarioController.SetTimeScale(1);
                pauseSimulation.text = "Pause";
                paused = false;
            }
            else
            {
                scenarioController.SetTimeScale(0);
                pauseSimulation.text = "Resume";
                paused = true;
            }
        });
    }

    public void SetEndBtns()
    {
        Button endScenarioBtn = ui.Q("EndScenarioBtn") as Button;
        endScenarioBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            scenarioController.EndScenario();
        });

        Button terminateSimulation = ui.Q("TerminateSimulation") as Button;
        terminateSimulation.RegisterCallback((ClickEvent clickEvent) =>
        {
            scenarioController.EndAllScenarios();
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
