using Godot;

public partial class SettingsMenu : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	private const string SettingsPath = "user://settings.cfg";
	private const string MusicBusName = "Music";
	private const string EffectsBusName = "Effects";

	private static readonly Vector2I[] WindowSizes =
	{
		new Vector2I(1280, 720),
		new Vector2I(1600, 900),
		new Vector2I(1920, 1080),
		new Vector2I(2560, 1440)
	};

	private HSlider _masterVolumeSlider;
	private Label _masterVolumeValue;
	private HSlider _musicVolumeSlider;
	private Label _musicVolumeValue;
	private HSlider _effectsVolumeSlider;
	private Label _effectsVolumeValue;
	private CheckButton _fullscreenToggle;
	private OptionButton _resolutionOptions;
	private Button _backButton;

	public override void _Ready()
	{
		EnsureAudioBus(MusicBusName);
		EnsureAudioBus(EffectsBusName);

		_masterVolumeSlider = GetNode<HSlider>("%MasterVolumeSlider");
		_masterVolumeValue = GetNode<Label>("%MasterVolumeValue");
		_musicVolumeSlider = GetNode<HSlider>("%MusicVolumeSlider");
		_musicVolumeValue = GetNode<Label>("%MusicVolumeValue");
		_effectsVolumeSlider = GetNode<HSlider>("%EffectsVolumeSlider");
		_effectsVolumeValue = GetNode<Label>("%EffectsVolumeValue");
		_fullscreenToggle = GetNode<CheckButton>("%FullscreenToggle");
		_resolutionOptions = GetNode<OptionButton>("%ResolutionOptions");
		_backButton = GetNode<Button>("%BackButton");

		PopulateResolutionOptions();
		LoadSettings();

		_masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
		_musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
		_effectsVolumeSlider.ValueChanged += OnEffectsVolumeChanged;
		_fullscreenToggle.Toggled += OnFullscreenToggled;
		_resolutionOptions.ItemSelected += OnResolutionSelected;
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
		float masterVolume = GetBusVolume("Master");
		float musicVolume = GetBusVolume(MusicBusName);
		float effectsVolume = GetBusVolume(EffectsBusName);

		DisplayServer.WindowMode windowMode = DisplayServer.WindowGetMode();
		bool fullscreen =
			windowMode == DisplayServer.WindowMode.Fullscreen ||
			windowMode == DisplayServer.WindowMode.ExclusiveFullscreen;
		Vector2I windowSize = DisplayServer.WindowGetSize();
		bool hasSavedResolution = false;

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
			fullscreen = config
				.GetValue("display", "fullscreen", fullscreen)
				.AsBool();
			hasSavedResolution =
				config.HasSectionKey("display", "window_width") &&
				config.HasSectionKey("display", "window_height");
			windowSize = new Vector2I(
				config.GetValue("display", "window_width", windowSize.X).AsInt32(),
				config.GetValue("display", "window_height", windowSize.Y).AsInt32());
		}

		ApplyBusVolume("Master", masterVolume);
		ApplyBusVolume(MusicBusName, musicVolume);
		ApplyBusVolume(EffectsBusName, effectsVolume);
		ApplyFullscreen(fullscreen);

		int resolutionIndex = FindClosestResolutionIndex(windowSize);
		_resolutionOptions.Select(resolutionIndex);
		_resolutionOptions.Disabled = fullscreen;

		if (!fullscreen && hasSavedResolution)
			ApplyResolution(WindowSizes[resolutionIndex]);

		_masterVolumeSlider.Value = ToPercent(masterVolume);
		_musicVolumeSlider.Value = ToPercent(musicVolume);
		_effectsVolumeSlider.Value = ToPercent(effectsVolume);
		_fullscreenToggle.ButtonPressed = fullscreen;
		UpdateVolumeLabels();
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
			"display",
			"fullscreen",
			_fullscreenToggle.ButtonPressed);

		Vector2I selectedResolution = GetSelectedResolution();
		config.SetValue("display", "window_width", selectedResolution.X);
		config.SetValue("display", "window_height", selectedResolution.Y);

		Error error = config.Save(SettingsPath);
		if (error != Error.Ok)
			GD.PushWarning($"SettingsMenu: Einstellungen konnten nicht gespeichert werden: {error}");
	}

	private void OnMasterVolumeChanged(double value)
	{
		ApplyBusVolume("Master", (float)(value / 100.0));
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

	private void OnFullscreenToggled(bool enabled)
	{
		ApplyFullscreen(enabled);
		_resolutionOptions.Disabled = enabled;

		if (!enabled)
			ApplyResolution(GetSelectedResolution());
	}

	private void OnResolutionSelected(long index)
	{
		if (_fullscreenToggle.ButtonPressed)
			return;

		ApplyResolution(WindowSizes[(int)index]);
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

		if (AudioServer.IsBusMute(busIndex))
			return 0.0f;

		return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
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

	private static float ToPercent(float linearVolume)
	{
		return Mathf.Clamp(linearVolume, 0.0f, 1.0f) * 100.0f;
	}

	private static string FormatPercent(double value)
	{
		return $"{Mathf.RoundToInt(value)} %";
	}
}
