using Godot;
using System;
using System.Collections.Generic;

public partial class PestPlantSwarmEffect : Node3D
{
	private const string ShaderPath =
		"res://shaders/pest-local-particles.gdshader";

	[ExportGroup("Connections")]
	[Export] public NodePath TurnManagerPath =
		new NodePath("../TurnManager");
	[Export] public NodePath BoardManagerPath =
		new NodePath("../BoardManager");

	[ExportGroup("General")]
	[Export] public bool EffectEnabled = true;
	[Export] public int LayoutSeed = 7319;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float EffectOpacity = 0.9f;

	[ExportGroup("Insects")]
	[Export(PropertyHint.Range, "1,24,1")]
	public int InsectsPerPlant = 7;
	[Export(PropertyHint.Range, "0.05,2.5,0.01")]
	public float InsectOrbitRadius = 0.58f;
	[Export(PropertyHint.Range, "0.05,2.0,0.01")]
	public float InsectVerticalRange = 0.72f;
	[Export(PropertyHint.Range, "0.1,8.0,0.05")]
	public float InsectOrbitSpeedMin = 1.1f;
	[Export(PropertyHint.Range, "0.1,10.0,0.05")]
	public float InsectOrbitSpeedMax = 2.6f;
	[Export(PropertyHint.Range, "0.0,1.5,0.01")]
	public float InsectWanderStrength = 0.24f;
	[Export(PropertyHint.Range, "0.005,0.16,0.001")]
	public float InsectSize = 0.052f;
	[Export] public Color InsectColorA = new Color(0.035f, 0.018f, 0.008f);
	[Export] public Color InsectColorB = new Color(0.32f, 0.13f, 0.025f);
	[Export] public Color InsectOutlineColor =
		new Color(0.86f, 0.52f, 0.14f);
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float InsectOutlineStrength = 0.72f;
	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float InsectEmissionStrength = 0.16f;

	[ExportGroup("Camera Edge Insects")]
	[Export] public bool CameraEdgeInsectsEnabled = true;
	[Export(PropertyHint.Range, "1,12,1")]
	public int CameraEdgeInsectCount = 5;
	[Export] public int CameraEdgeLayoutSeed = 9421;
	[Export(PropertyHint.Range, "5.0,240.0,1.0")]
	public float CameraEdgeCrawlSpeed = 75.0f;
	[Export(PropertyHint.Range, "3.0,32.0,0.5")]
	public float CameraEdgeInsectSize = 10.0f;
	[Export(PropertyHint.Range, "4.0,120.0,1.0")]
	public float CameraEdgeInset = 22.0f;
	[Export(PropertyHint.Range, "0.0,40.0,0.5")]
	public float CameraEdgeWobble = 6.0f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float CameraEdgeOpacity = 0.78f;
	[Export(PropertyHint.Range, "0.1,8.0,0.1")]
	public float CameraEdgeFadeSpeed = 2.4f;
	[Export] public Color CameraEdgeInsectColor =
		new Color(0.045f, 0.025f, 0.012f);
	[Export] public Color CameraEdgeOutlineColor =
		new Color(0.85f, 0.5f, 0.14f);

	[ExportGroup("Falling Fragments")]
	[Export(PropertyHint.Range, "0,10,1")]
	public int FragmentsPerPlant = 2;
	[Export(PropertyHint.Range, "0.05,2.5,0.01")]
	public float FragmentFallSpeed = 0.42f;
	[Export(PropertyHint.Range, "0.1,3.0,0.01")]
	public float FragmentFallDistance = 0.72f;
	[Export(PropertyHint.Range, "0.0,1.5,0.01")]
	public float FragmentDrift = 0.16f;
	[Export(PropertyHint.Range, "0.005,0.18,0.001")]
	public float FragmentSize = 0.045f;
	[Export] public Color FragmentColorA = new Color(0.22f, 0.39f, 0.05f);
	[Export] public Color FragmentColorB = new Color(0.34f, 0.16f, 0.035f);

