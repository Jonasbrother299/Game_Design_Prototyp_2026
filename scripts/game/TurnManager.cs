using Godot;
using System;
using System.Collections.Generic;

public partial class TurnManager : Node
{
	public event Action<int> TurnStarted;
	public event Action<int> EndTurnRequested;
	public event Action<PlantType, HexCoord> PlantPlaced;
	public event Action<WaterPhaseResult> WaterPhaseResolved;
	public event Action<SpreadPhaseResult> SpreadPhaseResolved;
	public event Action<GrowthPhaseResult> GrowthPhaseResolved;
	public event Action<EventPhaseResult> EventPhaseResolved;
	public event Action<GameEventType> EventActivated;
	public event Action<GameState> GameEnded;

	[Export] public GameConfig Config = GameConfig.LoadDefault();
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
		State.HandCards.Clear();

		DrawConfiguredStartingCards();

		StartTurn();
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

		WaterPhaseResult waterResult = _waterPhase.Resolve(context, resolvedRound);
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

		if (State.IsGameOver)
		{
			GameEnded?.Invoke(State);
			PrintGameOver();
			return;
		}

		DrawCardsUntilTargetHandSize();
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
		tile.PlacePlant(plantInstance);

		_boardManager.GetTileView(tile.Coord)?.UpdateVisualState();
		_boardManager.RecalculateLightLevels();
		State.HandCards.Remove(card);

		PlantPlaced?.Invoke(card.PlantType, tile.Coord);
		return true;
	}

	public bool DiscardHand()
	{
		if (!CanDiscardHand)
			return false;

		State.HandCards.Clear();
		return true;
	}

	public void AddRandomEvent()
	{
		if (State == null || State.IsGameOver || State.ActiveEvents.Count > 0)
			return;

		GameEventType? eventType = _eventPhase.SelectRandomEvent(CreatePhaseContext());
		if (eventType.HasValue)
		{
			AddEvent(eventType.Value);
		}
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
			int copies = System.Math.Max(plant.StartingHandCopies, 0);

			for (int index = 0;
				index < copies && State.HandCards.Count < targetHandSize;
				index++)
			{
				DrawCard(CardData.CreatePlantCard(plant.Type));
			}
		}

		DrawCardsUntilTargetHandSize();
	}

	private void DrawCardsUntilTargetHandSize()
	{
		int targetHandSize = Mathf.Min(Config.StartingHandSize, Config.MaxHandSize);

		while (State.HandCards.Count < targetHandSize)
		{
			DrawRandomCard();
		}
	}

	private void DrawRandomCard()
	{
		if (State.HandCards.Count >= Config.MaxHandSize)
			return;

		PlantType plantType = GetRandomPlantType();
		DrawCard(CardData.CreatePlantCard(plantType));
	}

	private void DrawCard(CardData card)
	{
		if (card == null || State.HandCards.Count >= Config.MaxHandSize)
			return;

		State.HandCards.Add(card);
	}

	private PlantType GetRandomPlantType()
	{
		List<PlantDefinition> plants = PlantDatabase.GetAll();
		int totalWeight = 0;

		foreach (PlantDefinition plant in plants)
		{
			if (plant.Type is PlantType.None or PlantType.Oak)
				continue;

			totalWeight += System.Math.Max(plant.DrawWeight, 0);
		}

		if (totalWeight <= 0)
			return PlantType.Moss;

		int selection = _rng.RandiRange(1, totalWeight);

		foreach (PlantDefinition plant in plants)
		{
			if (plant.Type is PlantType.None or PlantType.Oak)
				continue;

			selection -= System.Math.Max(plant.DrawWeight, 0);

			if (selection <= 0)
				return plant.Type;
		}

		return PlantType.Moss;
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
