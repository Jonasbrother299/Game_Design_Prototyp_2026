using Godot;
using System;
using System.Collections.Generic;

public partial class WindMapStreakEffect : Node3D
{
	private const string ShaderPath =
		"res://shaders/wind-map-streaks.gdshader";
	private const string LeafShaderPath =
		"res://shaders/wind-leaf-particles.gdshader";

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

	[ExportGroup("Windpartikel")]
	[Export] public bool LeafParticlesEnabled = true;

	[Export(PropertyHint.Range, "16,256,1")]
	public int LeafParticleCount = 28;

	[Export(PropertyHint.Range, "16,320,1")]
	public int DustParticleCount = 48;

	[Export(PropertyHint.Range, "1.0,8.0,0.1")]
	public float LeafLifetime = 4.2f;

	[Export] public Vector3 LeafEmissionSize = new Vector3(10.8f, 4.8f, 1.8f);

	[Export(PropertyHint.Range, "0.0,8.0,0.1")]
	public float LeafEmissionHeight = 0.0f;

	[Export(PropertyHint.Range, "1.0,12.0,0.1")]
	public float LeafCameraDistance = 5.2f;

	[ExportGroup("Windpartikel - Bewegung")]
	[Export(PropertyHint.Range, "0.2,20.0,0.1")]
	public float LeafMinSpeed = 4.8f;

	[Export(PropertyHint.Range, "0.2,24.0,0.1")]
	public float LeafMaxSpeed = 8.8f;

	[Export(PropertyHint.Range, "0.02,1.0,0.01")]
	public float LeafMinSize = 0.16f;

	[Export(PropertyHint.Range, "0.02,1.2,0.01")]
	public float LeafMaxSize = 0.46f;

	[Export(PropertyHint.Range, "0.0,2.0,0.05")]
	public float LeafFallSpeed = 0.55f;

	[Export(PropertyHint.Range, "0.0,2.0,0.05")]
	public float LeafVerticalWobble = 0.7f;

	[Export(PropertyHint.Range, "0.0,2.0,0.05")]
	public float LeafSideWobble = 0.55f;

	[Export(PropertyHint.Range, "0.1,5.0,0.05")]
	public float LeafWobbleFrequencyMin = 1.1f;

	[Export(PropertyHint.Range, "0.1,6.0,0.05")]
	public float LeafWobbleFrequencyMax = 2.7f;

	[Export(PropertyHint.Range, "0.0,1.5,0.01")]
	public float LeafLateralSpread = 0.72f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float LeafVerticalSpread = 0.22f;

	[Export(PropertyHint.Range, "0.0,8.0,0.05")]
	public float LeafMinSpin = 1.2f;

	[Export(PropertyHint.Range, "0.0,10.0,0.05")]
	public float LeafMaxSpin = 4.3f;

	[ExportGroup("Windpartikel - Böen")]
	[Export(PropertyHint.Range, "0.0,5.0,0.01")]
	public float GustPrimarySpeed = 1.65f;

	[Export(PropertyHint.Range, "0.0,5.0,0.01")]
	public float GustSecondarySpeed = 0.59f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float GustMinimumStrength = 0.32f;

	[ExportGroup("Staubpartikel")]
	[Export(PropertyHint.Range, "0.1,2.0,0.01")]
	public float DustLifetimeMultiplier = 0.62f;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float DustMinSpeedMultiplier = 0.62f;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float DustMaxSpeedMultiplier = 0.82f;

	[Export(PropertyHint.Range, "-1.0,2.0,0.01")]
	public float DustMinFallSpeed = -0.08f;

	[Export(PropertyHint.Range, "-1.0,2.0,0.01")]
	public float DustMaxFallSpeed = 0.18f;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float DustVerticalWobbleMultiplier = 0.55f;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float DustSideWobbleMultiplier = 0.45f;

	[Export(PropertyHint.Range, "0.005,0.2,0.001")]
	public float DustMinSize = 0.018f;

	[Export(PropertyHint.Range, "0.005,0.3,0.001")]
	public float DustMaxSize = 0.052f;

	[Export(PropertyHint.Range, "0.0,5.0,0.05")]
	public float DustMinSpin = 0.35f;

	[Export(PropertyHint.Range, "0.0,6.0,0.05")]
	public float DustMaxSpin = 1.3f;

