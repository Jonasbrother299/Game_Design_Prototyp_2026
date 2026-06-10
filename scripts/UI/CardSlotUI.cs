using Godot;

public partial class CardSlotUI : Control
{
	[Signal]
	public delegate void CardClickedEventHandler(CardData cardData);

	[Export] public TextureRect CardVisual;
	[Export] public CardData CardData;

	[Export] public float HoverLift = 45.0f;
	[Export] public float HoverScale = 1.08f;
	[Export] public float HoverDuration = 0.12f;

	private Vector2 _basePosition;
	private Tween _hoverTween;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;

		if (CardVisual == null)
			CardVisual = GetNodeOrNull<TextureRect>("CardVisual");

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
		GuiInput += OnGuiInput;

		CallDeferred(nameof(Setup));
	}

	private void Setup()
	{
		if (CardVisual == null)
		{
			GD.PrintErr("CardSlotUI: CardVisual not found.");
			return;
		}

		_basePosition = CardVisual.Position;
		CardVisual.PivotOffset = CardVisual.Size / 2.0f;

		Refresh();
	}

	public void SetCard(CardData cardData)
	{
		CardData = cardData;
		Refresh();
	}

	private void Refresh()
	{
		if (CardVisual == null || CardData == null)
			return;

		CardVisual.Texture = CardData.CardImage;
	}

	private void OnMouseEntered()
	{
		AnimateHover(true);
	}

	private void OnMouseExited()
	{
		AnimateHover(false);
	}

	private void AnimateHover(bool isHovering)
	{
		if (CardVisual == null)
			return;

		_hoverTween?.Kill();

		Vector2 targetPosition = isHovering
			? _basePosition + new Vector2(0.0f, -HoverLift)
			: _basePosition;

		Vector2 targetScale = isHovering
			? new Vector2(HoverScale, HoverScale)
			: Vector2.One;

		_hoverTween = CreateTween();
		_hoverTween.SetParallel(true);

		_hoverTween.TweenProperty(CardVisual, "position", targetPosition, HoverDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		_hoverTween.TweenProperty(CardVisual, "scale", targetScale, HoverDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}

	private void OnGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseButton)
			return;

		if (!mouseButton.Pressed)
			return;

		if (mouseButton.ButtonIndex != MouseButton.Left)
			return;

		if (CardData == null)
			return;

		GD.Print($"Selected card: {CardData.CardName}");
		EmitSignal(SignalName.CardClicked, CardData);
	}
}
