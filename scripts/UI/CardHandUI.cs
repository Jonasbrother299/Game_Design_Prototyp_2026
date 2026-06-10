using Godot;
using System.Collections.Generic;

public partial class CardHandUI : Control
{
	[Export] public Vector2 CardSize = new Vector2(95, 140);
	[Export] public int StartHandSize = 3;

	[Export] public float HoverLift = 90.0f;
	[Export] public float HoverScale = 1.25f;
	[Export] public float HoverDuration = 0.14f;

	[Export] public float CardSpacing = 65.0f;
	[Export] public float FanRotationDegrees = 7.0f;
	[Export] public float FanYOffset = 14.0f;

	[Export] public float HandHeight = 320.0f;
	[Export] public float HandBottomOffset = 20.0f;

	private readonly string[] _cardPaths =
	{
		"res://assets/cards/card_baum.jpeg",
		"res://assets/cards/card_flechte.jpeg",
		"res://assets/cards/card_moos.png",
        "res://assets/cards/card_pilz.jpeg"
	};

	private readonly RandomNumberGenerator _rng = new();

	private readonly Dictionary<TextureRect, Tween> _tweens = new();
	private readonly Dictionary<TextureRect, Vector2> _basePositions = new();
	private readonly Dictionary<TextureRect, float> _baseRotations = new();
	private readonly Dictionary<TextureRect, int> _baseZIndexes = new();

	public override void _Ready()
	{
		_rng.Randomize();
		SetupPosition();
		CallDeferred(nameof(LoadCards));
	}

	private void SetupPosition()
	{
		AnchorLeft = 0.0f;
		AnchorRight = 1.0f;
		AnchorTop = 1.0f;
		AnchorBottom = 1.0f;

		OffsetLeft = 0.0f;
		OffsetRight = 0.0f;

		// weiter hoch = größeren BottomOffset oder größere negative Top-Position
		OffsetTop = -(HandHeight + HandBottomOffset);
		OffsetBottom = -HandBottomOffset;
	}

	private void LoadCards()
	{
		foreach (Node child in GetChildren())
			child.QueueFree();

		List<string> cardPaths = new(_cardPaths);
		Shuffle(cardPaths);

		int cardsToDraw = Mathf.Min(StartHandSize, cardPaths.Count);

		for (int i = 0; i < cardsToDraw; i++)
		{
			Texture2D texture = GD.Load<Texture2D>(cardPaths[i]);

			if (texture == null)
			{
				GD.PrintErr($"Card image not found: {cardPaths[i]}");
				continue;
			}

			CreateCard(texture, cardPaths[i], i, cardsToDraw);
		}
	}

	private void CreateCard(Texture2D texture, string path, int index, int totalCards)
	{
		TextureRect card = new TextureRect();

		card.Texture = texture;

		// DAS ist der wichtige Teil: nicht Originalgröße vom Bild behalten
		card.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		card.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

		card.CustomMinimumSize = CardSize;
		card.Size = CardSize;
		card.PivotOffset = CardSize / 2.0f;
		card.MouseFilter = MouseFilterEnum.Stop;

		float centerIndex = (totalCards - 1) / 2.0f;
		float relativeIndex = index - centerIndex;

		Vector2 viewportSize = GetViewportRect().Size;

		float centerX = viewportSize.X / 2.0f - CardSize.X / 2.0f;
		float baseX = centerX + relativeIndex * CardSpacing;

		// Erstmal bewusst relativ weit unten im CardHand-Bereich
		float baseY = 80.0f + Mathf.Abs(relativeIndex) * FanYOffset;
	
		Vector2 basePosition = new Vector2(baseX, baseY);
		float baseRotation = relativeIndex * FanRotationDegrees;

		card.Position = basePosition;
		card.RotationDegrees = baseRotation;
		card.ZIndex = index;

		_basePositions[card] = basePosition;
		_baseRotations[card] = baseRotation;
		_baseZIndexes[card] = index;

		card.MouseEntered += () => AnimateCard(card, true);
		card.MouseExited += () => AnimateCard(card, false);

		card.GuiInput += inputEvent =>
		{
			if (inputEvent is InputEventMouseButton mouseButton &&
				mouseButton.Pressed &&
				mouseButton.ButtonIndex == MouseButton.Left)
			{
				GD.Print($"Selected card: {path}");
			}
		};

		AddChild(card);
	}

	private void AnimateCard(TextureRect card, bool isHovering)
	{
		if (_tweens.TryGetValue(card, out Tween oldTween))
		{
			oldTween.Kill();
			_tweens.Remove(card);
		}

		Vector2 basePosition = _basePositions[card];
		float baseRotation = _baseRotations[card];

		Vector2 targetPosition = isHovering
			? basePosition + new Vector2(0.0f, -HoverLift)
			: basePosition;

		Vector2 targetScale = isHovering
			? new Vector2(HoverScale, HoverScale)
			: Vector2.One;

		float targetRotation = isHovering ? 0.0f : baseRotation;

		card.ZIndex = isHovering ? 100 : _baseZIndexes[card];

		Tween tween = CreateTween();
		tween.SetParallel(true);

		tween.TweenProperty(card, "position", targetPosition, HoverDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(card, "scale", targetScale, HoverDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(card, "rotation_degrees", targetRotation, HoverDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		_tweens[card] = tween;
	}

	private void Shuffle(List<string> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int randomIndex = _rng.RandiRange(0, i);

			string temp = list[i];
			list[i] = list[randomIndex];
			list[randomIndex] = temp;
		}
	}
}
