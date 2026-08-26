using Godot;
using System;
using System.Collections.Generic;

public partial class WindMapStreakEffect : Node3D
{
	private const string ShaderPath =
		"res://shaders/wind-map-streaks.gdshader";

	[ExportGroup("Connections")]
	[Export] public NodePath TurnManagerPath =
		new NodePath("../TurnManager");

	[ExportGroup("General")]
	[Export] public bool EffectEnabled = true;

	[Export(PropertyHint.Range, "1,160,1")]
	public int StreakCount = 36;

	[Export(PropertyHint.Range, "6,48,1")]
	public int RibbonSegments = 20;

	[Export] public int LayoutSeed = 4187;

	[ExportGroup("Map Coverage")]
	[Export(PropertyHint.Range, "4.0,60.0,0.5")]
	public float TravelDistance = 30.0f;

	[Export(PropertyHint.Range, "2.0,50.0,0.5")]
	public float CrossSpread = 24.0f;

	[Export(PropertyHint.Range, "-1.0,5.0,0.05")]
	public float BaseHeight = 0.72f;

	[Export(PropertyHint.Range, "0.0,2.0,0.05")]
	public float HeightVariation = 0.32f;

	[Export(PropertyHint.Range, "-180.0,180.0,0.5")]
	public float DirectionDegrees = 18.0f;

	[Export(PropertyHint.Range, "0.0,35.0,0.5")]
	public float DirectionVariationDegrees = 7.0f;

	[ExportGroup("Shape")]
	[Export(PropertyHint.Range, "0.2,8.0,0.05")]
	public float MinLength = 1.15f;

	[Export(PropertyHint.Range, "0.2,10.0,0.05")]
	public float MaxLength = 3.4f;

	[Export(PropertyHint.Range, "0.005,0.30,0.005")]
	public float MinWidth = 0.025f;

	[Export(PropertyHint.Range, "0.005,0.40,0.005")]
	public float MaxWidth = 0.075f;

	[Export(PropertyHint.Range, "0.0,0.8,0.01")]
	public float WaveAmplitude = 0.075f;

	[Export(PropertyHint.Range, "0.1,8.0,0.05")]
	public float WaveFrequency = 1.45f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float LoopChance = 0.12f;

	[Export(PropertyHint.Range, "0.05,1.5,0.05")]
	public float LoopRadius = 0.38f;

	[Export(PropertyHint.Range, "1.0,2.0,1.0")]
	public float LoopTurns = 1.0f;

	[Export(PropertyHint.Range, "0.15,0.75,0.01")]
	public float LoopPosition = 0.48f;

	[Export(PropertyHint.Range, "0.0,0.25,0.01")]
	public float LoopPositionVariation = 0.16f;

	[Export(PropertyHint.Range, "0.0,0.35,0.01")]
	public float VerticalLift = 0.045f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float BrokenStreakChance = 0.16f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float BrokenStreakStrength = 0.48f;

	[ExportGroup("Motion")]
	[Export(PropertyHint.Range, "0.2,30.0,0.1")]
	public float MinSpeed = 6.0f;

	[Export(PropertyHint.Range, "0.2,40.0,0.1")]
	public float MaxSpeed = 10.0f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float NormalAppearanceChance = 0.08f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float WindEventAppearanceChance = 0.46f;

	[Export(PropertyHint.Range, "0.001,0.20,0.001")]
	public float AppearanceFadeSoftness = 0.035f;

	[Export(PropertyHint.Range, "0.08,1.0,0.01")]
	public float VisibleTravelFraction = 0.32f;

	[Export(PropertyHint.Range, "0.25,3.0,0.05")]
	public float WindEventSpeedMultiplier = 1.12f;

	[Export(PropertyHint.Range, "0.1,5.0,0.05")]
	public float EventTransitionSpeed = 1.4f;

	[Export(PropertyHint.Range, "0.01,0.35,0.01")]
	public float CycleFade = 0.12f;

	[ExportGroup("Look")]
	[Export] public Color StreakColor =
		new Color(0.89f, 0.96f, 1.0f, 1.0f);

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float Alpha = 0.58f;

