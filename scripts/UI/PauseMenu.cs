using Godot;
using System.Collections.Generic;

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
	private Control _controlsOverlay;
	private Control _confirmOverlay;
	private Button _resumeButton;
	private Button _settingsButton;
	private Button _controlsButton;
	private Button _encyclopediaButton;
	private Button _restartButton;
	private Button _tutorialButton;
	private Button _mainMenuButton;
	private Button _quitButton;
	private Button _controlsBackButton;
	private Button _cancelButton;
	private Button _confirmButton;
	private Label _roundValue;
	private Label _waterValue;
	private Label _eventValue;
	private Label _confirmTitle;
	private Label _confirmMessage;
	private SettingsMenu _settingsMenu;
	private EncyclopediaMenu _encyclopediaMenu;
	private GameHub _gameHub;
	private TurnManager _turnManager;
	private PendingAction _pendingAction;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_pausePanel = GetNode<PanelContainer>("%PausePanel");
		_controlsOverlay = GetNode<Control>("%ControlsOverlay");
		_confirmOverlay = GetNode<Control>("%ConfirmOverlay");
		_resumeButton = GetNode<Button>("%ResumeButton");
		_settingsButton = GetNode<Button>("%SettingsButton");
		_controlsButton = GetNode<Button>("%ControlsButton");
		_encyclopediaButton = GetNode<Button>("%EncyclopediaButton");
		_restartButton = GetNode<Button>("%RestartButton");
		_tutorialButton = GetNode<Button>("%TutorialButton");
		_mainMenuButton = GetNode<Button>("%MainMenuButton");
		_quitButton = GetNode<Button>("%QuitButton");
		_controlsBackButton = GetNode<Button>("%ControlsBackButton");
		_cancelButton = GetNode<Button>("%CancelButton");
		_confirmButton = GetNode<Button>("%ConfirmButton");
		_roundValue = GetNode<Label>("%RoundValue");
		_waterValue = GetNode<Label>("%WaterValue");
		_eventValue = GetNode<Label>("%EventValue");
		_confirmTitle = GetNode<Label>("%ConfirmTitle");
		_confirmMessage = GetNode<Label>("%ConfirmMessage");
		_settingsMenu = GetNode<SettingsMenu>("SettingsMenu");
		_encyclopediaMenu = GetNode<EncyclopediaMenu>("EncyclopediaMenu");

		_resumeButton.Pressed += ClosePauseMenu;
		_settingsButton.Pressed += OpenSettings;
		_controlsButton.Pressed += OpenControls;
		_encyclopediaButton.Pressed += OpenEncyclopedia;
		_restartButton.Pressed += RequestRestart;
		_tutorialButton.Pressed += RequestTutorialReplay;
		_mainMenuButton.Pressed += RequestMainMenu;
		_quitButton.Pressed += RequestQuit;
		_controlsBackButton.Pressed += CloseControls;
		_cancelButton.Pressed += CancelConfirmation;
		_confirmButton.Pressed += ConfirmPendingAction;
		_settingsMenu.Closed += OnSettingsClosed;
		_encyclopediaMenu.Closed += OnEncyclopediaClosed;

		_gameHub = GetTree().CurrentScene?.GetNodeOrNull<GameHub>("UI/CanvasLayer/GameHub");
		if (_gameHub != null)
			_gameHub.MenuRequested += OpenPauseMenu;
		else
			GD.PushWarning("PauseMenu: GameHub fehlt.");

		_turnManager = GetTree().CurrentScene?.GetNodeOrNull<TurnManager>("TurnManager");
		Hide();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!inputEvent.IsActionPressed("ui_cancel"))
			return;

		if (_confirmOverlay.Visible)
			CancelConfirmation();
		else if (_encyclopediaMenu.Visible)
			_encyclopediaMenu.Close();
		else if (_controlsOverlay.Visible)
			CloseControls();
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
		if (_gameHub != null)
			_gameHub.MenuRequested -= OpenPauseMenu;

		GetTree().Paused = false;
	}

	public void OpenPauseMenu()
	{
		if (Visible)
			return;

		UpdateGameState();
		Show();
		ShowPausePanel();
		GetTree().Paused = true;
		_resumeButton.GrabFocus();
	}

	private void ClosePauseMenu()
	{
		_settingsMenu.Hide();
		_encyclopediaMenu.Hide();
		_controlsOverlay.Hide();
		_confirmOverlay.Hide();
		Hide();
		GetTree().Paused = false;
	}

	private void UpdateGameState()
	{
		GameState state = _turnManager?.State;
		if (state == null)
		{
			_roundValue.Text = "–";
			_waterValue.Text = "–";
			_eventValue.Text = "Keines";
			return;
		}

		_roundValue.Text = state.CurrentRound.ToString();
		_waterValue.Text = state.Water.ToString();

		if (state.ActiveEvents.Count == 0)
		{
			_eventValue.Text = "Keines";
			return;
		}

		List<string> activeEventTexts = new();
		foreach (ActiveGameEvent activeEvent in state.ActiveEvents)
		{
			string eventName = activeEvent.Definition?.DisplayName ?? "Unbekannt";
			activeEventTexts.Add(
				$"{eventName} ({activeEvent.RemainingRounds} R.)");
		}

		_eventValue.Text = string.Join(", ", activeEventTexts);
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

	private void OpenControls()
	{
		_pausePanel.Hide();
		_controlsOverlay.Show();
		_controlsBackButton.GrabFocus();
	}

	private void CloseControls()
	{
		_controlsOverlay.Hide();
		ShowPausePanel();
		_controlsButton.GrabFocus();
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
		_controlsOverlay.Hide();
		_confirmOverlay.Hide();
		_pausePanel.Show();
	}
}
