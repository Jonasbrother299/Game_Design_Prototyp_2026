using System.Collections.Generic;

public class BoardData
{
	private readonly Dictionary<HexCoord, HexTileData> _tiles = new();

	public IReadOnlyDictionary<HexCoord, HexTileData> Tiles => _tiles;

	public void Generate(int radius, HexCoord center)
	{
		_tiles.Clear();

		for (int q = -radius; q <= radius; q++)
		{
			int r1 = System.Math.Max(-radius, -q - radius);
			int r2 = System.Math.Min(radius, -q + radius);

			for (int r = r1; r <= r2; r++)
			{
				HexCoord coord = new HexCoord(
					q + center.Q,
					r + center.R);
				_tiles.Add(coord, new HexTileData(coord));
			}
		}
	}

	public void GenerateRectangle(int columns, int rows)
	{
		_tiles.Clear();

		int safeColumns = System.Math.Max(columns, 1);
		int safeRows = System.Math.Max(rows, 1);
		int firstQ = -(safeColumns / 2);

		for (int column = 0; column < safeColumns; column++)
		{
			int q = firstQ + column;
			int columnOffset = (int)System.Math.Floor(q / 2.0);

			for (int row = 0; row < safeRows; row++)
			{
				int r = row - columnOffset;
				HexCoord coord = new HexCoord(q, r);
				_tiles.Add(coord, new HexTileData(coord));
			}
		}
	}

	public HexTileData GetTile(HexCoord coord)
	{
		if (_tiles.TryGetValue(coord, out HexTileData tile))
			return tile;

		return null;
	}

	public List<HexTileData> GetNeighbors(HexCoord coord)
	{
		List<HexTileData> neighbors = new();

		for (int i = 0; i < HexDirections.Directions.Length; i++)
		{
			HexCoord neighborCoord = HexDirections.GetNeighbor(coord, i);
			HexTileData neighborTile = GetTile(neighborCoord);

			if (neighborTile != null)
			{
				neighbors.Add(neighborTile);
			}
		}

		return neighbors;
	}
}
