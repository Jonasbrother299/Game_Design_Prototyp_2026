using Godot;

public static class FlowerVisualBuilder
{
	private static readonly Vector3[] ClusterOffsets =
	{
		Vector3.Zero,
		new Vector3(-0.15f, 0.0f, 0.10f),
		new Vector3(0.15f, 0.0f, 0.08f),
		new Vector3(0.03f, 0.0f, -0.15f),
		new Vector3(-0.20f, 0.0f, -0.10f),
		new Vector3(0.20f, 0.0f, -0.08f),
		new Vector3(-0.03f, 0.0f, 0.19f)
	};

	private static readonly float[] ClusterScaleMultipliers =
	{
		1.0f,
		0.82f,
		0.90f,
		0.76f,
		0.84f,
		0.72f,
		0.78f
	};

	private static readonly float[] ClusterRotations =
	{
		0.0f,
		42.0f,
		-35.0f,
		85.0f,
		-72.0f,
		128.0f,
		-118.0f
	};

	public static Node3D Create(
		PlantInstance plant,
		float modelScale,
		int matureFlowerCount)
	{
		Node3D root = new Node3D();
		root.Name = "Flower_Visual";

		if (TryAddModelCluster(
			root,
			plant,
			modelScale,
			matureFlowerCount))
		{
			return root;
		}

		AddFallbackVisual(root, plant);
		return root;
	}

	private static bool TryAddModelCluster(
		Node3D root,
		PlantInstance plant,
		float modelScale,
		int matureFlowerCount)
	{
		PackedScene flowerScene = plant?.Definition?.PlantScene;

		if (flowerScene == null)
			return false;

		int stage = Mathf.Clamp(plant?.VisualGrowthStage ?? 1, 1, 3);
		int maximumCount = Mathf.Clamp(
			matureFlowerCount,
			1,
			ClusterOffsets.Length);
		int visibleCount = stage switch
		{
			1 => 1,
			2 => Mathf.Min(2, maximumCount),
			_ => maximumCount
		};
		float stageScale = stage switch
		{
			1 => 0.70f,
			2 => 0.85f,
			_ => 1.0f
		};
		float safeModelScale = Mathf.Max(0.01f, modelScale);

		for (int i = 0; i < visibleCount; i++)
		{
			Node instance = flowerScene.Instantiate();

			if (instance is not Node3D flowerModel)
			{
				instance?.Free();
				ClearChildren(root);
				return false;
			}

			flowerModel.Name = $"FlowerModel_{i + 1}";
			flowerModel.Position = ClusterOffsets[i];
			flowerModel.RotationDegrees =
				new Vector3(0.0f, ClusterRotations[i], 0.0f);
			flowerModel.Scale *=
				safeModelScale *
				stageScale *
				ClusterScaleMultipliers[i];

			root.AddChild(flowerModel);
		}

		return true;
	}

	private static void ClearChildren(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			root.RemoveChild(child);
			child.Free();
		}
	}

	private static void AddFallbackVisual(Node3D root, PlantInstance plant)
	{
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
