using Godot;

public partial class EndTurnButtonUI : TextureButton
{
	[ExportGroup("Button Feedback")]
	[Export] public Color NormalColor = Colors.White;
	[Export] public Color HoverColor = new Color(1.0f, 0.96f, 0.78f);
	[Export] public Color PressedColor = new Color(0.78f, 0.72f, 0.58f);
	[Export] public Color DisabledColor = new Color(0.55f, 0.57f, 0.55f, 0.62f);

	[Export(PropertyHint.Range, "1.0,1.2,0.01")]
	public float HoverScale = 1.05f;

	[Export(PropertyHint.Range, "0.8,1.0,0.01")]
	public float PressedScale = 0.96f;

	[Export(PropertyHint.Range, "0.05,0.5,0.01")]
	public float TransitionDuration = 0.12f;

	private Tween _stateTween;
	private bool _isHovered;
	private bool _isPressed;
	private bool _lastDisabledState;

	public override void _Ready()
	{
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
		ButtonDown += OnButtonDown;
		ButtonUp += OnButtonUp;
		Resized += UpdatePivot;

		MouseDefaultCursorShape = CursorShape.PointingHand;
		_lastDisabledState = Disabled;
		UpdatePivot();
		ApplyCurrentState(false);
	}

	public override void _Process(double delta)
	{
		if (_lastDisabledState == Disabled)
			return;

		_lastDisabledState = Disabled;
		ApplyCurrentState(true);
	}

	public override void _ExitTree()
	{
		MouseEntered -= OnMouseEntered;
		MouseExited -= OnMouseExited;
		ButtonDown -= OnButtonDown;
		ButtonUp -= OnButtonUp;
		Resized -= UpdatePivot;

		if (_stateTween != null && _stateTween.IsValid())
			_stateTween.Kill();
	}

	private void OnMouseEntered()
	{
		_isHovered = true;
		ApplyCurrentState(true);
	}

	private void OnMouseExited()
	{
		_isHovered = false;
		_isPressed = false;
		ApplyCurrentState(true);
	}

	private void OnButtonDown()
	{
		_isPressed = true;
		ApplyCurrentState(true);
	}

	private void OnButtonUp()
	{
		_isPressed = false;
		ApplyCurrentState(true);
	}

	private void UpdatePivot()
	{
		PivotOffset = Size / 2.0f;
	}

	private void ApplyCurrentState(bool animated)
	{
		Color targetColor;
		Vector2 targetScale;

		if (Disabled)
		{
			targetColor = DisabledColor;
			targetScale = Vector2.One;
		}
		else if (_isPressed)
		{
			targetColor = PressedColor;
			targetScale = new Vector2(PressedScale, PressedScale);
		}
		else if (_isHovered)
		{
			targetColor = HoverColor;
			targetScale = new Vector2(HoverScale, HoverScale);
		}
		else
		{
			targetColor = NormalColor;
			targetScale = Vector2.One;
		}

		if (_stateTween != null && _stateTween.IsValid())
			_stateTween.Kill();

		if (!animated)
		{
			SelfModulate = targetColor;
			Scale = targetScale;
			return;
		}

		_stateTween = CreateTween();
		_stateTween.SetParallel(true);
		_stateTween.TweenProperty(
				this,
				"self_modulate",
				targetColor,
				TransitionDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_stateTween.TweenProperty(
				this,
				"scale",
				targetScale,
				TransitionDuration)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
	}
}
