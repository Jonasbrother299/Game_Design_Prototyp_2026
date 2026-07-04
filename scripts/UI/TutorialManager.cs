using Godot;
using System;

public partial class TutorialManager : Node
{
    private GameHub _hub;
    private BoardManager _boardManager;
    private CardHandUI _cardHand;
    private TurnManager _turnManager;

    private Control _panel;
    private TextureRect _cardImage;
    private Label _cardInfo;
    private Label _titleLabel;
    private Label _textLabel;
    private Button _nextButton;
    private Button _backButton;
    private HBoxContainer _progressDots;

    private int _step = 0;
    private const int TotalSteps = 9;

    public void Start(GameHub hub, BoardManager boardManager, CardHandUI cardHand, TurnManager turnManager)
    {
        _hub = hub;
        _boardManager = boardManager;
        _cardHand = cardHand;
        _turnManager = turnManager;

        // Find tutorial panel elements
        _panel = _hub?.GetNodeOrNull<Control>("TutorialPanel") ?? GetTree().CurrentScene.GetNodeOrNull<Control>("UI/CanvasLayer/GameHub/TutorialPanel");

        if (_panel == null)
        {
            GD.PrintErr("TutorialManager: TutorialPanel not found.");
            return;
        }

        _titleLabel = _panel.GetNodeOrNull<Label>("CenterContainer/TutorialWindow/TutorialLayoutVBox/TutorialTitle");
        _textLabel = _panel.GetNodeOrNull<Label>("CenterContainer/TutorialWindow/TutorialLayoutVBox/ScrollContainer/ScrollContent/TutorialText");
        _cardImage = _panel.GetNodeOrNull<TextureRect>("CenterContainer/TutorialWindow/TutorialLayoutVBox/ScrollContainer/ScrollContent/TutorialContentHBox/TutorialCardImage");
        _cardInfo = _panel.GetNodeOrNull<Label>("CenterContainer/TutorialWindow/TutorialLayoutVBox/ScrollContainer/ScrollContent/TutorialContentHBox/TutorialCardInfo");
        _nextButton = _panel.GetNodeOrNull<Button>("CenterContainer/TutorialWindow/TutorialLayoutVBox/HBoxContainer/TutorialNextButton");
        _backButton = _panel.GetNodeOrNull<Button>("CenterContainer/TutorialWindow/TutorialLayoutVBox/HBoxContainer/TutorialBackButton");
        _progressDots = _panel.GetNodeOrNull<HBoxContainer>("CenterContainer/TutorialWindow/TutorialLayoutVBox/ProgressDots");

        if (_nextButton != null)
            _nextButton.Pressed += OnNext;

        if (_backButton != null)
            _backButton.Pressed += OnBack;

        _hub?.ShowTutorialPanel();
        _panel.Show();
        _step = 0;
        ShowStep(_step);
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

        if (_nextButton != null)
            _nextButton.Text = step == TotalSteps - 1 ? "Beenden" : "Weiter";

        if (_backButton != null)
            _backButton.Disabled = step == 0;

        UpdateProgressDots(step);

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
                var moss = PlantDatabase.Get(PlantType.Moss);
                ShowCard(moss.CardImage, $"{moss.DisplayName}\nProduktion: {moss.WaterProduction} - Verbrauch: {moss.WaterConsumption}\nWächst nach {moss.GrowthRounds} Runden\nVerbreitung: 1/{moss.SpreadChanceDenominator}");
                break;

            case 3:
                SetTitle("Karten: Blume");
                var flower = PlantDatabase.Get(PlantType.Flower);
                ShowCard(flower.CardImage, $"{flower.DisplayName}\nProduktion: {flower.WaterProduction} - Verbrauch: {flower.WaterConsumption}\nWächst nach {flower.GrowthRounds} Runden\nVerbreitung: 1/{flower.SpreadChanceDenominator}");
                break;

            case 4:
                SetTitle("Karten: Pilz");
                var mush = PlantDatabase.Get(PlantType.Mushroom);
                ShowCard(mush.CardImage, $"{mush.DisplayName}\nProduktion: {mush.WaterProduction} - Verbrauch: {mush.WaterConsumption}\nWächst nach {mush.GrowthRounds} Runden\nVerbreitung: 1/{mush.SpreadChanceDenominator}");
                break;

            case 5:
                SetTitle("Karten: Birke");
                var birch = PlantDatabase.Get(PlantType.Birch);
                ShowCard(birch.CardImage, $"{birch.DisplayName}\nProduktion: {birch.WaterProduction} - Verbrauch: {birch.WaterConsumption}\nWächst nach {birch.GrowthRounds} Runden\nVerbreitung: 1/{birch.SpreadChanceDenominator}");
                break;

            case 6:
                SetTitle("Ereignisse");
                SetText("Ereignisse wie Regen oder Dürre verändern das Wasser. Sie können das Ökosystem stark beeinflussen.");
                ShowCard(null, "Beispiel: Regen → +3 Wasser. Starkregen → +5 Wasser (mit Risiken).");
                break;

            case 7:
                SetTitle("Spielfeld");
                SetText("Das Spielfeld besteht aus Hexfeldern. Pflanzen werden per Drag & Drop platziert. Manche Pflanzen bevorzugen Schatten oder Sonne.");
                HighlightCenterTile();
                break;

            case 8:
                SetTitle("Handkarten");
                SetText("Jetzt bist du bereit. Wähle deine erste Karte und ziehe sie per Drag & Drop auf ein Feld deiner Wahl.");
                HighlightNode("UI/CanvasLayer/CardHand");
                break;

            default:
                EndTutorial();
                break;
        }
    }

    private void UpdateProgressDots(int currentStep)
    {
        if (_progressDots == null)
            return;

        foreach (Node child in _progressDots.GetChildren())
            child.QueueFree();

        for (int index = 0; index < TotalSteps; index++)
        {
            var dot = new Label();
            dot.Text = index == currentStep ? "●" : "○";
            dot.HorizontalAlignment = HorizontalAlignment.Center;
            dot.VerticalAlignment = VerticalAlignment.Center;
            dot.CustomMinimumSize = new Vector2(18, 18);
            dot.SizeFlagsHorizontal = Control.SizeFlags.Fill;
            dot.SizeFlagsVertical = Control.SizeFlags.Fill;
            dot.AddThemeFontSizeOverride("font_size", 18);
            _progressDots.AddChild(dot);
        }
    }

    private void SetTitle(string title)
    {
        if (_titleLabel != null)
            _titleLabel.Text = title;
    }

    private void SetText(string text)
    {
        if (_textLabel != null)
            _textLabel.Text = text;
    }

    private void ShowCard(Texture2D tex, string info)
    {
        if (_cardImage != null)
        {
            _cardImage.Texture = tex;
            _cardImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _cardImage.CustomMinimumSize = new Vector2(220, 320);
        }

        if (_cardInfo != null)
        {
            _cardInfo.Text = info ?? "";
            _cardInfo.AddThemeFontSizeOverride("font_size", 16);
        }
    }

    private void HighlightHand()
    {
        Node handNode = GetTree().CurrentScene.GetNodeOrNull("UI/CanvasLayer/CardHand");

        if (handNode == null)
            return;

        foreach (Node child in handNode.GetChildren())
        {
            if (child is TextureRect card)
            {
                card.Modulate = Colors.White;
                var tween = CreateTween();
                tween.TweenProperty(card, "scale", new Vector2(1.08f, 1.08f), 0.28f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                tween.SetLoops(4);
            }
        }
    }

    private void HighlightNode(string path)
    {
        Node node = GetTree().CurrentScene.GetNodeOrNull<Node>(path);

        if (node == null)
            return;

        // animate modulate if possible
        var canvasItem = node as CanvasItem;

        if (canvasItem != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(canvasItem, "modulate", new Color(1.0f, 0.9f, 0.5f), 0.45f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tween.SetLoops(4);
        }
    }

    private void HighlightCenterTile()
    {
        var board = _boardManager ?? GetTree().CurrentScene.GetNodeOrNull<BoardManager>("BoardManager");

        if (board == null)
            return;

        var center = new HexCoord(0, 0);
        var tileView = board.GetTileView(center);

        if (tileView != null)
        {
            tileView.SetPlacementPreview(true);
            // remove preview after short delay
            GetTree().CreateTimer(1.2f).Timeout += () => tileView.ClearPlacementPreview();
        }
    }

    private void ClearHighlights()
    {
        // clear any placement previews
        var board = _boardManager ?? GetTree().CurrentScene.GetNodeOrNull<BoardManager>("BoardManager");

        if (board != null)
        {
            foreach (var coordTile in board.BoardData.Tiles)
            {
                var view = board.GetTileView(coordTile.Key);
                view?.ClearPlacementPreview();
            }
        }
    }

    private void EndTutorial()
    {
        _panel?.Hide();
        QueueFree();
    }
}
