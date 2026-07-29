using Godot;
using System.Collections.Generic;

public sealed class SpreadPhase
{
	public SpreadPhaseResult Resolve(TurnPhaseContext context, int round)
	{
		List<PlantSpreadResult> spreads = new();

		if (ShouldCheckSpread(context))
		{
			ApplySpread(context, spreads);
		}

		return new SpreadPhaseResult(round, spreads);
	}

	private static bool ShouldCheckSpread(TurnPhaseContext context)
	{
		return context.Config.SpreadCheckInterval > 0 &&
			context.State.CurrentRound % context.Config.SpreadCheckInterval == 0;
	}

	private static void ApplySpread(
		TurnPhaseContext context,
		List<PlantSpreadResult> spreads)
	{
		List<HexTileData> spreadingPlants = new();

		foreach (HexTileData tile in context.BoardManager.BoardData.Tiles.Values)
		{
			if (CanPlantSpread(tile))
			{
				spreadingPlants.Add(tile);
			}
		}

		foreach (HexTileData sourceTile in spreadingPlants)
		{
			PlantSpreadResult spread = TrySpreadFromTile(context, sourceTile);
			if (spread != null)
			{
				spreads.Add(spread);
			}
		}
	}

	private static bool CanPlantSpread(HexTileData tile)
	{
		if (tile?.Plant == null || !tile.Plant.IsMature)
			return false;

		return tile.Plant.Definition.SpreadChanceDenominator > 0;
	}

	private static PlantSpreadResult TrySpreadFromTile(
		TurnPhaseContext context,
		HexTileData sourceTile)
	{
		PlantDefinition definition = sourceTile.Plant.Definition;
		int denominator = GetModifiedSpreadDenominator(context, sourceTile);

		if (context.Random.RandiRange(1, denominator) != 1)
			return null;

		List<HexTileData> possibleTiles = GetValidSpreadTargets(
			context.BoardManager,
			sourceTile,
			definition);

		if (possibleTiles.Count == 0)
			return null;

		int randomIndex = context.Random.RandiRange(0, possibleTiles.Count - 1);
		HexTileData targetTile = possibleTiles[randomIndex];
		PlantInstance newPlant = new PlantInstance(definition, wasCreatedBySpread: true);

		targetTile.PlacePlant(newPlant);
		context.BoardManager.GetTileView(targetTile.Coord)?.UpdateVisualState();

		GD.Print(
			$"{definition.DisplayName} spread from {sourceTile.Coord} to {targetTile.Coord}");

		return new PlantSpreadResult(
			definition.Type,
			sourceTile.Coord,
			targetTile.Coord);
	}

	private static int GetModifiedSpreadDenominator(
		TurnPhaseContext context,
		HexTileData sourceTile)
	{
		int denominator = sourceTile.Plant.Definition.SpreadChanceDenominator;

		denominator -= GetNeighborSpreadReduction(
			context.BoardManager,
			sourceTile);

		denominator -= GetEventSpreadReduction(context.State.ActiveEvents);

		return Mathf.Max(
			denominator,
			System.Math.Max(
				context.Config.MinimumSpreadChanceDenominator,
				2));
	}

	private static int GetNeighborSpreadReduction(
		BoardManager boardManager,
		HexTileData sourceTile)
	{
		if (sourceTile.Plant.Definition.Type == PlantType.Flower)
			return 0;

		int reduction = 0;

		foreach (HexTileData neighbor in boardManager.GetNeighborData(sourceTile.Coord))
		{
			if (neighbor.Plant == null || !neighbor.Plant.IsMature)
				continue;

			if (neighbor.Plant.Definition.EffectType ==
				PlantEffectType.SpreadChancePlusOneForNeighbors)
			{
				reduction = System.Math.Max(
					reduction,
					neighbor.Plant.Definition
						.NeighborSpreadDenominatorReduction);
			}
		}

		return System.Math.Max(reduction, 0);
	}

	private static int GetEventSpreadReduction(
		List<ActiveGameEvent> activeEvents)
	{
		int reduction = 0;

		foreach (ActiveGameEvent activeEvent in activeEvents)
		{
			if (activeEvent.Definition.EffectType ==
				GameEventEffectType.IncreaseSpreadChance)
			{
				reduction = System.Math.Max(
					reduction,
					activeEvent.Definition.SpreadDenominatorReduction);
			}
		}

		return System.Math.Max(reduction, 0);
	}

	private static List<HexTileData> GetValidSpreadTargets(
		BoardManager boardManager,
		HexTileData sourceTile,
		PlantDefinition definition)
	{
		List<HexTileData> result = new();

		foreach (HexTileData neighbor in boardManager.GetFreeNeighborTiles(sourceTile.Coord))
		{
			if (neighbor != null && neighbor.CanPlacePlant(definition))
			{
				result.Add(neighbor);
			}
		}

		return result;
	}
}
