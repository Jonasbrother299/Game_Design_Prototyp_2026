using Godot;
using System.Collections.Generic;

public partial class DroughtWorldEffect : WorldEnvironment
{
	private const float DimmingDuration = 1.80f;
	private const float NightHoldDuration = 1.50f;
	private const float SunriseMoonPhaseDuration = 1.25f;
	private const float BrighteningDuration = 1.80f;
	private const float SunriseDuration =
		SunriseMoonPhaseDuration +
		BrighteningDuration;
	private const float DayNightCycleDuration =
		DimmingDuration +
		NightHoldDuration +
		SunriseDuration;

	private enum WorldLook
	{
		Normal,
		HeatDay,
		Drought
	}

	[ExportGroup("Connections")]
	[Export] public NodePath TurnManagerPath =
		new NodePath("../TurnManager");
	[Export] public NodePath DirectionalLightPath =
		new NodePath("../DirectionalLight3D");

	[ExportGroup("Drought Look")]
	[Export] public Color DroughtBackgroundColor =
		new Color(0.24f, 0.14f, 0.09f);
	[Export] public Color DroughtAmbientColor =
		new Color(0.68f, 0.43f, 0.29f);
	[Export] public Color DroughtLightColor =
		new Color(1.0f, 0.69f, 0.48f);

	[Export(PropertyHint.Range, "0.5,1.2,0.01")]
	public float DroughtBrightness = 0.92f;

	[Export(PropertyHint.Range, "0.5,1.5,0.01")]
	public float DroughtContrast = 1.08f;

	[Export(PropertyHint.Range, "0.0,1.5,0.01")]
	public float DroughtSaturation = 0.82f;

	[ExportGroup("Heat Day Look")]
	[Export] public Color HeatBackgroundColor =
		new Color(0.19f, 0.16f, 0.13f);
	[Export] public Color HeatAmbientColor =
		new Color(0.61f, 0.54f, 0.44f);
	[Export] public Color HeatLightColor =
		new Color(1.0f, 0.86f, 0.70f);

	[Export(PropertyHint.Range, "0.5,1.2,0.01")]
	public float HeatBrightness = 0.97f;

	[Export(PropertyHint.Range, "0.5,1.5,0.01")]
	public float HeatContrast = 1.03f;

	[Export(PropertyHint.Range, "0.0,1.5,0.01")]
	public float HeatSaturation = 0.92f;

	[ExportGroup("Transition")]
	[Export(PropertyHint.Range, "0.0,3.0,0.05")]
	public float FadeDuration = 0.8f;

	private TurnManager _turnManager;
	private DirectionalLight3D _directionalLight;
	private Tween _transitionTween;
	private Tween _dayNightTween;

	private Color _baseBackgroundColor;
	private Color _baseAmbientColor;
	private Color _baseLightColor = Colors.White;
	private bool _baseAdjustmentEnabled;
	private float _baseAmbientEnergy;
	private float _baseLightEnergy = 1.0f;
	private float _baseBrightness;
	private float _baseContrast;
	private float _baseSaturation;
	private WorldLook _currentLook = WorldLook.Normal;
	private WorldLook _requestedLook = WorldLook.Normal;
	private bool _isDayNightCycleActive;

	public override void _Ready()
	{
		if (Environment == null)
		{
			GD.PushWarning("DroughtWorldEffect: Environment fehlt.");
			return;
		}

		_turnManager = GetNodeOrNull<TurnManager>(TurnManagerPath);
		_directionalLight =
			GetNodeOrNull<DirectionalLight3D>(DirectionalLightPath);

		SaveBaseLook();

		if (_turnManager == null)
		{
			GD.PushWarning("DroughtWorldEffect: TurnManager fehlt.");
			return;
		}

		_turnManager.TurnStarted += OnTurnStarted;
		_turnManager.EventActivated += OnEventActivated;
		_turnManager.EventPhaseResolved += OnEventPhaseResolved;
		_turnManager.GameEnded += OnGameEnded;

		if (_turnManager.State != null)
		{
			ApplyLook(GetLook(_turnManager.State.ActiveEvents));
		}
	}

	public override void _ExitTree()
	{
		if (_turnManager != null)
		{
			_turnManager.TurnStarted -= OnTurnStarted;
			_turnManager.EventActivated -= OnEventActivated;
			_turnManager.EventPhaseResolved -= OnEventPhaseResolved;
			_turnManager.GameEnded -= OnGameEnded;
		}

		if (_transitionTween != null && _transitionTween.IsValid())
			_transitionTween.Kill();
		if (_dayNightTween != null && _dayNightTween.IsValid())
			_dayNightTween.Kill();
	}

	public void RefreshFromRestoredState()
	{
		if (_turnManager?.State == null)
			return;

		CancelDayNightCycle();
		ApplyLook(GetLook(_turnManager.State.ActiveEvents), immediate: true);
	}

