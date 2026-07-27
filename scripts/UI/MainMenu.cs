using Godot;

public partial class MainMenu : Control
{
	private const string LoadingScenePath = "res://scenes/UI/LoadingScreen.tscn";
	private const string GameScenePath = "res://scenes/Main.tscn";

	private Button _startButton;
	private Button _settingsButton;
	private Button _quitButton;
	private SettingsMenu _settingsMenu;
	private bool _isChangingScene;

	public override void _Ready()
	{
		_startButton = GetNodeOrNull<Button>("%StartButton");
		_settingsButton = GetNodeOrNull<Button>("%SettingsButton");
		_quitButton = GetNodeOrNull<Button>("%QuitButton");
		_settingsMenu = GetNodeOrNull<SettingsMenu>("SettingsMenu");

		if (_startButton == null || _settingsButton == null || _quitButton == null)
		{
			GD.PushError("MainMenu: Mindestens ein Menübutton fehlt.");
			return;
		}

		_startButton.Pressed += OnStartPressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;

		if (_settingsMenu != null)
			_settingsMenu.Closed += OnSettingsClosed;
		else
			GD.PushWarning("MainMenu: SettingsMenu fehlt.");

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
		_settingsButton.Disabled = disabled;
		_quitButton.Disabled = disabled;
	}
}
