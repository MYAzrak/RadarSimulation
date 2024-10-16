using UnityEngine.UIElements;

public class DynamicMenuUI
{
    VisualElement ui;
    MainMenuController mainMenuController;
    ScenarioController scenarioController;
    WeatherController weatherController;
    WavesController wavesController;
    RadarController radarController;
    CameraController cameraController;

    public DynamicMenuUI(
        VisualElement ui,
        MainMenuController mainMenuController,
        ScenarioController scenarioController,
        WeatherController weatherController,
        WavesController wavesController,
        RadarController radarController,
        CameraController cameraController)
    {
        this.ui = ui;
        this.mainMenuController = mainMenuController;
        this.weatherController = weatherController;
        this.wavesController = wavesController;
        this.scenarioController = scenarioController;
        this.radarController = radarController;
        this.cameraController = cameraController;
    }

    public void SetBtnEvents()
    {
        SetScenarioEvents();
        SetRadarEvents();
        SetWeatherEvents();
        SetWavesEvents();
        SetCameraEvents();
    }

    public void SetScenarioEvents()
    {
        SliderInt simulationSpeedSlider = ui.Q("SimulationSpeedSlider") as SliderInt;
        simulationSpeedSlider.RegisterValueChangedCallback((ChangeEvent<int> evt) =>
        {
            scenarioController.SetTimeScale(evt.newValue);
        });
    }
    public void SetRadarEvents()
    {
        // One radar
        Button generateOneRadarAtNetworkBtn = ui.Q("GenerateRadarAtNetworkBtn") as Button;
        generateOneRadarAtNetworkBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            radarController.GenerateRadar();
        });
        
        // Lattice network
        SliderInt radarColSlider = ui.Q("RadarColSlider") as SliderInt;
        radarColSlider.value = 3;
        radarController.cols = radarColSlider.value;
        radarColSlider.RegisterValueChangedCallback((ChangeEvent<int> evt) =>
        {
            radarController.cols = evt.newValue;
        });

        SliderInt radarRowSlider = ui.Q("RadarRowSlider") as SliderInt;
        radarRowSlider.value = 3;
        radarController.rows = radarRowSlider.value;
        radarRowSlider.RegisterValueChangedCallback((ChangeEvent<int> evt) =>
        {
            radarController.rows = evt.newValue;
        });

        Button generateNetworkBtn = ui.Q("GenerateNetworkBtn") as Button;
        generateNetworkBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            int numOfRadars = radarController.rows * radarController.cols;
            radarController.numOfRadars = numOfRadars;
            radarController.GenerateRadars(numOfRadars);
        });
    }

    public void SetWeatherEvents()
    {
        Button clearWeatherBtn = ui.Q("ClearWeatherBtn") as Button;
        clearWeatherBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            scenarioController.SetWeather(Weather.Clear);
        });

        Button lightRainBtn = ui.Q("LightRainBtn") as Button;
        lightRainBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            scenarioController.SetWeather(Weather.LightRain);
        });

        Button heavyRainBtn = ui.Q("HeavyRainBtn") as Button;
        heavyRainBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            scenarioController.SetWeather(Weather.HeavyRain);
        });
    }

    public void SetWavesEvents()
    {
        Button calmWavesBtn = ui.Q("CalmWavesBtn") as Button;
        calmWavesBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            scenarioController.SetWaves(Waves.Calm);
        });

        Button moderateWavesBtn = ui.Q("ModerateWavesBtn") as Button;
        moderateWavesBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            scenarioController.SetWaves(Waves.Moderate);
        });
    }

    public void SetCameraEvents()
    {
        SliderInt cameraSpeedSlider = ui.Q("CameraSpeedSlider") as SliderInt;
        cameraSpeedSlider.RegisterValueChangedCallback((ChangeEvent<int> evt) =>
        {
            cameraController.SetSpeed(evt.newValue);
        });

        Button goToRadarBtn = ui.Q("GoToRadarBtn") as Button;
        goToRadarBtn.RegisterCallback((ClickEvent clickEvent) => {
            
            // Get the number of scenarios from the UI
            IntegerField radarIDField = ui.Q("RadarIDField") as IntegerField;
            int radarID = int.Parse(radarIDField.text);

            if (radarController.radars.ContainsKey(radarID))
            {
                float x = radarController.radars[radarID].transform.position.x;
                float y = radarController.radars[radarID].transform.position.y;
                float z = radarController.radars[radarID].transform.position.z;
                cameraController.gameObject.transform.position = new UnityEngine.Vector3(x, y + 10, z);
            }
        });
    }
}
