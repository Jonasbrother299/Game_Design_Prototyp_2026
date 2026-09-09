using Godot;
using System;

public partial class TutorialOverlay : Control
{
	public event Action NextRequested;
	public event Action BackRequested;

	public enum HintPosition
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	private Control _centerContainer;
	private ColorRect _backdrop;
	private PanelContainer _window;
	private Label _titleLabel;
	private Label _textLabel;
	private Button _nextButton;
	private Button _backButton;

	private Control _cardExplanationRoot;
	private Control _cardArea;
	private TextureRect _cardImage;
	private Panel _cardHighlight;
	private Label _cardExplanationTitle;
	private Label _cardExplanationText;
	private Button _cardNextButton;
	private Button _cardBackButton;

	private HintPosition _hintPosition = HintPosition.TopLeft;

	private Rect2 _currentCardHighlightNormalized = new Rect2(
		new Vector2(0.0f, 0.0f),
		new Vector2(1.0f, 1.0f)
	);

	private readonly Vector2 _modalWindowMinSize =
		new Vector2(780, 470);

	private readonly Vector2 _hintWindowMinSize =
		new Vector2(500, 340);

	private const float HintSideMargin = 36.0f;
	private const float HintTopMargin = 185.0f;
	private const float HintBottomMargin = 36.0f;
	private const float HintTargetGap = 28.0f;

	public override void _Ready()
	{
		ZIndex = 1000;

		_centerContainer = GetNodeOrNull<Control>(
			"CenterContainer"
		);

		_backdrop = GetNodeOrNull<ColorRect>(
			"Backdrop"
		);

		_window = GetNodeOrNull<PanelContainer>(
			"CenterContainer/TutorialWindow"
		);

		_titleLabel = GetNodeOrNull<Label>(
			"CenterContainer/TutorialWindow/" +
			"TutorialLayoutVBox/TutorialTitle"
		);

		_textLabel = GetNodeOrNull<Label>(
			"CenterContainer/TutorialWindow/" +
			"TutorialLayoutVBox/BodyPanel/TutorialText"
		);

		_backButton = GetNodeOrNull<Button>(
			"CenterContainer/TutorialWindow/" +
			"TutorialLayoutVBox/Navigation/TutorialBackButton"
		);

		_nextButton = GetNodeOrNull<Button>(
			"CenterContainer/TutorialWindow/" +
			"TutorialLayoutVBox/Navigation/TutorialNextButton"
		);

		_cardExplanationRoot =
			GetNodeOrNull<Control>("CardExplanationRoot");

		_cardArea = GetNodeOrNull<Control>(
			"CardExplanationRoot/CardExplanationCenter/" +
			"CardExplanationPanel/CardExplanationLayout/CardArea"
		);

		_cardImage = GetNodeOrNull<TextureRect>(
			"CardExplanationRoot/CardExplanationCenter/" +
			"CardExplanationPanel/CardExplanationLayout/" +
			"CardArea/CardImage"
		);

		_cardHighlight = GetNodeOrNull<Panel>(
			"CardExplanationRoot/CardExplanationCenter/" +
			"CardExplanationPanel/CardExplanationLayout/" +
			"CardArea/CardHighlight"
		);

		_cardExplanationTitle = GetNodeOrNull<Label>(
			"CardExplanationRoot/CardExplanationCenter/" +
			"CardExplanationPanel/CardExplanationLayout/" +
			"ExplanationArea/ExplanationTitle"
		);

		_cardExplanationText = GetNodeOrNull<Label>(
			"CardExplanationRoot/CardExplanationCenter/" +
			"CardExplanationPanel/CardExplanationLayout/" +
			"ExplanationArea/ExplanationBody/ExplanationText"
		);

		_cardBackButton = GetNodeOrNull<Button>(
			"CardExplanationRoot/CardExplanationCenter/" +
			"CardExplanationPanel/CardExplanationLayout/" +
			"ExplanationArea/CardNavigation/CardBackButton"
		);

		_cardNextButton = GetNodeOrNull<Button>(
			"CardExplanationRoot/CardExplanationCenter/" +
			"CardExplanationPanel/CardExplanationLayout/" +
			"ExplanationArea/CardNavigation/CardNextButton"
		);

		if (_nextButton != null)
			_nextButton.Pressed += OnNextPressed;

		if (_backButton != null)
			_backButton.Pressed += OnBackPressed;

		if (_cardNextButton != null)
			_cardNextButton.Pressed += OnNextPressed;

		if (_cardBackButton != null)
			_cardBackButton.Pressed += OnBackPressed;

		if (_cardArea != null)
			_cardArea.Resized += UpdateCardHighlight;

		HideOverlay();
	}

