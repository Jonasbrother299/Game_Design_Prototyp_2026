using Godot;

public static class MossVisualBuilder
{
	private static readonly StringName GrassBlockerGroup = "grass_blocker";

	public static Node3D Create(
		PlantInstance plant,
		HexCoord tileCoord)
	{
		int stage = GetStage(plant);
		Node3D modelVisual = CreateModelVisual(plant, stage);
		modelVisual ??= CreateFallbackVisual(stage);
		ApplyModelVariation(modelVisual, tileCoord);

		Node3D root = new Node3D
		{
			Name = "Moss_Visual"
		};
		root.AddChild(modelVisual);

		return root;
	}

	private static Node3D CreateModelVisual(
		PlantInstance plant,
		int stage)
	{
		PackedScene plantScene = plant?.Definition?.PlantScene;
		if (plantScene == null)
			return null;

		Node instance = plantScene.Instantiate();
		if (instance is not Node3D root)
		{
			instance.Free();
			GD.PushWarning(
				"MossVisualBuilder: PlantScene benötigt einen Node3D-Root.");
			return null;
		}

		root.Name = "MossModel";
		SetPartVisible(root, "Ground", false);
		SetPartVisible(root, "Moss1", true);
		SetPartVisible(root, "Moss2", stage >= 2);
		SetPartVisible(root, "Moss3", stage >= 3);
		SetPartVisible(root, "Moss4", false);
		return root;
	}

	private static void SetPartVisible(
		Node3D root,
		string nodeName,
		bool visible)
	{
		Node3D part = root.GetNodeOrNull<Node3D>(nodeName);
		if (part == null)
			return;

		if (nodeName != "Ground")
			part.AddToGroup(GrassBlockerGroup);

		part.Visible = visible;
		SetCollisionShapesEnabled(part, visible);
	}

	private static void SetCollisionShapesEnabled(Node node, bool enabled)
	{
		if (node is CollisionShape3D collisionShape)
			collisionShape.Disabled = !enabled;

		foreach (Node child in node.GetChildren())
			SetCollisionShapesEnabled(child, enabled);
	}

	private static Node3D CreateFallbackVisual(int stage)
	{
		Node3D root = new Node3D
		{
			Name = "MossModel"
		};

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

	private static void ApplyModelVariation(
		Node3D modelVisual,
		HexCoord tileCoord)
	{
		modelVisual.Position += new Vector3(
			GetSignedTileRandom(tileCoord, 31u) * 0.035f,
			-0.08f,
			GetSignedTileRandom(tileCoord, 37u) * 0.035f);
		modelVisual.RotationDegrees += new Vector3(
			0.0f,
			GetTileRandom(tileCoord, 41u) * 360.0f,
			0.0f);

		float widthScale = Mathf.Lerp(
			1.08f,
			1.24f,
			GetTileRandom(tileCoord, 43u));
		float depthScale = Mathf.Lerp(
			1.06f,
			1.26f,
			GetTileRandom(tileCoord, 47u));
		modelVisual.Scale *= new Vector3(widthScale, 0.68f, depthScale);

		ApplyPartVariation(modelVisual, "Moss1", tileCoord, 61u);
		ApplyPartVariation(modelVisual, "Moss2", tileCoord, 71u);
		ApplyPartVariation(modelVisual, "Moss3", tileCoord, 81u);
	}

	private static void ApplyPartVariation(
		Node3D root,
		string nodeName,
		HexCoord tileCoord,
		uint salt)
	{
		Node3D part = root.GetNodeOrNull<Node3D>(nodeName);
		if (part == null)
			return;

		part.Position += new Vector3(
			GetSignedTileRandom(tileCoord, salt) * 0.055f,
			0.0f,
			GetSignedTileRandom(tileCoord, salt + 1u) * 0.055f);
		part.RotationDegrees += new Vector3(
			0.0f,
			GetSignedTileRandom(tileCoord, salt + 2u) * 32.0f,
			0.0f);
		float scale = Mathf.Lerp(
			0.96f,
			1.14f,
			GetTileRandom(tileCoord, salt + 3u));
		part.Scale *= new Vector3(scale, 1.0f, scale);
	}

	private static int GetStage(PlantInstance plant)
	{
		return Mathf.Clamp(plant?.VisualGrowthStage ?? 1, 1, 3);
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

	private static float GetSignedTileRandom(HexCoord coord, uint salt)
	{
		return GetTileRandom(coord, salt) * 2.0f - 1.0f;
	}

	private static float GetTileRandom(HexCoord coord, uint salt)
	{
		unchecked
		{
			uint value = (uint)coord.Q * 0x9E3779B9u;
			value ^= (uint)coord.R * 0x85EBCA6Bu;
			value ^= salt * 0xC2B2AE35u;
			value ^= value >> 16;
			value *= 0x7FEB352Du;
			value ^= value >> 15;
			value *= 0x846CA68Bu;
			value ^= value >> 16;
			return (value & 0x00FFFFFFu) / 16777215.0f;
		}
	}
}
