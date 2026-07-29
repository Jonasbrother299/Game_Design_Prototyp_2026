using Godot;

public partial class RoundDisplayUI : Control
{
	[Export] public Label RoundValueLabel;

	[ExportGroup("Round Change Animation")]
	[Export(PropertyHint.Range, "0.05,1.0,0.01")]
	public float FadeInDuration = 0.16f;

	[Export(PropertyHint.Range, "0.05,1.0,0.01")]
	public float SettleDuration = 0.22f;

	[Export(PropertyHint.Range, "1.0,1.3,0.01")]
	public float PulseScale = 1.06f;

	private Tween _roundTween;

	public override void _Ready()
	{
		if (RoundValueLabel == null)
			RoundValueLabel = GetNodeOrNull<Label>("RoundValue");

		Resized += UpdatePivot;
		UpdatePivot();
	}

	public override void _ExitTree()
	{
		Resized -= UpdatePivot;

		if (_roundTween != null && _roundTween.IsValid())
			_roundTween.Kill();
	}

	public void ShowRound(int round)
	{
		if (RoundValueLabel != null)
			RoundValueLabel.Text = Mathf.Max(round, 1).ToString();

		PlayRoundChangeAnimation();
	}

	private void UpdatePivot()
	{
		PivotOffset = Size / 2.0f;
	}

	private void PlayRoundChangeAnimation()
	{
		if (_roundTween != null && _roundTween.IsValid())
			_roundTween.Kill();

		UpdatePivot();
		Scale = new Vector2(0.92f, 0.92f);
		Modulate = new Color(1.0f, 1.0f, 1.0f, 0.45f);

		_roundTween = CreateTween();
		_roundTween.SetParallel(true);
		_roundTween.TweenProperty(
				this,
				"scale",
				new Vector2(PulseScale, PulseScale),
				FadeInDuration)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		_roundTween.TweenProperty(
				this,
				"modulate",
				Colors.White,
				FadeInDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		_roundTween.SetParallel(false);
		_roundTween.TweenProperty(this, "scale", Vector2.One, SettleDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}
}
