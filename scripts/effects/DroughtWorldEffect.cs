using Godot;
using System.Collections.Generic;

[Tool]
public partial class DroughtWorldEffect : WorldEnvironment
{
	private static readonly StringName NightFirefliesGroup =
		"night_fireflies";
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
	private const string DroughtHeatWaveShaderPath =
		"res://shaders/drought_heat_waves.gdshader";

	private enum WorldLook
	{
		Normal,
		HeatDay,
		Drought,
		Rain,
		HeavyRain
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

	[ExportGroup("Drought Heat Waves")]
	[Export] public bool EnableDroughtHeatWaves = true;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float DroughtHeatWaveIntensity = 0.60f;

	[Export(PropertyHint.Range, "0.0,0.03,0.0005")]
	public float DroughtHeatWaveDistortionStrength = 0.0075f;

	[Export(PropertyHint.Range, "0.1,0.95,0.01")]
	public float DroughtHeatWaveHeight = 0.62f;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float DroughtHeatWaveRiseSpeed = 0.55f;

	[Export(PropertyHint.Range, "4.0,5.0,1.0")]
	public float DroughtHeatWavePlumeCount = 5.0f;

	[Export(PropertyHint.Range, "0.02,0.12,0.005")]
	public float DroughtHeatWavePlumeWidth = 0.055f;

	[Export(PropertyHint.Range, "0.0,0.2,0.005")]
	public float DroughtHeatWaveLateralDrift = 0.085f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float DroughtHeatWaveShimmerStrength = 0.36f;

	[Export(PropertyHint.Range, "0.0,0.008,0.00025")]
	public float DroughtHeatWaveChromaticSplit = 0.00125f;

	[Export(PropertyHint.Range, "0.0,3.0,0.05")]
	public float DroughtHeatWaveFadeDuration = 0.9f;

	[ExportGroup("Drought Heat Waves Preview")]
	[Export] public bool PreviewDroughtHeatWaves;

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

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float HeatDayHeatWaveIntensity = 0.28f;

	[ExportGroup("Rain Look")]
	[Export] public Color RainBackgroundColor =
		new Color(0.11f, 0.16f, 0.20f);
	[Export] public Color RainAmbientColor =
		new Color(0.50f, 0.57f, 0.62f);
	[Export] public Color RainLightColor =
		new Color(0.78f, 0.86f, 0.92f);

	[Export(PropertyHint.Range, "0.5,1.2,0.01")]
	public float RainBrightness = 0.94f;

	[Export(PropertyHint.Range, "0.5,1.5,0.01")]
	public float RainContrast = 1.02f;

	[Export(PropertyHint.Range, "0.0,1.5,0.01")]
	public float RainSaturation = 0.92f;

	[ExportGroup("Heavy Rain Look")]
	[Export] public Color HeavyRainBackgroundColor =
		new Color(0.075f, 0.11f, 0.16f);
	[Export] public Color HeavyRainAmbientColor =
		new Color(0.38f, 0.46f, 0.53f);
	[Export] public Color HeavyRainLightColor =
		new Color(0.64f, 0.74f, 0.82f);

	[Export(PropertyHint.Range, "0.5,1.2,0.01")]
	public float HeavyRainBrightness = 0.89f;

	[Export(PropertyHint.Range, "0.5,1.5,0.01")]
	public float HeavyRainContrast = 1.04f;

	[Export(PropertyHint.Range, "0.0,1.5,0.01")]
	public float HeavyRainSaturation = 0.84f;

	[ExportGroup("Night Look")]
	[Export] public Color NightBackgroundColor =
		new Color(0.20f, 0.23f, 0.28f);
	[Export] public Color NightAmbientColor =
		new Color(0.52f, 0.55f, 0.58f);

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float NightAmbientEnergy = 0.88f;

	[Export(PropertyHint.Range, "0.2,1.2,0.01")]
	public float NightBrightness = 1.05f;

	[Export(PropertyHint.Range, "0.5,1.5,0.01")]
	public float NightContrast = 0.92f;

	[Export(PropertyHint.Range, "0.0,1.5,0.01")]
	public float NightSaturation = 0.82f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float NightSkyAmount = 0.70f;

	[Export(PropertyHint.Range, "0.0,0.1,0.001")]
	public float NightFogDensity;

	[ExportGroup("Transition")]
	[Export(PropertyHint.Range, "0.0,3.0,0.05")]
	public float FadeDuration = 0.8f;

	[ExportGroup("Day Night Sun Path")]
	[Export] public bool AnimateSunPath = true;
	[Export] public bool EnableSunShadowsDuringCycle = true;

