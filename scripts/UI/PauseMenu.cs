using Godot;

public partial class PauseMenu : Control
{
	private enum PendingAction
	{
		None,
		Restart,
		ReplayTutorial,
		MainMenu,
		Quit
	}

	private const string MainScenePath = "res://scenes/Main.tscn";
	private const string MainMenuScenePath = "res://scenes/UI/MainMenu.tscn";

	private PanelContainer _pausePanel;
	private Control _confirmOverlay;
	private Button _resumeButton;
	private TextureButton _settingsButton;
	private Button _encyclopediaButton;
	private Button _restartButton;
	private Button _tutorialButton;
	private Button _mainMenuButton;
	private Button _quitButton;
	private Button _cancelButton;
	private Button _confirmButton;
	private Label _confirmTitle;
	private Label _confirmMessage;
	private SettingsMenu _settingsMenu;
	private EncyclopediaMenu _encyclopediaMenu;
	private TextureButton _musicToggle;
	private TextureButton _soundToggle;
	private CenterContainer[] _scaledCenters;
	private GameHub _gameHub;
	private PendingAction _pendingAction;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_pausePanel = GetNode<PanelContainer>("%PausePanel");
		_confirmOverlay = GetNode<Control>("%ConfirmOverlay");
		_resumeButton = GetNode<Button>("%ResumeButton");
		_settingsButton = GetNode<TextureButton>("%SettingsButton");
		_encyclopediaButton = GetNode<Button>("%EncyclopediaButton");
		_restartButton = GetNode<Button>("%RestartButton");
		_tutorialButton = GetNode<Button>("%TutorialButton");
		_mainMenuButton = GetNode<Button>("%MainMenuButton");
		_quitButton = GetNode<Button>("%QuitButton");
		_cancelButton = GetNode<Button>("%CancelButton");
		_confirmButton = GetNode<Button>("%ConfirmButton");
		_confirmTitle = GetNode<Label>("%ConfirmTitle");
		_confirmMessage = GetNode<Label>("%ConfirmMessage");
		_settingsMenu = GetNode<SettingsMenu>("SettingsMenu");
		_encyclopediaMenu = GetNode<EncyclopediaMenu>("EncyclopediaMenu");
		_musicToggle = GetNode<TextureButton>("%MusicToggle");
		_soundToggle = GetNode<TextureButton>("%SoundToggle");
		_scaledCenters = new[]
		{
			GetNode<CenterContainer>("CenterContainer"),
			GetNode<CenterContainer>("ConfirmOverlay/CenterContainer")
		};
		Resized += UpdatePauseLayout;
		UpdatePauseLayout();
		UpdateAudioShortcuts();

		_resumeButton.Pressed += ClosePauseMenu;
		_musicToggle.Toggled += OnMusicMuteToggled;
		_soundToggle.Toggled += OnSoundMuteToggled;
		_settingsButton.Pressed += OpenSettings;
		_encyclopediaButton.Pressed += OpenEncyclopedia;
		_restartButton.Pressed += RequestRestart;
		_tutorialButton.Pressed += RequestTutorialReplay;
		_mainMenuButton.Pressed += RequestMainMenu;
		_quitButton.Pressed += RequestQuit;
		_cancelButton.Pressed += CancelConfirmation;
		_confirmButton.Pressed += ConfirmPendingAction;
		_settingsMenu.Closed += OnSettingsClosed;
		_encyclopediaMenu.Closed += OnEncyclopediaClosed;

		_gameHub = GetNodeOrNull<GameHub>("../../CanvasLayer/GameHub");
		if (_gameHub != null)
			_gameHub.MenuRequested += OpenPauseMenu;
		else
			GD.PushWarning("PauseMenu: GameHub fehlt.");

