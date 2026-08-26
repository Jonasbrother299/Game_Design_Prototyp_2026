using Godot;
using System.Collections.Generic;

[System.Flags]
internal enum OuterRingVisualGroup
{
	None = 0,
	OuterGrass = 1 << 0,
	CommonTree = 1 << 1,
	Pine1 = 1 << 2,
	Pine2 = 1 << 3,
	Pine3 = 1 << 4,
	Bush = 1 << 5,
	FloweringBush = 1 << 6,
	Flowers = 1 << 7,
	Mushrooms = 1 << 8,
	Other = 1 << 9
}

public partial class BoardManager : Node3D
{
	private static readonly string[] DefaultHexTileVariantPaths =
	{
		"res://scenes/board/tiles/HexTile1.tscn",
		"res://scenes/board/tiles/HexTile2.tscn",
		"res://scenes/board/tiles/HexTile3.tscn"
	};
	private const string DefaultSide1StonePath =
		"res://assets/models/Hextilestones/Hextilestone_side1.glb";
	private const string DefaultSide2StonePath =
		"res://assets/models/Hextilestones/Hextilestone_side2.glb";
	private const string DefaultCornerStonePath =
		"res://assets/models/Hextilestones/Hextilestone_corner.glb";
	private static readonly string[] DefaultBorderRockScenePaths =
	{
		"res://scenes/board/tiles/rocks/rock_1.tscn",
		"res://scenes/board/tiles/rocks/rock_2.tscn",
		"res://scenes/board/tiles/rocks/rock_3.tscn",
		"res://scenes/board/tiles/rocks/rock_4.tscn"
	};
	private static readonly string[] DefaultOuterTreeScenePaths =
	{
		"res://assets/models/stylized_nature/CommonTree_1.gltf",
		"res://assets/models/stylized_nature/Pine_1.gltf",
		"res://assets/models/stylized_nature/Pine_2.gltf",
		"res://assets/models/stylized_nature/Pine_3.gltf"
	};
	private static readonly string[] DefaultOuterDetailScenePaths =
	{
		"res://assets/models/stylized_nature/Bush_Common.gltf",
		"res://assets/models/stylized_nature/Bush_Common_Flowers.gltf",
		"res://assets/models/stylized_nature/Flower_3_Group.gltf",
		"res://assets/models/stylized_nature/Mushroom_Common.gltf"
	};

	[ExportGroup("Balance")]
	[Export] public GameConfig Balance;

	[ExportGroup("Board Visual")]
	[Export] public PackedScene HexTileScene;
	[Export] public Godot.Collections.Array<PackedScene> HexTileVariants = new();
	[Export(PropertyHint.Range, "0.8,1.5,0.05")]
	public float HexSize = 1.15f;

