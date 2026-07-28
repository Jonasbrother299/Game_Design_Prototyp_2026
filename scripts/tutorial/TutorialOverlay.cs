using Godot;
using System;

public partial class TutorialOverlay : Control
{
	public event Action NextRequested;
	public event Action BackRequested;

	private Control _centerContainer;
	private ColorRect _backdrop;
	private PanelContainer _window;
	private Label _titleLabel;
	private Label _textLabel;
	private Button _nextButton;
	private Button _backButton;

	private readonly Vector2 _modalWindowMinSize = new Vector2(780, 470);
	private readonly Vector2 _hintWindowMinSize = new Vector2(540, 450);

	public override void _Ready()
	{
		ZIndex = 1000;

		_centerContainer = GetNodeOrNull<Control>("CenterContainer");
		_backdrop = GetNodeOrNull<ColorRect>("Backdrop");
		_window = GetNodeOrNull<PanelContainer>(
			"CenterContainer/TutorialWindow");
		_titleLabel = GetNodeOrNull<Label>("CenterContainer/TutorialWindow/TutorialLayoutVBox/TutorialTitle");
		_textLabel = GetNodeOrNull<Label>(
			"CenterContainer/TutorialWindow/TutorialLayoutVBox/" +
			"BodyPanel/TutorialText");
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

	public void ShowModal()
	{
		Show();

		MoveWindowToModalContainer();
		SetBackdropColor(new Color(0.005f, 0.012f, 0.007f, 0.68f));

		MouseFilter = MouseFilterEnum.Stop;

		if (_centerContainer != null)
			_centerContainer.MouseFilter = MouseFilterEnum.Stop;

		if (_window != null)
		{
			_window.CustomMinimumSize = _modalWindowMinSize;
			_window.MouseFilter = MouseFilterEnum.Stop;
		}

		SetChildMouseFilters(_window, MouseFilterEnum.Ignore);
		SetButtonMouseFilters();

		AnimateWindowIn();
	}

	public void ShowHint()
	{
		Show();

		MoveWindowToOverlayRoot();
		SetBackdropColor(new Color(0.0f, 0.0f, 0.0f, 0.10f));

		// Wichtig:
		// Der große TutorialOverlay-Control darf im Hint-Modus keine Klicks blockieren.
		MouseFilter = MouseFilterEnum.Ignore;

		if (_centerContainer != null)
			_centerContainer.MouseFilter = MouseFilterEnum.Ignore;

		if (_window != null)
		{
			_window.CustomMinimumSize = _hintWindowMinSize;
			_window.Size = _hintWindowMinSize;

			// Wichtig:
			// Auch das Panel selbst ignoriert Mausinput.
			// Nur sichtbare Buttons dürfen Input annehmen.
			_window.MouseFilter = MouseFilterEnum.Ignore;
		}

		SetChildMouseFilters(_window, MouseFilterEnum.Ignore);
		SetButtonMouseFilters();
		PositionHintWindow();

		AnimateWindowIn();
	}

	public void ShowOverlay()
	{
		ShowModal();
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

	public void SetNavigation(bool canGoBack, bool isLastStep)
	{
		if (_backButton != null)
			_backButton.Disabled = !canGoBack;

		if (_nextButton != null)
			_nextButton.Text = isLastStep ? "Beenden" : "Weiter";

		SetButtonMouseFilters();
	}

	public void SetNextButtonVisible(bool visible)
	{
		if (_nextButton != null)
			_nextButton.Visible = visible;

		SetButtonMouseFilters();
	}

	public void SetBackButtonVisible(bool visible)
	{
		if (_backButton != null)
			_backButton.Visible = visible;

		SetButtonMouseFilters();
	}

	private void MoveWindowToModalContainer()
	{
		if (_window == null || _centerContainer == null)
			return;

		if (_window.GetParent() != _centerContainer)
			_window.Reparent(_centerContainer, false);
	}

	private void MoveWindowToOverlayRoot()
	{
		if (_window == null)
			return;

		if (_window.GetParent() != this)
			_window.Reparent(this, false);

		_window.SetAnchorsPreset(
			Control.LayoutPreset.TopLeft,
			keepOffsets: false);
	}

	private void PositionHintWindow()
	{
		if (_window == null)
			return;

		_window.Position = new Vector2(32, 96);
	}

	private void SetBackdropColor(Color color)
	{
		if (_backdrop != null)
			_backdrop.Color = color;
	}

	private void SetChildMouseFilters(Node node, MouseFilterEnum mouseFilter)
	{
		if (node == null)
			return;

		foreach (Node child in node.GetChildren())
		{
			if (child is Control control)
				control.MouseFilter = mouseFilter;

			SetChildMouseFilters(child, mouseFilter);
		}
	}

	private void SetButtonMouseFilters()
	{
		if (_nextButton != null)
		{
			_nextButton.MouseFilter = _nextButton.Visible
				? MouseFilterEnum.Stop
				: MouseFilterEnum.Ignore;
		}

		if (_backButton != null)
		{
			_backButton.MouseFilter = _backButton.Visible
				? MouseFilterEnum.Stop
				: MouseFilterEnum.Ignore;
		}
	}

	private void AnimateWindowIn()
	{
		if (_window == null)
			return;

		_window.PivotOffset = _window.Size * 0.5f;
		_window.Scale = new Vector2(0.96f, 0.96f);
		_window.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);

		Tween tween = CreateTween();
		tween.TweenProperty(_window, "scale", Vector2.One, 0.18f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(
			_window,
			"modulate",
			Colors.White,
			0.16f);
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
