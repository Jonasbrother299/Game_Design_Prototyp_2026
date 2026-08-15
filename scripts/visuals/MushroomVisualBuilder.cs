using Godot;

public static class MushroomVisualBuilder
{
	private static readonly Vector3[] ClusterOffsets =
	{
		Vector3.Zero,
		new Vector3(0.28f, 0.0f, 0.12f),
		new Vector3(-0.25f, 0.0f, 0.16f),
		new Vector3(0.06f, 0.0f, -0.28f)
	};

	private static readonly float[] ClusterRotations =
	{
		0.0f,
		120.0f,
		240.0f,
		45.0f
	};

	private static readonly float[] ClusterScaleMultipliers =
	{
		1.0f,
		0.82f,
		0.92f,
		0.76f
	};

	public static Node3D Create(
		PlantInstance plant,
		float modelScale,
		float animationSpeed,
		bool animateGrowth)
	{
		Node3D root = new Node3D();
		root.Name = "Mushroom_Visual";

		AddMushroomModels(root, plant, modelScale);

		return root;
	}

	private static void AddMushroomModels(
		Node3D root,
		PlantInstance plant,
		float modelScale)
	{
		PackedScene mushroomScene = plant?.Definition?.PlantScene;
		if (mushroomScene == null)
			return;

		int visibleModelCount = Mathf.Clamp(
			plant?.VisualGrowthStage ?? 1,
			1,
			ClusterOffsets.Length);
		float safeModelScale = Mathf.Max(0.1f, modelScale);

		for (int i = 0; i < visibleModelCount; i++)
		{
			Node instance = mushroomScene.Instantiate();
			if (instance is not Node3D mushroomModel)
			{
				instance?.Free();
				continue;
			}

			mushroomModel.Name = $"MushroomModel_{i + 1}";
			mushroomModel.Position = ClusterOffsets[i];
			mushroomModel.RotationDegrees =
				new Vector3(0.0f, ClusterRotations[i], 0.0f);
			mushroomModel.Scale *=
				safeModelScale *
				ClusterScaleMultipliers[i];
			root.AddChild(mushroomModel);
		}
	}

}
