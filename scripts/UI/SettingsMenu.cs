using Godot;

public partial class SettingsMenu : Control
{
	private enum SettingsSection
	{
		Audio,
		Display,
		Controls,
		Developer
	}

	[Signal]
	public delegate void ClosedEventHandler();

	private const string SettingsPath = "user://settings.cfg";
	private const string DeveloperSection = "developer";
	private const string DayNightCycleEnabledKey = "day_night_cycle_enabled";
	private const string FishEnabledKey = "fish_enabled";
	private const string CameraStartPitchKey = "camera_start_pitch_degrees";
	private const string MusicBusName = "Music";
	private const string EffectsBusName = "Effects";
	private const string PlantingBusName = "Planting";
	private const string ForestAmbienceBusName = "ForestAmbience";
	private const string WaterAmbienceBusName = "WaterAmbience";
	private const string HeatAmbienceBusName = "HeatAmbience";
	private const string RainAmbienceBusName = "RainAmbience";
	private const string HeavyRainAmbienceBusName = "HeavyRainAmbience";
	private const float MinimumRenderScale = 0.5f;
	private const float MaximumRenderScale = 1.0f;
	private const string CameraSensitivitySetting =
		"gameplay/camera_sensitivity_multiplier";
	private const string ZoomSensitivitySetting =
		"gameplay/zoom_sensitivity_multiplier";
	private const string TileFocusDistanceSetting =
		"gameplay/tile_focus_distance";
	private const string BoardOverviewDistanceSetting =
		"gameplay/board_overview_distance_multiplier";
	private const string InvertVerticalSetting =
		"gameplay/invert_vertical_camera";

	private static readonly Vector2I[] WindowSizes =
	{
		new Vector2I(1280, 720),
		new Vector2I(1600, 900),
		new Vector2I(1920, 1080),
		new Vector2I(2560, 1440)
	};

	private Button _audioTabButton;
	private Button _displayTabButton;
	private Button _controlsTabButton;
	private Button _developerTabButton;
	private Control _audioPage;
	private Control _displayPage;
	private Control _controlsPage;
	private Control _developerPage;
	private HSlider _masterVolumeSlider;
	private Label _masterVolumeValue;
	private HSlider _musicVolumeSlider;
	private Label _musicVolumeValue;
	private HSlider _effectsVolumeSlider;
	private Label _effectsVolumeValue;
	private HSlider _plantingVolumeSlider;
	private Label _plantingVolumeValue;
	private HSlider _forestAmbienceVolumeSlider;
	private Label _forestAmbienceVolumeValue;
	private HSlider _waterAmbienceVolumeSlider;
	private Label _waterAmbienceVolumeValue;
	private HSlider _heatAmbienceVolumeSlider;
	private Label _heatAmbienceVolumeValue;
	private HSlider _rainAmbienceVolumeSlider;
	private Label _rainAmbienceVolumeValue;
	private HSlider _heavyRainAmbienceVolumeSlider;
	private Label _heavyRainAmbienceVolumeValue;
	private CheckButton _dayNightCycleToggle;
	private HSlider _cameraPitchSlider;
	private Label _cameraPitchValue;
	private CheckButton _grassVisibilityToggle;
	private CheckButton _tileModelsVisibilityToggle;
	private CheckButton _plantsVisibilityToggle;
	private CheckButton _stoneBorderVisibilityToggle;
	private CheckButton _outerRingVisibilityToggle;
	private CheckButton _outerGrassVisibilityToggle;
	private CheckButton _outerCommonTreeVisibilityToggle;
	private CheckButton _outerPine1VisibilityToggle;
	private CheckButton _outerPine2VisibilityToggle;
	private CheckButton _outerPine3VisibilityToggle;
	private CheckButton _outerBushVisibilityToggle;
	private CheckButton _outerFloweringBushVisibilityToggle;
	private CheckButton _outerFlowersVisibilityToggle;
	private CheckButton _outerMushroomsVisibilityToggle;
	private CheckButton _outerOtherVisibilityToggle;
	private CheckButton _waterVisibilityToggle;
	private CheckButton _fishVisibilityToggle;
	private CheckButton _shadowsToggle;
	private Label _drawCallsValue;
	private CheckButton _muteToggle;
	private CheckButton _fullscreenToggle;
	private CheckButton _vsyncToggle;
	private OptionButton _resolutionOptions;
	private HSlider _renderScaleSlider;
	private Label _renderScaleValue;
	private HSlider _cameraSensitivitySlider;
	private Label _cameraSensitivityValue;
	private HSlider _zoomSensitivitySlider;
	private Label _zoomSensitivityValue;
	private HSlider _tileFocusDistanceSlider;
	private Label _tileFocusDistanceValue;
	private HSlider _boardOverviewDistanceSlider;
	private Label _boardOverviewDistanceValue;
	private CheckButton _invertVerticalToggle;
	private Button _backButton;
	private CameraRigController _cameraRig;
	private float _cameraStartPitchDegrees;

