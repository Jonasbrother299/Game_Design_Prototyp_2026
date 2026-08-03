using Godot;
using System.Collections.Generic;

public partial class GameHub : Control
{
	private const string EffectsBusName = "Effects";
	private const float DimmingDuration = 1.80f;
	private const float NightHoldDuration = 1.50f;
	private const float SunriseMoonPhaseDuration = 1.25f;
	private const float BrighteningDuration = 1.80f;
	private const float SunriseDuration =
		SunriseMoonPhaseDuration +
		BrighteningDuration;
	private const float DayNightTransitionDuration =
		DimmingDuration +
		NightHoldDuration +
		SunriseDuration;

	[Signal]
	public delegate void MenuRequestedEventHandler();

	[Export] public Button ExitButton;

	[ExportGroup("Water Feedback")]
	[Export(PropertyHint.Range, "0.0,3.0,0.05")]
	public float WaterFeedbackDelay = 0.0f;

	[Export(PropertyHint.Range, "0.2,3.0,0.05")]
	public float WaterLabelDuration = 1.15f;

	[Export] public Font WaterFeedbackFont;

	[Export(PropertyHint.Range, "32,96,1")]
	public int WaterFeedbackFontSize = 62;

	[Export(PropertyHint.Range, "0,20,1")]
	public int WaterFeedbackOutlineSize = 9;

	[Export] public Color PositiveWaterColor =
		new Color(0.32f, 0.76f, 0.66f);
	[Export] public Color NegativeWaterColor =
		new Color(0.86f, 0.55f, 0.40f);
	[Export] public Color WaterFeedbackOutlineColor =
		new Color(0.08f, 0.12f, 0.10f, 0.92f);

	private TurnManager _turnManager;
	private BoardManager _boardManager;
	private EventDisplayUI _eventDisplay;
	private WaterDisplayUI _waterDisplay;
	private RoundDisplayUI _roundDisplay;
	private BaseButton _endTurnButton;
	private ColorRect _dayNightOverlay;
	private DayCycleDisplayUI _dayCycleDisplay;
	private DroughtWorldEffect _droughtWorldEffect;
	private GameManager _gameManager;
	private AudioStreamPlayer _plantPlacementAudio;
	private AudioStreamPlayer _forestAmbienceAudio;
	private AudioStreamPlayer _heatAmbienceAudio;
	private AudioStreamPlayer _rainAmbienceAudio;
	private AudioStreamPlayer _heavyRainAmbienceAudio;
	private bool _environmentalAudioEventsConnected;
	private CanvasLayer _rainLensLayer;
	private RainLensCyaniluxOverlay _rainLensOverlay;
	private Tween _feedbackTimelineTween;
	private Tween _dayNightTransitionTween;
	private float _feedbackSequenceEndDelay;
	private CanvasLayer _saveFeedbackLayer;
	private PanelContainer _saveFeedbackPanel;
	private Label _saveFeedbackLabel;
	private TextureRect _saveFeedbackSpinner;
	private Tween _saveFeedbackTween;
	private Tween _saveFeedbackSpinnerTween;
	private CanvasLayer _achievementFeedbackLayer;
	private PanelContainer _achievementFeedbackPanel;
	private PanelContainer _achievementBadgePanel;
	private Label _achievementBadgeLabel;
	private Label _achievementFeedbackTitleLabel;
	private Label _achievementFeedbackNameLabel;
	private Label _achievementFeedbackDescriptionLabel;
	private Tween _achievementFeedbackTween;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		WaterFeedbackFont ??= GD.Load<Font>(
			"res://assets/ui/fonts/Eckmannpsych-Medium.ttf");

		if (ExitButton == null)
			ExitButton = GetNodeOrNull<Button>("ExitButton");

		if (ExitButton != null)
			ExitButton.Pressed += OnExitButtonPressed;

