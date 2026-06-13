
using Godot;
using System.Collections.Generic;

public partial class TurnManager : Node
{
	public GameConfig Config { get; private set; } = new GameConfig();
	public GameState State { get; private set; }

	private BoardManager _boardManager;
	private readonly RandomNumberGenerator _rng = new();

	public void Setup(BoardManager boardManager)
	{
		_boardManager = boardManager;
		_rng.Randomize();
	}

	public void StartGame()
	{
		State = new GameState(Config);

		State.HandCards.Clear();

		DrawCard(CardData.CreatePlantCard(PlantType.Moss));
		DrawCard(CardData.CreatePlantCard(PlantType.Flower));
		DrawCard(CardData.CreatePlantCard(PlantType.Mushroom));

		 PlaceStartingOak();
		GD.Print("Game started.");
		StartTurn();
	}
	
	private void PlaceStartingOak()
	{
		HexCoord startCoord = new HexCoord(0, 0);
		HexTileData startTile = _boardManager.GetTileData(startCoord);

		if (startTile == null)
		{
			GD.PrintErr("Starting oak could not be placed. Start tile is missing.");
			return;
		}

		if (startTile.Plant != null)
		{
			return;
		}

		PlantDefinition oakDefinition = PlantDatabase.Get(PlantType.Oak);

		if (oakDefinition == null)
		{
			GD.PrintErr("Starting oak could not be placed. Oak definition is missing.");
			return;
		}

		PlantInstance startingOak = new PlantInstance(oakDefinition, wasCreatedBySpread: false);

		startTile.PlacePlant(startingOak);

		HexTile startTileView = _boardManager.GetTileView(startCoord);
		startTileView?.UpdateVisualState();

		_boardManager.RecalculateLightLevels();
	}

	public void StartTurn()
	{
		State.CardsPlayedThisTurn = 0;

		GD.Print("----------------------------------------");
		GD.Print($"Round {State.CurrentRound} started.");
		PrintState();
	}

	public void EndTurn()
	{
		if (State.IsGameOver)
			return;

		ApplyActiveEvents();
		ApplyPlantProduction();
		GrowPlants();
		TickBlockedTiles();

		if (ShouldCheckSpreadThisRound())
		{
			ApplySpread();
		}

		RemoveFinishedEvents();

		_boardManager.RecalculateLightLevels();

		State.CheckWinLose(Config);

		if (State.IsGameOver)
		{
			PrintGameOver();
			return;
		}

		DrawRandomCard();

		State.CurrentRound++;
		StartTurn();
	}

	public bool TryPlayCardOnTile(CardData card, HexTileData tile, out string errorMessage)
	{
		errorMessage = "";

		if (State == null)
		{
			errorMessage = "GameState missing. Call StartGame first.";
			GD.PrintErr(errorMessage);
			return false;
		}

		if (State.IsGameOver)
		{
			errorMessage = "Game is already over.";
			GD.Print(errorMessage);
			return false;
		}

		if (card == null)
		{
			errorMessage = "Card is null.";
			GD.PrintErr(errorMessage);
			return false;
		}

		if (card.CardType != CardType.Plant)
		{
			errorMessage = "Only plant cards can be played on tiles.";
			GD.Print(errorMessage);
			return false;
		}

		if (tile == null)
		{
			errorMessage = "Tile is null.";
			GD.PrintErr(errorMessage);
			return false;
		}

		if (State.CardsPlayedThisTurn >= Config.CardsPerTurnLimit)
		{
			errorMessage = "Card limit for this turn reached.";
			GD.Print(errorMessage);
			return false;
		}

		PlantDefinition plantDefinition = PlantDatabase.Get(card.PlantType);
		PlantInstance plantInstance = new PlantInstance(plantDefinition, wasCreatedBySpread: false);

		int cost = plantInstance.GetWaterCostWhenPlaced();

		if (State.Water < cost)
		{
			errorMessage = $"Not enough water. Need {cost}, have {State.Water}.";
			GD.Print(errorMessage);
			return false;
		}

		if (!tile.CanPlacePlant(plantDefinition))
		{
			errorMessage = $"Cannot place {plantDefinition.DisplayName} on {tile.Coord}. Light: {tile.LightLevel}";
			GD.Print(errorMessage);
			return false;
		}

		State.Water -= cost;
		State.CardsPlayedThisTurn++;

		tile.PlacePlant(plantInstance);

		HexTile tileView = _boardManager.GetTileView(tile.Coord);
		tileView?.UpdateVisualState();

		_boardManager.RecalculateLightLevels();

		State.HandCards.Remove(card);

		GD.Print($"Played {plantDefinition.DisplayName}. Cost: {cost}");
		PrintState();

		return true;
	}

