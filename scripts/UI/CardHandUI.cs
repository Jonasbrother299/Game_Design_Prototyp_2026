using Godot;
using System;
using System.Collections.Generic;

public partial class CardHandUI : Control
{
	public event Action<PlantType> PlantCardSelected;
	public event Action<PlantType, Vector2> PlantCardDragged;
	public event Action<PlantType, Vector2> PlantCardDragReleased;

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

	[Export] public float HoverHitboxGrow = 18.0f;

	private const float DragScale = 0.45f;

	private const int HoverZIndex = 100;
	private const int DragZIndex = 200;

	private readonly string[] _cardPaths =
	{
		"res://assets/cards/card_baum.jpeg",
		"res://assets/cards/card_flechte.jpeg",
		"res://assets/cards/card_moos.png",
		"res://assets/cards/card_pilz.jpeg"
	};

	private readonly RandomNumberGenerator _rng = new();

	private readonly List<TextureRect> _cards = new();

	private readonly Dictionary<TextureRect, string> _cardPathsByCard = new();
	private readonly Dictionary<TextureRect, Tween> _tweens = new();
	private readonly Dictionary<TextureRect, Vector2> _basePositions = new();
	private readonly Dictionary<TextureRect, float> _baseRotations = new();
	private readonly Dictionary<TextureRect, int> _baseZIndexes = new();

	private TextureRect _hoveredCard;

	private TextureRect _selectedCard;
	private PlantType? _selectedPlantType = null;

	private TextureRect _draggedCard;
	private PlantType? _draggedPlantType = null;
	private Vector2 _dragOffset = Vector2.Zero;
	private bool _isDragging = false;
	private bool _removeDraggedCardAfterRelease = false;

	private int _dragDebugFrameCounter = 0;

	public override void _Ready()
	{
		_rng.Randomize();

		MouseFilter = MouseFilterEnum.Ignore;
		ClipContents = false;

		SetProcess(true);
		SetProcessInput(true);

		SetupPosition();
		CallDeferred(nameof(LoadCards));
	}

	public override void _Process(double delta)
	{
		if (_isDragging)
		{
			UpdateDraggedCard();
			return;
		}

		UpdateHoveredCard();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseButton)
			return;

		if (mouseButton.ButtonIndex != MouseButton.Left)
			return;

		if (mouseButton.Pressed)
		{
			if (_isDragging)
				return;

			UpdateHoveredCard();

			if (_hoveredCard == null)
				return;

			if (!_cardPathsByCard.TryGetValue(_hoveredCard, out string path))
				return;

			SelectCard(_hoveredCard, path);
			StartDrag(_hoveredCard, path);

			GetViewport().SetInputAsHandled();
			return;
		}

		if (!_isDragging || _draggedCard == null)
			return;

