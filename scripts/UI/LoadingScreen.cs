using Godot;

public partial class LoadingScreen : Control
{
	private const string GameScenePath = "res://scenes/Main.tscn";
	private const string MainMenuScenePath = "res://scenes/UI/MainMenu.tscn";
	private const float DisplayTime = 1.1f;

	private Label _statusLabel;
	private ProgressBar _progressBar;
	private float _elapsedTime;
	private bool _isChangingScene;

	public override void _Ready()
	{
		_statusLabel = GetNode<Label>("%StatusLabel");
		_progressBar = GetNode<ProgressBar>("%ProgressBar");

		if (SceneTransition.Instance != null)
			SceneTransition.Instance.SceneChangeFailed += OnSceneChangeFailed;
	}

	public override void _ExitTree()
	{
		if (SceneTransition.Instance != null)
			SceneTransition.Instance.SceneChangeFailed -= OnSceneChangeFailed;
	}

	public override void _Process(double delta)
	{
		if (_isChangingScene)
			return;

		_elapsedTime += (float)delta;
		float progress = Mathf.Clamp(_elapsedTime / DisplayTime, 0.0f, 1.0f);

		_progressBar.Value = progress * 100.0f;
		UpdateStatusText(progress);

		if (progress >= 1.0f)
			OpenGameScene();
	}

	private void UpdateStatusText(float progress)
	{
		if (progress < 0.25f)
			_statusLabel.Text = "Boden wird vorbereitet …";
		else if (progress < 0.5f)
			_statusLabel.Text = "Pflanzen werden geladen …";
		else if (progress < 0.75f)
			_statusLabel.Text = "Karten werden gemischt …";
		else
			_statusLabel.Text = "Ökosystem wird aufgebaut …";
	}

	private void OpenGameScene()
	{
		_isChangingScene = true;
		_statusLabel.Text = "Spielwelt wird gestartet …";

		if (SceneTransition.Instance != null)
		{
			SceneTransition.Instance.ChangeScene(GameScenePath);
			return;
		}

		Error error = GetTree().ChangeSceneToFile(GameScenePath);
		if (error != Error.Ok)
			ReturnToMainMenu($"Spielwelt konnte nicht geöffnet werden: {error}");
	}

	private void OnSceneChangeFailed(string scenePath, string reason)
	{
		if (_isChangingScene && scenePath == GameScenePath)
			ReturnToMainMenu($"Spielwelt konnte nicht geöffnet werden: {reason}");
	}

	private void ReturnToMainMenu(string message)
	{
		MainMenu.SetPendingStartError(message);
		GD.PushError($"LoadingScreen: {message}");

		Error error = GetTree().ChangeSceneToFile(MainMenuScenePath);
		if (error == Error.Ok)
			return;

		_isChangingScene = false;
		_statusLabel.Text = message;
		_statusLabel.AddThemeColorOverride(
			"font_color",
			new Color(1.0f, 0.58f, 0.48f));
		GD.PushError($"LoadingScreen: Rückkehr zum Hauptmenü fehlgeschlagen: {error}");
	}
}
