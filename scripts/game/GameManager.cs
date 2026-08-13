using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	private enum TutorialStartMode
	{
		ProfileDefault,
		SkipOnce,
		ForceOnce
	}

	private const float PointerClickThreshold = 6.0f;
	private const int MassPlantDeathThreshold = 15;
	private static TutorialStartMode _tutorialStartMode;

	public event Action<HexTile> TileInformationRequested;

	private BoardManager _boardManager;
	private TurnManager _turnManager;
	private CameraRigController _cameraRig;
	private HexTile _mainTreeTile;
	private CardHandUI _cardHand;
	private GameHub _gameHub;
	private BaseButton _endTurnButton;
	private BaseButton _discardHandButton;
	private HexTile _currentPreviewTile;
	private TutorialManager _tutorialManager;
	private readonly GlobalStatisticsManager _statisticsManager = new();
	private readonly DeveloperProfileSettings _profileSettings = new();
	private readonly HashSet<int> _recordedMassPlantDeathRounds = new();
	private string _lastDebugMessage = "";
	private bool _isCardDragActive;
	private bool _isDayNightPresentationLocked;
	private bool _isTileClickCandidate;
	private Vector2 _tileClickPressPosition;
	private bool _hasRecordedCompletedGame;

	public override void _Ready()
	{
		CallDeferred(nameof(SetupGame));
	}

	public override void _Process(double delta)
	{
		if (_turnManager?.State == null ||
			_turnManager.State.IsGameOver)
		{
			return;
		}

		_turnManager.State.PlayTimeSeconds += Math.Max(delta, 0.0);
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
		ConnectGameHub();
		ConnectRoundResolution();
		ConnectCardHand();
		ConnectEndTurnButton();
		ConnectDiscardHandButton();

		_recordedMassPlantDeathRounds.Clear();
		_hasRecordedCompletedGame = false;
		bool shouldStartTutorial = ShouldStartTutorial();
		_turnManager.ResetProgressStateForNewGame();
		if (!shouldStartTutorial)
		{
			_turnManager.Config.EventsUnlocked = true;
			_turnManager.Config.ForceRainAsFirstEvent = false;
		}
		_turnManager.StartGame();
		PlaceStarterOak();
		ConfigureCameraRig();
		RefreshGameInterfaces();

		if (shouldStartTutorial)
			StartTutorial();
	}

	public static void SkipTutorialOnNextStart()
	{
		_tutorialStartMode = TutorialStartMode.SkipOnce;
	}

	public static void ReplayTutorialOnNextStart()
	{
		_tutorialStartMode = TutorialStartMode.ForceOnce;
	}

	public static void ClearTutorialSkipRequest()
	{
		_tutorialStartMode = TutorialStartMode.ProfileDefault;
	}

	private bool ShouldStartTutorial()
	{
		TutorialStartMode startMode = ConsumeTutorialStartMode();
		if (startMode == TutorialStartMode.ForceOnce)
			return true;

		if (startMode == TutorialStartMode.SkipOnce)
			return false;

		try
		{
			return !_profileSettings.Load().HasSeenTutorial;
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				$"GameManager: Tutorialstatus konnte nicht geladen werden: {exception.Message}");
			return true;
		}
	}

	private static TutorialStartMode ConsumeTutorialStartMode()
	{
		TutorialStartMode startMode = _tutorialStartMode;
		_tutorialStartMode = TutorialStartMode.ProfileDefault;
		return startMode;
	}

	private void ConnectCardHand()
	{
		CardHandUI cardHand = GetTree().CurrentScene.GetNodeOrNull<CardHandUI>(
			"UI/CanvasLayer/CardHand");

		if (cardHand == null)
		{
			GD.PrintErr("CardHandUI not found. Expected path: UI/CanvasLayer/CardHand");
			return;
		}

		if (_cardHand != cardHand)
		{
			if (_cardHand != null)
			{
				_cardHand.PlantCardDragged -= OnPlantCardDragged;
				_cardHand.PlantCardDragReleased -= OnPlantCardDragReleased;
				_cardHand.PlantCardDragCanceled -= OnPlantCardDragCanceled;
			}

			_cardHand = cardHand;
			_cardHand.PlantCardDragged += OnPlantCardDragged;
			_cardHand.PlantCardDragReleased += OnPlantCardDragReleased;
			_cardHand.PlantCardDragCanceled += OnPlantCardDragCanceled;
		}

		if (_turnManager?.State != null)
			_cardHand.SetCards(_turnManager.State.HandCards);

		GD.Print("GameManager connected to CardHandUI.");
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (_isDayNightPresentationLocked)
			return;

		if (inputEvent is InputEventMouseMotion mouseMotion)
		{
			if (_isTileClickCandidate &&
				mouseMotion.Position.DistanceTo(_tileClickPressPosition) >
				GetPointerClickThreshold())
			{
				_isTileClickCandidate = false;
			}

			return;
		}

		if (inputEvent is not InputEventMouseButton mouseButton ||
			mouseButton.ButtonIndex != MouseButton.Left)
			return;

		if (mouseButton.Pressed)
		{
			_isTileClickCandidate = CanFocusTileFromMouse();
			_tileClickPressPosition = mouseButton.Position;

			if (!mouseButton.DoubleClick || !_isTileClickCandidate)
				return;

			_isTileClickCandidate = false;
			HandleTileDoubleClick(mouseButton.Position);
			return;
		}

		bool shouldRequestInformation =
			_isTileClickCandidate &&
			mouseButton.Position.DistanceTo(_tileClickPressPosition) <=
			GetPointerClickThreshold() &&
			CanFocusTileFromMouse();
		_isTileClickCandidate = false;

		if (!shouldRequestInformation)
			return;

		HexTile clickedTile = GetHexTileUnderMouse(mouseButton.Position);
		if (clickedTile == null)
			return;

		TileInformationRequested?.Invoke(clickedTile);
	}

	private float GetPointerClickThreshold()
	{
		return _cameraRig != null
			? Mathf.Max(_cameraRig.DragThreshold, 0.0f)
			: PointerClickThreshold;
	}

	private void HandleTileDoubleClick(Vector2 mousePosition)
	{
		HexTile clickedTile = GetHexTileUnderMouse(mousePosition);
		if (clickedTile == null)
			return;

		bool isMainTree = clickedTile == _mainTreeTile;
		if (!isMainTree && clickedTile.Data?.Plant == null)
			return;

		bool cameraChanged = isMainTree
			? _cameraRig.ShowBoardOverview()
			: _cameraRig.FocusTile(clickedTile);

		if (!cameraChanged)
			return;

		GetViewport().SetInputAsHandled();
	}

	public void SetDayNightPresentationInputLocked(bool isLocked)
	{
		if (_isDayNightPresentationLocked == isLocked)
			return;

		_isDayNightPresentationLocked = isLocked;
		_cardHand?.SetInteractionEnabled(!isLocked);
		_cameraRig?.SetInteractionEnabled(!isLocked);

		if (isLocked)
		{
			_isTileClickCandidate = false;
			ClearCurrentPreview();
		}

		UpdateDiscardHandButtonState();
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

		HexCoord mainTreeCoord = new HexCoord(0, 0);
		if (_turnManager != null)
			_turnManager.TryGetMainTreeCoord(out mainTreeCoord);

		_mainTreeTile = _boardManager.GetTileView(mainTreeCoord);
		_cameraRig.ConfigureBoardContext(_boardManager, _mainTreeTile);
	}

	private void RefreshGameInterfaces()
	{
		ConnectCardHand();
		_cardHand?.SetCards(_turnManager?.State?.HandCards);
		UpdateDiscardHandButtonState();

		Node currentScene = GetTree().CurrentScene;
		ConnectGameHub();
		_gameHub?.RefreshFromRestoredState();

		DroughtWorldEffect droughtWorldEffect = currentScene?.GetNodeOrNull<
			DroughtWorldEffect>("WorldEnvironment");
		droughtWorldEffect?.RefreshFromRestoredState();
	}

	private void ConnectGameHub()
	{
		_gameHub = GetTree().CurrentScene?.GetNodeOrNull<GameHub>(
			"UI/CanvasLayer/GameHub");
	}

	private void ConnectRoundResolution()
	{
		if (_turnManager == null)
			return;

		_turnManager.RoundFullyResolved -= OnRoundFullyResolved;
		_turnManager.RoundFullyResolved += OnRoundFullyResolved;
		_turnManager.GameEnded -= OnGameEnded;
		_turnManager.GameEnded += OnGameEnded;
	}

	private void OnRoundFullyResolved(RoundStatisticsEntry statisticsEntry)
	{
		if (statisticsEntry == null ||
			statisticsEntry.PlantsDiedThisRound < MassPlantDeathThreshold ||
			_recordedMassPlantDeathRounds.Contains(statisticsEntry.RoundNumber))
		{
			return;
		}

		if (TryRecordStatistics(
			() => _statisticsManager.RecordMassPlantDeath(statisticsEntry)))
		{
			_recordedMassPlantDeathRounds.Add(statisticsEntry.RoundNumber);
		}
	}

	private void OnGameEnded(GameState state)
	{
		if (state == null || _hasRecordedCompletedGame)
			return;

		if (TryRecordStatistics(
			() => _statisticsManager.RecordCompletedGame(
				_turnManager.CaptureCompletedGameStatistics())))
		{
			_hasRecordedCompletedGame = true;
		}
	}

	private bool TryRecordStatistics(
		Func<IReadOnlyList<AchievementDefinition>> recordStatistics)
	{
		try
		{
			IReadOnlyList<AchievementDefinition> newlyUnlocked = recordStatistics();
			_gameHub?.ShowSaveFeedback("Statistik gespeichert", isWarning: false);
			_gameHub?.ShowAchievementFeedback(newlyUnlocked);
			return true;
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"GameManager: Die Statistik konnte nicht gespeichert werden: " +
				$"{exception.Message}");
			_gameHub?.ShowSaveFeedback(
				"Statistik konnte nicht gespeichert werden",
				isWarning: true);
			return false;
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

		TutorialOverlay overlay = currentScene.GetNodeOrNull<TutorialOverlay>(
			"UI/CanvasLayer/TutorialOverlay");

		if (overlay == null)
		{
			GD.PrintErr("TutorialOverlay not found. Expected path: UI/CanvasLayer/TutorialOverlay");
			return;
		}

		_tutorialManager = new TutorialManager();
		_tutorialManager.Name = "TutorialManager";
		currentScene.AddChild(_tutorialManager);
		_tutorialManager.Start(overlay, _boardManager, _cardHand, _turnManager);

		try
		{
			_profileSettings.MarkTutorialSeen();
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				$"GameManager: Tutorialstatus konnte nicht gespeichert werden: {exception.Message}");
		}
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
		if (_turnManager != null)
		{
			_turnManager.RoundFullyResolved -= OnRoundFullyResolved;
			_turnManager.GameEnded -= OnGameEnded;
		}

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
	BaseButton endTurnButton = FindNodeByName<BaseButton>(
		GetTree().CurrentScene,
		"EndTurnButton");

	if (endTurnButton == null)
	{
		GD.PrintErr("EndTurnButton not found. Make sure the button node is named EndTurnButton.");
		return;
	}

	if (_endTurnButton != endTurnButton)
	{
		if (_endTurnButton != null)
			_endTurnButton.Pressed -= OnEndTurnButtonPressed;

		_endTurnButton = endTurnButton;
		_endTurnButton.Pressed += OnEndTurnButtonPressed;
	}

	GD.Print("EndTurnButton connected.");
}