		CallDeferred(nameof(SetupEventDisplay));
	}

	public override void _ExitTree()
	{
		if (ExitButton != null)
			ExitButton.Pressed -= OnExitButtonPressed;

		if (_turnManager != null)
		{
			_turnManager.TurnStarted -= OnTurnStarted;
			_turnManager.PlantPlaced -= OnPlantPlaced;
			_turnManager.EventActivated -= OnEventActivated;
			_turnManager.WaterPhaseResolved -= OnWaterPhaseResolved;
			_turnManager.EventPhaseResolved -= OnEventPhaseResolved;
			_turnManager.EndTurnRequested -= OnEndTurnRequested;
		}

		DisconnectEnvironmentalAudioEvents();
		StopEnvironmentalAudio();

		if (_feedbackTimelineTween != null &&
			_feedbackTimelineTween.IsValid())
		{
			_feedbackTimelineTween.Kill();
		}

		if (_dayNightTransitionTween != null &&
			_dayNightTransitionTween.IsValid())
		{
			_dayNightTransitionTween.Kill();
		}

		if (_saveFeedbackTween != null && _saveFeedbackTween.IsValid())
			_saveFeedbackTween.Kill();
		if (_saveFeedbackSpinnerTween != null && _saveFeedbackSpinnerTween.IsValid())
			_saveFeedbackSpinnerTween.Kill();
		if (_achievementFeedbackTween != null && _achievementFeedbackTween.IsValid())
			_achievementFeedbackTween.Kill();
	}

	private void OnExitButtonPressed()
	{
		EmitSignal(SignalName.MenuRequested);
	}

	private void SetupEventDisplay()
	{
		Node currentScene = GetTree().CurrentScene;
		if (currentScene == null)
			return;

		_turnManager = currentScene.GetNodeOrNull<TurnManager>("TurnManager");
		if (_turnManager == null)
		{
			GD.PushError("GameHub: TurnManager fehlt.");
			return;
		}

		_boardManager = currentScene.GetNodeOrNull<BoardManager>("BoardManager");
		if (_boardManager == null)
		{
			GD.PushError("GameHub: BoardManager fehlt.");
			return;
		}

		_eventDisplay = GetNodeOrNull<EventDisplayUI>("EventDisplay");
		if (_eventDisplay == null)
		{
			PackedScene displayScene = GD.Load<PackedScene>(
				"res://scenes/UI/EventDisplay.tscn");
			_eventDisplay = displayScene?.Instantiate<EventDisplayUI>();

			if (_eventDisplay != null)
			{
				AddChild(_eventDisplay);
			}
		}

		_waterDisplay = GetNodeOrNull<WaterDisplayUI>("WaterLabel");
		if (_waterDisplay == null)
		{
			GD.PushError("GameHub: Wasseranzeige fehlt.");
		}
		else if (_turnManager.State != null)
		{
			_waterDisplay.ShowCurrentState(
				_turnManager.State.Water,
				_turnManager.Config.WinWaterLimit);
			UpdateWaterPreview();
		}

		_roundDisplay = GetNodeOrNull<RoundDisplayUI>("RoundDisplay");
		if (_roundDisplay == null)
		{
			GD.PushError("GameHub: Rundenanzeige fehlt.");
		}
		else if (_turnManager.State != null)
		{
			_roundDisplay.ShowRound(_turnManager.State.CurrentRound);
		}

		_endTurnButton = GetNodeOrNull<BaseButton>("EndTurnButton");
		_dayNightOverlay = GetNodeOrNull<ColorRect>("DayNightOverlay");
		_dayCycleDisplay = GetNodeOrNull<DayCycleDisplayUI>("DayCycleDisplay");
		_droughtWorldEffect =
			currentScene.GetNodeOrNull<DroughtWorldEffect>("WorldEnvironment");
		_gameManager = currentScene.GetNodeOrNull<GameManager>("GameManager");
		SetupEnvironmentalAudio();

		_rainLensOverlay =
			currentScene.GetNodeOrNull<RainLensCyaniluxOverlay>(
				"RainLensLayer/RainLensRoot/RainLensOverlay");
		_rainLensLayer = _rainLensOverlay?.GetParent()?.GetParent() as CanvasLayer;

		_turnManager.TurnStarted += OnTurnStarted;
		_turnManager.PlantPlaced += OnPlantPlaced;
		_turnManager.EventActivated += OnEventActivated;
		_turnManager.WaterPhaseResolved += OnWaterPhaseResolved;
		_turnManager.EventPhaseResolved += OnEventPhaseResolved;
		_turnManager.EndTurnRequested += OnEndTurnRequested;
		RefreshFromRestoredState();
	}

	public void RefreshFromRestoredState()
	{
		if (_turnManager?.State == null || _boardManager == null)
			return;

		if (_feedbackTimelineTween != null &&
			_feedbackTimelineTween.IsValid())
		{
			_feedbackTimelineTween.Kill();
		}

		_feedbackSequenceEndDelay = 0.0f;
		ResetDayNightPresentation();
		_waterDisplay?.ShowCurrentState(
			_turnManager.State.Water,
			_turnManager.Config.WinWaterLimit);
		_roundDisplay?.ShowRound(_turnManager.State.CurrentRound);
		SetEndTurnLocked(_turnManager.State.IsGameOver);
		UpdateWaterPreview();
		RefreshActiveEventDisplay();
	}

	public void ShowSaveFeedback(string message, bool isWarning)
	{
		EnsureSaveFeedbackLabel();
		if (_saveFeedbackLabel == null)
			return;

		if (_saveFeedbackTween != null && _saveFeedbackTween.IsValid())
			_saveFeedbackTween.Kill();

		_saveFeedbackLabel.Text = message;
		_saveFeedbackLabel.AddThemeColorOverride(
			"font_color",
			isWarning
				? new Color(0.90f, 0.50f, 0.32f)
				: new Color(0.84f, 0.94f, 0.67f));
		_saveFeedbackPanel.Modulate = Colors.White;
		_saveFeedbackPanel.Show();

		_saveFeedbackTween = _saveFeedbackPanel.CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process);
		_saveFeedbackTween.TweenInterval(1.35f);
		_saveFeedbackTween.TweenProperty(
			_saveFeedbackPanel,
			"modulate:a",
			0.0f,
			0.20f);
		_saveFeedbackTween.TweenCallback(Callable.From(() => _saveFeedbackPanel.Hide()));
	}

	public void ShowAchievementFeedback(IReadOnlyList<AchievementDefinition> achievements)
	{
		if (achievements == null || achievements.Count == 0)
			return;

		EnsureAchievementFeedback();
		if (_achievementFeedbackPanel == null)
			return;

		if (_achievementFeedbackTween != null && _achievementFeedbackTween.IsValid())
			_achievementFeedbackTween.Kill();

		AchievementDefinition firstAchievement = achievements[0];
		Color badgeColor = GetAchievementBadgeColor(firstAchievement.BadgeTier);
		_achievementFeedbackPanel.AddThemeStyleboxOverride(
			"panel",
			CreateAchievementFeedbackStyle(badgeColor));
		_achievementBadgePanel.AddThemeStyleboxOverride(
			"panel",
			CreateAchievementBadgeStyle(badgeColor));
		_achievementBadgeLabel.AddThemeColorOverride("font_color", badgeColor);
		_achievementFeedbackTitleLabel.Text = achievements.Count == 1
			? $"{GetAchievementBadgeLabel(firstAchievement.BadgeTier)}-ABZEICHEN FREIGESCHALTET"
			: "ACHIEVEMENTS FREIGESCHALTET";
		_achievementFeedbackNameLabel.Text = achievements.Count == 1
			? firstAchievement.DisplayName
			: $"{firstAchievement.DisplayName} + {achievements.Count - 1} weitere";
		_achievementFeedbackDescriptionLabel.Text = firstAchievement.Description;
		_achievementFeedbackPanel.Modulate = Colors.White;
		_achievementFeedbackPanel.Show();

		_achievementFeedbackTween = _achievementFeedbackPanel.CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process);
		_achievementFeedbackTween.TweenInterval(3.0f);
		_achievementFeedbackTween.TweenProperty(
			_achievementFeedbackPanel,
			"modulate:a",
			0.0f,
			0.25f);
		_achievementFeedbackTween.TweenCallback(
			Callable.From(() => _achievementFeedbackPanel.Hide()));
	}

	private void OnEventActivated(GameEventType eventType)
	{
		_eventDisplay?.ShowActivated(EventDatabase.Get(eventType));
		UpdateWaterPreview();

		if (eventType == GameEventType.Rain ||
			eventType == GameEventType.HeavyRain)
		{
			_dayCycleDisplay?.SetWeather(
				hasRain: true,
				hasHeavyRain: eventType == GameEventType.HeavyRain);

			if (_rainLensLayer != null)
				_rainLensLayer.Visible = true;

			float intensity = eventType == GameEventType.HeavyRain ? 0.90f : 0.62f;
			_rainLensOverlay?.StartRain(intensity);
		}

		UpdateEnvironmentalAudio();
	}

	private void RefreshActiveEventDisplay()
	{
		ActiveGameEvent activeEvent = _turnManager.State.ActiveEvents.Count > 0
			? _turnManager.State.ActiveEvents[0]
			: null;
		if (activeEvent?.Definition != null)
			_eventDisplay?.ShowActivated(activeEvent.Definition);

		bool hasRain = false;
		bool hasHeavyRain = false;
		foreach (ActiveGameEvent gameEvent in _turnManager.State.ActiveEvents)
		{
			if (gameEvent?.Definition?.Type == GameEventType.HeavyRain)
			{
				hasRain = true;
				hasHeavyRain = true;
			}
			else if (gameEvent?.Definition?.Type == GameEventType.Rain)
			{
				hasRain = true;
			}
		}

		_dayCycleDisplay?.SetWeather(hasRain, hasHeavyRain);
		UpdateEnvironmentalAudio();

		if (!hasRain)
		{
			_rainLensOverlay?.StopRain();
			return;
		}

		if (_rainLensLayer != null)
			_rainLensLayer.Visible = true;

		_rainLensOverlay?.StartRain(hasHeavyRain ? 0.90f : 0.62f);
	}

	private void OnWaterPhaseResolved(WaterPhaseResult result)
	{
		if (_feedbackTimelineTween != null &&
			_feedbackTimelineTween.IsValid())
		{
			_feedbackTimelineTween.Kill();
		}

		_feedbackSequenceEndDelay = Mathf.Max(
			_feedbackSequenceEndDelay,
			WaterFeedbackDelay + WaterLabelDuration);
		SetEndTurnLocked(true);

		_eventDisplay?.ShowWaterResult(result);
		_waterDisplay?.ShowWaterResult(
			result,
			_turnManager.Config.WinWaterLimit);

		foreach (PlantWaterResult plantResult in result.Plants)
		{
			if (plantResult.NetChange == 0)
				continue;

			HexTile tile = _boardManager?.GetTileView(plantResult.Coord);
			Color color = plantResult.NetChange > 0
				? PositiveWaterColor
				: NegativeWaterColor;

			tile?.ShowFloatingWaterChange(
				plantResult.NetChange,
				color,
				WaterFeedbackOutlineColor,
				WaterFeedbackFont,
				WaterFeedbackFontSize,
				WaterFeedbackOutlineSize,
				WaterFeedbackDelay,
				WaterLabelDuration);
		}
	}

	private void OnTurnStarted(int round)
	{
		ScheduleRoundStartFeedback(round);
		UpdateWaterPreview();
	}

	private void OnEndTurnRequested(int _)
	{
		_gameManager?.SetDayNightPresentationInputLocked(true);
		float userInterfaceDuration = StartDayNightTransition();
		float worldDuration = _droughtWorldEffect?.PlayDayNightCycle() ?? 0.0f;
		_feedbackSequenceEndDelay = Mathf.Max(
			_feedbackSequenceEndDelay,
			Mathf.Max(userInterfaceDuration, worldDuration));
		SetEndTurnLocked(true);
	}

	private float StartDayNightTransition()
	{
		if (_dayNightOverlay == null)
			return 0.0f;

		if (_dayNightTransitionTween != null &&
			_dayNightTransitionTween.IsValid())
		{
			_dayNightTransitionTween.Kill();
		}

		_dayCycleDisplay?.ShowDay();
		_dayCycleDisplay?.PlaySunset(DimmingDuration);
		_dayNightOverlay.Color = new Color(0.05f, 0.12f, 0.30f, 0.0f);
		_dayNightOverlay.Show();

		_dayNightTransitionTween = CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_dayNightTransitionTween.TweenProperty(
			_dayNightOverlay,
			"color",
			new Color(0.06f, 0.14f, 0.34f, 0.16f),
			DimmingDuration);
		_dayNightTransitionTween.TweenCallback(
			Callable.From(() => _dayCycleDisplay?.ShowNight()));
		_dayNightTransitionTween.TweenInterval(NightHoldDuration);
		_dayNightTransitionTween.TweenCallback(
			Callable.From(() => _dayCycleDisplay?.PlaySunrise(
				SunriseDuration)));
		_dayNightTransitionTween.TweenInterval(SunriseMoonPhaseDuration);
		_dayNightTransitionTween.TweenProperty(
			_dayNightOverlay,
			"color",
			new Color(0.05f, 0.12f, 0.30f, 0.0f),
			BrighteningDuration);
		_dayNightTransitionTween.TweenCallback(
			Callable.From(() => _dayCycleDisplay?.ShowDay()));
		_dayNightTransitionTween.TweenCallback(
			Callable.From(() => _dayNightOverlay.Hide()));

		return DayNightTransitionDuration;
	}

	private void ResetDayNightPresentation()
	{
		if (_dayNightTransitionTween != null &&
			_dayNightTransitionTween.IsValid())
		{
			_dayNightTransitionTween.Kill();
		}

		if (_dayNightOverlay != null)
		{
			_dayNightOverlay.Color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
			_dayNightOverlay.Hide();
		}

		_dayCycleDisplay?.ShowDay();
		_droughtWorldEffect?.CancelDayNightCycle();
		_gameManager?.SetDayNightPresentationInputLocked(false);
	}

	private void ScheduleRoundStartFeedback(int round)
	{
		ScheduleRoundPresentationCompletion(round);
	}

	private void ScheduleRoundPresentationCompletion(int round)
	{
		float delay = Mathf.Max(_feedbackSequenceEndDelay, 0.0f);

		if (delay <= 0.01f)
		{
			_roundDisplay?.ShowRound(round);
			SetEndTurnLocked(false);
			_gameManager?.SetDayNightPresentationInputLocked(false);
			_feedbackSequenceEndDelay = 0.0f;
			return;
		}

		_feedbackTimelineTween = CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process);
		_feedbackTimelineTween.TweenInterval(delay);
		_feedbackTimelineTween.TweenCallback(Callable.From(() =>
		{
			_roundDisplay?.ShowRound(round);
			SetEndTurnLocked(false);
			_gameManager?.SetDayNightPresentationInputLocked(false);
			_feedbackSequenceEndDelay = 0.0f;
		}));
	}

	private void EnsureSaveFeedbackLabel()
	{
		if (_saveFeedbackPanel != null && IsInstanceValid(_saveFeedbackPanel))
			return;

		_saveFeedbackLayer = new CanvasLayer
		{
			Name = "StatisticsSaveFeedbackLayer",
			Layer = 1001,
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(_saveFeedbackLayer);

		_saveFeedbackPanel = new PanelContainer
		{
			Name = "StatisticsSaveFeedback",
			MouseFilter = MouseFilterEnum.Ignore,
			AnchorTop = 1.0f,
			AnchorBottom = 1.0f,
			OffsetLeft = 28.0f,
			OffsetTop = -98.0f,
			OffsetRight = 368.0f,
			OffsetBottom = -28.0f
		};
		_saveFeedbackPanel.AddThemeStyleboxOverride("panel", CreateSaveFeedbackStyle());
		_saveFeedbackLayer.AddChild(_saveFeedbackPanel);

		HBoxContainer content = new()
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		content.AddThemeConstantOverride("separation", 12);
		_saveFeedbackPanel.AddChild(content);

		_saveFeedbackSpinner = new TextureRect
		{
			CustomMinimumSize = new Vector2(44.0f, 44.0f),
			Texture = GD.Load<Texture2D>("res://assets/ui/decor/ivy_corner.svg"),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
			PivotOffset = new Vector2(22.0f, 22.0f)
		};
		Panel spinnerRing = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			AnchorRight = 1.0f,
			AnchorBottom = 1.0f,
			OffsetRight = 0.0f,
			OffsetBottom = 0.0f
		};
		spinnerRing.AddThemeStyleboxOverride("panel", CreateSaveFeedbackSpinnerStyle());
		_saveFeedbackSpinner.AddChild(spinnerRing);
		content.AddChild(_saveFeedbackSpinner);

		_saveFeedbackLabel = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			VerticalAlignment = VerticalAlignment.Center
		};
		_saveFeedbackLabel.AddThemeColorOverride(
			"font_outline_color",
			new Color(0.05f, 0.09f, 0.04f, 0.98f));
		_saveFeedbackLabel.AddThemeConstantOverride("outline_size", 5);
		_saveFeedbackLabel.AddThemeFontSizeOverride("font_size", 22);
		content.AddChild(_saveFeedbackLabel);

		_saveFeedbackSpinnerTween = _saveFeedbackSpinner.CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process)
			.SetLoops();
		_saveFeedbackSpinnerTween.TweenProperty(
			_saveFeedbackSpinner,
			"rotation",
			Mathf.Tau,
			1.6f);
	}

	private void EnsureAchievementFeedback()
	{
		if (_achievementFeedbackPanel != null &&
			IsInstanceValid(_achievementFeedbackPanel))
		{
			return;
		}

		_achievementFeedbackLayer = new CanvasLayer
		{
			Name = "AchievementFeedbackLayer",
			Layer = 1002,
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(_achievementFeedbackLayer);

		_achievementFeedbackPanel = new PanelContainer
		{
			Name = "AchievementFeedback",
			MouseFilter = MouseFilterEnum.Ignore,
			AnchorTop = 1.0f,
			AnchorBottom = 1.0f,
			OffsetLeft = 28.0f,
			OffsetTop = -194.0f,
			OffsetRight = 508.0f,
			OffsetBottom = -110.0f
		};
		_achievementFeedbackLayer.AddChild(_achievementFeedbackPanel);

		HBoxContainer content = new()
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		content.AddThemeConstantOverride("separation", 14);
		_achievementFeedbackPanel.AddChild(content);

		_achievementBadgePanel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(58.0f, 58.0f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		content.AddChild(_achievementBadgePanel);

		_achievementBadgeLabel = new Label
		{
			Text = "✦",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_achievementBadgeLabel.AddThemeFontSizeOverride("font_size", 34);
		_achievementBadgeLabel.AddThemeColorOverride(
			"font_outline_color",
			new Color(0.10f, 0.07f, 0.03f, 0.96f));
		_achievementBadgeLabel.AddThemeConstantOverride("outline_size", 4);
		_achievementBadgePanel.AddChild(_achievementBadgeLabel);

		VBoxContainer textContent = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		textContent.AddThemeConstantOverride("separation", 1);
		content.AddChild(textContent);

		_achievementFeedbackTitleLabel = CreateAchievementTextLabel(16, new Color(0.83f, 0.91f, 0.62f));
		textContent.AddChild(_achievementFeedbackTitleLabel);
		_achievementFeedbackNameLabel = CreateAchievementTextLabel(24, new Color(1.0f, 0.94f, 0.78f));
		textContent.AddChild(_achievementFeedbackNameLabel);
		_achievementFeedbackDescriptionLabel = CreateAchievementTextLabel(17, new Color(0.90f, 0.86f, 0.69f));
		textContent.AddChild(_achievementFeedbackDescriptionLabel);
	}

	private static Label CreateAchievementTextLabel(int fontSize, Color fontColor)
	{
		Label label = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", fontColor);
		label.AddThemeColorOverride(
			"font_outline_color",
			new Color(0.04f, 0.06f, 0.025f, 0.94f));
		label.AddThemeConstantOverride("outline_size", 3);
		return label;
	}

	private static StyleBoxFlat CreateSaveFeedbackStyle()
	{
		return new StyleBoxFlat
		{
			ContentMarginLeft = 14.0f,
			ContentMarginTop = 10.0f,
			ContentMarginRight = 18.0f,
			ContentMarginBottom = 10.0f,
			BgColor = new Color(0.11f, 0.20f, 0.08f, 0.98f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			BorderColor = new Color(0.72f, 0.82f, 0.42f, 1.0f),
			CornerRadiusTopLeft = 16,
			CornerRadiusTopRight = 16,
			CornerRadiusBottomRight = 16,
			CornerRadiusBottomLeft = 16,
			ShadowColor = new Color(0.01f, 0.03f, 0.01f, 0.78f),
			ShadowSize = 8,
			ShadowOffset = new Vector2(2.0f, 3.0f)
		};
	}

	private static StyleBoxFlat CreateSaveFeedbackSpinnerStyle()
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.13f, 0.045f, 0.0f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			BorderColor = new Color(0.77f, 0.87f, 0.48f, 0.92f),
			CornerRadiusTopLeft = 22,
			CornerRadiusTopRight = 22,
			CornerRadiusBottomRight = 22,
			CornerRadiusBottomLeft = 22
		};
	}

	private static StyleBoxFlat CreateAchievementFeedbackStyle(Color badgeColor)
	{
		return new StyleBoxFlat
		{
			ContentMarginLeft = 13.0f,
			ContentMarginTop = 11.0f,
			ContentMarginRight = 18.0f,
			ContentMarginBottom = 11.0f,
			BgColor = new Color(0.075f, 0.13f, 0.055f, 0.99f),
			BorderWidthLeft = 3,
			BorderWidthTop = 3,
			BorderWidthRight = 3,
			BorderWidthBottom = 3,
			BorderColor = badgeColor,
			CornerRadiusTopLeft = 15,
			CornerRadiusTopRight = 15,
			CornerRadiusBottomRight = 15,
			CornerRadiusBottomLeft = 15,
			ShadowColor = new Color(0.01f, 0.02f, 0.01f, 0.84f),
			ShadowSize = 9,
			ShadowOffset = new Vector2(2.0f, 4.0f)
		};
	}

	private static StyleBoxFlat CreateAchievementBadgeStyle(Color badgeColor)
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(0.16f, 0.22f, 0.09f, 1.0f),
			BorderWidthLeft = 3,
			BorderWidthTop = 3,
			BorderWidthRight = 3,
			BorderWidthBottom = 3,
			BorderColor = badgeColor,
			CornerRadiusTopLeft = 29,
			CornerRadiusTopRight = 29,
			CornerRadiusBottomRight = 29,
			CornerRadiusBottomLeft = 29
		};
	}

	private static Color GetAchievementBadgeColor(AchievementBadgeTier badgeTier)
	{
		return badgeTier switch
		{
			AchievementBadgeTier.Bronze => new Color(0.78f, 0.50f, 0.26f),
			AchievementBadgeTier.Silver => new Color(0.76f, 0.82f, 0.82f),
			AchievementBadgeTier.Gold => new Color(0.98f, 0.76f, 0.28f),
			AchievementBadgeTier.Verdant => new Color(0.48f, 0.81f, 0.39f),
			AchievementBadgeTier.Copper => new Color(0.86f, 0.39f, 0.24f),
			_ => new Color(0.84f, 0.91f, 0.62f)
		};
	}

	private static string GetAchievementBadgeLabel(AchievementBadgeTier badgeTier)
	{
		return badgeTier switch
		{
			AchievementBadgeTier.Bronze => "BRONZE",
			AchievementBadgeTier.Silver => "SILBER",
			AchievementBadgeTier.Gold => "GOLD",
			AchievementBadgeTier.Verdant => "WALD",
			AchievementBadgeTier.Copper => "KUPFER",
			_ => "WALD"
		};
	}

	private void SetEndTurnLocked(bool isLocked)
	{
		if (_endTurnButton != null)
			_endTurnButton.Disabled = isLocked;
	}

	private void OnPlantPlaced(PlantType plantType, HexCoord coord)
	{
		PlayPlantPlacementAudio();
		UpdateWaterPreview();
	}

	private void UpdateWaterPreview()
	{
		if (_waterDisplay == null ||
			_turnManager?.State == null ||
			_boardManager == null)
		{
			return;
		}

		WaterBalanceCalculation balance = WaterBalanceCalculator.Calculate(
			_boardManager,
			_turnManager.State.ActiveEvents);

		_waterDisplay.ShowPreview(
			balance.NetChange,
			_turnManager.Config.WinWaterLimit,
			balance.DisplayedProduction,
			balance.DisplayedConsumption);
	}

	private void OnEventPhaseResolved(EventPhaseResult result)
	{
		_eventDisplay?.ShowPhaseResult(result);

		bool hasHeavyRain = false;
		foreach (GameEventType eventType in result.ActiveEvents)
		{
			if (eventType == GameEventType.HeavyRain)
			{
				hasHeavyRain = true;
				break;
			}
		}
		_dayCycleDisplay?.SetWeather(
			ContainsRainEvent(result.ActiveEvents),
			hasHeavyRain);
		UpdateEnvironmentalAudio();

		if (!ContainsRainEvent(result.ActiveEvents))
		{
			_rainLensOverlay?.StopRain();
		}
	}

	private static bool ContainsRainEvent(
		System.Collections.Generic.IReadOnlyList<GameEventType> activeEvents)
	{
		foreach (GameEventType eventType in activeEvents)
		{
			if (eventType == GameEventType.Rain ||
				eventType == GameEventType.HeavyRain)
			{
				return true;
			}
		}

		return false;
	}

	private void SetupEnvironmentalAudio()
	{
		EnsureEffectsBus();
		_plantPlacementAudio = GetNodeOrNull<AudioStreamPlayer>(
			"PlantPlacementAudio");
		_forestAmbienceAudio = GetNodeOrNull<AudioStreamPlayer>(
			"ForestAmbienceAudio");
		_heatAmbienceAudio = GetNodeOrNull<AudioStreamPlayer>(
			"HeatAmbienceAudio");
		_rainAmbienceAudio = GetNodeOrNull<AudioStreamPlayer>(
			"RainAmbienceAudio");
		_heavyRainAmbienceAudio = GetNodeOrNull<AudioStreamPlayer>(
			"HeavyRainAmbienceAudio");

		if (_environmentalAudioEventsConnected)
			return;

		SubscribeEnvironmentalAudio(_forestAmbienceAudio);
		SubscribeEnvironmentalAudio(_heatAmbienceAudio);
		SubscribeEnvironmentalAudio(_rainAmbienceAudio);
		SubscribeEnvironmentalAudio(_heavyRainAmbienceAudio);
		_environmentalAudioEventsConnected = true;
	}

	private void SubscribeEnvironmentalAudio(AudioStreamPlayer audioPlayer)
	{
		if (audioPlayer != null)
			audioPlayer.Finished += OnEnvironmentalAudioFinished;
	}

	private void DisconnectEnvironmentalAudioEvents()
	{
		if (!_environmentalAudioEventsConnected)
			return;

		UnsubscribeEnvironmentalAudio(_forestAmbienceAudio);
		UnsubscribeEnvironmentalAudio(_heatAmbienceAudio);
		UnsubscribeEnvironmentalAudio(_rainAmbienceAudio);
		UnsubscribeEnvironmentalAudio(_heavyRainAmbienceAudio);
		_environmentalAudioEventsConnected = false;
	}

	private void UnsubscribeEnvironmentalAudio(AudioStreamPlayer audioPlayer)
	{
		if (audioPlayer != null)
			audioPlayer.Finished -= OnEnvironmentalAudioFinished;
	}

	private void OnEnvironmentalAudioFinished()
	{
		UpdateEnvironmentalAudio();
	}

	private void UpdateEnvironmentalAudio()
	{
		if (_turnManager?.State == null)
			return;

		bool hasRain = false;
		bool hasHeavyRain = false;
		bool hasHeat = false;
		foreach (ActiveGameEvent gameEvent in _turnManager.State.ActiveEvents)
		{
			GameEventType? eventType = gameEvent?.Definition?.Type;
			if (eventType == GameEventType.HeavyRain)
			{
				hasRain = true;
				hasHeavyRain = true;
			}
			else if (eventType == GameEventType.Rain)
			{
				hasRain = true;
			}
			else if (eventType == GameEventType.HeatDay)
			{
				hasHeat = true;
			}
		}

		SetAmbientPlayback(_forestAmbienceAudio, !hasRain);
		SetAmbientPlayback(_heatAmbienceAudio, !hasRain && hasHeat);
		SetAmbientPlayback(_rainAmbienceAudio, hasRain && !hasHeavyRain);
		SetAmbientPlayback(_heavyRainAmbienceAudio, hasHeavyRain);
	}

	private void StopEnvironmentalAudio()
	{
		SetAmbientPlayback(_forestAmbienceAudio, false);
		SetAmbientPlayback(_heatAmbienceAudio, false);
		SetAmbientPlayback(_rainAmbienceAudio, false);
		SetAmbientPlayback(_heavyRainAmbienceAudio, false);
		_plantPlacementAudio?.Stop();
	}

	private static void SetAmbientPlayback(
		AudioStreamPlayer audioPlayer,
		bool shouldPlay)
	{
		if (audioPlayer == null)
			return;

		if (!shouldPlay)
		{
			audioPlayer.Stop();
			return;
		}

		if (audioPlayer.Stream != null && !audioPlayer.Playing)
			audioPlayer.Play();
	}

	private void PlayPlantPlacementAudio()
	{
		if (_plantPlacementAudio?.Stream == null)
			return;

		_plantPlacementAudio.PitchScale = 0.88f + GD.Randf() * 0.20f;
		_plantPlacementAudio.Play();
	}

	private static void EnsureEffectsBus()
	{
		if (AudioServer.GetBusIndex(EffectsBusName) >= 0)
			return;

		AudioServer.AddBus();
		int effectsBusIndex = AudioServer.BusCount - 1;
		AudioServer.SetBusName(effectsBusIndex, EffectsBusName);
		AudioServer.SetBusSend(effectsBusIndex, "Master");
	}
}
