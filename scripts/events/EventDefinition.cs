using Godot;

[GlobalClass]
public partial class EventDefinition : Resource
{
	[Export] public GameEventType Type;
	[Export] public string DisplayName = "";
	[Export] public int WaterModifierPerRound;
	[Export] public int DurationRounds = 1;
	[Export] public GameEventEffectType EffectType = GameEventEffectType.None;
	[Export] public int SeedlingDeathChanceDenominator;
	[Export] public bool SeedlingDeathRequiresSun;
	[Export] public int MatureDeathChanceDenominator;
	[Export] public bool MatureDeathRequiresMonoculture;

	[Export(PropertyHint.MultilineText)]
	public string Description = "";
}
