using Godot;
using System.Collections.Generic;

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

	[Export] public bool ShowDecorativeShoreStones = true;

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
	[Export] public Color TreeShadowColor =
		new Color(0.015f, 0.025f, 0.012f, 0.86f);

	[Export(PropertyHint.Range, "1.0,7.0,0.1")]
	public float StartingOakShadowSize = 6.2f;

	[Export] public Vector2 StartingOakShadowOffset =
		Vector2.Zero;

	[Export(PropertyHint.Range, "0.8,4.0,0.1")]
	public float BirchShadowSize = 2.8f;

	[Export] public Vector2 BirchShadowOffset =
		new Vector2(0.0f, 0.18f);

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

	private readonly Dictionary<HexCoord, HexTile> _tileViews = new();
	private readonly List<PackedScene> _activeHexTileVariants = new();
	private readonly List<PackedScene> _activeOuterTreeScenes = new();
	private readonly List<PackedScene> _activeOuterDetailScenes = new();
	private readonly List<PackedScene> _activeBorderRockScenes = new();
	private Vector3 _boardWorldCenter = Vector3.Zero;

	public override void _Ready()
	{
		Balance ??= GameConfig.LoadDefault();
		SetupHexTileVariants();
		SetupStoneBorderScenes();
		SetupOuterVegetationScenes();

		GenerateBoard();
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
		ClearBoard();
		BoardData = new BoardData();

		GameConfig balance = Balance ?? GameConfig.LoadDefault();

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

		UpdateBoardWorldCenter();

		foreach (HexTileData tileData in BoardData.Tiles.Values)
		{
			CreateTileView(tileData);
		}

		CreateDecorativeOuterRing(balance);
		CreateStoneBorder(balance);

		GD.Print($"Board generated with {BoardData.Tiles.Count} tiles.");
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

		UpdateAllTileViews();
	}

	public void UpdateAllTileViews()
	{
		foreach (HexTile tileView in _tileViews.Values)
		{
			tileView.UpdateVisualState();
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
			TreeShadowColor,
			StartingOakShadowSize,
			StartingOakShadowOffset,
			BirchShadowSize,
			BirchShadowOffset);
		tileView.ConfigureTreeProximityFade(
			EnableTreeProximityFade,
			TreeFadeStartDistance,
			TreeFadeFullDistance,
			TreeFadeMaximumTransparency,
			TreeFadeSpeed);
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
				Vector3 centerToNeighbor = neighborPosition - tilePosition;
				Vector3 outwardDirection = centerToNeighbor.Normalized();
				Vector3 tangentDirection = new Vector3(
					-outwardDirection.Z,
					0.0f,
					outwardDirection.X);
				uint edgeHash = GetTileVisualHash(coord) ^
					((uint)(directionIndex + 1) * 0x9E3779B9u);
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
					Vector3 rockPosition = tileView.Position +
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
						$"Rock{directionIndex}_{rockIndex}",
						BorderRockScale * scaleJitter);
				}
			}
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

				Node3D decorativeTile = CreateDecorativeTile(coord);

				if (decorativeTile != null &&
					ShowDecorativeShoreStones &&
					distance == innerRadius)
				{
					CreateDecorativeShoreStones(
						coord,
						decorativeTile,
						innerRadius);
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
	}

	private Node3D CreateDecorativeTile(HexCoord coord)
	{
		if (_activeHexTileVariants.Count == 0)
			return null;

		uint visualHash = GetTileVisualHash(coord);
		int variantIndex = (int)(visualHash % (uint)_activeHexTileVariants.Count);
		PackedScene tileScene = _activeHexTileVariants[variantIndex];
		Node tileInstance = tileScene.Instantiate();

		if (tileInstance is not Node3D decorativeTile)
		{
			GD.PrintErr($"{tileScene.ResourcePath}: Der Root-Node muss Node3D verwenden.");
			tileInstance.Free();
			return null;
		}

		int rotationStep = (int)(
			(visualHash / (uint)_activeHexTileVariants.Count) % 6u);
		decorativeTile.Name = $"DecorativeTile_{coord.Q}_{coord.R}";
		decorativeTile.Position =
			HexToWorld(coord, HexSize) + Vector3.Up * DecorativeGroundHeight;
		decorativeTile.Rotation = new Vector3(
			0.0f,
			rotationStep * Mathf.Pi / 3.0f,
			0.0f);
		float tileScale = Mathf.Max(HexSize, 0.1f);
		decorativeTile.Scale = new Vector3(tileScale, 1.0f, tileScale);
		decorativeTile.ProcessMode = ProcessModeEnum.Disabled;

		AddChild(decorativeTile);

		return decorativeTile;
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
		Node instance = scene.Instantiate();

		if (instance is not Node3D vegetation)
		{
			GD.PushWarning(
				$"{scene.ResourcePath}: Der Root-Node muss Node3D verwenden.");
			instance.Free();
			return;
		}

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

		vegetation.Name =
			$"OuterVegetation_{coord.Q}_{coord.R}";
		vegetation.Position = new Vector3(
			Mathf.Cos(offsetAngle) * offsetDistance,
			OuterVegetationHeightOffset,
			Mathf.Sin(offsetAngle) * offsetDistance);
		vegetation.Rotation = new Vector3(
			0.0f,
			random.RandfRange(0.0f, Mathf.Tau),
			0.0f);
		float inverseTileScale = 1.0f / Mathf.Max(HexSize, 0.1f);
		vegetation.Scale = new Vector3(
			uniformScale * inverseTileScale,
			uniformScale,
			uniformScale * inverseTileScale);
		vegetation.ProcessMode = ProcessModeEnum.Disabled;

		decorativeTile.AddChild(vegetation);
	}

	private void CreateDecorativeShoreStones(
		HexCoord coord,
		Node3D decorativeTile,
		int innerRadius)
	{
		Vector3 tilePosition = GetRawHexPosition(coord, HexSize);
		Vector3 inwardDirection = Vector3.Zero;
		int inwardNeighborCount = 0;

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
			inwardDirection += (neighborPosition - tilePosition).Normalized();
			inwardNeighborCount++;
		}

		if (inwardNeighborCount == 0 || inwardDirection.IsZeroApprox())
			return;

		bool isCorner = inwardNeighborCount == 1;
		PackedScene stoneScene = isCorner
			? CornerStoneScene
			: Side2StoneScene;
		Vector3 modelOffset = isCorner
			? CornerStoneModelOffset
			: Side2StoneModelOffset;
		float rotationY = -Mathf.Atan2(
			inwardDirection.Z,
			inwardDirection.X);

		if (isCorner)
		{
			rotationY += Mathf.Pi / 4.0f + Mathf.DegToRad(
				CornerStoneRotationOffsetDegrees);
		}

		CreateStoneBorderPiece(
			stoneScene,
			decorativeTile.Position,
			coord,
			modelOffset,
			rotationY,
			isCorner ? "OuterShoreCorner" : "OuterShoreEdge");
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
		if (stoneScene == null)
			return;

		Node stoneInstance = stoneScene.Instantiate();

		if (stoneInstance is not Node3D stoneModel)
		{
			GD.PrintErr($"{stoneScene.ResourcePath}: Der Root-Node muss Node3D verwenden.");
			stoneInstance.Free();
			return;
		}

		Node3D borderPiece = new Node3D
		{
			Name = $"StoneBorder_{edgeName}_{coord.Q}_{coord.R}",
			Position = tilePosition + Vector3.Up * StoneBorderHeight,
			Rotation = new Vector3(0.0f, rotationY, 0.0f),
			Scale = new Vector3(
				HexSize * uniformScale,
				StoneBorderYScale * uniformScale,
				HexSize * uniformScale)
		};

		stoneModel.Position = modelOffset;
		borderPiece.AddChild(stoneModel);
		AddChild(borderPiece);
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
	}
}
