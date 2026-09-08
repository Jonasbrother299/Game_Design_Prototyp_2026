using Godot;

public partial class LoadingScreen : Control
{
	private const string GameScenePath = "res://scenes/Main.tscn";
	private const string MainMenuScenePath = "res://scenes/UI/MainMenu.tscn";
	private const float MinimumDisplayTime = 0.65f;
	private const float ResourceLoadProgressShare = 0.9f;
	private const float ProgressCatchUpPerSecond = 3.0f;
	private const float SceneFadeDuration = 0.55f;
	private const float TipDisplayDuration = 6.0f;
	private static readonly string[] LoadingTips =
	{
		"Mit Escape pausierst du das Spiel.",
		"Mit der Schaufel holst du Pflanzen in Stufe 1 als Karte zurück.",
		"Im Lexikon stehen die Werte und Effekte entdeckter Karten.",
		"Blau zeigt die Wasserproduktion, Rot den Wasserverbrauch.",
		"Moos sammelt Wasser, sobald es ausgewachsen ist.",
		"Ausgewachsene Blumen fördern die Ausbreitung ihrer Nachbarn.",
		"Pilze können die Wasserproduktion benachbarter Pflanzen erhöhen."
	};

	private Label _statusLabel;
	private ProgressBar _progressBar;
	private Control _layout;
	private readonly Godot.Collections.Array _loadProgress = new();
	private float _elapsedTime;
	private float _tipElapsedTime;
	private int _tipIndex;
	private float _displayedProgress;
	private ulong _resourceLoadStartedUsec;
	private int _lastLoggedProgressBucket = -10;
	private bool _resourceLoadFinishedLogged;
	private bool _isChangingScene;

	public override void _EnterTree()
	{
		Resized += UpdateLayout;
	}

	public override void _Ready()
	{
		LoadProfiler.StartSession();
		_statusLabel = GetNode<Label>("%StatusLabel");
		_progressBar = GetNode<ProgressBar>("%ProgressBar");
		_layout = GetNode<Control>("Layout");
		UpdateLayout();
		_tipIndex = GD.RandRange(0, LoadingTips.Length - 1);
		_statusLabel.Text = LoadingTips[_tipIndex];

		_resourceLoadStartedUsec = LoadProfiler.BeginPhase(
			"Main.tscn einschließlich Abhängigkeiten laden");
		ulong requestStartedUsec = LoadProfiler.BeginPhase(
			"Asynchrone Ladeanfrage starten");
		Error error = ResourceLoader.LoadThreadedRequest(
			GameScenePath,
			"PackedScene");
		LoadProfiler.EndPhase(
			"Asynchrone Ladeanfrage starten",
			requestStartedUsec);
		if (error != Error.Ok)
		{
			ReturnToMainMenu(
				$"Spielwelt konnte nicht geladen werden: {error}");
			return;
		}
	}

	public override void _ExitTree()
	{
		Resized -= UpdateLayout;
	}

	private void UpdateLayout()
	{
		if (_layout == null || Size.X <= 0.0f || Size.Y <= 0.0f)
			return;

		float scale = Mathf.Min(Size.X / 1920.0f, Size.Y / 1080.0f);
		_layout.Size = Size / scale;
		_layout.Scale = Vector2.One * scale;
	}

	public override void _Process(double delta)
	{
		if (_isChangingScene)
			return;

		_elapsedTime += (float)delta;
		UpdateTip((float)delta);
		_loadProgress.Clear();
		ResourceLoader.ThreadLoadStatus loadStatus =
			ResourceLoader.LoadThreadedGetStatus(
				GameScenePath,
				_loadProgress);

		switch (loadStatus)
		{
			case ResourceLoader.ThreadLoadStatus.InProgress:
				float progress = _loadProgress.Count > 0
					? Mathf.Clamp(_loadProgress[0].AsSingle(), 0.0f, 1.0f)
					: 0.0f;
				LogResourceProgress(progress);
				UpdateDisplayedProgress(progress, (float)delta);
				break;
			case ResourceLoader.ThreadLoadStatus.Loaded:
				if (!_resourceLoadFinishedLogged)
				{
					_resourceLoadFinishedLogged = true;
					LoadProfiler.EndPhase(
						"Main.tscn einschließlich Abhängigkeiten laden",
						_resourceLoadStartedUsec);
				}

				CompleteDisplayedProgress();
				if (_elapsedTime >= MinimumDisplayTime)
					OpenGameScene();
				break;
			case ResourceLoader.ThreadLoadStatus.Failed:
			case ResourceLoader.ThreadLoadStatus.InvalidResource:
				ReturnToMainMenu(
					$"Spielwelt konnte nicht geladen werden: {loadStatus}");
				break;
		}
	}