	public override void _Ready()
	{
		EnsureAudioBus(MusicBusName);
		EnsureAudioBus(EffectsBusName);
		EnsureAudioBus(PlantingBusName, EffectsBusName);
		EnsureAudioBus(ForestAmbienceBusName, EffectsBusName);
		EnsureAudioBus(WaterAmbienceBusName, EffectsBusName);
		EnsureAudioBus(HeatAmbienceBusName, EffectsBusName);
		EnsureAudioBus(RainAmbienceBusName, EffectsBusName);
		EnsureAudioBus(HeavyRainAmbienceBusName, EffectsBusName);

		_audioTabButton = GetNode<Button>("%AudioTabButton");
		_displayTabButton = GetNode<Button>("%DisplayTabButton");
		_controlsTabButton = GetNode<Button>("%ControlsTabButton");
		_developerTabButton = GetNode<Button>("%DeveloperTabButton");
		_audioPage = GetNode<Control>("%AudioPage");
		_displayPage = GetNode<Control>("%DisplayPage");
		_controlsPage = GetNode<Control>("%ControlsPage");
		_developerPage = GetNode<Control>("%DeveloperPage");
		_masterVolumeSlider = GetNode<HSlider>("%MasterVolumeSlider");
		_masterVolumeValue = GetNode<Label>("%MasterVolumeValue");
		_musicVolumeSlider = GetNode<HSlider>("%MusicVolumeSlider");
		_musicVolumeValue = GetNode<Label>("%MusicVolumeValue");
		_effectsVolumeSlider = GetNode<HSlider>("%EffectsVolumeSlider");
		_effectsVolumeValue = GetNode<Label>("%EffectsVolumeValue");
		_plantingVolumeSlider = GetNode<HSlider>("%PlantingVolumeSlider");
		_plantingVolumeValue = GetNode<Label>("%PlantingVolumeValue");
		_forestAmbienceVolumeSlider = GetNode<HSlider>("%ForestAmbienceVolumeSlider");
		_forestAmbienceVolumeValue = GetNode<Label>("%ForestAmbienceVolumeValue");
		_waterAmbienceVolumeSlider = GetNode<HSlider>("%WaterAmbienceVolumeSlider");
		_waterAmbienceVolumeValue = GetNode<Label>("%WaterAmbienceVolumeValue");
		_heatAmbienceVolumeSlider = GetNode<HSlider>("%HeatAmbienceVolumeSlider");
		_heatAmbienceVolumeValue = GetNode<Label>("%HeatAmbienceVolumeValue");
		_rainAmbienceVolumeSlider = GetNode<HSlider>("%RainAmbienceVolumeSlider");
		_rainAmbienceVolumeValue = GetNode<Label>("%RainAmbienceVolumeValue");
		_heavyRainAmbienceVolumeSlider = GetNode<HSlider>("%HeavyRainAmbienceVolumeSlider");
		_heavyRainAmbienceVolumeValue = GetNode<Label>("%HeavyRainAmbienceVolumeValue");
		_dayNightCycleToggle = GetNode<CheckButton>("%DayNightCycleToggle");
		_cameraPitchSlider = GetNode<HSlider>("%CameraPitchSlider");
		_cameraPitchValue = GetNode<Label>("%CameraPitchValue");
		_grassVisibilityToggle = GetNode<CheckButton>("%GrassVisibilityToggle");
		_tileModelsVisibilityToggle = GetNode<CheckButton>("%TileModelsVisibilityToggle");
		_plantsVisibilityToggle = GetNode<CheckButton>("%PlantsVisibilityToggle");
		_stoneBorderVisibilityToggle = GetNode<CheckButton>("%StoneBorderVisibilityToggle");
		_outerRingVisibilityToggle = GetNode<CheckButton>("%OuterRingVisibilityToggle");
		_outerGrassVisibilityToggle = GetNode<CheckButton>("%OuterGrassVisibilityToggle");
		_outerCommonTreeVisibilityToggle =
			GetNode<CheckButton>("%OuterCommonTreeVisibilityToggle");
		_outerPine1VisibilityToggle =
			GetNode<CheckButton>("%OuterPine1VisibilityToggle");
		_outerPine2VisibilityToggle =
			GetNode<CheckButton>("%OuterPine2VisibilityToggle");
		_outerPine3VisibilityToggle =
			GetNode<CheckButton>("%OuterPine3VisibilityToggle");
		_outerBushVisibilityToggle =
			GetNode<CheckButton>("%OuterBushVisibilityToggle");
		_outerFloweringBushVisibilityToggle =
			GetNode<CheckButton>("%OuterFloweringBushVisibilityToggle");
		_outerFlowersVisibilityToggle =
			GetNode<CheckButton>("%OuterFlowersVisibilityToggle");
		_outerMushroomsVisibilityToggle =
			GetNode<CheckButton>("%OuterMushroomsVisibilityToggle");
		_outerOtherVisibilityToggle =
			GetNode<CheckButton>("%OuterOtherVisibilityToggle");
		_waterVisibilityToggle = GetNode<CheckButton>("%WaterVisibilityToggle");
		_fishVisibilityToggle = GetNode<CheckButton>("%FishVisibilityToggle");
		_shadowsToggle = GetNode<CheckButton>("%ShadowsToggle");
		_drawCallsValue = GetNode<Label>("%DrawCallsValue");
		_muteToggle = GetNode<CheckButton>("%MuteToggle");
		_fullscreenToggle = GetNode<CheckButton>("%FullscreenToggle");
		_vsyncToggle = GetNode<CheckButton>("%VsyncToggle");
		_resolutionOptions = GetNode<OptionButton>("%ResolutionOptions");
		_renderScaleSlider = GetNode<HSlider>("%RenderScaleSlider");
		_renderScaleValue = GetNode<Label>("%RenderScaleValue");
		_cameraSensitivitySlider = GetNode<HSlider>("%CameraSensitivitySlider");
		_cameraSensitivityValue = GetNode<Label>("%CameraSensitivityValue");
		_zoomSensitivitySlider = GetNode<HSlider>("%ZoomSensitivitySlider");
		_zoomSensitivityValue = GetNode<Label>("%ZoomSensitivityValue");
		_tileFocusDistanceSlider = GetNode<HSlider>("%TileFocusDistanceSlider");
		_tileFocusDistanceValue = GetNode<Label>("%TileFocusDistanceValue");
		_boardOverviewDistanceSlider = GetNode<HSlider>("%BoardOverviewDistanceSlider");
		_boardOverviewDistanceValue = GetNode<Label>("%BoardOverviewDistanceValue");
		_invertVerticalToggle = GetNode<CheckButton>("%InvertVerticalToggle");
		_backButton = GetNode<Button>("%BackButton");

		PopulateResolutionOptions();
		UpdateCameraPitchAvailability();
		LoadSettings();
		SetSection(SettingsSection.Audio);

		_audioTabButton.Pressed += () => SetSection(SettingsSection.Audio);
		_displayTabButton.Pressed += () => SetSection(SettingsSection.Display);
		_controlsTabButton.Pressed += () => SetSection(SettingsSection.Controls);
		_developerTabButton.Pressed += () => SetSection(SettingsSection.Developer);
		_masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
		_musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
		_effectsVolumeSlider.ValueChanged += OnEffectsVolumeChanged;
		_plantingVolumeSlider.ValueChanged += value =>
			OnIndividualVolumeChanged(PlantingBusName, _plantingVolumeValue, value);
		_forestAmbienceVolumeSlider.ValueChanged += value =>
			OnIndividualVolumeChanged(
				ForestAmbienceBusName,
				_forestAmbienceVolumeValue,
				value);
		_waterAmbienceVolumeSlider.ValueChanged += value =>
			OnIndividualVolumeChanged(
				WaterAmbienceBusName,
				_waterAmbienceVolumeValue,
				value);
		_heatAmbienceVolumeSlider.ValueChanged += value =>
			OnIndividualVolumeChanged(
				HeatAmbienceBusName,
				_heatAmbienceVolumeValue,
				value);
		_rainAmbienceVolumeSlider.ValueChanged += value =>
			OnIndividualVolumeChanged(
				RainAmbienceBusName,
				_rainAmbienceVolumeValue,
				value);
		_heavyRainAmbienceVolumeSlider.ValueChanged += value =>
			OnIndividualVolumeChanged(
				HeavyRainAmbienceBusName,
				_heavyRainAmbienceVolumeValue,
				value);
		_muteToggle.Toggled += OnMuteToggled;
		_fullscreenToggle.Toggled += OnFullscreenToggled;
		_vsyncToggle.Toggled += OnVsyncToggled;
		_resolutionOptions.ItemSelected += OnResolutionSelected;
		_renderScaleSlider.ValueChanged += OnRenderScaleChanged;
		_cameraSensitivitySlider.ValueChanged += OnControlSettingChanged;
		_zoomSensitivitySlider.ValueChanged += OnControlSettingChanged;
		_tileFocusDistanceSlider.ValueChanged += OnControlSettingChanged;
		_boardOverviewDistanceSlider.ValueChanged += OnControlSettingChanged;
		_invertVerticalToggle.Toggled += OnInvertVerticalToggled;
		_cameraPitchSlider.ValueChanged += OnCameraPitchChanged;
		_grassVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_tileModelsVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_plantsVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_stoneBorderVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerRingVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerGrassVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerCommonTreeVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerPine1VisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerPine2VisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerPine3VisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerBushVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerFloweringBushVisibilityToggle.Toggled +=
			OnRenderDiagnosticToggled;
		_outerFlowersVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerMushroomsVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_outerOtherVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_waterVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_fishVisibilityToggle.Toggled += OnRenderDiagnosticToggled;
		_shadowsToggle.Toggled += OnRenderDiagnosticToggled;
		_backButton.Pressed += Close;

		UpdateRenderDiagnosticsAvailability();
		UpdateCameraPitchAvailability();
	}

