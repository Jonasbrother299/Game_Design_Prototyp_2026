using Godot;
using System.Threading.Tasks;

public partial class SceneTransition : CanvasLayer
{
	public static SceneTransition Instance { get; private set; }

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