	private void LogResourceProgress(float progress)
	{
		int progressBucket = Mathf.Clamp(
			Mathf.FloorToInt(progress * 10.0f) * 10,
			0,
			100);
		if (progressBucket <= _lastLoggedProgressBucket)
			return;

		_lastLoggedProgressBucket = progressBucket;
		LoadProfiler.Mark($"Ressourcenladen: {progressBucket} %");
	}

	private void CompleteDisplayedProgress()
	{
		_displayedProgress = 1.0f;
		_progressBar.Value = 100.0f;
	}

	private void UpdateDisplayedProgress(float resourceProgress, float delta)
	{
		float timeProgress = Mathf.Clamp(
			_elapsedTime / MinimumDisplayTime,
			0.0f,
			1.0f);
		float targetProgress = Mathf.Min(resourceProgress, timeProgress) *
			ResourceLoadProgressShare;
		_displayedProgress = Mathf.MoveToward(
			_displayedProgress,
			targetProgress,
			ProgressCatchUpPerSecond * delta);

		_progressBar.Value = _displayedProgress * 100.0f;
	}

	private void UpdateTip(float delta)
	{
		_tipElapsedTime += delta;
		if (_tipElapsedTime < TipDisplayDuration)
			return;

		_tipElapsedTime %= TipDisplayDuration;
		_tipIndex = (_tipIndex + 1) % LoadingTips.Length;
		_statusLabel.Text = LoadingTips[_tipIndex];
	}

	private async void OpenGameScene()
	{
		_isChangingScene = true;
		ulong phaseStartedUsec = LoadProfiler.BeginPhase(
			"Geladene PackedScene übernehmen");
		PackedScene gameScene =
			ResourceLoader.LoadThreadedGet(GameScenePath) as PackedScene;
		LoadProfiler.EndPhase(
			"Geladene PackedScene übernehmen",
			phaseStartedUsec);
		if (gameScene == null)
		{
			ReturnToMainMenu("Die geladene Spielwelt ist ungültig.");
			return;
		}

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Main.tscn instanziieren");
		Node gameRoot = gameScene.Instantiate();
		LoadProfiler.EndPhase(
			"Main.tscn instanziieren",
			phaseStartedUsec);
		if (gameRoot == null)
		{
			ReturnToMainMenu("Die Spielwelt konnte nicht aufgebaut werden.");
			return;
		}

		SceneTree tree = GetTree();
		Window root = tree.Root;
		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Spielwelt in SceneTree einhängen und _Ready ausführen");
		root.AddChild(gameRoot);
		tree.CurrentScene = gameRoot;
		LoadProfiler.EndPhase(
			"Spielwelt in SceneTree einhängen und _Ready ausführen",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Ladebildschirm für Übergang vorbereiten");
		CanvasLayer fadeLayer = new CanvasLayer
		{
			Layer = 1000
		};
		root.AddChild(fadeLayer);
		Reparent(fadeLayer, false);

		_progressBar.Value = 100.0f;
		LoadProfiler.EndPhase(
			"Ladebildschirm für Übergang vorbereiten",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Erstes Frame der Spielwelt");
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		LoadProfiler.EndPhase(
			"Erstes Frame der Spielwelt",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Ladebildschirm ausblenden");
		Tween fadeTween = CreateTween();
		fadeTween.SetTrans(Tween.TransitionType.Cubic);
		fadeTween.SetEase(Tween.EaseType.InOut);
		fadeTween.TweenProperty(
			this,
			"modulate:a",
			0.0f,
			SceneFadeDuration);
		await ToSignal(fadeTween, Tween.SignalName.Finished);
		LoadProfiler.EndPhase(
			"Ladebildschirm ausblenden",
			phaseStartedUsec);
		fadeLayer.QueueFree();
		LoadProfiler.FinishSession("Ladevorgang abgeschlossen");
	}

	private void ReturnToMainMenu(string message)
	{
		_isChangingScene = true;
		LoadProfiler.FinishSession($"Ladevorgang abgebrochen: {message}");
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