private void ConnectDiscardHandButton()
{
	BaseButton discardHandButton = FindNodeByName<BaseButton>(
		GetTree().CurrentScene,
		"DiscardHandButton");

	if (discardHandButton == null)
	{
		GD.PrintErr(
			"DiscardHandButton not found. " +
			"Make sure the button node is named DiscardHandButton.");
		return;
	}

	if (_discardHandButton != discardHandButton)
	{
		if (_discardHandButton != null)
			_discardHandButton.Pressed -= OnDiscardHandButtonPressed;

		_discardHandButton = discardHandButton;
		_discardHandButton.Pressed += OnDiscardHandButtonPressed;
	}
	UpdateDiscardHandButtonState();
}

private void OnEndTurnButtonPressed()
{
	if (_isDayNightPresentationLocked)
		return;

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
	if (_isDayNightPresentationLocked)
		return;

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
		_isDayNightPresentationLocked ||
		_turnManager == null ||
		!_turnManager.CanDiscardHand;
}

private void OnPlantCardDragged(PlantType plantType, Vector2 mousePosition)
{
	_isCardDragActive = true;
	_isTileClickCandidate = false;
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
	ClearCurrentPreview();
		_tutorialManager?.RefreshTutorialHighlights();
	}

	private void OnPlantCardDragReleased(PlantType plantType, Vector2 mousePosition)
	{
	_isCardDragActive = false;
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
