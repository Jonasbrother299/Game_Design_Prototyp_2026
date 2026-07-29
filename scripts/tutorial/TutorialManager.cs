using Godot;
using System.Collections.Generic;

public partial class TutorialManager : Node
{
	private TutorialOverlay _overlay;
	private BoardManager _boardManager;
	private CardHandUI _cardHand;
	private TurnManager _turnManager;
	private GrowthPhaseResult _pendingGrowthResult;
	private SpreadPhaseResult _pendingSpreadResult;
	private EventPhaseResult _pendingEventResult;
	private readonly Dictionary<string, Panel> _highlightFrames = new();

	private enum TutorialStepId
	{
		Intro,
		Goal,
		WaitForMossPlacement,
		OptionalCardPlay,
		Water,
		Growth,
		Spread,
		Event,
		PlantDeath
	}

	private TutorialStepId _currentStep = TutorialStepId.Intro;

	private bool _hasShownWater;
	private bool _hasShownGrowth;
	private bool _hasShownSpread;
	private bool _hasShownEvent;
	private bool _hasShownPlantDeath;
	private bool _isTutorialVisible;
	private bool _unlockEventsAfterNextEventPhase;
	private HexCoord? _requiredMossPlacementCoord;
	private bool _isFinished;

	public void Start(
		TutorialOverlay overlay,
		BoardManager boardManager,
		CardHandUI cardHand,
		TurnManager turnManager
	)
	{
		_overlay = overlay;
		_boardManager = boardManager;
		_cardHand = cardHand;
		_turnManager = turnManager;

		if (_overlay == null)
		{
			GD.PrintErr("TutorialManager: TutorialOverlay not found.");
			return;
		}

		_overlay.NextRequested += OnNext;
		_overlay.BackRequested += OnBack;

		if (_turnManager != null)
		{
			_turnManager.PlantPlaced += OnPlantPlaced;
			_turnManager.EndTurnRequested += OnEndTurnRequested;
			_turnManager.WaterPhaseResolved += OnWaterPhaseResolved;
			_turnManager.GrowthPhaseResolved += OnGrowthPhaseResolved;
			_turnManager.SpreadPhaseResolved += OnSpreadPhaseResolved;
			_turnManager.EventPhaseResolved += OnEventPhaseResolved;
		}

		_currentStep = TutorialStepId.Intro;
		ShowCurrentStep();
	}

	public override void _ExitTree()
	{
		if (_overlay != null)
		{
			_overlay.NextRequested -= OnNext;
			_overlay.BackRequested -= OnBack;
		}

		if (_turnManager != null)
		{
			_turnManager.PlantPlaced -= OnPlantPlaced;
			_turnManager.EndTurnRequested -= OnEndTurnRequested;
			_turnManager.WaterPhaseResolved -= OnWaterPhaseResolved;
			_turnManager.GrowthPhaseResolved -= OnGrowthPhaseResolved;
			_turnManager.SpreadPhaseResolved -= OnSpreadPhaseResolved;
			_turnManager.EventPhaseResolved -= OnEventPhaseResolved;
		}
	}

	private void OnNext()
	{
		switch (_currentStep)
		{
			case TutorialStepId.Intro:
				GoToStep(TutorialStepId.Goal);
				break;

			case TutorialStepId.Goal:
				GoToStep(TutorialStepId.WaitForMossPlacement);
				break;

			case TutorialStepId.Water:
				if (_pendingGrowthResult != null && !_hasShownGrowth)
					GoToStep(TutorialStepId.Growth);
				else
					HideTutorialButKeepWatching();
				break;

			case TutorialStepId.Growth:
				if (_pendingSpreadResult != null && !_hasShownSpread)
					GoToStep(TutorialStepId.Spread);
				else
					HideTutorialButKeepWatching();
				break;

			case TutorialStepId.Spread:
				_unlockEventsAfterNextEventPhase = true;
				HideTutorialButKeepWatching();
				break;

			case TutorialStepId.Event:
				HideTutorialButKeepWatching();
				break;

			case TutorialStepId.PlantDeath:
				HideTutorialButKeepWatching();
				break;

			case TutorialStepId.WaitForMossPlacement:
			case TutorialStepId.OptionalCardPlay:
				// Diese Schritte werden durch echte Spielaktionen beendet.
				break;
		}
	}

	private void OnBack()
	{
		// Zurück ist im interaktiven Tutorial zunächst deaktiviert.
		// Grund: Schritte wie Karte platzieren oder Runde beenden verändern den echten Spielzustand.
	}

