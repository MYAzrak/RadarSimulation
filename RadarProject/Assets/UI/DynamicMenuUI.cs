using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DynamicMenuUI
{
    VisualElement ui;
    MainMenuController mainMenuController;
    ScenarioController scenarioController;
    WeatherController weatherController;
    WavesController wavesController;
    RadarController radarController;

    public DynamicMenuUI(
        VisualElement ui,
        MainMenuController mainMenuController,
        ScenarioController scenarioController,
        WeatherController weatherController,
        WavesController wavesController,
        RadarController radarController)
    {
        this.ui = ui;
        this.mainMenuController = mainMenuController;
        this.weatherController = weatherController;
        this.wavesController = wavesController;
        this.scenarioController = scenarioController;
        this.radarController = radarController;
    }

    public void SetBtnEvents()
    {
        SetScenarioEvents();
        SetWeatherEvents();
        SetWavesEvents();
    }

    public void SetScenarioEvents()
    {
        SliderInt simulationSpeedSlider = ui.Q("SimulationSpeedSlider") as SliderInt;
        simulationSpeedSlider.RegisterValueChangedCallback((ChangeEvent<int> evt) =>
        {
            scenarioController.SetTimeScale(evt.newValue);
        });
    }

    public void SetWeatherEvents()
    {
        Button clearWeatherBtn = ui.Q("ClearWeatherBtn") as Button;
        clearWeatherBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            weatherController.GenerateWeather(Weather.Clear);
            radarController.SetWeather(Weather.Clear);
        });

        Button lightRainBtn = ui.Q("LightRainBtn") as Button;
        lightRainBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            weatherController.GenerateWeather(Weather.LightRain);
            radarController.SetWeather(Weather.LightRain);
        });

        Button heavyRainBtn = ui.Q("HeavyRainBtn") as Button;
        heavyRainBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            weatherController.GenerateWeather(Weather.HeavyRain);
            radarController.SetWeather(Weather.HeavyRain);
        });
    }

    public void SetWavesEvents()
    {
        Button calmWavesBtn = ui.Q("CalmWavesBtn") as Button;
        calmWavesBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            wavesController.GenerateWaves(Waves.Calm);
        });

        Button moderateWavesBtn = ui.Q("ModerateWavesBtn") as Button;
        moderateWavesBtn.RegisterCallback((ClickEvent clickEvent) =>
        {
            wavesController.GenerateWaves(Waves.Moderate);
        });
    }
}
