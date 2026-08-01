using System;

public sealed class RoundStatisticsEntry
{
	public int RoundNumber { get; set; }
	public DateTimeOffset CompletedAt { get; set; }
	public int WaterAtRoundEnd { get; set; }
	public int LivingPlantCount { get; set; }
	public int PlantsDiedThisRound { get; set; }
	public int DeadPlantCountTotal { get; set; }
	public int CardsPlayedTotal { get; set; }
	public int MainTreeProgress { get; set; }
	public double PlayTimeSeconds { get; set; }
}
