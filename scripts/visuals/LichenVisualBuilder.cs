using Godot;

public static class LichenVisualBuilder
{
	public static Node3D Create(PlantInstance plant)
	{
		Node3D root = new Node3D();
		root.Name = "Lichen_Visual";

		int stage = GetStage(plant);

		switch (stage)
		{
			case 1:
				AddLichenSpot(root, new Vector3(-0.05f, 0.012f, 0.02f), 0.11f, new Color(0.62f, 0.72f, 0.42f));
				AddLichenSpot(root, new Vector3(0.08f, 0.013f, -0.05f), 0.08f, new Color(0.72f, 0.78f, 0.52f));
				break;

			case 2:
				AddLichenSpot(root, new Vector3(-0.14f, 0.012f, -0.04f), 0.12f, new Color(0.64f, 0.72f, 0.42f));
				AddLichenSpot(root, new Vector3(0.05f, 0.013f, 0.08f), 0.13f, new Color(0.76f, 0.80f, 0.55f));
				AddLichenSpot(root, new Vector3(0.16f, 0.012f, -0.08f), 0.08f, new Color(0.52f, 0.64f, 0.38f));
				AddLichenSpot(root, new Vector3(-0.04f, 0.014f, -0.16f), 0.07f, new Color(0.82f, 0.82f, 0.60f));
				break;

			case 3:
				AddLichenSpot(root, new Vector3(-0.20f, 0.012f, -0.10f), 0.13f, new Color(0.62f, 0.70f, 0.42f));
				AddLichenSpot(root, new Vector3(-0.06f, 0.013f, 0.08f), 0.15f, new Color(0.76f, 0.80f, 0.55f));
				AddLichenSpot(root, new Vector3(0.16f, 0.012f, 0.04f), 0.12f, new Color(0.54f, 0.64f, 0.38f));
				AddLichenSpot(root, new Vector3(0.22f, 0.013f, -0.16f), 0.08f, new Color(0.84f, 0.82f, 0.58f));
				AddLichenSpot(root, new Vector3(-0.16f, 0.014f, 0.18f), 0.09f, new Color(0.68f, 0.75f, 0.48f));
				AddLichenSpot(root, new Vector3(0.02f, 0.013f, -0.22f), 0.10f, new Color(0.58f, 0.68f, 0.40f));
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

	private static void AddLichenSpot(
		Node3D root,
		Vector3 position,
		float radius,
		Color color
	)
	{
		MeshInstance3D spot = VisualPrimitiveFactory.CreateSphere(
			position,
			radius,
			color,
			new Vector3(1.35f, 0.045f, 0.75f)
		);

		root.AddChild(spot);
	}
}