	[Export(PropertyHint.Range, "-45.0,45.0,1.0")]
	public float SunPathTiltDegrees;

	[Export] public bool ReverseSunPath;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float CycleShadowOpacity = 0.7f;

	[Export(PropertyHint.Range, "0.02,0.5,0.01")]
	public float ShadowHorizonFade = 0.18f;

	private TurnManager _turnManager;
	private BoardManager _boardManager;
	private DirectionalLight3D _directionalLight;
	private NightFireflyController _nightFireflies;
	private Tween _transitionTween;
	private Tween _dayNightTween;
	private Tween _sunPathTween;
	private Tween _droughtHeatWaveTween;
	private ShaderMaterial _skyMaterial;
	private ShaderMaterial _droughtHeatWaveMaterial;
	private CanvasLayer _droughtHeatWaveLayer;
	private float _droughtHeatWaveIntensity;
	private float _droughtHeatWaveTargetIntensity;

	private Color _baseBackgroundColor;
	private Color _baseAmbientColor;
	private Color _baseLightColor = Colors.White;
	private bool _baseAdjustmentEnabled;
	private float _baseAmbientEnergy;
	private float _baseLightEnergy = 1.0f;
	private float _baseBrightness;
	private float _baseContrast;
	private float _baseSaturation;
	private float _baseVolumetricFogDensity;
	private float _skyNightAmount;
	private float _cycleDayAmbientEnergy;
	private float _cycleDayLightEnergy;
	private float _cycleDayFogDensity;
	private WorldLook _currentLook = WorldLook.Normal;
	private WorldLook _requestedLook = WorldLook.Normal;
	private bool _isDayNightCycleActive;
	private Transform3D _cycleSunStartTransform;
	private Vector3 _sunOrbitAxis = Vector3.Right;
	private bool _cycleSunStartShadowEnabled;
	private float _cycleSunStartShadowOpacity;
	private bool _hasCycleSunState;

	public override void _Ready()
	{
		if (Environment == null)
		{
			GD.PushWarning("DroughtWorldEffect: Environment fehlt.");
			return;
		}

		_directionalLight =
			GetNodeOrNull<DirectionalLight3D>(DirectionalLightPath);
		_nightFireflies = GetTree()?.GetFirstNodeInGroup(
			NightFirefliesGroup) as NightFireflyController;
		_nightFireflies?.SetNightAmount(0.0f);
		_boardManager = GetNodeOrNull<BoardManager>("../BoardManager");
		_skyMaterial = Environment.Sky?.SkyMaterial as ShaderMaterial;
		SetupDroughtHeatWaves();

		if (Engine.IsEditorHint())
		{
			UpdateDroughtHeatWaveEditorPreview();
			return;
		}

		_turnManager = GetNodeOrNull<TurnManager>(TurnManagerPath);
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

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
			UpdateDroughtHeatWaveEditorPreview();
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
		if (_sunPathTween != null && _sunPathTween.IsValid())
			_sunPathTween.Kill();
		if (_droughtHeatWaveTween != null && _droughtHeatWaveTween.IsValid())
			_droughtHeatWaveTween.Kill();
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
		if (_sunPathTween != null && _sunPathTween.IsValid())
			_sunPathTween.Kill();

		RestoreSunAfterCycle();

		_isDayNightCycleActive = true;
		Environment.AdjustmentEnabled = true;

		float startingAmbientEnergy = Environment.AmbientLightEnergy;
		float startingLightEnergy = _directionalLight?.LightEnergy ?? _baseLightEnergy;
		float startingFogDensity = Environment.VolumetricFogDensity;
		float startingSkyNightAmount = _skyNightAmount;
		_cycleDayAmbientEnergy = startingAmbientEnergy;
		_cycleDayLightEnergy = startingLightEnergy;
		_cycleDayFogDensity = startingFogDensity;

		_dayNightTween = CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		AppendDayCycleLook(
			NightBackgroundColor,
			NightAmbientColor,
			NightAmbientColor,
			NightAmbientEnergy,
			0.0f,
			NightBrightness,
			NightContrast,
			NightSaturation,
			startingSkyNightAmount,
			NightSkyAmount,
			NightFogDensity,
			DimmingDuration);
		_dayNightTween.TweenInterval(NightHoldDuration);
		_dayNightTween.TweenInterval(SunriseMoonPhaseDuration);
		_dayNightTween.TweenCallback(Callable.From(StartDayNightBrightening));
		StartSunPath();

		return DayNightCycleDuration;
	}

