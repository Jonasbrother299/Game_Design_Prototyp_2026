public class EventDefinition
{
	public GameEventType Type { get; private set; }

	public string DisplayName { get; private set; }

	public int WaterModifierPerRound { get; private set; }

	public int DurationRounds { get; private set; }

	public GameEventEffectType EffectType { get; private set; }

	public EventDefinition(
		GameEventType type,
		string displayName,
		int waterModifierPerRound,
		int durationRounds,
		GameEventEffectType effectType
	)
	{
		Type = type;
		DisplayName = displayName;
		WaterModifierPerRound = waterModifierPerRound;
		DurationRounds = durationRounds;
		EffectType = effectType;
	}
}