	public override void _ExitTree()
	{
		if (_nextButton != null)
			_nextButton.Pressed -= OnNextPressed;

		if (_backButton != null)
			_backButton.Pressed -= OnBackPressed;

		if (_cardNextButton != null)
			_cardNextButton.Pressed -= OnNextPressed;

		if (_cardBackButton != null)
			_cardBackButton.Pressed -= OnBackPressed;

		if (_cardArea != null)
			_cardArea.Resized -= UpdateCardHighlight;
	}

	public void ShowModal()
	{
		Show();
		HideCardExplanationView();
		ShowNormalWindow();

		MoveWindowToModalContainer();

		SetBackdropColor(
			new Color(0.005f, 0.012f, 0.007f, 0.68f)
		);

		MouseFilter = MouseFilterEnum.Stop;

		if (_centerContainer != null)
			_centerContainer.MouseFilter = MouseFilterEnum.Stop;

		if (_window != null)
		{
			_window.CustomMinimumSize = _modalWindowMinSize;
			_window.MouseFilter = MouseFilterEnum.Stop;
		}

		SetChildMouseFilters(
			_window,
			MouseFilterEnum.Ignore
		);

		SetButtonMouseFilters();

		AnimateWindowIn();
	}

	public void ShowHint()
	{
		Show();
		HideCardExplanationView();
		ShowNormalWindow();

		MoveWindowToOverlayRoot();

		SetBackdropColor(
			new Color(0.0f, 0.0f, 0.0f, 0.10f)
		);

		MouseFilter = MouseFilterEnum.Ignore;

		if (_centerContainer != null)
			_centerContainer.MouseFilter =
				MouseFilterEnum.Ignore;

		if (_window != null)
		{
			_window.CustomMinimumSize = _hintWindowMinSize;
			_window.Size = _hintWindowMinSize;
			_window.MouseFilter = MouseFilterEnum.Ignore;
		}

		SetChildMouseFilters(
			_window,
			MouseFilterEnum.Ignore
		);

		SetButtonMouseFilters();

		PositionHintWindow();

		AnimateWindowIn();
	}

	public void ShowCardExplanation(Texture2D cardTexture)
	{
		Show();
		HideNormalWindow();

		if (_cardExplanationRoot != null)
		{
			_cardExplanationRoot.Visible = true;
			_cardExplanationRoot.MouseFilter =
				MouseFilterEnum.Stop;
		}

		if (_cardImage != null)
			_cardImage.Texture = cardTexture;

		SetBackdropColor(
			new Color(0.005f, 0.012f, 0.007f, 0.82f)
		);

		MouseFilter = MouseFilterEnum.Stop;

		if (_centerContainer != null)
			_centerContainer.MouseFilter =
				MouseFilterEnum.Ignore;

		SetChildMouseFilters(
			_cardExplanationRoot,
			MouseFilterEnum.Ignore
		);

		SetCardButtonMouseFilters();

		Callable.From(UpdateCardHighlight).CallDeferred();

		AnimateCardExplanationIn();
	}

	public void HideCardExplanation()
	{
		HideCardExplanationView();
	}

	public void SetCardExplanation(
		string title,
		string text,
		Rect2 normalizedHighlight)
	{
		if (_cardExplanationTitle != null)
			_cardExplanationTitle.Text = title ?? "";

		if (_cardExplanationText != null)
			_cardExplanationText.Text = text ?? "";

		if (_cardHighlight != null)
			_cardHighlight.Visible = true;

		_currentCardHighlightNormalized =
			normalizedHighlight;

		UpdateCardHighlight();
		AnimateCardHighlight();
	}

	public void SetCardHighlightVisible(bool visible)
	{
		if (_cardHighlight != null)
			_cardHighlight.Visible = visible;
	}

