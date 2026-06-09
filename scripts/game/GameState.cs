using System.Collections.Generic;

public class GameState
{
	public int CurrentRound { get; set; } = 1;

	public int Water { get; set; }

	public int CardsPlayedThisTurn { get; set; } = 0;

	public bool HasWon { get; set; } = false;
	public bool HasLost { get; set; } = false;

	public List<CardData> HandCards { get; private set; } = new();
	public List<ActiveGameEvent> ActiveEvents { get; private set; } = new();

	public bool IsGameOver => HasWon || HasLost;

	public GameState(GameConfig config)
	{
		Water = config.StartingWater;
	}

	public void CheckWinLose(GameConfig config)
	{
		if (Water <= config.LoseWaterLimit)
		{
			Water = config.LoseWaterLimit;
			HasLost = true;
		}

		if (Water >= config.WinWaterLimit)
		{
			Water = config.WinWaterLimit;
			HasWon = true;
		}
	}
}
