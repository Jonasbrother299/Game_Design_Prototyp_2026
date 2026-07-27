public class GameConfig
{
	public int StartingWater { get; set; } = 10;

	public int LoseWaterLimit { get; set; } = 0;
	public int WinWaterLimit { get; set; } = 50;

	public int StartingHandSize { get; set; } = 3;
	public int MaxHandSize { get; set; } = 3;

	public int CardsPerTurnLimit { get; set; } = 3;
	public int CardsDrawnPerRound { get; set; } = 1;

	public int SpreadCheckInterval { get; set; } = 1;
	public int EventChanceDenominator { get; set; } = 3;
	public int MonocultureMinimumPlantCount { get; set; } = 3;
	public int DeadPlantBlockedRounds { get; set; } = 2;
}
