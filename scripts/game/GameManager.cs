using Godot;

public partial class GameManager : Node
{
	private BoardManager _boardManager;
	private TurnManager _turnManager;

	public override void _Ready()
	{
		CallDeferred(nameof(SetupGame));
	}

	private void SetupGame()
	{
		_boardManager = GetNodeOrNull<BoardManager>("../BoardManager");
		_turnManager = GetNodeOrNull<TurnManager>("../TurnManager");

		if (_boardManager == null)
		{
			GD.PrintErr("BoardManager not found. Make sure the node is named BoardManager.");
			return;
		}

		if (_turnManager == null)
		{
			GD.PrintErr("TurnManager not found. Make sure the node is named TurnManager.");
			return;
		}

		_turnManager.Setup(_boardManager);
		_turnManager.StartGame();

		RunTestRound();
	}

	private void RunTestRound()
{
	PlaceStarterOak();
	PlayHandCard(PlantType.Moss, new HexCoord(1, 0));
}

	private void PlaceStarterOak()
	{
		HexTileData centerTile = _boardManager.GetTileData(new HexCoord(0, 0));

		CardData oakCard = CardData.CreatePlantCard(PlantType.Oak);

		bool oakPlaced = _turnManager.TryPlayCardOnTile(
			oakCard,
			centerTile,
			out string oakError
		);

		if (!oakPlaced)
		{
			GD.PrintErr(oakError);
		}
	}

	private void PlayHandCard(PlantType plantType, HexCoord coord)
	{
		CardData card = GetCardFromHand(plantType);

		if (card == null)
		{
			GD.PrintErr($"No {plantType} card found in hand.");
			return;
		}

		HexTileData tile = _boardManager.GetTileData(coord);

		bool played = _turnManager.TryPlayCardOnTile(
			card,
			tile,
			out string error
		);

		if (!played)
		{
			GD.PrintErr(error);
		}
	}

	private CardData GetCardFromHand(PlantType plantType)
	{
		foreach (CardData card in _turnManager.State.HandCards)
		{
			if (card.CardType == CardType.Plant && card.PlantType == plantType)
			{
				return card;
			}
		}

		return null;
	}
}
