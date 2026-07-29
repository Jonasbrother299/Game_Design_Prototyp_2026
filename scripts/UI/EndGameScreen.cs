using Godot;

public partial class EndGameScreen : Control
{
	private const string MainScenePath = "res://scenes/Main.tscn";
	private const string MainMenuScenePath = "res://scenes/UI/MainMenu.tscn";

	[ExportGroup("Colors")]
	[Export] public Color VictoryColor = new Color(0.78f, 0.86f, 0.47f);
	[Export] public Color DefeatColor = new Color(0.84f, 0.48f, 0.34f);

	private PanelContainer _resultPanel;
	private Label _outcomeLabel;
	private Label _titleLabel;
	private Label _messageLabel;
	private Label _roundValue;
	private Label _waterValue;
	private Control _dimmer;
	private Control _centerContainer;
	private Button _viewBoardButton;
	private Button _restoreResultButton;
	private Button _restartButton;
	private Button _mainMenuButton;
	private CameraRigController _cameraRig;
	private ProcessModeEnum _cameraRigProcessMode;
	private TurnManager _turnManager;
	private Tween _entranceTween;
	private bool _isInspectingBoard;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_resultPanel = GetNode<PanelContainer>("%ResultPanel");
		_outcomeLabel = GetNode<Label>("%OutcomeLabel");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_messageLabel = GetNode<Label>("%MessageLabel");
		_roundValue = GetNode<Label>("%RoundValue");
		_waterValue = GetNode<Label>("%WaterValue");
		_dimmer = GetNode<Control>("Dimmer");
		_centerContainer = GetNode<Control>("CenterContainer");
		_viewBoardButton = GetNode<Button>("%ViewBoardButton");
		_restoreResultButton = GetNode<Button>("%RestoreResultButton");
		_restartButton = GetNode<Button>("%RestartButton");
		_mainMenuButton = GetNode<Button>("%MainMenuButton");

		_viewBoardButton.Pressed += ShowBoard;
		_restoreResultButton.Pressed += ShowResultPanel;
		_restartButton.Pressed += RestartGame;
		_mainMenuButton.Pressed += OpenMainMenu;

		_cameraRig = GetTree().CurrentScene?.GetNodeOrNull<CameraRigController>(
			"CameraRig");

		if (_cameraRig != null)
			_cameraRigProcessMode = _cameraRig.ProcessMode;

		_turnManager = GetTree().CurrentScene?.GetNodeOrNull<TurnManager>(
			"TurnManager");

		if (_turnManager != null)
			_turnManager.GameEnded += ShowResult;
		else
			GD.PushWarning("EndGameScreen: TurnManager fehlt.");

		Hide();
	}

	public override void _ExitTree()
	{
		if (_turnManager != null)
			_turnManager.GameEnded -= ShowResult;

		if (_viewBoardButton != null)
			_viewBoardButton.Pressed -= ShowBoard;

		if (_restoreResultButton != null)
			_restoreResultButton.Pressed -= ShowResultPanel;

		if (_restartButton != null)
			_restartButton.Pressed -= RestartGame;

		if (_mainMenuButton != null)
			_mainMenuButton.Pressed -= OpenMainMenu;

		if (_entranceTween != null && _entranceTween.IsValid())
			_entranceTween.Kill();

		if (_cameraRig != null && IsInstanceValid(_cameraRig))
		{
			_cameraRig.SetInteractionEnabled(true);
			_cameraRig.ProcessMode = _cameraRigProcessMode;
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!Visible || !inputEvent.IsActionPressed("ui_cancel"))
			return;

		if (_isInspectingBoard)
			ShowResultPanel();
		else
			_restartButton.GrabFocus();

		GetViewport().SetInputAsHandled();
	}

	private void ShowResult(GameState state)
	{
		if (state == null || Visible)
			return;

		bool hasWon = state.HasWon;
		int targetWater = _turnManager?.Config?.WinWaterLimit ?? 50;

		_outcomeLabel.Text = hasWon ? "SIEG" : "NIEDERLAGE";
		_titleLabel.Text = hasWon
			? "Ökosystem gerettet"
			: "Die alte Eiche ist verdorrt";
		_messageLabel.Text = hasWon
			? $"Der Wasserstand hat {targetWater} erreicht. Die alte Eiche kann weiterleben."
			: "Der Wasservorrat ist auf 0 gefallen. Das Ökosystem konnte die Eiche nicht versorgen.";
		_roundValue.Text = state.CurrentRound.ToString();
		_waterValue.Text = $"{state.Water} / {targetWater}";
		_viewBoardButton.Visible = !hasWon;

		Color accentColor = hasWon ? VictoryColor : DefeatColor;
		ApplyAccentColor(accentColor);

		Show();
		GetTree().Paused = true;
		ShowResultPanel();
		CallDeferred(nameof(AnimateIn));
	}

	private void ShowBoard()
	{
		if (!Visible)
			return;

		_isInspectingBoard = true;
		_dimmer.Hide();
		_centerContainer.Hide();
		_restoreResultButton.Show();

		if (_cameraRig != null)
		{
			_cameraRig.ProcessMode = ProcessModeEnum.Always;
			_cameraRig.SetInteractionEnabled(true);
		}

		_restoreResultButton.GrabFocus();
	}

	private void ShowResultPanel()
	{
		if (!Visible)
			return;

		_isInspectingBoard = false;
		_restoreResultButton.Hide();
		_dimmer.Show();
		_centerContainer.Show();

		if (_cameraRig != null)
		{
			_cameraRig.SetInteractionEnabled(false);
			_cameraRig.ProcessMode = ProcessModeEnum.Always;
		}

		_restartButton.GrabFocus();
	}

	private void ApplyAccentColor(Color accentColor)
	{
		_outcomeLabel.AddThemeColorOverride("font_color", accentColor);
		_titleLabel.AddThemeColorOverride("font_color", accentColor);

		StyleBoxFlat panelStyle =
			_resultPanel.GetThemeStylebox("panel")?.Duplicate() as StyleBoxFlat;

		if (panelStyle == null)
			return;

		panelStyle.BorderColor = accentColor;
		_resultPanel.AddThemeStyleboxOverride("panel", panelStyle);
	}

	private void AnimateIn()
	{
		if (!Visible)
			return;

		if (_entranceTween != null && _entranceTween.IsValid())
			_entranceTween.Kill();

		_resultPanel.PivotOffset = _resultPanel.Size * 0.5f;
		_resultPanel.Scale = new Vector2(0.94f, 0.94f);
		_resultPanel.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);

		_entranceTween = CreateTween();
		_entranceTween.SetParallel(true);
		_entranceTween.TweenProperty(
			_resultPanel,
			"scale",
			Vector2.One,
			0.22f).SetTrans(Tween.TransitionType.Back);
		_entranceTween.TweenProperty(
			_resultPanel,
			"modulate:a",
			1.0f,
			0.16f);
	}

	private void RestartGame()
	{
		ChangeScene(MainScenePath, "Partie");
	}

	private void OpenMainMenu()
	{
		ChangeScene(MainMenuScenePath, "Hauptmenü");
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

		GD.PushError(
			$"EndGameScreen: {sceneName} konnte nicht geöffnet werden: {error}");
		GetTree().Paused = true;
		_restartButton.GrabFocus();
	}
}