	[ExportGroup("Death Burst")]
	[Export(PropertyHint.Range, "1,40,1")]
	public int DeathBurstCount = 14;
	[Export(PropertyHint.Range, "0.1,3.0,0.05")]
	public float DeathBurstDuration = 0.75f;
	[Export(PropertyHint.Range, "0.1,4.0,0.05")]
	public float DeathBurstDistance = 1.35f;
	[Export(PropertyHint.Range, "0.0,2.5,0.05")]
	public float DeathBurstLift = 0.65f;

	[ExportGroup("Plant Heights")]
	[Export(PropertyHint.Range, "0.0,3.0,0.01")]
	public float MossHeight = 0.22f;
	[Export(PropertyHint.Range, "0.0,3.0,0.01")]
	public float MushroomHeight = 0.42f;
	[Export(PropertyHint.Range, "0.0,3.0,0.01")]
	public float FlowerHeight = 0.52f;
	[Export(PropertyHint.Range, "0.0,4.0,0.01")]
	public float BirchHeight = 1.0f;

	private sealed class ParticleState
	{
		public Vector3 Center;
		public float A;
		public float B;
		public float C;
		public float D;
	}

	private TurnManager _turnManager;
	private BoardManager _boardManager;
	private MultiMeshInstance3D _insectVisual;
	private MultiMeshInstance3D _fragmentVisual;
	private MultiMeshInstance3D _burstVisual;
	private ShaderMaterial _insectMaterial;
	private ShaderMaterial _fragmentMaterial;
	private ShaderMaterial _burstMaterial;
	private CanvasLayer _cameraEdgeLayer;
	private PestCameraEdgeOverlay _cameraEdgeOverlay;
	private readonly List<Vector3> _targetCenters = new();
	private readonly List<ParticleState> _insects = new();
	private readonly List<ParticleState> _fragments = new();
	private readonly List<ParticleState> _burstParticles = new();
	private float _animationTime;
	private float _burstTime = float.MaxValue;
	private float _cameraEdgeBlend;
	private bool _pestsActive;
	private int _layoutSignature;

	public override void _Ready()
	{
		_turnManager = GetNodeOrNull<TurnManager>(TurnManagerPath);
		_boardManager = GetNodeOrNull<BoardManager>(BoardManagerPath);

		if (_turnManager == null || _boardManager == null)
		{
			GD.PushError(
				"PestPlantSwarmEffect: TurnManager oder BoardManager fehlt.");
			SetProcess(false);
			return;
		}

		if (!CreateVisuals())
		{
			SetProcess(false);
			return;
		}

		_turnManager.TurnStarted += OnTurnStarted;
		_turnManager.PlantPlaced += OnPlantPlaced;
		_turnManager.EventActivated += OnEventActivated;
		_turnManager.EventPhaseResolved += OnEventPhaseResolved;
		_turnManager.GameEnded += OnGameEnded;

		_pestsActive = ContainsPestsInState();
		RefreshTargets();
		ApplyMaterialSettings();
	}

	public override void _ExitTree()
	{
		if (_turnManager == null)
			return;

		_turnManager.TurnStarted -= OnTurnStarted;
		_turnManager.PlantPlaced -= OnPlantPlaced;
		_turnManager.EventActivated -= OnEventActivated;
		_turnManager.EventPhaseResolved -= OnEventPhaseResolved;
		_turnManager.GameEnded -= OnGameEnded;
	}

	public override void _Process(double delta)
	{
		if (_insectVisual == null)
			return;

		float deltaTime = (float)delta;
		_animationTime += deltaTime;
		ApplyMaterialSettings();

		int signature = CalculateLayoutSignature();
		if (signature != _layoutSignature)
			RefreshTargets();

		bool showLocalEffect = EffectEnabled && _pestsActive;
		_insectVisual.Visible = showLocalEffect && _insects.Count > 0;
		_fragmentVisual.Visible = showLocalEffect && _fragments.Count > 0;

		if (showLocalEffect)
		{
			AnimateInsects();
			AnimateFragments();
		}

		UpdateCameraEdgeEffect(showLocalEffect, deltaTime);
		AnimateBurst(deltaTime);
	}