	public void AddRandomEvent()
	{
		GameEventType eventType = GetRandomEventType();
		EventDefinition eventDefinition = EventDatabase.Get(eventType);

		State.ActiveEvents.Add(new ActiveGameEvent(eventDefinition));

		GD.Print($"Event started: {eventDefinition.DisplayName} for {eventDefinition.DurationRounds} rounds.");
	}

	private void ApplyActiveEvents()
	{
		int totalWaterModifier = 0;

		foreach (ActiveGameEvent activeEvent in State.ActiveEvents)
		{
			totalWaterModifier += activeEvent.ApplyWaterModifier();
			activeEvent.TickDown();

			GD.Print($"Event active: {activeEvent.Definition.DisplayName} | Water: {activeEvent.Definition.WaterModifierPerRound}");
		}

		State.Water += totalWaterModifier;
	}

	private void ApplyPlantProduction()
	{
		int totalProduction = 0;

		foreach (HexTileData tile in _boardManager.BoardData.Tiles.Values)
		{
			if (tile.Plant == null)
				continue;

			int production = tile.Plant.GetWaterProduction();

			production += GetAdjacentProductionBonus(tile);

			totalProduction += production;
		}

		State.Water += totalProduction;

		GD.Print($"Plant production: +{totalProduction} water.");
	}

	private int GetAdjacentProductionBonus(HexTileData tile)
	{
		int bonus = 0;
		List<HexTileData> neighbors = _boardManager.GetNeighborData(tile.Coord);

		foreach (HexTileData neighbor in neighbors)
		{
			if (neighbor.Plant == null)
				continue;

			if (!neighbor.Plant.IsMature)
				continue;

			if (neighbor.Plant.Definition.EffectType == PlantEffectType.AdjacentPlantsProducePlusOne)
			{
				bonus += 1;
			}
		}

		return bonus;
	}

	private void GrowPlants()
	{
		foreach (HexTileData tile in _boardManager.BoardData.Tiles.Values)
		{
			if (tile.Plant == null)
				continue;

			bool wasMatureBefore = tile.Plant.IsMature;

			tile.Plant.GrowOneRound();

			if (!wasMatureBefore && tile.Plant.IsMature)
			{
				GD.Print($"{tile.Plant.Definition.DisplayName} on {tile.Coord} is now mature.");
			}
		}
	}

	private bool ShouldCheckSpreadThisRound()
	{
		return State.CurrentRound % Config.SpreadCheckInterval == 0;
	}

	private void ApplySpread()
	{
		List<HexTileData> spreadingPlants = new();

		foreach (HexTileData tile in _boardManager.BoardData.Tiles.Values)
		{
			if (tile.Plant == null)
				continue;

			if (!tile.Plant.IsMature)
				continue;

			if (tile.Plant.Definition.SpreadChanceDenominator <= 0)
				continue;

			spreadingPlants.Add(tile);
		}

		foreach (HexTileData sourceTile in spreadingPlants)
		{
			TrySpreadFromTile(sourceTile);
		}
	}

