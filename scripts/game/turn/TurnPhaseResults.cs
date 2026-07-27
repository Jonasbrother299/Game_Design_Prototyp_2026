using System.Collections.Generic;

public sealed class PlantWaterResult
{
	public HexCoord Coord { get; }
	public PlantType PlantType { get; }
	public int Production { get; }
	public int Consumption { get; }
	public int AdjacentProductionBonus { get; }
	public int NetChange => Production + AdjacentProductionBonus - Consumption;

	public PlantWaterResult(
		HexCoord coord,
		PlantType plantType,
		int production,
		int consumption,
		int adjacentProductionBonus)
	{
		Coord = coord;
		PlantType = plantType;
		Production = production;
		Consumption = consumption;
		AdjacentProductionBonus = adjacentProductionBonus;
	}
}

public sealed class WaterPhaseResult
{
	public int Round { get; }
	public int StartingWater { get; }
	public int EndingWater { get; }
	public int EventWaterModifier { get; }
	public int PlantWaterProduction { get; }
	public int PlantWaterConsumption { get; }
	public int NetChange => EndingWater - StartingWater;
	public IReadOnlyList<PlantWaterResult> Plants { get; }

	public WaterPhaseResult(
		int round,
		int startingWater,
		int endingWater,
		int eventWaterModifier,
		int plantWaterProduction,
		int plantWaterConsumption,
		List<PlantWaterResult> plants)
	{
		Round = round;
		StartingWater = startingWater;
		EndingWater = endingWater;
		EventWaterModifier = eventWaterModifier;
		PlantWaterProduction = plantWaterProduction;
		PlantWaterConsumption = plantWaterConsumption;
		Plants = plants.AsReadOnly();
	}
}

public sealed class PlantSpreadResult
{
	public PlantType PlantType { get; }
	public HexCoord SourceCoord { get; }
	public HexCoord TargetCoord { get; }

	public PlantSpreadResult(PlantType plantType, HexCoord sourceCoord, HexCoord targetCoord)
	{
		PlantType = plantType;
		SourceCoord = sourceCoord;
		TargetCoord = targetCoord;
	}
}

public sealed class SpreadPhaseResult
{
	public int Round { get; }
	public IReadOnlyList<PlantSpreadResult> Spreads { get; }

	public SpreadPhaseResult(int round, List<PlantSpreadResult> spreads)
	{
		Round = round;
		Spreads = spreads.AsReadOnly();
	}
}

public sealed class PlantGrowthResult
{
	public PlantType PlantType { get; }
	public HexCoord Coord { get; }
	public int PreviousRemainingRounds { get; }
	public int RemainingRounds { get; }
	public bool BecameMature { get; }

	public PlantGrowthResult(
		PlantType plantType,
		HexCoord coord,
		int previousRemainingRounds,
		int remainingRounds)
	{
		PlantType = plantType;
		Coord = coord;
		PreviousRemainingRounds = previousRemainingRounds;
		RemainingRounds = remainingRounds;
		BecameMature = previousRemainingRounds > 0 && remainingRounds == 0;
	}
}

public sealed class GrowthPhaseResult
{
	public int Round { get; }
	public IReadOnlyList<PlantGrowthResult> Plants { get; }

	public GrowthPhaseResult(int round, List<PlantGrowthResult> plants)
	{
		Round = round;
		Plants = plants.AsReadOnly();
	}
}

public sealed class EventPhaseResult
{
	public int Round { get; }
	public IReadOnlyList<GameEventType> ActiveEvents { get; }
	public IReadOnlyList<GameEventType> FinishedEvents { get; }
	public IReadOnlyList<PlantDeathResult> PlantDeaths { get; }
	public GameEventType? ActivatedEvent { get; }

	public EventPhaseResult(
		int round,
		List<GameEventType> activeEvents,
		List<GameEventType> finishedEvents,
		List<PlantDeathResult> plantDeaths,
		GameEventType? activatedEvent)
	{
		Round = round;
		ActiveEvents = activeEvents.AsReadOnly();
		FinishedEvents = finishedEvents.AsReadOnly();
		PlantDeaths = plantDeaths.AsReadOnly();
		ActivatedEvent = activatedEvent;
	}
}

public sealed class PlantDeathResult
{
	public PlantType PlantType { get; }
	public HexCoord Coord { get; }
	public GameEventType Cause { get; }
	public int ChanceDenominator { get; }
	public bool WasMonoculture { get; }
	public int BlockedRounds { get; }

	public PlantDeathResult(
		PlantType plantType,
		HexCoord coord,
		GameEventType cause,
		int chanceDenominator,
		bool wasMonoculture,
		int blockedRounds)
	{
		PlantType = plantType;
		Coord = coord;
		Cause = cause;
		ChanceDenominator = chanceDenominator;
		WasMonoculture = wasMonoculture;
		BlockedRounds = blockedRounds;
	}
}
