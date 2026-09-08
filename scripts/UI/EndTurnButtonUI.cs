using Godot;

public partial class EndTurnButtonUI : Button
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

	[ExportGroup("Calendar Flip")]
	[Export(PropertyHint.Range, "0.05,0.6,0.01")]
	public float FlipUpDuration = 0.32f;

	[Export(PropertyHint.Range, "0.05,0.6,0.01")]
	public float FlipDownDuration = 0.42f;

	public float CalendarFlipDuration =>
		Mathf.Max(FlipUpDuration, 0.0f) +
		Mathf.Max(FlipDownDuration, 0.0f);

	private Tween _stateTween;
	private Tween _flipTween;
	private TurnManager _turnManager;
	private Label _currentDayLeftLabel;
	private Label _currentDayRightLabel;
	private int _displayedRound = 1;
	private int _confirmedRound = -1;
	private bool _isFlipAnimating;
	private bool _isHovered;
	private bool _isPressed;
	private bool _lastDisabledState;

	public override void _Ready()
	{
		_currentDayLeftLabel = GetNodeOrNull<Label>("CurrentPage/DayLeft");
		_currentDayRightLabel = GetNodeOrNull<Label>("CurrentPage/DayRight");
		_turnManager = GetNodeOrNull<TurnManager>(
			"../../../../TurnManager") ??
			GetTree().CurrentScene?.GetNodeOrNull<TurnManager>("TurnManager");

		if (_turnManager != null)
		{
			_turnManager.EndTurnRequested += OnEndTurnRequested;
			_turnManager.TurnStarted += OnTurnStarted;
			_displayedRound = Mathf.Max(
				_turnManager.State?.CurrentRound ?? 1,
				1);
		}

		SetDayLabels(
			_currentDayLeftLabel,
			_currentDayRightLabel,
			_displayedRound);
		TooltipText = $"Tag {_displayedRound}: Runde beenden";

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
		if (_turnManager != null)
		{
			_turnManager.EndTurnRequested -= OnEndTurnRequested;
			_turnManager.TurnStarted -= OnTurnStarted;
		}

		MouseEntered -= OnMouseEntered;
		MouseExited -= OnMouseExited;
		ButtonDown -= OnButtonDown;
		ButtonUp -= OnButtonUp;
		Resized -= UpdatePivot;

		if (_stateTween != null && _stateTween.IsValid())
			_stateTween.Kill();

		if (_flipTween != null && _flipTween.IsValid())
			_flipTween.Kill();
	}

	private void OnEndTurnRequested(int round)
	{
		_confirmedRound = Mathf.Max(round + 1, 1);
		PlayCalendarFlip();
	}

	private void OnTurnStarted(int round)
	{
		int nextRound = Mathf.Max(round, 1);
		_confirmedRound = nextRound;
		if (nextRound == _displayedRound)
		{
			SetDayLabels(
				_currentDayLeftLabel,
				_currentDayRightLabel,
				nextRound);
			return;
		}

		if (!_isFlipAnimating)
			PlayCalendarFlip();
	}

	private void PlayCalendarFlip()
	{
		if (_currentDayLeftLabel == null ||
			_currentDayRightLabel == null)
		{
			_isFlipAnimating = false;
			if (_confirmedRound > 0)
			{
				_displayedRound = _confirmedRound;
				SetDayLabels(
					_currentDayLeftLabel,
					_currentDayRightLabel,
					_displayedRound);
			}
			return;
		}

		if (_flipTween != null && _flipTween.IsValid())
			_flipTween.Kill();

		int nextRound = _confirmedRound > 0
			? _confirmedRound
			: _displayedRound + 1;
		string nextDayText = Mathf.Max(nextRound, 1).ToString("D2");
		int splitIndex = Mathf.Max(nextDayText.Length / 2, 1);
		bool leftChanges =
			_currentDayLeftLabel.Text != nextDayText.Substring(0, splitIndex);
		bool rightChanges =
			_currentDayRightLabel.Text != nextDayText.Substring(splitIndex);

		if (!leftChanges && !rightChanges)
		{
			_displayedRound = nextRound;
			_confirmedRound = -1;
			TooltipText = $"Tag {_displayedRound}: Runde beenden";
			return;
		}

		_isFlipAnimating = true;
		_currentDayLeftLabel.Scale = Vector2.One;
		_currentDayRightLabel.Scale = Vector2.One;
		_currentDayLeftLabel.PivotOffset = _currentDayLeftLabel.Size / 2.0f;
		_currentDayRightLabel.PivotOffset = _currentDayRightLabel.Size / 2.0f;

		_flipTween = CreateTween();
		if (leftChanges)
		{
			_flipTween.TweenProperty(
					_currentDayLeftLabel,
					"scale",
					new Vector2(1.0f, 0.04f),
					FlipUpDuration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);
		}
		if (rightChanges)
		{
			Tween collapseTween = leftChanges ? _flipTween.Parallel() : _flipTween;
			collapseTween.TweenProperty(
					_currentDayRightLabel,
					"scale",
					new Vector2(1.0f, 0.04f),
					FlipUpDuration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);
		}
		_flipTween.TweenCallback(Callable.From(() =>
		{
			_displayedRound = nextRound;
			SetDayLabels(
				_currentDayLeftLabel,
				_currentDayRightLabel,
				_displayedRound);
		}));
		if (leftChanges)
		{
			_flipTween.TweenProperty(
					_currentDayLeftLabel,
					"scale",
					Vector2.One,
					FlipDownDuration)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}
		if (rightChanges)
		{
			Tween expandTween = leftChanges ? _flipTween.Parallel() : _flipTween;
			expandTween.TweenProperty(
					_currentDayRightLabel,
					"scale",
					Vector2.One,
					FlipDownDuration)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}
		_flipTween.TweenCallback(Callable.From(() =>
		{
			_isFlipAnimating = false;
			_confirmedRound = -1;
			TooltipText = $"Tag {_displayedRound}: Runde beenden";
		}));
	}

	private static void SetDayLabels(
		Label leftLabel,
		Label rightLabel,
		int round)
	{
		if (leftLabel == null && rightLabel == null)
			return;

		string dayText = Mathf.Max(round, 1).ToString("D2");
		int splitIndex = Mathf.Max(dayText.Length / 2, 1);

		if (leftLabel != null)
			leftLabel.Text = dayText.Substring(0, splitIndex);
		if (rightLabel != null)
			rightLabel.Text = dayText.Substring(splitIndex);
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
