public class PlantInstance
{
	public PlantDefinition Definition { get; private set; }

	public int RemainingGrowthRounds { get; private set; }

	public bool WasCreatedBySpread { get; private set; }

	public bool IsMature => RemainingGrowthRounds <= 0;

	public PlantInstance(PlantDefinition definition, bool wasCreatedBySpread)
	{
		Definition = definition;
		WasCreatedBySpread = wasCreatedBySpread;
		RemainingGrowthRounds = definition.GrowthRounds;
	}

	public void GrowOneRound()
	{
		if (RemainingGrowthRounds > 0)
		{
			RemainingGrowthRounds--;
		}
	}

	public int GetWaterProduction()
	{
		if (!IsMature)
			return 0;

		return Definition.WaterProduction;
	}

	public int GetWaterCostWhenPlaced()
	{
		if (WasCreatedBySpread)
			return 0;

		return Definition.PlayCost;
	}
}