	[Export(PropertyHint.Range, "0.0,4.0,0.05")]
	public float EmissionStrength = 0.68f;

	[Export(PropertyHint.Range, "0.2,2.5,0.05")]
	public float WindEventBrightnessMultiplier = 1.22f;

	[Export(PropertyHint.Range, "0.01,0.48,0.01")]
	public float EndFade = 0.18f;

	[Export(PropertyHint.Range, "0.0,0.5,0.01")]
	public float CenterHighlight = 0.16f;

	private TurnManager _turnManager;
	private MultiMeshInstance3D _streakInstance;
	private ShaderMaterial _material;
	private float _eventBlend;
	private float _animationTime;
	private bool _windEventActive;

	private int _builtStreakCount = -1;
	private int _builtRibbonSegments = -1;
	private int _builtLayoutSeed;
	private float _builtCrossSpread;
	private float _builtBaseHeight;
	private float _builtHeightVariation;
	private float _builtDirectionDegrees;
	private float _builtDirectionVariationDegrees;

	public override void _Ready()
	{
		BuildVisual();
		ConnectTurnManager();
		RefreshWindEventState();
		ApplyShaderParameters();
	}

	public override void _Process(double delta)
	{
		if (NeedsVisualRebuild())
			BuildVisual();

		float targetBlend = _windEventActive ? 1.0f : 0.0f;
		_eventBlend = Mathf.MoveToward(
			_eventBlend,
			targetBlend,
			EventTransitionSpeed * (float)delta);
		_animationTime += (float)delta * Mathf.Lerp(
			1.0f,
			WindEventSpeedMultiplier,
			_eventBlend);

		if (_streakInstance != null)
			_streakInstance.Visible = EffectEnabled;

		ApplyShaderParameters();
	}

	public override void _ExitTree()
	{
		if (_turnManager == null)
			return;

		_turnManager.TurnStarted -= OnTurnStarted;
		_turnManager.EventActivated -= OnEventActivated;
		_turnManager.EventPhaseResolved -= OnEventPhaseResolved;
		_turnManager.GameEnded -= OnGameEnded;
	}

	private void ConnectTurnManager()
	{
		_turnManager = GetNodeOrNull<TurnManager>(TurnManagerPath);
		if (_turnManager == null)
		{
			GD.PushWarning("WindMapStreakEffect: TurnManager fehlt.");
			return;
		}

		_turnManager.TurnStarted += OnTurnStarted;
		_turnManager.EventActivated += OnEventActivated;
		_turnManager.EventPhaseResolved += OnEventPhaseResolved;
		_turnManager.GameEnded += OnGameEnded;
	}

	private void BuildVisual()
	{
		_streakInstance?.QueueFree();
		_streakInstance = null;
		_material = null;

		Shader shader = GD.Load<Shader>(ShaderPath);
		if (shader == null)
		{
			GD.PushWarning("WindMapStreakEffect: Shader fehlt.");
			return;
		}

		_material = new ShaderMaterial
		{
			Shader = shader
		};

		MultiMesh multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseCustomData = true,
			Mesh = CreateRibbonMesh(),
			InstanceCount = Math.Max(StreakCount, 1),
			VisibleInstanceCount = -1
		};

		for (int index = 0; index < multiMesh.InstanceCount; index++)
			ConfigureInstance(multiMesh, index);

		_streakInstance = new MultiMeshInstance3D
		{
			Name = "WindStreaks",
			Multimesh = multiMesh,
			MaterialOverride = _material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			ExtraCullMargin = TravelDistance + MaxLength + LoopRadius * 2.0f
		};
		AddChild(_streakInstance);

