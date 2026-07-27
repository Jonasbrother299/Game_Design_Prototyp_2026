using Godot;
using System.Collections.Generic;

public sealed class WaterPhase
{
	public WaterPhaseResult Resolve(TurnPhaseContext context, int round)
	{
		int startingWater = context.State.Water;
		int eventWaterModifier = ApplyActiveEventWater(context.State.ActiveEvents);
		int totalProduction = 0;
		int totalConsumption = 0;
		List<PlantWaterResult> plants = new();

		foreach (HexTileData tile in context.BoardManager.BoardData.Tiles.Values)
		{
			if (tile.Plant == null)
				continue;

			int consumption = tile.Plant.GetWaterConsumption();
			int production = tile.Plant.GetWaterProduction();
			int adjacentBonus = tile.Plant.IsMature
				? GetAdjacentProductionBonus(context.BoardManager, tile)
				: 0;

			totalConsumption += consumption;
			totalProduction += production + adjacentBonus;

			plants.Add(new PlantWaterResult(
				tile.Coord,
				tile.Plant.Definition.Type,
				production,
				consumption,
				adjacentBonus));
		}

		context.State.Water += eventWaterModifier + totalProduction - totalConsumption;

		GD.Print(
			$"Water balance: events {eventWaterModifier}, +{totalProduction} production " +
			$"-{totalConsumption} consumption. Water: {context.State.Water}");

		return new WaterPhaseResult(
			round,
			startingWater,
			context.State.Water,
			eventWaterModifier,
			totalProduction,
			totalConsumption,
			plants);
	}

	private static int ApplyActiveEventWater(List<ActiveGameEvent> activeEvents)
	{
		int waterModifier = 0;

		foreach (ActiveGameEvent activeEvent in activeEvents)
		{
			waterModifier += activeEvent.ApplyWaterModifier();
			activeEvent.TickDown();
		}

		return waterModifier;
	}

	private static int GetAdjacentProductionBonus(
		BoardManager boardManager,
		HexTileData tile)
	{
		foreach (HexTileData neighbor in boardManager.GetNeighborData(tile.Coord))
		{
			if (neighbor.Plant == null || !neighbor.Plant.IsMature)
				continue;

			if (neighbor.Plant.Definition.EffectType ==
				PlantEffectType.AdjacentPlantsProducePlusOne)
			{
				return 1;
			}
		}

		return 0;
	}
}
