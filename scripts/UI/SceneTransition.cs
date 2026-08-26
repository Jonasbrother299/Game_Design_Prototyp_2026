using Godot;
using System.Threading.Tasks;

public partial class SceneTransition : CanvasLayer
{
	private const string DefaultCursorPath =
		"res://assets/ui/cursors/forest_pointer.svg";
	private const string InteractionCursorPath =
		"res://assets/ui/cursors/forest_pointer_interact.svg";

	public static SceneTransition Instance { get; private set; }
	public event System.Action<string, string> SceneChangeFailed;

	[Export] public float FadeDuration = 0.25f;
	[Export] public int FramesBeforeReveal = 2;

	private ColorRect _cover;
	private bool _isTransitioning;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_cover = GetNode<ColorRect>("%Cover");
		ApplyCursorTheme();
	}

	private static void ApplyCursorTheme()
	{
		Texture2D defaultCursor = GD.Load<Texture2D>(DefaultCursorPath);
		Texture2D interactionCursor = GD.Load<Texture2D>(InteractionCursorPath);

		if (defaultCursor == null || interactionCursor == null)
		{
			GD.PushWarning("Mauszeiger-Assets konnten nicht geladen werden.");
			return;
		}

		Input.SetCustomMouseCursor(
			defaultCursor,
			Input.CursorShape.Arrow,
			new Vector2(3.0f, 2.0f));
		Input.SetCustomMouseCursor(
			interactionCursor,
			Input.CursorShape.PointingHand,
			new Vector2(4.0f, 3.0f));
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public async void ChangeScene(string scenePath)
	{
		if (_isTransitioning)
			return;

		if (!ResourceLoader.Exists(scenePath))
		{
			GD.PushError($"SceneTransition: Zielszene fehlt: {scenePath}");
			SceneChangeFailed?.Invoke(scenePath, "Die Zielszene fehlt.");
			return;
		}

		_isTransitioning = true;
		_cover.Show();
		_cover.MouseFilter = Control.MouseFilterEnum.Stop;

		await FadeCoverTo(1.0f);

		Error error = GetTree().ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
		{
			GD.PushError(
				$"SceneTransition: Szenenwechsel fehlgeschlagen: {error}");
			SceneChangeFailed?.Invoke(scenePath, error.ToString());
			await FadeCoverTo(0.0f);
			FinishTransition();
			return;
		}

		int frameCount = Mathf.Max(1, FramesBeforeReveal);
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		await FadeCoverTo(0.0f);
		FinishTransition();
	}

	private async Task FadeCoverTo(float targetAlpha)
	{
		Tween tween = CreateTween();
		tween.TweenProperty(
			_cover,
			"color:a",
			targetAlpha,
			Mathf.Max(0.01f, FadeDuration));
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private void FinishTransition()
	{
		_cover.Hide();
		_cover.MouseFilter = Control.MouseFilterEnum.Ignore;
		_isTransitioning = false;
	}
}
