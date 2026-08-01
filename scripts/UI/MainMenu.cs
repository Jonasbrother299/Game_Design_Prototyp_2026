using Godot;

public partial class MainMenu : Control
{
	private const string LoadingScenePath = "res://scenes/UI/LoadingScreen.tscn";
	private const string GameScenePath = "res://scenes/Main.tscn";
	private static string _pendingStartError = "";

	private Button _playButton;
	private Button _achievementsButton;
	private Button _encyclopediaButton;
	private Button _settingsButton;
	private Button _quitButton;
	private Label _errorLabel;
	private EncyclopediaMenu _encyclopediaMenu;
	private AchievementMenu _achievementMenu;
	private SettingsMenu _settingsMenu;
	private bool _isChangingScene;

	public override void _Ready()
	{
		_playButton = GetNodeOrNull<Button>("%PlayButton");
		_achievementsButton = GetNodeOrNull<Button>("%AchievementsButton");
		_encyclopediaButton = GetNodeOrNull<Button>("%EncyclopediaButton");
		_settingsButton = GetNodeOrNull<Button>("%SettingsButton");
		_quitButton = GetNodeOrNull<Button>("%QuitButton");
		_errorLabel = GetNodeOrNull<Label>("%MenuErrorLabel");
		_encyclopediaMenu = GetNodeOrNull<EncyclopediaMenu>("EncyclopediaMenu");
		_achievementMenu = GetNodeOrNull<AchievementMenu>("AchievementMenu");
		_settingsMenu = GetNodeOrNull<SettingsMenu>("SettingsMenu");

		if (!AreRequiredNodesAvailable())
			return;

		_playButton.Pressed += StartNewGame;
		_achievementsButton.Pressed += OpenAchievements;
		_encyclopediaButton.Pressed += OpenEncyclopedia;
		_settingsButton.Pressed += OpenSettings;
		_quitButton.Pressed += QuitGame;
		_settingsMenu.Closed += OnSettingsClosed;
		_encyclopediaMenu.Closed += OnEncyclopediaClosed;
		_achievementMenu.Closed += OnAchievementsClosed;

		if (SceneTransition.Instance != null)
			SceneTransition.Instance.SceneChangeFailed += OnSceneChangeFailed;

		ShowPendingStartError();
		_playButton.GrabFocus();
	}

	public static void SetPendingStartError(string message)
	{
		_pendingStartError = message ?? "";
	}

	public override void _ExitTree()
	{
		if (_playButton != null)
			_playButton.Pressed -= StartNewGame;
		if (_achievementsButton != null)
			_achievementsButton.Pressed -= OpenAchievements;
		if (_encyclopediaButton != null)
			_encyclopediaButton.Pressed -= OpenEncyclopedia;
		if (_settingsButton != null)
			_settingsButton.Pressed -= OpenSettings;
		if (_quitButton != null)
			_quitButton.Pressed -= QuitGame;
		if (_settingsMenu != null)
			_settingsMenu.Closed -= OnSettingsClosed;
		if (_encyclopediaMenu != null)
			_encyclopediaMenu.Closed -= OnEncyclopediaClosed;
		if (_achievementMenu != null)
			_achievementMenu.Closed -= OnAchievementsClosed;
		if (SceneTransition.Instance != null)
			SceneTransition.Instance.SceneChangeFailed -= OnSceneChangeFailed;
	}

	private bool AreRequiredNodesAvailable()
	{
		if (_playButton != null && _achievementsButton != null && _encyclopediaButton != null &&
			_settingsButton != null && _quitButton != null &&
			_errorLabel != null && _achievementMenu != null && _settingsMenu != null &&
			_encyclopediaMenu != null)
		{
			return true;
		}

		GD.PushError("MainMenu: Mindestens ein benötigter Menü-Node fehlt.");
		return false;
	}

	private void StartNewGame()
	{
		string targetScene = ResourceLoader.Exists(LoadingScenePath)
			? LoadingScenePath
			: GameScenePath;
		if (!ResourceLoader.Exists(targetScene))
		{
			ShowError($"Die Zielszene fehlt: {targetScene}");
			return;
		}

		ChangeToGameScene(targetScene);
	}

	private void ChangeToGameScene(string targetScene)
	{
		if (_isChangingScene)
			return;

		_isChangingScene = true;
		SetMenuButtonsDisabled(true);

		if (SceneTransition.Instance != null)
		{
			SceneTransition.Instance.ChangeScene(targetScene);
			return;
		}

		Error error = GetTree().ChangeSceneToFile(targetScene);
		if (error == Error.Ok)
			return;

		_isChangingScene = false;
		SetMenuButtonsDisabled(false);
		ShowError($"Szenenwechsel fehlgeschlagen: {error}");
	}

	private void OnSceneChangeFailed(string scenePath, string reason)
	{
		if (!_isChangingScene ||
			(scenePath != LoadingScenePath && scenePath != GameScenePath))
		{
			return;
		}

		_isChangingScene = false;
		SetMenuButtonsDisabled(false);
		ShowError($"Szenenwechsel fehlgeschlagen: {reason}");
	}

	private void OpenSettings()
	{
		SetMenuButtonsDisabled(true);
		_settingsMenu.Open();
	}

	private void OpenAchievements()
	{
		SetMenuButtonsDisabled(true);
		_achievementMenu.Open();
	}

	private void OpenEncyclopedia()
	{
		SetMenuButtonsDisabled(true);
		_encyclopediaMenu.Open();
	}

	private void OnSettingsClosed()
	{
		SetMenuButtonsDisabled(false);
		_settingsButton.GrabFocus();
	}

	private void OnEncyclopediaClosed()
	{
		SetMenuButtonsDisabled(false);
		_encyclopediaButton.GrabFocus();
	}

	private void OnAchievementsClosed()
	{
		SetMenuButtonsDisabled(false);
		_achievementsButton.GrabFocus();
	}

	private void QuitGame()
	{
		GetTree().Quit();
	}

	private void ShowError(string message)
	{
		_errorLabel.Text = message;
		_errorLabel.Show();
		GD.PushError($"MainMenu: {message}");
	}

	private void ClearError()
	{
		_errorLabel.Text = "";
		_errorLabel.Hide();
	}

	private void ShowPendingStartError()
	{
		if (string.IsNullOrWhiteSpace(_pendingStartError))
		{
			ClearError();
			return;
		}

		string message = _pendingStartError;
		_pendingStartError = "";
		ShowError(message);
	}

	private void SetMenuButtonsDisabled(bool disabled)
	{
		_playButton.Disabled = disabled;
		_achievementsButton.Disabled = disabled;
		_encyclopediaButton.Disabled = disabled;
		_settingsButton.Disabled = disabled;
		_quitButton.Disabled = disabled;
	}
}
