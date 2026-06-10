using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class PlantDefinition : Resource
{
	[Export] public PlantType Type;
	[Export] public string DisplayName = "";

	[Export] public int PlayCost = 1;
	[Export] public int WaterProduction = 0;
	[Export] public int GrowthRounds = 1;
	[Export] public int SpreadChanceDenominator = 0;

	public List<LightLevel> AllowedLightLevels = new();

	[Export] public PlantEffectType EffectType = PlantEffectType.None;

	[Export] public Texture2D CardImage;
	[Export] public PackedScene PlantScene;

	[Export(PropertyHint.MultilineText)]
	public string Description = "";

	// Godot braucht einen leeren Konstruktor für Resources.
	public PlantDefinition()
	{
	}

	// Dieser Konstruktor passt exakt zu deinem PlantDatabase.cs.
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
