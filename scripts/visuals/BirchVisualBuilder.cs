using Godot;

public static class BirchVisualBuilder
{
	public static Node3D Create(PlantInstance plant)
	{
		Node3D root = new Node3D();
		root.Name = "Birch_Visual";

		int stage = Mathf.Clamp(plant?.VisualGrowthStage ?? 1, 1, 5);
		float progress = (stage - 1) / 4.0f;
		float trunkHeight = Mathf.Lerp(0.30f, 1.10f, progress);
		float trunkRadius = Mathf.Lerp(0.035f, 0.09f, progress);

		root.AddChild(VisualPrimitiveFactory.CreateCylinder(
			new Vector3(0.0f, trunkHeight * 0.5f, 0.0f),
			trunkRadius * 0.78f,
			trunkRadius,
			trunkHeight,
			new Color(0.84f, 0.82f, 0.76f)));

		AddBarkMark(root, trunkHeight * 0.28f, trunkRadius, progress);
		if (stage >= 3)
			AddBarkMark(root, trunkHeight * 0.52f, trunkRadius, progress);
		if (stage >= 4)
			AddBarkMark(root, trunkHeight * 0.72f, trunkRadius, progress);

		if (stage == 1)
		{
			AddLeafBlob(root, new Vector3(0.0f, trunkHeight + 0.05f, 0.0f), 0.13f);
			return root;
		}

		float canopyRadius = Mathf.Lerp(0.17f, 0.34f, progress);
		AddLeafBlob(
			root,
			new Vector3(-canopyRadius * 0.40f, trunkHeight + canopyRadius * 0.15f, 0.0f),
			canopyRadius);
		AddLeafBlob(
			root,
			new Vector3(canopyRadius * 0.45f, trunkHeight, 0.04f),
			canopyRadius * 0.92f);

		if (stage >= 3)
		{
			AddLeafBlob(
				root,
				new Vector3(0.0f, trunkHeight + canopyRadius * 0.62f, -0.08f),
				canopyRadius * 0.88f);
		}

		if (stage >= 4)
		{
			AddLeafBlob(
				root,
				new Vector3(-canopyRadius * 0.20f, trunkHeight - 0.08f, 0.20f),
				canopyRadius * 0.72f);
		}

		return root;
	}

	private static void AddBarkMark(
		Node3D root,
		float height,
		float trunkRadius,
		float progress)
	{
		root.AddChild(VisualPrimitiveFactory.CreateCylinder(
			new Vector3(0.0f, height, 0.0f),
			trunkRadius * 1.04f,
			trunkRadius * 1.04f,
			Mathf.Lerp(0.025f, 0.045f, progress),
			new Color(0.20f, 0.18f, 0.16f)));
	}

	private static void AddLeafBlob(Node3D root, Vector3 position, float radius)
	{
		root.AddChild(VisualPrimitiveFactory.CreateSphere(
			position,
			radius,
			new Color(0.44f, 0.64f, 0.30f),
			new Vector3(1.0f, 0.82f, 0.95f)));
	}
}
