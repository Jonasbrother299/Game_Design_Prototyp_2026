using Godot;

public static class StartingOakVisualBuilder
{
	private const string ModelPath = "res://assets/models/starting_oak.fbx";

	public static Node3D Create(PlantInstance plant)
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
		model.Scale = new Vector3(1.0f, 1.0f, 1.0f);

		return model;
	}

	private static Node3D CreateFallback(PlantInstance plant)
	{
		Node3D fallback = OakVisualBuilder.Create(plant);
		fallback.Name = "StartingOak_Fallback";
		fallback.Scale = new Vector3(1.35f, 1.35f, 1.35f);

		return fallback;
	}
}