		EndDrag();
		GetViewport().SetInputAsHandled();
	}

	private void SetupPosition()
	{
		AnchorLeft = 0.0f;
		AnchorRight = 1.0f;
		AnchorTop = 1.0f;
		AnchorBottom = 1.0f;

		OffsetLeft = 0.0f;
		OffsetRight = 0.0f;

		OffsetTop = -(HandHeight + HandBottomOffset);
		OffsetBottom = -HandBottomOffset;
	}

	private void LoadCards()
	{
		foreach (Node child in GetChildren())
			child.QueueFree();

		_cards.Clear();
		_cardPathsByCard.Clear();
		_tweens.Clear();
		_basePositions.Clear();
		_baseRotations.Clear();
		_baseZIndexes.Clear();

		_hoveredCard = null;

		_selectedCard = null;
		_selectedPlantType = null;

		_draggedCard = null;
		_draggedPlantType = null;
		_isDragging = false;
		_removeDraggedCardAfterRelease = false;
		_dragDebugFrameCounter = 0;

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
		card.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		card.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

		card.CustomMinimumSize = CardSize;
		card.Size = CardSize;
		card.PivotOffset = CardSize / 2.0f;

		card.MouseFilter = MouseFilterEnum.Ignore;
		card.ZAsRelative = false;

		float centerIndex = (totalCards - 1) / 2.0f;
		float relativeIndex = index - centerIndex;

		Vector2 viewportSize = GetViewportRect().Size;

		float centerX = viewportSize.X / 2.0f - CardSize.X / 2.0f;
		float baseX = centerX + relativeIndex * CardSpacing;
		float baseY = 80.0f + Mathf.Abs(relativeIndex) * FanYOffset;

		Vector2 basePosition = new Vector2(baseX, baseY);
		float baseRotation = relativeIndex * FanRotationDegrees;

		card.Position = basePosition;
		card.RotationDegrees = baseRotation;
		card.ZIndex = index;

		_cards.Add(card);
		_cardPathsByCard[card] = path;
		_basePositions[card] = basePosition;
		_baseRotations[card] = baseRotation;
		_baseZIndexes[card] = index;

		AddChild(card);
	}

	public void CommitDraggedCardPlacement()
	{
		if (!_isDragging || _draggedCard == null)
			return;

		_removeDraggedCardAfterRelease = true;
	}

	private void UpdateHoveredCard()
	{
		TextureRect nextHoveredCard = GetCardUnderMouse();

		if (_hoveredCard == nextHoveredCard)
			return;

		if (_hoveredCard != null && _hoveredCard != _selectedCard)
		{
			AnimateCard(_hoveredCard, false);
		}

		_hoveredCard = nextHoveredCard;

		if (_hoveredCard != null)
		{
			AnimateCard(_hoveredCard, true);
		}
	}

	private TextureRect GetCardUnderMouse()
	{
		Vector2 globalMousePosition = GetGlobalMousePosition();

		TextureRect bestCard = null;
		float bestScore = float.MaxValue;

		foreach (TextureRect card in _cards)
		{
			if (card == null || !IsInstanceValid(card))
				continue;

			if (card == _draggedCard)
				continue;

			if (!_basePositions.ContainsKey(card))
				continue;

			Rect2 baseRect = GetCardBaseGlobalRect(card).Grow(HoverHitboxGrow);
			Rect2 currentRect = card.GetGlobalRect().Grow(HoverHitboxGrow);

			bool mouseInsideBaseRect = baseRect.HasPoint(globalMousePosition);
			bool mouseInsideCurrentRect = currentRect.HasPoint(globalMousePosition);

			if (!mouseInsideBaseRect && !mouseInsideCurrentRect)
				continue;

			Vector2 baseCenter = baseRect.Position + baseRect.Size / 2.0f;

			float horizontalDistance = Mathf.Abs(globalMousePosition.X - baseCenter.X);
			float verticalDistance = Mathf.Abs(globalMousePosition.Y - baseCenter.Y);

			float score = horizontalDistance + verticalDistance * 0.35f;

			if (score < bestScore)
			{
				bestScore = score;
				bestCard = card;
			}
		}

		return bestCard;
	}

	private Rect2 GetCardBaseGlobalRect(TextureRect card)
	{
		Vector2 basePosition = _basePositions[card];
		Vector2 globalBasePosition = GlobalPosition + basePosition;

		return new Rect2(globalBasePosition, CardSize);
	}

	private void UpdateDraggedCard()
	{
		if (_draggedCard == null)
			return;

		Vector2 globalMousePosition = GetGlobalMousePosition();
		_draggedCard.GlobalPosition = globalMousePosition - _dragOffset;

		if (!_draggedPlantType.HasValue)
			return;

		Vector2 viewportMousePosition = GetViewport().GetMousePosition();

		PlantCardDragged?.Invoke(_draggedPlantType.Value, viewportMousePosition);

		_dragDebugFrameCounter++;

	}

	private void SelectCard(TextureRect card, string path)
	{
		if (_selectedCard != null && _selectedCard != card)
		{
			AnimateCard(_selectedCard, false);
		}

		_selectedCard = card;
		_selectedPlantType = GetPlantTypeFromPath(path);

		GD.Print($"Selected plant card: {_selectedPlantType}");

		AnimateCard(card, true);

		if (_selectedPlantType.HasValue)
		{
			PlantCardSelected?.Invoke(_selectedPlantType.Value);
		}
	}

	private void StartDrag(TextureRect card, string path)
	{
		_draggedCard = card;
		_draggedPlantType = GetPlantTypeFromPath(path);
		_isDragging = true;
		_removeDraggedCardAfterRelease = false;
		_dragDebugFrameCounter = 0;

		_hoveredCard = null;

		if (_tweens.TryGetValue(card, out Tween oldTween))
		{
			oldTween.Kill();
			_tweens.Remove(card);
		}

		BringCardToFront(card, DragZIndex);

		card.RotationDegrees = 0.0f;
		card.Scale = new Vector2(DragScale, DragScale);

		Vector2 mousePosition = GetGlobalMousePosition();
		_dragOffset = mousePosition - card.GlobalPosition;

		if (_draggedPlantType.HasValue)
		{
			Vector2 viewportMousePosition = GetViewport().GetMousePosition();
			PlantCardDragged?.Invoke(_draggedPlantType.Value, viewportMousePosition);
		}

		GD.Print($"Dragging plant card: {_draggedPlantType}");
	}

	private void EndDrag()
	{
		if (_draggedCard == null)
			return;

		Vector2 mousePosition = GetViewport().GetMousePosition();

		if (_draggedPlantType.HasValue)
		{
			PlantCardDragReleased?.Invoke(_draggedPlantType.Value, mousePosition);
			GD.Print($"Released plant card: {_draggedPlantType} at {mousePosition}");
		}

		TextureRect releasedCard = _draggedCard;
		bool shouldRemoveReleasedCard = _removeDraggedCardAfterRelease;

		_draggedCard = null;
		_draggedPlantType = null;
		_isDragging = false;
		_removeDraggedCardAfterRelease = false;
		_dragDebugFrameCounter = 0;

		_selectedCard = null;
		_selectedPlantType = null;
		_hoveredCard = null;

		if (shouldRemoveReleasedCard)
		{
			RemoveCardFromHand(releasedCard);
		}
		else
		{
			AnimateCard(releasedCard, false);
		}
	}

	private void RemoveCardFromHand(TextureRect card)
	{
		if (card == null)
			return;

		if (_tweens.TryGetValue(card, out Tween oldTween))
		{
			oldTween.Kill();
			_tweens.Remove(card);
		}

		_cards.Remove(card);
		_cardPathsByCard.Remove(card);
		_basePositions.Remove(card);
		_baseRotations.Remove(card);
		_baseZIndexes.Remove(card);

		if (_hoveredCard == card)
			_hoveredCard = null;

		if (_selectedCard == card)
			_selectedCard = null;

		if (_draggedCard == card)
			_draggedCard = null;

		card.QueueFree();

		RecalculateHandLayout();
	}

	private void RecalculateHandLayout()
	{
		int totalCards = _cards.Count;

		if (totalCards == 0)
			return;

		Vector2 viewportSize = GetViewportRect().Size;

		float centerIndex = (totalCards - 1) / 2.0f;

		for (int i = 0; i < totalCards; i++)
		{
			TextureRect card = _cards[i];

			if (card == null || !IsInstanceValid(card))
				continue;

			float relativeIndex = i - centerIndex;

			float centerX = viewportSize.X / 2.0f - CardSize.X / 2.0f;
			float baseX = centerX + relativeIndex * CardSpacing;
			float baseY = 80.0f + Mathf.Abs(relativeIndex) * FanYOffset;

			Vector2 basePosition = new Vector2(baseX, baseY);
			float baseRotation = relativeIndex * FanRotationDegrees;

			_basePositions[card] = basePosition;
			_baseRotations[card] = baseRotation;
			_baseZIndexes[card] = i;

			AnimateCard(card, false);
		}

		RestoreDefaultChildOrder();
	}

	public void ClearSelection()
	{
		if (_selectedCard != null)
		{
			AnimateCard(_selectedCard, false);
		}

		_selectedCard = null;
		_selectedPlantType = null;
		_hoveredCard = null;
	}

	private PlantType GetPlantTypeFromPath(string path)
	{
		string lowerPath = path.ToLower();

		if (lowerPath.Contains("baum"))
			return PlantType.Oak;

		if (lowerPath.Contains("moos"))
			return PlantType.Moss;

		if (lowerPath.Contains("pilz"))
			return PlantType.Mushroom;

		if (lowerPath.Contains("flechte"))
			return PlantType.Moss;

		return PlantType.Moss;
	}

	private void AnimateCard(TextureRect card, bool isHovering)
	{
		if (card == null)
			return;

		if (!_basePositions.ContainsKey(card))
			return;

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

		if (isHovering)
		{
			BringCardToFront(card, HoverZIndex);
		}
		else
		{
			RestoreCardLayer(card);
		}

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

	private void BringCardToFront(TextureRect card, int zIndex)
	{
		if (card == null)
			return;

		card.ZAsRelative = false;
		card.ZIndex = zIndex;

		int lastIndex = GetChildCount() - 1;

		if (lastIndex >= 0)
		{
			MoveChild(card, lastIndex);
		}
	}

	private void RestoreCardLayer(TextureRect card)
	{
		if (card == null)
			return;

		if (!_baseZIndexes.ContainsKey(card))
			return;

		card.ZAsRelative = false;
		card.ZIndex = _baseZIndexes[card];

		RestoreDefaultChildOrder();
	}

	private void RestoreDefaultChildOrder()
	{
		List<TextureRect> cards = new();

		foreach (Node child in GetChildren())
		{
			if (child is TextureRect card && _baseZIndexes.ContainsKey(card))
			{
				cards.Add(card);
			}
		}

		cards.Sort((a, b) => _baseZIndexes[a].CompareTo(_baseZIndexes[b]));

		for (int i = 0; i < cards.Count; i++)
		{
			MoveChild(cards[i], i);
		}
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