	public void CancelDayNightCycle()
	{
		if (_dayNightTween != null && _dayNightTween.IsValid())
			_dayNightTween.Kill();
		if (_sunPathTween != null && _sunPathTween.IsValid())
			_sunPathTween.Kill();

		bool wasActive = _isDayNightCycleActive;
		_isDayNightCycleActive = false;
		RestoreSunAfterCycle();
		RestoreDayNightAtmosphere();

		if (!wasActive)
			return;

		ApplyLook(_requestedLook, immediate: true);
	}

	private void StartSunPath()
	{
		if (!AnimateSunPath || _directionalLight == null)
			return;

		_cycleSunStartTransform = _directionalLight.GlobalTransform;
		_cycleSunStartShadowEnabled = _directionalLight.ShadowEnabled;
		_cycleSunStartShadowOpacity = _directionalLight.ShadowOpacity;
		_hasCycleSunState = true;

		Vector3 lightDirection =
			-_cycleSunStartTransform.Basis.Z.Normalized();
		_sunOrbitAxis = lightDirection.Cross(Vector3.Up).Normalized();
		if (_sunOrbitAxis.IsZeroApprox())
			_sunOrbitAxis = Vector3.Right;

		if (!Mathf.IsZeroApprox(SunPathTiltDegrees))
		{
			Basis tilt = new Basis(
				lightDirection,
				Mathf.DegToRad(SunPathTiltDegrees));
			_sunOrbitAxis = (tilt * _sunOrbitAxis).Normalized();
		}

		_directionalLight.ShadowEnabled = EnableSunShadowsDuringCycle;
		UpdateSunPath(0.0f);

		_sunPathTween = CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process)
			.SetTrans(Tween.TransitionType.Linear);
		_sunPathTween.TweenMethod(
			Callable.From<float>(UpdateSunPath),
			0.0f,
			1.0f,
			DayNightCycleDuration);
		_sunPathTween.TweenCallback(Callable.From(RestoreSunAfterCycle));
	}

	private void UpdateSunPath(float progress)
	{
		if (!_hasCycleSunState || _directionalLight == null)
			return;

		float direction = ReverseSunPath ? -1.0f : 1.0f;
		float angle = Mathf.Tau * Mathf.Clamp(progress, 0.0f, 1.0f) * direction;
		Basis orbit = new Basis(_sunOrbitAxis, angle);
		Basis rotatedBasis = orbit * _cycleSunStartTransform.Basis;
		_directionalLight.GlobalTransform = new Transform3D(
			rotatedBasis,
			_cycleSunStartTransform.Origin);

		if (!EnableSunShadowsDuringCycle)
			return;

		Vector3 lightDirection = -rotatedBasis.Z.Normalized();
		float height = Mathf.Max(-lightDirection.Y, 0.0f);
		float shadowFactor = Mathf.Clamp(
			height / Mathf.Max(ShadowHorizonFade, 0.001f),
			0.0f,
			1.0f);
		shadowFactor =
			shadowFactor * shadowFactor * (3.0f - (2.0f * shadowFactor));
		_directionalLight.ShadowOpacity =
			CycleShadowOpacity * shadowFactor;
	}

