using Godot;

[GlobalClass]
public partial class GameConfig : Resource
{
	public const string DefaultResourcePath =
		"res://data/balance/game_balance.tres";

	private static GameConfig _default;

	[ExportGroup("Wasser und Spielende")]
	[Export] public int StartingWater = 10;
	[Export] public int LoseWaterLimit = 0;
	[Export] public int WinWaterLimit = 50;

	[ExportGroup("Karten")]
	[Export] public int StartingHandSize = 3;
	[Export] public int MaxHandSize = 3;
	[Export] public int CardsPerTurnLimit = 3;
	[Export] public int HandDiscardAvailableFromRound = 2;

	[ExportGroup("Rundenregeln")]
	[Export] public int SpreadCheckInterval = 1;
	[Export] public int MinimumSpreadChanceDenominator = 2;
	[Export] public int EventChanceDenominator = 3;
	[Export] public int MonocultureMinimumPlantCount = 3;
	[Export] public int DeadPlantBlockedRounds = 2;

	[ExportGroup("Board")]
	[Export] public bool UseRectangularBoard = false;
	[Export] public int BoardColumns = 9;
	[Export] public int BoardRows = 7;
	[Export] public int BoardRadius = 4;

	[ExportGroup("Pflanzen")]
	[Export] public PlantDefinition Oak;
	[Export] public PlantDefinition Moss;
	[Export] public PlantDefinition Flower;
	[Export] public PlantDefinition Mushroom;
	[Export] public PlantDefinition Birch;

	[ExportGroup("Ereignisse")]
	[Export] public EventDefinition Rain;
	[Export] public EventDefinition HeavyRain;
	[Export] public EventDefinition Drought;
	[Export] public EventDefinition HeatDay;
	[Export] public EventDefinition Wind;
	[Export] public EventDefinition Pests;

	public bool EventsUnlocked { get; set; } = false;
	public bool ForceRainAsFirstEvent { get; set; } = true;
	public bool HasTriggeredFirstTutorialEvent { get; set; } = false;

	public static GameConfig LoadDefault()
	{
		if (_default != null && IsInstanceValid(_default))
			return _default;

		_default = GD.Load<GameConfig>(DefaultResourcePath);

		if (_default == null)
		{
			GD.PushError(
				$"GameConfig: Balance-Resource fehlt: {DefaultResourcePath}.");
			_default = new GameConfig();
		}

		return _default;
	}

	public PlantDefinition GetPlant(PlantType type)
	{
		return type switch
		{
			PlantType.Oak => Oak,
			PlantType.Moss => Moss,
			PlantType.Flower => Flower,
			PlantType.Mushroom => Mushroom,
			PlantType.Birch => Birch,
			_ => null
		};
	}

	public EventDefinition GetEvent(GameEventType type)
	{
		return type switch
		{
			GameEventType.Rain => Rain,
			GameEventType.HeavyRain => HeavyRain,
			GameEventType.Drought => Drought,
			GameEventType.HeatDay => HeatDay,
			GameEventType.Wind => Wind,
			GameEventType.Pests => Pests,
			_ => null
		};
	}
}
