using Godot;
using System.Collections.Generic;

public partial class MenuCardMotion : Node
{
	[Export]
	public NodePath TargetPath { get; set; }

	[Export(PropertyHint.Range, "1.0,1.1,0.005")]
	public float HoverScale { get; set; } = 1.025f;

	[Export(PropertyHint.Range, "0.05,0.4,0.01")]
	public float MotionDuration { get; set; } = 0.12f;

	private readonly HashSet<BaseButton> _hoveredButtons = new();
	private readonly HashSet<BaseButton> _focusedButtons = new();
	private readonly Dictionary<BaseButton, Tween> _buttonTweens = new();

	public override void _Ready()
	{
		Control target = GetNodeOrNull<Control>(TargetPath);
		if (target == null)
		{
			GD.PushWarning($"MenuCardMotion: Ziel fehlt: {TargetPath}");
			return;
		}

		RegisterButtons(target);
	}

	private void RegisterButtons(Node node)
	{
		if (node is BaseButton button)
			RegisterButton(button);

		foreach (Node child in node.GetChildren())
			RegisterButtons(child);
	}

	private void RegisterButton(BaseButton button)
	{
		button.Resized += () => UpdatePivot(button);
		button.MouseEntered += () =>
		{
			_hoveredButtons.Add(button);
			Animate(button, true);
		};
		button.MouseExited += () =>
		{
			_hoveredButtons.Remove(button);
			Animate(button, IsActive(button));
		};
		button.FocusEntered += () =>
		{
			_focusedButtons.Add(button);
			Animate(button, true);
		};
		button.FocusExited += () =>
		{
			_focusedButtons.Remove(button);
			Animate(button, IsActive(button));
		};
		button.ButtonDown += () => Animate(button, false, 0.985f);
		button.ButtonUp += () => Animate(button, IsActive(button));

		UpdatePivot(button);
	}

	private bool IsActive(BaseButton button)
	{
		return _hoveredButtons.Contains(button) || _focusedButtons.Contains(button);
	}

	private void UpdatePivot(Control control)
	{
		control.PivotOffset = control.Size / 2.0f;
	}

	private void Animate(BaseButton button, bool active, float scaleOverride = -1.0f)
	{
		if (!IsInstanceValid(button))
			return;

		if (_buttonTweens.TryGetValue(button, out Tween previousTween) &&
			previousTween.IsValid())
		{
			previousTween.Kill();
		}

		UpdatePivot(button);
		float targetScale = scaleOverride > 0.0f
			? scaleOverride
			: active ? HoverScale : 1.0f;

		Tween tween = CreateTween();
		tween.TweenProperty(
				button,
				"scale",
				new Vector2(targetScale, targetScale),
				MotionDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		_buttonTweens[button] = tween;
	}
}
