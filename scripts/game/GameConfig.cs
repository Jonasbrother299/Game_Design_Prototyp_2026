public class GameConfig
{
	public int StartingWater { get; set; } = 6;

	public int LoseWaterLimit { get; set; } = 0;
	public int WinWaterLimit { get; set; } = 50;

	public int StartingHandSize { get; set; } = 3;
	public int MaxHandSize { get; set; } = 5;

	public int CardsPerTurnLimit { get; set; } = 3;
	public int CardsDrawnPerRound { get; set; } = 1;

	public int SpreadCheckInterval { get; set; } = 2;
}
