using System.Collections.Generic;

public sealed class EventPhase
{
	private bool _hasActivatedEvent;

	public EventPhaseResult Resolve(TurnPhaseContext context, int round)
	{
		if (context.State.ActiveEvents.Count > 0)
			_hasActivatedEvent = true;

		List<PlantDeathResult> plantDeaths = ApplyEventDeathRisks(context);
		List<GameEventType> finishedEvents = GetFinishedEvents(context.State.ActiveEvents);

		context.State.ActiveEvents.RemoveAll(activeEvent => activeEvent.IsFinished);

		GameEventType? activatedEvent = TryActivateEventForNextRound(context);
		List<GameEventType> activeEvents = GetActiveEventTypes(context.State.ActiveEvents);

		return new EventPhaseResult(
			round,
			activeEvents,
			finishedEvents,
			plantDeaths,
			activatedEvent);
	}

	public GameEventType? SelectRandomEvent(TurnPhaseContext context)
	{
		List<EventDefinition> events = EventDatabase.GetAll();
		int totalWeight = 0;

		foreach (EventDefinition definition in events)
		{
			if (!_hasActivatedEvent && definition.Type == GameEventType.Drought)
				continue;

			totalWeight += System.Math.Max(definition.SelectionWeight, 0);
		}

		if (totalWeight <= 0)
			return null;

		int selection = context.Random.RandiRange(1, totalWeight);

		foreach (EventDefinition definition in events)
		{
			if (!_hasActivatedEvent && definition.Type == GameEventType.Drought)
				continue;

			selection -= System.Math.Max(definition.SelectionWeight, 0);

			if (selection <= 0)
				return definition.Type;
		}

		return null;
	}

	private static List<PlantDeathResult> ApplyEventDeathRisks(
		TurnPhaseContext context)
	{
		List<PlantDeathResult> deaths = new();

		foreach (ActiveGameEvent activeEvent in context.State.ActiveEvents)
		{
			if (activeEvent.Definition.EffectType !=
				GameEventEffectType.PlantDeathRisk)
			{
				continue;
			}

			foreach (HexTileData tile in context.BoardManager.BoardData.Tiles.Values)
			{
				if (tile.Plant == null || tile.Plant.Definition.Type == PlantType.Oak)
					continue;

				bool isMonoculture = IsPartOfMonoculture(context, tile);
				int denominator = GetDeathChanceDenominator(
					activeEvent.Definition,
					tile,
					isMonoculture);

				if (denominator <= 0 ||
					context.Random.RandiRange(1, denominator) != 1)
				{
					continue;
				}

				PlantType plantType = tile.Plant.Definition.Type;
				tile.RemovePlantAndBlockTile(context.Config.DeadPlantBlockedRounds);
				context.BoardManager.GetTileView(tile.Coord)?.UpdateVisualState();

				deaths.Add(new PlantDeathResult(
					plantType,
					tile.Coord,
					activeEvent.Definition.Type,
					denominator,
					isMonoculture,
					context.Config.DeadPlantBlockedRounds));
			}
		}

		return deaths;
	}

	private static int GetDeathChanceDenominator(
		EventDefinition definition,
		HexTileData tile,
		bool isMonoculture)
	{
		if (!tile.Plant.IsMature)
		{
			if (definition.SeedlingDeathChanceDenominator <= 0)
				return 0;

			if (definition.SeedlingDeathRequiresSun &&
				tile.LightLevel != LightLevel.Sun)
			{
				return 0;
			}

			return ApplyGrowthStageDeathResistance(
				definition.SeedlingDeathChanceDenominator,
				tile);
		}

		if (definition.MatureDeathChanceDenominator <= 0)
			return 0;

		if (definition.MatureDeathRequiresMonoculture && !isMonoculture)
			return 0;

		return ApplyGrowthStageDeathResistance(
			definition.MatureDeathChanceDenominator,
			tile);
	}

	private static int ApplyGrowthStageDeathResistance(
		int denominator,
		HexTileData tile)
	{
		int resistancePerStage = System.Math.Max(
			tile.Plant.Definition.EventDeathResistancePerGrowthStage,
			0);
		int completedGrowthStages = System.Math.Max(
			tile.Plant.VisualGrowthStage - 1,
			0);

		return denominator + resistancePerStage * completedGrowthStages;
	}

	private static bool IsPartOfMonoculture(
		TurnPhaseContext context,
		HexTileData startTile)
	{
		int requiredCount = context.Config.MonocultureMinimumPlantCount;
		if (requiredCount <= 1)
			return true;

		PlantType plantType = startTile.Plant.Definition.Type;
		HashSet<HexCoord> visited = new();
		Queue<HexTileData> openTiles = new();

		visited.Add(startTile.Coord);
		openTiles.Enqueue(startTile);

		while (openTiles.Count > 0)
		{
			HexTileData current = openTiles.Dequeue();

			foreach (HexTileData neighbor in
				context.BoardManager.GetNeighborData(current.Coord))
			{
				if (neighbor.Plant == null ||
					neighbor.Plant.Definition.Type != plantType ||
					!visited.Add(neighbor.Coord))
				{
					continue;
				}

				if (visited.Count >= requiredCount)
					return true;

				openTiles.Enqueue(neighbor);
			}
		}

		return visited.Count >= requiredCount;
	}

	private static List<GameEventType> GetFinishedEvents(
		List<ActiveGameEvent> activeEvents)
	{
		List<GameEventType> finishedEvents = new();

		foreach (ActiveGameEvent activeEvent in activeEvents)
		{
			if (activeEvent.IsFinished)
			{
				finishedEvents.Add(activeEvent.Definition.Type);
			}
		}

		return finishedEvents;
	}

	private static List<GameEventType> GetActiveEventTypes(
		List<ActiveGameEvent> activeEvents)
	{
		List<GameEventType> activeTypes = new();

		foreach (ActiveGameEvent activeEvent in activeEvents)
		{
			activeTypes.Add(activeEvent.Definition.Type);
		}

		return activeTypes;
	}

	private GameEventType? TryActivateEventForNextRound(TurnPhaseContext context)
	{
		if (!context.Config.EventsUnlocked)
			return null;

		if (context.State.ActiveEvents.Count > 0 ||
			context.Config.EventChanceDenominator <= 0)
		{
			return null;
		}

		GameEventType eventType;

		if (context.Config.ForceRainAsFirstEvent &&
			!context.Config.HasTriggeredFirstTutorialEvent)
		{
			eventType = GameEventType.Rain;
			context.Config.HasTriggeredFirstTutorialEvent = true;
		}
		else
		{
			if (context.Random.RandiRange(1, context.Config.EventChanceDenominator) != 1)
				return null;

			GameEventType? selectedEvent = SelectRandomEvent(context);
			if (!selectedEvent.HasValue)
				return null;

			eventType = selectedEvent.Value;
		}

		EventDefinition definition = EventDatabase.Get(eventType);
		if (definition == null)
			return null;

		context.State.ActiveEvents.Add(new ActiveGameEvent(definition));
		_hasActivatedEvent = true;
		return eventType;
	}
}
