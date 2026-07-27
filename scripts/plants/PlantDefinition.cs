using Godot;

[GlobalClass]
public partial class PlantDefinition : Resource
{
	[Export] public PlantType Type;
	[Export] public string DisplayName = "";

	[Export] public int PlayCost = 0;
	[Export] public int WaterConsumption = 0;
	[Export] public int WaterProduction = 0;
	[Export] public int GrowthRounds = 1;
	[Export] public int GrowthStageCount = 2;
	[Export] public int SpreadChanceDenominator = 0;

	[Export] public Godot.Collections.Array<LightLevel> AllowedLightLevels = new();

	[Export] public PlantEffectType EffectType = PlantEffectType.None;
	[Export] public bool ShadeRequiresMaturity = true;

	[Export] public Texture2D CardImage;
	[Export] public PackedScene PlantScene;

	[Export(PropertyHint.MultilineText)]
	public string Description = "";

	public bool CanGrowOnLightLevel(LightLevel lightLevel)
	{
		return AllowedLightLevels.Contains(lightLevel);
	}

	public bool CanProduceShade(bool isMature)
	{
		if (EffectType != PlantEffectType.TreeShade)
			return false;

		return isMature || !ShadeRequiresMaturity;
	}
}
