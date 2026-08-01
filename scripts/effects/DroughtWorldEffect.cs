using Godot;
using System.Collections.Generic;

public partial class DroughtWorldEffect : WorldEnvironment
{
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

	private Color _baseBackgroundColor;
	private Color _baseAmbientColor;
	private Color _baseLightColor = Colors.White;
	private bool _baseAdjustmentEnabled;
	private float _baseBrightness;
	private float _baseContrast;
	private float _baseSaturation;
	private WorldLook _currentLook = WorldLook.Normal;

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
	}

	public void RefreshFromRestoredState()
	{
		if (_turnManager?.State == null)
			return;

		ApplyLook(GetLook(_turnManager.State.ActiveEvents), immediate: true);
	}

	private void SaveBaseLook()
	{
		_baseBackgroundColor = Environment.BackgroundColor;
		_baseAmbientColor = Environment.AmbientLightColor;
		_baseAdjustmentEnabled = Environment.AdjustmentEnabled;
		_baseBrightness = Environment.AdjustmentBrightness;
		_baseContrast = Environment.AdjustmentContrast;
		_baseSaturation = Environment.AdjustmentSaturation;

		if (_directionalLight != null)
			_baseLightColor = _directionalLight.LightColor;
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

	private void ApplyLook(WorldLook look, bool immediate = false)
	{
		if (Environment == null)
			return;

		if (!immediate && _currentLook == look)
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
