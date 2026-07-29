using Godot;

public partial class GameManager : Node
{
	private const float TileFocusClickThreshold = 6.0f;
	private static bool _skipTutorialOnNextStart;

	private BoardManager _boardManager;
	private TurnManager _turnManager;
	private CameraRigController _cameraRig;
	private HexTile _mainTreeTile;
	private CardHandUI _cardHand;
	private BaseButton _endTurnButton;
	private BaseButton _discardHandButton;
	private HexTile _currentPreviewTile;
	private TutorialManager _tutorialManager;
	private string _lastDebugMessage = "";
	private bool _isCardDragActive;
	private bool _isTileFocusClickCandidate;
	private Vector2 _tileFocusPressPosition;

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

		PlaceStarterOak();
		ConfigureCameraRig();

		ConnectCardHand();
		ConnectEndTurnButton();
		ConnectDiscardHandButton();

		if (!ConsumeTutorialSkipRequest())
			StartTutorial();
	}

	public static void SkipTutorialOnNextStart()
	{
		_skipTutorialOnNextStart = true;
	}

	public static void ClearTutorialSkipRequest()
	{
		_skipTutorialOnNextStart = false;
	}

	private static bool ConsumeTutorialSkipRequest()
	{
		bool shouldSkip = _skipTutorialOnNextStart;
		_skipTutorialOnNextStart = false;
		return shouldSkip;
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
		_cardHand.PlantCardDragCanceled += OnPlantCardDragCanceled;
		_cardHand.SetCards(_turnManager.State.HandCards);

		GD.Print("GameManager connected to CardHandUI.");
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseMotion mouseMotion)
		{
			if (_isTileFocusClickCandidate &&
				mouseMotion.Position.DistanceTo(_tileFocusPressPosition) >
				TileFocusClickThreshold)
			{
				_isTileFocusClickCandidate = false;
			}

			return;
		}

		if (inputEvent is not InputEventMouseButton mouseButton ||
			mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (mouseButton.Pressed)
		{
			_isTileFocusClickCandidate = CanFocusTileFromMouse();
			_tileFocusPressPosition = mouseButton.Position;
			return;
		}

		bool shouldFocusTile =
			_isTileFocusClickCandidate &&
			mouseButton.Position.DistanceTo(_tileFocusPressPosition) <=
			TileFocusClickThreshold &&
			CanFocusTileFromMouse();
		_isTileFocusClickCandidate = false;

		if (!shouldFocusTile)
			return;

		HexTile clickedTile = IsMainTreeVisualUnderMouse(mouseButton.Position)
			? _mainTreeTile
			: GetHexTileUnderMouse(mouseButton.Position);

		if (clickedTile == null || !_cameraRig.FocusTile(clickedTile))
			return;

		GetViewport().SetInputAsHandled();
	}

	private void ConfigureCameraRig()
	{
		_cameraRig = GetTree().CurrentScene?.GetNodeOrNull<CameraRigController>(
			"CameraRig");

		if (_cameraRig == null)
		{
			GD.PrintErr("CameraRigController not found. Expected path: CameraRig");
			return;
		}

		_mainTreeTile = _boardManager.GetTileView(new HexCoord(0, 0));
		_cameraRig.ConfigureBoardContext(_boardManager, _mainTreeTile);
	}

	private bool IsMainTreeVisualUnderMouse(Vector2 mousePosition)
	{
		if (_mainTreeTile == null || !IsInstanceValid(_mainTreeTile))
			return false;

		Camera3D camera = GetViewport().GetCamera3D();
		Node treeVisual = _mainTreeTile.FindChild(
			"StartingOak_Visual",
			recursive: true,
			owned: false);

		if (camera == null || treeVisual == null)
			return false;

		Rect2 screenBounds = default;
		bool hasScreenBounds = false;
		ExpandVisualScreenBounds(
			treeVisual,
			camera,
			ref screenBounds,
			ref hasScreenBounds);

		return hasScreenBounds && screenBounds.Grow(8.0f).HasPoint(mousePosition);
	}

	private static void ExpandVisualScreenBounds(
		Node node,
		Camera3D camera,
		ref Rect2 screenBounds,
		ref bool hasScreenBounds)
	{
		if (node is VisualInstance3D visual && visual.Visible)
		{
			Aabb bounds = visual.GetAabb();

			for (int x = 0; x <= 1; x++)
			{
				for (int y = 0; y <= 1; y++)
				{
					for (int z = 0; z <= 1; z++)
					{
						Vector3 corner = bounds.Position + new Vector3(
							bounds.Size.X * x,
							bounds.Size.Y * y,
							bounds.Size.Z * z);
						Vector3 worldCorner = visual.GlobalTransform * corner;

						if (camera.IsPositionBehind(worldCorner))
							continue;

						Vector2 screenCorner = camera.UnprojectPosition(worldCorner);

						if (!hasScreenBounds)
						{
							screenBounds = new Rect2(screenCorner, Vector2.Zero);
							hasScreenBounds = true;
						}
						else
						{
							screenBounds = screenBounds.Expand(screenCorner);
						}
					}
				}
			}
		}

		foreach (Node child in node.GetChildren())
		{
			ExpandVisualScreenBounds(
				child,
				camera,
				ref screenBounds,
				ref hasScreenBounds);
		}
	}

	private bool CanFocusTileFromMouse()
	{
		if (_cameraRig == null ||
			!_cameraRig.InteractionEnabled ||
			_isCardDragActive ||
			_currentPreviewTile != null ||
			GetTree().Paused)
		{
			return false;
		}

		Control hoveredControl = GetViewport().GuiGetHoveredControl();
		return hoveredControl == null ||
			hoveredControl.MouseFilter != Control.MouseFilterEnum.Stop;
	}

