using Godot;

public partial class MainMenu : Control
{
	private const string LoadingScenePath = "res://scenes/UI/LoadingScreen.tscn";
	private const string GameScenePath = "res://scenes/Main.tscn";

	private Button _startButton;
	private Button _encyclopediaButton;
	private Button _settingsButton;
	private Button _quitButton;
	private EncyclopediaMenu _encyclopediaMenu;
	private SettingsMenu _settingsMenu;
	private bool _isChangingScene;

	public override void _Ready()
	{
		_startButton = GetNodeOrNull<Button>("%StartButton");
		_encyclopediaButton = GetNodeOrNull<Button>("%EncyclopediaButton");
		_settingsButton = GetNodeOrNull<Button>("%SettingsButton");
		_quitButton = GetNodeOrNull<Button>("%QuitButton");
		_encyclopediaMenu = GetNodeOrNull<EncyclopediaMenu>("EncyclopediaMenu");
		_settingsMenu = GetNodeOrNull<SettingsMenu>("SettingsMenu");

		if (_startButton == null ||
			_encyclopediaButton == null ||
			_settingsButton == null ||
			_quitButton == null)
		{
			GD.PushError("MainMenu: Mindestens ein Menübutton fehlt.");
			return;
		}

		_startButton.Pressed += OnStartPressed;
		_encyclopediaButton.Pressed += OnEncyclopediaPressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;

		if (_settingsMenu != null)
			_settingsMenu.Closed += OnSettingsClosed;
		else
			GD.PushWarning("MainMenu: SettingsMenu fehlt.");

		if (_encyclopediaMenu != null)
			_encyclopediaMenu.Closed += OnEncyclopediaClosed;
		else
			GD.PushWarning("MainMenu: EncyclopediaMenu fehlt.");

		_startButton.GrabFocus();
	}

	private void OnStartPressed()
	{
		if (_isChangingScene)
			return;

		_isChangingScene = true;
		SetMenuButtonsDisabled(true);

		string targetScene = ResourceLoader.Exists(LoadingScenePath)
			? LoadingScenePath
			: GameScenePath;

		if (!ResourceLoader.Exists(targetScene))
		{
			GD.PushError($"MainMenu: Zielszene fehlt: {targetScene}");
			_isChangingScene = false;
			SetMenuButtonsDisabled(false);
			return;
		}

		if (SceneTransition.Instance != null)
		{
			SceneTransition.Instance.ChangeScene(targetScene);
			return;
		}

		Error error = GetTree().ChangeSceneToFile(targetScene);
		if (error != Error.Ok)
		{
			GD.PushError($"MainMenu: Szenenwechsel fehlgeschlagen: {error}");
			_isChangingScene = false;
			SetMenuButtonsDisabled(false);
		}
	}

	private void OnSettingsPressed()
	{
		if (_settingsMenu == null)
			return;

		SetMenuButtonsDisabled(true);
		_settingsMenu.Open();
	}

	private void OnEncyclopediaPressed()
	{
		if (_encyclopediaMenu == null)
			return;

		SetMenuButtonsDisabled(true);
		_encyclopediaMenu.Open();
	}

	private void OnEncyclopediaClosed()
	{
		SetMenuButtonsDisabled(false);
		_encyclopediaButton.GrabFocus();
	}

	private void OnSettingsClosed()
	{
		SetMenuButtonsDisabled(false);
		_settingsButton.GrabFocus();
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	private void SetMenuButtonsDisabled(bool disabled)
	{
		_startButton.Disabled = disabled;
		_encyclopediaButton.Disabled = disabled;
		_settingsButton.Disabled = disabled;
		_quitButton.Disabled = disabled;
	}
}
