using Godot;

public static class StartingOakVisualBuilder
{
	private const string ModelPath = "res://scenes/board/plants/Oak.tscn";
	private const float MinimumFallbackScale = 0.65f;
	private const float MaximumFallbackScale = 1.0f;

	public static Node3D Create(
		PlantInstance plant,
		float modelScale)
	{
		PackedScene modelScene = GD.Load<PackedScene>(ModelPath);

		if (modelScene == null)
		{
			GD.PrintErr($"Starting oak model not found: {ModelPath}");
			return CreateFallback(plant);
		}

		Node instance = modelScene.Instantiate();

		if (instance is not Node3D model)
		{
			GD.PrintErr($"Starting oak model is not a Node3D: {ModelPath}");
			instance.QueueFree();
			return CreateFallback(plant);
		}

		model.Name = "StartingOak_Visual";
		model.Position = Vector3.Zero;
		model.Rotation = Vector3.Zero;
		model.Scale = Vector3.One * modelScale;

		return model;
	}

	private static Node3D CreateFallback(PlantInstance plant)
	{
		Node3D fallback = OakVisualBuilder.Create(plant);
		fallback.Name = "StartingOak_Fallback";
		fallback.Scale = Vector3.One * GetScale(
			plant,
			MinimumFallbackScale,
			MaximumFallbackScale);

		return fallback;
	}

	private static float GetScale(
		PlantInstance plant,
		float minimumScale,
		float maximumScale)
	{
		float growthProgress = plant?.GrowthProgress ?? 1.0f;
		return Mathf.Lerp(minimumScale, maximumScale, growthProgress);
	}
}