	private void GoToStep(TutorialStepId step)
	{
		_currentStep = step;
		ShowCurrentStep();
	}

	public bool CanPlayCard(CardData card, HexTileData tile)
	{
		if (_isFinished)
			return true;

		// Sobald kein Tutorialfenster sichtbar ist, soll das Spiel normal bedienbar sein.
		// Der TutorialManager bleibt trotzdem aktiv und kann spätere Just-in-Time-Erklärungen zeigen.
		if (!_isTutorialVisible)
			return true;

		if (card == null || tile == null)
			return false;

		if (_currentStep == TutorialStepId.WaitForMossPlacement)
		{
			if (card.CardType != CardType.Plant)
				return false;

			if (card.PlantType != PlantType.Moss)
				return false;

			if (!_requiredMossPlacementCoord.HasValue)
				return false;

			return tile.Coord.Equals(_requiredMossPlacementCoord.Value);
		}

		if (_currentStep == TutorialStepId.OptionalCardPlay)
			return true;

		return false;
	}

	public bool CanEndTurn()
	{
		if (_isFinished)
			return true;

		// Wenn gerade kein Tutorialfenster offen ist, darf die Runde normal beendet werden.
		if (!_isTutorialVisible)
			return true;

		return _currentStep == TutorialStepId.OptionalCardPlay;
	}

	public void RefreshTutorialHighlights()
	{
		if (_isFinished)
			return;

		if (!_isTutorialVisible)
			return;

		if (_currentStep == TutorialStepId.WaitForMossPlacement)
		{
			HighlightFirstPlayableTileFor(PlantType.Moss);
			return;
		}

		if (_currentStep == TutorialStepId.OptionalCardPlay)
		{
			// Kein Highlight auf CardHand:
			// Das würde aktuell nur einen großen Kasten um alle Karten erzeugen.
			// Einzelkarten-Highlights bauen wir später sauber in CardHandUI/CardView.
			HighlightNode("UI/CanvasLayer/GameHub/EndTurnButton");
		}
	}