	private TurnManager _turnManager;
	private MultiMeshInstance3D _streakInstance;
	private ShaderMaterial _material;
	private GpuParticles3D _leafParticles;
	private GpuParticles3D _dustParticles;
	private ShaderMaterial _leafProcessMaterial;
	private ShaderMaterial _dustProcessMaterial;
	private float _eventBlend;
	private float _animationTime;
	private float _gustTime;
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
		BuildLeafParticles();
		ConnectTurnManager();
		RefreshWindEventState();
		UpdateLeafParticleTransform();
		ApplyShaderParameters();
		ApplyLeafParticleParameters();
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
		_gustTime += (float)delta;

		if (_streakInstance != null)
			_streakInstance.Visible = EffectEnabled;

		UpdateLeafParticleTransform();
		ApplyShaderParameters();
		ApplyLeafParticleParameters();
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

	private void BuildLeafParticles()
	{
		Shader shader = GD.Load<Shader>(LeafShaderPath);
		if (shader == null)
		{
			GD.PushWarning("WindMapStreakEffect: Blatt-Partikel-Shader fehlt.");
			return;
		}

		StandardMaterial3D drawMaterial = new StandardMaterial3D
		{
			AlbedoColor = Colors.White,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			VertexColorUseAsAlbedo = true
		};
		ArrayMesh particleMesh = CreateLeafParticleMesh(drawMaterial);

		_leafProcessMaterial = new ShaderMaterial
		{
			Shader = shader
		};
		_dustProcessMaterial = new ShaderMaterial
		{
			Shader = shader
		};

		_leafParticles = CreateParticleEmitter(
			"WindLeaves",
			LeafParticleCount,
			LeafLifetime,
			_leafProcessMaterial,
			particleMesh);
		_dustParticles = CreateParticleEmitter(
			"WindDust",
			DustParticleCount,
			Mathf.Max(LeafLifetime * DustLifetimeMultiplier, 1.0f),
			_dustProcessMaterial,
			particleMesh);

		AddChild(_leafParticles);
		AddChild(_dustParticles);
	}

	private void UpdateLeafParticleTransform()
	{
		if (_leafParticles == null || _dustParticles == null)
			return;

		Camera3D camera = GetViewport()?.GetCamera3D();
		if (camera == null)
			return;

		_leafParticles.GlobalTransform = camera.GlobalTransform;
		_dustParticles.GlobalTransform = camera.GlobalTransform;
	}

