using Godot;
using System.Collections.Generic;

public partial class HexTile : Node3D
{
	public HexTileData Data { get; private set; }

	public HexCoord Coord => Data.Coord;

	private static Shader _placementPreviewShader;

	private MeshInstance3D _tileMesh;

	private Node3D _plantAnchor;
	private Node3D _plantVisualRoot;
	private MultiMeshInstance3D _grassMultiMesh;
	private readonly List<GeometryInstance3D> _tileVisualGeometry = new();

	private Node3D _placementIndicatorRoot;
	private MeshInstance3D _placementIndicatorMesh;

	private StandardMaterial3D _tileMaterial;
	private Material _validPreviewMaterial;
	private Material _invalidPreviewMaterial;
	private Material _blockedPreviewMaterial;
	private Material _tutorialHighlightMaterial;
	private bool _isTutorialHighlightActive;
	private Tween _tutorialHighlightTween;

	private const float TutorialHighlightMinAlpha = 0.22f;
	private const float TutorialHighlightMaxAlpha = 0.38f;
	private const float TutorialHighlightMinEmission = 0.0f;
	private const float TutorialHighlightMaxEmission = 0.0f;
	private const float TutorialHighlightPulseDuration = 0.85f;
	private PlantInstance _renderedPlant;
	private int _renderedGrowthStage = -1;
	private bool _renderedAsDead;
	private const string AnimeGrassPiecePath =
		"res://assets/models/plants/grass/anime_grass_piece.obj";
	private static readonly StringName GrassBlockerGroup = "grass_blocker";
	private static readonly StringName GrassBaseBlockerGroup =
		"grass_base_blocker";
	private static readonly StringName GrassTightBlockerGroup =
		"grass_tight_blocker";
	private const float GrassBlockerClearance = 0.30f;
	private const float GrassBaseBlockerHeight = 0.45f;
	private const float GrassTightBlockerClearanceScale = 0.45f;
	private const int GrassPlacementCandidateMultiplier = 16;
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
	private Vector2[] _grassBorderDirections = new Vector2[6];
	private float[] _grassOuterEdges = new float[6];
	private float _tileVisualScale = 1.0f;
	private float _grassTileHeight;
	private readonly List<GrassPlacementCandidate> _grassPlacementCandidates =
		new();

