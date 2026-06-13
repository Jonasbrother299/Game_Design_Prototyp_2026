
public class HexTileData
{
	public HexCoord Coord { get; private set; }

	public LightLevel LightLevel { get; set; } = LightLevel.Sun;

	public PlantInstance Plant { get; private set; }

	public int BlockedRounds { get; private set; } = 0;

	public bool IsOccupied => Plant != null;

	public bool IsBlocked => BlockedRounds > 0;

	public HexTileData(HexCoord coord)
	{
		Coord = coord;
	}

	public bool CanPlacePlant(PlantDefinition plantDefinition)
	{
		if (IsOccupied)
			return false;

		if (IsBlocked)
			return false;

		if (!plantDefinition.CanGrowOnLightLevel(LightLevel))
			return false;

		return true;
	}

	public void PlacePlant(PlantInstance plant)
	{
		Plant = plant;
	}

	public void RemovePlantAndBlockTile(int rounds)
	{
		Plant = null;
		BlockedRounds = rounds;
	}

	public void TickBlockedRound()
	{
		if (BlockedRounds > 0)
		{
			BlockedRounds--;
		}
	}
}
