using Godot;
using System;
using System.Collections.Generic;

public static class PlantDatabase
{
	private static readonly Dictionary<PlantType, string> ResourcePaths = new()
	{
		{ PlantType.Oak, "res://data/plants/oak.tres" },
		{ PlantType.Moss, "res://data/plants/moss.tres" },
		{ PlantType.Flower, "res://data/plants/flower.tres" },
		{ PlantType.Mushroom, "res://data/plants/mushroom.tres" },
		{ PlantType.Birch, "res://data/plants/birch.tres" }
	};

	private static readonly Dictionary<PlantType, PlantDefinition> Plants = LoadPlants();

	public static bool IsValid => Plants.Count == ResourcePaths.Count;

	public static PlantDefinition Get(PlantType type)
	{
		Plants.TryGetValue(type, out PlantDefinition plant);
		return plant;
	}

	public static List<PlantDefinition> GetAll()
	{
		return new List<PlantDefinition>(Plants.Values);
	}

	private static Dictionary<PlantType, PlantDefinition> LoadPlants()
	{
		Dictionary<PlantType, PlantDefinition> plants = new();

		foreach (KeyValuePair<PlantType, string> entry in ResourcePaths)
		{
			PlantDefinition plant = GD.Load<PlantDefinition>(entry.Value);
			if (plant == null)
			{
				GD.PushError($"PlantDatabase: Ressource fehlt oder ist ungültig: {entry.Value}");
				continue;
			}

			if (!ValidatePlant(plant, entry.Key, entry.Value))
			{
				continue;
			}

			plants[entry.Key] = plant;
		}

		return plants;
	}

	private static bool ValidatePlant(
		PlantDefinition plant,
		PlantType expectedType,
		string resourcePath)
	{
		List<string> errors = new();

		if (plant.Type != expectedType)
			errors.Add($"Typ ist {plant.Type}, erwartet wurde {expectedType}");

		if (!Enum.IsDefined(plant.Type) || plant.Type == PlantType.None)
			errors.Add("Pflanzentyp ist ungültig");

		if (string.IsNullOrWhiteSpace(plant.DisplayName))
			errors.Add("Anzeigename fehlt");

		if (plant.PlayCost < 0)
			errors.Add("Spielkosten dürfen nicht negativ sein");

		if (plant.WaterConsumption < 0 || plant.WaterProduction < 0)
			errors.Add("Wasserwerte dürfen nicht negativ sein");

		if (plant.GrowthRounds <= 0)
			errors.Add("Wachstumsdauer muss größer als 0 sein");

		if (plant.GrowthStageCount != plant.GrowthRounds + 1)
		{
			errors.Add(
				$"Wachstumsstufen müssen Wachstumsrunden + 1 entsprechen " +
				$"({plant.GrowthStageCount} statt {plant.GrowthRounds + 1})");
		}

		if (plant.SpreadChanceDenominator == 1 || plant.SpreadChanceDenominator < 0)
			errors.Add("Ausbreitungsnenner muss 0 oder mindestens 2 sein");

		if (plant.AllowedLightLevels == null || plant.AllowedLightLevels.Count == 0)
		{
			errors.Add("Mindestens ein erlaubtes Lichtlevel fehlt");
		}
		else
		{
			HashSet<LightLevel> lightLevels = new();
			foreach (LightLevel lightLevel in plant.AllowedLightLevels)
			{
				if (!Enum.IsDefined(lightLevel))
					errors.Add($"Lichtlevel {lightLevel} ist ungültig");

				if (!lightLevels.Add(lightLevel))
					errors.Add($"Lichtlevel {lightLevel} ist doppelt eingetragen");
			}
		}

		if (!Enum.IsDefined(plant.EffectType))
			errors.Add($"Pflanzeneffekt {plant.EffectType} ist ungültig");

		if (plant.EffectType != PlantEffectType.TreeShade &&
			!plant.ShadeRequiresMaturity)
		{
			errors.Add("ShadeRequiresMaturity ist nur für schattenspendende Pflanzen zulässig");
		}

		if (plant.Type != PlantType.Oak && plant.CardImage == null)
			errors.Add("Kartenbild fehlt");

		if (plant.Type == PlantType.Mushroom)
		{
			if (plant.GrowthStageScenes == null ||
				plant.GrowthStageScenes.Count != 3)
			{
				errors.Add("Für den Pilz müssen genau drei Wachstumsmodelle hinterlegt sein");
			}
			else
			{
				for (int i = 0; i < plant.GrowthStageScenes.Count; i++)
				{
					if (plant.GrowthStageScenes[i] == null)
						errors.Add($"Pilz-Wachstumsmodell {i + 1} fehlt");
				}
			}
		}

		foreach (string error in errors)
		{
			GD.PushError($"PlantDatabase: {resourcePath}: {error}.");
		}

		return errors.Count == 0;
	}
}
