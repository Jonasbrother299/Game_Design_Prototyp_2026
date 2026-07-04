using System.Collections.Generic;

public static class PlantDatabase
{
	private static readonly Dictionary<PlantType, PlantDefinition> Plants = new();

	static PlantDatabase()
	{
		// Create definitions and assign card images from assets
		var oak = new PlantDefinition(
			PlantType.Oak,
			"Eiche",
			waterConsumption: 0,
			waterProduction: 0,
			growthRounds: 3,
			spreadChanceDenominator: 0,
			allowedLightLevels: new List<LightLevel> { LightLevel.Sun, LightLevel.PartialShade },
			effectType: PlantEffectType.TreeShade
		);
		oak.CardImage = Godot.GD.Load<Godot.Texture2D>("res://assets/cards/card_baum.jpeg");

		var moss = new PlantDefinition(
			PlantType.Moss,
			"Moos",
			waterConsumption: 2,
			waterProduction: 3,
			growthRounds: 2,
			spreadChanceDenominator: 3,
			allowedLightLevels: new List<LightLevel> { LightLevel.Shade, LightLevel.PartialShade },
			effectType: PlantEffectType.None
		);
		moss.CardImage = Godot.GD.Load<Godot.Texture2D>("res://assets/cards/card_moos.png");

		var flower = new PlantDefinition(
			PlantType.Flower,
			"Blume",
			waterConsumption: 2,
			waterProduction: 2,
			growthRounds: 2,
			spreadChanceDenominator: 3,
			allowedLightLevels: new List<LightLevel> { LightLevel.Sun, LightLevel.PartialShade },
			effectType: PlantEffectType.SpreadChancePlusOneForNeighbors
		);
		flower.CardImage = Godot.GD.Load<Godot.Texture2D>("res://assets/cards/card_baum.jpeg");

		var mushroom = new PlantDefinition(
			PlantType.Mushroom,
			"Pilz",
			waterConsumption: 1,
			waterProduction: 1,
			growthRounds: 3,
			spreadChanceDenominator: 3,
			allowedLightLevels: new List<LightLevel> { LightLevel.Shade, LightLevel.PartialShade },
			effectType: PlantEffectType.AdjacentPlantsProducePlusOne
		);
		mushroom.CardImage = Godot.GD.Load<Godot.Texture2D>("res://assets/cards/card_pilz.jpeg");

		var birch = new PlantDefinition(
			PlantType.Birch,
			"Birke",
			waterConsumption: 3,
			waterProduction: 0,
			growthRounds: 4,
			spreadChanceDenominator: 5,
			allowedLightLevels: new List<LightLevel> { LightLevel.Sun, LightLevel.PartialShade },
			effectType: PlantEffectType.TreeShade
		);
		birch.CardImage = Godot.GD.Load<Godot.Texture2D>("res://assets/cards/card_baum.jpeg");

		var lichen = new PlantDefinition(
			PlantType.Lichen,
			"Flechte",
			waterConsumption: 1,
			waterProduction: 2,
			growthRounds: 2,
			spreadChanceDenominator: 4,
			allowedLightLevels: new List<LightLevel> { LightLevel.Shade, LightLevel.PartialShade },
			effectType: PlantEffectType.None
		);
		lichen.CardImage = Godot.GD.Load<Godot.Texture2D>("res://assets/cards/card_flechte.jpeg");

		Plants[PlantType.Oak] = oak;
		Plants[PlantType.Moss] = moss;
		Plants[PlantType.Flower] = flower;
		Plants[PlantType.Mushroom] = mushroom;
		Plants[PlantType.Birch] = birch;
		Plants[PlantType.Lichen] = lichen;
	}

	public static PlantDefinition Get(PlantType type)
	{
		return Plants[type];
	}

	public static List<PlantDefinition> GetAll()
	{
		return new List<PlantDefinition>(Plants.Values);
	}
}
