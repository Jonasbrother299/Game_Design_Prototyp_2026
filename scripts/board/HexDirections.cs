public static class HexDirections
{
	public static readonly HexCoord[] Directions =
	{
		new HexCoord(1, 0),
		new HexCoord(1, -1),
		new HexCoord(0, -1),
		new HexCoord(-1, 0),
		new HexCoord(-1, 1),
		new HexCoord(0, 1)
	};

	public static HexCoord GetNeighbor(HexCoord coord, int directionIndex)
	{
		HexCoord direction = Directions[directionIndex];

		return new HexCoord(
			coord.Q + direction.Q,
			coord.R + direction.R
		);
	}
}
