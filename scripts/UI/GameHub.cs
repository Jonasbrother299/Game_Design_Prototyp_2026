using Godot;

public partial class GameHub : Control
{
	[Signal]
	public delegate void MenuRequestedEventHandler();

	[Export] public Button ExitButton;

	private TurnManager _turnManager;
	private BoardManager _boardManager;
	private EventDisplayUI _eventDisplay;
	private WaterDisplayUI _waterDisplay;
	private RoundDisplayUI _roundDisplay;
	private CanvasLayer _rainLensLayer;
	private RainLensCyaniluxOverlay _rainLensOverlay;

	public override void _Ready()
	{
		if (ExitButton == null)
			ExitButton = GetNodeOrNull<Button>("ExitButton");

		if (ExitButton != null)
			ExitButton.Pressed += OnExitButtonPressed;

		CallDeferred(nameof(SetupEventDisplay));
	}

	public override void _ExitTree()
	{
		if (ExitButton != null)
			ExitButton.Pressed -= OnExitButtonPressed;

		if (_turnManager != null)
		{
			_turnManager.TurnStarted -= OnTurnStarted;
			_turnManager.PlantPlaced -= OnPlantPlaced;
			_turnManager.EventActivated -= OnEventActivated;
			_turnManager.WaterPhaseResolved -= OnWaterPhaseResolved;
			_turnManager.EventPhaseResolved -= OnEventPhaseResolved;
		}
	}

	private void OnExitButtonPressed()
	{
		EmitSignal(SignalName.MenuRequested);
	}

	private void SetupEventDisplay()
	{
		Node currentScene = GetTree().CurrentScene;
		if (currentScene == null)
			return;

		_turnManager = currentScene.GetNodeOrNull<TurnManager>("TurnManager");
		if (_turnManager == null)
		{
			GD.PushError("GameHub: TurnManager fehlt.");
			return;
		}

		_boardManager = currentScene.GetNodeOrNull<BoardManager>("BoardManager");
		if (_boardManager == null)
		{
			GD.PushError("GameHub: BoardManager fehlt.");
			return;
		}

		_eventDisplay = GetNodeOrNull<EventDisplayUI>("EventDisplay");
		if (_eventDisplay == null)
		{
			PackedScene displayScene = GD.Load<PackedScene>(
				"res://scenes/UI/EventDisplay.tscn");
			_eventDisplay = displayScene?.Instantiate<EventDisplayUI>();

			if (_eventDisplay != null)
			{
				AddChild(_eventDisplay);
			}
		}

		_waterDisplay = GetNodeOrNull<WaterDisplayUI>("WaterLabel");
		if (_waterDisplay == null)
		{
			GD.PushError("GameHub: Wasseranzeige fehlt.");
		}
		else if (_turnManager.State != null)
		{
			_waterDisplay.ShowCurrentState(
				_turnManager.State.Water,
				_turnManager.Config.WinWaterLimit);
			UpdateWaterPreview();
		}

		_roundDisplay = GetNodeOrNull<RoundDisplayUI>("RoundDisplay");
		if (_roundDisplay == null)
		{
			GD.PushError("GameHub: Rundenanzeige fehlt.");
		}
		else if (_turnManager.State != null)
		{
			_roundDisplay.ShowRound(_turnManager.State.CurrentRound);
		}

		_rainLensOverlay =
			currentScene.GetNodeOrNull<RainLensCyaniluxOverlay>(
				"RainLensLayer/RainLensRoot/RainLensOverlay");
		_rainLensLayer = _rainLensOverlay?.GetParent()?.GetParent() as CanvasLayer;

		_turnManager.TurnStarted += OnTurnStarted;
		_turnManager.PlantPlaced += OnPlantPlaced;
		_turnManager.EventActivated += OnEventActivated;
		_turnManager.WaterPhaseResolved += OnWaterPhaseResolved;
		_turnManager.EventPhaseResolved += OnEventPhaseResolved;
	}

	private void OnEventActivated(GameEventType eventType)
	{
		_eventDisplay?.ShowActivated(EventDatabase.Get(eventType));
		UpdateWaterPreview();

		if (eventType == GameEventType.Rain ||
			eventType == GameEventType.HeavyRain)
		{
			if (_rainLensLayer != null)
				_rainLensLayer.Visible = true;

			float intensity = eventType == GameEventType.HeavyRain ? 0.90f : 0.62f;
			_rainLensOverlay?.StartRain(intensity);
		}
	}

	private void OnWaterPhaseResolved(WaterPhaseResult result)
	{
		_eventDisplay?.ShowWaterResult(result);
		_waterDisplay?.ShowWaterResult(
			result,
			_turnManager.Config.WinWaterLimit);
	}

	private void OnTurnStarted(int round)
	{
		_roundDisplay?.ShowRound(round);
		UpdateWaterPreview();
	}

	private void OnPlantPlaced(PlantType plantType, HexCoord coord)
	{
		UpdateWaterPreview();
	}

	private void UpdateWaterPreview()
	{
		if (_waterDisplay == null ||
			_turnManager?.State == null ||
			_boardManager == null)
		{
			return;
		}

		WaterBalanceCalculation balance = WaterBalanceCalculator.Calculate(
			_boardManager,
			_turnManager.State.ActiveEvents);

		_waterDisplay.ShowPreview(
			balance.NetChange,
			_turnManager.Config.WinWaterLimit,
			balance.DisplayedProduction,
			balance.DisplayedConsumption);
	}

	private void OnEventPhaseResolved(EventPhaseResult result)
	{
		_eventDisplay?.ShowPhaseResult(result);

		if (!ContainsRainEvent(result.ActiveEvents))
		{
			_rainLensOverlay?.StopRain();
		}
	}

	private static bool ContainsRainEvent(
		System.Collections.Generic.IReadOnlyList<GameEventType> activeEvents)
	{
		foreach (GameEventType eventType in activeEvents)
		{
			if (eventType == GameEventType.Rain ||
				eventType == GameEventType.HeavyRain)
			{
				return true;
			}
		}

		return false;
	}
}
