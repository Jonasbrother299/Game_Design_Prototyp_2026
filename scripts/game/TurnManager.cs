using Godot;
using System;
using System.Collections.Generic;

public partial class TurnManager : Node
{
	private const int FirstCardDrawPityRound = 2;
	private const int SecondCardDrawPityRound = 3;
	private const int FirstCardDrawPityMultiplier = 2;
	private const int SecondCardDrawPityMultiplier = 3;

	public event Action<int> TurnStarted;
	public event Action<int> EndTurnRequested;
	public event Action<PlantType, HexCoord> PlantPlaced;
	public event Action<WaterPhaseResult> WaterPhaseResolved;
	public event Action<SpreadPhaseResult> SpreadPhaseResolved;
	public event Action<GrowthPhaseResult> GrowthPhaseResolved;
	public event Action<EventPhaseResult> EventPhaseResolved;
	public event Action<GameEventType> EventActivated;
	public event Action<RoundStatisticsEntry> RoundFullyResolved;
	public event Action<GameState> GameEnded;

	[Export] public GameConfig Config;

	[ExportGroup("Developer Testing")]
	[Export] public WaterManagementMode WaterManagement =
		WaterManagementMode.CurrentAllPlants;

	public GameState State { get; private set; }
	public bool CanDiscardHand =>
		State != null &&
		!State.IsGameOver &&
		State.CurrentRound >= Config.HandDiscardAvailableFromRound &&
		State.HandCards.Count > 0;

	private readonly RandomNumberGenerator _rng = new();
	private readonly WaterPhase _waterPhase = new();
	private readonly SpreadPhase _spreadPhase = new();
	private readonly GrowthPhase _growthPhase = new();
	private readonly EventPhase _eventPhase = new();

	private BoardManager _boardManager;

	public override void _Ready()
	{
		Config ??= GameConfig.LoadDefault();
	}

	public void Setup(BoardManager boardManager)
	{
		_boardManager = boardManager;
		_rng.Randomize();
	}

	public void StartGame()
	{
		if (_boardManager == null)
		{
			GD.PushError("TurnManager: BoardManager fehlt. Setup muss vor StartGame aufgerufen werden.");
			return;
		}

		if (!PlantDatabase.IsValid || !EventDatabase.IsValid)
		{
			GD.PushError(
				"TurnManager: Pflanzen- oder Wetterdaten sind ungültig. " +
				"Die vorherigen Fehlermeldungen nennen die betroffenen Ressourcen.");
		}

		State = new GameState(Config);
		InitializeCardDrawMissedRounds();
		DrawConfiguredStartingCards();
		StartTurn();
	}

	public void ResetProgressStateForNewGame()
	{
		Config.EventsUnlocked = false;
		Config.ForceRainAsFirstEvent = true;
		Config.HasTriggeredFirstTutorialEvent = false;
	}

	public void StartTurn()
	{
		if (State == null)
			return;

		State.CardsPlayedThisTurn = 0;

		GD.Print("----------------------------------------");
		GD.Print($"Round {State.CurrentRound} started.");
		PrintState();

		TurnStarted?.Invoke(State.CurrentRound);
	}

	public void EndTurn()
	{
		if (State == null || State.IsGameOver || _boardManager == null)
			return;

		int resolvedRound = State.CurrentRound;
		TurnPhaseContext context = CreatePhaseContext();

		EndTurnRequested?.Invoke(resolvedRound);

		WaterPhaseResult waterResult = _waterPhase.Resolve(
			context,
			resolvedRound,
			WaterManagement);
		WaterPhaseResolved?.Invoke(waterResult);

		SpreadPhaseResult spreadResult = _spreadPhase.Resolve(context, resolvedRound);
		SpreadPhaseResolved?.Invoke(spreadResult);

		GrowthPhaseResult growthResult = _growthPhase.Resolve(
			context,
			resolvedRound,
			GetSpreadTargetCoords(spreadResult));
		GrowthPhaseResolved?.Invoke(growthResult);

		EventPhaseResult eventResult = _eventPhase.Resolve(context, resolvedRound);
		if (eventResult.ActivatedEvent.HasValue)
		{
			EventActivated?.Invoke(eventResult.ActivatedEvent.Value);
		}
		EventPhaseResolved?.Invoke(eventResult);

		_boardManager.RecalculateLightLevels();
		State.CheckWinLose(Config);
		State.PlantsDiedTotal += eventResult.PlantDeaths.Count;
		RoundStatisticsEntry statisticsEntry = RecordRoundStatistics(
			resolvedRound,
			eventResult.PlantDeaths.Count);

		if (State.IsGameOver)
		{
			RoundFullyResolved?.Invoke(statisticsEntry);
			GameEnded?.Invoke(State);
			PrintGameOver();
			return;
		}

		DrawCardsUntilTargetHandSize(updateMissedRounds: true);
		State.CurrentRound++;
		RoundFullyResolved?.Invoke(statisticsEntry);
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
			return false;
		}

