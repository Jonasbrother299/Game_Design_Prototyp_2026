using Godot;
using System.Collections.Generic;

public partial class BoardManager : Node3D
{
	[Export] public PackedScene HexTileScene;
	[Export] public int Radius = 2;
	[Export] public float HexSize = 1.0f;

	public BoardData BoardData { get; private set; } = new BoardData();

	private readonly Dictionary<HexCoord, HexTile> _tileViews = new();

	public override void _Ready()
	{
		if (HexTileScene == null)
		{
			HexTileScene = GD.Load<PackedScene>("res://scenes/board/HexTile.tscn");
		}

		GenerateBoard();
	}

	public void GenerateBoard()
	{
		ClearBoard();

		BoardData.Generate(Radius);

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

			if (tile.Plant.Definition.EffectType != PlantEffectType.TreeShade)
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
		tileView.Setup(tileData);

		AddChild(tileView);

		_tileViews.Add(tileData.Coord, tileView);
	}

	private Vector3 HexToWorld(HexCoord coord, float size)
	{
		float x = size * 1.5f * coord.Q;
		float z = size * Mathf.Sqrt(3.0f) * (coord.R + coord.Q / 2.0f);

		return new Vector3(x, 0.0f, z);
	}

	private void ClearBoard()
	{
		foreach (Node child in GetChildren())
		{
			child.QueueFree();
		}

		_tileViews.Clear();
	}
}