	public float PlayDayNightCycle()
	{
		if (Environment == null)
			return 0.0f;

		if (_transitionTween != null && _transitionTween.IsValid())
			_transitionTween.Kill();
		if (_dayNightTween != null && _dayNightTween.IsValid())
			_dayNightTween.Kill();

		_isDayNightCycleActive = true;
		Environment.AdjustmentEnabled = true;

		Color startingBackground = Environment.BackgroundColor;
		Color startingAmbient = Environment.AmbientLightColor;
		Color startingLight = _directionalLight?.LightColor ?? _baseLightColor;
		float startingAmbientEnergy = Environment.AmbientLightEnergy;
		float startingLightEnergy = _directionalLight?.LightEnergy ?? _baseLightEnergy;
		float startingBrightness = Environment.AdjustmentBrightness;
		float startingContrast = Environment.AdjustmentContrast;
		float startingSaturation = Environment.AdjustmentSaturation;

		_dayNightTween = CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		AppendDayCycleLook(
			new Color(0.055f, 0.10f, 0.24f),
			new Color(0.16f, 0.28f, 0.50f),
			new Color(0.46f, 0.58f, 1.0f),
			0.32f,
			0.36f,
			0.80f,
			1.04f,
			0.86f,
			DimmingDuration);
		_dayNightTween.TweenInterval(NightHoldDuration);
		_dayNightTween.TweenInterval(SunriseMoonPhaseDuration);
		AppendDayCycleLook(
			startingBackground,
			startingAmbient,
			startingLight,
			startingAmbientEnergy,
			startingLightEnergy,
			startingBrightness,
			startingContrast,
			startingSaturation,
			BrighteningDuration);
		_dayNightTween.TweenCallback(Callable.From(FinishDayNightCycle));

		return DayNightCycleDuration;
	}

	public void CancelDayNightCycle()
	{
		if (_dayNightTween != null && _dayNightTween.IsValid())
			_dayNightTween.Kill();

		if (!_isDayNightCycleActive)
			return;

		_isDayNightCycleActive = false;
		ApplyLook(_requestedLook, immediate: true);
	}

	private void SaveBaseLook()
	{
		_baseBackgroundColor = Environment.BackgroundColor;
		_baseAmbientColor = Environment.AmbientLightColor;
		_baseAdjustmentEnabled = Environment.AdjustmentEnabled;
		_baseAmbientEnergy = Environment.AmbientLightEnergy;
		_baseBrightness = Environment.AdjustmentBrightness;
		_baseContrast = Environment.AdjustmentContrast;
		_baseSaturation = Environment.AdjustmentSaturation;

		if (_directionalLight != null)
		{
			_baseLightColor = _directionalLight.LightColor;
			_baseLightEnergy = _directionalLight.LightEnergy;
		}
	}

	private void OnTurnStarted(int round)
	{
		if (_turnManager?.State == null)
			return;

		ApplyLook(GetLook(_turnManager.State.ActiveEvents));
	}

	private void OnEventActivated(GameEventType eventType)
	{
		ApplyLook(GetLook(eventType));
	}

	private void OnEventPhaseResolved(EventPhaseResult result)
	{
		ApplyLook(GetLook(result.ActiveEvents));
	}

	private void OnGameEnded(GameState state)
	{
		ApplyLook(WorldLook.Normal, immediate: true);
	}

