using Godot;
using System.Collections.Generic;

public partial class HexTile : Node3D
{
	public HexTileData Data { get; private set; }

	public HexCoord Coord => Data.Coord;

	private MeshInstance3D _tileMesh;

	private Node3D _plantAnchor;
	private Node3D _plantVisualRoot;
	private PlantInstance _plantInspectionRenderPlant;
	private int _plantInspectionRenderLayer;
	private readonly List<GeometryInstance3D>
		_plantInspectionLayerAdditions = new();
	private readonly List<TreeProximityFade3D>
		_plantInspectionSuppressedProximityFades = new();
	private Node3D _mushroomNeighborVisualRoot;
	private MultiMeshInstance3D _grassMultiMesh;
	private readonly List<GeometryInstance3D> _tileVisualGeometry = new();
	private bool _receivesCanopyShadow;
	private float _canopyShadowAmount;
	private Tween _canopyShadowTween;
	private readonly List<CanopyShadowMaterialBinding>
		_canopyShadowMaterialBindings = new();

	private Node3D _placementIndicatorRoot;
	private PlantPlacementWindIndicator _placementWindIndicator;

	private StandardMaterial3D _tileMaterial;
	private bool _isTutorialHighlightActive;
	private Tween _tutorialHighlightTween;

	private const float TutorialHighlightMinAlpha = 0.48f;
	private const float TutorialHighlightMaxAlpha = 0.72f;
	private const float TutorialHighlightMinEmission = 0.85f;
	private const float TutorialHighlightMaxEmission = 1.20f;
	private const float TutorialHighlightPulseDuration = 0.85f;
	private const float MossPlacementAnimationDuration = 2.0f;
	private const float MossGrowthPhaseAnimationDuration = 6.35f;
	private const float MushroomPlacementAnimationDuration = 2.0f;
	private const float MushroomGrowthPhaseAnimationDuration = 6.35f;
	private const float GrassStateTransitionDuration = 1.4f;
	private const float GrassBlockerTransitionDuration = 0.85f;
	private const float PlantDeathAnimationDuration = 1.4f;
	private const float TreePlacementAnimationDuration = 2.0f;
	private const float TreeGrowthPhaseAnimationDuration = 6.35f;
	private const float TreePlacementHorizontalStartScale = 0.38f;
	private const float TreePlacementVerticalStartScale = 0.06f;
	private static readonly StringName FoliageColor1Parameter =
		"foliage_colour1";
	private static readonly StringName FoliageColor2Parameter =
		"foliage_colour2";
	private PlantInstance _renderedPlant;
	private int _renderedGrowthStage = -1;
	private bool _renderedAsDead;
	private int _renderedDeadBlockedRounds = -1;
	private Tween _grassStateTween;
	private Tween _grassBlockerTween;
	private Tween _treeGrowthTween;
	private Vector4 _renderedGrassState;
	private bool _hasRenderedGrassState;
	private float[] _renderedGrassBlockerVisibility = new float[0];
	private const float DeadPlantFirstRoundTintStrength = 0.42f;
	private const float DeadPlantFinalRoundTintStrength = 0.78f;
	private const string AnimeGrassPiecePath =
		"res://assets/models/plants/grass/anime_grass_piece.obj";
	private static readonly StringName GrassBlockerGroup = "grass_blocker";
	private static readonly StringName GrassBaseBlockerGroup =
		"grass_base_blocker";
	private const float GrassBaseBlockerHeight = 0.45f;
	private const int GrassPlacementCandidateMultiplier = 16;
	private const float GrassBlockerEdgeNoiseScale = 1.7f;
	private const float GrassBlockerEdgeDetailScale = 4.8f;
	private const float GrassBlockerEdgeMinimum = 0.55f;
	private const float GrassBlockerEdgeMaximum = 1.20f;
	private static Mesh _animeGrassPieceMesh;
	private static bool _grassPieceLoadAttempted;
	private float _grassBaseDensity = 1.0f;
	private int _grassInstancesPerTile = 320;
	private float _grassWindWaveSpeed = 0.035f;
	private float _grassWindWaveStrength = 0.075f;
	private float _grassWindDetailSpeed = 0.07f;
	private float _grassWindDetailStrength = 0.012f;
	private Vector3 _grassTileWorldCenter;
	private float _grassEdgeDistance;
	private float _grassOuterMargin;
	private float _grassStoneMargin = 0.30f;
	private float _grassOakMargin = 0.30f;
	private float _grassBirchMargin = 0.30f;
	private float _grassMushroomMargin = 0.14f;
	private float _grassMossMargin = 0.30f;
	private Vector2[] _grassBorderDirections = new Vector2[6];
	private float[] _grassOuterEdges = new float[6];
	private float _tileVisualScale = 1.0f;
	private float _grassTileHeight;
	private bool _visibilityRangesEnabled = true;
	private float _grassVisibilityRange = 36.0f;
	private float _vegetationVisibilityRange = 42.0f;
	private float _visibilityRangeMargin = 4.0f;
	private float _frustumCullMargin = 2.0f;
	private readonly List<GrassPlacementCandidate> _grassPlacementCandidates =
		new();

	private sealed class CanopyShadowMaterialBinding
	{
		public GeometryInstance3D Geometry;
		public int SurfaceIndex = -1;
		public Material OriginalAssignedMaterial;
		public Material ShadowMaterial;
		public bool HasAlbedoColor;
		public Color AlbedoColor;
		public bool HasFoliageColor1;
		public Color FoliageColor1;
		public bool HasFoliageColor2;
		public Color FoliageColor2;
	}

	private readonly struct GrassPlacementCandidate
	{
		public Transform3D Transform { get; }
		public Vector2 TilePosition { get; }
		public float ScaleJitter { get; }
		public float BlockerEdgeVariation { get; }

		public GrassPlacementCandidate(
			Transform3D transform,
			Vector2 tilePosition,
			float scaleJitter,
			float blockerEdgeVariation)
		{
			Transform = transform;
			TilePosition = tilePosition;
			ScaleJitter = scaleJitter;
			BlockerEdgeVariation = blockerEdgeVariation;
		}
	}

	private readonly struct GrassBlockerTriangle
	{
		private readonly Vector2 _a;
		private readonly Vector2 _b;
		private readonly Vector2 _c;
		private readonly float _minX;
		private readonly float _maxX;
		private readonly float _minY;
		private readonly float _maxY;
		private readonly float _margin;

		public GrassBlockerTriangle(
			Vector2 a,
			Vector2 b,
			Vector2 c,
			float margin)
		{
			_a = a;
			_b = b;
			_c = c;
			_minX = Mathf.Min(a.X, Mathf.Min(b.X, c.X));
			_maxX = Mathf.Max(a.X, Mathf.Max(b.X, c.X));
			_minY = Mathf.Min(a.Y, Mathf.Min(b.Y, c.Y));
			_maxY = Mathf.Max(a.Y, Mathf.Max(b.Y, c.Y));
			_margin = Mathf.Max(0.0f, margin);
		}

		public bool Blocks(
			Vector2 point,
			float scaleJitter,
			float edgeVariation)
		{
			return BlocksWithClearance(
				point,
				_margin * scaleJitter * edgeVariation);
		}

		public bool BlocksWithClearance(Vector2 point, float clearance)
		{
			float scaledClearance = Mathf.Max(0.0f, clearance);

			if (point.X < _minX - scaledClearance ||
				point.X > _maxX + scaledClearance ||
				point.Y < _minY - scaledClearance ||
				point.Y > _maxY + scaledClearance)
			{
				return false;
			}

			float area = Cross(_a, _b, _c);

			if (!Mathf.IsZeroApprox(area))
			{
				float first = Cross(_a, _b, point);
				float second = Cross(_b, _c, point);
				float third = Cross(_c, _a, point);
				bool hasNegative = first < 0.0f || second < 0.0f || third < 0.0f;
				bool hasPositive = first > 0.0f || second > 0.0f || third > 0.0f;

				if (!(hasNegative && hasPositive))
					return true;
			}

			float clearanceSquared = scaledClearance * scaledClearance;
			return DistanceSquaredToSegment(point, _a, _b) <= clearanceSquared ||
				DistanceSquaredToSegment(point, _b, _c) <= clearanceSquared ||
				DistanceSquaredToSegment(point, _c, _a) <= clearanceSquared;
		}

		private static float Cross(Vector2 first, Vector2 second, Vector2 third)
		{
			Vector2 firstEdge = second - first;
			Vector2 secondEdge = third - first;
			return firstEdge.X * secondEdge.Y - firstEdge.Y * secondEdge.X;
		}

		private static float DistanceSquaredToSegment(
			Vector2 point,
			Vector2 start,
			Vector2 end)
		{
			Vector2 segment = end - start;
			float lengthSquared = segment.LengthSquared();

			if (Mathf.IsZeroApprox(lengthSquared))
				return point.DistanceSquaredTo(start);

			float position = Mathf.Clamp(
				(point - start).Dot(segment) / lengthSquared,
				0.0f,
				1.0f);
			Vector2 closestPoint = start + segment * position;
			return point.DistanceSquaredTo(closestPoint);
		}
	}

	public float StartingOakScale { get; private set; } = 0.25f;
	public float DeadPlantScale { get; private set; } = 0.6f;
	public Color DeadPlantTint { get; private set; } =
		new Color(0.32f, 0.27f, 0.20f);
	public Color BlockedTileTint { get; private set; } =
		new Color(0.38f, 0.40f, 0.38f);
	public Color BlockedPreviewTint { get; private set; } =
		new Color(0.48f, 0.50f, 0.48f);
	public float MushroomModelScale { get; private set; } = 0.32f;
	public float MushroomGrowthAnimationSpeed { get; private set; } = 1.0f;
	public float FlowerModelScale { get; private set; } = 0.38f;
	public int MatureFlowerCount { get; private set; } = 4;
	public float BirchModelScale { get; private set; } = 0.18f;
	public float TreeShadowStrength { get; private set; } = 0.55f;
	public Color TreeShadowColor { get; private set; } =
		new Color(0.08f, 0.12f, 0.06f, 0.55f);
	public Color CanopyShadowFieldTint { get; private set; } =
		new Color(0.56f, 0.64f, 0.52f, 0.55f);
	public float StartingOakShadowSize { get; private set; } = 6.2f;
	public Vector2 StartingOakShadowOffset { get; private set; } =
		Vector2.Zero;
	public float BirchShadowSize { get; private set; } = 2.8f;
	public Vector2 BirchShadowOffset { get; private set; } =
		new Vector2(0.0f, 0.18f);
	public float TreeShadowFadeDuration { get; private set; } = 1.25f;
	public bool TreeProximityFadeEnabled { get; private set; } = true;
	public float TreeFadeStartDistance { get; private set; } = 3.0f;
	public float TreeFadeFullDistance { get; private set; } = 0.6f;
	public float TreeFadeMaximumTransparency { get; private set; } = 0.8f;
	public float TreeFadeSpeed { get; private set; } = 1.2f;
	public Color SunTileTint { get; private set; } = Colors.White;
	public Color PartialShadeTileTint { get; private set; } =
		new Color(0.82f, 0.91f, 0.80f);
	public Color ShadeTileTint { get; private set; } =
		new Color(0.62f, 0.74f, 0.64f);

	public void ConfigureTileVisualScale(float scale)
	{
		_tileVisualScale = Mathf.Max(0.1f, scale);
	}

	public void ConfigureStartingOakScale(float scale)
	{
		StartingOakScale = Mathf.Max(0.01f, scale);
	}

	public void ConfigureDeadPlantVisuals(
		float deadPlantScale,
		Color deadPlantTint,
		Color blockedTileTint,
		Color blockedPreviewTint)
	{
		DeadPlantScale = Mathf.Clamp(deadPlantScale, 0.1f, 1.0f);
		DeadPlantTint = deadPlantTint;
		BlockedTileTint = blockedTileTint;
		BlockedPreviewTint = blockedPreviewTint;
	}