	private readonly struct GrassPlacementCandidate
	{
		public Transform3D Transform { get; }
		public Vector2 TilePosition { get; }
		public float BlockerClearance { get; }

		public GrassPlacementCandidate(
			Transform3D transform,
			Vector2 tilePosition,
			float blockerClearance)
		{
			Transform = transform;
			TilePosition = tilePosition;
			BlockerClearance = blockerClearance;
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
		private readonly float _clearanceScale;

		public GrassBlockerTriangle(
			Vector2 a,
			Vector2 b,
			Vector2 c,
			float clearanceScale = 1.0f)
		{
			_a = a;
			_b = b;
			_c = c;
			_minX = Mathf.Min(a.X, Mathf.Min(b.X, c.X));
			_maxX = Mathf.Max(a.X, Mathf.Max(b.X, c.X));
			_minY = Mathf.Min(a.Y, Mathf.Min(b.Y, c.Y));
			_maxY = Mathf.Max(a.Y, Mathf.Max(b.Y, c.Y));
			_clearanceScale = Mathf.Max(0.0f, clearanceScale);
		}

		public bool Blocks(Vector2 point, float clearance)
		{
			float scaledClearance = clearance * _clearanceScale;

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
	public Color TreeShadowColor { get; private set; } =
		new Color(0.015f, 0.025f, 0.012f, 0.86f);
	public float StartingOakShadowSize { get; private set; } = 6.2f;
	public Vector2 StartingOakShadowOffset { get; private set; } =
		Vector2.Zero;
	public float BirchShadowSize { get; private set; } = 2.8f;
	public Vector2 BirchShadowOffset { get; private set; } =
		new Vector2(0.0f, 0.18f);
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
		Color shadowColor,
		float startingOakShadowSize,
		Vector2 startingOakShadowOffset,
		float birchShadowSize,
		Vector2 birchShadowOffset)
	{
		TreeShadowColor = shadowColor;
		StartingOakShadowSize = Mathf.Max(0.1f, startingOakShadowSize);
		StartingOakShadowOffset = startingOakShadowOffset;
		BirchShadowSize = Mathf.Max(0.1f, birchShadowSize);
		BirchShadowOffset = birchShadowOffset;
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
		Vector2[] borderDirections,
		float[] outerEdges)
	{
		_grassBaseDensity = Mathf.Clamp(baseDensity, 0.0f, 1.0f);
		_grassInstancesPerTile = Mathf.Clamp(instancesPerTile, 64, 512);
		_grassWindWaveSpeed = Mathf.Max(0.0f, windWaveSpeed);
		_grassWindWaveStrength = Mathf.Max(0.0f, windWaveStrength);
		_grassWindDetailSpeed = Mathf.Max(0.0f, windDetailSpeed);
		_grassWindDetailStrength = Mathf.Max(0.0f, windDetailStrength);
		_grassTileWorldCenter = tileWorldCenter;
		_grassEdgeDistance = Mathf.Max(0.0f, edgeDistance);
		_grassOuterMargin = Mathf.Clamp(outerMargin, 0.0f, _grassEdgeDistance);

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

		if (_plantAnchor == null)
		{
			GD.PrintErr($"{Name}: PlantAnchor not found. Creating fallback PlantAnchor.");
			_plantAnchor = new Node3D();
			_plantAnchor.Name = "PlantAnchor";
			AddChild(_plantAnchor);
		}

		SetupPlacementIndicator();
		ApplyTileVisualScale();
		SetupGrassCoverage();
		CollectVisibleTileGeometry(this);
		SetupUniqueTileMaterial();
		EnsureCollision();
		UpdateVisualState();
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
		_validPreviewMaterial = CreatePlacementIndicatorMaterial(
			new Color(0.25f, 1.0f, 0.45f, 1.0f));

		_invalidPreviewMaterial = CreatePlacementIndicatorMaterial(
			new Color(1.0f, 0.15f, 0.15f, 1.0f));

		_blockedPreviewMaterial = CreatePlacementIndicatorMaterial(
			BlockedPreviewTint);

		_tutorialHighlightMaterial = CreatePlacementIndicatorMaterial(
			new Color(1.0f, 1.0f, 1.0f, 1.0f),
			maxAlpha: TutorialHighlightMinAlpha,
			emissionStrength: TutorialHighlightMinEmission,
			fadePower: 0.65f);

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

		_placementIndicatorMesh = FindFirstMeshInstance(_placementIndicatorRoot);

		if (_placementIndicatorMesh == null)
		{
			GD.PrintErr($"{Name}: Placement indicator root found, but no MeshInstance3D inside. Creating fallback mesh.");
			_placementIndicatorMesh = CreateFallbackPlacementIndicatorMesh();
			_placementIndicatorRoot.AddChild(_placementIndicatorMesh);
		}

		_placementIndicatorMesh.Visible = false;
		_placementIndicatorMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		_placementIndicatorMesh.MaterialOverride = _validPreviewMaterial;
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

	private MeshInstance3D CreateFallbackPlacementIndicatorMesh()
	{
		MeshInstance3D meshInstance = new MeshInstance3D();
		CylinderMesh mesh = new CylinderMesh();

		mesh.TopRadius = 0.55f;
		mesh.BottomRadius = 0.55f;
		mesh.Height = 0.45f;

		meshInstance.Name = "PlacementDebugMesh";
		meshInstance.Mesh = mesh;
		meshInstance.Position = Vector3.Zero;
		meshInstance.MaterialOverride = _validPreviewMaterial;

		return meshInstance;
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

	private MeshInstance3D FindFirstMeshInstance(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D meshInstance)
			{
				return meshInstance;
			}

			MeshInstance3D found = FindFirstMeshInstance(child);

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
		if (_placementIndicatorMesh == null)
		{
			GD.PrintErr($"{Name}: Cannot show tile placement indicator because mesh is null.");
			return;
		}

		_isTutorialHighlightActive = false;
		StopTutorialHighlightGlow();

		if (_placementIndicatorRoot != null)
			_placementIndicatorRoot.Visible = true;

		_placementIndicatorMesh.Visible = true;
		_placementIndicatorMesh.MaterialOverride = Data?.IsBlocked == true
			? _blockedPreviewMaterial
			: isValid
				? _validPreviewMaterial
				: _invalidPreviewMaterial;
	}

	public void SetTutorialHighlight(bool enabled)
	{
		if (!enabled)
		{
			ClearTutorialHighlight();
			return;
		}

		if (_placementIndicatorMesh == null)
		{
			GD.PrintErr($"{Name}: Cannot show tutorial highlight because mesh is null.");
			return;
		}

		_isTutorialHighlightActive = true;

		if (_placementIndicatorRoot != null)
			_placementIndicatorRoot.Visible = true;

		_placementIndicatorMesh.Visible = true;
		_placementIndicatorMesh.MaterialOverride = _tutorialHighlightMaterial;

		StartTutorialHighlightGlow();
	}

	public void ClearTutorialHighlight()
	{
		if (!_isTutorialHighlightActive)
			return;

		_isTutorialHighlightActive = false;
		StopTutorialHighlightGlow();

		if (_placementIndicatorMesh != null)
			_placementIndicatorMesh.Visible = false;
	}

	private void StartTutorialHighlightGlow()
	{
		StopTutorialHighlightGlow();

		if (_tutorialHighlightMaterial is not ShaderMaterial shaderMaterial)
			return;

		SetTutorialHighlightShaderValues(
			shaderMaterial,
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

				SetTutorialHighlightShaderValues(shaderMaterial, alpha, emission);
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

				SetTutorialHighlightShaderValues(shaderMaterial, alpha, emission);
			}),
			1.0f,
			0.0f,
			TutorialHighlightPulseDuration
		).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	}

	private void SetTutorialHighlightShaderValues(
		ShaderMaterial shaderMaterial,
		float maxAlpha,
		float emissionStrength)
	{
		shaderMaterial.SetShaderParameter("max_alpha", maxAlpha);
		shaderMaterial.SetShaderParameter("emission_strength", emissionStrength);
	}

	private void StopTutorialHighlightGlow()
	{
		if (_tutorialHighlightTween != null)
		{
			_tutorialHighlightTween.Kill();
			_tutorialHighlightTween = null;
		}

		if (_tutorialHighlightMaterial is ShaderMaterial shaderMaterial)
		{
			SetTutorialHighlightShaderValues(
				shaderMaterial,
				TutorialHighlightMinAlpha,
				TutorialHighlightMinEmission);
		}
	}

	public void ClearPlacementPreview()
	{
		_isTutorialHighlightActive = false;
		StopTutorialHighlightGlow();

		if (_placementIndicatorMesh == null)
			return;

		_placementIndicatorMesh.Visible = false;
	}

	private Material CreatePlacementIndicatorMaterial(
		Color color,
		float maxAlpha = 0.28f,
		float emissionStrength = 0.45f,
		float fadePower = 1.35f)
	{
		ShaderMaterial material = new ShaderMaterial();

		material.Shader = GetPlacementPreviewShader();

		material.SetShaderParameter("base_color", color);
		material.SetShaderParameter("bottom_y", -0.25f);
		material.SetShaderParameter("top_y", 0.75f);
		material.SetShaderParameter("max_alpha", maxAlpha);
		material.SetShaderParameter("min_alpha", 0.0f);
		material.SetShaderParameter("fade_power", fadePower);
		material.SetShaderParameter("emission_strength", emissionStrength);

		return material;
	}

	private Shader GetPlacementPreviewShader()
	{
		if (_placementPreviewShader != null)
			return _placementPreviewShader;

		_placementPreviewShader = new Shader();

		_placementPreviewShader.Code = @"
shader_type spatial;
render_mode blend_mix, unshaded, depth_prepass_alpha;

uniform vec4 base_color : source_color = vec4(0.25, 1.0, 0.45, 1.0);

uniform float bottom_y = -0.25;
uniform float top_y = 0.75;

uniform float max_alpha = 0.28;
uniform float min_alpha = 0.0;
uniform float fade_power = 1.35;
uniform float emission_strength = 0.45;

varying float local_height;

void vertex() {
    local_height = VERTEX.y;
}

void fragment() {
    float height_range = max(top_y - bottom_y, 0.001);
    float height_factor = clamp((local_height - bottom_y) / height_range, 0.0, 1.0);
    float fade = pow(height_factor, fade_power);

    float alpha = mix(max_alpha, min_alpha, fade);

    ALBEDO = base_color.rgb;
    EMISSION = base_color.rgb * emission_strength;
    ALPHA = alpha;
}
";

		return _placementPreviewShader;
	}

	public void UpdateVisualState()
	{
		if (Data == null)
			return;

		//UpdateTileMaterial();
		RebuildPlantVisual();
		UpdateGrassVisual();

		if (Data.Plant != null)
		{
			GD.Print($"{Name} | Light: {Data.LightLevel} | Plant: {Data.Plant.Definition.DisplayName}");
		}
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

		_grassMultiMesh.SetInstanceShaderParameter(
			"grass_state",
			new Vector4(densityAndHeight.X, densityAndHeight.Y, dryAmount, seed));
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

	private void SetupGrassCoverage()
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

		int tileSeed = unchecked(Coord.Q * 73856093 ^ Coord.R * 19349663);
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
			float tuftWidth = 2.45f * scaleJitter;
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
				GrassBlockerClearance * scaleJitter));
		}

		MultiMesh expanded = new MultiMesh();
		expanded.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		expanded.Mesh = GetAnimeGrassPieceMesh() ?? source.Mesh;
		expanded.InstanceCount = _grassPlacementCandidates.Count;
		expanded.VisibleInstanceCount = -1;
		_grassMultiMesh.Multimesh = expanded;
		RefreshGrassBlockers();
	}

	private void RefreshGrassBlockers()
	{
		MultiMesh grassMultiMesh = _grassMultiMesh?.Multimesh;

		if (grassMultiMesh == null)
			return;

		List<GrassBlockerTriangle> blockerTriangles =
			CollectGrassBlockerTriangles();
		int visibleIndex = 0;

		foreach (GrassPlacementCandidate candidate in _grassPlacementCandidates)
		{
			if (IsGrassPositionBlocked(
				candidate.TilePosition,
				candidate.BlockerClearance,
				blockerTriangles))
			{
				continue;
			}

			grassMultiMesh.SetInstanceTransform(
				visibleIndex,
				candidate.Transform);
			visibleIndex++;
		}

		grassMultiMesh.VisibleInstanceCount = visibleIndex;
	}

	private List<GrassBlockerTriangle> CollectGrassBlockerTriangles()
	{
		List<GrassBlockerTriangle> triangles = new();
		CollectGrassBlockerTriangles(this, false, false, false, triangles);
		return triangles;
	}

	private void CollectGrassBlockerTriangles(
		Node node,
		bool isFullBlockerBranch,
		bool isBaseBlockerBranch,
		bool isTightBlockerBranch,
		List<GrassBlockerTriangle> triangles)
	{
		bool isFullBlocker =
			isFullBlockerBranch || node.IsInGroup(GrassBlockerGroup);
		bool isBaseBlocker =
			isBaseBlockerBranch || node.IsInGroup(GrassBaseBlockerGroup);
		bool isTightBlocker =
			isTightBlockerBranch || node.IsInGroup(GrassTightBlockerGroup);

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
					isTightBlocker
						? GrassTightBlockerClearanceScale
						: 1.0f));
			}
		}

		foreach (Node child in node.GetChildren())
		{
			CollectGrassBlockerTriangles(
				child,
				isFullBlocker,
				isBaseBlocker,
				isTightBlocker,
				triangles);
		}
	}

	private static bool IsGrassPositionBlocked(
		Vector2 position,
		float clearance,
		List<GrassBlockerTriangle> triangles)
	{
		foreach (GrassBlockerTriangle triangle in triangles)
		{
			if (triangle.Blocks(position, clearance))
				return true;
		}

		return false;
	}

	private static Mesh GetAnimeGrassPieceMesh()
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
		int growthStage = visualPlant?.VisualGrowthStage ?? -1;

		bool visualPresenceMatches = visualPlant == null
			? _plantVisualRoot == null
			: _plantVisualRoot != null;

		if (visualPresenceMatches &&
			ReferenceEquals(_renderedPlant, visualPlant) &&
			_renderedGrowthStage == growthStage &&
			_renderedAsDead == renderAsDead)
		{
			return;
		}

		if (_plantVisualRoot != null)
		{
			Node3D previousVisual = _plantVisualRoot;
			_plantVisualRoot = null;
			previousVisual.GetParent()?.RemoveChild(previousVisual);
			previousVisual.QueueFree();
		}

		_renderedPlant = visualPlant;
		_renderedGrowthStage = growthStage;
		_renderedAsDead = renderAsDead;

		if (visualPlant == null || _plantAnchor == null)
		{
			RefreshGrassBlockers();
			return;
		}

		_plantVisualRoot = CreatePlantVisual(
			visualPlant,
			this,
			animateGrowth: !renderAsDead);
		_plantVisualRoot.Position = Vector3.Zero;
		_plantVisualRoot.Rotation = Vector3.Zero;

		if (renderAsDead)
			ApplyDeadPlantStyle(_plantVisualRoot);

		_plantAnchor.AddChild(_plantVisualRoot);
		RefreshGrassBlockers();
	}

	private void ApplyDeadPlantStyle(Node3D visualRoot)
	{
		visualRoot.Scale *= DeadPlantScale;
		visualRoot.Position += new Vector3(0.0f, -0.03f, 0.0f);

		Node productionAura = visualRoot.FindChild(
			"ProductionAura",
			recursive: true,
			owned: false);
		productionAura?.Free();

		StandardMaterial3D deadMaterial = new StandardMaterial3D();
		deadMaterial.AlbedoColor = DeadPlantTint;
		deadMaterial.Roughness = 1.0f;
		deadMaterial.Metallic = 0.0f;

		ApplyMaterialOverride(visualRoot, deadMaterial);
	}

	private static void ApplyMaterialOverride(Node node, Material material)
	{
		if (node is GeometryInstance3D geometry)
			geometry.MaterialOverride = material;

		foreach (Node child in node.GetChildren())
			ApplyMaterialOverride(child, material);
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
			showTreeShadow: false);

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
