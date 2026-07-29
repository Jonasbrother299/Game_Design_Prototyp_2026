using Godot;

[GlobalClass]
public partial class PlantDefinition : Resource
{
	[ExportGroup("Allgemein")]
	[Export] public PlantType Type;
	[Export] public string DisplayName = "";

	[ExportGroup("Karten")]
	[Export] public int PlayCost = 0;
	[Export] public int StartingHandCopies = 0;
	[Export] public int DrawWeight = 1;

	[ExportGroup("Wasser und Wachstum")]
	[Export] public int WaterConsumption = 0;
	[Export] public int WaterProduction = 0;
	[Export] public int GrowthRounds = 1;
	[Export] public int GrowthStageCount = 2;
	[Export] public int SpreadChanceDenominator = 0;
	[Export] public int EventDeathResistancePerGrowthStage = 0;

	[Export] public Godot.Collections.Array<LightLevel> AllowedLightLevels = new();

	[ExportGroup("Effekte")]
	[Export] public PlantEffectType EffectType = PlantEffectType.None;
	[Export] public bool ShadeRequiresMaturity = true;
	[Export] public int AdjacentWaterProductionBonus = 0;
	[Export] public int NeighborSpreadDenominatorReduction = 0;

	[ExportGroup("Darstellung")]
	[Export] public Texture2D CardImage;
	[Export] public PackedScene PlantScene;
	[Export] public Godot.Collections.Array<PackedScene> GrowthStageScenes = new();

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
