using System.Collections.Generic;

public class PlantDefinition
{
	public PlantType Type { get; private set; }

	public string DisplayName { get; private set; }

	public int PlayCost { get; private set; }

	public int WaterProduction { get; private set; }

	public int GrowthRounds { get; private set; }

	public int SpreadChanceDenominator { get; private set; }

	public List<LightLevel> AllowedLightLevels { get; private set; }

	public PlantEffectType EffectType { get; private set; }

	public PlantDefinition(
		PlantType type,
		string displayName,
		int playCost,
		int waterProduction,
		int growthRounds,
		int spreadChanceDenominator,
		List<LightLevel> allowedLightLevels,
		PlantEffectType effectType
	)
	{
		Type = type;
		DisplayName = displayName;
		PlayCost = playCost;
		WaterProduction = waterProduction;
		GrowthRounds = growthRounds;
		SpreadChanceDenominator = spreadChanceDenominator;
		AllowedLightLevels = allowedLightLevels;
		EffectType = effectType;
	}

	public bool CanGrowOnLightLevel(LightLevel lightLevel)
	{
		return AllowedLightLevels.Contains(lightLevel);
	}
}