	private bool CreateVisuals()
	{
		Shader shader = GD.Load<Shader>(ShaderPath);
		if (shader == null)
		{
			GD.PushError(
				$"PestPlantSwarmEffect: Shader fehlt: {ShaderPath}");
			return false;
		}

		ArrayMesh particleMesh = CreateParticleMesh();
		_insectMaterial = CreateMaterial(shader);
		_fragmentMaterial = CreateMaterial(shader);
		_burstMaterial = CreateMaterial(shader);
		_insectVisual = CreateMultiMeshVisual(
			"PestInsects",
			particleMesh,
			_insectMaterial);
		_fragmentVisual = CreateMultiMeshVisual(
			"PestFragments",
			particleMesh,
			_fragmentMaterial);
		_burstVisual = CreateMultiMeshVisual(
			"PestDeathBurst",
			particleMesh,
			_burstMaterial);
		CreateCameraEdgeVisual();
		return true;
	}

	private void CreateCameraEdgeVisual()
	{
		_cameraEdgeLayer = new CanvasLayer
		{
			Name = "PestCameraEdgeLayer",
			Layer = 0
		};
		AddChild(_cameraEdgeLayer);

		_cameraEdgeOverlay = new PestCameraEdgeOverlay
		{
			Name = "PestCameraEdgeOverlay",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_cameraEdgeLayer.AddChild(_cameraEdgeOverlay);
		_cameraEdgeOverlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect);
	}

	private MultiMeshInstance3D CreateMultiMeshVisual(
		string nodeName,
		Mesh mesh,
		Material material)
	{
		MultiMesh multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseCustomData = true,
			Mesh = mesh,
			InstanceCount = 0
		};
		MultiMeshInstance3D visual = new MultiMeshInstance3D
		{
			Name = nodeName,
			Multimesh = multiMesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		AddChild(visual);
		return visual;
	}

	private static ShaderMaterial CreateMaterial(Shader shader)
	{
		return new ShaderMaterial
		{
			Shader = shader
		};
	}

