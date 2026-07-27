using Godot;
using System;
using System.Collections.Generic;

public static class EventDatabase
{
	private static readonly Dictionary<GameEventType, string> ResourcePaths = new()
	{
		{ GameEventType.Rain, "res://data/events/rain.tres" },
		{ GameEventType.HeavyRain, "res://data/events/heavy_rain.tres" },
		{ GameEventType.Drought, "res://data/events/drought.tres" },
		{ GameEventType.HeatDay, "res://data/events/heat_day.tres" },
		{ GameEventType.Wind, "res://data/events/wind.tres" },
		{ GameEventType.Pests, "res://data/events/pests.tres" }
	};

	private static readonly Dictionary<GameEventType, EventDefinition> Events = LoadEvents();

	public static bool IsValid => Events.Count == ResourcePaths.Count;

	public static EventDefinition Get(GameEventType type)
	{
		Events.TryGetValue(type, out EventDefinition definition);
		return definition;
	}

	public static List<EventDefinition> GetAll()
	{
		return new List<EventDefinition>(Events.Values);
	}

	private static Dictionary<GameEventType, EventDefinition> LoadEvents()
	{
		Dictionary<GameEventType, EventDefinition> events = new();

		foreach (KeyValuePair<GameEventType, string> entry in ResourcePaths)
		{
			EventDefinition definition = GD.Load<EventDefinition>(entry.Value);
			if (definition == null)
			{
				GD.PushError($"EventDatabase: Ressource fehlt oder ist ungültig: {entry.Value}");
				continue;
			}

			if (!ValidateEvent(definition, entry.Key, entry.Value))
			{
				continue;
			}

			events[entry.Key] = definition;
		}

		return events;
	}

	private static bool ValidateEvent(
		EventDefinition definition,
		GameEventType expectedType,
		string resourcePath)
	{
		List<string> errors = new();

		if (definition.Type != expectedType)
			errors.Add($"Typ ist {definition.Type}, erwartet wurde {expectedType}");

		if (!Enum.IsDefined(definition.Type) || definition.Type == GameEventType.None)
			errors.Add("Ereignistyp ist ungültig");

		if (string.IsNullOrWhiteSpace(definition.DisplayName))
			errors.Add("Anzeigename fehlt");

		if (string.IsNullOrWhiteSpace(definition.Description))
			errors.Add("Beschreibung fehlt");

		if (definition.DurationRounds <= 0)
			errors.Add("Dauer muss größer als 0 sein");

		if (!Enum.IsDefined(definition.EffectType))
			errors.Add($"Ereigniseffekt {definition.EffectType} ist ungültig");

		ValidateDeathRisk(definition, errors);
		ValidateEventSemantics(definition, errors);

		foreach (string error in errors)
		{
			GD.PushError($"EventDatabase: {resourcePath}: {error}.");
		}

		return errors.Count == 0;
	}

	private static void ValidateDeathRisk(
		EventDefinition definition,
		List<string> errors)
	{
		bool hasDeathEffect = definition.EffectType == GameEventEffectType.PlantDeathRisk;

		if (!IsValidChanceDenominator(definition.SeedlingDeathChanceDenominator))
			errors.Add("Setzling-Sterberisiko muss 0 oder mindestens 2 sein");

		if (!IsValidChanceDenominator(definition.MatureDeathChanceDenominator))
			errors.Add("Sterberisiko ausgewachsener Pflanzen muss 0 oder mindestens 2 sein");

		if (hasDeathEffect &&
			definition.SeedlingDeathChanceDenominator == 0 &&
			definition.MatureDeathChanceDenominator == 0)
		{
			errors.Add("Ereignis mit Sterberisiko benötigt mindestens eine Sterbechance");
		}

		if (!hasDeathEffect &&
			(definition.SeedlingDeathChanceDenominator > 0 ||
			 definition.MatureDeathChanceDenominator > 0 ||
			 definition.SeedlingDeathRequiresSun ||
			 definition.MatureDeathRequiresMonoculture))
		{
			errors.Add("Sterberegeln sind nur für Ereignisse mit PlantDeathRisk zulässig");
		}

		if (definition.SeedlingDeathRequiresSun &&
			definition.SeedlingDeathChanceDenominator == 0)
		{
			errors.Add("Sonnenbedingung benötigt eine Setzling-Sterbechance");
		}

		if (definition.MatureDeathRequiresMonoculture &&
			definition.MatureDeathChanceDenominator == 0)
		{
			errors.Add("Monokulturbedingung benötigt eine Sterbechance für ausgewachsene Pflanzen");
		}
	}

	private static void ValidateEventSemantics(
		EventDefinition definition,
		List<string> errors)
	{
		switch (definition.Type)
		{
			case GameEventType.Rain:
				if (definition.WaterModifierPerRound <= 0)
					errors.Add("Regen benötigt einen positiven Wassermodifikator");
				break;

			case GameEventType.HeavyRain:
				if (definition.WaterModifierPerRound <= 0 ||
					definition.EffectType != GameEventEffectType.PlantDeathRisk)
				{
					errors.Add("Starkregen benötigt positiven Wasserwert und PlantDeathRisk");
				}
				break;

			case GameEventType.Drought:
			case GameEventType.HeatDay:
				if (definition.WaterModifierPerRound >= 0)
					errors.Add("Dürre und Hitzetag benötigen einen negativen Wassermodifikator");
				break;

			case GameEventType.Wind:
				if (definition.EffectType != GameEventEffectType.IncreaseSpreadChance)
					errors.Add("Wind benötigt IncreaseSpreadChance");
				break;

			case GameEventType.Pests:
				if (definition.EffectType != GameEventEffectType.PlantDeathRisk)
					errors.Add("Schädlinge benötigen PlantDeathRisk");
				break;
		}
	}

	private static bool IsValidChanceDenominator(int denominator)
	{
		return denominator == 0 || denominator >= 2;
	}
}
