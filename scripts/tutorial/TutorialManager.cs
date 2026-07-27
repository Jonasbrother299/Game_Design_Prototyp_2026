using Godot;

public partial class TutorialManager : Node
{
	private TutorialOverlay _overlay;
	private BoardManager _boardManager;
	private CardHandUI _cardHand;
	private TurnManager _turnManager;

	private int _step;
	private const int TotalSteps = 9;

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
		_overlay.ShowOverlay();

		_step = 0;
		ShowStep(_step);
	}

	public override void _ExitTree()
	{
		if (_overlay == null)
			return;

		_overlay.NextRequested -= OnNext;
		_overlay.BackRequested -= OnBack;
	}

	private void OnNext()
	{
		if (_step >= TotalSteps - 1)
		{
			EndTutorial();
			return;
		}

		_step++;
		ShowStep(_step);
	}

	private void OnBack()
	{
		if (_step <= 0)
			return;

		_step--;
		ShowStep(_step);
	}

	private void ShowStep(int step)
	{
		if (step < 0 || step >= TotalSteps)
		{
			EndTutorial();
			return;
		}

		ClearHighlights();
		_overlay.SetNavigation(canGoBack: step > 0, isLastStep: step == TotalSteps - 1);
		_overlay.SetProgress(step, TotalSteps);

		switch (step)
		{
			case 0:
				SetTitle("Willkommen");
				SetText("Du kümmerst dich um ein kleines Ökosystem. Die Eiche in der Mitte ist das Herz des Waldes. Ziel: Wasserhaushalt stabil halten und die Eiche wachsen lassen.");
				ShowCard(null, "Spielstart: Du hast 3 Karten. Ziehe eine Karte auf ein Feld, um sie zu platzieren.");
				break;

			case 1:
				SetTitle("Wasseranzeige");
				SetText("Das Wasser reicht von 0 (Verlust) bis 50 (Sieg). Achte auf den Wasserwert.");
				HighlightNode("UI/CanvasLayer/GameHub/WaterLabel");
				ShowCard(null, "Wasser: Der wichtigste Wert. 50 = Sieg, 0 = Niederlage.");
				break;

			case 2:
				SetTitle("Karten: Moos");
				PlantDefinition moss = PlantDatabase.Get(PlantType.Moss);
				ShowCard(moss.CardImage, $"{moss.DisplayName}\nProduktion: {moss.WaterProduction} - Verbrauch: {moss.WaterConsumption}\nWächst nach {moss.GrowthRounds} Runden\nVerbreitung: 1/{moss.SpreadChanceDenominator}");
				break;

			case 3:
				SetTitle("Karten: Blume");
				PlantDefinition flower = PlantDatabase.Get(PlantType.Flower);
				ShowCard(flower.CardImage, $"{flower.DisplayName}\nProduktion: {flower.WaterProduction} - Verbrauch: {flower.WaterConsumption}\nWächst nach {flower.GrowthRounds} Runden\nVerbreitung: 1/{flower.SpreadChanceDenominator}");
				break;

			case 4:
				SetTitle("Karten: Pilz");
				PlantDefinition mushroom = PlantDatabase.Get(PlantType.Mushroom);
				ShowCard(mushroom.CardImage, $"{mushroom.DisplayName}\nProduktion: {mushroom.WaterProduction} - Verbrauch: {mushroom.WaterConsumption}\nWächst nach {mushroom.GrowthRounds} Runden\nVerbreitung: 1/{mushroom.SpreadChanceDenominator}");
				break;

			case 5:
				SetTitle("Karten: Birke");
				PlantDefinition birch = PlantDatabase.Get(PlantType.Birch);
				ShowCard(birch.CardImage, $"{birch.DisplayName}\nProduktion: {birch.WaterProduction} - Verbrauch: {birch.WaterConsumption}\nWächst nach {birch.GrowthRounds} Runden\nVerbreitung: 1/{birch.SpreadChanceDenominator}");
				break;

			case 6:
				SetTitle("Ereignisse");
				SetText("Ereignisse wie Regen oder Dürre verändern das Wasser. Sie können das Ökosystem stark beeinflussen.");
				ShowCard(null, "Beispiel: Regen → +3 Wasser. Starkregen → +4 Wasser mit Risiken.");
				break;

			case 7:
				SetTitle("Spielfeld");
				SetText("Das Spielfeld besteht aus Hexfeldern. Pflanzen werden per Drag-and-drop platziert. Manche Pflanzen bevorzugen Schatten oder Sonne.");
				HighlightCenterTile();
				break;

			case 8:
				SetTitle("Handkarten");
				SetText("Jetzt bist du bereit. Wähle deine erste Karte und ziehe sie auf ein Feld deiner Wahl.");
				HighlightNode("UI/CanvasLayer/CardHand");
				break;
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

	private void ShowCard(Texture2D texture, string info)
	{
		_overlay?.SetCard(texture, info);
	}

	private void HighlightNode(string path)
	{
		Node node = GetTree().CurrentScene.GetNodeOrNull<Node>(path);

		if (node is not CanvasItem canvasItem)
			return;

		Tween tween = CreateTween();
		tween.TweenProperty(canvasItem, "modulate", new Color(1.0f, 0.9f, 0.5f), 0.45f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		tween.SetLoops(4);
	}

	private void HighlightCenterTile()
	{
		BoardManager board = _boardManager ?? GetTree().CurrentScene.GetNodeOrNull<BoardManager>("BoardManager");

		if (board == null)
			return;

		HexTile tileView = board.GetTileView(new HexCoord(0, 0));

		if (tileView == null)
			return;

		tileView.SetPlacementPreview(true);
		GetTree().CreateTimer(1.2f).Timeout += tileView.ClearPlacementPreview;
	}

	private void ClearHighlights()
	{
		BoardManager board = _boardManager ?? GetTree().CurrentScene.GetNodeOrNull<BoardManager>("BoardManager");

		if (board == null)
			return;

		foreach (HexCoord coord in board.BoardData.Tiles.Keys)
			board.GetTileView(coord)?.ClearPlacementPreview();
	}

	private void EndTutorial()
	{
		_overlay?.HideOverlay();
		QueueFree();
	}
}