	private static ArrayMesh CreateParticleMesh()
	{
		SurfaceTool surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);
		AddDiamond(
			surface,
			new Vector3(0.0f, 0.0f, -0.62f),
			new Vector3(0.42f, 0.0f, 0.0f),
			new Vector3(0.0f, 0.0f, 0.62f),
			new Vector3(-0.42f, 0.0f, 0.0f));
		AddDiamond(
			surface,
			new Vector3(0.0f, -0.62f, 0.0f),
			new Vector3(0.42f, 0.0f, 0.0f),
			new Vector3(0.0f, 0.62f, 0.0f),
			new Vector3(-0.42f, 0.0f, 0.0f));
		return surface.Commit();
	}

	private static void AddDiamond(
		SurfaceTool surface,
		Vector3 top,
		Vector3 right,
		Vector3 bottom,
		Vector3 left)
	{
		surface.SetUV(new Vector2(0.5f, 0.0f));
		surface.AddVertex(top);
		surface.SetUV(new Vector2(1.0f, 0.5f));
		surface.AddVertex(right);
		surface.SetUV(new Vector2(0.5f, 1.0f));
		surface.AddVertex(bottom);
		surface.SetUV(new Vector2(0.5f, 0.0f));
		surface.AddVertex(top);
		surface.SetUV(new Vector2(0.5f, 1.0f));
		surface.AddVertex(bottom);
		surface.SetUV(new Vector2(0.0f, 0.5f));
		surface.AddVertex(left);
	}

	private void ApplyMaterialSettings()
	{
		SetMaterialLook(
			_insectMaterial,
			InsectColorA,
			InsectColorB,
			EffectOpacity,
			InsectOutlineColor,
			InsectOutlineStrength,
			InsectEmissionStrength);
		SetMaterialLook(
			_fragmentMaterial,
			FragmentColorA,
			FragmentColorB,
			EffectOpacity * 0.82f,
			FragmentColorB,
			0.18f,
			0.02f);
		SetMaterialLook(
			_burstMaterial,
			InsectColorA,
			InsectColorB,
			EffectOpacity,
			InsectOutlineColor,
			InsectOutlineStrength,
			InsectEmissionStrength);
	}

	private static void SetMaterialLook(
		ShaderMaterial material,
		Color colorA,
		Color colorB,
		float opacity,
		Color outlineColor,
		float outlineStrength,
		float emissionStrength)
	{
		if (material == null)
			return;

		material.SetShaderParameter("color_a", colorA);
		material.SetShaderParameter("color_b", colorB);
		material.SetShaderParameter("effect_opacity", Mathf.Clamp(opacity, 0.0f, 1.0f));
		material.SetShaderParameter("outline_color", outlineColor);
		material.SetShaderParameter(
			"outline_strength",
			Mathf.Clamp(outlineStrength, 0.0f, 1.0f));
		material.SetShaderParameter(
			"emission_strength",
			Mathf.Max(emissionStrength, 0.0f));
	}

	private void UpdateCameraEdgeEffect(bool eventVisible, float delta)
	{
		if (_cameraEdgeOverlay == null)
			return;

		bool shouldShow = eventVisible && CameraEdgeInsectsEnabled;
		_cameraEdgeBlend = Mathf.MoveToward(
			_cameraEdgeBlend,
			shouldShow ? 1.0f : 0.0f,
			Mathf.Max(CameraEdgeFadeSpeed, 0.01f) * delta);
		_cameraEdgeOverlay.Visible = _cameraEdgeBlend > 0.001f;
		_cameraEdgeOverlay.InsectCount = CameraEdgeInsectCount;
		_cameraEdgeOverlay.LayoutSeed = CameraEdgeLayoutSeed;
		_cameraEdgeOverlay.CrawlSpeed = CameraEdgeCrawlSpeed;
		_cameraEdgeOverlay.InsectSize = CameraEdgeInsectSize;
		_cameraEdgeOverlay.EdgeInset = CameraEdgeInset;
		_cameraEdgeOverlay.EdgeWobble = CameraEdgeWobble;
		_cameraEdgeOverlay.EffectOpacity = CameraEdgeOpacity;
		_cameraEdgeOverlay.EffectBlend = _cameraEdgeBlend;
		_cameraEdgeOverlay.InsectColor = CameraEdgeInsectColor;
		_cameraEdgeOverlay.OutlineColor = CameraEdgeOutlineColor;
	}

	private void RefreshTargets()
	{
		_targetCenters.Clear();
		_insects.Clear();
		_fragments.Clear();

		if (_pestsActive && EffectEnabled && _boardManager?.BoardData != null)
		{
			foreach (HexTileData tile in _boardManager.BoardData.Tiles.Values)
			{
				if (!IsPestTarget(tile))
					continue;

				HexTile tileView = _boardManager.GetTileView(tile.Coord);
				if (tileView == null)
					continue;

				Vector3 center = ToLocal(tileView.GlobalPosition);
				center.Y += GetPlantHeight(tile.Plant.Definition.Type);
				_targetCenters.Add(center);
			}
		}

		BuildParticles(
			_insects,
			Math.Max(InsectsPerPlant, 0),
			LayoutSeed + 101);
		BuildParticles(
			_fragments,
			Math.Max(FragmentsPerPlant, 0),
			LayoutSeed + 503);
		ConfigureInstances(_insectVisual, _insects);
		ConfigureInstances(_fragmentVisual, _fragments);
		_layoutSignature = CalculateLayoutSignature();
	}

	private void BuildParticles(
		List<ParticleState> particles,
		int countPerTarget,
		int seedOffset)
	{
		for (int targetIndex = 0; targetIndex < _targetCenters.Count; targetIndex++)
		{
			for (int particleIndex = 0;
				particleIndex < countPerTarget;
				particleIndex++)
			{
				int seed = seedOffset + targetIndex * 977 + particleIndex * 131;
				particles.Add(new ParticleState
				{
					Center = _targetCenters[targetIndex],
					A = Hash01(seed),
					B = Hash01(seed + 19),
					C = Hash01(seed + 47),
					D = Hash01(seed + 83)
				});
			}
		}
	}

	private static void ConfigureInstances(
		MultiMeshInstance3D visual,
		List<ParticleState> particles)
	{
		if (visual?.Multimesh == null)
			return;

		visual.Multimesh.InstanceCount = particles.Count;
		for (int i = 0; i < particles.Count; i++)
		{
			ParticleState particle = particles[i];
			visual.Multimesh.SetInstanceCustomData(
				i,
				new Color(particle.A, particle.B, particle.C, 1.0f));
		}
	}

	private void AnimateInsects()
	{
		float minimumSpeed = Mathf.Max(InsectOrbitSpeedMin, 0.01f);
		float maximumSpeed = Mathf.Max(InsectOrbitSpeedMax, minimumSpeed);
		float radius = Mathf.Max(InsectOrbitRadius, 0.0f);

		for (int i = 0; i < _insects.Count; i++)
		{
			ParticleState particle = _insects[i];
			float direction = particle.C < 0.45f ? -1.0f : 1.0f;
			float speed = Mathf.Lerp(minimumSpeed, maximumSpeed, particle.B);
			float angle = particle.A * Mathf.Tau +
				_animationTime * speed * direction;
			float irregularity = Mathf.Sin(
				_animationTime * (1.35f + particle.D) +
				particle.C * Mathf.Tau);
			float pulse = Mathf.Max(
				Mathf.Sin(_animationTime * 0.73f + particle.D * 17.0f),
				0.0f);
			float localRadius = radius * Mathf.Lerp(0.5f, 1.0f, particle.C) +
				InsectWanderStrength * pulse;
			Vector3 offset = new Vector3(
				Mathf.Cos(angle) * localRadius,
				Mathf.Lerp(-0.2f, 0.8f, particle.D) * InsectVerticalRange +
					irregularity * InsectVerticalRange * 0.18f,
				Mathf.Sin(angle) * localRadius);
			offset.X += irregularity * InsectWanderStrength * 0.45f;
			offset.Z += Mathf.Sin(angle * 1.7f + particle.B * 9.0f) *
				InsectWanderStrength * 0.35f;
			float flutter = 0.78f + 0.22f * Mathf.Abs(
				Mathf.Sin(_animationTime * (7.0f + particle.C * 5.0f)));
			float size = Mathf.Max(InsectSize, 0.001f) *
				Mathf.Lerp(0.7f, 1.35f, particle.B) * flutter;
			SetParticleTransform(
				_insectVisual.Multimesh,
				i,
				particle.Center + offset,
				size,
				angle,
				irregularity * 0.7f);
		}
	}

	private void AnimateFragments()
	{
		float fallSpeed = Mathf.Max(FragmentFallSpeed, 0.01f);
		float fallDistance = Mathf.Max(FragmentFallDistance, 0.01f);

		for (int i = 0; i < _fragments.Count; i++)
		{
			ParticleState particle = _fragments[i];
			float progress = Fract(
				_animationTime * fallSpeed * Mathf.Lerp(0.72f, 1.2f, particle.B) +
				particle.A);
			float angle = particle.C * Mathf.Tau;
			Vector3 offset = new Vector3(
				Mathf.Cos(angle) * InsectOrbitRadius * 0.45f,
				InsectVerticalRange * 0.6f - progress * fallDistance,
				Mathf.Sin(angle) * InsectOrbitRadius * 0.45f);
			offset.X += Mathf.Sin(progress * Mathf.Tau + particle.D * 8.0f) *
				FragmentDrift;
			offset.Z += Mathf.Cos(progress * Mathf.Tau * 0.7f + particle.B * 7.0f) *
				FragmentDrift * 0.7f;
			float visibility = Mathf.Clamp(
				Mathf.Min(progress * 7.0f, (1.0f - progress) * 7.0f),
				0.0f,
				1.0f);
			float size = Mathf.Max(FragmentSize, 0.001f) *
				Mathf.Lerp(0.75f, 1.35f, particle.C) * visibility;
			SetParticleTransform(
				_fragmentVisual.Multimesh,
				i,
				particle.Center + offset,
				size,
				angle + progress * Mathf.Tau * 1.8f,
				progress * Mathf.Tau);
		}
	}

	private void AnimateBurst(float delta)
	{
		if (_burstVisual == null || _burstParticles.Count == 0)
			return;

		_burstTime += delta;
		float duration = Mathf.Max(DeathBurstDuration, 0.05f);
		float progress = Mathf.Clamp(_burstTime / duration, 0.0f, 1.0f);
		_burstVisual.Visible = EffectEnabled && progress < 1.0f;
		if (!_burstVisual.Visible)
			return;

		float eased = 1.0f - (1.0f - progress) * (1.0f - progress);
		for (int i = 0; i < _burstParticles.Count; i++)
		{
			ParticleState particle = _burstParticles[i];
			float angle = particle.A * Mathf.Tau;
			float distance = DeathBurstDistance *
				Mathf.Lerp(0.55f, 1.2f, particle.B) * eased;
			Vector3 offset = new Vector3(
				Mathf.Cos(angle) * distance,
				Mathf.Sin(progress * Mathf.Pi) * DeathBurstLift +
					(particle.C - 0.5f) * progress * 0.5f,
				Mathf.Sin(angle) * distance);
			float size = Mathf.Max(InsectSize, 0.001f) *
				Mathf.Lerp(0.8f, 1.45f, particle.C) * (1.0f - progress * 0.6f);
			SetParticleTransform(
				_burstVisual.Multimesh,
				i,
				particle.Center + offset,
				size,
				angle + progress * 8.0f,
				particle.D * Mathf.Tau + progress * 4.0f);
			_burstVisual.Multimesh.SetInstanceCustomData(
				i,
				new Color(particle.A, particle.B, particle.C, 1.0f - progress));
		}
	}

	private static void SetParticleTransform(
		MultiMesh multiMesh,
		int index,
		Vector3 position,
		float size,
		float yaw,
		float tilt)
	{
		Basis basis = Basis.Identity
			.Rotated(Vector3.Up, yaw)
			.Rotated(Vector3.Right, tilt)
			.Scaled(Vector3.One * size);
		multiMesh.SetInstanceTransform(index, new Transform3D(basis, position));
	}

	private void StartDeathBurst(IReadOnlyList<PlantDeathResult> deaths)
	{
		_burstParticles.Clear();
		for (int deathIndex = 0; deathIndex < deaths.Count; deathIndex++)
		{
			PlantDeathResult death = deaths[deathIndex];
			if (death.Cause != GameEventType.Pests)
				continue;

			HexTile tileView = _boardManager.GetTileView(death.Coord);
			if (tileView == null)
				continue;

			Vector3 center = ToLocal(tileView.GlobalPosition);
			center.Y += GetPlantHeight(death.PlantType);
			for (int particleIndex = 0;
				particleIndex < Math.Max(DeathBurstCount, 0);
				particleIndex++)
			{
				int seed = LayoutSeed + deathIndex * 1291 + particleIndex * 173;
				_burstParticles.Add(new ParticleState
				{
					Center = center,
					A = Hash01(seed),
					B = Hash01(seed + 23),
					C = Hash01(seed + 61),
					D = Hash01(seed + 97)
				});
			}
		}

		ConfigureInstances(_burstVisual, _burstParticles);
		_burstTime = _burstParticles.Count > 0 ? 0.0f : float.MaxValue;
	}

	private bool IsPestTarget(HexTileData tile)
	{
		PlantInstance plant = tile?.Plant;
		if (plant?.Definition == null || plant.Definition.Type == PlantType.Oak)
			return false;

		EventDefinition definition = EventDatabase.Get(GameEventType.Pests);
		if (definition == null)
			return false;

		if (!plant.IsMature)
		{
			if (definition.SeedlingDeathChanceDenominator <= 0)
				return false;

			return !definition.SeedlingDeathRequiresSun ||
				tile.LightLevel == LightLevel.Sun;
		}

		if (definition.MatureDeathChanceDenominator <= 0)
			return false;

		return !definition.MatureDeathRequiresMonoculture ||
			IsPartOfMonoculture(tile);
	}

	private bool IsPartOfMonoculture(HexTileData startTile)
	{
		int requiredCount = Math.Max(
			_turnManager.Config?.MonocultureMinimumPlantCount ?? 1,
			1);
		if (requiredCount <= 1)
			return true;

		PlantType plantType = startTile.Plant.Definition.Type;
		HashSet<HexCoord> visited = new() { startTile.Coord };
		Queue<HexTileData> openTiles = new();
		openTiles.Enqueue(startTile);

		while (openTiles.Count > 0)
		{
			HexTileData current = openTiles.Dequeue();
			foreach (HexTileData neighbor in
				_boardManager.GetNeighborData(current.Coord))
			{
				if (neighbor.Plant == null ||
					neighbor.Plant.Definition.Type != plantType ||
					!visited.Add(neighbor.Coord))
				{
					continue;
				}

				if (visited.Count >= requiredCount)
					return true;

				openTiles.Enqueue(neighbor);
			}
		}

		return visited.Count >= requiredCount;
	}

	private float GetPlantHeight(PlantType plantType)
	{
		return plantType switch
		{
			PlantType.Moss => MossHeight,
			PlantType.Mushroom => MushroomHeight,
			PlantType.Flower => FlowerHeight,
			PlantType.Birch => BirchHeight,
			_ => 0.35f
		};
	}

	private void OnTurnStarted(int round)
	{
		_pestsActive = ContainsPestsInState();
		RefreshTargets();
	}

	private void OnPlantPlaced(PlantType plantType, HexCoord coord)
	{
		if (_pestsActive)
			RefreshTargets();
	}

	private void OnEventActivated(GameEventType eventType)
	{
		if (eventType != GameEventType.Pests)
			return;

		_pestsActive = true;
		RefreshTargets();
	}

	private void OnEventPhaseResolved(EventPhaseResult result)
	{
		StartDeathBurst(result.PlantDeaths);
		_pestsActive = ContainsPests(result.ActiveEvents);
		RefreshTargets();
	}

	private void OnGameEnded(GameState state)
	{
		_pestsActive = false;
		RefreshTargets();
	}

	private bool ContainsPestsInState()
	{
		if (_turnManager?.State?.ActiveEvents == null)
			return false;

		foreach (ActiveGameEvent activeEvent in _turnManager.State.ActiveEvents)
		{
			if (activeEvent?.Definition?.Type == GameEventType.Pests)
				return true;
		}

		return false;
	}

	private static bool ContainsPests(IReadOnlyList<GameEventType> activeEvents)
	{
		for (int i = 0; i < activeEvents.Count; i++)
		{
			if (activeEvents[i] == GameEventType.Pests)
				return true;
		}

		return false;
	}

	private int CalculateLayoutSignature()
	{
		unchecked
		{
			int hash = 17;
			hash = hash * 31 + InsectsPerPlant;
			hash = hash * 31 + FragmentsPerPlant;
			hash = hash * 31 + LayoutSeed;
			hash = hash * 31 + Mathf.RoundToInt(MossHeight * 1000.0f);
			hash = hash * 31 + Mathf.RoundToInt(MushroomHeight * 1000.0f);
			hash = hash * 31 + Mathf.RoundToInt(FlowerHeight * 1000.0f);
			hash = hash * 31 + Mathf.RoundToInt(BirchHeight * 1000.0f);
			hash = hash * 31 + (_pestsActive ? 1 : 0);
			hash = hash * 31 + (EffectEnabled ? 1 : 0);
			return hash;
		}
	}

	private static float Hash01(int value)
	{
		uint hash = unchecked((uint)value);
		hash ^= hash >> 16;
		hash *= 0x7feb352d;
		hash ^= hash >> 15;
		hash *= 0x846ca68b;
		hash ^= hash >> 16;
		return (hash & 0x00ffffff) / 16777215.0f;
	}

	private static float Fract(float value)
	{
		return value - Mathf.Floor(value);
	}
}