private void StartTutorial()
{
	Node currentScene = GetTree().CurrentScene;

	if (currentScene == null)
		return;

	TutorialOverlay overlay = currentScene.GetNodeOrNull<TutorialOverlay>("UI/CanvasLayer/TutorialOverlay");

	if (overlay == null)
	{
		GD.PrintErr("TutorialOverlay not found. Expected path: UI/CanvasLayer/TutorialOverlay");
		return;
	}

	_tutorialManager = new TutorialManager();
	_tutorialManager.Name = "TutorialManager";
	currentScene.AddChild(_tutorialManager);
	_tutorialManager.Start(overlay, _boardManager, _cardHand, _turnManager);
}

private T FindNodeByName<T>(Node root, string nodeName) where T : Node
{
	if (root == null)
		return null;

	if (root.Name == nodeName && root is T typedNode)
		return typedNode;

	foreach (Node child in root.GetChildren())
	{
		T foundNode = FindNodeByName<T>(child, nodeName);

		if (foundNode != null)
			return foundNode;
	}

	return null;
}
	public override void _ExitTree()
	{
		if (_cardHand != null)
		{
			_cardHand.PlantCardDragged -= OnPlantCardDragged;
			_cardHand.PlantCardDragReleased -= OnPlantCardDragReleased;
			_cardHand.PlantCardDragCanceled -= OnPlantCardDragCanceled;
		}
			if (_endTurnButton != null)
		{
			_endTurnButton.Pressed -= OnEndTurnButtonPressed;
		}

		if (_discardHandButton != null)
		{
			_discardHandButton.Pressed -= OnDiscardHandButtonPressed;
		}
	}
private void ConnectEndTurnButton()
{
	_endTurnButton = FindNodeByName<BaseButton>(
		GetTree().CurrentScene,
		"EndTurnButton");

	if (_endTurnButton == null)
	{
		GD.PrintErr("EndTurnButton not found. Make sure the button node is named EndTurnButton.");
		return;
	}

	_endTurnButton.Pressed += OnEndTurnButtonPressed;

	GD.Print("EndTurnButton connected.");
}

private void ConnectDiscardHandButton()
{
	_discardHandButton = FindNodeByName<BaseButton>(
		GetTree().CurrentScene,
		"DiscardHandButton");

	if (_discardHandButton == null)
	{
		GD.PrintErr(
			"DiscardHandButton not found. " +
			"Make sure the button node is named DiscardHandButton.");
		return;
	}

	_discardHandButton.Pressed += OnDiscardHandButtonPressed;
	UpdateDiscardHandButtonState();
}

