using Godot;

public static class OakVisualBuilder
{
	public static Node3D Create(PlantInstance plant)
	{
		Node3D root = new Node3D();
		root.Name = "Oak_Visual";

		int stage = GetTreeStage(plant);

		switch (stage)
		{
			case 1:
				CreateSapling(root);
				break;

			case 2:
				CreateYoungTree(root);
				break;

			case 3:
				CreateAlmostTree(root);
				break;

			case 4:
				CreateFullTree(root);
				break;
		}

		return root;
	}

	private static int GetTreeStage(PlantInstance plant)
	{
		if (plant == null)
			return 1;

		if (plant.IsMature)
			return 4;

		int totalGrowthRounds = plant.Definition.GrowthRounds;
		int completedGrowthRounds = totalGrowthRounds - plant.RemainingGrowthRounds;

		if (completedGrowthRounds <= 0)
			return 1;

		if (completedGrowthRounds == 1)
			return 2;

		return 3;
	}

	private static void CreateSapling(Node3D root)
	{
		MeshInstance3D stem = VisualPrimitiveFactory.CreateCylinder(
			new Vector3(0.0f, 0.10f, 0.0f),
			0.025f,
			0.035f,
			0.20f,
			new Color(0.42f, 0.25f, 0.12f)
		);

		MeshInstance3D leaves = VisualPrimitiveFactory.CreateSphere(
			new Vector3(0.0f, 0.25f, 0.0f),
			0.12f,
			new Color(0.25f, 0.55f, 0.20f),
			new Vector3(1.0f, 0.75f, 1.0f)
		);

		root.AddChild(stem);
		root.AddChild(leaves);
	}

	private static void CreateYoungTree(Node3D root)
	{
		MeshInstance3D trunk = VisualPrimitiveFactory.CreateCylinder(
			new Vector3(0.0f, 0.18f, 0.0f),
			0.035f,
			0.055f,
			0.36f,
			new Color(0.42f, 0.25f, 0.12f)
		);

		root.AddChild(trunk);

		AddLeafBlob(root, new Vector3(-0.08f, 0.42f, 0.0f), 0.16f);
		AddLeafBlob(root, new Vector3(0.08f, 0.45f, 0.03f), 0.17f);
	}

	private static void CreateAlmostTree(Node3D root)
	{
		MeshInstance3D trunk = VisualPrimitiveFactory.CreateCylinder(
			new Vector3(0.0f, 0.28f, 0.0f),
			0.055f,
			0.085f,
			0.56f,
			new Color(0.40f, 0.23f, 0.11f)
		);

		root.AddChild(trunk);

		AddLeafBlob(root, new Vector3(-0.14f, 0.65f, 0.0f), 0.22f);
		AddLeafBlob(root, new Vector3(0.12f, 0.68f, 0.04f), 0.23f);
		AddLeafBlob(root, new Vector3(0.0f, 0.78f, -0.10f), 0.20f);
	}

	private static void CreateFullTree(Node3D root)
	{
		MeshInstance3D trunk = VisualPrimitiveFactory.CreateCylinder(
			new Vector3(0.0f, 0.36f, 0.0f),
			0.075f,
			0.12f,
			0.72f,
			new Color(0.36f, 0.20f, 0.10f)
		);

		root.AddChild(trunk);

		AddLeafBlob(root, new Vector3(-0.20f, 0.82f, 0.02f), 0.27f);
		AddLeafBlob(root, new Vector3(0.18f, 0.84f, 0.04f), 0.28f);
		AddLeafBlob(root, new Vector3(0.00f, 0.98f, -0.12f), 0.25f);
		AddLeafBlob(root, new Vector3(0.02f, 0.78f, 0.18f), 0.22f);
	}

	private static void AddLeafBlob(Node3D root, Vector3 position, float radius)
	{
		MeshInstance3D leaves = VisualPrimitiveFactory.CreateSphere(
			position,
			radius,
			new Color(0.24f, 0.55f, 0.18f),
			new Vector3(1.15f, 0.85f, 1.0f)
		);

		root.AddChild(leaves);
	}
}
