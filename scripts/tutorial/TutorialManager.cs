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

	private readonly Dictionary<string, Panel>
		_highlightFrames = new();

	private enum TutorialStepId
	{
		Intro,
		Goal,
		CardExplanation,
		WaitForMossPlacement,
		OptionalCardPlay,
		Water,
		Growth,
		Spread,
		Event,
		PlantDeath
	}

	private enum CardExplanationPage
	{
		Intro,
		Light,
		Growth,
		WaterConsumption,
		WaterProduction,
		Description
	}

	private TutorialStepId _currentStep =
		TutorialStepId.Intro;

	private CardExplanationPage _cardExplanationPage =
		CardExplanationPage.Intro;

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
			GD.PrintErr(
				"TutorialManager: TutorialOverlay not found."
			);
			return;
		}

		_overlay.NextRequested += OnNext;
		_overlay.BackRequested += OnBack;

		if (_turnManager != null)
		{
			_turnManager.PlantPlaced +=
				OnPlantPlaced;

			_turnManager.EndTurnRequested +=
				OnEndTurnRequested;

			_turnManager.WaterPhaseResolved +=
				OnWaterPhaseResolved;

			_turnManager.GrowthPhaseResolved +=
				OnGrowthPhaseResolved;

			_turnManager.SpreadPhaseResolved +=
				OnSpreadPhaseResolved;

			_turnManager.EventPhaseResolved +=
				OnEventPhaseResolved;
		}

		_currentStep =
			TutorialStepId.Intro;

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
			_turnManager.PlantPlaced -=
				OnPlantPlaced;

			_turnManager.EndTurnRequested -=
				OnEndTurnRequested;

			_turnManager.WaterPhaseResolved -=
				OnWaterPhaseResolved;

			_turnManager.GrowthPhaseResolved -=
				OnGrowthPhaseResolved;

			_turnManager.SpreadPhaseResolved -=
				OnSpreadPhaseResolved;

			_turnManager.EventPhaseResolved -=
				OnEventPhaseResolved;
		}
	}

	private void OnNext()
	{
		switch (_currentStep)
		{
			case TutorialStepId.Intro:
				GoToStep(
					TutorialStepId.Goal
				);
				break;

			case TutorialStepId.Goal:
				GoToStep(
					TutorialStepId.CardExplanation
				);
				break;

			case TutorialStepId.CardExplanation:
				AdvanceCardExplanation();
				break;

			case TutorialStepId.Water:
				if (_pendingGrowthResult != null &&
					!_hasShownGrowth)
				{
					GoToStep(
						TutorialStepId.Growth
					);
				}
				else
				{
					HideTutorialButKeepWatching();
				}
				break;

			case TutorialStepId.Growth:
				if (_pendingSpreadResult != null &&
					!_hasShownSpread)
				{
					GoToStep(
						TutorialStepId.Spread
					);
				}
				else
				{
					HideTutorialButKeepWatching();
				}
				break;

			case TutorialStepId.Spread:
				_unlockEventsAfterNextEventPhase =
					true;

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
				break;
		}
	}

	private void OnBack()
	{
		if (_currentStep !=
			TutorialStepId.CardExplanation)
		{
			return;
		}

		if (_cardExplanationPage ==
			CardExplanationPage.Intro)
		{
			return;
		}

		_cardExplanationPage--;

		ShowCardExplanationPage();
	}

	private void GoToStep(
		TutorialStepId step)
	{
		if (step ==
				TutorialStepId.CardExplanation &&
			_currentStep !=
				TutorialStepId.CardExplanation)
		{
			_cardExplanationPage =
				CardExplanationPage.Intro;
		}

		_currentStep = step;

		ShowCurrentStep();
	}

	private void AdvanceCardExplanation()
	{
		if (_cardExplanationPage <
			CardExplanationPage.Description)
		{
			_cardExplanationPage++;
			ShowCardExplanationPage();
			return;
		}

		_overlay?.HideCardExplanation();

		GoToStep(
			TutorialStepId.WaitForMossPlacement
		);
	}

	public bool CanPlayCard(
		CardData card,
		HexTileData tile)
	{
		if (_isFinished)
			return true;

		if (!_isTutorialVisible)
			return true;

		if (card == null || tile == null)
			return false;

		if (_currentStep ==
			TutorialStepId.WaitForMossPlacement)
		{
			if (card.CardType != CardType.Plant)
				return false;

			if (card.PlantType != PlantType.Moss)
				return false;

			if (!_requiredMossPlacementCoord.HasValue)
				return false;

			return tile.Coord.Equals(
				_requiredMossPlacementCoord.Value
			);
		}

		if (_currentStep ==
			TutorialStepId.OptionalCardPlay)
		{
			return false;
		}

		return false;
	}

	public bool CanEndTurn()
	{
		if (_isFinished)
			return true;

		if (!_isTutorialVisible)
			return true;

		return _currentStep ==
			TutorialStepId.OptionalCardPlay;
	}

	public void RefreshTutorialHighlights()
	{
		if (_isFinished ||
			!_isTutorialVisible)
		{
			return;
		}

		if (_currentStep ==
			TutorialStepId.WaitForMossPlacement)
		{
			HighlightFirstPlayableTileFor(
				PlantType.Moss
			);

			return;
		}

		if (_currentStep ==
			TutorialStepId.OptionalCardPlay)
		{
			const string path =
				"UI/CanvasLayer/GameHub/EndTurnButton";

			HighlightNode(path);
			PositionHintNearNode(path);
		}
	}

	private void ShowCurrentStep()
	{
		ClearHighlights();

		_isTutorialVisible = true;

		if (_currentStep ==
			TutorialStepId.CardExplanation)
		{
			ShowCardExplanationPage();
			return;
		}

		if (_currentStep ==
			TutorialStepId.Intro)
		{
			_overlay.ShowModal();
		}
		else
		{
			_overlay.ShowHint();
		}

		_overlay.SetNavigation(
			canGoBack: false,
			isLastStep:
				_currentStep ==
				TutorialStepId.PlantDeath
		);

		bool waitsForAction =
			_currentStep ==
				TutorialStepId.WaitForMossPlacement ||
			_currentStep ==
				TutorialStepId.OptionalCardPlay;

		_overlay.SetNextButtonVisible(
			!waitsForAction
		);

		_overlay.SetBackButtonVisible(false);

		switch (_currentStep)
		{
			case TutorialStepId.Intro:
				SetTitle("Einstieg");

				SetText(
					"Die Natur ist aus dem Gleichgewicht geraten.\n\n" +
					"Lass dieses kleine Ökosystem wachsen."
				);
				break;

			case TutorialStepId.Goal:
				SetTitle("Ziel");

				SetText(
					"Baue ein stabiles Ökosystem auf und halte die alte Eiche am Leben."
				);

				HighlightCenterTile();

				PositionHintDefault();
				break;

			case TutorialStepId.WaitForMossPlacement:
				_cardHand?.SetCardInteractionFilter(
					IsMossCard
				);

				SetTitle("Deine erste Karte");

				SetText(
					"Ziehe die Mooskarte auf das leuchtende Feld.\n\n" +
					"Für deinen ersten Zug beginnen wir nur mit dieser Karte."
				);

				HighlightFirstPlayableTileFor(
					PlantType.Moss
				);

				PositionHintDefault();
				break;

			case TutorialStepId.OptionalCardPlay:
			{
				_cardHand?.SetCardInteractionFilter(
					_ => false
				);

				SetTitle("Runde beenden");

				SetText(
					"Geschafft!\n\n" +
					"Normalerweise entscheidest du selbst, welche und wie viele Karten du in einer Runde spielst.\n\n" +
					"Achte dabei auf den Wasserverbrauch: Zu viele neue Pflanzen auf einmal können deinen Vorrat schnell senken.\n\n" +
					"Beende jetzt die Runde."
				);

				const string path =
					"UI/CanvasLayer/GameHub/EndTurnButton";

				HighlightNode(path);
				PositionHintNearNode(path);

				break;
			}

			case TutorialStepId.Water:
			{
				_hasShownWater = true;

				SetTitle("Wasserhaushalt");

				SetText(
					"Hier siehst du deinen Wasservorrat und die Veränderung dieser Runde.\n\n" +
					"Alle Pflanzen verbrauchen Wasser. Ausgewachsene Pflanzen können Wasser produzieren.\n\n" +
					"Fällt der Vorrat auf 0, verdorrt die alte Eiche."
				);

				const string path =
					"UI/CanvasLayer/GameHub/WaterLabel";

				HighlightNode(path);
				PositionHintNearNode(
					path,
					alignTop: true
				);

				break;
			}

			case TutorialStepId.Growth:
				_hasShownGrowth = true;

				SetTitle("Wachstum");

			SetText(
				"Dein Moos ist gewachsen!\n\n" +
				"Mit jeder Runde kommen Pflanzen ihrer ausgewachsenen Form näher.\n\n" +
				"Ausgewachsene Pflanzen können Wasser produzieren und sich verbreiten."
			);

				HighlightMossInGrowthResult();

				PositionHintDefault();
				break;

			case TutorialStepId.Spread:
				_hasShownSpread = true;

				SetTitle("Verbreitung");

			SetText(
				"Eine deiner ausgewachsenen Pflanzen hat sich verbreitet!\n\n" +
				"Ursprung und neues Feld sind hervorgehoben.\n\n" +
				"Tipp: Klicke auf eine Pflanze, um genauere Infos zu sehen (Wasserverbrauch, Wasserproduktion, Wachstumsstand)."
			);

				if (_pendingSpreadResult != null)
				{
					foreach (
						PlantSpreadResult spread
						in _pendingSpreadResult.Spreads)
					{
						_boardManager
							.GetTileView(
								spread.SourceCoord
							)?
							.SetTutorialHighlight(
								true
							);

						_boardManager
							.GetTileView(
								spread.TargetCoord
							)?
							.SetTutorialHighlight(
								true
							);
					}
				}

				PositionHintDefault();
				break;

			case TutorialStepId.Event:
			{
				_hasShownEvent = true;

				SetTitle("Ereignis");

				SetText(
					"Ein Ereignis ist eingetreten!\n\n" +
					"Ereignisse können die Bedingungen in deinem Ökosystem vorübergehend verändern."
				);

				const string path =
					"UI/CanvasLayer/GameHub/EventDisplay";

				HighlightNodeWithHeightOverride(
					path,
					340.0f
				);

				PositionHintNearNode(path);

				break;
			}

			case TutorialStepId.PlantDeath:
				_hasShownPlantDeath = true;

				SetTitle("Pflanze stirbt");

				SetText(
					"Leider konnten nicht alle deine Pflanzen überleben.\n\n" +
					"Stirbt eine Pflanze aufgrund aktueller Bedingungen, verschwindet sie vom Spielfeld.\n\n" +
					"Das Feld bleibt danach 2 Runden gesperrt."
				);

				if (_pendingEventResult != null)
				{
					foreach (
						PlantDeathResult death
						in _pendingEventResult.PlantDeaths)
					{
						_boardManager
							.GetTileView(
								death.Coord
							)?
							.SetTutorialHighlight(
								true
							);
					}
				}

				PositionHintDefault();
				break;
		}
	}

	private void PositionHintDefault()
	{
		_overlay?.SetHintPosition(
			TutorialOverlay.HintPosition.TopLeft
		);
	}

	private void PositionHintNearNode(
		string path,
		bool alignTop = false)
	{
		if (_overlay == null)
			return;

		Node node =
			GetTree()
				.CurrentScene?
				.GetNodeOrNull<Node>(path);

		if (node is not Control targetControl)
		{
			PositionHintDefault();
			return;
		}

		Rect2 targetRect =
			targetControl.GetGlobalRect();

		_overlay.PositionHintNear(
			targetRect,
			alignTop
		);
	}

	private void ShowCardExplanationPage()
	{
		PlantDefinition moss =
			PlantDatabase.Get(
				PlantType.Moss
			);

		if (moss == null ||
			moss.CardImage == null)
		{
			GD.PrintErr(
				"TutorialManager: Moos-Kartendaten oder Kartenbild fehlen."
			);

			GoToStep(
				TutorialStepId.WaitForMossPlacement
			);

			return;
		}

		_cardHand?.SetCardInteractionFilter(
			IsMossCard
		);

		_overlay.ShowCardExplanation(
			moss.CardImage
		);

		bool isLastPage =
			_cardExplanationPage ==
			CardExplanationPage.Description;

		_overlay.SetCardNavigation(
			canGoBack:
				_cardExplanationPage !=
				CardExplanationPage.Intro,
			isLastPage: isLastPage
		);

		switch (_cardExplanationPage)
		{
			case CardExplanationPage.Intro:
				_overlay.SetCardExplanation(
					"Pflanzenkarten",
					"Schauen wir uns den Aufbau der Pflanzenkarten näher an.",
					new Rect2()
				);

				_overlay.SetCardHighlightVisible(false);
				break;

			case CardExplanationPage.Light:
				_overlay.SetCardExplanation(
					"Lichtbedarf",
					$"Hier siehst du, bei welchen Lichtverhältnissen du die Pflanze platzieren kannst.\n\n" +
					$"Moos: {FormatLightLevels(moss)}",
					new Rect2(
						0.055f,
						0.047f,
						0.19f,
						0.115f
					)
				);
				break;

			case CardExplanationPage.Growth:
				_overlay.SetCardExplanation(
					"Wachstumsdauer",
					$"So viele Wachstumsschritte braucht die Pflanze, bis sie ausgewachsen ist.\n\n" +
					$"Moos: {moss.GrowthRounds} Schritte",
					new Rect2(
						0.755f,
						0.047f,
						0.19f,
						0.115f
					)
				);
				break;

			case CardExplanationPage.WaterConsumption:
				_overlay.SetCardExplanation(
					"Wasserverbrauch",
					$"So viel Wasser verbraucht die Pflanze pro Runde.\n\n" +
					$"Moos: {moss.WaterConsumption}",
					new Rect2(
						0.127f,
						0.252f,
						0.20f,
						0.115f
					)
				);
				break;

			case CardExplanationPage.WaterProduction:
				_overlay.SetCardExplanation(
					"Wasserproduktion",
					$"So viel Wasser produziert die Pflanze, sobald sie ausgewachsen ist.\n\n" +
					$"Moos: +{moss.WaterProduction}",
					new Rect2(
						0.090f,
						0.364f,
						0.20f,
						0.115f
					)
				);
				break;

			case CardExplanationPage.Description:
			{
				string description =
					string.IsNullOrWhiteSpace(
						moss.Description
					)
						? "Hier steht die besondere Stärke der Pflanze."
						: moss.Description;

				_overlay.SetCardExplanation(
					"Kartentext",
					"Hier wird die besondere Eigenschaft der Pflanze beschrieben.\n\n" +
					description,
					new Rect2(
						0.10f,
						0.81f,
						0.80f,
						0.152f
					)
				);

				break;
			}
		}
	}

	private string FormatLightLevels(
		PlantDefinition plant)
	{
		if (plant?.AllowedLightLevels == null ||
			plant.AllowedLightLevels.Count == 0)
		{
			return "keine Angabe";
		}

		List<string> names = new();

		foreach (
			LightLevel lightLevel
			in plant.AllowedLightLevels)
		{
			string name =
				lightLevel switch
				{
					LightLevel.Sun =>
						"Sonne",

					LightLevel.PartialShade =>
						"Halbschatten",

					LightLevel.Shade =>
						"Schatten",

					_ =>
						lightLevel.ToString()
				};

			names.Add(name);
		}

		return string.Join(
			", ",
			names
		);
	}

	private bool IsMossCard(
		CardData card)
	{
		return card != null &&
			card.CardType == CardType.Plant &&
			card.PlantType == PlantType.Moss;
	}

	private void OnPlantPlaced(
		PlantType plantType,
		HexCoord coord)
	{
		if (_currentStep !=
			TutorialStepId.WaitForMossPlacement)
		{
			return;
		}

		if (plantType != PlantType.Moss)
			return;

		GoToStep(
			TutorialStepId.OptionalCardPlay
		);
	}

	private void OnEndTurnRequested(
		int round)
	{
		if (_currentStep !=
			TutorialStepId.OptionalCardPlay)
		{
			return;
		}

		_cardHand?
			.ClearCardInteractionFilter();
	}

	private void OnWaterPhaseResolved(
		WaterPhaseResult result)
	{
		if (_hasShownWater)
			return;

		if (_currentStep !=
			TutorialStepId.OptionalCardPlay)
		{
			return;
		}

		GoToStep(
			TutorialStepId.Water
		);
	}

	private void OnGrowthPhaseResolved(
		GrowthPhaseResult result)
	{
		if (_hasShownGrowth)
			return;

		if (!_hasShownWater)
			return;

		if (result == null ||
			result.Plants == null ||
			result.Plants.Count == 0)
		{
			return;
		}

		if (!GrowthResultContainsMoss(result))
			return;

		_pendingGrowthResult = result;

		if (_isTutorialVisible)
			return;

		GoToStep(
			TutorialStepId.Growth
		);
	}

	private void OnSpreadPhaseResolved(
		SpreadPhaseResult result)
	{
		if (_hasShownSpread)
			return;

		if (!_hasShownGrowth)
			return;

		if (result == null ||
			result.Spreads == null ||
			result.Spreads.Count == 0)
		{
			return;
		}

		_pendingSpreadResult = result;

		if (_isTutorialVisible)
			return;

		GoToStep(
			TutorialStepId.Spread
		);
	}

	private void OnEventPhaseResolved(
		EventPhaseResult result)
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

			GoToStep(
				TutorialStepId.PlantDeath
			);

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

			GoToStep(
				TutorialStepId.Event
			);

			UnlockEventsAfterBufferRoundIfNeeded();
			return;
		}

		UnlockEventsAfterBufferRoundIfNeeded();
	}

	private void UnlockEventsAfterBufferRoundIfNeeded()
	{
		if (!_unlockEventsAfterNextEventPhase)
			return;

		_unlockEventsAfterNextEventPhase =
			false;

		if (_turnManager != null)
			_turnManager.Config.EventsUnlocked = true;
	}

	private bool GrowthResultContainsMoss(
		GrowthPhaseResult result)
	{
		if (result == null ||
			result.Plants == null)
		{
			return false;
		}

		foreach (
			PlantGrowthResult plant
			in result.Plants)
		{
			if (plant.PlantType ==
				PlantType.Moss)
			{
				return true;
			}
		}

		return false;
	}

	private void HighlightMossInGrowthResult()
	{
		if (_pendingGrowthResult == null ||
			_pendingGrowthResult.Plants == null)
		{
			return;
		}

		foreach (
			PlantGrowthResult plant
			in _pendingGrowthResult.Plants)
		{
			if (plant.PlantType !=
				PlantType.Moss)
			{
				continue;
			}

			_boardManager
				.GetTileView(
					plant.Coord
				)?
				.SetTutorialHighlight(
					true
				);
		}
	}

	private void SetTitle(
		string title)
	{
		_overlay?.SetTitle(title);
	}

	private void SetText(
		string text)
	{
		_overlay?.SetText(text);
	}

	private void HighlightNode(
		string path)
	{
		Node node =
			GetTree()
				.CurrentScene
				.GetNodeOrNull<Node>(path);

		if (node is not Control targetControl)
			return;

		if (_overlay == null)
			return;

		ClearHighlightedNode(path);

		Rect2 targetRect =
			targetControl.GetGlobalRect();

		CreateHighlightFrame(
			path,
			targetRect.Position -
				new Vector2(8, 8),
			targetRect.Size +
				new Vector2(16, 16)
		);
	}

	private void HighlightNodeWithHeightOverride(
		string path,
		float height)
	{
		Node node =
			GetTree()
				.CurrentScene
				.GetNodeOrNull<Node>(path);

		if (node is not Control targetControl)
			return;

		if (_overlay == null)
			return;

		ClearHighlightedNode(path);

		Rect2 targetRect =
			targetControl.GetGlobalRect();

		Vector2 finalPosition =
			new Vector2(
				targetRect.Position.X - 8.0f,
				targetRect.Position.Y - 8.0f
			);

		Vector2 finalSize =
			new Vector2(
				targetRect.Size.X + 16.0f,
				height
			);

		CreateHighlightFrame(
			path,
			finalPosition,
			finalSize
		);
	}

	private void CreateHighlightFrame(
		string path,
		Vector2 position,
		Vector2 size)
	{
		Panel highlightFrame =
			new Panel();

		highlightFrame.Name =
			"TutorialHighlightFrame";

		highlightFrame.MouseFilter =
			Control.MouseFilterEnum.Ignore;

		highlightFrame.ZIndex = 100;
		highlightFrame.Position = position;
		highlightFrame.Size = size;
		highlightFrame.Modulate =
			Colors.White;

		StyleBoxFlat style =
			new StyleBoxFlat();

		style.BgColor =
			new Color(
				1,
				1,
				1,
				0.0f
			);

		style.BorderColor =
			new Color(
				1,
				1,
				1,
				0.55f
			);

		style.BorderWidthLeft = 3;
		style.BorderWidthTop = 3;
		style.BorderWidthRight = 3;
		style.BorderWidthBottom = 3;

		style.CornerRadiusTopLeft = 12;
		style.CornerRadiusTopRight = 12;
		style.CornerRadiusBottomRight = 12;
		style.CornerRadiusBottomLeft = 12;

		style.ShadowColor =
			new Color(
				1,
				1,
				1,
				0.18f
			);

		style.ShadowSize = 8;

		highlightFrame
			.AddThemeStyleboxOverride(
				"panel",
				style
			);

		_overlay.AddChild(
			highlightFrame
		);

		_highlightFrames[path] =
			highlightFrame;
	}

	private void HighlightCenterTile()
	{
		BoardManager board =
			_boardManager ??
			GetTree()
				.CurrentScene
				.GetNodeOrNull<BoardManager>(
					"BoardManager"
				);

		if (board == null)
			return;

		HexTile tileView =
			board.GetTileView(
				new HexCoord(0, 0)
			);

		if (tileView == null)
			return;

		tileView.SetTutorialHighlight(
			true
		);
	}

	private void HighlightFirstPlayableTileFor(
		PlantType plantType)
	{
		BoardManager board =
			_boardManager ??
			GetTree()
				.CurrentScene
				.GetNodeOrNull<BoardManager>(
					"BoardManager"
				);

		if (board == null)
			return;

		PlantDefinition plant =
			PlantDatabase.Get(
				plantType
			);

		if (plant == null)
			return;

		HexCoord preferredCoord =
			new HexCoord(1, 0);

		HexTileData preferredTileData =
			board.BoardData.GetTile(
				preferredCoord
			);

		if (preferredTileData != null &&
			preferredTileData.CanPlacePlant(
				plant
			))
		{
			_requiredMossPlacementCoord =
				preferredCoord;

			board.GetTileView(
				preferredCoord
			)?
			.SetTutorialHighlight(
				true
			);

			return;
		}

		foreach (
			HexCoord coord
			in board.BoardData.Tiles.Keys)
		{
			HexTileData tileData =
				board.BoardData.GetTile(
					coord
				);

			if (tileData != null &&
				tileData.CanPlacePlant(
					plant
				))
			{
				_requiredMossPlacementCoord =
					coord;

				board.GetTileView(
					coord
				)?
				.SetTutorialHighlight(
					true
				);

				return;
			}
		}
	}

	private void ClearAllHighlightFrames()
	{
		foreach (
			Panel highlightFrame
			in _highlightFrames.Values)
		{
			if (IsInstanceValid(
				highlightFrame
			))
			{
				highlightFrame.QueueFree();
			}
		}

		_highlightFrames.Clear();
	}

	private void ClearHighlights()
	{
		BoardManager board =
			_boardManager ??
			GetTree()
				.CurrentScene
				.GetNodeOrNull<BoardManager>(
					"BoardManager"
				);

		if (board != null)
		{
			foreach (
				HexCoord coord
				in board.BoardData.Tiles.Keys)
			{
				HexTile tileView =
					board.GetTileView(
						coord
					);

				if (tileView == null)
					continue;

				tileView
					.ClearTutorialHighlight();

				tileView
					.ClearPlacementPreview();
			}
		}

		ClearHighlightedNode(
			"UI/CanvasLayer/GameHub/WaterLabel"
		);

		ClearHighlightedNode(
			"UI/CanvasLayer/GameHub/EndTurnButton"
		);

		ClearHighlightedNode(
			"UI/CanvasLayer/CardHand"
		);

		ClearHighlightedNode(
			"UI/CanvasLayer/GameHub/EventDisplay"
		);

		ClearAllHighlightFrames();
	}

	private void ClearHighlightedNode(
		string path)
	{
		if (!_highlightFrames.TryGetValue(
			path,
			out Panel highlightFrame))
		{
			return;
		}

		if (IsInstanceValid(
			highlightFrame
		))
		{
			highlightFrame.QueueFree();
		}

		_highlightFrames.Remove(
			path
		);
	}

	private void EndTutorial()
	{
		_cardHand?
			.ClearCardInteractionFilter();

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
	}
}