using Godot;
using System.Collections.Generic;

public partial class BoardManager : Node3D
{
	[ExportGroup("Balance")]
	[Export] public GameConfig Balance;

	[ExportGroup("Board Visual")]
	[Export] public PackedScene HexTileScene;
	[Export] public float HexSize = 1.0f;

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
		new Color(0.055f, 0.08f, 0.045f, 0.66f);

	[Export(PropertyHint.Range, "1.0,7.0,0.1")]
	public float StartingOakShadowSize = 5.4f;

	[Export] public Vector2 StartingOakShadowOffset =
		new Vector2(0.0f, 0.45f);

	[Export(PropertyHint.Range, "0.8,4.0,0.1")]
	public float BirchShadowSize = 2.8f;

	[Export] public Vector2 BirchShadowOffset =
		new Vector2(0.0f, 0.18f);

	[ExportGroup("Light Level Visuals")]
	[Export] public Color SunTileTint = Colors.White;
	[Export] public Color PartialShadeTileTint =
		new Color(0.82f, 0.91f, 0.80f);
	[Export] public Color ShadeTileTint =
		new Color(0.62f, 0.74f, 0.64f);

	public BoardData BoardData { get; private set; } = new BoardData();

	private readonly Dictionary<HexCoord, HexTile> _tileViews = new();
	private Vector3 _boardWorldCenter = Vector3.Zero;

	public override void _Ready()
	{
		Balance ??= GameConfig.LoadDefault();

		if (HexTileScene == null)
		{
			HexTileScene = GD.Load<PackedScene>("res://scenes/board/tiles/HexTile2.tscn");
		}

		GenerateBoard();
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

	private void CreateTileView(HexTileData tileData)
	{
		if (HexTileScene == null)
		{
			GD.PrintErr("HexTileScene missing. Create scenes/board/HexTile.tscn first.");
			return;
		}

		HexTile tileView = HexTileScene.Instantiate<HexTile>();

		tileView.Position = HexToWorld(tileData.Coord, HexSize);
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
		tileView.ConfigureLightVisuals(
			SunTileTint,
			PartialShadeTileTint,
			ShadeTileTint);
		tileView.Setup(tileData);

		AddChild(tileView);

		_tileViews.Add(tileData.Coord, tileView);
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