	private void ShowCurrentStep()
	{
		ClearHighlights();
		_isTutorialVisible = true;

		if (_currentStep == TutorialStepId.Intro)
			_overlay.ShowModal();
		else
			_overlay.ShowHint();

		_overlay.SetNavigation(
			canGoBack: false,
			isLastStep: _currentStep == TutorialStepId.PlantDeath
		);

		bool waitsForAction =
			_currentStep == TutorialStepId.WaitForMossPlacement ||
			_currentStep == TutorialStepId.OptionalCardPlay;

		_overlay.SetNextButtonVisible(!waitsForAction);
		_overlay.SetBackButtonVisible(false);

		switch (_currentStep)
		{
			case TutorialStepId.Intro:
				SetTitle("Einstieg");
				SetText(
					"Die Natur ist aus dem Gleichgewicht geraten. Lass dieses kleine Ökosystem wachsen."
				);
				break;

			case TutorialStepId.Goal:
				SetTitle("Ziel");
				SetText(
					"Baue ein stabiles Ökosystem auf, damit diese alte Eiche überleben kann.\n\n" +
					"Die Eiche in der Mitte ist dein wichtigstes Ziel. Fällt der Wasserwert auf 0, stirbt sie."
				);
				HighlightCenterTile();
				break;

			case TutorialStepId.WaitForMossPlacement:
				SetTitle("Karten ausspielen");
				SetText(
					"Ziehe die Moos-Karte auf das leuchtende Feld.\n\n" +
					"Moos ist eine gute Startpflanze. Platziere sie auf einem geeigneten Feld neben der Eiche. " +
					"Danach kannst du weitere Karten spielen oder die Runde beenden."
				);
				HighlightFirstPlayableTileFor(PlantType.Moss);
				break;

			case TutorialStepId.OptionalCardPlay:
				SetTitle("Weitere Karten spielen");
				SetText(
					"Du hast Moos erfolgreich platziert.\n\n" +
					"Jetzt kannst du so viele weitere Karten spielen, wie du möchtest — von keiner bis zu allen. " +
					"Wenn du fertig bist, beende die Runde, damit dein Ökosystem sich entwickeln kann."
				);

				// Kein Highlight auf CardHand:
				// Aktuell wirkt das als großer Kasten / Farbschleier über allen Karten.
				// Für einzelne Karten bauen wir danach ein eigenes Karten-Highlight.
				HighlightNode("UI/CanvasLayer/GameHub/EndTurnButton");
				break;

			case TutorialStepId.Water:
				_hasShownWater = true;

				SetTitle("Wasserhaushalt");
				SetText(
					"Der Wasserhaushalt zeigt, ob dein Ökosystem stabil bleibt.\n\n" +
					"Einige Pflanzen produzieren Wasser, andere verbrauchen es. Fällt der Wert auf 0, stirbt die Eiche. " +
					"Achte nach jeder Runde darauf, wie sich der Wasserwert verändert."
				);
				HighlightNode("UI/CanvasLayer/GameHub/WaterLabel");
				break;

			case TutorialStepId.Growth:
				_hasShownGrowth = true;

				SetTitle("Wachstum");
				SetText(
					"Dein Moos ist gerade gewachsen.\n\n" +
					"In jeder Übergangsphase wachsen bestehende Pflanzen weiter. " +
					"Sobald eine Pflanze ausgewachsen ist, kann sie zum Beispiel stärkere Effekte haben oder sich verbreiten."
				);

				HighlightMossInGrowthResult();
				break;

			case TutorialStepId.Spread:
				_hasShownSpread = true;

				SetTitle("Verbreitung");
				SetText(
					"Eine Pflanze hat sich gerade von selbst verbreitet.\n\n" +
					"Nach jeder Runde besteht die Chance, dass sich ausgewachsene Pflanzen auf passende benachbarte Felder ausbreiten. " +
					"Das passiert nicht garantiert, sondern hängt von der Pflanze und der Spielsituation ab."
				);

				if (_pendingSpreadResult != null)
				{
					foreach (PlantSpreadResult spread in _pendingSpreadResult.Spreads)
					{
						_boardManager.GetTileView(spread.SourceCoord)?.SetTutorialHighlight(true);
						_boardManager.GetTileView(spread.TargetCoord)?.SetTutorialHighlight(true);
					}
				}
				break;

			case TutorialStepId.Event:
				_hasShownEvent = true;

				SetTitle("Ereignis");
				SetText(
					"Gerade wurde das erste Ereignis ausgelöst: Regen.\n\n" +
					"Ein Ereignis verändert dein Ökosystem für die nächste Runde. " +
					"Ab jetzt können am Ende jeder Runde zufällig Ereignisse auftreten — oder auch keines."
				);

				// EventDisplay hat grundsätzlich die richtige Position und Breite.
				// Wir lassen deshalb X-Position und Breite unverändert und reduzieren nur die Höhe.
				// Die Y-Position bleibt oben am EventDisplay ausgerichtet, damit der Kasten nicht darunter rutscht.
				HighlightNodeWithHeightOverride(
					"UI/CanvasLayer/GameHub/EventDisplay",
					163.0f
				);
				break;

			case TutorialStepId.PlantDeath:
				_hasShownPlantDeath = true;

				SetTitle("Pflanze stirbt");
				SetText(
					"Eine Pflanze konnte gerade nicht überleben und ist gestorben.\n\n" +
					"Das Feld bleibt danach für 2 Runden gesperrt. Du kannst dort also nicht sofort wieder eine neue Pflanze setzen."
				);

				if (_pendingEventResult != null)
				{
					foreach (PlantDeathResult death in _pendingEventResult.PlantDeaths)
						_boardManager.GetTileView(death.Coord)?.SetTutorialHighlight(true);
				}
				break;
		}
	}

	private void OnPlantPlaced(PlantType plantType, HexCoord coord)
	{
		if (_currentStep != TutorialStepId.WaitForMossPlacement)
			return;

		if (plantType != PlantType.Moss)
			return;

		GoToStep(TutorialStepId.OptionalCardPlay);
	}

	private void OnEndTurnRequested(int round)
	{
		if (_currentStep != TutorialStepId.OptionalCardPlay)
			return;

		// Wir wechseln hier noch nicht direkt weiter.
		// Grund: Der Wasserwert wurde zu diesem Zeitpunkt noch nicht berechnet.
		// Die Erklärung zum Wasserhaushalt kommt deshalb erst in OnWaterPhaseResolved.
	}

	private void OnWaterPhaseResolved(WaterPhaseResult result)
	{
		if (_hasShownWater)
			return;

		if (_currentStep != TutorialStepId.OptionalCardPlay)
			return;

		GoToStep(TutorialStepId.Water);
	}