	private void ApplyLook(
		WorldLook look,
		bool immediate = false,
		bool force = false)
	{
		if (Environment == null)
			return;

		_requestedLook = look;
		if (_isDayNightCycleActive)
			return;

		if (!immediate && !force && _currentLook == look)
			return;

		_currentLook = look;

		if (_transitionTween != null && _transitionTween.IsValid())
			_transitionTween.Kill();

		Color targetBackground = _baseBackgroundColor;
		Color targetAmbient = _baseAmbientColor;
		Color targetLight = _baseLightColor;
		float targetBrightness = _baseBrightness;
		float targetContrast = _baseContrast;
		float targetSaturation = _baseSaturation;

		if (look == WorldLook.Drought)
		{
			targetBackground = DroughtBackgroundColor;
			targetAmbient = DroughtAmbientColor;
			targetLight = DroughtLightColor;
			targetBrightness = DroughtBrightness;
			targetContrast = DroughtContrast;
			targetSaturation = DroughtSaturation;
		}
		else if (look == WorldLook.HeatDay)
		{
			targetBackground = HeatBackgroundColor;
			targetAmbient = HeatAmbientColor;
			targetLight = HeatLightColor;
			targetBrightness = HeatBrightness;
			targetContrast = HeatContrast;
			targetSaturation = HeatSaturation;
		}

		Environment.AdjustmentEnabled =
			look != WorldLook.Normal || _baseAdjustmentEnabled;

		float duration = immediate
			? 0.0f
			: Mathf.Max(FadeDuration, 0.0f);

		if (duration <= 0.001f)
		{
			SetLookValues(
				targetBackground,
				targetAmbient,
				targetLight,
				targetBrightness,
				targetContrast,
				targetSaturation);
			RestoreAdjustmentStateIfNormal();
			return;
		}

		_transitionTween = CreateTween();
		_transitionTween.SetParallel(true);
		_transitionTween.TweenProperty(
			Environment,
			"background_color",
			targetBackground,
			duration);
		_transitionTween.TweenProperty(
			Environment,
			"ambient_light_color",
			targetAmbient,
			duration);
		_transitionTween.TweenProperty(
			Environment,
			"adjustment_brightness",
			targetBrightness,
			duration);
		_transitionTween.TweenProperty(
			Environment,
			"adjustment_contrast",
			targetContrast,
			duration);
		_transitionTween.TweenProperty(
			Environment,
			"adjustment_saturation",
			targetSaturation,
			duration);

		if (_directionalLight != null)
		{
			_transitionTween.TweenProperty(
				_directionalLight,
				"light_color",
				targetLight,
				duration);
		}

		_transitionTween.SetParallel(false);
		_transitionTween.TweenCallback(
			Callable.From(RestoreAdjustmentStateIfNormal));
	}

	private void SetLookValues(
		Color backgroundColor,
		Color ambientColor,
		Color lightColor,
		float brightness,
		float contrast,
		float saturation)
	{
		Environment.BackgroundColor = backgroundColor;
		Environment.AmbientLightColor = ambientColor;
		Environment.AdjustmentBrightness = brightness;
		Environment.AdjustmentContrast = contrast;
		Environment.AdjustmentSaturation = saturation;

		if (_directionalLight != null)
			_directionalLight.LightColor = lightColor;
	}

	private void AppendDayCycleLook(
		Color backgroundColor,
		Color ambientColor,
		Color lightColor,
		float ambientEnergy,
		float lightEnergy,
		float brightness,
		float contrast,
		float saturation,
		float duration)
	{
		_dayNightTween.SetParallel(true);
		_dayNightTween.TweenProperty(
			Environment,
			"background_color",
			backgroundColor,
			duration);
		_dayNightTween.TweenProperty(
			Environment,
			"ambient_light_color",
			ambientColor,
			duration);
		_dayNightTween.TweenProperty(
			Environment,
			"ambient_light_energy",
			ambientEnergy,
			duration);
		_dayNightTween.TweenProperty(
			Environment,
			"adjustment_brightness",
			brightness,
			duration);
		_dayNightTween.TweenProperty(
			Environment,
			"adjustment_contrast",
			contrast,
			duration);
		_dayNightTween.TweenProperty(
			Environment,
			"adjustment_saturation",
			saturation,
			duration);

		if (_directionalLight != null)
		{
			_dayNightTween.TweenProperty(
				_directionalLight,
				"light_color",
				lightColor,
				duration);
			_dayNightTween.TweenProperty(
				_directionalLight,
				"light_energy",
				lightEnergy,
				duration);
		}

		_dayNightTween.SetParallel(false);
	}

	private void FinishDayNightCycle()
	{
		_isDayNightCycleActive = false;
		ApplyLook(_requestedLook, force: true);
	}

	private void RestoreAdjustmentStateIfNormal()
	{
		if (_currentLook == WorldLook.Normal)
			Environment.AdjustmentEnabled = _baseAdjustmentEnabled;
	}

	private static WorldLook GetLook(GameEventType eventType)
	{
		return eventType switch
		{
			GameEventType.Drought => WorldLook.Drought,
			GameEventType.HeatDay => WorldLook.HeatDay,
			_ => WorldLook.Normal
		};
	}

	private static WorldLook GetLook(
		IReadOnlyList<GameEventType> eventTypes)
	{
		if (eventTypes == null)
			return WorldLook.Normal;

		WorldLook look = WorldLook.Normal;

		foreach (GameEventType eventType in eventTypes)
		{
			WorldLook eventLook = GetLook(eventType);
			if (eventLook > look)
				look = eventLook;
		}

		return look;
	}

	private static WorldLook GetLook(
		IReadOnlyList<ActiveGameEvent> activeEvents)
	{
		if (activeEvents == null)
			return WorldLook.Normal;

		WorldLook look = WorldLook.Normal;

		foreach (ActiveGameEvent activeEvent in activeEvents)
		{
			if (activeEvent?.Definition == null)
				continue;

			WorldLook eventLook = GetLook(activeEvent.Definition.Type);
			if (eventLook > look)
				look = eventLook;
		}

		return look;
	}
}