	[ExportGroup("Grass Visual")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float GrassBaseDensity = 1.0f;

	[Export(PropertyHint.Range, "64,4096,16")]
	public int GrassInstancesPerTile = 320;

	[Export(PropertyHint.Range, "0.0,1.0,0.005")]
	public float GrassWindWaveSpeed = 0.035f;

	[Export(PropertyHint.Range, "0.0,0.2,0.005")]
	public float GrassWindWaveStrength = 0.075f;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float GrassWindDetailSpeed = 0.07f;

	[Export(PropertyHint.Range, "0.0,0.1,0.002")]
	public float GrassWindDetailStrength = 0.012f;

	[Export(PropertyHint.Range, "0.0,0.8,0.01")]
	public float GrassOuterMargin = 0.16f;

	[ExportSubgroup("Model Margins")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float GrassStoneMargin = 0.30f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float GrassOakMargin = 0.30f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float GrassBirchMargin = 0.30f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float GrassMushroomMargin = 0.14f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float GrassMossMargin = 0.30f;

	[ExportGroup("Stone Border")]
	[Export] public bool ShowStoneBorder = true;
	[Export] public PackedScene Side1StoneScene;
	[Export] public PackedScene Side2StoneScene;
	[Export] public PackedScene CornerStoneScene;
	[Export] public Godot.Collections.Array<PackedScene> BorderRockScenes = new();

	[Export(PropertyHint.Range, "0.1,1.0,0.01")]
	public float BorderRockScale = 0.42f;

	[Export(PropertyHint.Range, "1,6,1")]
	public int BorderRocksPerEdge = 4;

	[Export(PropertyHint.Range, "0.0,0.5,0.01")]
	public float BorderRockPositionJitter = 0.0f;

	[Export(PropertyHint.Range, "-1.0,1.0,0.01")]
	public float StoneBorderHeight = -0.15f;

	[Export(PropertyHint.Range, "0.1,2.0,0.01")]
	public float StoneBorderYScale = 1.0f;

	[Export(PropertyHint.Range, "-0.5,0.5,0.01")]
	public float StoneBorderOutwardOffset = 0.0f;

	[ExportGroup("Natural Board Basin")]
	[Export] public bool ShowNaturalBoardBasin = true;

	[Export(PropertyHint.Range, "-0.5,0.0,0.01")]
	public float BoardBasinTopHeight = -0.11f;

	[Export(PropertyHint.Range, "0.1,1.5,0.01")]
	public float BoardBasinDepth = 0.58f;

	[Export(PropertyHint.Range, "0.0,0.8,0.01")]
	public float BoardBasinWallInset = 0.18f;

	[Export(PropertyHint.Range, "0.0,0.12,0.005")]
	public float BoardBasinSurfaceVariation = 0.025f;

	[Export(PropertyHint.Range, "1,6,1")]
	public int BoardBasinWallSegments = 3;

	[Export] public Color BoardBasinTopColor =
		new Color(0.28f, 0.20f, 0.11f);
	[Export] public Color BoardBasinAccentColor =
		new Color(0.20f, 0.24f, 0.11f);
	[Export] public Color BoardBasinSideColor =
		new Color(0.12f, 0.075f, 0.035f);

	[Export] public Vector3 Side1StoneModelOffset =
		new Vector3(-13.20f, 0.0f, -4.17f);

	[Export] public Vector3 Side2StoneModelOffset =
		new Vector3(-14.41f, 0.0f, -2.17f);

	[Export] public Vector3 CornerStoneModelOffset =
		new Vector3(-14.05f, 0.07f, -6.15f);

	[Export(PropertyHint.Range, "-180.0,180.0,1.0")]
	public float CornerStoneRotationOffsetDegrees = 0.0f;

	[ExportGroup("Decorative Outer Ring")]
	[Export] public bool ShowDecorativeOuterRing = true;

	[Export(PropertyHint.Range, "1,8,1")]
	public int WaterGapRings = 4;

	[Export(PropertyHint.Range, "1,8,1")]
	public int DecorativeGroundRows = 5;

	[Export(PropertyHint.Range, "-0.5,0.2,0.01")]
	public float DecorativeGroundHeight = -0.04f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float OuterGrassDensity = 0.72f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float OuterGrassShoreMargin = 0.34f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float OuterGrassDroughtDryAmount = 0.32f;

	[Export] public bool ShowDecorativeShoreStones = true;

	[ExportGroup("Decorative Cliff")]
	[Export] public bool ShowDecorativeCliff = true;

	[Export(PropertyHint.Enum,
		"Right,Lower Right,Lower Left,Left,Upper Left,Upper Right")]
	public int DecorativeCliffSide = 2;

	[Export(PropertyHint.Range, "10,30,1")]
	public int DecorativeCliffWidth = 10;

	[Export(PropertyHint.Range, "0.2,2.0,0.05")]
	public float DecorativeCliffHeight = 0.8f;

	[ExportGroup("Decorative Outer Vegetation")]
	[Export] public bool ShowDecorativeOuterVegetation = true;
	[Export] public Godot.Collections.Array<PackedScene> OuterTreeScenes = new();
	[Export] public Godot.Collections.Array<PackedScene> OuterDetailScenes = new();

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float OuterTreeChance = 0.24f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float OuterDetailChance = 0.42f;

	[Export(PropertyHint.Range, "0,4,1")]
	public int OuterVegetationShoreClearRows = 1;

	[Export(PropertyHint.Range, "0.0,0.8,0.01")]
	public float OuterVegetationPositionRadius = 0.52f;

	[Export(PropertyHint.Range, "-0.5,0.5,0.01")]
	public float OuterVegetationHeightOffset = 0.02f;

	[Export(PropertyHint.Range, "0.05,1.5,0.01")]
	public float OuterTreeMinimumScale = 0.50f;

	[Export(PropertyHint.Range, "0.05,1.5,0.01")]
	public float OuterTreeMaximumScale = 0.68f;

	[Export(PropertyHint.Range, "0.05,1.5,0.01")]
	public float OuterDetailMinimumScale = 0.55f;

	[Export(PropertyHint.Range, "0.05,1.5,0.01")]
	public float OuterDetailMaximumScale = 0.90f;

	[Export(PropertyHint.Range, "0.1,1.0,0.01")]
	public float OuterFlowerScaleMultiplier = 0.55f;

	[Export] public int OuterVegetationRandomSeed = 62831;

	[ExportGroup("Render Visibility Ranges")]
	[Export] public bool EnableVisibilityRanges = true;

	[Export(PropertyHint.Range, "0.0,80.0,1.0")]
	public float GrassVisibilityRange = 0.0f;

	[Export(PropertyHint.Range, "0.0,80.0,1.0")]
	public float VegetationVisibilityRange = 0.0f;

	[Export(PropertyHint.Range, "0.0,12.0,0.5")]
	public float VisibilityRangeMargin = 4.0f;

	[Export(PropertyHint.Range, "0.0,8.0,0.5")]
	public float FrustumCullMargin = 2.0f;

	[ExportGroup("Starting Oak Visual")]
	[Export(PropertyHint.Range, "0.05,2.0,0.01")]
	public float StartingOakScale = 0.25f;

	[ExportGroup("Dead Plant Visuals")]
	[Export(PropertyHint.Range, "0.1,1.0,0.05")]
	public float DeadPlantScale = 0.6f;

	[Export] public Color DeadPlantTint = new Color(0.32f, 0.27f, 0.20f);
	[Export] public Color BlockedTileTint = new Color(0.38f, 0.40f, 0.38f);
	[Export] public Color BlockedPreviewTint = new Color(0.48f, 0.50f, 0.48f);

	[ExportGroup("Mushroom Visual")]
	[Export(PropertyHint.Range, "0.1,2.0,0.05")]
	public float MushroomModelScale = 0.32f;

	[Export(PropertyHint.Range, "0.1,3.0,0.1")]
	public float MushroomGrowthAnimationSpeed = 1.0f;

	[ExportGroup("Flower Visual")]
	[Export(PropertyHint.Range, "0.01,1.0,0.01")]
	public float FlowerModelScale = 0.38f;

	[Export(PropertyHint.Range, "1,7,1")]
	public int MatureFlowerCount = 4;

	[ExportGroup("Birch Visual")]
	[Export(PropertyHint.Range, "0.01,1.0,0.01")]
	public float BirchModelScale = 0.18f;

	[ExportGroup("Tree Shadow Visual")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float TreeShadowStrength = 0.55f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float YoungTreeCanopyShadowStrength = 0.25f;

	[Export] public Color TreeShadowColor =
		new Color(0.08f, 0.12f, 0.06f);

	[Export(PropertyHint.Range, "1.0,7.0,0.1")]
	public float StartingOakShadowSize = 6.2f;

	[Export] public Vector2 StartingOakShadowOffset =
		Vector2.Zero;

	[Export(PropertyHint.Range, "0.8,4.0,0.1")]
	public float BirchShadowSize = 2.8f;

	[Export] public Vector2 BirchShadowOffset =
		new Vector2(0.0f, 0.18f);

	[Export(PropertyHint.Range, "0.0,5.0,0.05")]
	public float TreeShadowFadeDuration = 1.25f;

	[Export] public Color CanopyShadowFieldTint =
		new Color(0.56f, 0.64f, 0.52f);

	[ExportGroup("Tree Camera Fade")]
	[Export] public bool EnableTreeProximityFade = true;

	[Export(PropertyHint.Range, "0.5,8.0,0.1")]
	public float TreeFadeStartDistance = 3.0f;

	[Export(PropertyHint.Range, "0.0,4.0,0.1")]
	public float TreeFadeFullDistance = 0.6f;

	[Export(PropertyHint.Range, "0.0,0.8,0.01")]
	public float TreeFadeMaximumTransparency = 0.8f;

	[Export(PropertyHint.Range, "0.1,10.0,0.1")]
	public float TreeFadeSpeed = 1.2f;

	[ExportGroup("Light Level Visuals")]
	[Export] public Color SunTileTint = Colors.White;
	[Export] public Color PartialShadeTileTint =
		new Color(0.82f, 0.91f, 0.80f);
	[Export] public Color ShadeTileTint =
		new Color(0.62f, 0.74f, 0.64f);

	public BoardData BoardData { get; private set; } = new BoardData();

	private sealed class StoneMeshTemplate
	{
		public Mesh Mesh;
		public Transform3D LocalTransform;
		public GeometryInstance3D.ShadowCastingSetting CastShadow;
		public uint Layers;
		public Material MaterialOverride;
		public Material MaterialOverlay;
	}

	private sealed class StoneCollisionTemplate
	{
		public Shape3D Shape;
		public Transform3D LocalTransform;
		public uint CollisionLayer;
		public uint CollisionMask;
		public bool Disabled;
	}

	private sealed class StoneSceneBatch
	{
		public StoneSceneBatch(Transform3D rootTransform)
		{
			RootTransform = rootTransform;
		}

		public Transform3D RootTransform { get; }
		public readonly List<StoneMeshTemplate> Meshes = new();
		public readonly List<StoneCollisionTemplate> Collisions = new();
		public readonly List<Transform3D> Instances = new();
	}

	private sealed class DecorativeGroundBatch
	{
		public Mesh Mesh;
		public GeometryInstance3D.ShadowCastingSetting CastShadow;
		public uint Layers;
		public Material MaterialOverride;
		public Material MaterialOverlay;
		public readonly List<Transform3D> Instances = new();
	}

	private sealed class DecorativeGrassBatch
	{
		public Mesh Mesh;
		public GeometryInstance3D.ShadowCastingSetting CastShadow;
		public uint Layers;
		public Material MaterialOverride;
		public Material MaterialOverlay;
		public readonly List<Transform3D> Instances = new();
	}

	private sealed class DecorativeTileTemplate
	{
		public Node3D Tile;
		public bool UsesGeneratedGrass;
	}

	private sealed class OuterVegetationBatch
	{
		public bool CanBatch = true;
		public OuterRingVisualGroup VisualGroup;
		public readonly List<StoneMeshTemplate> Meshes = new();
		public readonly List<Transform3D> Instances = new();
	}

	private readonly Dictionary<HexCoord, HexTile> _tileViews = new();
	private readonly Dictionary<PackedScene, StoneSceneBatch> _stoneSceneBatches = new();
	private readonly Dictionary<(PackedScene Scene, int Sector), StoneSceneBatch>
		_decorativeDetailBatches = new();
	private readonly Dictionary<PackedScene, DecorativeGroundBatch>
		_decorativeGroundBatches = new();
	private readonly Dictionary<(PackedScene Scene, int Sector), DecorativeGrassBatch>
		_decorativeGrassBatches = new();
	private readonly Dictionary<(PackedScene Scene, int Sector), OuterVegetationBatch>
		_outerVegetationBatches = new();
	private readonly List<MultiMeshInstance3D> _decorativeGrassInstances = new();
	private readonly Dictionary<Node3D, OuterRingVisualGroup>
		_outerRingVisualNodes = new();
	private readonly List<PackedScene> _activeHexTileVariants = new();
	private readonly List<PackedScene> _activeOuterTreeScenes = new();
	private readonly List<PackedScene> _activeOuterDetailScenes = new();
	private readonly List<PackedScene> _activeBorderRockScenes = new();
	private StylizedBridgeController _stylizedBridge;
	private Vector3 _boardWorldCenter = Vector3.Zero;
	private float _decorativeGrassDryAmount;

	public override void _Ready()
	{
		ulong totalStartedUsec = LoadProfiler.BeginPhase(
			"BoardManager._Ready gesamt");
		ulong phaseStartedUsec = LoadProfiler.BeginPhase(
			"Board-Konfiguration laden");
		Balance ??= GameConfig.LoadDefault();
		LoadProfiler.EndPhase("Board-Konfiguration laden", phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Hexfeldvarianten vorbereiten");
		SetupHexTileVariants();
		LoadProfiler.EndPhase("Hexfeldvarianten vorbereiten", phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Uferstein-Szenen vorbereiten");
		SetupStoneBorderScenes();
		LoadProfiler.EndPhase("Uferstein-Szenen vorbereiten", phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Außenring-Vegetationsszenen vorbereiten");
		SetupOuterVegetationScenes();
		LoadProfiler.EndPhase(
			"Außenring-Vegetationsszenen vorbereiten",
			phaseStartedUsec);

		GenerateBoard();
		LoadProfiler.EndPhase("BoardManager._Ready gesamt", totalStartedUsec);
	}

	private void SetupHexTileVariants()
	{
		_activeHexTileVariants.Clear();

		if (HexTileVariants != null)
		{
			foreach (PackedScene variant in HexTileVariants)
			{
				if (variant != null)
					_activeHexTileVariants.Add(variant);
			}
		}

		if (_activeHexTileVariants.Count == 0 && HexTileScene != null)
			_activeHexTileVariants.Add(HexTileScene);

		if (_activeHexTileVariants.Count > 0)
			return;

		foreach (string path in DefaultHexTileVariantPaths)
		{
			PackedScene variant = GD.Load<PackedScene>(path);

			if (variant != null)
				_activeHexTileVariants.Add(variant);
		}
	}

	private void SetupStoneBorderScenes()
	{
		Side1StoneScene ??= GD.Load<PackedScene>(DefaultSide1StonePath);
		Side2StoneScene ??= GD.Load<PackedScene>(DefaultSide2StonePath);
		CornerStoneScene ??= GD.Load<PackedScene>(DefaultCornerStonePath);
		SetupSceneList(
			BorderRockScenes,
			DefaultBorderRockScenePaths,
			_activeBorderRockScenes);
	}

	private void SetupOuterVegetationScenes()
	{
		SetupSceneList(
			OuterTreeScenes,
			DefaultOuterTreeScenePaths,
			_activeOuterTreeScenes);
		SetupSceneList(
			OuterDetailScenes,
			DefaultOuterDetailScenePaths,
			_activeOuterDetailScenes);
	}

	private static void SetupSceneList(
		Godot.Collections.Array<PackedScene> configuredScenes,
		string[] defaultPaths,
		List<PackedScene> targetScenes)
	{
		targetScenes.Clear();

		if (configuredScenes != null)
		{
			foreach (PackedScene scene in configuredScenes)
			{
				if (scene != null)
					targetScenes.Add(scene);
			}
		}

		if (targetScenes.Count > 0)
			return;

		foreach (string path in defaultPaths)
		{
			PackedScene scene = GD.Load<PackedScene>(path);
			if (scene != null)
				targetScenes.Add(scene);
			else
				GD.PushWarning($"BoardManager: Dekorationsszene fehlt: {path}");
		}
	}

	public void GenerateBoard()
	{
		ulong totalStartedUsec = LoadProfiler.BeginPhase(
			"BoardManager.GenerateBoard gesamt");
		ulong phaseStartedUsec = LoadProfiler.BeginPhase(
			"Vorhandenes Board zurücksetzen");
		_stylizedBridge = FindStylizedBridgeController();
		ClearBoard();
		BoardData = new BoardData();
		LoadProfiler.EndPhase(
			"Vorhandenes Board zurücksetzen",
			phaseStartedUsec);

		GameConfig balance = Balance ?? GameConfig.LoadDefault();

		phaseStartedUsec = LoadProfiler.BeginPhase("Board-Daten erzeugen");
		if (balance.UseRectangularBoard)
		{
			BoardData.GenerateRectangle(
				balance.BoardColumns,
				balance.BoardRows);
		}
		else
		{
			BoardData.Generate(
				balance.BoardRadius,
				new HexCoord(0, 0));
		}
		LoadProfiler.EndPhase("Board-Daten erzeugen", phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Natürliche Board-Basis erzeugen");
		UpdateBoardWorldCenter();
		CreateNaturalBoardBasin();
		LoadProfiler.EndPhase(
			"Natürliche Board-Basis erzeugen",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Spielbare Hexfelder erzeugen");
		foreach (HexTileData tileData in BoardData.Tiles.Values)
		{
			CreateTileView(tileData);
		}
		LoadProfiler.EndPhase(
			"Spielbare Hexfelder erzeugen",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Schattenempfänger aktualisieren");
		UpdateCanopyShadowReceivers();
		LoadProfiler.EndPhase(
			"Schattenempfänger aktualisieren",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Außenring-Inhalte verteilen");
		CreateDecorativeOuterRing(balance);
		LoadProfiler.EndPhase(
			"Außenring-Inhalte verteilen",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Außenring-Boden-MultiMeshes bauen");
		BuildDecorativeGroundMultiMeshes();
		LoadProfiler.EndPhase(
			"Außenring-Boden-MultiMeshes bauen",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Außenring-Gras-MultiMeshes bauen");
		BuildDecorativeGrassMultiMeshes();
		LoadProfiler.EndPhase(
			"Außenring-Gras-MultiMeshes bauen",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Außenring-Detail-MultiMeshes bauen");
		BuildDecorativeDetailMultiMeshes();
		LoadProfiler.EndPhase(
			"Außenring-Detail-MultiMeshes bauen",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Außenring-Vegetations-MultiMeshes bauen");
		BuildOuterVegetationMultiMeshes();
		LoadProfiler.EndPhase(
			"Außenring-Vegetations-MultiMeshes bauen",
			phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Ufersteine verteilen");
		CreateStoneBorder(balance);
		LoadProfiler.EndPhase("Ufersteine verteilen", phaseStartedUsec);

		phaseStartedUsec = LoadProfiler.BeginPhase(
			"Uferstein-MultiMeshes bauen");
		BuildStoneBorderMultiMeshes();
		LoadProfiler.EndPhase(
			"Uferstein-MultiMeshes bauen",
			phaseStartedUsec);

		GD.Print($"Board generated with {BoardData.Tiles.Count} tiles.");
		LoadProfiler.EndPhase(
			"BoardManager.GenerateBoard gesamt",
			totalStartedUsec);
	}

	private StylizedBridgeController FindStylizedBridgeController()
	{
		Node parent = GetParent();
		if (parent == null)
			return null;

		foreach (Node child in parent.GetChildren())
		{
			if (child is StylizedBridgeController bridge)
				return bridge;
		}

		return null;
	}

	private bool IsInsideBridgeLanding(
		Vector3 boardLocalPosition,
		float additionalClearance = 0.0f)
	{
		return _stylizedBridge != null &&
			_stylizedBridge.ContainsLandingFootprint(
				this,
				boardLocalPosition,
				additionalClearance);
	}

	private void CreateNaturalBoardBasin()
	{
		if (!ShowNaturalBoardBasin || BoardData.Tiles.Count == 0)
			return;

		SurfaceTool surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		foreach (HexCoord coord in BoardData.Tiles.Keys)
		{
			Vector3 center = HexToWorld(coord, HexSize);
			uint tileHash = GetTileVisualHash(coord);
			center.Y = BoardBasinTopHeight + Mathf.Lerp(
				-BoardBasinSurfaceVariation,
				BoardBasinSurfaceVariation,
				HashToUnitFloat(tileHash ^ 0x6D2B79F5u));

			for (int cornerIndex = 0; cornerIndex < 6; cornerIndex++)
			{
				Vector3 corner = GetBoardBasinCorner(coord, cornerIndex);
				Vector3 nextCorner = GetBoardBasinCorner(
					coord,
					(cornerIndex + 1) % 6);
				AddBoardBasinTriangle(
					surface,
					center,
					nextCorner,
					corner,
					GetBoardBasinTopColor(center, tileHash),
					GetBoardBasinTopColor(nextCorner, 0xA24BAED5u),
					GetBoardBasinTopColor(corner, 0xA24BAED5u));
			}

			CreateBoardBasinWalls(surface, coord);
		}

		surface.GenerateNormals();
		ArrayMesh basinMesh = surface.Commit();

		if (basinMesh == null)
			return;

		StandardMaterial3D basinMaterial = new StandardMaterial3D
		{
			AlbedoColor = Colors.White,
			VertexColorUseAsAlbedo = true,
			Roughness = 0.96f,
			Metallic = 0.0f,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};
		basinMesh.SurfaceSetMaterial(0, basinMaterial);

		MeshInstance3D basin = new MeshInstance3D
		{
			Name = "NaturalBoardBasin",
			Mesh = basinMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
			ExtraCullMargin = 0.5f
		};
		AddChild(basin);
	}

	private void CreateBoardBasinWalls(
		SurfaceTool surface,
		HexCoord coord)
	{
		Vector3 tileCenter = HexToWorld(coord, HexSize);
		int segmentCount = System.Math.Max(BoardBasinWallSegments, 1);

		for (int directionIndex = 0;
			directionIndex < HexDirections.Directions.Length;
			directionIndex++)
		{
			HexCoord neighborCoord = HexDirections.GetNeighbor(
				coord,
				directionIndex);

			if (BoardData.GetTile(neighborCoord) != null)
				continue;

			Vector3 centerToNeighbor =
				HexToWorld(neighborCoord, HexSize) - tileCenter;
			Vector3 outward = centerToNeighbor.Normalized();
			Vector3 tangent = new Vector3(-outward.Z, 0.0f, outward.X);
			Vector3 edgeCenter = tileCenter + centerToNeighbor * 0.5f;
			Vector3 edgeStart = edgeCenter - tangent * HexSize * 0.5f;
			Vector3 edgeEnd = edgeCenter + tangent * HexSize * 0.5f;
			edgeStart.Y = GetBoardBasinSurfaceHeight(edgeStart);
			edgeEnd.Y = GetBoardBasinSurfaceHeight(edgeEnd);

			for (int segmentIndex = 0;
				segmentIndex < segmentCount;
				segmentIndex++)
			{
				float startWeight = segmentIndex / (float)segmentCount;
				float endWeight = (segmentIndex + 1) / (float)segmentCount;
				Vector3 topStart = edgeStart.Lerp(edgeEnd, startWeight);
				Vector3 topEnd = edgeStart.Lerp(edgeEnd, endWeight);
				Vector3 bottomStart = GetBoardBasinBottomPoint(topStart);
				Vector3 bottomEnd = GetBoardBasinBottomPoint(topEnd);
				Color topStartColor = GetBoardBasinTopColor(
					topStart,
					0x165667B1u);
				Color topEndColor = GetBoardBasinTopColor(
					topEnd,
					0x165667B1u);
				Color bottomStartColor = GetBoardBasinSideColor(
					bottomStart,
					0xD3A2646Cu);
				Color bottomEndColor = GetBoardBasinSideColor(
					bottomEnd,
					0xD3A2646Cu);

				AddBoardBasinTriangle(
					surface,
					topStart,
					bottomEnd,
					bottomStart,
					topStartColor,
					bottomEndColor,
					bottomStartColor);
				AddBoardBasinTriangle(
					surface,
					topStart,
					topEnd,
					bottomEnd,
					topStartColor,
					topEndColor,
					bottomEndColor);
			}
		}
	}

	private Vector3 GetBoardBasinCorner(HexCoord coord, int cornerIndex)
	{
		Vector3 center = HexToWorld(coord, HexSize);
		float angle = cornerIndex * Mathf.Pi / 3.0f;
		Vector3 corner = center + new Vector3(
			Mathf.Cos(angle) * HexSize,
			0.0f,
			Mathf.Sin(angle) * HexSize);
		corner.Y = GetBoardBasinSurfaceHeight(corner);
		return corner;
	}

	private float GetBoardBasinSurfaceHeight(Vector3 position)
	{
		float variation = Mathf.Lerp(
			-BoardBasinSurfaceVariation,
			BoardBasinSurfaceVariation,
			GetBoardBasinPositionNoise(position, 0x9E3779B9u));
		return BoardBasinTopHeight + variation;
	}

	private Vector3 GetBoardBasinBottomPoint(Vector3 topPoint)
	{
		float shapeNoise = GetBoardBasinPositionNoise(
			topPoint,
			0xC2B2AE35u);
		float depthNoise = GetBoardBasinPositionNoise(
			topPoint,
			0x27D4EB2Fu);
		Vector3 inward = new Vector3(-topPoint.X, 0.0f, -topPoint.Z);

		if (!inward.IsZeroApprox())
			inward = inward.Normalized();

		Vector3 bottomPoint = topPoint + inward *
			BoardBasinWallInset * HexSize *
			Mathf.Lerp(0.72f, 1.28f, shapeNoise);
		bottomPoint.Y = BoardBasinTopHeight - BoardBasinDepth *
			Mathf.Lerp(0.84f, 1.16f, depthNoise);
		return bottomPoint;
	}

	private Color GetBoardBasinTopColor(Vector3 position, uint salt)
	{
		float colorNoise = GetBoardBasinPositionNoise(position, salt);
		return BoardBasinTopColor.Lerp(
			BoardBasinAccentColor,
			Mathf.Lerp(0.08f, 0.48f, colorNoise));
	}

	private Color GetBoardBasinSideColor(Vector3 position, uint salt)
	{
		float colorNoise = GetBoardBasinPositionNoise(position, salt);
		return BoardBasinSideColor.Lerp(
			BoardBasinTopColor,
			Mathf.Lerp(0.04f, 0.20f, colorNoise));
	}

	private static float GetBoardBasinPositionNoise(
		Vector3 position,
		uint salt)
	{
		unchecked
		{
			uint x = (uint)Mathf.RoundToInt(position.X * 1000.0f);
			uint z = (uint)Mathf.RoundToInt(position.Z * 1000.0f);
			uint hash = salt ^ x * 0x9E3779B1u ^ z * 0x85EBCA77u;
			hash ^= hash >> 16;
			hash *= 0x7FEB352Du;
			hash ^= hash >> 15;
			return HashToUnitFloat(hash);
		}
	}

	private static float HashToUnitFloat(uint hash)
	{
		return (hash & 0xFFFFu) / 65535.0f;
	}

	private static void AddBoardBasinTriangle(
		SurfaceTool surface,
		Vector3 first,
		Vector3 second,
		Vector3 third,
		Color firstColor,
		Color secondColor,
		Color thirdColor)
	{
		surface.SetColor(firstColor);
		surface.AddVertex(first);
		surface.SetColor(secondColor);
		surface.AddVertex(second);
		surface.SetColor(thirdColor);
		surface.AddVertex(third);
	}

	public HexTileData GetTileData(HexCoord coord)
	{
		return BoardData.GetTile(coord);
	}

	public HexTile GetTileView(HexCoord coord)
	{
		if (_tileViews.TryGetValue(coord, out HexTile tileView))
			return tileView;

		return null;
	}

	public List<HexTileData> GetNeighborData(HexCoord coord)
	{
		return BoardData.GetNeighbors(coord);
	}

	public List<HexTileData> GetFreeNeighborTiles(HexCoord coord)
	{
		List<HexTileData> result = new();
		List<HexTileData> neighbors = BoardData.GetNeighbors(coord);

		foreach (HexTileData neighbor in neighbors)
		{
			if (!neighbor.IsOccupied && !neighbor.IsBlocked)
			{
				result.Add(neighbor);
			}
		}

		return result;
	}

	public void RecalculateLightLevels()
	{
		foreach (HexTileData tile in BoardData.Tiles.Values)
		{
			tile.LightLevel = LightLevel.Sun;
		}

		foreach (HexTileData tile in BoardData.Tiles.Values)
		{
			if (tile.Plant == null)
				continue;

			if (!tile.Plant.Definition.CanProduceShade(tile.Plant.IsMature))
				continue;

			tile.LightLevel = LightLevel.Shade;

			List<HexTileData> neighbors = BoardData.GetNeighbors(tile.Coord);

			foreach (HexTileData neighbor in neighbors)
			{
				if (neighbor.LightLevel == LightLevel.Sun)
				{
					neighbor.LightLevel = LightLevel.PartialShade;
				}
			}
		}

		UpdateCanopyShadowReceivers();
		UpdateAllTileViews();
	}

	private void UpdateCanopyShadowReceivers()
	{
		Dictionary<HexCoord, float> receiverAmounts = new();

		foreach (HexTileData tile in BoardData.Tiles.Values)
		{
			PlantInstance plant = tile.Plant;

			if (plant == null ||
				(plant.Definition.Type != PlantType.Oak &&
				plant.Definition.Type != PlantType.Birch))
			{
				continue;
			}

			if (!ProducesCanopyShadow(tile))
				continue;

			bool isStartingOak =
				plant.Definition.Type == PlantType.Oak &&
				tile.Coord.Q == 0 &&
				tile.Coord.R == 0;
			float growthProgress = Mathf.Clamp(
				plant.GrowthProgress,
				0.0f,
				1.0f);
			float centerAmount = isStartingOak
				? 1.0f
				: Mathf.Lerp(
					Mathf.Clamp(YoungTreeCanopyShadowStrength, 0.0f, 1.0f),
					1.0f,
					growthProgress);
			float neighborAmount = isStartingOak
				? 1.0f
				: growthProgress * growthProgress;

			SetMaximumCanopyShadowAmount(
				receiverAmounts,
				tile.Coord,
				centerAmount);

			foreach (HexTileData neighbor in BoardData.GetNeighbors(tile.Coord))
			{
				SetMaximumCanopyShadowAmount(
					receiverAmounts,
					neighbor.Coord,
					neighborAmount);
			}
		}

		foreach (KeyValuePair<HexCoord, HexTile> entry in _tileViews)
		{
			receiverAmounts.TryGetValue(entry.Key, out float shadowAmount);
			entry.Value.ConfigureCanopyShadowReceiver(shadowAmount);
		}
	}

	private static void SetMaximumCanopyShadowAmount(
		Dictionary<HexCoord, float> receiverAmounts,
		HexCoord coord,
		float amount)
	{
		float clampedAmount = Mathf.Clamp(amount, 0.0f, 1.0f);
		if (clampedAmount <= 0.0f)
			return;

		if (!receiverAmounts.TryGetValue(coord, out float currentAmount) ||
			clampedAmount > currentAmount)
		{
			receiverAmounts[coord] = clampedAmount;
		}
	}

	private static bool ProducesCanopyShadow(HexTileData tile)
	{
		PlantInstance plant = tile?.Plant;

		if (plant == null)
			return false;

		PlantType plantType = plant.Definition.Type;
		bool isTree =
			plantType == PlantType.Oak ||
			plantType == PlantType.Birch;

		if (!isTree)
			return false;

		return true;
	}

	public void UpdateAllTileViews()
	{
		foreach (HexTile tileView in _tileViews.Values)
		{
			tileView.UpdateLightVisualState();
		}
	}

	public void SetRenderGroupVisibility(
		bool grassVisible,
		bool tileModelsVisible,
		bool plantsVisible,
		bool stoneBorderVisible,
		bool outerRingVisible)
	{
		foreach (HexTile tileView in _tileViews.Values)
		{
			tileView.SetRenderGroupVisibility(
				grassVisible,
				tileModelsVisible,
				plantsVisible);
		}

		foreach (Node child in GetChildren())
		{
			string childName = child.Name.ToString();

			if (childName.StartsWith(
				"StoneBorder_",
				System.StringComparison.Ordinal))
			{
				if (child is Node3D stoneBorder)
					stoneBorder.Visible = stoneBorderVisible;

				continue;
			}

			if (childName.StartsWith(
				"DecorativeTile_",
				System.StringComparison.Ordinal) &&
				child is Node3D decorativeTile)
			{
				decorativeTile.Visible = outerRingVisible;
			}
		}
	}

	internal void SetOuterRingDetailVisibility(
		bool outerRingVisible,
		OuterRingVisualGroup visibleGroups)
	{
		foreach (KeyValuePair<Node3D, OuterRingVisualGroup> entry in
			_outerRingVisualNodes)
		{
			Node3D visual = entry.Key;
			if (visual == null || !GodotObject.IsInstanceValid(visual))
				continue;

			visual.Visible = outerRingVisible &&
				(visibleGroups & entry.Value) != OuterRingVisualGroup.None;
		}
	}

	private void CreateTileView(HexTileData tileData)
	{
		if (_activeHexTileVariants.Count == 0)
		{
			GD.PrintErr("Keine HexTile-Variante konfiguriert.");
			return;
		}

		uint visualHash = GetTileVisualHash(tileData.Coord);
		int variantIndex = (int)(visualHash % (uint)_activeHexTileVariants.Count);
		PackedScene tileScene = _activeHexTileVariants[variantIndex];
		Node tileInstance = tileScene.Instantiate();

		if (tileInstance is not HexTile tileView)
		{
			GD.PrintErr($"{tileScene.ResourcePath}: Der Root-Node muss HexTile verwenden.");
			tileInstance.Free();
			return;
		}

		Vector3 tilePosition = HexToWorld(tileData.Coord, HexSize);
		tileView.Position = tilePosition;
		int rotationStep = (int)((visualHash / (uint)_activeHexTileVariants.Count) % 6u);
		tileView.Rotation = new Vector3(0.0f, rotationStep * Mathf.Pi / 3.0f, 0.0f);
		tileView.ConfigureTileVisualScale(HexSize);
		tileView.ConfigureStartingOakScale(StartingOakScale);
		tileView.ConfigureDeadPlantVisuals(
			DeadPlantScale,
			DeadPlantTint,
			BlockedTileTint,
			BlockedPreviewTint);
		tileView.ConfigureMushroomVisual(
			MushroomModelScale,
			MushroomGrowthAnimationSpeed);
		tileView.ConfigureFlowerVisual(
			FlowerModelScale,
			MatureFlowerCount);
		tileView.ConfigureBirchVisual(BirchModelScale);
		tileView.ConfigureTreeShadowVisual(
			TreeShadowStrength,
			TreeShadowColor,
			CanopyShadowFieldTint,
			StartingOakShadowSize,
			StartingOakShadowOffset,
			BirchShadowSize,
			BirchShadowOffset,
			TreeShadowFadeDuration);
		tileView.ConfigureTreeProximityFade(
			EnableTreeProximityFade,
			TreeFadeStartDistance,
			TreeFadeFullDistance,
			TreeFadeMaximumTransparency,
			TreeFadeSpeed);
		tileView.ConfigureVisibilityRanges(
			EnableVisibilityRanges,
			GrassVisibilityRange,
			VegetationVisibilityRange,
			VisibilityRangeMargin,
			FrustumCullMargin);
		tileView.ConfigureLightVisuals(
			SunTileTint,
			PartialShadeTileTint,
			ShadeTileTint);
		tileView.ConfigureGrassVisual(
			GrassBaseDensity,
			GrassInstancesPerTile,
			GrassWindWaveSpeed,
			GrassWindWaveStrength,
			GrassWindDetailSpeed,
			GrassWindDetailStrength,
			ToGlobal(tilePosition),
			HexSize * Mathf.Sqrt(3.0f) * 0.5f,
			GrassOuterMargin,
			GrassStoneMargin,
			GrassOakMargin,
			GrassBirchMargin,
			GrassMushroomMargin,
			GrassMossMargin,
			GetGrassBorderDirections(tileData.Coord),
			GetGrassOuterEdges(tileData.Coord));
		AddChild(tileView);
		tileView.Setup(tileData);

		_tileViews.Add(tileData.Coord, tileView);
	}

	private Vector2[] GetGrassBorderDirections(HexCoord coord)
	{
		Vector2[] borderDirections = new Vector2[HexDirections.Directions.Length];
		Vector3 tilePosition = GetRawHexPosition(coord, HexSize);

		for (int directionIndex = 0;
			directionIndex < HexDirections.Directions.Length;
			directionIndex++)
		{
			HexCoord neighborCoord = HexDirections.GetNeighbor(coord, directionIndex);
			Vector3 neighborPosition = GetRawHexPosition(neighborCoord, HexSize);
			Vector3 worldDirection = GlobalTransform.Basis *
				(neighborPosition - tilePosition);
			worldDirection.Y = 0.0f;
			worldDirection = worldDirection.Normalized();

			borderDirections[directionIndex] =
				new Vector2(worldDirection.X, worldDirection.Z);
		}

		return borderDirections;
	}

	private float[] GetGrassOuterEdges(HexCoord coord)
	{
		float[] outerEdges = new float[HexDirections.Directions.Length];

		for (int directionIndex = 0;
			directionIndex < HexDirections.Directions.Length;
			directionIndex++)
		{
			HexCoord neighborCoord = HexDirections.GetNeighbor(coord, directionIndex);
			outerEdges[directionIndex] = BoardData.GetTile(neighborCoord) == null
				? 1.0f
				: 0.0f;
		}

		return outerEdges;
	}

	private void CreateStoneBorder(GameConfig balance)
	{
		if (!ShowStoneBorder)
			return;

		if (_activeBorderRockScenes.Count > 0)
		{
			CreateVariantRockBorder();
			return;
		}

		if (!balance.UseRectangularBoard)
		{
			CreateRoundStoneBorder();
			return;
		}

		int columns = System.Math.Max(balance.BoardColumns, 1);
		int rows = System.Math.Max(balance.BoardRows, 1);
		int firstQ = -(columns / 2);

		for (int column = 0; column < columns; column++)
		{
			int q = firstQ + column;
			int columnOffset = (int)System.Math.Floor(q / 2.0);

			for (int row = 0; row < rows; row++)
			{
				bool isLeftSide = column == 0;
				bool isRightSide = column == columns - 1;
				bool isBack = row == 0;
				bool isFront = row == rows - 1;
				bool isCorner =
					(isLeftSide || isRightSide) && (isBack || isFront);

				if (!isLeftSide && !isRightSide && !isBack && !isFront)
					continue;

				HexCoord coord = new HexCoord(q, row - columnOffset);
				HexTile tileView = GetTileView(coord);

				if (tileView == null)
					continue;

				PackedScene stoneScene;
				Vector3 modelOffset;
				float rotationY;
				string edgeName;

				if (isCorner)
				{
					stoneScene = CornerStoneScene;
					modelOffset = CornerStoneModelOffset;
					rotationY = GetCornerStoneRotation(
						isLeftSide,
						isFront);
					edgeName = "Corner";
				}
				else if (isLeftSide)
				{
					stoneScene = Side2StoneScene;
					modelOffset = Side2StoneModelOffset;
					rotationY = Mathf.Pi;
					edgeName = "Left";
				}
				else if (isRightSide)
				{
					stoneScene = Side2StoneScene;
					modelOffset = Side2StoneModelOffset;
					rotationY = 0.0f;
					edgeName = "Right";
				}
				else
				{
					bool useSide2 = isFront
						? column % 2 != 0
						: column % 2 == 0;
					stoneScene = useSide2 ? Side2StoneScene : Side1StoneScene;
					modelOffset = useSide2
						? Side2StoneModelOffset
						: Side1StoneModelOffset;
					rotationY = isFront
						? -Mathf.Pi / 2.0f
						: Mathf.Pi / 2.0f;
					edgeName = isFront ? "Front" : "Back";
				}

				CreateStoneBorderPiece(
					stoneScene,
					tileView.Position,
					tileView.Coord,
					modelOffset,
					rotationY,
					edgeName);
			}
		}
	}

	private void CreateVariantRockBorder()
	{
		foreach (KeyValuePair<HexCoord, HexTile> tileEntry in _tileViews)
		{
			HexCoord coord = tileEntry.Key;
			HexTile tileView = tileEntry.Value;
			Vector3 tilePosition = GetRawHexPosition(coord, HexSize);

			for (int directionIndex = 0;
				directionIndex < HexDirections.Directions.Length;
				directionIndex++)
			{
				HexCoord neighborCoord = HexDirections.GetNeighbor(
					coord,
					directionIndex);

				if (BoardData.GetTile(neighborCoord) != null)
					continue;

				Vector3 neighborPosition = GetRawHexPosition(
					neighborCoord,
					HexSize);
				CreateVariantRockEdge(
					tileView.Position,
					neighborPosition - tilePosition,
					coord,
					directionIndex,
					$"Rock{directionIndex}");
			}
		}
	}

	private void CreateVariantRockEdge(
		Vector3 tilePosition,
		Vector3 centerToNeighbor,
		HexCoord coord,
		int directionIndex,
		string edgeName,
		float scaleMultiplier = 1.0f,
		uint variationSalt = 0u)
	{
		if (_activeBorderRockScenes.Count == 0 || centerToNeighbor.IsZeroApprox())
			return;

		Vector3 outwardDirection = centerToNeighbor.Normalized();
		Vector3 tangentDirection = new Vector3(
			-outwardDirection.Z,
			0.0f,
			outwardDirection.X);
		uint edgeHash = GetTileVisualHash(coord) ^
			((uint)(directionIndex + 1) * 0x9E3779B9u) ^
			variationSalt;
		float rotationY = -Mathf.Atan2(
			tangentDirection.Z,
			tangentDirection.X);
		int rocksPerEdge = System.Math.Max(BorderRocksPerEdge, 1);
		float rockSpacing = HexSize / rocksPerEdge;

		for (int rockIndex = 0; rockIndex < rocksPerEdge; rockIndex++)
		{
			uint hash = edgeHash ^
				((uint)(rockIndex + 1) * 0x85EBCA6Bu);
			PackedScene rockScene = _activeBorderRockScenes[
				(int)((edgeHash + (uint)rockIndex) %
				(uint)_activeBorderRockScenes.Count)];
			float centeredIndex = rockIndex - (rocksPerEdge - 1) * 0.5f;
			float tangentJitter = Mathf.Lerp(
				-BorderRockPositionJitter,
				BorderRockPositionJitter,
				((hash >> 16) & 0xFFu) / 255.0f) * rockSpacing;
			float scaleJitter = Mathf.Lerp(
				1.0f,
				1.08f,
				((hash >> 24) & 0xFFu) / 255.0f);
			Vector3 rockPosition = tilePosition +
				centerToNeighbor * 0.5f +
				outwardDirection * StoneBorderOutwardOffset * HexSize +
				tangentDirection *
				(centeredIndex * rockSpacing + tangentJitter);

			CreateStoneBorderPiece(
				rockScene,
				rockPosition,
				coord,
				Vector3.Zero,
				rotationY,
				$"{edgeName}_{rockIndex}",
				BorderRockScale * scaleJitter * scaleMultiplier);
		}
	}

	private void CreateRoundStoneBorder()
	{
		foreach (KeyValuePair<HexCoord, HexTile> tileEntry in _tileViews)
		{
			HexCoord coord = tileEntry.Key;
			HexTile tileView = tileEntry.Value;
			Vector3 tilePosition = GetRawHexPosition(coord, HexSize);
			Vector3 outwardDirection = Vector3.Zero;
			int missingNeighborCount = 0;

			for (int directionIndex = 0;
				directionIndex < HexDirections.Directions.Length;
				directionIndex++)
			{
				HexCoord neighborCoord = HexDirections.GetNeighbor(
					coord,
					directionIndex);

				if (BoardData.GetTile(neighborCoord) != null)
					continue;

				Vector3 neighborPosition = GetRawHexPosition(
					neighborCoord,
					HexSize);
				outwardDirection += (neighborPosition - tilePosition).Normalized();
				missingNeighborCount++;
			}

			if (missingNeighborCount == 0 || outwardDirection.IsZeroApprox())
				continue;

			bool isCorner = missingNeighborCount >= 3;
			PackedScene stoneScene = isCorner
				? CornerStoneScene
				: Side2StoneScene;
			Vector3 modelOffset = isCorner
				? CornerStoneModelOffset
				: Side2StoneModelOffset;
			float rotationY = -Mathf.Atan2(
				outwardDirection.Z,
				outwardDirection.X);

			if (isCorner)
			{
				rotationY += Mathf.Pi / 4.0f + Mathf.DegToRad(
					CornerStoneRotationOffsetDegrees);
			}

			CreateStoneBorderPiece(
				stoneScene,
				tileView.Position +
					outwardDirection.Normalized() *
					StoneBorderOutwardOffset * HexSize,
				coord,
				modelOffset,
				rotationY,
				isCorner ? "RoundCorner" : "RoundEdge");
		}
	}

	private void CreateDecorativeOuterRing(GameConfig balance)
	{
		if (!ShowDecorativeOuterRing || balance.UseRectangularBoard)
			return;

		int boardRadius = System.Math.Max(balance.BoardRadius, 1);
		int waterGap = System.Math.Max(WaterGapRings, 1);
		int groundRows = System.Math.Max(DecorativeGroundRows, 1);
		int innerRadius = boardRadius + waterGap + 1;
		int outerRadius = innerRadius + groundRows - 1;
		Dictionary<PackedScene, DecorativeTileTemplate> tileTemplates = new();

		for (int q = -outerRadius; q <= outerRadius; q++)
		{
			int rMinimum = System.Math.Max(-outerRadius, -q - outerRadius);
			int rMaximum = System.Math.Min(outerRadius, -q + outerRadius);

			for (int r = rMinimum; r <= rMaximum; r++)
			{
				HexCoord coord = new HexCoord(q, r);
				int distance = GetHexDistance(coord);

				if (distance < innerRadius)
					continue;

				Node3D decorativeTile = CreateDecorativeTile(
					coord,
					innerRadius,
					tileTemplates);

				if (decorativeTile != null &&
					ShowDecorativeShoreStones &&
					distance == innerRadius)
				{
					CreateDecorativeShoreStones(
						coord,
						decorativeTile,
						innerRadius);
				}

				if (decorativeTile != null && ShowDecorativeCliff)
				{
					CreateDecorativeCliffFaces(
						coord,
						decorativeTile,
						innerRadius,
						outerRadius);
				}

				if (decorativeTile != null)
				{
					CreateOuterVegetation(
						coord,
						decorativeTile,
						distance,
						innerRadius);
				}
			}
		}

		foreach (DecorativeTileTemplate template in tileTemplates.Values)
		{
			template.Tile.GetParent()?.RemoveChild(template.Tile);
			template.Tile.Free();
		}
	}

	private Node3D CreateDecorativeTile(
		HexCoord coord,
		int innerRadius,
		Dictionary<PackedScene, DecorativeTileTemplate> tileTemplates)
	{
		if (_activeHexTileVariants.Count == 0)
			return null;

		uint visualHash = GetTileVisualHash(coord);
		int variantIndex = (int)(visualHash % (uint)_activeHexTileVariants.Count);
		PackedScene tileScene = _activeHexTileVariants[variantIndex];
		bool createsTemplate = !tileTemplates.TryGetValue(
			tileScene,
			out DecorativeTileTemplate template);

		if (createsTemplate)
		{
			Node tileInstance = tileScene.Instantiate();

			if (tileInstance is not Node3D templateTile)
			{
				GD.PrintErr(
					$"{tileScene.ResourcePath}: Der Root-Node muss Node3D verwenden.");
				tileInstance.Free();
				return null;
			}

			template = new DecorativeTileTemplate
			{
				Tile = templateTile
			};
			tileTemplates.Add(tileScene, template);
		}

		Node3D decorativeTile = template.Tile;
		int rotationStep = (int)(
			(visualHash / (uint)_activeHexTileVariants.Count) % 6u);
		decorativeTile.Name = $"DecorativeTile_{coord.Q}_{coord.R}";
		float cliffHeight = GetDecorativeCliffHeight(coord, innerRadius);
		decorativeTile.Position = HexToWorld(coord, HexSize) +
			Vector3.Up * (DecorativeGroundHeight + cliffHeight);
		decorativeTile.Rotation = new Vector3(
			0.0f,
			rotationStep * Mathf.Pi / 3.0f,
			0.0f);
		float tileScale = Mathf.Max(HexSize, 0.1f);
		decorativeTile.Scale = new Vector3(tileScale, 1.0f, tileScale);
		decorativeTile.ProcessMode = ProcessModeEnum.Disabled;

		if (createsTemplate)
		{
			AddChild(decorativeTile);

			if (decorativeTile is HexTile decorativeHexTile)
			{
				float inverseTileScale = 1.0f / tileScale;
				decorativeHexTile.ConfigureGrassVisual(
					GrassBaseDensity,
					GrassInstancesPerTile,
					GrassWindWaveSpeed,
					GrassWindWaveStrength,
					GrassWindDetailSpeed,
					GrassWindDetailStrength,
					ToGlobal(decorativeTile.Position),
					HexSize * Mathf.Sqrt(3.0f) * 0.5f * inverseTileScale,
					GrassOuterMargin * inverseTileScale,
					GrassStoneMargin * inverseTileScale,
					GrassOakMargin * inverseTileScale,
					GrassBirchMargin * inverseTileScale,
					GrassMushroomMargin * inverseTileScale,
					GrassMossMargin * inverseTileScale,
					null,
					null);
				template.UsesGeneratedGrass =
					decorativeHexTile.PrepareDecorativeGrass(coord, tileScale);
			}
		}

		QueueDecorativeGround(tileScene, decorativeTile);
		QueueDecorativeGrass(
			tileScene,
			decorativeTile,
			visualHash,
			template.UsesGeneratedGrass,
			coord,
			innerRadius);
		QueueDecorativeDetails(tileScene, decorativeTile);

		return decorativeTile;
	}

	private void QueueDecorativeDetails(
		PackedScene tileScene,
		Node3D decorativeTile)
	{
		Vector3 sectorPosition = decorativeTile.Position - _boardWorldCenter;
		float angle = Mathf.PosMod(
			Mathf.Atan2(sectorPosition.Z, sectorPosition.X),
			Mathf.Tau);
		int sector = Mathf.Clamp(
			Mathf.FloorToInt(angle / Mathf.Tau * 6.0f),
			0,
			5);
		var batchKey = (Scene: tileScene, Sector: sector);
		if (!_decorativeDetailBatches.TryGetValue(
			batchKey,
			out StoneSceneBatch batch))
		{
			batch = new StoneSceneBatch(Transform3D.Identity);

			foreach (Node child in decorativeTile.GetChildren())
			{
				if (!child.Name.ToString().StartsWith(
					"Rock_",
					System.StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				CollectStoneTemplateNodes(
					child,
					Transform3D.Identity,
					batch,
					isRoot: false,
					collisionLayer: 1,
					collisionMask: 1);
			}

			_decorativeDetailBatches.Add(batchKey, batch);
		}

		if (batch.Meshes.Count > 0 || batch.Collisions.Count > 0)
			batch.Instances.Add(decorativeTile.Transform);
	}

	private void QueueDecorativeGround(
		PackedScene tileScene,
		Node3D decorativeTile)
	{
		if (!TryFindDecorativeGroundMesh(
			decorativeTile,
			Transform3D.Identity,
			isRoot: true,
			out MeshInstance3D groundMesh,
			out Transform3D groundTransform))
		{
			GD.PushWarning($"{tileScene.ResourcePath}: Kein TileNew-Bodenmesh gefunden.");
			return;
		}

		if (!_decorativeGroundBatches.TryGetValue(
			tileScene,
			out DecorativeGroundBatch batch))
		{
			batch = new DecorativeGroundBatch
			{
				Mesh = CreateBatchMesh(groundMesh),
				CastShadow = groundMesh.CastShadow,
				Layers = groundMesh.Layers,
				MaterialOverride = groundMesh.MaterialOverride,
				MaterialOverlay = groundMesh.MaterialOverlay
			};
			_decorativeGroundBatches.Add(tileScene, batch);
		}

		batch.Instances.Add(decorativeTile.Transform * groundTransform);
	}

	private static bool TryFindDecorativeGroundMesh(
		Node node,
		Transform3D parentTransform,
		bool isRoot,
		out MeshInstance3D groundMesh,
		out Transform3D groundTransform)
	{
		Transform3D localTransform = parentTransform;
		if (!isRoot && node is Node3D node3D)
			localTransform = parentTransform * node3D.Transform;

		if (node is MeshInstance3D meshInstance &&
			meshInstance.Mesh != null &&
			meshInstance.Name.ToString().StartsWith(
				"TileNew",
				System.StringComparison.Ordinal))
		{
			groundMesh = meshInstance;
			groundTransform = localTransform;
			return true;
		}

		foreach (Node child in node.GetChildren())
		{
			if (TryFindDecorativeGroundMesh(
				child,
				localTransform,
				isRoot: false,
				out groundMesh,
				out groundTransform))
			{
				return true;
			}
		}

		groundMesh = null;
		groundTransform = Transform3D.Identity;
		return false;
	}

	private void BuildDecorativeGroundMultiMeshes()
	{
		int batchIndex = 0;

		foreach (DecorativeGroundBatch batch in _decorativeGroundBatches.Values)
		{
			if (batch.Instances.Count == 0)
				continue;

			MultiMesh multiMesh = new()
			{
				TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
				Mesh = batch.Mesh
			};
			multiMesh.InstanceCount = batch.Instances.Count;
			multiMesh.VisibleInstanceCount = -1;

			for (int instanceIndex = 0;
				instanceIndex < batch.Instances.Count;
				instanceIndex++)
			{
				multiMesh.SetInstanceTransform(
					instanceIndex,
					batch.Instances[instanceIndex]);
			}

			MultiMeshInstance3D multiMeshInstance = new()
			{
				Name = $"DecorativeTile_GroundMultiMesh_{batchIndex}",
				Multimesh = multiMesh,
				CastShadow = batch.CastShadow,
				Layers = batch.Layers,
				MaterialOverride = batch.MaterialOverride,
				MaterialOverlay = batch.MaterialOverlay
			};
			AddChild(multiMeshInstance);
			batchIndex++;
		}
	}

	private void QueueDecorativeGrass(
		PackedScene tileScene,
		Node3D decorativeTile,
		uint visualHash,
		bool usesGeneratedGrass,
		HexCoord coord,
		int innerRadius)
	{
		if (OuterGrassDensity <= 0.0f ||
			!TryFindDecorativeGrassMultiMesh(
				decorativeTile,
				Transform3D.Identity,
				isRoot: true,
				out MultiMeshInstance3D grassInstance,
				out Transform3D grassTransform) ||
			grassInstance.Multimesh?.Mesh == null)
		{
			return;
		}

		MultiMesh source = grassInstance.Multimesh;
		int sourceCount = usesGeneratedGrass && source.VisibleInstanceCount >= 0
			? Mathf.Min(source.VisibleInstanceCount, source.InstanceCount)
			: source.InstanceCount;
		if (sourceCount <= 0)
			return;
		Mesh grassPieceMesh = HexTile.GetAnimeGrassPieceMesh();

		Vector3 sectorPosition = decorativeTile.Position - _boardWorldCenter;
		float angle = Mathf.PosMod(
			Mathf.Atan2(sectorPosition.Z, sectorPosition.X),
			Mathf.Tau);
		int sector = Mathf.Clamp(
			Mathf.FloorToInt(angle / Mathf.Tau * 6.0f),
			0,
			5);
		var batchKey = (Scene: tileScene, Sector: sector);

		if (!_decorativeGrassBatches.TryGetValue(
			batchKey,
			out DecorativeGrassBatch batch))
		{
			batch = new DecorativeGrassBatch
			{
				Mesh = grassPieceMesh ?? source.Mesh,
				CastShadow = grassInstance.CastShadow,
				Layers = grassInstance.Layers,
				MaterialOverride = grassInstance.MaterialOverride,
				MaterialOverlay = grassInstance.MaterialOverlay
			};
			_decorativeGrassBatches.Add(batchKey, batch);
		}

		int selectedCount = Mathf.Clamp(
			Mathf.RoundToInt(
				sourceCount * Mathf.Clamp(OuterGrassDensity, 0.0f, 1.0f)),
			1,
			sourceCount);
		int sourceOffset = (int)(visualHash % (uint)sourceCount);

		for (int selectedIndex = 0;
			selectedIndex < selectedCount;
			selectedIndex++)
		{
			int sourceIndex = (
				sourceOffset + Mathf.FloorToInt(
					selectedIndex * sourceCount / (float)selectedCount)) %
				sourceCount;
			Transform3D sourceTransform = source.GetInstanceTransform(sourceIndex);

			if (!usesGeneratedGrass && grassPieceMesh != null)
			{
				uint scaleHash = visualHash ^
					((uint)(sourceIndex + 1) * 0x85EBCA6Bu);
				float scaleJitter = Mathf.Lerp(
					0.62f,
					1.0f,
					HashToUnitFloat(scaleHash));
				Basis grassBasis = sourceTransform.Basis
					.Orthonormalized()
					.Scaled(new Vector3(
						2.45f * scaleJitter,
						2.35f * scaleJitter,
						2.45f * scaleJitter));
				sourceTransform = new Transform3D(
					grassBasis,
					sourceTransform.Origin);
			}

			Transform3D instanceTransform =
				decorativeTile.Transform *
				grassTransform *
				sourceTransform;
			if (IsInsideDecorativeShoreGrassMargin(
				coord,
				innerRadius,
				instanceTransform.Origin))
			{
				continue;
			}

			if (IsInsideBridgeLanding(instanceTransform.Origin, 0.1f))
				continue;

			batch.Instances.Add(instanceTransform);
		}
	}

	private bool IsInsideDecorativeShoreGrassMargin(
		HexCoord coord,
		int innerRadius,
		Vector3 grassPosition)
	{
		float shoreMargin = Mathf.Max(OuterGrassShoreMargin, 0.0f);
		if (shoreMargin <= 0.0f || GetHexDistance(coord) != innerRadius)
			return false;

		Vector3 tilePosition = HexToWorld(coord, HexSize);
		Vector3 grassOffset = grassPosition - tilePosition;
		float edgeDistance = HexSize * Mathf.Sqrt(3.0f) * 0.5f;

		for (int directionIndex = 0;
			directionIndex < HexDirections.Directions.Length;
			directionIndex++)
		{
			HexCoord neighborCoord = HexDirections.GetNeighbor(
				coord,
				directionIndex);
			if (GetHexDistance(neighborCoord) >= innerRadius)
				continue;

			Vector3 shoreDirection =
				HexToWorld(neighborCoord, HexSize) - tilePosition;
			shoreDirection.Y = 0.0f;
			shoreDirection = shoreDirection.Normalized();

			float distanceToShore = edgeDistance -
				grassOffset.Dot(shoreDirection);
			if (distanceToShore < shoreMargin)
				return true;
		}

		return false;
	}

	private static bool TryFindDecorativeGrassMultiMesh(
		Node node,
		Transform3D parentTransform,
		bool isRoot,
		out MultiMeshInstance3D grassInstance,
		out Transform3D grassTransform)
	{
		Transform3D localTransform = parentTransform;
		if (!isRoot && node is Node3D node3D)
			localTransform = parentTransform * node3D.Transform;

		if (node is MultiMeshInstance3D multiMeshInstance &&
			multiMeshInstance.Multimesh?.Mesh != null)
		{
			grassInstance = multiMeshInstance;
			grassTransform = localTransform;
			return true;
		}

		foreach (Node child in node.GetChildren())
		{
			if (TryFindDecorativeGrassMultiMesh(
				child,
				localTransform,
				isRoot: false,
				out grassInstance,
				out grassTransform))
			{
				return true;
			}
		}

		grassInstance = null;
		grassTransform = Transform3D.Identity;
		return false;
	}

	private void BuildDecorativeGrassMultiMeshes()
	{
		int batchIndex = 0;

		foreach (DecorativeGrassBatch batch in _decorativeGrassBatches.Values)
		{
			if (batch.Instances.Count == 0)
				continue;

			MultiMesh multiMesh = new()
			{
				TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
				UseCustomData = true,
				Mesh = batch.Mesh
			};
			multiMesh.InstanceCount = batch.Instances.Count;
			multiMesh.VisibleInstanceCount = -1;

			for (int instanceIndex = 0;
				instanceIndex < batch.Instances.Count;
				instanceIndex++)
			{
				multiMesh.SetInstanceTransform(
					instanceIndex,
					batch.Instances[instanceIndex]);
				multiMesh.SetInstanceCustomData(instanceIndex, Colors.White);
			}

			MultiMeshInstance3D multiMeshInstance = new()
			{
				Name = $"DecorativeTile_GrassMultiMesh_{batchIndex}",
				Multimesh = multiMesh,
				CastShadow = batch.CastShadow,
				Layers = batch.Layers,
				MaterialOverride = batch.MaterialOverride,
				MaterialOverlay = batch.MaterialOverlay
			};
			ApplyDecorativeGrassState(multiMeshInstance, batchIndex);
			multiMeshInstance.SetInstanceShaderParameter(
				"grass_wind",
				new Vector4(
					GrassWindWaveSpeed,
					GrassWindWaveStrength,
					GrassWindDetailSpeed,
					GrassWindDetailStrength));
			AddChild(multiMeshInstance);
			_decorativeGrassInstances.Add(multiMeshInstance);
			_outerRingVisualNodes[multiMeshInstance] =
				OuterRingVisualGroup.OuterGrass;
			VisibilityRangeUtility.Configure(
				multiMeshInstance,
				EnableVisibilityRanges,
				GrassVisibilityRange,
				VisibilityRangeMargin,
				FrustumCullMargin);
			batchIndex++;
		}
	}

	public void SetDecorativeGrassDroughtActive(bool active)
	{
		_decorativeGrassDryAmount = active
			? Mathf.Clamp(OuterGrassDroughtDryAmount, 0.0f, 1.0f)
			: 0.0f;

		for (int batchIndex = 0;
			batchIndex < _decorativeGrassInstances.Count;
			batchIndex++)
		{
			MultiMeshInstance3D grassInstance =
				_decorativeGrassInstances[batchIndex];
			if (IsInstanceValid(grassInstance))
				ApplyDecorativeGrassState(grassInstance, batchIndex);
		}
	}

	private void ApplyDecorativeGrassState(
		MultiMeshInstance3D grassInstance,
		int batchIndex)
	{
		grassInstance.SetInstanceShaderParameter(
			"grass_state",
			new Vector4(
				Mathf.Clamp(GrassBaseDensity, 0.0f, 1.0f),
				0.78f,
				_decorativeGrassDryAmount,
				batchIndex + 1.0f));
	}

	private void CreateOuterVegetation(
		HexCoord coord,
		Node3D decorativeTile,
		int distance,
		int innerRadius)
	{
		if (!ShowDecorativeOuterVegetation)
			return;

		int firstVegetationRadius =
			innerRadius + System.Math.Max(OuterVegetationShoreClearRows, 0);
		if (distance < firstVegetationRadius)
			return;

		float treeChance = _activeOuterTreeScenes.Count > 0
			? Mathf.Clamp(OuterTreeChance, 0.0f, 1.0f)
			: 0.0f;
		float detailChance = _activeOuterDetailScenes.Count > 0
			? Mathf.Clamp(OuterDetailChance, 0.0f, 1.0f - treeChance)
			: 0.0f;

		if (treeChance + detailChance <= 0.0f)
			return;

		uint visualHash = GetTileVisualHash(coord);
		RandomNumberGenerator random = new RandomNumberGenerator
		{
			Seed = visualHash ^ (uint)System.Math.Max(OuterVegetationRandomSeed, 1)
		};
		float selection = random.Randf();
		bool createTree = selection < treeChance;

		if (!createTree && selection >= treeChance + detailChance)
			return;

		List<PackedScene> scenes = createTree
			? _activeOuterTreeScenes
			: _activeOuterDetailScenes;
		PackedScene scene = scenes[random.RandiRange(0, scenes.Count - 1)];

		float positionRadius =
			Mathf.Max(OuterVegetationPositionRadius, 0.0f);
		float offsetDistance = Mathf.Sqrt(random.Randf()) * positionRadius;
		float offsetAngle = random.RandfRange(0.0f, Mathf.Tau);
		float minimumScale = createTree
			? Mathf.Min(OuterTreeMinimumScale, OuterTreeMaximumScale)
			: Mathf.Min(OuterDetailMinimumScale, OuterDetailMaximumScale);
		float maximumScale = createTree
			? Mathf.Max(OuterTreeMinimumScale, OuterTreeMaximumScale)
			: Mathf.Max(OuterDetailMinimumScale, OuterDetailMaximumScale);
		float uniformScale = random.RandfRange(
			Mathf.Max(minimumScale, 0.01f),
			Mathf.Max(maximumScale, 0.01f));

		if (!createTree && scene.ResourcePath.EndsWith(
			"Flower_3_Group.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			uniformScale *= Mathf.Clamp(
				OuterFlowerScaleMultiplier,
				0.1f,
				1.0f);
		}

		Vector3 localPosition = new Vector3(
			Mathf.Cos(offsetAngle) * offsetDistance,
			OuterVegetationHeightOffset,
			Mathf.Sin(offsetAngle) * offsetDistance);
		float inverseTileScale = 1.0f / Mathf.Max(HexSize, 0.1f);
		Vector3 vegetationScale = new Vector3(
			uniformScale * inverseTileScale,
			uniformScale,
			uniformScale * inverseTileScale);
		Basis vegetationBasis = Basis.Identity
			.Rotated(
				Vector3.Up,
				random.RandfRange(0.0f, Mathf.Tau))
			.Scaled(vegetationScale);
		Transform3D vegetationTransform = decorativeTile.Transform *
			new Transform3D(vegetationBasis, localPosition);
		Vector3 sectorPosition = vegetationTransform.Origin - _boardWorldCenter;
		float sectorAngle = Mathf.PosMod(
			Mathf.Atan2(sectorPosition.Z, sectorPosition.X),
			Mathf.Tau);
		int sector = Mathf.Clamp(
			Mathf.FloorToInt(sectorAngle / Mathf.Tau * 6.0f),
			0,
			5);
		OuterRingVisualGroup visualGroup =
			GetOuterVegetationVisualGroup(scene);
		OuterVegetationBatch batch = GetOrCreateOuterVegetationBatch(
			scene,
			sector,
			visualGroup);

		if (batch.CanBatch)
		{
			batch.Instances.Add(vegetationTransform);
			return;
		}

		Node instance = scene.Instantiate();
		if (instance is not Node3D vegetation)
		{
			instance.Free();
			return;
		}

		vegetation.Name =
			$"DecorativeTile_OuterVegetation_{coord.Q}_{coord.R}";
		vegetation.Transform = vegetationTransform;
		vegetation.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(vegetation);
		_outerRingVisualNodes[vegetation] = visualGroup;
		VisibilityRangeUtility.Configure(
			vegetation,
			EnableVisibilityRanges,
			VegetationVisibilityRange,
			VisibilityRangeMargin,
			FrustumCullMargin);
	}

	private OuterVegetationBatch GetOrCreateOuterVegetationBatch(
		PackedScene scene,
		int sector,
		OuterRingVisualGroup visualGroup)
	{
		var batchKey = (Scene: scene, Sector: sector);
		if (_outerVegetationBatches.TryGetValue(
			batchKey,
			out OuterVegetationBatch batch))
		{
			return batch;
		}

		batch = new OuterVegetationBatch
		{
			VisualGroup = visualGroup
		};
		Node instance = scene.Instantiate();

		if (instance is not Node3D vegetationRoot)
		{
			batch.CanBatch = false;
			GD.PushWarning(
				$"{scene.ResourcePath}: Der Root-Node muss Node3D verwenden.");
			instance.Free();
		}
		else
		{
			CollectOuterVegetationTemplateNodes(
				vegetationRoot,
				Transform3D.Identity,
				batch,
				isRoot: true);
			vegetationRoot.Free();
			batch.CanBatch &= batch.Meshes.Count > 0;
		}

		_outerVegetationBatches.Add(batchKey, batch);
		return batch;
	}

	private static OuterRingVisualGroup GetOuterVegetationVisualGroup(
		PackedScene scene)
	{
		string resourcePath = scene?.ResourcePath ?? "";

		if (resourcePath.EndsWith(
			"/CommonTree_1.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			return OuterRingVisualGroup.CommonTree;
		}

		if (resourcePath.EndsWith(
			"/Pine_1.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			return OuterRingVisualGroup.Pine1;
		}

		if (resourcePath.EndsWith(
			"/Pine_2.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			return OuterRingVisualGroup.Pine2;
		}

		if (resourcePath.EndsWith(
			"/Pine_3.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			return OuterRingVisualGroup.Pine3;
		}

		if (resourcePath.EndsWith(
			"/Bush_Common.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			return OuterRingVisualGroup.Bush;
		}

		if (resourcePath.EndsWith(
			"/Bush_Common_Flowers.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			return OuterRingVisualGroup.FloweringBush;
		}

		if (resourcePath.EndsWith(
			"/Flower_3_Group.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			return OuterRingVisualGroup.Flowers;
		}

		if (resourcePath.EndsWith(
			"/Mushroom_Common.gltf",
			System.StringComparison.OrdinalIgnoreCase))
		{
			return OuterRingVisualGroup.Mushrooms;
		}

		return OuterRingVisualGroup.Other;
	}

	private static void CollectOuterVegetationTemplateNodes(
		Node node,
		Transform3D parentTransform,
		OuterVegetationBatch batch,
		bool isRoot)
	{
		if (!batch.CanBatch)
			return;

		bool hasScript =
			node.GetScript().VariantType != Variant.Type.Nil;
		bool hasUnsupportedRuntimeContent =
			node is AnimationMixer ||
			node is Skeleton3D ||
			node is CollisionObject3D ||
			node is CollisionShape3D ||
			node is VisualInstance3D && node is not MeshInstance3D;

		if (hasScript || hasUnsupportedRuntimeContent)
		{
			batch.CanBatch = false;
			batch.Meshes.Clear();
			return;
		}

		Transform3D localTransform = parentTransform;
		if (!isRoot && node is Node3D node3D)
			localTransform = parentTransform * node3D.Transform;

		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			batch.Meshes.Add(new StoneMeshTemplate
			{
				Mesh = CreateBatchMesh(meshInstance),
				LocalTransform = localTransform,
				CastShadow = meshInstance.CastShadow,
				Layers = meshInstance.Layers,
				MaterialOverride = meshInstance.MaterialOverride,
				MaterialOverlay = meshInstance.MaterialOverlay
			});
		}

		foreach (Node child in node.GetChildren())
		{
			CollectOuterVegetationTemplateNodes(
				child,
				localTransform,
				batch,
				isRoot: false);

			if (!batch.CanBatch)
				return;
		}
	}

	private void BuildOuterVegetationMultiMeshes()
	{
		int sceneBatchIndex = 0;

		foreach (OuterVegetationBatch batch in _outerVegetationBatches.Values)
		{
			if (!batch.CanBatch || batch.Instances.Count == 0)
				continue;

			for (int meshIndex = 0; meshIndex < batch.Meshes.Count; meshIndex++)
			{
				StoneMeshTemplate meshTemplate = batch.Meshes[meshIndex];
				MultiMesh multiMesh = new()
				{
					TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
					Mesh = meshTemplate.Mesh
				};
				multiMesh.InstanceCount = batch.Instances.Count;
				multiMesh.VisibleInstanceCount = -1;

				for (int instanceIndex = 0;
					instanceIndex < batch.Instances.Count;
					instanceIndex++)
				{
					multiMesh.SetInstanceTransform(
						instanceIndex,
						batch.Instances[instanceIndex] *
							meshTemplate.LocalTransform);
				}

				MultiMeshInstance3D multiMeshInstance = new()
				{
					Name =
						$"DecorativeTile_OuterVegetationMultiMesh_{sceneBatchIndex}_{meshIndex}",
					Multimesh = multiMesh,
					CastShadow = meshTemplate.CastShadow,
					Layers = meshTemplate.Layers,
					MaterialOverride = meshTemplate.MaterialOverride,
					MaterialOverlay = meshTemplate.MaterialOverlay
				};
				AddChild(multiMeshInstance);
				_outerRingVisualNodes[multiMeshInstance] = batch.VisualGroup;
				VisibilityRangeUtility.Configure(
					multiMeshInstance,
					EnableVisibilityRanges,
					VegetationVisibilityRange,
					VisibilityRangeMargin,
					FrustumCullMargin);
			}

			sceneBatchIndex++;
		}
	}

	private void CreateDecorativeShoreStones(
		HexCoord coord,
		Node3D decorativeTile,
		int innerRadius)
	{
		Vector3 tilePosition = GetRawHexPosition(coord, HexSize);

		for (int directionIndex = 0;
			directionIndex < HexDirections.Directions.Length;
			directionIndex++)
		{
			HexCoord neighborCoord = HexDirections.GetNeighbor(
				coord,
				directionIndex);

			if (GetHexDistance(neighborCoord) >= innerRadius)
				continue;

			Vector3 neighborPosition = GetRawHexPosition(
				neighborCoord,
				HexSize);
			Vector3 centerToNeighbor = neighborPosition - tilePosition;
			CreateVariantRockEdge(
				decorativeTile.Position,
				centerToNeighbor,
				coord,
				directionIndex,
				$"OuterRock{directionIndex}");
		}
	}

	private void CreateDecorativeCliffFaces(
		HexCoord coord,
		Node3D decorativeTile,
		int innerRadius,
		int outerRadius)
	{
		float cliffHeight = GetDecorativeCliffHeight(coord, innerRadius);
		if (cliffHeight <= 0.0f)
			return;

		Vector3 tilePosition = GetRawHexPosition(coord, HexSize);

		for (int directionIndex = 0;
			directionIndex < HexDirections.Directions.Length;
			directionIndex++)
		{
			HexCoord neighborCoord = HexDirections.GetNeighbor(
				coord,
				directionIndex);
			int neighborDistance = GetHexDistance(neighborCoord);
			float neighborCliffHeight = neighborDistance <= outerRadius
				? GetDecorativeCliffHeight(neighborCoord, innerRadius)
				: cliffHeight;
			float heightDifference = cliffHeight - neighborCliffHeight;

			if (heightDifference <= 0.01f)
				continue;

			Vector3 neighborPosition = GetRawHexPosition(
				neighborCoord,
				HexSize);
			CreateDecorativeCliffRockEdge(
				decorativeTile.Position,
				neighborPosition - tilePosition,
				coord,
				directionIndex);
		}
	}

	private void CreateDecorativeCliffRockEdge(
		Vector3 tilePosition,
		Vector3 centerToNeighbor,
		HexCoord coord,
		int directionIndex)
	{
		CreateVariantRockEdge(
			tilePosition,
			centerToNeighbor,
			coord,
			directionIndex,
			$"DecorativeCliff{directionIndex}");
	}

	private float GetDecorativeCliffHeight(
		HexCoord coord,
		int innerRadius)
	{
		int cliffStep = GetDecorativeCliffStep(coord, innerRadius);
		if (cliffStep < 0)
			return 0.0f;

		return Mathf.Max(DecorativeCliffHeight, 0.0f);
	}

	private int GetDecorativeCliffStep(
		HexCoord coord,
		int innerRadius)
	{
		if (!ShowDecorativeCliff)
			return -1;

		int forward;
		float tangent;

		switch (Mathf.PosMod(DecorativeCliffSide, 6))
		{
			case 0:
				forward = coord.Q;
				tangent = coord.R + coord.Q * 0.5f;
				break;
			case 1:
				forward = coord.Q + coord.R;
				tangent = (coord.Q - coord.R) * 0.5f;
				break;
			case 2:
				forward = coord.R;
				tangent = -(coord.Q + coord.R * 0.5f);
				break;
			case 3:
				forward = -coord.Q;
				tangent = -(coord.R + coord.Q * 0.5f);
				break;
			case 4:
				forward = -coord.Q - coord.R;
				tangent = (coord.R - coord.Q) * 0.5f;
				break;
			default:
				forward = -coord.R;
				tangent = coord.Q + coord.R * 0.5f;
				break;
		}

		float halfWidth = System.Math.Max(DecorativeCliffWidth, 10) * 0.5f;
		if (forward < innerRadius + 2 ||
			tangent < -halfWidth ||
			tangent >= halfWidth)
		{
			return -1;
		}

		return 0;
	}

	private float GetCornerStoneRotation(bool isLeftSide, bool isFront)
	{
		float rotationY;

		if (!isLeftSide && isFront)
			rotationY = 0.0f;
		else if (isLeftSide && isFront)
			rotationY = -Mathf.Pi / 2.0f;
		else if (!isLeftSide)
			rotationY = Mathf.Pi / 2.0f;
		else
			rotationY = Mathf.Pi;

		return rotationY + Mathf.DegToRad(CornerStoneRotationOffsetDegrees);
	}

	private void CreateStoneBorderPiece(
		PackedScene stoneScene,
		Vector3 tilePosition,
		HexCoord coord,
		Vector3 modelOffset,
		float rotationY,
		string edgeName,
		float uniformScale = 1.0f)
	{
		if (stoneScene == null || IsInsideBridgeLanding(tilePosition))
			return;

		StoneSceneBatch batch = GetOrCreateStoneSceneBatch(stoneScene);
		if (batch == null)
			return;

		Vector3 scale = new(
			HexSize * uniformScale,
			StoneBorderYScale * uniformScale,
			HexSize * uniformScale);
		Basis basis = Basis.Identity
			.Rotated(Vector3.Up, rotationY)
			.Scaled(scale);
		Transform3D borderTransform = new(
			basis,
			tilePosition + Vector3.Up * StoneBorderHeight);
		Transform3D modelTransform = batch.RootTransform;
		modelTransform.Origin = modelOffset;
		batch.Instances.Add(borderTransform * modelTransform);
	}

	private StoneSceneBatch GetOrCreateStoneSceneBatch(PackedScene stoneScene)
	{
		if (_stoneSceneBatches.TryGetValue(stoneScene, out StoneSceneBatch batch))
			return batch;

		Node stoneInstance = stoneScene.Instantiate();
		if (stoneInstance is not Node3D stoneRoot)
		{
			GD.PrintErr($"{stoneScene.ResourcePath}: Der Root-Node muss Node3D verwenden.");
			stoneInstance.Free();
			return null;
		}

		batch = new StoneSceneBatch(stoneRoot.Transform);
		CollectStoneTemplateNodes(
			stoneRoot,
			Transform3D.Identity,
			batch,
			isRoot: true,
			collisionLayer: 1,
			collisionMask: 1);
		stoneRoot.Free();

		if (batch.Meshes.Count == 0)
		{
			GD.PushWarning($"{stoneScene.ResourcePath}: Kein Steinmesh gefunden.");
		}

		_stoneSceneBatches.Add(stoneScene, batch);
		return batch;
	}

	private static void CollectStoneTemplateNodes(
		Node node,
		Transform3D parentTransform,
		StoneSceneBatch batch,
		bool isRoot,
		uint collisionLayer,
		uint collisionMask)
	{
		Transform3D localTransform = parentTransform;
		if (!isRoot && node is Node3D node3D)
			localTransform = parentTransform * node3D.Transform;

		if (node is StaticBody3D staticBody)
		{
			collisionLayer = staticBody.CollisionLayer;
			collisionMask = staticBody.CollisionMask;
		}

		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			batch.Meshes.Add(new StoneMeshTemplate
			{
				Mesh = CreateBatchMesh(meshInstance),
				LocalTransform = localTransform,
				CastShadow = meshInstance.CastShadow,
				Layers = meshInstance.Layers,
				MaterialOverride = meshInstance.MaterialOverride,
				MaterialOverlay = meshInstance.MaterialOverlay
			});
		}

		if (node is CollisionShape3D collisionShape && collisionShape.Shape != null)
		{
			batch.Collisions.Add(new StoneCollisionTemplate
			{
				Shape = collisionShape.Shape,
				LocalTransform = localTransform,
				CollisionLayer = collisionLayer,
				CollisionMask = collisionMask,
				Disabled = collisionShape.Disabled
			});
		}

		foreach (Node child in node.GetChildren())
		{
			CollectStoneTemplateNodes(
				child,
				localTransform,
				batch,
				isRoot: false,
				collisionLayer,
				collisionMask);
		}
	}

	private static Mesh CreateBatchMesh(MeshInstance3D meshInstance)
	{
		Mesh sourceMesh = meshInstance.Mesh;
		bool hasSurfaceOverride = false;

		for (int surfaceIndex = 0;
			surfaceIndex < sourceMesh.GetSurfaceCount();
			surfaceIndex++)
		{
			if (meshInstance.GetSurfaceOverrideMaterial(surfaceIndex) != null)
			{
				hasSurfaceOverride = true;
				break;
			}
		}

		if (!hasSurfaceOverride)
			return sourceMesh;

		Mesh batchMesh = sourceMesh.Duplicate() as Mesh;
		if (batchMesh == null)
			return sourceMesh;

		for (int surfaceIndex = 0;
			surfaceIndex < batchMesh.GetSurfaceCount();
			surfaceIndex++)
		{
			Material surfaceOverride =
				meshInstance.GetSurfaceOverrideMaterial(surfaceIndex);
			if (surfaceOverride != null)
				batchMesh.SurfaceSetMaterial(surfaceIndex, surfaceOverride);
		}

		return batchMesh;
	}

	private void BuildDecorativeDetailMultiMeshes()
	{
		BuildStaticSceneMultiMeshes(
			_decorativeDetailBatches.Values,
			"DecorativeTile_Detail");
	}

	private void BuildStoneBorderMultiMeshes()
	{
		BuildStaticSceneMultiMeshes(_stoneSceneBatches.Values, "StoneBorder");
	}

	private void BuildStaticSceneMultiMeshes(
		IEnumerable<StoneSceneBatch> sceneBatches,
		string namePrefix)
	{
		int sceneBatchIndex = 0;

		foreach (StoneSceneBatch batch in sceneBatches)
		{
			if (batch.Instances.Count == 0)
				continue;

			for (int meshIndex = 0; meshIndex < batch.Meshes.Count; meshIndex++)
			{
				StoneMeshTemplate meshTemplate = batch.Meshes[meshIndex];
				MultiMesh multiMesh = new()
				{
					TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
					Mesh = meshTemplate.Mesh
				};
				multiMesh.InstanceCount = batch.Instances.Count;
				multiMesh.VisibleInstanceCount = -1;

				for (int instanceIndex = 0;
					instanceIndex < batch.Instances.Count;
					instanceIndex++)
				{
					multiMesh.SetInstanceTransform(
						instanceIndex,
						batch.Instances[instanceIndex] * meshTemplate.LocalTransform);
				}

				MultiMeshInstance3D multiMeshInstance = new()
				{
					Name = $"{namePrefix}_MultiMesh_{sceneBatchIndex}_{meshIndex}",
					Multimesh = multiMesh,
					CastShadow = meshTemplate.CastShadow,
					Layers = meshTemplate.Layers,
					MaterialOverride = meshTemplate.MaterialOverride,
					MaterialOverlay = meshTemplate.MaterialOverlay
				};
				AddChild(multiMeshInstance);
			}

			for (int collisionIndex = 0;
				collisionIndex < batch.Collisions.Count;
				collisionIndex++)
			{
				StoneCollisionTemplate collisionTemplate =
					batch.Collisions[collisionIndex];
				StaticBody3D collisionBody = new()
				{
					Name = $"{namePrefix}_Collision_{sceneBatchIndex}_{collisionIndex}",
					CollisionLayer = collisionTemplate.CollisionLayer,
					CollisionMask = collisionTemplate.CollisionMask,
					ProcessMode = ProcessModeEnum.Disabled
				};

				foreach (Transform3D instanceTransform in batch.Instances)
				{
					CollisionShape3D collisionShape = new()
					{
						Shape = collisionTemplate.Shape,
						Transform = instanceTransform *
							collisionTemplate.LocalTransform,
						Disabled = collisionTemplate.Disabled
					};
					collisionBody.AddChild(collisionShape);
				}

				AddChild(collisionBody);
			}

			sceneBatchIndex++;
		}
	}

	private static int GetHexDistance(HexCoord coord)
	{
		return (
			System.Math.Abs(coord.Q) +
			System.Math.Abs(coord.R) +
			System.Math.Abs(coord.Q + coord.R)) / 2;
	}

	private static uint GetTileVisualHash(HexCoord coord)
	{
		unchecked
		{
			uint hash = 0xA511E9B3u;
			hash ^= (uint)coord.Q * 0x9E3779B1u;
			hash ^= (uint)coord.R * 0x85EBCA77u;
			hash ^= hash >> 16;
			hash *= 0x7FEB352Du;
			hash ^= hash >> 15;
			return hash;
		}
	}

	private Vector3 HexToWorld(HexCoord coord, float size)
	{
		Vector3 tilePosition = GetRawHexPosition(coord, size);

		return tilePosition - _boardWorldCenter;
	}

	private void UpdateBoardWorldCenter()
	{
		bool hasTile = false;
		Vector3 minimum = Vector3.Zero;
		Vector3 maximum = Vector3.Zero;

		foreach (HexCoord coord in BoardData.Tiles.Keys)
		{
			Vector3 position = GetRawHexPosition(coord, HexSize);

			if (!hasTile)
			{
				minimum = position;
				maximum = position;
				hasTile = true;
				continue;
			}

			minimum.X = Mathf.Min(minimum.X, position.X);
			minimum.Z = Mathf.Min(minimum.Z, position.Z);
			maximum.X = Mathf.Max(maximum.X, position.X);
			maximum.Z = Mathf.Max(maximum.Z, position.Z);
		}

		_boardWorldCenter = hasTile
			? (minimum + maximum) / 2.0f
			: Vector3.Zero;
	}

	private static Vector3 GetRawHexPosition(
		HexCoord coord,
		float size)
	{
		float x = size * 1.5f * coord.Q;
		float z = size * Mathf.Sqrt(3.0f) *
			(coord.R + coord.Q / 2.0f);

		return new Vector3(x, 0.0f, z);
	}

	private void ClearBoard()
	{
		while (GetChildCount() > 0)
		{
			Node child = GetChild(0);
			RemoveChild(child);
			child.QueueFree();
		}

		_tileViews.Clear();
		_decorativeDetailBatches.Clear();
		_decorativeGroundBatches.Clear();
		_decorativeGrassBatches.Clear();
		_decorativeGrassInstances.Clear();
		_outerRingVisualNodes.Clear();
		_outerVegetationBatches.Clear();
		_stoneSceneBatches.Clear();
	}
}
