using Godot;

public class PlantInstance
{
	public PlantDefinition Definition { get; private set; }

	public int RemainingGrowthRounds { get; private set; }

	public bool WasCreatedBySpread { get; private set; }

	public bool IsMature => RemainingGrowthRounds <= 0;

	public float GrowthProgress
	{
		get
		{
			if (Definition.GrowthRounds <= 0)
				return 1.0f;

			int completedGrowthRounds = Definition.GrowthRounds - RemainingGrowthRounds;

			return Mathf.Clamp(
				(float)completedGrowthRounds / Definition.GrowthRounds,
				0.0f,
				1.0f
			);
		}
	}

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

	public int GetWaterConsumption()
	{
		return Definition.WaterConsumption;
	}

	public int GetWaterProduction()
	{
		if (!IsMature)
			return 0;

		return Definition.WaterProduction;
	}

	public int GetWaterCostWhenPlaced()
	{
		return 0;
	}
}
