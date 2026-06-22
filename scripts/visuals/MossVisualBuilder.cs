using Godot;

public static class MossVisualBuilder
{
	public static Node3D Create(PlantInstance plant)
	{
		Node3D root = new Node3D();
		root.Name = "Moss_Visual";

		int stage = GetStage(plant);

		switch (stage)
		{
			case 1:
				AddMossPatch(root, new Vector3(0.0f, 0.015f, 0.0f), 0.18f, 0.10f, new Color(0.22f, 0.48f, 0.18f));
				break;

			case 2:
				AddMossPatch(root, new Vector3(-0.12f, 0.015f, 0.0f), 0.20f, 0.11f, new Color(0.20f, 0.45f, 0.17f));
				AddMossPatch(root, new Vector3(0.10f, 0.015f, 0.08f), 0.17f, 0.10f, new Color(0.28f, 0.55f, 0.22f));
				AddMossPatch(root, new Vector3(0.04f, 0.015f, -0.12f), 0.14f, 0.08f, new Color(0.18f, 0.40f, 0.15f));
				break;

			case 3:
				AddMossPatch(root, new Vector3(-0.18f, 0.015f, -0.04f), 0.24f, 0.13f, new Color(0.20f, 0.44f, 0.16f));
				AddMossPatch(root, new Vector3(0.08f, 0.015f, 0.10f), 0.25f, 0.13f, new Color(0.30f, 0.58f, 0.22f));
				AddMossPatch(root, new Vector3(0.18f, 0.015f, -0.10f), 0.18f, 0.10f, new Color(0.18f, 0.38f, 0.14f));
				AddMossPatch(root, new Vector3(-0.02f, 0.018f, -0.20f), 0.16f, 0.09f, new Color(0.26f, 0.52f, 0.20f));
				AddMossPatch(root, new Vector3(-0.08f, 0.018f, 0.20f), 0.15f, 0.08f, new Color(0.22f, 0.46f, 0.18f));
				break;
		}

		return root;
	}

	private static int GetStage(PlantInstance plant)
	{
		if (plant == null)
			return 1;

		if (plant.IsMature)
			return 3;

		if (plant.GrowthProgress < 0.5f)
			return 1;

		return 2;
	}

	private static void AddMossPatch(
		Node3D root,
		Vector3 position,
		float radius,
		float heightScale,
		Color color
	)
	{
		MeshInstance3D patch = VisualPrimitiveFactory.CreateSphere(
			position,
			radius,
			color,
			new Vector3(1.25f, heightScale, 0.85f)
		);

		root.AddChild(patch);
	}
}