	private void OnGrowthPhaseResolved(GrowthPhaseResult result)
	{
		if (_hasShownGrowth)
			return;

		if (!_hasShownWater)
			return;

		if (result == null || result.Plants == null || result.Plants.Count == 0)
			return;

		if (!GrowthResultContainsMoss(result))
			return;

		_pendingGrowthResult = result;

		// Wenn gerade schon ein Tutorialfenster offen ist, speichern wir das Ergebnis nur.
		// Es wird dann beim Klick auf "Weiter" angezeigt.
		if (_isTutorialVisible)
			return;

		GoToStep(TutorialStepId.Growth);
	}

	private void OnSpreadPhaseResolved(SpreadPhaseResult result)
	{
		if (_hasShownSpread)
			return;

		if (!_hasShownGrowth)
			return;

		if (result == null || result.Spreads == null || result.Spreads.Count == 0)
			return;

		_pendingSpreadResult = result;

		// Wenn gerade noch Growth oder ein anderes Tutorialfenster offen ist,
		// zeigen wir Spread erst nach dem Klick auf "Weiter".
		if (_isTutorialVisible)
			return;

		GoToStep(TutorialStepId.Spread);
	}

	private void OnEventPhaseResolved(EventPhaseResult result)
	{
		if (result == null)
		{
			UnlockEventsAfterBufferRoundIfNeeded();
			return;
		}

		if (!_hasShownPlantDeath &&
			result.PlantDeaths != null &&
			result.PlantDeaths.Count > 0)
		{
			_pendingEventResult = result;

			if (_isTutorialVisible)
			{
				UnlockEventsAfterBufferRoundIfNeeded();
				return;
			}

			GoToStep(TutorialStepId.PlantDeath);
			UnlockEventsAfterBufferRoundIfNeeded();
			return;
		}

		if (!_hasShownEvent &&
			_hasShownGrowth &&
			_hasShownSpread &&
			result.ActivatedEvent.HasValue)
		{
			_pendingEventResult = result;

			if (_isTutorialVisible)
			{
				UnlockEventsAfterBufferRoundIfNeeded();
				return;
			}

			GoToStep(TutorialStepId.Event);
			UnlockEventsAfterBufferRoundIfNeeded();
			return;
		}

		UnlockEventsAfterBufferRoundIfNeeded();
	}

	private void UnlockEventsAfterBufferRoundIfNeeded()
	{
		if (!_unlockEventsAfterNextEventPhase)
			return;

		_unlockEventsAfterNextEventPhase = false;

		if (_turnManager != null)
			_turnManager.Config.EventsUnlocked = true;
	}

	private bool GrowthResultContainsMoss(GrowthPhaseResult result)
	{
		if (result == null || result.Plants == null)
			return false;

		foreach (PlantGrowthResult plant in result.Plants)
		{
			if (plant.PlantType == PlantType.Moss)
				return true;
		}

		return false;
	}

	private void HighlightMossInGrowthResult()
	{
		if (_pendingGrowthResult == null || _pendingGrowthResult.Plants == null)
			return;

		foreach (PlantGrowthResult plant in _pendingGrowthResult.Plants)
		{
			if (plant.PlantType != PlantType.Moss)
				continue;

			_boardManager.GetTileView(plant.Coord)?.SetTutorialHighlight(true);
		}
	}

	private void SetTitle(string title)
	{
		_overlay?.SetTitle(title);
	}

	private void SetText(string text)
	{
		_overlay?.SetText(text);
	}

	private void HighlightNode(string path)
	{
		Node node = GetTree().CurrentScene.GetNodeOrNull<Node>(path);

		if (node is not Control targetControl)
			return;

		if (_overlay == null)
			return;

		ClearHighlightedNode(path);

		Rect2 targetRect = targetControl.GetGlobalRect();

		CreateHighlightFrame(
			path,
			targetRect.Position - new Vector2(8, 8),
			targetRect.Size + new Vector2(16, 16)
		);
	}

	private void HighlightNodeWithHeightOverride(string path, float height)
	{
		Node node = GetTree().CurrentScene.GetNodeOrNull<Node>(path);

		if (node is not Control targetControl)
			return;

		if (_overlay == null)
			return;

		ClearHighlightedNode(path);

		Rect2 targetRect = targetControl.GetGlobalRect();

		Vector2 finalPosition = new Vector2(
			targetRect.Position.X - 8.0f,
			targetRect.Position.Y - 8.0f
		);

		Vector2 finalSize = new Vector2(
			targetRect.Size.X + 16.0f,
			height
		);

		CreateHighlightFrame(path, finalPosition, finalSize);
	}