		Hide();
	}

	public override void _Process(double delta)
	{
		if (Visible && _pausePanel.Visible)
			UpdateAudioShortcuts();
	}

	private void UpdatePauseLayout()
	{
		if (_scaledCenters == null || Size.X <= 0.0f || Size.Y <= 0.0f)
			return;

		foreach (CenterContainer center in _scaledCenters)
		{
			Vector2 panelSize = center.GetChild<Control>(0).GetCombinedMinimumSize();
			float scale = Mathf.Min(1.0f, Mathf.Min(
				Size.X * 0.92f / Mathf.Max(panelSize.X, 1.0f),
				Size.Y * 0.94f / Mathf.Max(panelSize.Y, 1.0f)));
			center.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
			center.Size = Size / scale;
			center.Scale = Vector2.One * scale;
		}
	}

	private void OnMusicMuteToggled(bool muted)
	{
		int busIndex = AudioServer.GetBusIndex("Music");
		if (busIndex >= 0)
			AudioServer.SetBusMute(busIndex, muted);
		UpdateAudioShortcuts();
	}

	private void OnSoundMuteToggled(bool muted)
	{
		_settingsMenu.SetMasterMuted(muted);
		UpdateAudioShortcuts();
	}

	private void UpdateAudioShortcuts()
	{
		int musicBus = AudioServer.GetBusIndex("Music");
		int masterBus = AudioServer.GetBusIndex("Master");
		_musicToggle.SetPressedNoSignal(musicBus >= 0 && AudioServer.IsBusMute(musicBus));
		_soundToggle.SetPressedNoSignal(masterBus >= 0 && AudioServer.IsBusMute(masterBus));
		_musicToggle.TooltipText = _musicToggle.ButtonPressed
			? "Musik einschalten" : "Musik stummschalten";
		_soundToggle.TooltipText = _soundToggle.ButtonPressed
			? "Stummschaltung aufheben" : "Gesamten Ton stummschalten";
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!inputEvent.IsActionPressed("ui_cancel"))
			return;

		if (_confirmOverlay.Visible)
			CancelConfirmation();
		else if (_encyclopediaMenu.Visible)
			_encyclopediaMenu.Close();
		else if (_settingsMenu.Visible)
			_settingsMenu.Close();
		else if (Visible)
			ClosePauseMenu();
		else
			OpenPauseMenu();

		GetViewport().SetInputAsHandled();
	}

	public override void _ExitTree()
	{
		Resized -= UpdatePauseLayout;
		if (_musicToggle != null)
			_musicToggle.Toggled -= OnMusicMuteToggled;
		if (_soundToggle != null)
			_soundToggle.Toggled -= OnSoundMuteToggled;
		if (_gameHub != null)
			_gameHub.MenuRequested -= OpenPauseMenu;

		GetTree().Paused = false;
	}

	public void OpenPauseMenu()
	{
		if (Visible)
			return;

		Show();
		ShowPausePanel();
		UpdatePauseLayout();
		UpdateAudioShortcuts();
		GetTree().Paused = true;
		_resumeButton.GrabFocus();
	}

	private void ClosePauseMenu()
	{
		_settingsMenu.Hide();
		_encyclopediaMenu.Hide();
		_confirmOverlay.Hide();
		Hide();
		GetTree().Paused = false;
	}

	private void OpenSettings()
	{
		_pausePanel.Hide();
		_settingsMenu.Open();
	}

	private void OnSettingsClosed()
	{
		ShowPausePanel();
		_settingsButton.GrabFocus();
	}

	private void OpenEncyclopedia()
	{
		_pausePanel.Hide();
		_encyclopediaMenu.Open();
	}

	private void OnEncyclopediaClosed()
	{
		ShowPausePanel();
		_encyclopediaButton.GrabFocus();
	}

	private void RequestRestart()
	{
		ShowConfirmation(
			PendingAction.Restart,
			"Partie neu starten?",
			"Der aktuelle Fortschritt geht verloren.",
			"Neu starten");
	}

	private void RequestMainMenu()
	{
		ShowConfirmation(
			PendingAction.MainMenu,
			"Zum Hauptmenü?",
			"Die laufende Partie wird beendet.",
			"Hauptmenü");
	}

	private void RequestTutorialReplay()
	{
		ShowConfirmation(
			PendingAction.ReplayTutorial,
			"Tutorial wiederholen?",
			"Die aktuelle Partie wird beendet. Das Tutorial startet mit einer neuen Partie.",
			"Tutorial starten");
	}

	private void RequestQuit()
	{
		ShowConfirmation(
			PendingAction.Quit,
			"Spiel beenden?",
			"Die laufende Partie wird beendet und das Spiel geschlossen.",
			"Beenden");
	}

	private void ShowConfirmation(
		PendingAction pendingAction,
		string title,
		string message,
		string confirmText)
	{
		_pendingAction = pendingAction;
		_confirmTitle.Text = title;
		_confirmMessage.Text = message;
		_confirmButton.Text = confirmText;

		_pausePanel.Hide();
		_confirmOverlay.Show();
		UpdatePauseLayout();
		_cancelButton.GrabFocus();
	}

	private void CancelConfirmation()
	{
		_pendingAction = PendingAction.None;
		_confirmOverlay.Hide();
		ShowPausePanel();
		_resumeButton.GrabFocus();
	}

	private void ConfirmPendingAction()
	{
		PendingAction action = _pendingAction;
		_pendingAction = PendingAction.None;

		switch (action)
		{
			case PendingAction.Restart:
				GameManager.SkipTutorialOnNextStart();
				ChangeScene(MainScenePath, "Partie");
				break;
			case PendingAction.ReplayTutorial:
				GameManager.ReplayTutorialOnNextStart();
				ChangeScene(MainScenePath, "Tutorial");
				break;
			case PendingAction.MainMenu:
				GameManager.ClearTutorialSkipRequest();
				ChangeScene(MainMenuScenePath, "Hauptmenü");
				break;
			case PendingAction.Quit:
				GetTree().Paused = false;
				GetTree().Quit();
				break;
		}
	}

	private void ChangeScene(string scenePath, string sceneName)
	{
		GetTree().Paused = false;

		if (SceneTransition.Instance != null)
		{
			SceneTransition.Instance.ChangeScene(scenePath);
			return;
		}

		Error error = GetTree().ChangeSceneToFile(scenePath);
		if (error == Error.Ok)
			return;

		GD.PushError($"PauseMenu: {sceneName} konnte nicht geöffnet werden: {error}");
		GetTree().Paused = true;
		_confirmOverlay.Hide();
		ShowPausePanel();
	}

	private void ShowPausePanel()
	{
		_encyclopediaMenu.Hide();
		_confirmOverlay.Hide();
		_pausePanel.Show();
	}
}
