using Godot;
using System;
using System.Collections.Generic;

public partial class CardHandUI : Control
{
	public event Action<PlantType> PlantCardSelected;
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

	private const float DragScale = 0.45f;
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
	private readonly Dictionary<TextureRect, string> _cardPathsByCard = new();

	private TextureRect _selectedCard;
	private PlantType? _selectedPlantType = null;

	private TextureRect _draggedCard;
	private PlantType? _draggedPlantType = null;
	private Vector2 _dragOffset = Vector2.Zero;
	private bool _isDragging = false;

	public override void _Ready()
	{
		_rng.Randomize();

		MouseFilter = MouseFilterEnum.Ignore;
		ClipContents = false;

		SetupPosition();
		CallDeferred(nameof(LoadCards));
	}

	public override void _Process(double delta)
	{
		if (!_isDragging || _draggedCard == null)
			return;

		Vector2 mousePosition = GetGlobalMousePosition();
		_draggedCard.GlobalPosition = mousePosition - _dragOffset;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!_isDragging || _draggedCard == null)
			return;

		if (inputEvent is not InputEventMouseButton mouseButton)
			return;

		if (mouseButton.ButtonIndex != MouseButton.Left)
			return;

		if (mouseButton.Pressed)
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

		_tweens.Clear();
		_basePositions.Clear();
		_baseRotations.Clear();
		_baseZIndexes.Clear();
		_cardPathsByCard.Clear();

		_selectedCard = null;
		_selectedPlantType = null;

		_draggedCard = null;
		_draggedPlantType = null;
		_isDragging = false;

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
		card.MouseFilter = MouseFilterEnum.Stop;

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

		_basePositions[card] = basePosition;
		_baseRotations[card] = baseRotation;
		_baseZIndexes[card] = index;
		_cardPathsByCard[card] = path;

		card.MouseEntered += () =>
		{
			if (_isDragging)
				return;

			AnimateCard(card, true);
		};

		card.MouseExited += () =>
		{
			if (_isDragging)
				return;

			if (_selectedCard != card)
				AnimateCard(card, false);
		};

		card.GuiInput += inputEvent =>
		{
			if (inputEvent is not InputEventMouseButton mouseButton)
				return;

			if (mouseButton.ButtonIndex != MouseButton.Left)
				return;

			if (!mouseButton.Pressed)
				return;

			SelectCard(card, path);
			StartDrag(card, path);

			GetViewport().SetInputAsHandled();
		};

		AddChild(card);
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

		if (_tweens.TryGetValue(card, out Tween oldTween))
		{
			oldTween.Kill();
			_tweens.Remove(card);
		}

		card.ZIndex = 200;
		card.RotationDegrees = 0.0f;
		card.Scale = new Vector2(DragScale, DragScale);

		Vector2 mousePosition = GetGlobalMousePosition();
		_dragOffset = mousePosition - card.GlobalPosition;

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

		_draggedCard = null;
		_draggedPlantType = null;
		_isDragging = false;

		_selectedCard = null;
		_selectedPlantType = null;

		AnimateCard(releasedCard, false);
	}

	public void ClearSelection()
	{
		if (_selectedCard != null)
		{
			AnimateCard(_selectedCard, false);
		}

		_selectedCard = null;
		_selectedPlantType = null;
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
