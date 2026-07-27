using Godot;

public partial class SettingsMenu : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	private const string SettingsPath = "user://settings.cfg";

	private HSlider _masterVolumeSlider;
	private Label _masterVolumeValue;
	private CheckButton _fullscreenToggle;
	private CheckButton _vsyncToggle;
	private Button _backButton;

	public override void _Ready()
	{
		_masterVolumeSlider = GetNode<HSlider>("%MasterVolumeSlider");
		_masterVolumeValue = GetNode<Label>("%MasterVolumeValue");
		_fullscreenToggle = GetNode<CheckButton>("%FullscreenToggle");
		_vsyncToggle = GetNode<CheckButton>("%VsyncToggle");
		_backButton = GetNode<Button>("%BackButton");

		LoadSettings();

		_masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
		_fullscreenToggle.Toggled += OnFullscreenToggled;
		_vsyncToggle.Toggled += OnVsyncToggled;
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
		Show();
		_backButton.GrabFocus();
	}

	public void Close()
	{
		SaveSettings();
		Hide();
		EmitSignal(SignalName.Closed);
	}

	private void LoadSettings()
	{
		int masterBus = AudioServer.GetBusIndex("Master");
		float currentVolume = masterBus >= 0
			? Mathf.DbToLinear(AudioServer.GetBusVolumeDb(masterBus))
			: 1.0f;

		DisplayServer.WindowMode windowMode = DisplayServer.WindowGetMode();
		bool currentFullscreen =
			windowMode == DisplayServer.WindowMode.Fullscreen ||
			windowMode == DisplayServer.WindowMode.ExclusiveFullscreen;
		bool currentVsync =
			DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;

		ConfigFile config = new ConfigFile();
		if (config.Load(SettingsPath) == Error.Ok)
		{
			currentVolume = config
				.GetValue("audio", "master_volume", currentVolume)
				.AsSingle();
			currentFullscreen = config
				.GetValue("display", "fullscreen", currentFullscreen)
				.AsBool();
			currentVsync = config
				.GetValue("display", "vsync", currentVsync)
				.AsBool();

			ApplyMasterVolume(currentVolume);
			ApplyFullscreen(currentFullscreen);
			ApplyVsync(currentVsync);
		}

		_masterVolumeSlider.Value = Mathf.Clamp(currentVolume, 0.0f, 1.0f) * 100.0f;
		_fullscreenToggle.ButtonPressed = currentFullscreen;
		_vsyncToggle.ButtonPressed = currentVsync;
		UpdateVolumeLabel();
	}

	private void SaveSettings()
	{
		ConfigFile config = new ConfigFile();
		config.SetValue(
			"audio",
			"master_volume",
			(float)(_masterVolumeSlider.Value / 100.0));
		config.SetValue(
			"display",
			"fullscreen",
			_fullscreenToggle.ButtonPressed);
		config.SetValue(
			"display",
			"vsync",
			_vsyncToggle.ButtonPressed);

		Error error = config.Save(SettingsPath);
		if (error != Error.Ok)
			GD.PushWarning($"SettingsMenu: Einstellungen konnten nicht gespeichert werden: {error}");
	}

	private void OnMasterVolumeChanged(double value)
	{
		ApplyMasterVolume((float)(value / 100.0));
		UpdateVolumeLabel();
	}

	private void OnFullscreenToggled(bool enabled)
	{
		ApplyFullscreen(enabled);
	}

	private void OnVsyncToggled(bool enabled)
	{
		ApplyVsync(enabled);
	}

	private void UpdateVolumeLabel()
	{
		_masterVolumeValue.Text = $"{Mathf.RoundToInt(_masterVolumeSlider.Value)} %";
	}

	private static void ApplyMasterVolume(float linearVolume)
	{
		int masterBus = AudioServer.GetBusIndex("Master");
		if (masterBus < 0)
			return;

		float clampedVolume = Mathf.Clamp(linearVolume, 0.0f, 1.0f);
		bool muted = clampedVolume <= 0.001f;
		AudioServer.SetBusMute(masterBus, muted);

		if (!muted)
			AudioServer.SetBusVolumeDb(masterBus, Mathf.LinearToDb(clampedVolume));
	}

	private static void ApplyFullscreen(bool enabled)
	{
		DisplayServer.WindowSetMode(
			enabled
				? DisplayServer.WindowMode.Fullscreen
				: DisplayServer.WindowMode.Windowed);

		if (!enabled)
			DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
	}

	private static void ApplyVsync(bool enabled)
	{
		DisplayServer.WindowSetVsyncMode(
			enabled
				? DisplayServer.VSyncMode.Enabled
				: DisplayServer.VSyncMode.Disabled);
	}
}
