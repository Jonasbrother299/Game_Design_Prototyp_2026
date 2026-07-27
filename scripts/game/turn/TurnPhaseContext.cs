using Godot;

public sealed class TurnPhaseContext
{
	public GameState State { get; }
	public BoardManager BoardManager { get; }
	public GameConfig Config { get; }
	public RandomNumberGenerator Random { get; }

	public TurnPhaseContext(
		GameState state,
		BoardManager boardManager,
		GameConfig config,
		RandomNumberGenerator random)
	{
		State = state;
		BoardManager = boardManager;
		Config = config;
		Random = random;
	}
}
