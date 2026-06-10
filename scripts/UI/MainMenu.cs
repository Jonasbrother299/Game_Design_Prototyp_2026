using Godot;

public partial class MainMenu : Control
{
	[Export] public Button StartButton;
	[Export] public Button QuitButton;

	private const string LoadingScenePath = "res://scenes/UI/LoadingScreen.tscn";
	private const string GameScenePath = "res://scenes/Main.tscn";

	public override void _Ready()
	{
		GD.Print("MainMenu loaded.");

		if (StartButton == null)
			StartButton = GetNodeOrNull<Button>("CenterContainer/VBoxContainer/StartButton");

		if (QuitButton == null)
			QuitButton = GetNodeOrNull<Button>("CenterContainer/VBoxContainer/QuitButton");

		if (StartButton == null)
		{
			GD.PrintErr("StartButton not found. Check node path.");
			return;
		}

		StartButton.Pressed += OnStartPressed;

		if (QuitButton != null)
			QuitButton.Pressed += OnQuitPressed;
	}

	private void OnStartPressed()
	{
		GD.Print("Start button pressed.");

		if (ResourceLoader.Exists(LoadingScenePath))
		{
			GD.Print("Loading screen found. Switching to loading screen.");
			GetTree().ChangeSceneToFile(LoadingScenePath);
			return;
		}

		GD.PrintErr("LoadingScreen.tscn not found. Loading Main.tscn directly.");

		if (ResourceLoader.Exists(GameScenePath))
		{
			GetTree().ChangeSceneToFile(GameScenePath);
			return;
		}

		GD.PrintErr("Main.tscn also not found. Check scene paths.");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