		_builtStreakCount = StreakCount;
		_builtRibbonSegments = RibbonSegments;
		_builtLayoutSeed = LayoutSeed;
		_builtCrossSpread = CrossSpread;
		_builtBaseHeight = BaseHeight;
		_builtHeightVariation = HeightVariation;
		_builtDirectionDegrees = DirectionDegrees;
		_builtDirectionVariationDegrees = DirectionVariationDegrees;
	}

	private ArrayMesh CreateRibbonMesh()
	{
		SurfaceTool surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		int segmentCount = Math.Max(RibbonSegments, 1);
		for (int segment = 0; segment < segmentCount; segment++)
		{
			float start = segment / (float)segmentCount;
			float end = (segment + 1) / (float)segmentCount;
			Vector3 startLeft = new Vector3(start - 0.5f, 0.0f, -0.5f);
			Vector3 startRight = new Vector3(start - 0.5f, 0.0f, 0.5f);
			Vector3 endRight = new Vector3(end - 0.5f, 0.0f, 0.5f);
			Vector3 endLeft = new Vector3(end - 0.5f, 0.0f, -0.5f);

			AddRibbonVertex(surface, startLeft, new Vector2(start, 0.0f));
			AddRibbonVertex(surface, startRight, new Vector2(start, 1.0f));
			AddRibbonVertex(surface, endRight, new Vector2(end, 1.0f));

			AddRibbonVertex(surface, startLeft, new Vector2(start, 0.0f));
			AddRibbonVertex(surface, endRight, new Vector2(end, 1.0f));
			AddRibbonVertex(surface, endLeft, new Vector2(end, 0.0f));
		}

		return surface.Commit();
	}

	private static void AddRibbonVertex(
		SurfaceTool surface,
		Vector3 position,
		Vector2 uv)
	{
		surface.SetNormal(Vector3.Up);
		surface.SetUV(uv);
		surface.AddVertex(position);
	}

	private void ConfigureInstance(MultiMesh multiMesh, int index)
	{
		uint seed = unchecked((uint)LayoutSeed) +
			unchecked((uint)index * 0x9E3779B9u);
		float crossRandom = HashToUnit(seed ^ 0xA511E9B3u);
		float heightRandom = HashToUnit(seed ^ 0x63D83595u);
		float directionRandom = HashToUnit(seed ^ 0xC2B2AE35u);
		float phaseRandom = HashToUnit(seed ^ 0x27D4EB2Fu);
		float speedRandom = HashToUnit(seed ^ 0x165667B1u);
		float shapeRandom = HashToUnit(seed ^ 0x85EBCA77u);
		float detailRandom = HashToUnit(seed ^ 0xD3A2646Cu);

		float angle = Mathf.DegToRad(
			DirectionDegrees +
			(directionRandom * 2.0f - 1.0f) *
			DirectionVariationDegrees);
		Vector3 travelDirection = new Vector3(
			Mathf.Cos(angle),
			0.0f,
			Mathf.Sin(angle));
		Vector3 crossDirection = new Vector3(
			-travelDirection.Z,
			0.0f,
			travelDirection.X);
		Vector3 origin =
			crossDirection * ((crossRandom - 0.5f) * CrossSpread) +
			Vector3.Up * (BaseHeight + heightRandom * HeightVariation);
		Basis basis = Basis.Identity.Rotated(Vector3.Up, -angle);

		multiMesh.SetInstanceTransform(
			index,
			new Transform3D(basis, origin));
		multiMesh.SetInstanceCustomData(
			index,
			new Color(
				phaseRandom,
				speedRandom,
				shapeRandom,
				detailRandom));
	}

	private bool NeedsVisualRebuild()
	{
		return _streakInstance == null ||
			_builtStreakCount != StreakCount ||
			_builtRibbonSegments != RibbonSegments ||
			_builtLayoutSeed != LayoutSeed ||
			!Mathf.IsEqualApprox(_builtCrossSpread, CrossSpread) ||
			!Mathf.IsEqualApprox(_builtBaseHeight, BaseHeight) ||
			!Mathf.IsEqualApprox(_builtHeightVariation, HeightVariation) ||
			!Mathf.IsEqualApprox(_builtDirectionDegrees, DirectionDegrees) ||
			!Mathf.IsEqualApprox(
				_builtDirectionVariationDegrees,
				DirectionVariationDegrees);
	}

	private void ApplyShaderParameters()
	{
		if (_material == null)
			return;

		_material.SetShaderParameter("animation_time", _animationTime);
		_material.SetShaderParameter("event_blend", _eventBlend);
		_material.SetShaderParameter("travel_distance", TravelDistance);
		_material.SetShaderParameter("min_length", MinLength);
		_material.SetShaderParameter("max_length", MaxLength);
		_material.SetShaderParameter("min_width", MinWidth);
		_material.SetShaderParameter("max_width", MaxWidth);
		_material.SetShaderParameter("min_speed", MinSpeed);
		_material.SetShaderParameter("max_speed", MaxSpeed);
		_material.SetShaderParameter("wave_amplitude", WaveAmplitude);
		_material.SetShaderParameter("wave_frequency", WaveFrequency);
		_material.SetShaderParameter("loop_chance", LoopChance);
		_material.SetShaderParameter("loop_radius", LoopRadius);
		_material.SetShaderParameter("loop_turns", LoopTurns);
		_material.SetShaderParameter("loop_position", LoopPosition);
		_material.SetShaderParameter(
			"loop_position_variation",
			LoopPositionVariation);
		_material.SetShaderParameter("vertical_lift", VerticalLift);
		_material.SetShaderParameter(
			"broken_streak_chance",
			BrokenStreakChance);
		_material.SetShaderParameter(
			"broken_streak_strength",
			BrokenStreakStrength);
		_material.SetShaderParameter(
			"normal_appearance_chance",
			NormalAppearanceChance);
		_material.SetShaderParameter(
			"event_appearance_chance",
			WindEventAppearanceChance);
		_material.SetShaderParameter(
			"appearance_fade_softness",
			AppearanceFadeSoftness);
		_material.SetShaderParameter(
			"visible_travel_fraction",
			VisibleTravelFraction);
		_material.SetShaderParameter("cycle_fade", CycleFade);
		_material.SetShaderParameter("streak_color", StreakColor);
		_material.SetShaderParameter("base_alpha", Alpha);
		_material.SetShaderParameter(
			"emission_strength",
			EmissionStrength);
		_material.SetShaderParameter(
			"event_brightness_multiplier",
			WindEventBrightnessMultiplier);
		_material.SetShaderParameter("end_fade", EndFade);
		_material.SetShaderParameter("center_highlight", CenterHighlight);

		if (_streakInstance != null)
		{
			_streakInstance.ExtraCullMargin =
				TravelDistance + MaxLength + LoopRadius * 2.0f;
		}
	}

	private void RefreshWindEventState()
	{
		_windEventActive =
			_turnManager?.State != null &&
			ContainsWind(_turnManager.State.ActiveEvents);
	}

	private void OnTurnStarted(int round)
	{
		RefreshWindEventState();
	}

	private void OnEventActivated(GameEventType eventType)
	{
		if (eventType == GameEventType.Wind)
			_windEventActive = true;
		else
			RefreshWindEventState();
	}

	private void OnEventPhaseResolved(EventPhaseResult result)
	{
		_windEventActive = ContainsWind(result.ActiveEvents);
	}

	private void OnGameEnded(GameState state)
	{
		_windEventActive = false;
	}

	private static bool ContainsWind(
		IReadOnlyList<ActiveGameEvent> activeEvents)
	{
		foreach (ActiveGameEvent activeEvent in activeEvents)
		{
			if (activeEvent?.Definition?.Type == GameEventType.Wind)
				return true;
		}

		return false;
	}

	private static bool ContainsWind(
		IReadOnlyList<GameEventType> activeEvents)
	{
		foreach (GameEventType eventType in activeEvents)
		{
			if (eventType == GameEventType.Wind)
				return true;
		}

		return false;
	}

	private static float HashToUnit(uint value)
	{
		value ^= value >> 16;
		value *= 0x7FEB352Du;
		value ^= value >> 15;
		value *= 0x846CA68Bu;
		value ^= value >> 16;
		return (value & 0x00FFFFFFu) / 16777215.0f;
	}
}
