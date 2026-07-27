using Godot;

public static class FlowerVisualBuilder
{
	public static Node3D Create(PlantInstance plant)
	{
		Node3D root = new Node3D();
		root.Name = "Flower_Visual";

		int stage = Mathf.Clamp(plant?.VisualGrowthStage ?? 1, 1, 3);

		AddFlower(
			root,
			Vector3.Zero,
			stage == 1 ? 0.55f : 0.85f,
			hasBloom: stage >= 2,
			new Color(0.82f, 0.42f, 0.68f));

		if (stage >= 2)
		{
			AddFlower(
				root,
				new Vector3(-0.18f, 0.0f, 0.12f),
				0.68f,
				hasBloom: true,
				new Color(0.92f, 0.62f, 0.34f));
		}

		if (stage >= 3)
		{
			AddFlower(
				root,
				new Vector3(0.20f, 0.0f, 0.08f),
				0.72f,
				hasBloom: true,
				new Color(0.66f, 0.50f, 0.88f));

			AddFlower(
				root,
				new Vector3(0.05f, 0.0f, -0.18f),
				0.58f,
				hasBloom: true,
				new Color(0.90f, 0.78f, 0.38f));
		}

		return root;
	}

	private static void AddFlower(
		Node3D root,
		Vector3 offset,
		float scale,
		bool hasBloom,
		Color petalColor)
	{
		float stemHeight = 0.48f * scale;

		root.AddChild(VisualPrimitiveFactory.CreateCylinder(
			offset + new Vector3(0.0f, stemHeight * 0.5f, 0.0f),
			0.018f * scale,
			0.025f * scale,
			stemHeight,
			new Color(0.24f, 0.48f, 0.20f)));

		root.AddChild(VisualPrimitiveFactory.CreateSphere(
			offset + new Vector3(0.06f * scale, stemHeight * 0.55f, 0.0f),
			0.055f * scale,
			new Color(0.32f, 0.58f, 0.24f),
			new Vector3(1.5f, 0.45f, 0.8f)));

		if (!hasBloom)
		{
			root.AddChild(VisualPrimitiveFactory.CreateSphere(
				offset + new Vector3(0.0f, stemHeight + 0.035f, 0.0f),
				0.055f * scale,
				new Color(0.30f, 0.56f, 0.24f),
				new Vector3(0.85f, 1.2f, 0.85f)));
			return;
		}

		Vector3 bloomCenter = offset + new Vector3(0.0f, stemHeight + 0.04f, 0.0f);

		root.AddChild(VisualPrimitiveFactory.CreateSphere(
			bloomCenter,
			0.052f * scale,
			new Color(0.82f, 0.68f, 0.18f)));

		Vector3[] petalOffsets =
		{
			new Vector3(0.0f, 0.0f, 0.075f),
			new Vector3(0.0f, 0.0f, -0.075f),
			new Vector3(0.075f, 0.0f, 0.0f),
			new Vector3(-0.075f, 0.0f, 0.0f)
		};

		foreach (Vector3 petalOffset in petalOffsets)
		{
			root.AddChild(VisualPrimitiveFactory.CreateSphere(
				bloomCenter + petalOffset * scale,
				0.052f * scale,
				petalColor,
				new Vector3(1.2f, 0.48f, 1.2f)));
		}
	}
}