	public void ConfigureMushroomVisual(
		float modelScale,
		float growthAnimationSpeed)
	{
		MushroomModelScale = Mathf.Max(0.1f, modelScale);
		MushroomGrowthAnimationSpeed = Mathf.Max(0.1f, growthAnimationSpeed);
	}

	public void ConfigureFlowerVisual(float modelScale, int matureFlowerCount)
	{
		FlowerModelScale = Mathf.Max(0.01f, modelScale);
		MatureFlowerCount = Mathf.Clamp(matureFlowerCount, 1, 7);
	}

	public void ConfigureBirchVisual(float modelScale)
	{
		BirchModelScale = Mathf.Max(0.01f, modelScale);
	}

	public void ConfigureTreeShadowVisual(
		float shadowStrength,
		Color shadowColor,
		Color canopyShadowFieldTint,
		float startingOakShadowSize,
		Vector2 startingOakShadowOffset,
		float birchShadowSize,
		Vector2 birchShadowOffset,
		float shadowFadeDuration)
	{
		TreeShadowStrength = Mathf.Clamp(shadowStrength, 0.0f, 1.0f);
		TreeShadowColor = new Color(
			shadowColor.R,
			shadowColor.G,
			shadowColor.B,
			Mathf.Clamp(shadowColor.A, 0.0f, 1.0f) *
				TreeShadowStrength);
		CanopyShadowFieldTint = new Color(
			canopyShadowFieldTint.R,
			canopyShadowFieldTint.G,
			canopyShadowFieldTint.B,
			Mathf.Clamp(canopyShadowFieldTint.A, 0.0f, 1.0f) *
				TreeShadowStrength);
		StartingOakShadowSize = Mathf.Max(0.1f, startingOakShadowSize);
		StartingOakShadowOffset = startingOakShadowOffset;
		BirchShadowSize = Mathf.Max(0.1f, birchShadowSize);
		BirchShadowOffset = birchShadowOffset;
		TreeShadowFadeDuration = Mathf.Max(shadowFadeDuration, 0.0f);
	}

	public void ConfigureCanopyShadowReceiver(float shadowAmount)
	{
		float targetAmount = Mathf.Clamp(shadowAmount, 0.0f, 1.0f);
		bool receivesCanopyShadow = targetAmount > 0.001f;

		if (_receivesCanopyShadow == receivesCanopyShadow &&
			Mathf.IsEqualApprox(_canopyShadowAmount, targetAmount))
			return;

		_receivesCanopyShadow = receivesCanopyShadow;

		if (!IsInsideTree())
		{
			_canopyShadowAmount = targetAmount;
			return;
		}

		ApplyCanopyShadowReceiverLayer(this);
		StartCanopyShadowTransition(targetAmount);
	}

	public void ConfigureTreeProximityFade(
		bool enabled,
		float startDistance,
		float fullDistance,
		float maximumTransparency,
		float fadeSpeed)
	{
		TreeProximityFadeEnabled = enabled;
		TreeFadeStartDistance = Mathf.Max(startDistance, 0.01f);
		TreeFadeFullDistance = Mathf.Clamp(
			fullDistance,
			0.0f,
			TreeFadeStartDistance - 0.01f);
		TreeFadeMaximumTransparency = Mathf.Clamp(
			maximumTransparency,
			0.0f,
			0.8f);
		TreeFadeSpeed = Mathf.Max(fadeSpeed, 0.01f);
	}

	public void ConfigureLightVisuals(
		Color sunTileTint,
		Color partialShadeTileTint,
		Color shadeTileTint)
	{
		SunTileTint = sunTileTint;
		PartialShadeTileTint = partialShadeTileTint;
		ShadeTileTint = shadeTileTint;
	}

	public void ConfigureVisibilityRanges(
		bool enabled,
		float grassRange,
		float vegetationRange,
		float rangeMargin,
		float frustumMargin)
	{
		_visibilityRangesEnabled = enabled;
		_grassVisibilityRange = Mathf.Max(grassRange, 0.0f);
		_vegetationVisibilityRange = Mathf.Max(vegetationRange, 0.0f);
		_visibilityRangeMargin = Mathf.Max(rangeMargin, 0.0f);
		_frustumCullMargin = Mathf.Max(frustumMargin, 0.0f);
	}

	public void ConfigureGrassVisual(
		float baseDensity,
		int instancesPerTile,
		float windWaveSpeed,
		float windWaveStrength,
		float windDetailSpeed,
		float windDetailStrength,
		Vector3 tileWorldCenter,
		float edgeDistance,
		float outerMargin,
		float stoneMargin,
		float oakMargin,
		float birchMargin,
		float mushroomMargin,
		float mossMargin,
		Vector2[] borderDirections,
		float[] outerEdges)
	{
		_grassBaseDensity = Mathf.Clamp(baseDensity, 0.0f, 1.0f);
		_grassInstancesPerTile = Mathf.Clamp(instancesPerTile, 64, 4096);
		_grassWindWaveSpeed = Mathf.Max(0.0f, windWaveSpeed);
		_grassWindWaveStrength = Mathf.Max(0.0f, windWaveStrength);
		_grassWindDetailSpeed = Mathf.Max(0.0f, windDetailSpeed);
		_grassWindDetailStrength = Mathf.Max(0.0f, windDetailStrength);
		_grassTileWorldCenter = tileWorldCenter;
		_grassEdgeDistance = Mathf.Max(0.0f, edgeDistance);
		_grassOuterMargin = Mathf.Clamp(outerMargin, 0.0f, _grassEdgeDistance);
		_grassStoneMargin = Mathf.Max(0.0f, stoneMargin);
		_grassOakMargin = Mathf.Max(0.0f, oakMargin);
		_grassBirchMargin = Mathf.Max(0.0f, birchMargin);
		_grassMushroomMargin = Mathf.Max(0.0f, mushroomMargin);
		_grassMossMargin = Mathf.Max(0.0f, mossMargin);

		if (borderDirections != null && borderDirections.Length == 6)
			_grassBorderDirections = borderDirections;

		if (outerEdges != null && outerEdges.Length == 6)
			_grassOuterEdges = outerEdges;
	}

	public void Setup(HexTileData data)
	{
		Data = data;
		Name = $"HexTile_{data.Coord.Q}_{data.Coord.R}";

		_tileMesh = FindRenderableTileMesh();

		if (_tileMesh == null)
		{
			GD.PrintErr($"{Name}: No renderable tile mesh found.");
		}

		_plantAnchor = GetNodeOrNull<Node3D>("PlantAnchor");
		_grassMultiMesh = FindNodeByNamePart(this, "MultiMeshInstance3D") as MultiMeshInstance3D;
		if (_grassMultiMesh != null)
		{
			VisibilityRangeUtility.Configure(
				_grassMultiMesh,
				_visibilityRangesEnabled,
				_grassVisibilityRange,
				_visibilityRangeMargin,
				_frustumCullMargin);
		}

		if (_plantAnchor == null)
		{
			GD.PrintErr($"{Name}: PlantAnchor not found. Creating fallback PlantAnchor.");
			_plantAnchor = new Node3D();
			_plantAnchor.Name = "PlantAnchor";
			AddChild(_plantAnchor);
		}

		SetupPlacementIndicator();
		ApplyTileVisualScale();
		SetupGrassCoverage(Coord, refreshBlockers: false);
		CollectVisibleTileGeometry(this);
		SetupUniqueTileMaterial();
		EnsureCollision();
		UpdateVisualState();
	}

	public bool PrepareDecorativeGrass(HexCoord coord, float horizontalScale)
	{
		_grassMultiMesh =
			FindNodeByNamePart(this, "MultiMeshInstance3D") as MultiMeshInstance3D;

		if (_grassMultiMesh?.Multimesh == null)
			return false;

		SetupGrassCoverage(coord, Mathf.Max(horizontalScale, 0.1f));
		return true;
	}

	private void ApplyTileVisualScale()
	{
		Node grassRoot = _grassMultiMesh;

		while (grassRoot?.GetParent() != null &&
			!ReferenceEquals(grassRoot.GetParent(), this))
		{
			grassRoot = grassRoot.GetParent();
		}

		foreach (Node child in GetChildren())
		{
			if (child is not Node3D visualRoot ||
				ReferenceEquals(child, _plantAnchor) ||
				ReferenceEquals(child, _placementIndicatorRoot) ||
				ReferenceEquals(child, grassRoot) ||
				child is CollisionObject3D)
			{
				continue;
			}

			Vector3 position = visualRoot.Position;
			Vector3 scale = visualRoot.Scale;
			visualRoot.Position = new Vector3(
				position.X * _tileVisualScale,
				position.Y,
				position.Z * _tileVisualScale);
			visualRoot.Scale = new Vector3(
				scale.X * _tileVisualScale,
				scale.Y,
				scale.Z * _tileVisualScale);
		}

		if (_placementIndicatorRoot == null)
			return;

		Vector3 indicatorScale = _placementIndicatorRoot.Scale;
		_placementIndicatorRoot.Scale = new Vector3(
			indicatorScale.X * _tileVisualScale,
			indicatorScale.Y,
			indicatorScale.Z * _tileVisualScale);
	}

	public void SetRenderGroupVisibility(
		bool grassVisible,
		bool tileModelsVisible,
		bool plantsVisible)
	{
		if (_grassMultiMesh != null)
			_grassMultiMesh.Visible = grassVisible;

		foreach (GeometryInstance3D geometry in _tileVisualGeometry)
			geometry.Visible = tileModelsVisible;

		if (_plantAnchor != null)
			_plantAnchor.Visible = plantsVisible;
	}

	public bool EnablePlantInspectionRenderLayer(
		PlantInstance expectedPlant,
		int renderLayer)
	{
		DisablePlantInspectionRenderLayer();

		if (expectedPlant == null ||
			renderLayer < 1 ||
			renderLayer > 20 ||
			!ReferenceEquals(Data?.Plant, expectedPlant) ||
			_plantVisualRoot == null)
		{
			return false;
		}

		_plantInspectionRenderPlant = expectedPlant;
		_plantInspectionRenderLayer = renderLayer;
		ApplyPlantInspectionRenderLayer(_plantVisualRoot);
		return true;
	}

	public void DisablePlantInspectionRenderLayer()
	{
		ReleasePlantInspectionRenderLayer(clearRequest: true);
	}

	private void ApplyPlantInspectionRenderLayer(Node node)
	{
		if (node is TreeProximityFade3D proximityFade)
		{
			proximityFade.SetInspectionSuppressed(true);
			_plantInspectionSuppressedProximityFades.Add(proximityFade);
		}

		if (node is GeometryInstance3D geometry &&
			!geometry.GetLayerMaskValue(_plantInspectionRenderLayer))
		{
			geometry.SetLayerMaskValue(_plantInspectionRenderLayer, true);
			_plantInspectionLayerAdditions.Add(geometry);
		}

		foreach (Node child in node.GetChildren())
			ApplyPlantInspectionRenderLayer(child);
	}

	private void ReleasePlantInspectionRenderLayer(bool clearRequest)
	{
		foreach (TreeProximityFade3D proximityFade in
			_plantInspectionSuppressedProximityFades)
		{
			if (GodotObject.IsInstanceValid(proximityFade))
				proximityFade.SetInspectionSuppressed(false);
		}

		_plantInspectionSuppressedProximityFades.Clear();

		if (_plantInspectionRenderLayer > 0)
		{
			foreach (GeometryInstance3D geometry in
				_plantInspectionLayerAdditions)
			{
				if (GodotObject.IsInstanceValid(geometry))
				{
					geometry.SetLayerMaskValue(
						_plantInspectionRenderLayer,
						false);
				}
			}
		}

		_plantInspectionLayerAdditions.Clear();
		if (!clearRequest)
			return;

		_plantInspectionRenderPlant = null;
		_plantInspectionRenderLayer = 0;
	}

