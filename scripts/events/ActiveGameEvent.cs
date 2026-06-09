public class ActiveGameEvent
{
	public EventDefinition Definition { get; private set; }

	public int RemainingRounds { get; private set; }

	public bool IsFinished => RemainingRounds <= 0;

	public ActiveGameEvent(EventDefinition definition)
	{
		Definition = definition;
		RemainingRounds = definition.DurationRounds;
	}

	public int ApplyWaterModifier()
	{
		return Definition.WaterModifierPerRound;
	}

	public void TickDown()
	{
		RemainingRounds--;
	}
}
