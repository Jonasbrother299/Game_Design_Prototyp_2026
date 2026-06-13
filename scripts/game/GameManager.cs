using Godot;

public partial class GameManager : Node
{
	private BoardManager _boardManager;
	private TurnManager _turnManager;
	private CardHandUI _cardHand;

	private HexTile _currentPreviewTile;

	private string _lastDebugMessage = "";

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

		ConnectCardHand();
	}

	private void ConnectCardHand()
	{
		_cardHand = GetTree().CurrentScene.GetNodeOrNull<CardHandUI>("UI/CanvasLayer/CardHand");

		if (_cardHand == null)
		{
			GD.PrintErr("CardHandUI not found. Expected path: UI/CanvasLayer/CardHand");
			return;
		}

		_cardHand.PlantCardDragged += OnPlantCardDragged;
		_cardHand.PlantCardDragReleased += OnPlantCardDragReleased;

		GD.Print("GameManager connected to CardHandUI.");
	}

	public override void _ExitTree()
	{
		if (_cardHand != null)
		{
			_cardHand.PlantCardDragged -= OnPlantCardDragged;
			_cardHand.PlantCardDragReleased -= OnPlantCardDragReleased;
		}
	}

	private void OnPlantCardDragged(PlantType plantType, Vector2 mousePosition)
	{
	


		HexTile hoveredTile = GetHexTileUnderMouse(mousePosition);

		if (hoveredTile == null)
		{
			ClearCurrentPreview();
			return;
		}

		if (hoveredTile != _currentPreviewTile)
		{
			ClearCurrentPreview();
			_currentPreviewTile = hoveredTile;
		}

		UpdateCurrentPreview(plantType);
	}

	private void OnPlantCardDragReleased(PlantType plantType, Vector2 mousePosition)
	{
		HexTile releasedTile = GetHexTileUnderMouse(mousePosition);

		bool wasPlaced = TryPlacePlantOnReleasedTile(plantType, releasedTile);

		if (wasPlaced)
		{
			_cardHand?.CommitDraggedCardPlacement();
		}

		ClearCurrentPreview();

		GD.Print($"Released plant card: {plantType} at {mousePosition}");
		GD.Print("GameManager: drag released, preview cleared.");
	}

	private bool TryPlacePlantOnReleasedTile(PlantType plantType, HexTile releasedTile)
	{
		if (releasedTile == null)
		{
			GD.Print("GameManager: Card released, but no HexTile was hit.");
			return false;
		}

		PlantDefinition definition = PlantDatabase.Get(plantType);

		if (definition == null)
		{
			GD.PrintErr($"GameManager: No PlantDefinition found for {plantType}.");
			return false;
		}

		if (!releasedTile.CanPlacePlant(definition))
		{
			GD.Print($"GameManager: Cannot place {plantType} on {releasedTile.Name}.");
			return false;
		}

		CardData card = GetCardFromHand(plantType);

		if (card == null)
		{
			GD.Print($"GameManager: No {plantType} card found in TurnManager hand. Creating temporary card for prototype placement.");
			card = CardData.CreatePlantCard(plantType);
		}

		bool played = _turnManager.TryPlayCardOnTile(
			card,
			releasedTile.Data,
			out string error
		);

		if (!played)
		{
			GD.PrintErr($"GameManager: Failed to place {plantType} on {releasedTile.Name}. {error}");
			return false;
		}

		releasedTile.UpdateVisualState();

		GD.Print($"GameManager: Placed {plantType} on {releasedTile.Name}.");

		return true;
	}

	private void UpdateCurrentPreview(PlantType plantType)
	{
		if (_currentPreviewTile == null)
			return;

		PlantDefinition definition = PlantDatabase.Get(plantType);

		if (definition == null)
		{
			_currentPreviewTile.SetPlacementPreview(false);
			return;
		}

		bool canPlace = _currentPreviewTile.CanPlacePlant(definition);

		_currentPreviewTile.SetPlacementPreview(canPlace);
	}

	private void ClearCurrentPreview()
	{
		if (_currentPreviewTile == null)
			return;

		_currentPreviewTile.ClearPlacementPreview();
		_currentPreviewTile = null;
	}
	
	private void PrintDebugOnce(string message)
	{
		if (_lastDebugMessage == message)
			return;

		_lastDebugMessage = message;
		GD.Print(message);
	}
	
	private HexTile GetHexTileUnderMouse(Vector2 mousePosition)
	{
		Camera3D camera = GetViewport().GetCamera3D();

		if (camera == null)
		{
			GD.PrintErr("No active Camera3D found.");
			return null;
		}

		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
		Vector3 rayEnd = rayOrigin + camera.ProjectRayNormal(mousePosition) * 1000.0f;

		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;

		PhysicsDirectSpaceState3D spaceState = GetViewport().World3D.DirectSpaceState;
		Godot.Collections.Dictionary result = spaceState.IntersectRay(query);

		if (result.Count == 0)
		{
			PrintDebugOnce("Raycast hit nothing. Probably no collision under mouse.");
			return null;
		}

		GodotObject colliderObject = result["collider"].AsGodotObject();
		Node collider = colliderObject as Node;

		if (collider == null)
		{
			PrintDebugOnce("Raycast hit something, but collider is not a Node.");
			return null;
		}

		HexTile hexTile = FindParentHexTile(collider);

		if (hexTile == null)
		{
			PrintDebugOnce($"Raycast hit object, but not HexTile: {collider.Name}");
			return null;
		}

		return hexTile;
	}

	private HexTile FindParentHexTile(Node node)
	{
		while (node != null)
		{
			if (node is HexTile hexTile)
				return hexTile;

			node = node.GetParent();
		}

		return null;
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
