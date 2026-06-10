using Godot;
using System.Collections.Generic;

public partial class LoadingScreen : Control
{
	private const string GameScenePath = "res://scenes/Main.tscn";

	[Export] public float LoadDuration = 2.0f;

	private float _timer = 0.0f;

	private Label _titleLabel;
	private Label _statusLabel;
	private Label _debugLabel;
	private ProgressBar _progressBar;

	private readonly List<string> _debugLines = new();

	public override void _Ready()
	{
		GD.Print("LoadingScreen loaded.");

		BuildDebugUI();
		RunChecks();
	}

	public override void _Process(double delta)
	{
		_timer += (float)delta;

		float progress = Mathf.Clamp(_timer / LoadDuration, 0.0f, 1.0f);

		_progressBar.Value = progress * 100.0f;

		if (progress < 0.25f)
			_statusLabel.Text = "Boden wird vorbereitet...";
		else if (progress < 0.5f)
			_statusLabel.Text = "Karten werden geladen...";
		else if (progress < 0.75f)
			_statusLabel.Text = "Ökosystem wird aufgebaut...";
		else
			_statusLabel.Text = "Spielwelt wird gestartet...";

		if (_timer >= LoadDuration)
		{
			TryLoadGameScene();
		}
	}

	private void BuildDebugUI()
	{
		// Hintergrund
		ColorRect background = new ColorRect();
		background.Color = new Color(0.03f, 0.12f, 0.0f, 1.0f);
		background.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(background);

		// Center Container
		CenterContainer center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		VBoxContainer box = new VBoxContainer();
		box.CustomMinimumSize = new Vector2(900, 500);
		box.AddThemeConstantOverride("separation", 20);
		center.AddChild(box);

		_titleLabel = new Label();
		_titleLabel.Text = "Loading Debug";
		_titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_titleLabel.AddThemeFontSizeOverride("font_size", 42);
		box.AddChild(_titleLabel);

		_statusLabel = new Label();
		_statusLabel.Text = "Starte...";
		_statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_statusLabel.AddThemeFontSizeOverride("font_size", 26);
		box.AddChild(_statusLabel);

		_progressBar = new ProgressBar();
		_progressBar.MinValue = 0;
		_progressBar.MaxValue = 100;
		_progressBar.Value = 0;
		_progressBar.CustomMinimumSize = new Vector2(700, 30);
		box.AddChild(_progressBar);

		_debugLabel = new Label();
		_debugLabel.Text = "";
		_debugLabel.HorizontalAlignment = HorizontalAlignment.Left;
		_debugLabel.AddThemeFontSizeOverride("font_size", 18);
		box.AddChild(_debugLabel);
	}

	private void RunChecks()
	{
		AddDebug("Checking scene paths...");

		CheckPath(GameScenePath);
		CheckPath("res://assets/cards/card_baum.jpeg");
		CheckPath("res://assets/cards/card_flechte.jpeg");
		CheckPath("res://assets/cards/card_moos.png");
		CheckPath("res://assets/cards/card_pilz.jpeg");

		AddDebug("Checks finished.");
	}

	private void CheckPath(string path)
	{
		bool exists = ResourceLoader.Exists(path);

		if (exists)
			AddDebug("FOUND: " + path);
		else
			AddDebug("MISSING: " + path);
	}

	private void AddDebug(string message)
	{
		GD.Print(message);

		_debugLines.Add(message);
		_debugLabel.Text = string.Join("\n", _debugLines);
	}

	private void TryLoadGameScene()
	{
		SetProcess(false);

		AddDebug("Trying to load: " + GameScenePath);

		if (!ResourceLoader.Exists(GameScenePath))
		{
			AddDebug("ERROR: Main scene not found.");
			AddDebug("Expected path: " + GameScenePath);
			return;
		}

		Error error = GetTree().ChangeSceneToFile(GameScenePath);

		if (error != Error.Ok)
		{
			AddDebug("ERROR while changing scene: " + error);
			return;
		}

		AddDebug("Scene change requested.");
	}
}