	private static GpuParticles3D CreateParticleEmitter(
		string name,
		int amount,
		float lifetime,
		ShaderMaterial processMaterial,
		Mesh particleMesh)
	{
		return new GpuParticles3D
		{
			Name = name,
			Amount = Math.Max(amount, 1),
			AmountRatio = 0.0f,
			Lifetime = Mathf.Max(lifetime, 0.1f),
			Randomness = 0.35f,
			Preprocess = 0.5f,
			Emitting = false,
			LocalCoords = true,
			FixedFps = 30,
			Interpolate = true,
			FractDelta = true,
			VisibilityAabb = new Aabb(
				new Vector3(-24.0f, -2.0f, -20.0f),
				new Vector3(48.0f, 14.0f, 40.0f)),
			ProcessMaterial = processMaterial,
			DrawPass1 = particleMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
	}

	private static ArrayMesh CreateLeafParticleMesh(Material material)
	{
		SurfaceTool surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		Vector3 center = new Vector3(0.0f, 0.035f, 0.0f);
		Vector3[] edge =
		{
			new Vector3(0.0f, 0.0f, 0.5f),
			new Vector3(-0.14f, 0.0f, 0.30f),
			new Vector3(-0.23f, -0.02f, 0.02f),
			new Vector3(-0.13f, 0.0f, -0.29f),
			new Vector3(0.0f, -0.01f, -0.5f),
			new Vector3(0.13f, 0.0f, -0.29f),
			new Vector3(0.23f, -0.02f, 0.02f),
			new Vector3(0.14f, 0.0f, 0.30f)
		};

		for (int index = 0; index < edge.Length; index++)
		{
			AddParticleMeshVertex(surface, center);
			AddParticleMeshVertex(surface, edge[index]);
			AddParticleMeshVertex(surface, edge[(index + 1) % edge.Length]);
		}

		ArrayMesh mesh = surface.Commit();
		mesh.SurfaceSetMaterial(0, material);
		return mesh;
	}

	private static void AddParticleMeshVertex(
		SurfaceTool surface,
		Vector3 position)
	{
		surface.SetColor(Colors.White);
		surface.SetNormal(Vector3.Up);
		surface.AddVertex(position);
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

	private void ApplyLeafParticleParameters()
	{
		if (
			_leafParticles == null ||
			_dustParticles == null ||
			_leafProcessMaterial == null ||
			_dustProcessMaterial == null)
		{
			return;
		}

		int leafAmount = Math.Max(LeafParticleCount, 1);
		int dustAmount = Math.Max(DustParticleCount, 1);
		float leafLifetime = Mathf.Max(LeafLifetime, 0.1f);
		float dustLifetime = Mathf.Max(
			LeafLifetime * DustLifetimeMultiplier,
			0.1f);

		if (_leafParticles.Amount != leafAmount)
			_leafParticles.Amount = leafAmount;
		if (_dustParticles.Amount != dustAmount)
			_dustParticles.Amount = dustAmount;
		if (!Mathf.IsEqualApprox(_leafParticles.Lifetime, leafLifetime))
			_leafParticles.Lifetime = leafLifetime;
		if (!Mathf.IsEqualApprox(_dustParticles.Lifetime, dustLifetime))
			_dustParticles.Lifetime = dustLifetime;

		float intensity = EffectEnabled && LeafParticlesEnabled
			? _eventBlend
			: 0.0f;
		float primaryGust =
			0.5f + 0.5f * Mathf.Sin(_gustTime * GustPrimarySpeed);
		float secondaryGust =
			0.5f + 0.5f * Mathf.Sin(
				_gustTime * GustSecondarySpeed + 1.7f);
		float minimumGust = Mathf.Clamp(
			GustMinimumStrength,
			0.0f,
			1.0f);
		float gust = Mathf.Clamp(
			minimumGust +
				(1.0f - minimumGust) * primaryGust * secondaryGust,
			0.0f,
			1.0f);
		float gustBurst = gust * gust;
		bool shouldEmit =
			EffectEnabled &&
			LeafParticlesEnabled &&
			(_windEventActive || intensity > 0.002f);

		_leafParticles.Visible = shouldEmit;
		_dustParticles.Visible = shouldEmit;
		_leafParticles.Emitting = shouldEmit;
		_dustParticles.Emitting = shouldEmit;
		_leafParticles.AmountRatio = Mathf.Clamp(
			intensity * Mathf.Lerp(0.28f, 1.0f, gustBurst),
			0.0f,
			1.0f);
		_dustParticles.AmountRatio = Mathf.Clamp(
			intensity * Mathf.Lerp(0.4f, 0.9f, gustBurst),
			0.0f,
			1.0f);

		Vector3 windDirection = Vector3.Forward;
		Vector3 emissionBox = new Vector3(
			Mathf.Max(Mathf.Abs(LeafEmissionSize.X), 0.1f),
			Mathf.Max(Mathf.Abs(LeafEmissionSize.Y), 0.1f),
			Mathf.Max(Mathf.Abs(LeafEmissionSize.Z), 0.1f));
		Vector3 emissionCenter = new Vector3(
			0.0f,
			LeafEmissionHeight,
			-Mathf.Max(LeafCameraDistance, 0.1f));
		float minSpeed = Mathf.Min(LeafMinSpeed, LeafMaxSpeed);
		float maxSpeed = Mathf.Max(LeafMinSpeed, LeafMaxSpeed);
		float minSize = Mathf.Min(LeafMinSize, LeafMaxSize);
		float maxSize = Mathf.Max(LeafMinSize, LeafMaxSize);

		ApplyCommonParticleParameters(
			_leafProcessMaterial,
			emissionCenter,
			emissionBox,
			windDirection,
			intensity,
			gust,
			LeafWobbleFrequencyMin,
			LeafWobbleFrequencyMax,
			LeafLateralSpread,
			LeafVerticalSpread);
		_leafProcessMaterial.SetShaderParameter("speed_min", minSpeed);
		_leafProcessMaterial.SetShaderParameter("speed_max", maxSpeed);
		_leafProcessMaterial.SetShaderParameter(
			"fall_speed_min",
			LeafFallSpeed * 0.55f);
		_leafProcessMaterial.SetShaderParameter(
			"fall_speed_max",
			LeafFallSpeed * 1.35f);
		_leafProcessMaterial.SetShaderParameter(
			"vertical_wobble",
			LeafVerticalWobble);
		_leafProcessMaterial.SetShaderParameter(
			"side_wobble",
			LeafSideWobble);
		_leafProcessMaterial.SetShaderParameter("size_min", minSize);
		_leafProcessMaterial.SetShaderParameter("size_max", maxSize);
		_leafProcessMaterial.SetShaderParameter("spin_min", LeafMinSpin);
		_leafProcessMaterial.SetShaderParameter("spin_max", LeafMaxSpin);
		_leafProcessMaterial.SetShaderParameter(
			"color_one",
			new Color(0.69f, 0.45f, 0.12f, 0.94f));
		_leafProcessMaterial.SetShaderParameter(
			"color_two",
			new Color(0.48f, 0.58f, 0.16f, 0.92f));
		_leafProcessMaterial.SetShaderParameter(
			"color_three",
			new Color(0.72f, 0.25f, 0.12f, 0.92f));
		_leafProcessMaterial.SetShaderParameter(
			"color_four",
			new Color(0.35f, 0.18f, 0.10f, 0.90f));

		ApplyCommonParticleParameters(
			_dustProcessMaterial,
			emissionCenter,
			emissionBox,
			windDirection,
			intensity,
			gust,
			LeafWobbleFrequencyMin,
			LeafWobbleFrequencyMax,
			LeafLateralSpread,
			LeafVerticalSpread);
		_dustProcessMaterial.SetShaderParameter(
			"speed_min",
			minSpeed * DustMinSpeedMultiplier);
		_dustProcessMaterial.SetShaderParameter(
			"speed_max",
			maxSpeed * DustMaxSpeedMultiplier);
		_dustProcessMaterial.SetShaderParameter(
			"fall_speed_min",
			DustMinFallSpeed);
		_dustProcessMaterial.SetShaderParameter(
			"fall_speed_max",
			DustMaxFallSpeed);
		_dustProcessMaterial.SetShaderParameter(
			"vertical_wobble",
			LeafVerticalWobble * DustVerticalWobbleMultiplier);
		_dustProcessMaterial.SetShaderParameter(
			"side_wobble",
			LeafSideWobble * DustSideWobbleMultiplier);
		_dustProcessMaterial.SetShaderParameter("size_min", DustMinSize);
		_dustProcessMaterial.SetShaderParameter("size_max", DustMaxSize);
		_dustProcessMaterial.SetShaderParameter("spin_min", DustMinSpin);
		_dustProcessMaterial.SetShaderParameter("spin_max", DustMaxSpin);
		_dustProcessMaterial.SetShaderParameter(
			"color_one",
			new Color(0.93f, 0.79f, 0.55f, 0.28f));
		_dustProcessMaterial.SetShaderParameter(
			"color_two",
			new Color(0.76f, 0.84f, 0.67f, 0.24f));
		_dustProcessMaterial.SetShaderParameter(
			"color_three",
			new Color(0.87f, 0.58f, 0.38f, 0.24f));
		_dustProcessMaterial.SetShaderParameter(
			"color_four",
			new Color(0.66f, 0.53f, 0.38f, 0.22f));
	}

	private static void ApplyCommonParticleParameters(
		ShaderMaterial material,
		Vector3 emissionCenter,
		Vector3 emissionBox,
		Vector3 windDirection,
		float intensity,
		float gust,
		float wobbleFrequencyMin,
		float wobbleFrequencyMax,
		float lateralSpread,
		float verticalSpread)
	{
		material.SetShaderParameter("emission_center", emissionCenter);
		material.SetShaderParameter("emission_box", emissionBox);
		material.SetShaderParameter("wind_direction", windDirection);
		material.SetShaderParameter("wind_intensity", intensity);
		material.SetShaderParameter("gust_strength", gust);
		material.SetShaderParameter(
			"wobble_frequency_min",
			wobbleFrequencyMin);
		material.SetShaderParameter(
			"wobble_frequency_max",
			wobbleFrequencyMax);
		material.SetShaderParameter("lateral_spread", lateralSpread);
		material.SetShaderParameter("vertical_spread", verticalSpread);
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
