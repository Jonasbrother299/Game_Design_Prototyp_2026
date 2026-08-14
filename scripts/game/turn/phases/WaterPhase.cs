using Godot;
using System.Collections.Generic;

public sealed class WaterPhase
{
	public WaterPhaseResult Resolve(
		TurnPhaseContext context,
		int round,
		WaterManagementMode waterManagement)
	{
		int startingWater = context.State.Water;
		WaterBalanceCalculation balance = WaterBalanceCalculator.Calculate(
			context.BoardManager,
			context.State.ActiveEvents,
			waterManagement);

		TickActiveEvents(context.State.ActiveEvents);
		context.State.Water += balance.NetChange;

		GD.Print(
			$"Water balance ({waterManagement}): events {balance.EventWaterModifier}, " +
			$"+{balance.PlantWaterProduction} production " +
			$"-{balance.PlantWaterConsumption} consumption. " +
			$"Water: {context.State.Water}");

		return new WaterPhaseResult(
			round,
			startingWater,
			context.State.Water,
			balance.EventWaterModifier,
			balance.PlantWaterProduction,
			balance.PlantWaterConsumption,
			balance.Plants);
	}

	private static void TickActiveEvents(List<ActiveGameEvent> activeEvents)
	{
		foreach (ActiveGameEvent activeEvent in activeEvents)
			activeEvent.TickDown();
	}
}