	private void CreateHighlightFrame(string path, Vector2 position, Vector2 size)
	{
		Panel highlightFrame = new Panel();
		highlightFrame.Name = "TutorialHighlightFrame";
		highlightFrame.MouseFilter = Control.MouseFilterEnum.Ignore;
		highlightFrame.ZIndex = 100;
		highlightFrame.Position = position;
		highlightFrame.Size = size;
		highlightFrame.Modulate = Colors.White;

		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = new Color(1, 1, 1, 0.0f);
		style.BorderColor = new Color(1, 1, 1, 0.55f);
		style.BorderWidthLeft = 3;
		style.BorderWidthTop = 3;
		style.BorderWidthRight = 3;
		style.BorderWidthBottom = 3;
		style.CornerRadiusTopLeft = 12;
		style.CornerRadiusTopRight = 12;
		style.CornerRadiusBottomRight = 12;
		style.CornerRadiusBottomLeft = 12;
		style.ShadowColor = new Color(1, 1, 1, 0.18f);
		style.ShadowSize = 8;

		highlightFrame.AddThemeStyleboxOverride("panel", style);

		_overlay.AddChild(highlightFrame);
		_highlightFrames[path] = highlightFrame;
	}

	private void HighlightCenterTile()
	{
		BoardManager board = _boardManager ?? GetTree().CurrentScene.GetNodeOrNull<BoardManager>("BoardManager");

		if (board == null)
			return;

		HexTile tileView = board.GetTileView(new HexCoord(0, 0));

		if (tileView == null)
			return;

		tileView.SetTutorialHighlight(true);
	}

	private void HighlightFirstPlayableTileFor(PlantType plantType)
	{
		BoardManager board = _boardManager ?? GetTree().CurrentScene.GetNodeOrNull<BoardManager>("BoardManager");

		if (board == null)
			return;

		PlantDefinition plant = PlantDatabase.Get(plantType);

		if (plant == null)
			return;

		HexCoord preferredCoord = new HexCoord(1, 0);
		HexTileData preferredTileData = board.BoardData.GetTile(preferredCoord);

		if (preferredTileData != null && preferredTileData.CanPlacePlant(plant))
		{
			_requiredMossPlacementCoord = preferredCoord;
			board.GetTileView(preferredCoord)?.SetTutorialHighlight(true);
			return;
		}

		foreach (HexCoord coord in board.BoardData.Tiles.Keys)
		{
			HexTileData tileData = board.BoardData.GetTile(coord);

			if (tileData != null && tileData.CanPlacePlant(plant))
			{
				_requiredMossPlacementCoord = coord;
				board.GetTileView(coord)?.SetTutorialHighlight(true);
				return;
			}
		}
	}

	private void ClearAllHighlightFrames()
	{
		foreach (Panel highlightFrame in _highlightFrames.Values)
		{
			if (IsInstanceValid(highlightFrame))
				highlightFrame.QueueFree();
		}

		_highlightFrames.Clear();
	}

	private void ClearHighlights()
	{
		BoardManager board = _boardManager ?? GetTree().CurrentScene.GetNodeOrNull<BoardManager>("BoardManager");

		if (board != null)
		{
			foreach (HexCoord coord in board.BoardData.Tiles.Keys)
			{
				HexTile tileView = board.GetTileView(coord);

				if (tileView == null)
					continue;

				tileView.ClearTutorialHighlight();
				tileView.ClearPlacementPreview();
			}
		}

		ClearHighlightedNode("UI/CanvasLayer/GameHub/WaterLabel");
		ClearHighlightedNode("UI/CanvasLayer/GameHub/EndTurnButton");
		ClearHighlightedNode("UI/CanvasLayer/CardHand");
		ClearHighlightedNode("UI/CanvasLayer/GameHub/EventDisplay");

		ClearAllHighlightFrames();
	}

	private void ClearHighlightedNode(string path)
	{
		if (!_highlightFrames.TryGetValue(path, out Panel highlightFrame))
			return;

		if (IsInstanceValid(highlightFrame))
			highlightFrame.QueueFree();

		_highlightFrames.Remove(path);
	}

	private void EndTutorial()
	{
		_isFinished = true;
		ClearHighlights();
		_overlay?.HideOverlay();
		_isTutorialVisible = false;
		QueueFree();
	}

	private void HideTutorialButKeepWatching()
	{
		ClearHighlights();
		_overlay?.HideOverlay();
		_isTutorialVisible = false;

		// Der TutorialManager bleibt aktiv und hört weiter auf TurnManager-Events.
	}
}
