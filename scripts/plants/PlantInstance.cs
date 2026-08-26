using Godot;

public class PlantInstance
{
	public PlantDefinition Definition { get; private set; }

	public int RemainingGrowthRounds { get; private set; }

	public bool WasCreatedBySpread { get; private set; }

	public bool IsMature => RemainingGrowthRounds <= 0;

	public int VisualGrowthStage
	{
		get
		{
			int stageCount = Mathf.Max(Definition.GrowthStageCount, 2);

			if (IsMature)
				return stageCount;

			int stage = Mathf.FloorToInt(GrowthProgress * (stageCount - 1)) + 1;
			return Mathf.Clamp(stage, 1, stageCount - 1);
		}
	}

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
		bool isSpreadTree =
			Definition.Type == PlantType.Oak ||
			Definition.Type == PlantType.Birch;
		bool isGrowingSpreadMoss =
			Definition.Type == PlantType.Moss && !IsMature;

		if (WasCreatedBySpread && (isSpreadTree || isGrowingSpreadMoss))
		{
			return 0;
		}

		return Definition.WaterConsumption;
	}

	public int GetWaterProduction()
	{
		if (!IsMature)
			return 0;

		return Definition.WaterProduction;
	}

}
