using Godot;

public partial class FpsDisplay : Label
{
	[Export(PropertyHint.Range, "0.1,2.0,0.1")]
	public double UpdateIntervalSeconds = 0.5;

	private double _elapsedSeconds;

	public override void _Ready()
	{
		UpdateText();
	}

	public override void _Process(double delta)
	{
		_elapsedSeconds += delta;
		if (_elapsedSeconds < UpdateIntervalSeconds)
			return;

		_elapsedSeconds = 0.0;
		UpdateText();
	}

	private void UpdateText()
	{
		Text = $"FPS {Engine.GetFramesPerSecond():0}";
	}
}
