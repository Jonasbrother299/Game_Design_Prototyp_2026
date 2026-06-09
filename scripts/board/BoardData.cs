using System.Collections.Generic;

public class BoardData
{
	private readonly Dictionary<HexCoord, HexTileData> _tiles = new();

	public IReadOnlyDictionary<HexCoord, HexTileData> Tiles => _tiles;

	public void Generate(int radius)
	{
		_tiles.Clear();

		for (int q = -radius; q <= radius; q++)
		{
			int r1 = System.Math.Max(-radius, -q - radius);
			int r2 = System.Math.Min(radius, -q + radius);

			for (int r = r1; r <= r2; r++)
			{
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