	public void SetCardNavigation(
		bool canGoBack,
		bool isLastPage)
	{
		if (_cardBackButton != null)
			_cardBackButton.Disabled = !canGoBack;

		if (_cardNextButton != null)
		{
			_cardNextButton.Text =
				isLastPage
					? "Verstanden"
					: "Weiter";
		}

		SetCardButtonMouseFilters();
	}

	public void ShowOverlay()
	{
		ShowModal();
	}

	public void HideOverlay()
	{
		HideCardExplanationView();
		ShowNormalWindow();
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

	public void SetNavigation(
		bool canGoBack,
		bool isLastStep)
	{
		if (_backButton != null)
			_backButton.Disabled = !canGoBack;

		if (_nextButton != null)
		{
			_nextButton.Text =
				isLastStep
					? "Beenden"
					: "Weiter";
		}

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

	public void SetHintPosition(
		HintPosition position)
	{
		_hintPosition = position;

		if (_window == null)
			return;

		if (_window.GetParent() != this)
			return;

		PositionHintWindow();
	}

	public void PositionHintNear(
		Rect2 targetRect,
		bool alignTop = false)
	{
		if (_window == null)
			return;

		if (_window.GetParent() != this)
			return;

		Vector2 viewportSize =
			GetViewportRect().Size;

		Vector2 windowSize =
			GetCurrentHintWindowSize();

		float spaceLeft =
			targetRect.Position.X - HintSideMargin;

		float spaceRight =
			viewportSize.X -
			targetRect.End.X -
			HintSideMargin;

		float x;

		if (spaceRight >=
			windowSize.X + HintTargetGap)
		{
			x =
				targetRect.End.X +
				HintTargetGap;
		}
		else if (spaceLeft >=
			windowSize.X + HintTargetGap)
		{
			x =
				targetRect.Position.X -
				windowSize.X -
				HintTargetGap;
		}
		else
		{
			_hintPosition =
				HintPosition.TopLeft;

			PositionHintWindow();
			return;
		}

		float y;

		if (alignTop)
		{
			y = targetRect.Position.Y -4.0f;
		}
		else
		{
			y =
				targetRect.Position.Y +
				(targetRect.Size.Y - windowSize.Y) *
				0.5f;
		}

		x = Mathf.Clamp(
			x,
			HintSideMargin,
			Mathf.Max(
				HintSideMargin,
				viewportSize.X -
				windowSize.X -
				HintSideMargin
			)
		);

		float minimumY =
			alignTop
				? 0.0f
				: HintTopMargin;

		y = Mathf.Clamp(
			y,
			minimumY,
			Mathf.Max(
				minimumY,
				viewportSize.Y -
				windowSize.Y -
				HintBottomMargin
			)
		);

		_window.Position =
			new Vector2(x, y);
	}

	private void MoveWindowToModalContainer()
	{
		if (_window == null ||
			_centerContainer == null)
		{
			return;
		}

		if (_window.GetParent() != _centerContainer)
		{
			_window.Reparent(
				_centerContainer,
				false
			);
		}
	}

	private void MoveWindowToOverlayRoot()
	{
		if (_window == null)
			return;

		if (_window.GetParent() != this)
			_window.Reparent(this, false);

		_window.SetAnchorsPreset(
			Control.LayoutPreset.TopLeft,
			keepOffsets: false
		);
	}

	private void PositionHintWindow()
	{
		if (_window == null)
			return;

		Vector2 viewportSize =
			GetViewportRect().Size;

		Vector2 windowSize =
			GetCurrentHintWindowSize();

		float left =
			HintSideMargin;

		float right =
			Mathf.Max(
				HintSideMargin,
				viewportSize.X -
				windowSize.X -
				HintSideMargin
			);

		float top =
			HintTopMargin;

		float bottom =
			Mathf.Max(
				HintTopMargin,
				viewportSize.Y -
				windowSize.Y -
				HintBottomMargin
			);

		switch (_hintPosition)
		{
			case HintPosition.TopLeft:
				_window.Position =
					new Vector2(left, top);
				break;

			case HintPosition.TopRight:
				_window.Position =
					new Vector2(right, top);
				break;

			case HintPosition.BottomLeft:
				_window.Position =
					new Vector2(left, bottom);
				break;

			case HintPosition.BottomRight:
				_window.Position =
					new Vector2(right, bottom);
				break;
		}
	}

	private Vector2 GetCurrentHintWindowSize()
	{
		if (_window == null)
			return _hintWindowMinSize;

		Vector2 size = _window.Size;

		if (size.X <= 0.0f)
			size.X = _hintWindowMinSize.X;

		if (size.Y <= 0.0f)
			size.Y = _hintWindowMinSize.Y;

		return size;
	}

	private void ShowNormalWindow()
	{
		if (_window != null)
			_window.Visible = true;
	}

	private void HideNormalWindow()
	{
		if (_window != null)
			_window.Visible = false;
	}

	private void HideCardExplanationView()
	{
		if (_cardExplanationRoot != null)
			_cardExplanationRoot.Visible = false;
	}

	private void SetBackdropColor(Color color)
	{
		if (_backdrop != null)
			_backdrop.Color = color;
	}

	private void SetChildMouseFilters(
		Node node,
		MouseFilterEnum mouseFilter)
	{
		if (node == null)
			return;

		foreach (Node child in node.GetChildren())
		{
			if (child is Control control)
				control.MouseFilter = mouseFilter;

			SetChildMouseFilters(
				child,
				mouseFilter
			);
		}
	}

	private void SetButtonMouseFilters()
	{
		if (_nextButton != null)
		{
			_nextButton.MouseFilter =
				_nextButton.Visible
					? MouseFilterEnum.Stop
					: MouseFilterEnum.Ignore;
		}

		if (_backButton != null)
		{
			_backButton.MouseFilter =
				_backButton.Visible
					? MouseFilterEnum.Stop
					: MouseFilterEnum.Ignore;
		}
	}

	private void SetCardButtonMouseFilters()
	{
		if (_cardNextButton != null)
		{
			_cardNextButton.MouseFilter =
				MouseFilterEnum.Stop;
		}

		if (_cardBackButton != null)
		{
			_cardBackButton.MouseFilter =
				MouseFilterEnum.Stop;
		}
	}

	private void UpdateCardHighlight()
	{
		if (_cardArea == null ||
			_cardHighlight == null)
		{
			return;
		}

		Vector2 areaSize =
			_cardArea.Size;

		if (areaSize.X <= 0.0f ||
			areaSize.Y <= 0.0f)
		{
			return;
		}

		Vector2 position = new Vector2(
			_currentCardHighlightNormalized.Position.X *
			areaSize.X,
			_currentCardHighlightNormalized.Position.Y *
			areaSize.Y
		);

		Vector2 size = new Vector2(
			_currentCardHighlightNormalized.Size.X *
			areaSize.X,
			_currentCardHighlightNormalized.Size.Y *
			areaSize.Y
		);

		_cardHighlight.Position = position;
		_cardHighlight.Size = size;
		_cardHighlight.PivotOffset =
			size * 0.5f;
	}

	private void AnimateWindowIn()
	{
		if (_window == null)
			return;

		_window.PivotOffset =
			_window.Size * 0.5f;

		_window.Scale =
			new Vector2(0.96f, 0.96f);

		_window.Modulate =
			new Color(
				1.0f,
				1.0f,
				1.0f,
				0.0f
			);

		Tween tween =
			CreateTween();

		tween.TweenProperty(
				_window,
				"scale",
				Vector2.One,
				0.18f
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.Out
			);

		tween.Parallel().TweenProperty(
			_window,
			"modulate",
			Colors.White,
			0.16f
		);
	}

	private void AnimateCardExplanationIn()
	{
		if (_cardExplanationRoot == null)
			return;

		_cardExplanationRoot.Modulate =
			new Color(
				1.0f,
				1.0f,
				1.0f,
				0.0f
			);

		Tween tween =
			CreateTween();

		tween.TweenProperty(
				_cardExplanationRoot,
				"modulate",
				Colors.White,
				0.18f
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.Out
			);
	}

	private void AnimateCardHighlight()
	{
		if (_cardHighlight == null)
			return;

		_cardHighlight.Scale =
			new Vector2(1.06f, 1.06f);

		_cardHighlight.Modulate =
			new Color(
				1.0f,
				1.0f,
				1.0f,
				0.45f
			);

		Tween tween =
			CreateTween();

		tween.TweenProperty(
				_cardHighlight,
				"scale",
				Vector2.One,
				0.16f
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.Out
			);

		tween.Parallel().TweenProperty(
			_cardHighlight,
			"modulate",
			Colors.White,
			0.16f
		);
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