using System.Collections.Generic;

public sealed class GrowthPhase
{
	public GrowthPhaseResult Resolve(
		TurnPhaseContext context,
		int round,
		HashSet<HexCoord> newSpreadTargets)
	{
		List<PlantGrowthResult> plants = new();

		foreach (HexTileData tile in context.BoardManager.BoardData.Tiles.Values)
		{
			if (tile.Plant == null || newSpreadTargets.Contains(tile.Coord))
				continue;

			int previousRemainingRounds = tile.Plant.RemainingGrowthRounds;
			if (previousRemainingRounds <= 0)
				continue;

			tile.Plant.GrowOneRound();
			plants.Add(new PlantGrowthResult(
				tile.Plant.Definition.Type,
				tile.Coord,
				previousRemainingRounds,
				tile.Plant.RemainingGrowthRounds));

			context.BoardManager.GetTileView(tile.Coord)?.UpdateVisualState();
		}

		TickBlockedTiles(context.BoardManager);
		return new GrowthPhaseResult(round, plants);
	}

	private static void TickBlockedTiles(BoardManager boardManager)
	{
		foreach (HexTileData tile in boardManager.BoardData.Tiles.Values)
		{
			tile.TickBlockedRound();
		}
	}
}