	public override void _Process(double delta)
	{
		if (!Visible || !_developerPage.Visible)
			return;

		int drawCalls = (int)Performance.GetMonitor(
			Performance.Monitor.RenderTotalDrawCallsInFrame);
		_drawCallsValue.Text = $"Zeichenaufrufe gesamt: {drawCalls}";
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!Visible || !inputEvent.IsActionPressed("ui_cancel"))
			return;

		Close();
		GetViewport().SetInputAsHandled();
	}

	public void Open()
	{
		SetSection(SettingsSection.Audio);
		UpdateRenderDiagnosticsAvailability();
		UpdateCameraPitchAvailability();
		Show();
		_audioTabButton.GrabFocus();
	}

	public void Close()
	{
		SaveSettings();
		Hide();
		EmitSignal(SignalName.Closed);
	}

	private void SetSection(SettingsSection section)
	{
		_audioPage.Visible = section == SettingsSection.Audio;
		_displayPage.Visible = section == SettingsSection.Display;
		_controlsPage.Visible = section == SettingsSection.Controls;
		_developerPage.Visible = section == SettingsSection.Developer;
		_audioTabButton.ButtonPressed = section == SettingsSection.Audio;
		_displayTabButton.ButtonPressed = section == SettingsSection.Display;
		_controlsTabButton.ButtonPressed = section == SettingsSection.Controls;
		_developerTabButton.ButtonPressed = section == SettingsSection.Developer;
	}

	private void LoadSettings()
	{
		float masterVolume = GetBusVolume("Master");
		float musicVolume = GetBusVolume(MusicBusName);
		float effectsVolume = GetBusVolume(EffectsBusName);
		float plantingVolume = GetBusVolume(PlantingBusName);
		float forestAmbienceVolume = GetBusVolume(ForestAmbienceBusName);
		float waterAmbienceVolume = GetBusVolume(WaterAmbienceBusName);
		float heatAmbienceVolume = GetBusVolume(HeatAmbienceBusName);
		float rainAmbienceVolume = GetBusVolume(RainAmbienceBusName);
		float heavyRainAmbienceVolume = GetBusVolume(HeavyRainAmbienceBusName);
		bool muted = IsBusMuted("Master");

		DisplayServer.WindowMode windowMode = DisplayServer.WindowGetMode();
		bool fullscreen =
			windowMode == DisplayServer.WindowMode.Fullscreen ||
			windowMode == DisplayServer.WindowMode.ExclusiveFullscreen;
		bool vsyncEnabled =
			DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;
		Vector2I windowSize = DisplayServer.WindowGetSize();
		bool hasSavedResolution = false;
		float renderScale = Mathf.Clamp(
			GetTree().Root.Scaling3DScale,
			MinimumRenderScale,
			MaximumRenderScale);
		float cameraSensitivity = 1.0f;
		float zoomSensitivity = 1.0f;
		float tileFocusDistance = 14.0f;
		float boardOverviewDistance = 0.875f;
		bool invertVertical = false;
		bool dayNightCycleEnabled = true;
		bool fishEnabled = true;
		float cameraStartPitch = _cameraRig?.StartPitchDegrees ??
			(float)_cameraPitchSlider.Value;

		ConfigFile config = new ConfigFile();
		if (config.Load(SettingsPath) == Error.Ok)
		{
			masterVolume = config
				.GetValue("audio", "master_volume", masterVolume)
				.AsSingle();
			musicVolume = config
				.GetValue("audio", "music_volume", musicVolume)
				.AsSingle();
			effectsVolume = config
				.GetValue("audio", "effects_volume", effectsVolume)
				.AsSingle();
			plantingVolume = config
				.GetValue("developer_audio", "planting_volume", plantingVolume)
				.AsSingle();
			forestAmbienceVolume = config
				.GetValue(
					"developer_audio",
					"forest_ambience_volume",
					forestAmbienceVolume)
				.AsSingle();
			waterAmbienceVolume = config
				.GetValue(
					"developer_audio",
					"water_ambience_volume",
					waterAmbienceVolume)
				.AsSingle();
			heatAmbienceVolume = config
				.GetValue(
					"developer_audio",
					"heat_ambience_volume",
					heatAmbienceVolume)
				.AsSingle();
			rainAmbienceVolume = config
				.GetValue(
					"developer_audio",
					"rain_ambience_volume",
					rainAmbienceVolume)
				.AsSingle();
			heavyRainAmbienceVolume = config
				.GetValue(
					"developer_audio",
					"heavy_rain_ambience_volume",
					heavyRainAmbienceVolume)
				.AsSingle();
			muted = config.GetValue("audio", "muted", muted).AsBool();
			fullscreen = config
				.GetValue("display", "fullscreen", fullscreen)
				.AsBool();
			vsyncEnabled = config
				.GetValue("display", "vsync", vsyncEnabled)
				.AsBool();
			hasSavedResolution =
				config.HasSectionKey("display", "window_width") &&
				config.HasSectionKey("display", "window_height");
			windowSize = new Vector2I(
				config.GetValue("display", "window_width", windowSize.X).AsInt32(),
				config.GetValue("display", "window_height", windowSize.Y).AsInt32());
			renderScale = config
				.GetValue("display", "render_scale", renderScale)
				.AsSingle();
			cameraSensitivity = config
				.GetValue("controls", "camera_sensitivity", cameraSensitivity)
				.AsSingle();
			zoomSensitivity = config
				.GetValue("controls", "zoom_sensitivity", zoomSensitivity)
				.AsSingle();
			tileFocusDistance = config
				.GetValue("controls", "tile_focus_distance", tileFocusDistance)
				.AsSingle();
			boardOverviewDistance = config
				.GetValue("controls", "board_overview_distance", boardOverviewDistance)
				.AsSingle();
			invertVertical = config
				.GetValue("controls", "invert_vertical", invertVertical)
				.AsBool();
			dayNightCycleEnabled = config
				.GetValue(
					DeveloperSection,
					DayNightCycleEnabledKey,
					dayNightCycleEnabled)
				.AsBool();
			fishEnabled = config
				.GetValue(DeveloperSection, FishEnabledKey, fishEnabled)
				.AsBool();
			cameraStartPitch = config
				.GetValue(
					DeveloperSection,
					CameraStartPitchKey,
					cameraStartPitch)
				.AsSingle();
		}

		float minimumPitch = _cameraRig?.MinimumPitchDegrees ??
			(float)_cameraPitchSlider.MinValue;
		float maximumPitch = _cameraRig?.MaximumPitchDegrees ??
			(float)_cameraPitchSlider.MaxValue;
		_cameraStartPitchDegrees = Mathf.Clamp(
			cameraStartPitch,
			minimumPitch,
			maximumPitch);
		if (_cameraRig != null && IsInstanceValid(_cameraRig))
		{
			_cameraRig.StartPitchDegrees = _cameraStartPitchDegrees;
			_cameraRig.SetPitchDegrees(_cameraStartPitchDegrees);
		}

		ApplyBusVolume("Master", masterVolume);
		ApplyBusVolume(MusicBusName, musicVolume);
		ApplyBusVolume(EffectsBusName, effectsVolume);
		ApplyBusVolume(PlantingBusName, plantingVolume);
		ApplyBusVolume(ForestAmbienceBusName, forestAmbienceVolume);
		ApplyBusVolume(WaterAmbienceBusName, waterAmbienceVolume);
		ApplyBusVolume(HeatAmbienceBusName, heatAmbienceVolume);
		ApplyBusVolume(RainAmbienceBusName, rainAmbienceVolume);
		ApplyBusVolume(HeavyRainAmbienceBusName, heavyRainAmbienceVolume);
		ApplyMasterMute(muted);
		ApplyFullscreen(fullscreen);
		ApplyVsync(vsyncEnabled);
		ApplyRenderScale(renderScale);
		ApplyControlSettings(
			cameraSensitivity,
			zoomSensitivity,
			tileFocusDistance,
			boardOverviewDistance,
			invertVertical);

		int resolutionIndex = FindClosestResolutionIndex(windowSize);
		_resolutionOptions.Select(resolutionIndex);
		_resolutionOptions.Disabled = fullscreen;

		if (!fullscreen && hasSavedResolution)
			ApplyResolution(WindowSizes[resolutionIndex]);

		_masterVolumeSlider.Value = ToPercent(masterVolume);
		_musicVolumeSlider.Value = ToPercent(musicVolume);
		_effectsVolumeSlider.Value = ToPercent(effectsVolume);
		_plantingVolumeSlider.Value = ToPercent(plantingVolume);
		_forestAmbienceVolumeSlider.Value = ToPercent(forestAmbienceVolume);
		_waterAmbienceVolumeSlider.Value = ToPercent(waterAmbienceVolume);
		_heatAmbienceVolumeSlider.Value = ToPercent(heatAmbienceVolume);
		_rainAmbienceVolumeSlider.Value = ToPercent(rainAmbienceVolume);
		_heavyRainAmbienceVolumeSlider.Value = ToPercent(heavyRainAmbienceVolume);
		_muteToggle.ButtonPressed = muted;
		_fullscreenToggle.ButtonPressed = fullscreen;
		_vsyncToggle.ButtonPressed = vsyncEnabled;
		_renderScaleSlider.Value = Mathf.Clamp(
			renderScale,
			MinimumRenderScale,
			MaximumRenderScale) * 100.0f;
		_renderScaleValue.Text = FormatPercent(_renderScaleSlider.Value);
		_cameraSensitivitySlider.Value = ToSensitivityPercent(cameraSensitivity);
		_zoomSensitivitySlider.Value = ToSensitivityPercent(zoomSensitivity);
		_tileFocusDistanceSlider.Value = Mathf.Clamp(tileFocusDistance, 8.0f, 24.0f);
		_boardOverviewDistanceSlider.Value =
			ToOverviewDistancePercent(boardOverviewDistance);
		_invertVerticalToggle.ButtonPressed = invertVertical;
		_dayNightCycleToggle.ButtonPressed = dayNightCycleEnabled;
		_fishVisibilityToggle.ButtonPressed = fishEnabled;
		UpdateVolumeLabels();
		UpdateDeveloperVolumeLabels();
		UpdateControlLabels();
	}

	private void SaveSettings()
	{
		ConfigFile config = new ConfigFile();
		config.SetValue(
			"audio",
			"master_volume",
			(float)(_masterVolumeSlider.Value / 100.0));
		config.SetValue(
			"audio",
			"music_volume",
			(float)(_musicVolumeSlider.Value / 100.0));
		config.SetValue(
			"audio",
			"effects_volume",
			(float)(_effectsVolumeSlider.Value / 100.0));
		config.SetValue(
			"developer_audio",
			"planting_volume",
			(float)(_plantingVolumeSlider.Value / 100.0));
		config.SetValue(
			"developer_audio",
			"forest_ambience_volume",
			(float)(_forestAmbienceVolumeSlider.Value / 100.0));
		config.SetValue(
			"developer_audio",
			"water_ambience_volume",
			(float)(_waterAmbienceVolumeSlider.Value / 100.0));
		config.SetValue(
			"developer_audio",
			"heat_ambience_volume",
			(float)(_heatAmbienceVolumeSlider.Value / 100.0));
		config.SetValue(
			"developer_audio",
			"rain_ambience_volume",
			(float)(_rainAmbienceVolumeSlider.Value / 100.0));
		config.SetValue(
			"developer_audio",
			"heavy_rain_ambience_volume",
			(float)(_heavyRainAmbienceVolumeSlider.Value / 100.0));
		config.SetValue("audio", "muted", _muteToggle.ButtonPressed);
		config.SetValue("display", "fullscreen", _fullscreenToggle.ButtonPressed);
		config.SetValue("display", "vsync", _vsyncToggle.ButtonPressed);
		config.SetValue(
			"display",
			"render_scale",
			(float)(_renderScaleSlider.Value / 100.0));

		Vector2I selectedResolution = GetSelectedResolution();
		config.SetValue("display", "window_width", selectedResolution.X);
		config.SetValue("display", "window_height", selectedResolution.Y);
		config.SetValue(
			"controls",
			"camera_sensitivity",
			(float)(_cameraSensitivitySlider.Value / 100.0));
		config.SetValue(
			"controls",
			"zoom_sensitivity",
			(float)(_zoomSensitivitySlider.Value / 100.0));
		config.SetValue(
			"controls",
			"tile_focus_distance",
			(float)_tileFocusDistanceSlider.Value);
		config.SetValue(
			"controls",
			"board_overview_distance",
			(float)(_boardOverviewDistanceSlider.Value / 100.0));
		config.SetValue(
			"controls",
			"invert_vertical",
			_invertVerticalToggle.ButtonPressed);
		config.SetValue(
			DeveloperSection,
			DayNightCycleEnabledKey,
			_dayNightCycleToggle.ButtonPressed);
		config.SetValue(
			DeveloperSection,
			FishEnabledKey,
			_fishVisibilityToggle.ButtonPressed);
		config.SetValue(
			DeveloperSection,
			CameraStartPitchKey,
			_cameraStartPitchDegrees);

		Error error = config.Save(SettingsPath);
		if (error != Error.Ok)
			GD.PushWarning($"SettingsMenu: Einstellungen konnten nicht gespeichert werden: {error}");
	}

	internal static bool IsDayNightCycleEnabled()
	{
		ConfigFile config = new ConfigFile();

		if (config.Load(SettingsPath) != Error.Ok)
			return true;

		return config
			.GetValue(DeveloperSection, DayNightCycleEnabledKey, true)
			.AsBool();
	}

	internal static bool IsFishEnabled()
	{
		ConfigFile config = new ConfigFile();

		if (config.Load(SettingsPath) != Error.Ok)
			return true;

		return config
			.GetValue(DeveloperSection, FishEnabledKey, true)
			.AsBool();
	}

	private void OnMasterVolumeChanged(double value)
	{
		ApplyBusVolume("Master", (float)(value / 100.0));
		if (_muteToggle.ButtonPressed)
			ApplyMasterMute(true);
		UpdateVolumeLabels();
	}

	private void OnMusicVolumeChanged(double value)
	{
		ApplyBusVolume(MusicBusName, (float)(value / 100.0));
		UpdateVolumeLabels();
	}

	private void OnEffectsVolumeChanged(double value)
	{
		ApplyBusVolume(EffectsBusName, (float)(value / 100.0));
		UpdateVolumeLabels();
	}

	private static void OnIndividualVolumeChanged(
		string busName,
		Label valueLabel,
		double value)
	{
		ApplyBusVolume(busName, (float)(value / 100.0));
		valueLabel.Text = FormatPercent(value);
	}

	private void OnMuteToggled(bool enabled)
	{
		if (enabled)
		{
			ApplyMasterMute(true);
			return;
		}

		ApplyBusVolume(
			"Master",
			(float)(_masterVolumeSlider.Value / 100.0));
	}

	private void OnFullscreenToggled(bool enabled)
	{
		ApplyFullscreen(enabled);
		_resolutionOptions.Disabled = enabled;

		if (!enabled)
			ApplyResolution(GetSelectedResolution());
	}

	private static void OnVsyncToggled(bool enabled)
	{
		ApplyVsync(enabled);
	}

	private void OnResolutionSelected(long index)
	{
		if (_fullscreenToggle.ButtonPressed)
			return;

		ApplyResolution(WindowSizes[(int)index]);
	}

	private void OnRenderScaleChanged(double value)
	{
		ApplyRenderScale((float)(value / 100.0));
		_renderScaleValue.Text = FormatPercent(value);
	}

	private void OnControlSettingChanged(double value)
	{
		ApplyCurrentControlSettings();
		UpdateControlLabels();
	}

	private void OnInvertVerticalToggled(bool enabled)
	{
		ApplyCurrentControlSettings();
	}

	private void OnRenderDiagnosticToggled(bool enabled)
	{
		ApplyRenderDiagnostics();
	}

	private void UpdateRenderDiagnosticsAvailability()
	{
		Node currentScene = GetTree().CurrentScene;
		bool hasBoard = currentScene?.GetNodeOrNull<BoardManager>("BoardManager") != null;

		_grassVisibilityToggle.Disabled = !hasBoard;
		_tileModelsVisibilityToggle.Disabled = !hasBoard;
		_plantsVisibilityToggle.Disabled = !hasBoard;
		_stoneBorderVisibilityToggle.Disabled = !hasBoard;
		_outerRingVisibilityToggle.Disabled = !hasBoard;
		_outerGrassVisibilityToggle.Disabled = !hasBoard;
		_outerCommonTreeVisibilityToggle.Disabled = !hasBoard;
		_outerPine1VisibilityToggle.Disabled = !hasBoard;
		_outerPine2VisibilityToggle.Disabled = !hasBoard;
		_outerPine3VisibilityToggle.Disabled = !hasBoard;
		_outerBushVisibilityToggle.Disabled = !hasBoard;
		_outerFloweringBushVisibilityToggle.Disabled = !hasBoard;
		_outerFlowersVisibilityToggle.Disabled = !hasBoard;
		_outerMushroomsVisibilityToggle.Disabled = !hasBoard;
		_outerOtherVisibilityToggle.Disabled = !hasBoard;
		_waterVisibilityToggle.Disabled = !hasBoard;
		_fishVisibilityToggle.Disabled =
			currentScene?.GetNodeOrNull<FishSchoolController>("FishController") == null;
		_shadowsToggle.Disabled = !hasBoard;
	}

	private void OnCameraPitchChanged(double value)
	{
		_cameraStartPitchDegrees = (float)value;
		_cameraPitchValue.Text = $"{Mathf.RoundToInt(_cameraStartPitchDegrees)}°";

		if (_cameraRig == null || !IsInstanceValid(_cameraRig))
		{
			UpdateCameraPitchAvailability();
			return;
		}

		_cameraRig.StartPitchDegrees = _cameraStartPitchDegrees;
		_cameraRig.SetPitchDegrees(_cameraStartPitchDegrees);
	}

	private void UpdateCameraPitchAvailability()
	{
		Node currentScene = GetTree().CurrentScene;
		_cameraRig = currentScene?.GetNodeOrNull<CameraRigController>("CameraRig");
		bool hasCameraRig = _cameraRig != null && IsInstanceValid(_cameraRig);
		_cameraPitchSlider.Editable = hasCameraRig;

		if (!hasCameraRig)
		{
			_cameraPitchValue.Text = "Nur im Spiel";
			return;
		}

		_cameraPitchSlider.MinValue = _cameraRig.MinimumPitchDegrees;
		_cameraPitchSlider.MaxValue = _cameraRig.MaximumPitchDegrees;
		float pitch = Mathf.Clamp(
			_cameraRig.CurrentPitchDegrees,
			_cameraRig.MinimumPitchDegrees,
			_cameraRig.MaximumPitchDegrees);
		_cameraPitchSlider.SetValueNoSignal(pitch);
		_cameraPitchValue.Text = $"{Mathf.RoundToInt(pitch)}°";
	}

	private void ApplyRenderDiagnostics()
	{
		Node currentScene = GetTree().CurrentScene;
		BoardManager boardManager =
			currentScene?.GetNodeOrNull<BoardManager>("BoardManager");

		if (boardManager == null)
			return;

		boardManager.SetRenderGroupVisibility(
			_grassVisibilityToggle.ButtonPressed,
			_tileModelsVisibilityToggle.ButtonPressed,
			_plantsVisibilityToggle.ButtonPressed,
			_stoneBorderVisibilityToggle.ButtonPressed,
			_outerRingVisibilityToggle.ButtonPressed);

		OuterRingVisualGroup visibleOuterRingGroups =
			OuterRingVisualGroup.None;
		if (_outerGrassVisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.OuterGrass;
		if (_outerCommonTreeVisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.CommonTree;
		if (_outerPine1VisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.Pine1;
		if (_outerPine2VisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.Pine2;
		if (_outerPine3VisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.Pine3;
		if (_outerBushVisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.Bush;
		if (_outerFloweringBushVisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.FloweringBush;
		if (_outerFlowersVisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.Flowers;
		if (_outerMushroomsVisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.Mushrooms;
		if (_outerOtherVisibilityToggle.ButtonPressed)
			visibleOuterRingGroups |= OuterRingVisualGroup.Other;

		boardManager.SetOuterRingDetailVisibility(
			_outerRingVisibilityToggle.ButtonPressed,
			visibleOuterRingGroups);

		Node3D water = currentScene.GetNodeOrNull<Node3D>("StylizedWater");
		if (water != null)
			water.Visible = _waterVisibilityToggle.ButtonPressed;

		FishSchoolController fishController =
			currentScene.GetNodeOrNull<FishSchoolController>("FishController");
		fishController?.SetFishEnabled(_fishVisibilityToggle.ButtonPressed);

		DirectionalLight3D directionalLight =
			currentScene.GetNodeOrNull<DirectionalLight3D>("DirectionalLight3D");
		if (directionalLight != null)
			directionalLight.ShadowEnabled = _shadowsToggle.ButtonPressed;
	}

	private void ApplyCurrentControlSettings()
	{
		ApplyControlSettings(
			(float)(_cameraSensitivitySlider.Value / 100.0),
			(float)(_zoomSensitivitySlider.Value / 100.0),
			(float)_tileFocusDistanceSlider.Value,
			(float)(_boardOverviewDistanceSlider.Value / 100.0),
			_invertVerticalToggle.ButtonPressed);
	}

	private void PopulateResolutionOptions()
	{
		_resolutionOptions.Clear();

		foreach (Vector2I windowSize in WindowSizes)
			_resolutionOptions.AddItem($"{windowSize.X} × {windowSize.Y}");
	}

	private void UpdateVolumeLabels()
	{
		_masterVolumeValue.Text = FormatPercent(_masterVolumeSlider.Value);
		_musicVolumeValue.Text = FormatPercent(_musicVolumeSlider.Value);
		_effectsVolumeValue.Text = FormatPercent(_effectsVolumeSlider.Value);
	}

	private void UpdateDeveloperVolumeLabels()
	{
		_plantingVolumeValue.Text = FormatPercent(_plantingVolumeSlider.Value);
		_forestAmbienceVolumeValue.Text =
			FormatPercent(_forestAmbienceVolumeSlider.Value);
		_waterAmbienceVolumeValue.Text =
			FormatPercent(_waterAmbienceVolumeSlider.Value);
		_heatAmbienceVolumeValue.Text =
			FormatPercent(_heatAmbienceVolumeSlider.Value);
		_rainAmbienceVolumeValue.Text =
			FormatPercent(_rainAmbienceVolumeSlider.Value);
		_heavyRainAmbienceVolumeValue.Text =
			FormatPercent(_heavyRainAmbienceVolumeSlider.Value);
	}

	private void UpdateControlLabels()
	{
		_cameraSensitivityValue.Text = FormatPercent(_cameraSensitivitySlider.Value);
		_zoomSensitivityValue.Text = FormatPercent(_zoomSensitivitySlider.Value);
		_tileFocusDistanceValue.Text = FormatDistance(_tileFocusDistanceSlider.Value);
		_boardOverviewDistanceValue.Text = FormatPercent(_boardOverviewDistanceSlider.Value);
	}

	private Vector2I GetSelectedResolution()
	{
		int selectedIndex = Mathf.Clamp(
			_resolutionOptions.Selected,
			0,
			WindowSizes.Length - 1);

		return WindowSizes[selectedIndex];
	}

	private static int FindClosestResolutionIndex(Vector2I size)
	{
		int closestIndex = 0;
		long closestDifference = long.MaxValue;

		for (int index = 0; index < WindowSizes.Length; index++)
		{
			long widthDifference = WindowSizes[index].X - size.X;
			long heightDifference = WindowSizes[index].Y - size.Y;
			long difference =
				widthDifference * widthDifference +
				heightDifference * heightDifference;

			if (difference >= closestDifference)
				continue;

			closestDifference = difference;
			closestIndex = index;
		}

		return closestIndex;
	}

	private static void EnsureAudioBus(
		string busName,
		string sendBusName = "Master")
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
		{
			AudioServer.AddBus();
			busIndex = AudioServer.BusCount - 1;
			AudioServer.SetBusName(busIndex, busName);
		}

		AudioServer.SetBusSend(busIndex, sendBusName);
	}

	private static float GetBusVolume(string busName)
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
			return 1.0f;

		return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
	}

	private static bool IsBusMuted(string busName)
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		return busIndex >= 0 && AudioServer.IsBusMute(busIndex);
	}

	private static void ApplyBusVolume(string busName, float linearVolume)
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
			return;

		float volume = Mathf.Clamp(linearVolume, 0.0f, 1.0f);
		bool muted = volume <= 0.001f;
		AudioServer.SetBusMute(busIndex, muted);

		if (!muted)
			AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(volume));
	}

	private static void ApplyMasterMute(bool enabled)
	{
		int busIndex = AudioServer.GetBusIndex("Master");
		if (busIndex >= 0)
			AudioServer.SetBusMute(busIndex, enabled);
	}

	private static void ApplyFullscreen(bool enabled)
	{
		DisplayServer.WindowMode currentMode = DisplayServer.WindowGetMode();
		bool isFullscreen =
			currentMode == DisplayServer.WindowMode.Fullscreen ||
			currentMode == DisplayServer.WindowMode.ExclusiveFullscreen;

		if (isFullscreen == enabled)
			return;

		DisplayServer.WindowSetMode(
			enabled
				? DisplayServer.WindowMode.Fullscreen
				: DisplayServer.WindowMode.Windowed);

		if (!enabled)
			DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
	}

	private static void ApplyResolution(Vector2I size)
	{
		if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed)
			return;

		DisplayServer.WindowSetSize(size);
	}

	private static void ApplyVsync(bool enabled)
	{
		DisplayServer.WindowSetVsyncMode(
			enabled
				? DisplayServer.VSyncMode.Enabled
				: DisplayServer.VSyncMode.Disabled);
	}

	private void ApplyRenderScale(float scale)
	{
		Viewport rootViewport = GetTree()?.Root;
		if (rootViewport == null)
			return;

		rootViewport.Scaling3DScale = Mathf.Clamp(
			scale,
			MinimumRenderScale,
			MaximumRenderScale);
	}

	private static void ApplyControlSettings(
		float cameraSensitivity,
		float zoomSensitivity,
		float tileFocusDistance,
		float boardOverviewDistance,
		bool invertVertical)
	{
		ProjectSettings.SetSetting(
			CameraSensitivitySetting,
			Mathf.Clamp(cameraSensitivity, 0.5f, 2.0f));
		ProjectSettings.SetSetting(
			ZoomSensitivitySetting,
			Mathf.Clamp(zoomSensitivity, 0.5f, 2.0f));
		ProjectSettings.SetSetting(
			TileFocusDistanceSetting,
			Mathf.Clamp(tileFocusDistance, 8.0f, 24.0f));
		ProjectSettings.SetSetting(
			BoardOverviewDistanceSetting,
			Mathf.Clamp(boardOverviewDistance, 0.5f, 1.5f));
		ProjectSettings.SetSetting(InvertVerticalSetting, invertVertical);
	}

	private static float ToPercent(float linearVolume)
	{
		return Mathf.Clamp(linearVolume, 0.0f, 1.0f) * 100.0f;
	}

	private static float ToSensitivityPercent(float sensitivity)
	{
		return Mathf.Clamp(sensitivity, 0.5f, 2.0f) * 100.0f;
	}

	private static float ToOverviewDistancePercent(float distanceMultiplier)
	{
		return Mathf.Clamp(distanceMultiplier, 0.5f, 1.5f) * 100.0f;
	}

	private static string FormatPercent(double value)
	{
		return $"{Mathf.RoundToInt(value)} %";
	}

	private static string FormatDistance(double value)
	{
		return $"{value:0.0}";
	}
}