		if (!State.HandCards.Contains(card))
		{
			errorMessage = "Card is not in the current hand.";
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
			return false;
		}

		PlantDefinition plantDefinition = PlantDatabase.Get(card.PlantType);
		if (plantDefinition == null)
		{
			errorMessage = $"Plant definition is missing for {card.PlantType}.";
			GD.PrintErr(errorMessage);
			return false;
		}

		if (!tile.CanPlacePlant(plantDefinition))
		{
			errorMessage =
				$"Cannot place {plantDefinition.DisplayName} on {tile.Coord}. Light: {tile.LightLevel}";
			return false;
		}

		PlantInstance plantInstance = new PlantInstance(
			plantDefinition,
			wasCreatedBySpread: false);

		State.CardsPlayedThisTurn++;
		State.CardsPlayedTotal++;
		tile.PlacePlant(plantInstance);

		_boardManager.GetTileView(tile.Coord)?.UpdateVisualState();
		_boardManager.RecalculateLightLevels();
		State.HandCards.Remove(card);

		PlantPlaced?.Invoke(card.PlantType, tile.Coord);
		return true;
	}

	public bool CanRemoveYoungPlant(HexTileData tile)
	{
		return State != null &&
			!State.IsGameOver &&
			tile?.Plant != null &&
			!tile.Coord.Equals(new HexCoord(0, 0)) &&
			tile.Plant.VisualGrowthStage == 1;
	}

	public bool TryRemoveYoungPlant(HexTileData tile, out string errorMessage)
	{
		errorMessage = "";

		if (State == null)
		{
			errorMessage = "Das Spiel wurde noch nicht gestartet.";
			return false;
		}

		if (State.IsGameOver)
		{
			errorMessage = "Die Partie ist bereits beendet.";
			return false;
		}

		if (tile?.Plant == null)
		{
			errorMessage = "Auf diesem Feld steht keine lebende Pflanze.";
			return false;
		}

		if (tile.Coord.Equals(new HexCoord(0, 0)))
		{
			errorMessage = "Die Haupteiche kann nicht entfernt werden.";
			return false;
		}

		if (tile.Plant.VisualGrowthStage != 1)
		{
			errorMessage = "Nur Pflanzen im ersten Stadium können entfernt werden.";
			return false;
		}

		tile.RemovePlant();
		_boardManager.GetTileView(tile.Coord)?.UpdateVisualState();
		_boardManager.RecalculateLightLevels();
		return true;
	}

	public bool DiscardHand()
	{
		if (!CanDiscardHand)
			return false;

		State.HandCards.Clear();
		return true;
	}

	public bool AddEvent(GameEventType eventType)
	{
		if (State == null || State.IsGameOver || State.ActiveEvents.Count > 0)
			return false;

		EventDefinition eventDefinition = EventDatabase.Get(eventType);
		if (eventDefinition == null)
		{
			GD.PrintErr($"Event definition is missing for {eventType}.");
			return false;
		}

		State.ActiveEvents.Add(new ActiveGameEvent(eventDefinition));
		EventActivated?.Invoke(eventType);
		return true;
	}

	public CompletedGameStatisticsEntry CaptureCompletedGameStatistics()
	{
		if (State == null || _boardManager == null || !State.IsGameOver)
		{
			throw new InvalidOperationException(
				"Eine Partiestatistik kann nur nach einem Spielende erfasst werden.");
		}

		HexCoord mainTreeCoord = FindMainTreeCoord();
		return new CompletedGameStatisticsEntry
		{
			CompletedAt = DateTimeOffset.UtcNow,
			HasWon = State.HasWon,
			FinalRound = State.CurrentRound,
			FinalWater = State.Water,
			MainTreeProgress = GetMainTreeProgress(mainTreeCoord),
			LivingPlantCount = CountLivingPlants(),
			PlantsDiedTotal = State.PlantsDiedTotal,
			CardsPlayedTotal = State.CardsPlayedTotal,
			PlayTimeSeconds = State.PlayTimeSeconds
		};
	}

	public bool TryGetMainTreeCoord(out HexCoord coord)
	{
		if (_boardManager != null)
		{
			foreach (HexTileData tile in _boardManager.BoardData.Tiles.Values)
			{
				if (tile.Plant?.Definition.Type == PlantType.Oak)
				{
					coord = tile.Coord;
					return true;
				}
			}
		}

		coord = default;
		return false;
	}

	private TurnPhaseContext CreatePhaseContext()
	{
		return new TurnPhaseContext(State, _boardManager, Config, _rng);
	}

	private static HashSet<HexCoord> GetSpreadTargetCoords(
		SpreadPhaseResult spreadResult)
	{
		HashSet<HexCoord> result = new();

		foreach (PlantSpreadResult spread in spreadResult.Spreads)
		{
			result.Add(spread.TargetCoord);
		}

		return result;
	}

	private void DrawConfiguredStartingCards()
	{
		int targetHandSize = Mathf.Min(
			Config.StartingHandSize,
			Config.MaxHandSize);

		foreach (PlantDefinition plant in PlantDatabase.GetAll())
		{
			int copies = Math.Max(plant.StartingHandCopies, 0);

			for (int index = 0;
				index < copies && State.HandCards.Count < targetHandSize;
				index++)
			{
				if (WouldCompleteUniformHand(plant.Type, targetHandSize))
					continue;

				DrawCard(CardData.CreatePlantCard(plant.Type));
			}
		}

		DrawCardsUntilTargetHandSize();
	}

	private void DrawCardsUntilTargetHandSize(bool updateMissedRounds = false)
	{
		int targetHandSize = Mathf.Min(Config.StartingHandSize, Config.MaxHandSize);
		HashSet<PlantType> drawnPlantTypes = new();

		while (State.HandCards.Count < targetHandSize)
		{
			PlantType drawnPlantType = DrawRandomCard(targetHandSize);

			if (drawnPlantType == PlantType.None)
				break;

			drawnPlantTypes.Add(drawnPlantType);
		}

		if (updateMissedRounds)
			UpdateCardDrawMissedRounds(drawnPlantTypes);
	}

	private PlantType DrawRandomCard(int targetHandSize)
	{
		if (State.HandCards.Count >= Config.MaxHandSize)
			return PlantType.None;

		EnsureDrawPile(targetHandSize);
		if (State.DrawPile.Count == 0)
			return PlantType.None;

		CardData card = State.DrawPile[0];
		State.DrawPile.RemoveAt(0);
		DrawCard(card);
		return card?.PlantType ?? PlantType.None;
	}

	private void EnsureDrawPile(int targetHandSize)
	{
		if (State.DrawPile.Count > 0)
			return;

		int drawBatchSize = 1;
		for (int index = 0; index < drawBatchSize; index++)
		{
			CardData card = CardData.CreatePlantCard(
				GetRandomPlantType(targetHandSize));
			if (card != null)
				State.DrawPile.Add(card);
		}
	}

	private void DrawCard(CardData card)
	{
		if (card == null || State.HandCards.Count >= Config.MaxHandSize)
			return;

		State.HandCards.Add(card);
	}

	private PlantType GetRandomPlantType(int targetHandSize)
	{
		List<PlantDefinition> plants = PlantDatabase.GetAll();
		int totalWeight = 0;

		foreach (PlantDefinition plant in plants)
		{
			if (plant.Type is PlantType.None or PlantType.Oak ||
				WouldCompleteUniformHand(plant.Type, targetHandSize))
				continue;

			totalWeight += GetAdjustedDrawWeight(plant);
		}

		if (totalWeight <= 0)
			return GetFallbackPlantType(plants, targetHandSize);

		int selection = _rng.RandiRange(1, totalWeight);

		foreach (PlantDefinition plant in plants)
		{
			if (plant.Type is PlantType.None or PlantType.Oak ||
				WouldCompleteUniformHand(plant.Type, targetHandSize))
				continue;

			selection -= GetAdjustedDrawWeight(plant);

			if (selection <= 0)
				return plant.Type;
		}

		return GetFallbackPlantType(plants, targetHandSize);
	}

	private void InitializeCardDrawMissedRounds()
	{
		foreach (PlantDefinition plant in PlantDatabase.GetAll())
		{
			if (plant.Type is PlantType.None or PlantType.Oak)
				continue;

			State.CardDrawMissedRounds[plant.Type] = 0;
		}
	}

	private void UpdateCardDrawMissedRounds(
		HashSet<PlantType> drawnPlantTypes)
	{
		foreach (PlantDefinition plant in PlantDatabase.GetAll())
		{
			if (plant.Type is PlantType.None or PlantType.Oak)
				continue;

			if (drawnPlantTypes.Contains(plant.Type))
			{
				State.CardDrawMissedRounds[plant.Type] = 0;
				continue;
			}

			State.CardDrawMissedRounds.TryGetValue(
				plant.Type,
				out int missedRounds);
			State.CardDrawMissedRounds[plant.Type] = Math.Min(
				missedRounds + 1,
				SecondCardDrawPityRound);
		}
	}

	private int GetAdjustedDrawWeight(PlantDefinition plant)
	{
		int baseWeight = Math.Max(plant.DrawWeight, 0);
		State.CardDrawMissedRounds.TryGetValue(
			plant.Type,
			out int missedRounds);

		if (missedRounds >= SecondCardDrawPityRound)
			return baseWeight * SecondCardDrawPityMultiplier;

		if (missedRounds >= FirstCardDrawPityRound)
			return baseWeight * FirstCardDrawPityMultiplier;

		return baseWeight;
	}

	private bool WouldCompleteUniformHand(
		PlantType candidateType,
		int targetHandSize)
	{
		if (targetHandSize <= 1 ||
			State.HandCards.Count == 0 ||
			State.HandCards.Count + 1 < targetHandSize)
		{
			return false;
		}

		foreach (CardData card in State.HandCards)
		{
			if (card.CardType != CardType.Plant ||
				card.PlantType != candidateType)
			{
				return false;
			}
		}

		return true;
	}

	private PlantType GetFallbackPlantType(
		List<PlantDefinition> plants,
		int targetHandSize)
	{
		foreach (PlantDefinition plant in plants)
		{
			if (plant.Type is PlantType.None or PlantType.Oak ||
				WouldCompleteUniformHand(plant.Type, targetHandSize))
			{
				continue;
			}

			return plant.Type;
		}

		return PlantType.None;
	}

	private HexCoord FindMainTreeCoord()
	{
		foreach (HexTileData tile in _boardManager.BoardData.Tiles.Values)
		{
			if (tile.Plant?.Definition.Type == PlantType.Oak)
				return tile.Coord;
		}

		throw new InvalidOperationException(
			"Der Spielzustand enthält keine Haupteiche.");
	}

	private int CountLivingPlants()
	{
		int count = 0;
		foreach (HexTileData tile in _boardManager.BoardData.Tiles.Values)
		{
			if (tile.Plant != null)
				count++;
		}

		return count;
	}

	private int GetMainTreeProgress(HexCoord mainTreeCoord)
	{
		PlantInstance mainTree = _boardManager.GetTileData(mainTreeCoord)?.Plant;
		if (mainTree == null)
			return 0;

		return Mathf.RoundToInt(mainTree.GrowthProgress * 100.0f);
	}

	private RoundStatisticsEntry RecordRoundStatistics(
		int roundNumber,
		int plantsDiedThisRound)
	{
		foreach (RoundStatisticsEntry existingEntry in State.RoundHistory)
		{
			if (existingEntry.RoundNumber == roundNumber)
				return existingEntry;
		}

		HexCoord mainTreeCoord = FindMainTreeCoord();
		RoundStatisticsEntry entry = new()
		{
			RoundNumber = roundNumber,
			CompletedAt = DateTimeOffset.UtcNow,
			WaterAtRoundEnd = State.Water,
			LivingPlantCount = CountLivingPlants(),
			PlantsDiedThisRound = plantsDiedThisRound,
			DeadPlantCountTotal = State.PlantsDiedTotal,
			CardsPlayedTotal = State.CardsPlayedTotal,
			MainTreeProgress = GetMainTreeProgress(mainTreeCoord),
			PlayTimeSeconds = State.PlayTimeSeconds
		};

		State.RoundHistory.Add(entry);
		return entry;
	}

	private void PrintState()
	{
		GD.Print($"Water: {State.Water}");
		GD.Print($"Hand cards: {State.HandCards.Count}");
		GD.Print(
			$"Cards played: {State.CardsPlayedThisTurn}/{Config.CardsPerTurnLimit}");
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
