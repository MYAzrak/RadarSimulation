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
        Button generateOneRadarAtNetworkBtn = ui.Q("GenerateRadarAtNetworkBtn") as Button;
        generateOneRadarAtNetworkBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            radarController.GenerateRadar();
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
    }
}
