using Godot;
using System;

public partial class GameHub : Control
{
	[Export] public Control TutorialPanel;
	[Export] public Label TutorialTitleLabel;
	[Export] public Label TutorialTextLabel;
	[Export] public Button TutorialNextButton;
	[Export] public Button ExitButton;

	public override void _Ready()
	{
		// Ensure tutorial panel is discoverable and hidden by default.
		if (TutorialPanel == null)
			TutorialPanel = GetNodeOrNull<Control>("TutorialPanel");

		if (TutorialPanel != null)
		{
			TutorialPanel.MouseFilter = MouseFilterEnum.Stop;
			TutorialPanel.ZIndex = 1000;
			TutorialPanel.Hide();
		}

		if (ExitButton == null)
			ExitButton = GetNodeOrNull<Button>("ExitButton");

		if (ExitButton != null)
			ExitButton.Pressed += OnExitButtonPressed;
	}

	public void ShowTutorialPanel()
	{
		if (TutorialPanel == null)
			return;

		TutorialPanel.Show();

		// panel entrance animation
		var window = TutorialPanel.GetNodeOrNull<Panel>("CenterContainer/TutorialWindow");

		if (window != null)
		{
			window.Scale = new Vector2(0.9f, 0.9f);
			var tween = CreateTween();
			tween.TweenProperty(window, "scale", new Vector2(1f,1f), 0.28f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}
	}

	public void HideTutorialPanel()
	{
		TutorialPanel?.Hide();
	}

	// Helper to set texts and image; TutorialManager can manipulate nodes directly as well.
	public void SetTitle(string title)
	{
		if (TutorialTitleLabel != null)
			TutorialTitleLabel.Text = title;
	}

	public void SetText(string text)
	{
		if (TutorialTextLabel != null)
			TutorialTextLabel.Text = text;
	}

	private void OnExitButtonPressed()
	{
		GetTree().Quit();
	}
}
