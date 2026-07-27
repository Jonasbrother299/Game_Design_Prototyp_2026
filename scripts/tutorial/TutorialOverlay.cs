using Godot;
using System;

public partial class TutorialOverlay : Control
{
	public event Action NextRequested;
	public event Action BackRequested;

	private Panel _window;
	private Label _titleLabel;
	private Label _textLabel;
	private TextureRect _cardImage;
	private Label _cardInfo;
	private Button _nextButton;
	private Button _backButton;
	private HBoxContainer _progressDots;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		ZIndex = 1000;

		_window = GetNodeOrNull<Panel>("CenterContainer/TutorialWindow");
		_titleLabel = GetNodeOrNull<Label>("CenterContainer/TutorialWindow/TutorialLayoutVBox/TutorialTitle");
		_textLabel = GetNodeOrNull<Label>("CenterContainer/TutorialWindow/TutorialLayoutVBox/ScrollContainer/ScrollContent/TutorialText");
		_cardImage = GetNodeOrNull<TextureRect>("CenterContainer/TutorialWindow/TutorialLayoutVBox/ScrollContainer/ScrollContent/TutorialContentHBox/TutorialCardImage");
		_cardInfo = GetNodeOrNull<Label>("CenterContainer/TutorialWindow/TutorialLayoutVBox/ScrollContainer/ScrollContent/TutorialContentHBox/TutorialCardInfo");
		_progressDots = GetNodeOrNull<HBoxContainer>("CenterContainer/TutorialWindow/TutorialLayoutVBox/ProgressDots");
		_backButton = GetNodeOrNull<Button>("CenterContainer/TutorialWindow/TutorialLayoutVBox/Navigation/TutorialBackButton");
		_nextButton = GetNodeOrNull<Button>("CenterContainer/TutorialWindow/TutorialLayoutVBox/Navigation/TutorialNextButton");

		if (_nextButton != null)
			_nextButton.Pressed += OnNextPressed;

		if (_backButton != null)
			_backButton.Pressed += OnBackPressed;

		HideOverlay();
	}

	public override void _ExitTree()
	{
		if (_nextButton != null)
			_nextButton.Pressed -= OnNextPressed;

		if (_backButton != null)
			_backButton.Pressed -= OnBackPressed;
	}

	public void ShowOverlay()
	{
		Show();

		if (_window == null)
			return;

		_window.Scale = new Vector2(0.9f, 0.9f);

		Tween tween = CreateTween();
		tween.TweenProperty(_window, "scale", Vector2.One, 0.28f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
	}

	public void HideOverlay()
	{
		Hide();
	}

	public void SetTitle(string title)
	{
		if (_titleLabel != null)
			_titleLabel.Text = title ?? "";
	}

	public void SetText(string text)
	{
		if (_textLabel != null)
			_textLabel.Text = text ?? "";
	}

	public void SetCard(Texture2D texture, string info)
	{
		if (_cardImage != null)
		{
			_cardImage.Texture = texture;
			_cardImage.Visible = texture != null;
		}

		if (_cardInfo != null)
			_cardInfo.Text = info ?? "";
	}

	public void SetNavigation(bool canGoBack, bool isLastStep)
	{
		if (_backButton != null)
			_backButton.Disabled = !canGoBack;

		if (_nextButton != null)
			_nextButton.Text = isLastStep ? "Beenden" : "Weiter";
	}

	public void SetProgress(int currentStep, int totalSteps)
	{
		if (_progressDots == null)
			return;

		foreach (Node child in _progressDots.GetChildren())
		{
			_progressDots.RemoveChild(child);
			child.QueueFree();
		}

		for (int index = 0; index < totalSteps; index++)
		{
			Label dot = new Label();
			dot.Text = index == currentStep ? "●" : "○";
			dot.HorizontalAlignment = HorizontalAlignment.Center;
			dot.VerticalAlignment = VerticalAlignment.Center;
			dot.CustomMinimumSize = new Vector2(18, 18);
			dot.SizeFlagsHorizontal = SizeFlags.Fill;
			dot.SizeFlagsVertical = SizeFlags.Fill;
			dot.AddThemeFontSizeOverride("font_size", 18);
			_progressDots.AddChild(dot);
		}
	}

	private void OnNextPressed()
	{
		NextRequested?.Invoke();
	}

	private void OnBackPressed()
	{
		BackRequested?.Invoke();
	}
}
