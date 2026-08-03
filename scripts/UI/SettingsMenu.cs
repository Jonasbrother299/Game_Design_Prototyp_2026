using Godot;

public partial class SettingsMenu : Control
{
	private enum SettingsSection
	{
		Audio,
		Display,
		Controls
	}

	[Signal]
	public delegate void ClosedEventHandler();

	private const string SettingsPath = "user://settings.cfg";
	private const string MusicBusName = "Music";
	private const string EffectsBusName = "Effects";
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
	private Control _audioPage;
	private Control _displayPage;
	private Control _controlsPage;
	private HSlider _masterVolumeSlider;
	private Label _masterVolumeValue;
	private HSlider _musicVolumeSlider;
	private Label _musicVolumeValue;
	private HSlider _effectsVolumeSlider;
	private Label _effectsVolumeValue;
	private CheckButton _muteToggle;
	private CheckButton _fullscreenToggle;
	private CheckButton _vsyncToggle;
	private OptionButton _resolutionOptions;
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

	public override void _Ready()
	{
		EnsureAudioBus(MusicBusName);
		EnsureAudioBus(EffectsBusName);

		_audioTabButton = GetNode<Button>("%AudioTabButton");
		_displayTabButton = GetNode<Button>("%DisplayTabButton");
		_controlsTabButton = GetNode<Button>("%ControlsTabButton");
		_audioPage = GetNode<Control>("%AudioPage");
		_displayPage = GetNode<Control>("%DisplayPage");
		_controlsPage = GetNode<Control>("%ControlsPage");
		_masterVolumeSlider = GetNode<HSlider>("%MasterVolumeSlider");
		_masterVolumeValue = GetNode<Label>("%MasterVolumeValue");
		_musicVolumeSlider = GetNode<HSlider>("%MusicVolumeSlider");
		_musicVolumeValue = GetNode<Label>("%MusicVolumeValue");
		_effectsVolumeSlider = GetNode<HSlider>("%EffectsVolumeSlider");
		_effectsVolumeValue = GetNode<Label>("%EffectsVolumeValue");
		_muteToggle = GetNode<CheckButton>("%MuteToggle");
		_fullscreenToggle = GetNode<CheckButton>("%FullscreenToggle");
		_vsyncToggle = GetNode<CheckButton>("%VsyncToggle");
		_resolutionOptions = GetNode<OptionButton>("%ResolutionOptions");
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
		LoadSettings();
		SetSection(SettingsSection.Audio);

		_audioTabButton.Pressed += () => SetSection(SettingsSection.Audio);
		_displayTabButton.Pressed += () => SetSection(SettingsSection.Display);
		_controlsTabButton.Pressed += () => SetSection(SettingsSection.Controls);
		_masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
		_musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
		_effectsVolumeSlider.ValueChanged += OnEffectsVolumeChanged;
		_muteToggle.Toggled += OnMuteToggled;
		_fullscreenToggle.Toggled += OnFullscreenToggled;
		_vsyncToggle.Toggled += OnVsyncToggled;
		_resolutionOptions.ItemSelected += OnResolutionSelected;
		_cameraSensitivitySlider.ValueChanged += OnControlSettingChanged;
		_zoomSensitivitySlider.ValueChanged += OnControlSettingChanged;
		_tileFocusDistanceSlider.ValueChanged += OnControlSettingChanged;
		_boardOverviewDistanceSlider.ValueChanged += OnControlSettingChanged;
		_invertVerticalToggle.Toggled += OnInvertVerticalToggled;
		_backButton.Pressed += Close;
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
		_audioTabButton.ButtonPressed = section == SettingsSection.Audio;
		_displayTabButton.ButtonPressed = section == SettingsSection.Display;
		_controlsTabButton.ButtonPressed = section == SettingsSection.Controls;
	}

	private void LoadSettings()
	{
		float masterVolume = GetBusVolume("Master");
		float musicVolume = GetBusVolume(MusicBusName);
		float effectsVolume = GetBusVolume(EffectsBusName);
		bool muted = IsBusMuted("Master");

		DisplayServer.WindowMode windowMode = DisplayServer.WindowGetMode();
		bool fullscreen =
			windowMode == DisplayServer.WindowMode.Fullscreen ||
			windowMode == DisplayServer.WindowMode.ExclusiveFullscreen;
		bool vsyncEnabled =
			DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;
		Vector2I windowSize = DisplayServer.WindowGetSize();
		bool hasSavedResolution = false;
		float cameraSensitivity = 1.0f;
		float zoomSensitivity = 1.0f;
		float tileFocusDistance = 14.0f;
		float boardOverviewDistance = 0.875f;
		bool invertVertical = false;

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
		}

		ApplyBusVolume("Master", masterVolume);
		ApplyBusVolume(MusicBusName, musicVolume);
		ApplyBusVolume(EffectsBusName, effectsVolume);
		ApplyMasterMute(muted);
		ApplyFullscreen(fullscreen);
		ApplyVsync(vsyncEnabled);
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
		_muteToggle.ButtonPressed = muted;
		_fullscreenToggle.ButtonPressed = fullscreen;
		_vsyncToggle.ButtonPressed = vsyncEnabled;
		_cameraSensitivitySlider.Value = ToSensitivityPercent(cameraSensitivity);
		_zoomSensitivitySlider.Value = ToSensitivityPercent(zoomSensitivity);
		_tileFocusDistanceSlider.Value = Mathf.Clamp(tileFocusDistance, 8.0f, 24.0f);
		_boardOverviewDistanceSlider.Value =
			ToOverviewDistancePercent(boardOverviewDistance);
		_invertVerticalToggle.ButtonPressed = invertVertical;
		UpdateVolumeLabels();
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
		config.SetValue("audio", "muted", _muteToggle.ButtonPressed);
		config.SetValue("display", "fullscreen", _fullscreenToggle.ButtonPressed);
		config.SetValue("display", "vsync", _vsyncToggle.ButtonPressed);

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

		Error error = config.Save(SettingsPath);
		if (error != Error.Ok)
			GD.PushWarning($"SettingsMenu: Einstellungen konnten nicht gespeichert werden: {error}");
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

	private void OnControlSettingChanged(double value)
	{
		ApplyCurrentControlSettings();
		UpdateControlLabels();
	}

	private void OnInvertVerticalToggled(bool enabled)
	{
		ApplyCurrentControlSettings();
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

	private static void EnsureAudioBus(string busName)
	{
		if (AudioServer.GetBusIndex(busName) >= 0)
			return;

		AudioServer.AddBus();
		int busIndex = AudioServer.BusCount - 1;
		AudioServer.SetBusName(busIndex, busName);
		AudioServer.SetBusSend(busIndex, "Master");
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