	private void ReapplyPlantInspectionRenderLayer()
	{
		if (_plantInspectionRenderPlant == null ||
			_plantInspectionRenderLayer < 1 ||
			!ReferenceEquals(Data?.Plant, _plantInspectionRenderPlant) ||
			_plantVisualRoot == null)
		{
			return;
		}

		ApplyPlantInspectionRenderLayer(_plantVisualRoot);
	}

	private void CollectVisibleTileGeometry(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (ReferenceEquals(child, _plantAnchor) ||
				ReferenceEquals(child, _placementIndicatorRoot))
			{
				continue;
			}

			if (child is GeometryInstance3D geometry &&
				!ReferenceEquals(geometry, _grassMultiMesh) &&
				geometry.Visible)
			{
				_tileVisualGeometry.Add(geometry);
			}

			CollectVisibleTileGeometry(child);
		}
	}

	private void ApplyCanopyShadowReceiverLayer(Node node)
	{
		if (ReferenceEquals(node, _placementIndicatorRoot))
			return;

		if (node is GeometryInstance3D geometry)
		{
			uint receiverMask = TreeCanopyShadowBuilder.ReceiverLayerMask;
			geometry.Layers = _receivesCanopyShadow
				? geometry.Layers | receiverMask
				: geometry.Layers & ~receiverMask;
		}

		foreach (Node child in node.GetChildren())
			ApplyCanopyShadowReceiverLayer(child);
	}

	private void StartCanopyShadowTransition(float targetAmount)
	{
		_canopyShadowTween?.Kill();
		_canopyShadowTween = null;
		targetAmount = Mathf.Clamp(targetAmount, 0.0f, 1.0f);
		bool receivesCanopyShadow = targetAmount > 0.001f;

		if (receivesCanopyShadow)
			RefreshCanopyShadowMaterials();

		float transitionDuration = TreeShadowFadeDuration *
			Mathf.Abs(targetAmount - _canopyShadowAmount);

		if (transitionDuration <= 0.0f)
		{
			ApplyCanopyShadowAmount(targetAmount);

			if (!receivesCanopyShadow)
				RestoreCanopyShadowMaterials();

			return;
		}

		_canopyShadowTween = CreateTween();
		_canopyShadowTween.TweenMethod(
			Callable.From<float>(ApplyCanopyShadowAmount),
			_canopyShadowAmount,
			targetAmount,
			transitionDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		_canopyShadowTween.TweenCallback(Callable.From(() =>
		{
			if (!receivesCanopyShadow)
				RestoreCanopyShadowMaterials();

			_canopyShadowTween = null;
		}));
	}

	private void RefreshCanopyShadowMaterials()
	{
		RestoreCanopyShadowMaterials();

		if (!_receivesCanopyShadow)
			return;

		ApplyCanopyShadowMaterials(this);
		ApplyCanopyShadowAmount(_canopyShadowAmount);
	}

	private void ApplyCanopyShadowMaterials(Node node)
	{
		if (ReferenceEquals(node, _placementIndicatorRoot) ||
			ReferenceEquals(node, _grassMultiMesh))
		{
			return;
		}

		if (node is GeometryInstance3D geometry)
			ApplyCanopyShadowMaterial(geometry);

		foreach (Node child in node.GetChildren())
			ApplyCanopyShadowMaterials(child);
	}

	private void ApplyCanopyShadowMaterial(GeometryInstance3D geometry)
	{
		Material materialOverride = geometry.MaterialOverride;

		if (materialOverride != null)
		{
			CanopyShadowMaterialBinding binding =
				CreateCanopyShadowMaterialBinding(materialOverride);

			if (binding == null)
				return;

			binding.Geometry = geometry;
			binding.OriginalAssignedMaterial = materialOverride;
			geometry.MaterialOverride = binding.ShadowMaterial;
			_canopyShadowMaterialBindings.Add(binding);
			return;
		}

		if (geometry is not MeshInstance3D meshInstance ||
			meshInstance.Mesh == null)
		{
			return;
		}

		for (int surfaceIndex = 0;
			surfaceIndex < meshInstance.Mesh.GetSurfaceCount();
			surfaceIndex++)
		{
			Material surfaceOverride =
				meshInstance.GetSurfaceOverrideMaterial(surfaceIndex);
			Material sourceMaterial = surfaceOverride ??
				meshInstance.Mesh.SurfaceGetMaterial(surfaceIndex);
			CanopyShadowMaterialBinding binding =
				CreateCanopyShadowMaterialBinding(sourceMaterial);

			if (binding == null)
				continue;

			binding.Geometry = meshInstance;
			binding.SurfaceIndex = surfaceIndex;
			binding.OriginalAssignedMaterial = surfaceOverride;
			meshInstance.SetSurfaceOverrideMaterial(
				surfaceIndex,
				binding.ShadowMaterial);
			_canopyShadowMaterialBindings.Add(binding);
		}
	}

	private static CanopyShadowMaterialBinding
		CreateCanopyShadowMaterialBinding(Material sourceMaterial)
	{
		if (sourceMaterial == null)
			return null;

		Material shadowMaterial = sourceMaterial.Duplicate() as Material;

		if (shadowMaterial == null)
			return null;

		CanopyShadowMaterialBinding binding = new()
		{
			ShadowMaterial = shadowMaterial
		};

		if (shadowMaterial is BaseMaterial3D baseMaterial)
		{
			binding.HasAlbedoColor = true;
			binding.AlbedoColor = baseMaterial.AlbedoColor;
			return binding;
		}

		if (shadowMaterial is not ShaderMaterial shaderMaterial ||
			shaderMaterial.Shader == null)
		{
			return null;
		}

		string shaderCode = shaderMaterial.Shader.Code;

		if (shaderCode.Contains(FoliageColor1Parameter.ToString()))
		{
			binding.HasFoliageColor1 = true;
			binding.FoliageColor1 = (Color)shaderMaterial.GetShaderParameter(
				FoliageColor1Parameter);
		}

		if (shaderCode.Contains(FoliageColor2Parameter.ToString()))
		{
			binding.HasFoliageColor2 = true;
			binding.FoliageColor2 = (Color)shaderMaterial.GetShaderParameter(
				FoliageColor2Parameter);
		}

		return binding.HasFoliageColor1 || binding.HasFoliageColor2
			? binding
			: null;
	}

	private void ApplyCanopyShadowAmount(float amount)
	{
		_canopyShadowAmount = Mathf.Clamp(amount, 0.0f, 1.0f);
		float tintStrength = _canopyShadowAmount *
			Mathf.Clamp(CanopyShadowFieldTint.A, 0.0f, 1.0f);

		foreach (CanopyShadowMaterialBinding binding in
			_canopyShadowMaterialBindings)
		{
			if (binding.Geometry == null ||
				!GodotObject.IsInstanceValid(binding.Geometry))
			{
				continue;
			}

			if (binding.ShadowMaterial is BaseMaterial3D baseMaterial &&
				binding.HasAlbedoColor)
			{
				baseMaterial.AlbedoColor = GetCanopyShadowColor(
					binding.AlbedoColor,
					tintStrength);
			}
			else if (binding.ShadowMaterial is ShaderMaterial shaderMaterial)
			{
				if (binding.HasFoliageColor1)
				{
					shaderMaterial.SetShaderParameter(
						FoliageColor1Parameter,
						GetCanopyShadowColor(
							binding.FoliageColor1,
							tintStrength));
				}

				if (binding.HasFoliageColor2)
				{
					shaderMaterial.SetShaderParameter(
						FoliageColor2Parameter,
						GetCanopyShadowColor(
							binding.FoliageColor2,
							tintStrength));
				}
			}
		}

		if (_grassMultiMesh != null)
		{
			_grassMultiMesh.SetInstanceShaderParameter(
				"canopy_shadow",
				new Vector4(
					CanopyShadowFieldTint.R,
					CanopyShadowFieldTint.G,
					CanopyShadowFieldTint.B,
					tintStrength));
		}
	}

	private Color GetCanopyShadowColor(Color sourceColor, float strength)
	{
		float redMultiplier = Mathf.Lerp(
			1.0f,
			Mathf.Clamp(CanopyShadowFieldTint.R, 0.0f, 1.0f),
			strength);
		float greenMultiplier = Mathf.Lerp(
			1.0f,
			Mathf.Clamp(CanopyShadowFieldTint.G, 0.0f, 1.0f),
			strength);
		float blueMultiplier = Mathf.Lerp(
			1.0f,
			Mathf.Clamp(CanopyShadowFieldTint.B, 0.0f, 1.0f),
			strength);

		return new Color(
			sourceColor.R * redMultiplier,
			sourceColor.G * greenMultiplier,
			sourceColor.B * blueMultiplier,
			sourceColor.A);
	}

	private void RestoreCanopyShadowMaterials()
	{
		foreach (CanopyShadowMaterialBinding binding in
			_canopyShadowMaterialBindings)
		{
			if (binding.Geometry == null ||
				!GodotObject.IsInstanceValid(binding.Geometry))
			{
				continue;
			}

			if (binding.SurfaceIndex < 0)
			{
				if (ReferenceEquals(
					binding.Geometry.MaterialOverride,
					binding.ShadowMaterial))
				{
					binding.Geometry.MaterialOverride =
						binding.OriginalAssignedMaterial;
				}

				continue;
			}

			if (binding.Geometry is MeshInstance3D meshInstance &&
				meshInstance.Mesh != null &&
				binding.SurfaceIndex < meshInstance.Mesh.GetSurfaceCount() &&
				ReferenceEquals(
					meshInstance.GetSurfaceOverrideMaterial(binding.SurfaceIndex),
					binding.ShadowMaterial))
			{
				meshInstance.SetSurfaceOverrideMaterial(
					binding.SurfaceIndex,
					binding.OriginalAssignedMaterial);
			}
		}

		_canopyShadowMaterialBindings.Clear();
	}

	public void ShowFloatingWaterChange(
		int amount,
		Color color,
		Color outlineColor,
		Font font,
		int fontSize,
		int outlineSize,
		float delaySeconds,
		float durationSeconds)
	{
		if (amount == 0)
			return;

		Label3D label = new Label3D
		{
			Name = "WaterChangeFeedback",
			Text = amount > 0 ? $"+{amount}" : amount.ToString(),
			Position = new Vector3(0.0f, 1.35f, 0.0f),
			Font = font,
			FontSize = Mathf.Clamp(fontSize, 32, 96),
			OutlineSize = Mathf.Clamp(outlineSize, 0, 20),
			PixelSize = 0.0065f,
			Modulate = color,
			OutlineModulate = outlineColor,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			NoDepthTest = true,
			Visible = false,
			Scale = new Vector3(0.84f, 0.84f, 0.84f)
		};

		AddChild(label);

		float delay = Mathf.Max(delaySeconds, 0.0f);
		float duration = Mathf.Max(durationSeconds, 0.2f);
		Vector3 targetPosition = label.Position + new Vector3(0.0f, 0.65f, 0.0f);

		Tween tween = CreateTween();

		if (delay > 0.0f)
			tween.TweenInterval(delay);

		tween.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(label))
				label.Visible = true;
		}));

		tween.SetParallel(true);
		tween.TweenProperty(label, "position", targetPosition, duration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(
				label,
				"scale",
				Vector3.One,
				Mathf.Min(duration * 0.35f, 0.32f))
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(
				label,
				"transparency",
				1.0f,
				duration * 0.55f)
			.SetDelay(duration * 0.45f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);

		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(label))
				label.QueueFree();
		}));
	}

	private void SetupPlacementIndicator()
	{
		_placementIndicatorRoot = GetNodeOrNull<Node3D>("HandCardPlacementIndicator");

		if (_placementIndicatorRoot == null)
		{
			_placementIndicatorRoot = GetNodeOrNull<Node3D>("HandCardPlacmentIndicator");
		}

		if (_placementIndicatorRoot == null)
		{
			_placementIndicatorRoot = FindNodeByNamePart(this, "placement");
		}

		if (_placementIndicatorRoot == null)
		{
			_placementIndicatorRoot = FindNodeByNamePart(this, "placment");
		}

		if (_placementIndicatorRoot == null)
		{
			GD.PrintErr($"{Name}: No placement indicator found. Creating fallback indicator.");
			_placementIndicatorRoot = CreateFallbackPlacementIndicatorRoot();
			AddChild(_placementIndicatorRoot);
		}

		_placementIndicatorRoot.Visible = true;
		HideLegacyPlacementGeometry(_placementIndicatorRoot);

		_placementWindIndicator = _placementIndicatorRoot
			.GetNodeOrNull<PlantPlacementWindIndicator>(
				"PlantPlacementWindIndicator");

		if (_placementWindIndicator == null)
		{
			_placementWindIndicator = new PlantPlacementWindIndicator
			{
				Name = "PlantPlacementWindIndicator"
			};
			_placementIndicatorRoot.AddChild(_placementWindIndicator);
		}

		_placementWindIndicator.Position = new Vector3(0.0f, -0.31f, 0.0f);
		_placementWindIndicator.Setup();
		_placementWindIndicator.Conceal();
	}

	private Node3D CreateFallbackPlacementIndicatorRoot()
	{
		Node3D root = new Node3D();

		root.Name = "HandCardPlacementIndicator";
		root.Position = new Vector3(0.0f, 0.35f, 0.0f);
		root.RotationDegrees = new Vector3(0.0f, 30.0f, 0.0f);
		root.Scale = Vector3.One;

		return root;
	}

	private static void HideLegacyPlacementGeometry(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is PlantPlacementWindIndicator)
				continue;

			if (child is GeometryInstance3D geometry)
				geometry.Visible = false;

			HideLegacyPlacementGeometry(child);
		}
	}

	private Node3D FindNodeByNamePart(Node node, string namePart)
	{
		string search = namePart.ToLowerInvariant();

		foreach (Node child in node.GetChildren())
		{
			string childName = child.Name.ToString().ToLowerInvariant();

			if (child is Node3D childNode3D && childName.Contains(search))
			{
				return childNode3D;
			}

			Node3D found = FindNodeByNamePart(child, namePart);

			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private MeshInstance3D FindRenderableTileMesh()
	{
		MeshInstance3D directHexTile = GetNodeOrNull<MeshInstance3D>("hex_tile");

		if (directHexTile != null && directHexTile.Mesh != null)
			return directHexTile;

		MeshInstance3D nestedHexTile = GetNodeOrNull<MeshInstance3D>("hex_tile/MeshInstance3D");

		if (nestedHexTile != null && nestedHexTile.Mesh != null)
			return nestedHexTile;

		MeshInstance3D tileMesh = GetNodeOrNull<MeshInstance3D>("TileMesh");

		if (tileMesh != null && tileMesh.Mesh != null)
			return tileMesh;

		return FindFirstRenderableMeshInstance(this);
	}

	private MeshInstance3D FindFirstRenderableMeshInstance(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (IsIgnoredMeshNode(child))
				continue;

			if (child is MeshInstance3D meshInstance)
			{
				if (meshInstance.Mesh != null)
					return meshInstance;
			}

			MeshInstance3D found = FindFirstRenderableMeshInstance(child);

			if (found != null)
				return found;
		}

		return null;
	}

	private bool IsIgnoredMeshNode(Node node)
	{
		string fullText = "";
		Node current = node;

		while (current != null)
		{
			fullText += $"/{current.Name.ToString().ToLowerInvariant()}";

			if (current == this)
				break;

			current = current.GetParent();
		}

		if (fullText.Contains("handcard"))
			return true;

		if (fullText.Contains("placement"))
			return true;

		if (fullText.Contains("placment"))
			return true;

		if (fullText.Contains("indicator"))
			return true;

		if (fullText.Contains("indikactor"))
			return true;

		if (fullText.Contains("preview"))
			return true;

		return false;
	}

	private void SetupUniqueTileMaterial()
	{
		if (_tileMesh == null)
			return;

		Material sourceMaterial = _tileMesh.MaterialOverride;

		if (sourceMaterial == null && _tileMesh.Mesh.GetSurfaceCount() > 0)
			sourceMaterial = _tileMesh.Mesh.SurfaceGetMaterial(0);

		if (sourceMaterial is not StandardMaterial3D standardMaterial)
			return;

		_tileMaterial = standardMaterial.Duplicate() as StandardMaterial3D;

		if (_tileMaterial != null)
			_tileMesh.MaterialOverride = _tileMaterial;
	}

	private void EnsureCollision()
	{
		StaticBody3D body = GetNodeOrNull<StaticBody3D>("StaticBody3D");

		if (body == null)
		{
			body = GetNodeOrNull<StaticBody3D>("TileCollisionBody");
		}

		if (body == null)
		{
			body = new StaticBody3D();
			body.Name = "StaticBody3D";
			AddChild(body);
		}

		body.CollisionLayer = 1;
		body.CollisionMask = 1;

		CollisionShape3D collisionShape = body.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

		if (collisionShape == null)
		{
			collisionShape = new CollisionShape3D();
			collisionShape.Name = "CollisionShape3D";
			body.AddChild(collisionShape);
		}

		CylinderShape3D shape = new CylinderShape3D();
		shape.Radius = 1.05f * _tileVisualScale;
		shape.Height = 0.55f;

		collisionShape.Shape = shape;
		collisionShape.Disabled = false;
		collisionShape.Position = new Vector3(0.0f, 0.2f, 0.0f);
	}

	public bool CanPlacePlant(PlantDefinition plantDefinition)
	{
		if (Data == null)
			return false;

		return Data.CanPlacePlant(plantDefinition);
	}

	public void PlacePlant(PlantInstance plant)
	{
		if (Data == null)
			return;

		Data.PlacePlant(plant);
		UpdateVisualState();

		GD.Print($"Plant placed: {plant.Definition.DisplayName} on {Coord}");
	}

	public void SetPlacementPreview(bool isValid)
	{
		if (_placementWindIndicator == null)
		{
			GD.PrintErr($"{Name}: Cannot show tile placement indicator because wind visual is null.");
			return;
		}

		_isTutorialHighlightActive = false;
		StopTutorialHighlightGlow();

		if (_placementIndicatorRoot != null)
			_placementIndicatorRoot.Visible = true;

		bool isBlocked = Data?.IsBlocked == true;
		Color effectColor = isBlocked
			? BlockedPreviewTint
			: isValid
				? new Color(0.72f, 1.00f, 0.24f, 1.0f)
				: new Color(0.82f, 0.40f, 0.32f, 0.90f);
		Color fillColor = isBlocked
			? BlockedPreviewTint
			: isValid
				? new Color(0.52f, 0.92f, 0.18f, 1.0f)
				: new Color(0.72f, 0.34f, 0.26f, 1.0f);

		_placementWindIndicator.Display(
			effectColor,
			opacity: isBlocked ? 0.78f : isValid ? 0.96f : 0.90f,
			emissionStrength: isBlocked ? 0.72f : isValid ? 1.08f : 0.92f,
			fillOpacity: isBlocked ? 0.05f : isValid ? 0.14f : 0.055f,
			fillColor: fillColor);
	}

	public void SetTutorialHighlight(bool enabled)
	{
		if (!enabled)
		{
			ClearTutorialHighlight();
			return;
		}

		if (_placementWindIndicator == null)
		{
			GD.PrintErr($"{Name}: Cannot show tutorial highlight because wind visual is null.");
			return;
		}

		_isTutorialHighlightActive = true;

		if (_placementIndicatorRoot != null)
			_placementIndicatorRoot.Visible = true;

		_placementWindIndicator.Display(
			Colors.White,
			TutorialHighlightMinAlpha,
			TutorialHighlightMinEmission);

		StartTutorialHighlightGlow();
	}

	public void ClearTutorialHighlight()
	{
		if (!_isTutorialHighlightActive)
			return;

		_isTutorialHighlightActive = false;
		StopTutorialHighlightGlow();

		_placementWindIndicator?.Conceal();
	}

	private void StartTutorialHighlightGlow()
	{
		StopTutorialHighlightGlow();

		if (_placementWindIndicator == null)
			return;

		SetTutorialHighlightShaderValues(
			TutorialHighlightMinAlpha,
			TutorialHighlightMinEmission);

		_tutorialHighlightTween = CreateTween();
		_tutorialHighlightTween.SetLoops();

		_tutorialHighlightTween.TweenMethod(
			Callable.From<float>((value) =>
			{
				if (!IsInstanceValid(this))
					return;

				float normalized = value;
				float alpha = Mathf.Lerp(
					TutorialHighlightMinAlpha,
					TutorialHighlightMaxAlpha,
					normalized);
				float emission = Mathf.Lerp(
					TutorialHighlightMinEmission,
					TutorialHighlightMaxEmission,
					normalized);

				SetTutorialHighlightShaderValues(alpha, emission);
			}),
			0.0f,
			1.0f,
			TutorialHighlightPulseDuration
		).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		_tutorialHighlightTween.TweenMethod(
			Callable.From<float>((value) =>
			{
				if (!IsInstanceValid(this))
					return;

				float normalized = value;
				float alpha = Mathf.Lerp(
					TutorialHighlightMinAlpha,
					TutorialHighlightMaxAlpha,
					normalized);
				float emission = Mathf.Lerp(
					TutorialHighlightMinEmission,
					TutorialHighlightMaxEmission,
					normalized);

				SetTutorialHighlightShaderValues(alpha, emission);
			}),
			1.0f,
			0.0f,
			TutorialHighlightPulseDuration
		).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	}

	private void SetTutorialHighlightShaderValues(
		float maxAlpha,
		float emissionStrength)
	{
		_placementWindIndicator?.SetIntensity(maxAlpha, emissionStrength);
	}

	private void StopTutorialHighlightGlow()
	{
		if (_tutorialHighlightTween != null)
		{
			_tutorialHighlightTween.Kill();
			_tutorialHighlightTween = null;
		}

		if (_placementWindIndicator != null)
		{
			SetTutorialHighlightShaderValues(
				TutorialHighlightMinAlpha,
				TutorialHighlightMinEmission);
		}
	}

	public void ClearPlacementPreview()
	{
		_isTutorialHighlightActive = false;
		StopTutorialHighlightGlow();

		_placementWindIndicator?.Conceal();
	}

	public void UpdateVisualState()
	{
		if (Data == null)
			return;

		bool refreshAdjacentMushroomVisuals =
			(_renderedPlant?.Definition?.Type == PlantType.Mushroom &&
				!_renderedAsDead) ||
			Data.Plant?.Definition?.Type == PlantType.Mushroom;
		//UpdateTileMaterial();
		RebuildPlantVisual();
		RefreshMushroomNeighborVisual(refreshCanopyShadow: false);

		if (refreshAdjacentMushroomVisuals)
			RefreshAdjacentMushroomNeighborVisuals();

		UpdateLightVisualState();

		if (Data.Plant != null)
		{
			GD.Print($"{Name} | Light: {Data.LightLevel} | Plant: {Data.Plant.Definition.DisplayName}");
		}
	}

	internal void UpdateLightVisualState()
	{
		if (Data == null)
			return;

		UpdateGrassVisual();

		if (_receivesCanopyShadow)
		{
			ApplyCanopyShadowReceiverLayer(this);
			RefreshCanopyShadowMaterials();
		}
	}

	private void RefreshMushroomNeighborVisual(
		bool refreshCanopyShadow = true)
	{
		ClearMushroomNeighborVisual();
		HexTileData sourceTile = FindMatureAdjacentMushroomSource();

		if (sourceTile != null)
		{
			_mushroomNeighborVisualRoot =
				MushroomVisualBuilder.CreateNeighborDecoration(
					sourceTile.Plant,
					MushroomModelScale,
					sourceTile.Coord,
					this);

			if (_mushroomNeighborVisualRoot != null)
			{
				AddChild(_mushroomNeighborVisualRoot);
				MushroomVisualBuilder.AnimateNeighborGrowth(
					_mushroomNeighborVisualRoot,
					MushroomPlacementAnimationDuration /
					MushroomGrowthAnimationSpeed);
				VisibilityRangeUtility.Configure(
					_mushroomNeighborVisualRoot,
					_visibilityRangesEnabled,
					_vegetationVisibilityRange,
					_visibilityRangeMargin,
					_frustumCullMargin);
			}
		}

		RefreshGrassBlockers();

		if (refreshCanopyShadow && _receivesCanopyShadow)
		{
			ApplyCanopyShadowReceiverLayer(this);
			RefreshCanopyShadowMaterials();
		}
	}

	private HexTileData FindMatureAdjacentMushroomSource()
	{
		PlantType targetType =
			Data?.Plant?.Definition?.Type ?? PlantType.None;

		if (Data?.Plant == null ||
			!Data.Plant.IsMature ||
			targetType is PlantType.None or PlantType.Oak or PlantType.Birch)
			return null;

		BoardManager boardManager = FindBoardManager();
		if (boardManager == null)
			return null;

		foreach (HexTileData neighbor in boardManager.GetNeighborData(Coord))
		{
			PlantInstance neighborPlant = neighbor?.Plant;

			if (neighborPlant == null ||
				!neighborPlant.IsMature ||
				neighborPlant.Definition.Type != PlantType.Mushroom ||
				neighborPlant.Definition.EffectType !=
					PlantEffectType.AdjacentPlantsProducePlusOne)
			{
				continue;
			}

			return neighbor;
		}

		return null;
	}

	private void RefreshAdjacentMushroomNeighborVisuals()
	{
		BoardManager boardManager = FindBoardManager();
		if (boardManager == null)
			return;

		foreach (HexTileData neighbor in boardManager.GetNeighborData(Coord))
		{
			HexTile neighborView = boardManager.GetTileView(neighbor.Coord);
			neighborView?.RefreshMushroomNeighborVisual();
		}
	}

	private void ClearMushroomNeighborVisual()
	{
		if (_mushroomNeighborVisualRoot == null)
			return;

		_mushroomNeighborVisualRoot.GetParent()?.RemoveChild(
			_mushroomNeighborVisualRoot);
		_mushroomNeighborVisualRoot.QueueFree();
		_mushroomNeighborVisualRoot = null;
	}

	private void UpdateGrassVisual()
	{
		if (_grassMultiMesh == null)
			return;

		Vector2 densityAndHeight = GetGrassDensityAndHeight(Data);
		float dryAmount = GetGrassDryAmount(Data);
		float seed = Data.Plant != null
			? (int)Data.Plant.Definition.Type + 1.0f
			: Data.DeadPlant != null
				? (int)Data.DeadPlant.Definition.Type + 11.0f
				: 0.0f;

		BoardManager boardManager = FindBoardManager();

		TransitionGrassState(new Vector4(
			densityAndHeight.X,
			densityAndHeight.Y,
			dryAmount,
			seed));
		_grassMultiMesh.SetInstanceShaderParameter(
			"canopy_shadow",
			new Vector4(
				CanopyShadowFieldTint.R,
				CanopyShadowFieldTint.G,
				CanopyShadowFieldTint.B,
				_canopyShadowAmount * CanopyShadowFieldTint.A));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_wind",
			new Vector4(
				_grassWindWaveSpeed,
				_grassWindWaveStrength,
				_grassWindDetailSpeed,
				_grassWindDetailStrength));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_border",
			new Vector4(
				_grassTileWorldCenter.X,
				_grassTileWorldCenter.Z,
				_grassEdgeDistance,
				_grassOuterMargin));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_border_directions_01",
			PackGrassBorderDirections(0, 1));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_border_directions_23",
			PackGrassBorderDirections(2, 3));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_border_directions_45",
			PackGrassBorderDirections(4, 5));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_outer_edges_0123",
			new Vector4(
				_grassOuterEdges[0],
				_grassOuterEdges[1],
				_grassOuterEdges[2],
				_grassOuterEdges[3]));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_outer_edges_45",
			new Vector2(_grassOuterEdges[4], _grassOuterEdges[5]));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_neighbor_dry_01",
			PackGrassNeighborDryAmounts(boardManager, 0, 1));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_neighbor_dry_23",
			PackGrassNeighborDryAmounts(boardManager, 2, 3));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_neighbor_dry_45",
			PackGrassNeighborDryAmounts(boardManager, 4, 5));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_neighbor_state_01",
			PackGrassNeighborStates(boardManager, 0, 1));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_neighbor_state_23",
			PackGrassNeighborStates(boardManager, 2, 3));
		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_neighbor_state_45",
			PackGrassNeighborStates(boardManager, 4, 5));
	}

	private void TransitionGrassState(Vector4 targetState)
	{
		if (!_hasRenderedGrassState)
		{
			_hasRenderedGrassState = true;
			ApplyGrassState(targetState);
			return;
		}

		Vector4 startState = _renderedGrassState;
		startState.W = targetState.W;

		if (Mathf.IsEqualApprox(startState.X, targetState.X) &&
			Mathf.IsEqualApprox(startState.Y, targetState.Y) &&
			Mathf.IsEqualApprox(startState.Z, targetState.Z))
		{
			_grassStateTween?.Kill();
			_grassStateTween = null;
			ApplyGrassState(targetState);
			return;
		}

		_grassStateTween?.Kill();
		ApplyGrassState(startState);
		_grassStateTween = CreateTween()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_grassStateTween.TweenMethod(
			Callable.From<Vector4>(ApplyGrassState),
			startState,
			targetState,
			GrassStateTransitionDuration);
	}

	private void ApplyGrassState(Vector4 state)
	{
		_renderedGrassState = state;
		_grassMultiMesh?.SetInstanceShaderParameter("grass_state", state);
	}

	private Vector2 GetGrassDensityAndHeight(HexTileData tileData)
	{
		float density = _grassBaseDensity;
		float height = 0.78f;

		if (tileData.Plant != null)
		{
			float growth = tileData.Plant.GrowthProgress;

			switch (tileData.Plant.Definition.Type)
			{
				case PlantType.Oak:
					density = Mathf.Lerp(0.72f, 0.42f, growth);
					height = Mathf.Lerp(0.85f, 1.15f, growth);
					break;
				case PlantType.Birch:
					density = Mathf.Lerp(0.78f, 0.52f, growth);
					height = Mathf.Lerp(0.82f, 1.08f, growth);
					break;
				case PlantType.Moss:
					density = Mathf.Lerp(0.92f, 0.78f, growth);
					height = Mathf.Lerp(0.55f, 0.72f, growth);
					break;
				case PlantType.Flower:
					density = Mathf.Lerp(0.86f, 0.68f, growth);
					height = Mathf.Lerp(0.72f, 0.95f, growth);
					break;
				case PlantType.Mushroom:
					density = Mathf.Lerp(0.88f, 0.74f, growth);
					height = Mathf.Lerp(0.65f, 0.86f, growth);
					break;
			}
		}
		else if (tileData.DeadPlant != null)
		{
			density = 1.0f;
			height = 0.72f;
		}
		else if (tileData.LightLevel != LightLevel.Shade)
		{
			height = tileData.LightLevel == LightLevel.Sun ? 0.58f : 0.64f;
		}

		return new Vector2(density, height);
	}

	private static float GetGrassDryAmount(HexTileData tileData)
	{
		if (tileData.Plant != null)
			return 0.0f;

		if (tileData.DeadPlant != null)
			return 0.78f;

		return tileData.LightLevel switch
		{
			LightLevel.Sun => 0.60f,
			LightLevel.PartialShade => 0.42f,
			_ => 0.0f
		};
	}

	private Vector4 PackGrassNeighborDryAmounts(
		BoardManager boardManager,
		int firstDirection,
		int secondDirection)
	{
		Vector2 firstNeighbor = GetGrassNeighborDryAmount(
			boardManager,
			firstDirection);
		Vector2 secondNeighbor = GetGrassNeighborDryAmount(
			boardManager,
			secondDirection);

		return new Vector4(
			firstNeighbor.X,
			firstNeighbor.Y,
			secondNeighbor.X,
			secondNeighbor.Y);
	}

	private Vector2 GetGrassNeighborDryAmount(
		BoardManager boardManager,
		int directionIndex)
	{
		if (boardManager == null)
			return Vector2.Zero;

		HexCoord neighborCoord = HexDirections.GetNeighbor(Coord, directionIndex);
		HexTileData neighborData = boardManager.GetTileData(neighborCoord);

		return neighborData == null
			? Vector2.Zero
			: new Vector2(GetGrassDryAmount(neighborData), 1.0f);
	}

	private Vector4 PackGrassNeighborStates(
		BoardManager boardManager,
		int firstDirection,
		int secondDirection)
	{
		Vector2 firstNeighbor = GetGrassNeighborState(boardManager, firstDirection);
		Vector2 secondNeighbor = GetGrassNeighborState(boardManager, secondDirection);

		return new Vector4(
			firstNeighbor.X,
			firstNeighbor.Y,
			secondNeighbor.X,
			secondNeighbor.Y);
	}

	private Vector2 GetGrassNeighborState(
		BoardManager boardManager,
		int directionIndex)
	{
		if (boardManager == null)
			return Vector2.Zero;

		HexCoord neighborCoord = HexDirections.GetNeighbor(Coord, directionIndex);
		HexTileData neighborData = boardManager.GetTileData(neighborCoord);

		return neighborData == null
			? Vector2.Zero
			: GetGrassDensityAndHeight(neighborData);
	}

	private void SetupGrassCoverage(
		HexCoord grassCoord,
		float horizontalScale = 1.0f,
		bool refreshBlockers = true)
	{
		if (_grassMultiMesh?.Multimesh == null)
			return;

		MultiMesh source = _grassMultiMesh.Multimesh;
		int sourceCount = source.InstanceCount;

		if (sourceCount <= 0)
			return;

		Transform3D[] sourceTransforms = new Transform3D[sourceCount];

		for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
			sourceTransforms[sourceIndex] = source.GetInstanceTransform(sourceIndex);

		int tileSeed = unchecked(
			grassCoord.Q * 73856093 ^ grassCoord.R * 19349663);
		Transform3D grassToTile = GlobalTransform.AffineInverse() *
			_grassMultiMesh.GlobalTransform;
		Transform3D tileToGrass = grassToTile.AffineInverse();
		_grassTileHeight = 0.0f;

		for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
		{
			_grassTileHeight +=
				(grassToTile * sourceTransforms[sourceIndex].Origin).Y;
		}

		_grassTileHeight /= sourceCount;

		float hexRadius = _grassEdgeDistance * 2.0f / Mathf.Sqrt(3.0f);
		_grassPlacementCandidates.Clear();
		int candidateIndex = 0;
		int maximumCandidateCount =
			_grassInstancesPerTile * GrassPlacementCandidateMultiplier;

		while (_grassPlacementCandidates.Count < _grassInstancesPerTile &&
			candidateIndex < maximumCandidateCount)
		{
			int candidateSeed = unchecked(
				tileSeed ^ candidateIndex * 92837111);
			float tileX = Mathf.Lerp(
				-hexRadius,
				hexRadius,
				GetGrassDistributionValue(candidateSeed + 271));
			float tileZ = Mathf.Lerp(
				-_grassEdgeDistance,
				_grassEdgeDistance,
				GetGrassDistributionValue(candidateSeed + 1013));
			float allowedX = hexRadius - Mathf.Abs(tileZ) / Mathf.Sqrt(3.0f);

			candidateIndex++;

			if (Mathf.Abs(tileX) > allowedX)
				continue;

			int candidateOrdinal = _grassPlacementCandidates.Count;
			int instanceSeed = unchecked(
				candidateSeed ^ candidateOrdinal * 83492791);
			float scaleJitter = Mathf.Lerp(
				0.62f,
				1.0f,
				GetGrassDistributionValue(instanceSeed + 761));

			float rotation = Mathf.Tau *
				GetGrassDistributionValue(instanceSeed + 421);
			float heightJitter = Mathf.Lerp(
				0.92f,
				1.08f,
				GetGrassDistributionValue(instanceSeed + 1291));
			float tuftWidth = 2.45f * scaleJitter / horizontalScale;
			float tuftHeight = 2.35f * scaleJitter * heightJitter;
			Basis basis = Basis.Identity
				.Rotated(Vector3.Up, rotation)
				.Scaled(new Vector3(tuftWidth, tuftHeight, tuftWidth));
			Transform3D transform = new Transform3D(
				basis,
				tileToGrass * new Vector3(tileX, _grassTileHeight, tileZ));
			_grassPlacementCandidates.Add(new GrassPlacementCandidate(
				transform,
				new Vector2(tileX, tileZ),
				scaleJitter,
				GetGrassBlockerEdgeVariation(new Vector2(tileX, tileZ))));
		}

		MultiMesh expanded = new MultiMesh();
		expanded.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		expanded.UseCustomData = true;
		expanded.Mesh = GetAnimeGrassPieceMesh() ?? source.Mesh;
		expanded.InstanceCount = _grassPlacementCandidates.Count;
		expanded.VisibleInstanceCount = -1;
		_grassBlockerTween?.Kill();
		_grassBlockerTween = null;
		_renderedGrassBlockerVisibility = new float[0];
		_grassMultiMesh.Multimesh = expanded;

		if (refreshBlockers)
			RefreshGrassBlockers();
	}

	private void RefreshGrassBlockers()
	{
		MultiMesh grassMultiMesh = _grassMultiMesh?.Multimesh;

		if (grassMultiMesh == null)
			return;

		List<GrassBlockerTriangle> blockerTriangles =
			CollectGrassBlockerTriangles();
		int candidateCount = _grassPlacementCandidates.Count;
		float[] targetVisibility = new float[candidateCount];

		for (int candidateIndex = 0;
			candidateIndex < candidateCount;
			candidateIndex++)
		{
			GrassPlacementCandidate candidate =
				_grassPlacementCandidates[candidateIndex];
			if (IsGrassPositionBlocked(
				candidate.TilePosition,
				candidate.ScaleJitter,
				blockerTriangles,
				candidate.BlockerEdgeVariation))
			{
				continue;
			}

			targetVisibility[candidateIndex] = 1.0f;
		}

		if (_renderedGrassBlockerVisibility.Length != candidateCount)
		{
			_renderedGrassBlockerVisibility = targetVisibility;
			ApplySteadyGrassBlockerState(grassMultiMesh, targetVisibility);
			return;
		}

		float[] startVisibility =
			(float[])_renderedGrassBlockerVisibility.Clone();
		List<int> visibleCandidateIndices = new();
		List<int> transitionSlots = new();

		for (int candidateIndex = 0;
			candidateIndex < candidateCount;
			candidateIndex++)
		{
			float start = startVisibility[candidateIndex];
			float target = targetVisibility[candidateIndex];

			if (start <= 0.001f && target <= 0.001f)
				continue;

			int slot = visibleCandidateIndices.Count;
			visibleCandidateIndices.Add(candidateIndex);
			grassMultiMesh.SetInstanceTransform(
				slot,
				_grassPlacementCandidates[candidateIndex].Transform);
			grassMultiMesh.SetInstanceCustomData(
				slot,
				new Color(start, 0.0f, 0.0f, 1.0f));

			if (!Mathf.IsEqualApprox(start, target))
				transitionSlots.Add(slot);
		}

		_grassBlockerTween?.Kill();

		if (transitionSlots.Count == 0)
		{
			_grassBlockerTween = null;
			_renderedGrassBlockerVisibility = targetVisibility;
			ApplySteadyGrassBlockerState(grassMultiMesh, targetVisibility);
			return;
		}

		grassMultiMesh.VisibleInstanceCount = visibleCandidateIndices.Count;
		_grassBlockerTween = CreateTween()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_grassBlockerTween.TweenMethod(
			Callable.From<float>((progress) =>
				ApplyGrassBlockerTransition(
					grassMultiMesh,
					visibleCandidateIndices,
					transitionSlots,
					startVisibility,
					targetVisibility,
					progress)),
			0.0f,
			1.0f,
			GrassBlockerTransitionDuration);
		_grassBlockerTween.TweenCallback(Callable.From(() =>
		{
			if (!ReferenceEquals(_grassMultiMesh?.Multimesh, grassMultiMesh))
				return;

			_renderedGrassBlockerVisibility = targetVisibility;
			ApplySteadyGrassBlockerState(grassMultiMesh, targetVisibility);
			_grassBlockerTween = null;
		}));
	}

	private void ApplyGrassBlockerTransition(
		MultiMesh grassMultiMesh,
		List<int> visibleCandidateIndices,
		List<int> transitionSlots,
		float[] startVisibility,
		float[] targetVisibility,
		float progress)
	{
		if (!ReferenceEquals(_grassMultiMesh?.Multimesh, grassMultiMesh))
			return;

		foreach (int slot in transitionSlots)
		{
			int candidateIndex = visibleCandidateIndices[slot];
			float visibility = Mathf.Lerp(
				startVisibility[candidateIndex],
				targetVisibility[candidateIndex],
				progress);
			_renderedGrassBlockerVisibility[candidateIndex] = visibility;
			grassMultiMesh.SetInstanceCustomData(
				slot,
				new Color(visibility, 0.0f, 0.0f, 1.0f));
		}
	}

	private void ApplySteadyGrassBlockerState(
		MultiMesh grassMultiMesh,
		float[] visibility)
	{
		int visibleIndex = 0;

		for (int candidateIndex = 0;
			candidateIndex < visibility.Length;
			candidateIndex++)
		{
			if (visibility[candidateIndex] < 0.5f)
				continue;

			grassMultiMesh.SetInstanceTransform(
				visibleIndex,
				_grassPlacementCandidates[candidateIndex].Transform);
			grassMultiMesh.SetInstanceCustomData(
				visibleIndex,
				new Color(1.0f, 0.0f, 0.0f, 1.0f));
			visibleIndex++;
		}

		grassMultiMesh.VisibleInstanceCount = visibleIndex;
	}

	private List<GrassBlockerTriangle> CollectGrassBlockerTriangles()
	{
		return CollectGrassBlockerTriangles(null, _grassStoneMargin);
	}

	private List<GrassBlockerTriangle> CollectGrassBlockerTriangles(
		Node excludedBranch,
		float blockerMargin)
	{
		List<GrassBlockerTriangle> triangles = new();
		CollectGrassBlockerTriangles(
			this,
			false,
			false,
			blockerMargin,
			excludedBranch,
			triangles);
		return triangles;
	}

	private void CollectGrassBlockerTriangles(
		Node node,
		bool isFullBlockerBranch,
		bool isBaseBlockerBranch,
		float blockerMargin,
		Node excludedBranch,
		List<GrassBlockerTriangle> triangles)
	{
		if (node == excludedBranch)
			return;

		float branchMargin = node == _plantAnchor
			? GetPlantGrassMargin()
			: node == _mushroomNeighborVisualRoot
				? _grassMushroomMargin
				: blockerMargin;
		bool isFullBlocker =
			isFullBlockerBranch || node.IsInGroup(GrassBlockerGroup);
		bool isBaseBlocker =
			isBaseBlockerBranch || node.IsInGroup(GrassBaseBlockerGroup);

		if ((isFullBlocker || isBaseBlocker) &&
			node is CollisionShape3D collisionShape &&
			!collisionShape.Disabled &&
			collisionShape.Shape is ConcavePolygonShape3D concaveShape)
		{
			Transform3D collisionToTile = GlobalTransform.AffineInverse() *
				collisionShape.GlobalTransform;
			Vector3[] faces = concaveShape.GetFaces();

			for (int index = 0; index + 2 < faces.Length; index += 3)
			{
				Vector3 first = collisionToTile * faces[index];
				Vector3 second = collisionToTile * faces[index + 1];
				Vector3 third = collisionToTile * faces[index + 2];

				if (isBaseBlocker && !isFullBlocker &&
					Mathf.Min(first.Y, Mathf.Min(second.Y, third.Y)) >
					_grassTileHeight + GrassBaseBlockerHeight)
				{
					continue;
				}

				triangles.Add(new GrassBlockerTriangle(
					new Vector2(first.X, first.Z),
					new Vector2(second.X, second.Z),
					new Vector2(third.X, third.Z),
					branchMargin));
			}
		}

		foreach (Node child in node.GetChildren())
		{
			CollectGrassBlockerTriangles(
				child,
				isFullBlocker,
				isBaseBlocker,
				branchMargin,
				excludedBranch,
				triangles);
		}
	}

	public bool TryFindMushroomClusterPosition(
		IReadOnlyList<Vector2> candidates,
		IReadOnlyList<Vector2> footprintCenters,
		IReadOnlyList<float> footprintRadii,
		bool avoidPlantVisuals,
		out Vector2 position)
	{
		position = Vector2.Zero;

		if (candidates == null || candidates.Count == 0 ||
			footprintCenters == null || footprintRadii == null ||
			footprintCenters.Count != footprintRadii.Count)
		{
			return false;
		}

		Node excludedBranch = avoidPlantVisuals ? null : _plantAnchor;
		List<GrassBlockerTriangle> blockerTriangles =
			CollectGrassBlockerTriangles(excludedBranch, 0.0f);

		foreach (Vector2 candidate in candidates)
		{
			bool isBlocked = false;

			for (int footprintIndex = 0;
				footprintIndex < footprintCenters.Count;
				footprintIndex++)
			{
				Vector2 footprintPosition = candidate +
					footprintCenters[footprintIndex];
				float footprintRadius = footprintRadii[footprintIndex];

				foreach (GrassBlockerTriangle triangle in blockerTriangles)
				{
					if (!triangle.BlocksWithClearance(
						footprintPosition,
						footprintRadius))
					{
						continue;
					}

					isBlocked = true;
					break;
				}

				if (isBlocked)
					break;
			}

			if (isBlocked)
				continue;

			position = candidate;
			return true;
		}

		return false;
	}

	private float GetPlantGrassMargin()
	{
		PlantType plantType = Data?.Plant?.Definition?.Type ??
			Data?.DeadPlant?.Definition?.Type ?? PlantType.None;

		return plantType switch
		{
			PlantType.Oak => _grassOakMargin,
			PlantType.Birch => _grassBirchMargin,
			PlantType.Mushroom => _grassMushroomMargin,
			PlantType.Moss => _grassMossMargin,
			_ => _grassStoneMargin
		};
	}

	private static bool IsGrassPositionBlocked(
		Vector2 position,
		float scaleJitter,
		List<GrassBlockerTriangle> triangles,
		float edgeVariation)
	{
		foreach (GrassBlockerTriangle triangle in triangles)
		{
			if (triangle.Blocks(position, scaleJitter, edgeVariation))
				return true;
		}

		return false;
	}

	private float GetGrassBlockerEdgeVariation(Vector2 tilePosition)
	{
		Vector3 worldPosition = GlobalTransform * new Vector3(
			tilePosition.X,
			0.0f,
			tilePosition.Y);
		Vector2 worldXZ = new Vector2(worldPosition.X, worldPosition.Z);
		float broadNoise = SampleSmoothGrassNoise(
			worldXZ * GrassBlockerEdgeNoiseScale,
			15731);
		float detailNoise = SampleSmoothGrassNoise(
			worldXZ * GrassBlockerEdgeDetailScale,
			48109);
		float combinedNoise = broadNoise * 0.72f + detailNoise * 0.28f;

		return Mathf.Lerp(
			GrassBlockerEdgeMinimum,
			GrassBlockerEdgeMaximum,
			combinedNoise);
	}

	private static float SampleSmoothGrassNoise(Vector2 position, int salt)
	{
		int cellX = Mathf.FloorToInt(position.X);
		int cellY = Mathf.FloorToInt(position.Y);
		float blendX = position.X - cellX;
		float blendY = position.Y - cellY;
		blendX = blendX * blendX * (3.0f - 2.0f * blendX);
		blendY = blendY * blendY * (3.0f - 2.0f * blendY);

		float lowerLeft = GetGrassNoiseValue(cellX, cellY, salt);
		float lowerRight = GetGrassNoiseValue(cellX + 1, cellY, salt);
		float upperLeft = GetGrassNoiseValue(cellX, cellY + 1, salt);
		float upperRight = GetGrassNoiseValue(cellX + 1, cellY + 1, salt);
		float lower = Mathf.Lerp(lowerLeft, lowerRight, blendX);
		float upper = Mathf.Lerp(upperLeft, upperRight, blendX);
		return Mathf.Lerp(lower, upper, blendY);
	}

	private static float GetGrassNoiseValue(int cellX, int cellY, int salt)
	{
		int seed = unchecked(
			cellX * 73856093 ^
			cellY * 19349663 ^
			salt * 83492791);
		return GetGrassDistributionValue(seed);
	}

	internal static Mesh GetAnimeGrassPieceMesh()
	{
		if (!_grassPieceLoadAttempted)
		{
			_grassPieceLoadAttempted = true;
			_animeGrassPieceMesh = GD.Load<Mesh>(AnimeGrassPiecePath);

			if (_animeGrassPieceMesh == null)
			{
				GD.PushWarning(
					$"Grasgeometrie konnte nicht geladen werden: {AnimeGrassPiecePath}");
			}
		}

		return _animeGrassPieceMesh;
	}

	private static float GetGrassDistributionValue(int seed)
	{
		uint value = unchecked((uint)seed);
		value ^= value >> 16;
		value *= 0x7feb352d;
		value ^= value >> 15;
		value *= 0x846ca68b;
		value ^= value >> 16;

		return (value & 0x00ffffff) / 16777215.0f;
	}

	private Vector4 PackGrassBorderDirections(int firstIndex, int secondIndex)
	{
		Vector2 first = _grassBorderDirections[firstIndex];
		Vector2 second = _grassBorderDirections[secondIndex];

		return new Vector4(first.X, first.Y, second.X, second.Y);
	}

	/* private void UpdateTileMaterial()
	{
		if (_tileMesh == null || _tileMesh.Mesh == null)
		{
			_tileMesh = FindRenderableTileMesh();
		}

		if (_tileMesh == null)
		{
			GD.PrintErr($"{Name}: Cannot apply grass texture because tile mesh is null.");
			return;
		}

		Texture2D grassTexture = GD.Load<Texture2D>(
			"res://assets/textures/grass/grass.tga");

		if (grassTexture == null)
		{
			GD.PrintErr($"{Name}: Grass texture could not be loaded.");
			return;
		}

		_tileMaterial = new StandardMaterial3D();
		_tileMaterial.AlbedoTexture = grassTexture;
		_tileMaterial.AlbedoColor = Data.IsBlocked
			? BlockedTileTint
			: GetLightLevelTint();
		_tileMaterial.Roughness = 1.0f;
		_tileMaterial.Metallic = 0.0f;
		_tileMaterial.Uv1Scale = new Vector3(1.5f, 1.5f, 1.0f);

		_tileMesh.MaterialOverride = _tileMaterial;
	} */

	private Color GetLightLevelTint()
	{
		return Data.LightLevel switch
		{
			LightLevel.PartialShade => PartialShadeTileTint,
			LightLevel.Shade => ShadeTileTint,
			_ => SunTileTint
		};
	}

	private void RebuildPlantVisual()
	{
		PlantInstance visualPlant = Data.Plant ?? Data.DeadPlant;
		bool renderAsDead =
			Data.Plant == null &&
			Data.DeadPlant != null &&
			Data.DeadPlant.Definition.Type != PlantType.Oak;
		int deadBlockedRounds = renderAsDead ? Data.BlockedRounds : -1;
		int growthStage = visualPlant?.VisualGrowthStage ?? -1;
		PlantInstance previousRenderedPlant = _renderedPlant;
		int previousRenderedGrowthStage = _renderedGrowthStage;
		bool previousRenderedAsDead = _renderedAsDead;
		Vector3 previousVisualScale = _plantVisualRoot?.Scale ?? Vector3.One;
		bool animatePlantDeath =
			renderAsDead &&
			!previousRenderedAsDead &&
			ReferenceEquals(previousRenderedPlant, visualPlant);

		bool visualPresenceMatches = visualPlant == null
			? _plantVisualRoot == null
			: _plantVisualRoot != null;

		if (visualPresenceMatches &&
			ReferenceEquals(_renderedPlant, visualPlant) &&
			_renderedGrowthStage == growthStage &&
			_renderedAsDead == renderAsDead &&
			_renderedDeadBlockedRounds == deadBlockedRounds)
		{
			return;
		}

		if (_plantVisualRoot != null)
		{
			ReleasePlantInspectionRenderLayer(clearRequest: false);
			_treeGrowthTween?.Kill();
			_treeGrowthTween = null;
			Node3D previousVisual = _plantVisualRoot;
			_plantVisualRoot = null;
			previousVisual.GetParent()?.RemoveChild(previousVisual);
			previousVisual.QueueFree();
		}

		_renderedPlant = visualPlant;
		_renderedGrowthStage = growthStage;
		_renderedAsDead = renderAsDead;
		_renderedDeadBlockedRounds = deadBlockedRounds;

		if (visualPlant == null || _plantAnchor == null)
			return;

		int mossAnimationStartStage = growthStage;
		float mossAnimationDuration = 0.0f;
		if (!renderAsDead &&
			visualPlant.Definition.Type == PlantType.Moss)
		{
			if (!ReferenceEquals(previousRenderedPlant, visualPlant))
			{
				mossAnimationStartStage = 0;
				mossAnimationDuration = MossPlacementAnimationDuration;
			}
			else if (growthStage > previousRenderedGrowthStage)
			{
				mossAnimationStartStage = previousRenderedGrowthStage;
				mossAnimationDuration = MossGrowthPhaseAnimationDuration;
			}
		}

		int mushroomAnimationStartStage = growthStage;
		float mushroomAnimationDuration = 0.0f;
		if (!renderAsDead &&
			visualPlant.Definition.Type == PlantType.Mushroom)
		{
			if (!ReferenceEquals(previousRenderedPlant, visualPlant))
			{
				mushroomAnimationStartStage = 0;
				mushroomAnimationDuration =
					MushroomPlacementAnimationDuration;
			}
			else if (growthStage > previousRenderedGrowthStage)
			{
				mushroomAnimationStartStage = previousRenderedGrowthStage;
				mushroomAnimationDuration =
					MushroomGrowthPhaseAnimationDuration /
					MushroomGrowthAnimationSpeed;
			}
		}

		float treeAnimationDuration = 0.0f;
		float treeHorizontalStartScale = 1.0f;
		float treeVerticalStartScale = 1.0f;
		PlantType visualPlantType = visualPlant.Definition.Type;
		bool isTree =
			visualPlantType == PlantType.Oak ||
			visualPlantType == PlantType.Birch;
		bool isStartingOak =
			visualPlantType == PlantType.Oak &&
			Coord.Q == 0 &&
			Coord.R == 0;

		if (!renderAsDead && isTree && !isStartingOak)
		{
			if (!ReferenceEquals(previousRenderedPlant, visualPlant))
			{
				treeAnimationDuration = TreePlacementAnimationDuration;
				treeHorizontalStartScale =
					TreePlacementHorizontalStartScale;
				treeVerticalStartScale = TreePlacementVerticalStartScale;
			}
			else if (growthStage > previousRenderedGrowthStage)
			{
				float previousStageScale = GetTreeGrowthStageScale(
					visualPlantType,
					previousRenderedGrowthStage);
				float targetStageScale = GetTreeGrowthStageScale(
					visualPlantType,
					growthStage);
				float relativeStageScale = previousStageScale /
					Mathf.Max(targetStageScale, 0.01f);

				treeAnimationDuration = TreeGrowthPhaseAnimationDuration;
				treeHorizontalStartScale = Mathf.Clamp(
					relativeStageScale * previousVisualScale.X,
					0.01f,
					1.0f);
				treeVerticalStartScale = Mathf.Clamp(
					relativeStageScale * previousVisualScale.Y,
					0.01f,
					1.0f);
			}
		}

		bool animatePlantGrowth = !renderAsDead;
		_plantVisualRoot = CreatePlantVisual(
			visualPlant,
			this,
			animateGrowth: animatePlantGrowth);
		_plantVisualRoot.Position = Vector3.Zero;
		_plantVisualRoot.Rotation = Vector3.Zero;

		if (renderAsDead)
			ApplyDeadPlantStyle(_plantVisualRoot, deadBlockedRounds);

		if (TreeProximityFadeEnabled &&
			visualPlant.Definition.Type == PlantType.Birch)
		{
			TreeProximityFade3D proximityFade = new TreeProximityFade3D
			{
				Name = "TreeProximityFade",
				FadeStartDistance = TreeFadeStartDistance,
				FadeFullDistance = TreeFadeFullDistance,
				MaximumTransparency = TreeFadeMaximumTransparency,
				FadeSpeed = TreeFadeSpeed
			};
			_plantVisualRoot.AddChild(proximityFade);
		}

		_plantAnchor.AddChild(_plantVisualRoot);
		ReapplyPlantInspectionRenderLayer();

		if (treeAnimationDuration > 0.0f)
		{
			AnimateTreeGrowth(
				_plantVisualRoot,
				treeHorizontalStartScale,
				treeVerticalStartScale,
				treeAnimationDuration);
		}

		FadeInMatureTreeShadow(visualPlant, animatePlantGrowth);
		VisibilityRangeUtility.Configure(
			_plantVisualRoot,
			_visibilityRangesEnabled,
			_vegetationVisibilityRange,
			_visibilityRangeMargin,
			_frustumCullMargin);

		if (animatePlantDeath)
			AnimatePlantDeath(_plantVisualRoot);

		if (mossAnimationDuration > 0.0f)
		{
			MossVisualBuilder.AnimateGrowth(
				_plantVisualRoot,
				Coord,
				mossAnimationStartStage,
				growthStage,
				mossAnimationDuration);
		}

		if (mushroomAnimationDuration > 0.0f)
		{
			MushroomVisualBuilder.AnimateGrowth(
				_plantVisualRoot,
				visualPlant,
				Coord,
				mushroomAnimationStartStage,
				growthStage,
				mushroomAnimationDuration);
		}
	}

	private static float GetTreeGrowthStageScale(
		PlantType plantType,
		int growthStage)
	{
		if (plantType == PlantType.Birch)
		{
			int birchStage = Mathf.Clamp(growthStage, 1, 5);
			return Mathf.Lerp(0.35f, 1.0f, (birchStage - 1) / 4.0f);
		}

		return Mathf.Clamp(growthStage, 1, 4) switch
		{
			1 => 0.28f,
			2 => 0.50f,
			3 => 0.80f,
			_ => 1.0f
		};
	}

	private void AnimateTreeGrowth(
		Node3D visualRoot,
		float horizontalStartScale,
		float verticalStartScale,
		float duration)
	{
		Vector3 targetScale = visualRoot.Scale;
		Vector3 targetPosition = visualRoot.Position;
		visualRoot.Scale = new Vector3(
			targetScale.X * horizontalStartScale,
			targetScale.Y * verticalStartScale,
			targetScale.Z * horizontalStartScale);
		visualRoot.Position = targetPosition -
			Vector3.Up * 0.08f * (1.0f - verticalStartScale);

		_treeGrowthTween?.Kill();
		_treeGrowthTween = CreateTween()
			.SetParallel()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_treeGrowthTween.TweenProperty(
			visualRoot,
			"scale",
			targetScale,
			duration);
		_treeGrowthTween.TweenProperty(
			visualRoot,
			"position",
			targetPosition,
			duration);
	}

	private void AnimatePlantDeath(Node3D visualRoot)
	{
		Vector3 targetScale = visualRoot.Scale;
		Vector3 targetPosition = visualRoot.Position;
		visualRoot.Scale = targetScale / DeadPlantScale;
		visualRoot.Position = targetPosition + Vector3.Up * 0.03f;

		Tween deathTween = CreateTween()
			.SetParallel()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		deathTween.TweenProperty(
			visualRoot,
			"scale",
			targetScale,
			PlantDeathAnimationDuration);
		deathTween.TweenProperty(
			visualRoot,
			"position",
			targetPosition,
			PlantDeathAnimationDuration);
		deathTween.Chain().TweenCallback(
			Callable.From(RefreshGrassBlockers));
	}

	private void FadeInMatureTreeShadow(
		PlantInstance plant,
		bool animateGrowth)
	{
		if (!animateGrowth ||
			TreeShadowFadeDuration <= 0.0f ||
			plant == null ||
			!plant.IsMature)
		{
			return;
		}

		PlantType plantType = plant.Definition.Type;
		bool isTree =
			plantType == PlantType.Oak ||
			plantType == PlantType.Birch;
		bool isStartingOak =
			plantType == PlantType.Oak &&
			Coord.Q == 0 &&
			Coord.R == 0;

		if (!isTree || isStartingOak)
			return;

		Decal canopyShadow = _plantVisualRoot.FindChild(
			"CanopyShadow",
			recursive: true,
			owned: false) as Decal;

		if (canopyShadow == null)
			return;

		Color shadowColor = canopyShadow.Modulate;
		canopyShadow.Modulate = new Color(
			shadowColor.R,
			shadowColor.G,
			shadowColor.B,
			0.0f);

		Tween tween = CreateTween();
		tween.TweenProperty(
			canopyShadow,
			"modulate:a",
			shadowColor.A,
			TreeShadowFadeDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	private void ApplyDeadPlantStyle(
		Node3D visualRoot,
		int blockedRounds)
	{
		visualRoot.Scale *= DeadPlantScale;
		visualRoot.Position += new Vector3(0.0f, -0.03f, 0.0f);

		Node productionAura = visualRoot.FindChild(
			"ProductionAura",
			recursive: true,
			owned: false);
		productionAura?.Free();

		float tintStrength = blockedRounds > 1
			? DeadPlantFirstRoundTintStrength
			: DeadPlantFinalRoundTintStrength;
		ApplyDeadPlantTint(visualRoot, tintStrength);
	}

	private void ApplyDeadPlantTint(Node node, float tintStrength)
	{
		if (node is GeometryInstance3D geometry)
		{
			if (geometry.MaterialOverride != null)
			{
				geometry.MaterialOverride = CreateDeadPlantMaterial(
					geometry.MaterialOverride,
					tintStrength);
			}
			else if (node is MeshInstance3D meshInstance &&
				meshInstance.Mesh != null)
			{
				for (int surfaceIndex = 0;
					surfaceIndex < meshInstance.Mesh.GetSurfaceCount();
					surfaceIndex++)
				{
					Material sourceMaterial =
						meshInstance.GetSurfaceOverrideMaterial(surfaceIndex) ??
						meshInstance.Mesh.SurfaceGetMaterial(surfaceIndex);
					Material deadMaterial = CreateDeadPlantMaterial(
						sourceMaterial,
						tintStrength);

					if (deadMaterial != null)
					{
						meshInstance.SetSurfaceOverrideMaterial(
							surfaceIndex,
							deadMaterial);
					}
				}
			}
		}

		foreach (Node child in node.GetChildren())
			ApplyDeadPlantTint(child, tintStrength);
	}

	private Material CreateDeadPlantMaterial(
		Material sourceMaterial,
		float tintStrength)
	{
		if (sourceMaterial == null)
			return null;

		Material deadMaterial = sourceMaterial.Duplicate(true) as Material;
		if (deadMaterial is BaseMaterial3D baseMaterial)
		{
			Color sourceColor = baseMaterial.AlbedoColor;
			Color targetColor = new Color(
				DeadPlantTint.R,
				DeadPlantTint.G,
				DeadPlantTint.B,
				sourceColor.A);
			baseMaterial.AlbedoColor = sourceColor.Lerp(
				targetColor,
				tintStrength);
			baseMaterial.Roughness = Mathf.Lerp(
				baseMaterial.Roughness,
				1.0f,
				tintStrength);
			baseMaterial.Metallic = Mathf.Lerp(
				baseMaterial.Metallic,
				0.0f,
				tintStrength);
		}
		else if (deadMaterial is ShaderMaterial shaderMaterial)
		{
			TintShaderColor(
				shaderMaterial,
				"foliage_colour1",
				tintStrength);
			TintShaderColor(
				shaderMaterial,
				"foliage_colour2",
				tintStrength);
		}

		return deadMaterial;
	}

	private void TintShaderColor(
		ShaderMaterial shaderMaterial,
		string parameterName,
		float tintStrength)
	{
		if (shaderMaterial.Shader == null ||
			!shaderMaterial.Shader.Code.Contains(parameterName))
		{
			return;
		}

		Color sourceColor = (Color)shaderMaterial.GetShaderParameter(
			parameterName);
		Color targetColor = new Color(
			DeadPlantTint.R,
			DeadPlantTint.G,
			DeadPlantTint.B,
			sourceColor.A);
		shaderMaterial.SetShaderParameter(
			parameterName,
			sourceColor.Lerp(targetColor, tintStrength));
	}

	private BoardManager FindBoardManager()
	{
		Node current = GetParent();

		while (current != null)
		{
			if (current is BoardManager boardManager)
				return boardManager;

			current = current.GetParent();
		}

		return null;
	}

	private Node3D CreatePlantVisual(
		PlantInstance plant,
		HexTile tile,
		bool animateGrowth)
	{
		Node3D root = new Node3D();
		root.Name = $"{plant.Definition.Type}_Visual";

		Node3D factoryVisual = PlantVisualFactory.CreateVisual(
			plant,
			tile,
			animateGrowth,
			showTreeShadow: animateGrowth);

		if (factoryVisual != null)
		{
			return factoryVisual;
		}

		switch (plant.Definition.Type)
		{
			case PlantType.Oak:
				CreateOakVisual(root, plant);
				break;

			case PlantType.Moss:
				CreateMossVisual(root, plant);
				break;

			case PlantType.Flower:
				CreateFlowerVisual(root, plant);
				break;

			case PlantType.Birch:
				CreateBirchVisual(root, plant);
				break;

			case PlantType.Mushroom:
				CreateMushroomVisual(root, plant);
				break;
		}

		return root;
	}

	private void CreateOakVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.35f, 0.0f),
			0.11f,
			0.15f,
			0.7f,
			new Color("6b4f2d")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.92f, 0.0f),
			0.33f,
			new Color("4d7f45"),
			new Vector3(1.1f, 0.9f, 1.1f)
		));

		root.AddChild(CreateSphere(
			new Vector3(-0.18f, 0.82f, 0.06f),
			0.22f,
			new Color("5d914d"),
			new Vector3(1.0f, 0.85f, 1.0f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.2f, 0.82f, -0.05f),
			0.2f,
			new Color("3f6f39"),
			new Vector3(1.0f, 0.8f, 1.0f)
		));
	}

	private void CreateBirchVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.42f, 0.0f),
			0.075f,
			0.095f,
			0.85f,
			new Color("d7d2c8")
		));

		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.44f, 0.0f),
			0.083f,
			0.103f,
			0.18f,
			new Color("3b332e")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.98f, 0.0f),
			0.28f,
			new Color("82a85f"),
			new Vector3(1.0f, 0.9f, 1.0f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.18f, 0.9f, 0.04f),
			0.18f,
			new Color("6f9652"),
			new Vector3(1.0f, 0.85f, 1.0f)
		));
	}

	private void CreateMossVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.055f, 0.0f),
			0.22f,
			new Color("5a8f45"),
			new Vector3(1.5f, 0.28f, 1.2f)
		));

		root.AddChild(CreateSphere(
			new Vector3(-0.18f, 0.065f, 0.1f),
			0.14f,
			new Color("6ca252"),
			new Vector3(1.3f, 0.25f, 1.1f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.18f, 0.06f, -0.08f),
			0.13f,
			new Color("497d39"),
			new Vector3(1.25f, 0.25f, 1.0f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.05f, 0.075f, 0.18f),
			0.11f,
			new Color("7fb35f"),
			new Vector3(1.2f, 0.23f, 1.0f)
		));
	}

	private void CreateFlowerVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.24f, 0.0f),
			0.025f,
			0.035f,
			0.48f,
			new Color("4b7d3b")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.52f, 0.0f),
			0.08f,
			new Color("d9c14a"),
			new Vector3(1.0f, 1.0f, 1.0f)
		));

		Color petalColor = new Color("d88cc8");

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.52f, 0.09f),
			0.055f,
			petalColor,
			new Vector3(1.0f, 0.65f, 1.4f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.52f, -0.09f),
			0.055f,
			petalColor,
			new Vector3(1.0f, 0.65f, 1.4f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.09f, 0.52f, 0.0f),
			0.055f,
			petalColor,
			new Vector3(1.4f, 0.65f, 1.0f)
		));

		root.AddChild(CreateSphere(
			new Vector3(-0.09f, 0.52f, 0.0f),
			0.055f,
			petalColor,
			new Vector3(1.4f, 0.65f, 1.0f)
		));
	}

	private void CreateMushroomVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.14f, 0.0f),
			0.045f,
			0.06f,
			0.28f,
			new Color("d8c7aa")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.31f, 0.0f),
			0.16f,
			new Color("9a5c47"),
			new Vector3(1.2f, 0.55f, 1.2f)
		));

		root.AddChild(CreateSphere(
			new Vector3(-0.05f, 0.36f, 0.04f),
			0.025f,
			new Color("eadcc8")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.06f, 0.36f, -0.03f),
			0.018f,
			new Color("eadcc8")
		));
	}

	private MeshInstance3D CreateCylinder(
		Vector3 position,
		float topRadius,
		float bottomRadius,
		float height,
		Color color
	)
	{
		MeshInstance3D meshInstance = new MeshInstance3D();
		CylinderMesh mesh = new CylinderMesh();

		mesh.TopRadius = topRadius;
		mesh.BottomRadius = bottomRadius;
		mesh.Height = height;

		meshInstance.Mesh = mesh;
		meshInstance.Position = position;
		meshInstance.MaterialOverride = CreateMaterial(color);

		return meshInstance;
	}

	private MeshInstance3D CreateSphere(
		Vector3 position,
		float radius,
		Color color,
		Vector3? scaleOverride = null
	)
	{
		MeshInstance3D meshInstance = new MeshInstance3D();
		SphereMesh mesh = new SphereMesh();

		mesh.Radius = radius;
		mesh.Height = radius * 2.0f;

		meshInstance.Mesh = mesh;
		meshInstance.Position = position;
		meshInstance.MaterialOverride = CreateMaterial(color);

		if (scaleOverride.HasValue)
		{
			meshInstance.Scale = scaleOverride.Value;
		}

		return meshInstance;
	}

	private StandardMaterial3D CreateMaterial(Color color)
	{
		StandardMaterial3D material = new StandardMaterial3D();

		material.AlbedoColor = color;
		material.Roughness = 1.0f;
		material.Metallic = 0.0f;

		return material;
	}
}
