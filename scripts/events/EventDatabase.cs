using System.Collections.Generic;

public static class EventDatabase
{
	private static readonly Dictionary<GameEventType, EventDefinition> Events = new()
	{
		{
			GameEventType.Rain,
			new EventDefinition(
				GameEventType.Rain,
				"Regen",
				waterModifierPerRound: 3,
				durationRounds: 2,
				effectType: GameEventEffectType.None
			)
		},
		{
			GameEventType.HeavyRain,
			new EventDefinition(
				GameEventType.HeavyRain,
				"Starkregen",
				waterModifierPerRound: 5,
				durationRounds: 2,
				effectType: GameEventEffectType.PlantDeathRisk
			)
		},
		{
			GameEventType.Drought,
			new EventDefinition(
				GameEventType.Drought,
				"Dürre",
				waterModifierPerRound: -4,
				durationRounds: 2,
				effectType: GameEventEffectType.None
			)
		},
		{
			GameEventType.HeatDay,
			new EventDefinition(
				GameEventType.HeatDay,
				"Hitzetag",
				waterModifierPerRound: -3,
				durationRounds: 1,
				effectType: GameEventEffectType.None
			)
		},
		{
			GameEventType.Wind,
			new EventDefinition(
				GameEventType.Wind,
				"Wind",
				waterModifierPerRound: 0,
				durationRounds: 1,
				effectType: GameEventEffectType.IncreaseSpreadChance
			)
		},
		{
			GameEventType.Pests,
			new EventDefinition(
				GameEventType.Pests,
				"Schädlinge",
				waterModifierPerRound: 0,
				durationRounds: 1,
				effectType: GameEventEffectType.PlantDeathRisk
			)
		}
	};

	public static EventDefinition Get(GameEventType type)
	{
		return Events[type];
	}

	public static List<EventDefinition> GetAll()
	{
		return new List<EventDefinition>(Events.Values);
	}
}