	private void TrySpreadFromTile(HexTileData sourceTile)
	{
		PlantDefinition definition = sourceTile.Plant.Definition;

		int denominator = GetModifiedSpreadDenominator(sourceTile);

		int roll = _rng.RandiRange(1, denominator);

		if (roll != 1)
		{
			GD.Print($"{definition.DisplayName} did not spread. Roll {roll}/{denominator}");
			return;
		}

		List<HexTileData> possibleTiles = GetValidSpreadTargets(sourceTile, definition);

		if (possibleTiles.Count == 0)
		{
			GD.Print($"{definition.DisplayName} wanted to spread, but no valid tile found.");
			return;
		}

		int randomIndex = _rng.RandiRange(0, possibleTiles.Count - 1);
		HexTileData targetTile = possibleTiles[randomIndex];

		PlantInstance newPlant = new PlantInstance(definition, wasCreatedBySpread: true);
		targetTile.PlacePlant(newPlant);

		GD.Print($"{definition.DisplayName} spread from {sourceTile.Coord} to {targetTile.Coord}");

		HexTile tileView = _boardManager.GetTileView(targetTile.Coord);
		tileView?.UpdateVisualState();
	}

	private int GetModifiedSpreadDenominator(HexTileData sourceTile)
	{
		int denominator = sourceTile.Plant.Definition.SpreadChanceDenominator;

		List<HexTileData> neighbors = _boardManager.GetNeighborData(sourceTile.Coord);

		foreach (HexTileData neighbor in neighbors)
		{
			if (neighbor.Plant == null)
				continue;

			if (!neighbor.Plant.IsMature)
				continue;

			if (neighbor.Plant.Definition.EffectType == PlantEffectType.SpreadChancePlusOneForNeighbors)
			{
				denominator -= 1;
			}
		}

		foreach (ActiveGameEvent activeEvent in State.ActiveEvents)
		{
			if (activeEvent.Definition.EffectType == GameEventEffectType.IncreaseSpreadChance)
			{
				denominator -= 1;
			}
		}

		if (denominator < 2)
			denominator = 2;

		return denominator;
	}

	private List<HexTileData> GetValidSpreadTargets(HexTileData sourceTile, PlantDefinition definition)
	{
		List<HexTileData> result = new();
		List<HexTileData> neighbors = _boardManager.GetFreeNeighborTiles(sourceTile.Coord);

		foreach (HexTileData neighbor in neighbors)
		{
			if (neighbor.CanPlacePlant(definition))
			{
				result.Add(neighbor);
			}
		}

		return result;
	}

	private void TickBlockedTiles()
	{
		foreach (HexTileData tile in _boardManager.BoardData.Tiles.Values)
		{
			tile.TickBlockedRound();
		}
	}

	private void RemoveFinishedEvents()
	{
		State.ActiveEvents.RemoveAll(activeEvent => activeEvent.IsFinished);
	}

	private void DrawRandomCard()
	{
		if (State.HandCards.Count >= Config.MaxHandSize)
			return;

		PlantType plantType = GetRandomPlantType();
		CardData card = CardData.CreatePlantCard(plantType);

		DrawCard(card);
	}

	private void DrawCard(CardData card)
	{
		if (State.HandCards.Count >= Config.MaxHandSize)
			return;

		State.HandCards.Add(card);

		GD.Print($"Card drawn: {card.CardName}");
	}

	private PlantType GetRandomPlantType()
	{
		PlantType[] plants =
		{
			PlantType.Moss,
			PlantType.Flower,
			PlantType.Mushroom,
			PlantType.Birch
		};

		int index = _rng.RandiRange(0, plants.Length - 1);
		return plants[index];
	}

	private GameEventType GetRandomEventType()
	{
		GameEventType[] events =
		{
			GameEventType.Rain,
			GameEventType.HeavyRain,
			GameEventType.Drought,
			GameEventType.HeatDay,
			GameEventType.Wind,
			GameEventType.Pests
		};

		int index = _rng.RandiRange(0, events.Length - 1);
		return events[index];
	}

	private void PrintState()
	{
		GD.Print($"Water: {State.Water}");
		GD.Print($"Hand cards: {State.HandCards.Count}");
		GD.Print($"Cards played: {State.CardsPlayedThisTurn}/{Config.CardsPerTurnLimit}");
		GD.Print($"Active events: {State.ActiveEvents.Count}");
	}

	private void PrintGameOver()
	{
		if (State.HasWon)
		{
			GD.Print("You won. Water reached 50.");
		}

		if (State.HasLost)
		{
			GD.Print("You lost. Water reached 0.");
		}
	}
}
