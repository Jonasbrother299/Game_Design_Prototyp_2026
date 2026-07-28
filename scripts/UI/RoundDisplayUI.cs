using Godot;

public partial class RoundDisplayUI : Control
{
	[Export] public Label RoundValueLabel;

	public override void _Ready()
	{
		if (RoundValueLabel == null)
			RoundValueLabel = GetNodeOrNull<Label>("RoundValue");
	}

	public void ShowRound(int round)
	{
		if (RoundValueLabel != null)
			RoundValueLabel.Text = Mathf.Max(round, 1).ToString();
	}
}