	private void RestoreSunAfterCycle()
	{
		if (!_hasCycleSunState || _directionalLight == null)
			return;

		_directionalLight.GlobalTransform = _cycleSunStartTransform;
		_directionalLight.ShadowEnabled = _cycleSunStartShadowEnabled;
		_directionalLight.ShadowOpacity = _cycleSunStartShadowOpacity;
		_hasCycleSunState = false;
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
		_baseVolumetricFogDensity = Environment.VolumetricFogDensity;
		SetSkyNightAmount(0.0f);

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
		_boardManager?.SetDecorativeGrassDroughtActive(
			look == WorldLook.Drought);
		SetDroughtHeatWavesActive(
			look,
			immediate);

		if (_isDayNightCycleActive)
			return;

		if (!immediate && !force && _currentLook == look)
			return;

		_currentLook = look;

		if (_transitionTween != null && _transitionTween.IsValid())
			_transitionTween.Kill();

		ResolveLookValues(
			look,
			out Color targetBackground,
			out Color targetAmbient,
			out Color targetLight,
			out float targetBrightness,
			out float targetContrast,
			out float targetSaturation);

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

	private void SetupDroughtHeatWaves()
	{
		if (!EnableDroughtHeatWaves || _droughtHeatWaveMaterial != null)
			return;

		Shader heatWaveShader = GD.Load<Shader>(DroughtHeatWaveShaderPath);
		if (heatWaveShader == null)
		{
			GD.PushWarning(
				$"DroughtWorldEffect: Hitzewellen-Shader fehlt: " +
				DroughtHeatWaveShaderPath);
			return;
		}

		_droughtHeatWaveMaterial = new ShaderMaterial
		{
			Shader = heatWaveShader
		};
		ApplyDroughtHeatWaveShaderSettings();

		_droughtHeatWaveLayer = new CanvasLayer
		{
			Name = "DroughtHeatWaveLayer",
			Layer = -1,
			Visible = false
		};
		AddChild(_droughtHeatWaveLayer);

		ColorRect heatWaveOverlay = new ColorRect
		{
			Name = "DroughtHeatWaveOverlay",
			Color = Colors.White,
			Material = _droughtHeatWaveMaterial,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_droughtHeatWaveLayer.AddChild(heatWaveOverlay);
		heatWaveOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		heatWaveOverlay.OffsetLeft = 0.0f;
		heatWaveOverlay.OffsetTop = 0.0f;
		heatWaveOverlay.OffsetRight = 0.0f;
		heatWaveOverlay.OffsetBottom = 0.0f;

		SetDroughtHeatWaveIntensity(0.0f);
	}

	private void UpdateDroughtHeatWaveEditorPreview()
	{
		if (_droughtHeatWaveMaterial == null && EnableDroughtHeatWaves)
			SetupDroughtHeatWaves();

		if (_droughtHeatWaveMaterial == null)
			return;

		ApplyDroughtHeatWaveShaderSettings();
		float previewIntensity =
			EnableDroughtHeatWaves && PreviewDroughtHeatWaves
				? Mathf.Clamp(DroughtHeatWaveIntensity, 0.0f, 1.0f)
				: 0.0f;
		_droughtHeatWaveTargetIntensity = previewIntensity;
		SetDroughtHeatWaveIntensity(previewIntensity);
	}

	private void ApplyDroughtHeatWaveShaderSettings()
	{
		_droughtHeatWaveMaterial.SetShaderParameter(
			"distortion_strength",
			Mathf.Max(DroughtHeatWaveDistortionStrength, 0.0f));
		_droughtHeatWaveMaterial.SetShaderParameter(
			"effect_height",
			Mathf.Clamp(DroughtHeatWaveHeight, 0.01f, 1.0f));
		_droughtHeatWaveMaterial.SetShaderParameter(
			"rise_speed",
			Mathf.Max(DroughtHeatWaveRiseSpeed, 0.0f));
		_droughtHeatWaveMaterial.SetShaderParameter(
			"plume_count",
			Mathf.Clamp(DroughtHeatWavePlumeCount, 4.0f, 5.0f));
		_droughtHeatWaveMaterial.SetShaderParameter(
			"plume_width",
			Mathf.Clamp(DroughtHeatWavePlumeWidth, 0.02f, 0.12f));
		_droughtHeatWaveMaterial.SetShaderParameter(
			"lateral_drift",
			Mathf.Clamp(DroughtHeatWaveLateralDrift, 0.0f, 0.2f));
		_droughtHeatWaveMaterial.SetShaderParameter(
			"shimmer_strength",
			Mathf.Clamp(DroughtHeatWaveShimmerStrength, 0.0f, 1.0f));
		_droughtHeatWaveMaterial.SetShaderParameter(
			"chromatic_split",
			Mathf.Clamp(DroughtHeatWaveChromaticSplit, 0.0f, 0.008f));
	}

	private void SetDroughtHeatWavesActive(WorldLook look, bool immediate)
	{
		if (_droughtHeatWaveMaterial == null ||
			_droughtHeatWaveLayer == null)
		{
			return;
		}

		float targetIntensity = look switch
		{
			WorldLook.Drought when EnableDroughtHeatWaves =>
				Mathf.Clamp(DroughtHeatWaveIntensity, 0.0f, 1.0f),
			WorldLook.HeatDay when EnableDroughtHeatWaves =>
				Mathf.Clamp(HeatDayHeatWaveIntensity, 0.0f, 1.0f),
			_ => 0.0f
		};

		if (!immediate &&
			Mathf.IsEqualApprox(
				_droughtHeatWaveTargetIntensity,
				targetIntensity))
		{
			return;
		}

		_droughtHeatWaveTargetIntensity = targetIntensity;

		if (_droughtHeatWaveTween != null &&
			_droughtHeatWaveTween.IsValid())
		{
			_droughtHeatWaveTween.Kill();
		}

		float duration = immediate
			? 0.0f
			: Mathf.Max(DroughtHeatWaveFadeDuration, 0.0f);

		if (duration <= 0.001f ||
			Mathf.IsEqualApprox(
				_droughtHeatWaveIntensity,
				targetIntensity))
		{
			SetDroughtHeatWaveIntensity(targetIntensity);
			return;
		}

		_droughtHeatWaveLayer.Visible = true;
		_droughtHeatWaveTween = CreateTween()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_droughtHeatWaveTween.TweenMethod(
			Callable.From<float>(SetDroughtHeatWaveIntensity),
			_droughtHeatWaveIntensity,
			targetIntensity,
			duration);
	}

	private void SetDroughtHeatWaveIntensity(float intensity)
	{
		if (_droughtHeatWaveMaterial == null)
			return;

		_droughtHeatWaveIntensity = Mathf.Clamp(intensity, 0.0f, 1.0f);
		_droughtHeatWaveMaterial.SetShaderParameter(
			"intensity",
			_droughtHeatWaveIntensity);

		if (_droughtHeatWaveLayer != null)
		{
			_droughtHeatWaveLayer.Visible =
				_droughtHeatWaveIntensity > 0.001f ||
				_droughtHeatWaveTargetIntensity > 0.001f;
		}
	}

	private void ResolveLookValues(
		WorldLook look,
		out Color background,
		out Color ambient,
		out Color light,
		out float brightness,
		out float contrast,
		out float saturation)
	{
		background = _baseBackgroundColor;
		ambient = _baseAmbientColor;
		light = _baseLightColor;
		brightness = _baseBrightness;
		contrast = _baseContrast;
		saturation = _baseSaturation;

		if (look == WorldLook.Drought)
		{
			background = DroughtBackgroundColor;
			ambient = DroughtAmbientColor;
			light = DroughtLightColor;
			brightness = DroughtBrightness;
			contrast = DroughtContrast;
			saturation = DroughtSaturation;
		}
		else if (look == WorldLook.HeatDay)
		{
			background = HeatBackgroundColor;
			ambient = HeatAmbientColor;
			light = HeatLightColor;
			brightness = HeatBrightness;
			contrast = HeatContrast;
			saturation = HeatSaturation;
		}
		else if (look == WorldLook.Rain)
		{
			background = RainBackgroundColor;
			ambient = RainAmbientColor;
			light = RainLightColor;
			brightness = RainBrightness;
			contrast = RainContrast;
			saturation = RainSaturation;
		}
		else if (look == WorldLook.HeavyRain)
		{
			background = HeavyRainBackgroundColor;
			ambient = HeavyRainAmbientColor;
			light = HeavyRainLightColor;
			brightness = HeavyRainBrightness;
			contrast = HeavyRainContrast;
			saturation = HeavyRainSaturation;
		}
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
		float skyNightAmountFrom,
		float skyNightAmountTo,
		float volumetricFogDensity,
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
		_dayNightTween.TweenProperty(
			Environment,
			"volumetric_fog_density",
			volumetricFogDensity,
			duration);
		_dayNightTween.TweenMethod(
			Callable.From<float>(SetSkyNightAmount),
			skyNightAmountFrom,
			skyNightAmountTo,
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
		_currentLook = _requestedLook;
		RestoreAdjustmentStateIfNormal();
	}

	private void StartDayNightBrightening()
	{
		if (!_isDayNightCycleActive || Environment == null)
			return;

		ResolveLookValues(
			_requestedLook,
			out Color targetBackground,
			out Color targetAmbient,
			out Color targetLight,
			out float targetBrightness,
			out float targetContrast,
			out float targetSaturation);

		_dayNightTween = CreateTween()
			.SetPauseMode(Tween.TweenPauseMode.Process)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		AppendDayCycleLook(
			targetBackground,
			targetAmbient,
			targetLight,
			_cycleDayAmbientEnergy,
			_cycleDayLightEnergy,
			targetBrightness,
			targetContrast,
			targetSaturation,
			_skyNightAmount,
			0.0f,
			_cycleDayFogDensity,
			BrighteningDuration);
		_dayNightTween.TweenCallback(Callable.From(FinishDayNightCycle));
	}

	private void SetSkyNightAmount(float amount)
	{
		_skyNightAmount = Mathf.Clamp(amount, 0.0f, 1.0f);
		_skyMaterial?.SetShaderParameter(
			"night_amount",
			_skyNightAmount);
		_nightFireflies?.SetNightAmount(_skyNightAmount);
	}

	private void RestoreDayNightAtmosphere()
	{
		SetSkyNightAmount(0.0f);
		Environment.VolumetricFogDensity = _baseVolumetricFogDensity;
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
			GameEventType.Rain => WorldLook.Rain,
			GameEventType.HeavyRain => WorldLook.HeavyRain,
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
