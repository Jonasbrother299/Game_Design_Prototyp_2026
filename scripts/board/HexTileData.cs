
public class HexTileData
{
	public HexCoord Coord { get; private set; }

	public LightLevel LightLevel { get; set; } = LightLevel.Sun;

	public PlantInstance Plant { get; private set; }
	public PlantInstance DeadPlant { get; private set; }

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
		DeadPlant = null;
		BlockedRounds = 0;
	}

	public void RemovePlantAndBlockTile(int rounds)
	{
		if (Plant?.Definition.Type != PlantType.Oak)
			DeadPlant = Plant;

		Plant = null;
		BlockedRounds = System.Math.Max(rounds, 0);

		if (BlockedRounds == 0)
			DeadPlant = null;
	}

	public void RemovePlant()
	{
		Plant = null;
		DeadPlant = null;
		BlockedRounds = 0;
	}

	public bool TickBlockedRound()
	{
		if (BlockedRounds <= 0)
			return false;

		BlockedRounds--;

		if (BlockedRounds > 0)
			return false;

		DeadPlant = null;
		return true;
	}
}
