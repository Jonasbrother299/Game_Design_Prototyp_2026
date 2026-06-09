using System.Collections.Generic;

public static class PlantDatabase
{
	private static readonly Dictionary<PlantType, PlantDefinition> Plants = new()
	{
		{
			PlantType.Oak,
			new PlantDefinition(
				PlantType.Oak,
				"Eiche",
				playCost: 0,
				waterProduction: 0,
				growthRounds: 0,
				spreadChanceDenominator: 0,
				allowedLightLevels: new List<LightLevel>
				{
					LightLevel.Sun,
					LightLevel.PartialShade
				},
				effectType: PlantEffectType.TreeShade
			)
		},
		{
			PlantType.Moss,
			new PlantDefinition(
				PlantType.Moss,
				"Moos",
				playCost: 2,
				waterProduction: 3,
				growthRounds: 2,
				spreadChanceDenominator: 3,
				allowedLightLevels: new List<LightLevel>
				{
					LightLevel.Shade,
					LightLevel.PartialShade
				},
				effectType: PlantEffectType.None
			)
		},
		{
			PlantType.Flower,
			new PlantDefinition(
				PlantType.Flower,
				"Blume",
				playCost: 2,
				waterProduction: 2,
				growthRounds: 2,
				spreadChanceDenominator: 3,
				allowedLightLevels: new List<LightLevel>
				{
					LightLevel.Sun,
					LightLevel.PartialShade
				},
				effectType: PlantEffectType.SpreadChancePlusOneForNeighbors
			)
		},
		{
			PlantType.Mushroom,
			new PlantDefinition(
				PlantType.Mushroom,
				"Pilz",
				playCost: 1,
				waterProduction: 1,
				growthRounds: 3,
				spreadChanceDenominator: 3,
				allowedLightLevels: new List<LightLevel>
				{
					LightLevel.Shade,
					LightLevel.PartialShade
				},
				effectType: PlantEffectType.AdjacentPlantsProducePlusOne
			)
		},
		{
			PlantType.Birch,
			new PlantDefinition(
				PlantType.Birch,
				"Birke",
				playCost: 3,
				waterProduction: 0,
				growthRounds: 4,
				spreadChanceDenominator: 5,
				allowedLightLevels: new List<LightLevel>
				{
					LightLevel.Sun,
					LightLevel.PartialShade
				},
				effectType: PlantEffectType.TreeShade
			)
		}
	};

	public static PlantDefinition Get(PlantType type)
	{
		return Plants[type];
	}

	public static List<PlantDefinition> GetAll()
	{
		return new List<PlantDefinition>(Plants.Values);
	}
}
