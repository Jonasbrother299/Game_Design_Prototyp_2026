using Godot;

[GlobalClass]
public partial class EventDefinition : Resource
{
	[ExportGroup("Allgemein")]
	[Export] public GameEventType Type;
	[Export] public string DisplayName = "";
	[Export] public int SelectionWeight = 1;

	[ExportGroup("Rundenwirkung")]
	[Export] public int WaterModifierPerRound;
	[Export] public int DurationRounds = 1;
	[Export] public GameEventEffectType EffectType = GameEventEffectType.None;
	[Export] public int SpreadDenominatorReduction;

	[ExportGroup("Sterberisiko")]
	[Export] public int SeedlingDeathChanceDenominator;
	[Export] public bool SeedlingDeathRequiresSun;
	[Export] public int MatureDeathChanceDenominator;
	[Export] public bool MatureDeathRequiresMonoculture;

	[Export(PropertyHint.MultilineText)]
	public string Description = "";
}