private void OnEndTurnButtonPressed()
{
	if (_turnManager == null)
		return;

	if (_turnManager.State == null)
		return;

	if (_turnManager.State.IsGameOver)
		return;

	if (_tutorialManager != null && !_tutorialManager.CanEndTurn())
	{
		GD.Print("Tutorial: Runde beenden ist gerade noch nicht erlaubt.");
		return;
	}

	_turnManager.EndTurn();
	_cardHand?.SetCards(_turnManager.State.HandCards);
	UpdateDiscardHandButtonState();
}

private void OnDiscardHandButtonPressed()
{
	if (_turnManager == null || !_turnManager.DiscardHand())
		return;

	ClearCurrentPreview();
	_cardHand?.SetCards(_turnManager.State.HandCards);
	UpdateDiscardHandButtonState();
}

private void UpdateDiscardHandButtonState()
{
	if (_discardHandButton == null)
		return;

	_discardHandButton.Disabled =
		_turnManager == null ||
		!_turnManager.CanDiscardHand;
}

private void OnPlantCardDragged(PlantType plantType, Vector2 mousePosition)
{
	_isCardDragActive = true;
	_isTileFocusClickCandidate = false;
	HexTile hoveredTile = GetHexTileUnderMouse(mousePosition);

	if (hoveredTile == null)
	{
		ClearCurrentPreview();
		_tutorialManager?.RefreshTutorialHighlights();
		return;
	}

	if (hoveredTile != _currentPreviewTile)
	{
		ClearCurrentPreview();
		_currentPreviewTile = hoveredTile;
		_tutorialManager?.RefreshTutorialHighlights();
	}

	UpdateCurrentPreview(plantType);
}

	private void OnPlantCardDragCanceled()
	{
		_isCardDragActive = false;
		_isTileFocusClickCandidate = false;
		ClearCurrentPreview();
		_tutorialManager?.RefreshTutorialHighlights();
	}

	private void OnPlantCardDragReleased(PlantType plantType, Vector2 mousePosition)
	{
		_isCardDragActive = false;
		_isTileFocusClickCandidate = false;
		HexTile releasedTile = GetHexTileUnderMouse(mousePosition);

		bool wasPlaced = TryPlacePlantOnReleasedTile(plantType, releasedTile);

		if (wasPlaced)
		{
			_cardHand?.CommitDraggedCardPlacement();
			UpdateDiscardHandButtonState();
		}

		ClearCurrentPreview();
		_tutorialManager?.RefreshTutorialHighlights();

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
			GD.PrintErr($"GameManager: No {plantType} card found in the current hand.");
			return false;
		}

		if (_tutorialManager != null && !_tutorialManager.CanPlayCard(card, releasedTile.Data))
		{
			GD.Print("Tutorial: Diese Karte darf gerade nicht auf dieses Feld gespielt werden.");
			return false;
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

		bool canPlaceByRules = _currentPreviewTile.CanPlacePlant(definition);

		CardData card = GetCardFromHand(plantType);

		bool canPlaceByTutorial =
			_tutorialManager == null ||
			_tutorialManager.CanPlayCard(card, _currentPreviewTile.Data);

		_currentPreviewTile.SetPlacementPreview(canPlaceByRules && canPlaceByTutorial);
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
	HexCoord startCoord = new HexCoord(0, 0);
	HexTileData centerTile = _boardManager.GetTileData(startCoord);

	if (centerTile == null)
	{
		GD.PrintErr("Starting oak could not be placed. Center tile is missing.");
		return;
	}

	if (centerTile.Plant != null)
		return;

	PlantDefinition oakDefinition = PlantDatabase.Get(PlantType.Oak);

	if (oakDefinition == null)
	{
		GD.PrintErr("Starting oak could not be placed. Oak definition is missing.");
		return;
	}

	PlantInstance startingOak = new PlantInstance(oakDefinition, wasCreatedBySpread: false);

	centerTile.PlacePlant(startingOak);

	HexTile tileView = _boardManager.GetTileView(startCoord);
	tileView?.UpdateVisualState();

	_boardManager.RecalculateLightLevels();
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
