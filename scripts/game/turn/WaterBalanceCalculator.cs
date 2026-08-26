using System;
using System.Collections.Generic;

public enum WaterManagementMode
{
	CurrentAllPlants,
	GrowthOnly,
	MatureOnly
}

public sealed class WaterBalanceCalculation
{
	public int EventWaterModifier { get; }
	public int PlantWaterProduction { get; }
	public int PlantWaterConsumption { get; }
	public int DisplayedProduction =>
		PlantWaterProduction + Math.Max(EventWaterModifier, 0);
	public int DisplayedConsumption =>
		PlantWaterConsumption + Math.Max(-EventWaterModifier, 0);
	public int NetChange => DisplayedProduction - DisplayedConsumption;
	public List<PlantWaterResult> Plants { get; }

	public WaterBalanceCalculation(
		int eventWaterModifier,
		int plantWaterProduction,
		int plantWaterConsumption,
		List<PlantWaterResult> plants)
	{
		EventWaterModifier = eventWaterModifier;
		PlantWaterProduction = plantWaterProduction;
		PlantWaterConsumption = plantWaterConsumption;
		Plants = plants;
	}
}

public static class WaterBalanceCalculator
{
	public static WaterBalanceCalculation Calculate(
		BoardManager boardManager,
		IReadOnlyList<ActiveGameEvent> activeEvents,
		WaterManagementMode waterManagement = WaterManagementMode.CurrentAllPlants)
	{
		int eventWaterModifier = CalculateEventModifier(activeEvents);
		int totalProduction = 0;
		int totalConsumption = 0;
		List<PlantWaterResult> plants = new();

		if (boardManager == null)
		{
			return new WaterBalanceCalculation(
				eventWaterModifier,
				totalProduction,
				totalConsumption,
				plants);
		}

		foreach (HexTileData tile in boardManager.BoardData.Tiles.Values)
		{
			if (tile.Plant == null)
				continue;

			int consumption = GetWaterConsumption(tile.Plant, waterManagement);
			int production = tile.Plant.GetWaterProduction();
			int adjacentBonus = GetAdjacentProductionBonus(boardManager, tile);

			totalConsumption += consumption;
			totalProduction += production + adjacentBonus;

			plants.Add(new PlantWaterResult(
				tile.Coord,
				tile.Plant.Definition.Type,
				production,
				consumption,
				adjacentBonus));
		}

		return new WaterBalanceCalculation(
			eventWaterModifier,
			totalProduction,
			totalConsumption,
			plants);
	}

	private static int GetWaterConsumption(
		PlantInstance plant,
		WaterManagementMode waterManagement)
	{
		int consumption = plant.GetWaterConsumption();

		return waterManagement switch
		{
			WaterManagementMode.GrowthOnly => plant.IsMature ? 0 : consumption,
			WaterManagementMode.MatureOnly => plant.IsMature ? consumption : 0,
			_ => consumption
		};
	}

	private static int CalculateEventModifier(
		IReadOnlyList<ActiveGameEvent> activeEvents)
	{
		if (activeEvents == null)
			return 0;

		int eventWaterModifier = 0;

		foreach (ActiveGameEvent activeEvent in activeEvents)
		{
			if (activeEvent != null)
				eventWaterModifier += activeEvent.ApplyWaterModifier();
		}

		return eventWaterModifier;
	}

	private static int GetAdjacentProductionBonus(
		BoardManager boardManager,
		HexTileData tile)
	{
		if (!tile.Plant.IsMature ||
			tile.Plant.Definition.Type is PlantType.Oak or PlantType.Birch)
			return 0;

		foreach (HexTileData neighbor in boardManager.GetNeighborData(tile.Coord))
		{
			if (neighbor.Plant == null || !neighbor.Plant.IsMature)
				continue;

			if (neighbor.Plant.Definition.EffectType ==
				PlantEffectType.AdjacentPlantsProducePlusOne)
			{
				return System.Math.Max(
					neighbor.Plant.Definition.AdjacentWaterProductionBonus,
					0);
			}
		}

		return 0;
	}
}
