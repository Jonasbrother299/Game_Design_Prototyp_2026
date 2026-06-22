using Godot;

public static class MushroomVisualBuilder
{
	public static Node3D Create(PlantInstance plant)
	{
		Node3D root = new Node3D();
		root.Name = "Mushroom_Visual";

		int stage = GetMushroomStage(plant);

		switch (stage)
		{
			case 1:
				AddMushroom(
					root,
					new Vector3(0.0f, 0.0f, 0.0f),
					stemHeight: 0.16f,
					stemRadius: 0.03f,
					capRadius: 0.12f,
					scale: 0.75f
				);
				break;

			case 2:
				AddMushroom(
					root,
					new Vector3(-0.08f, 0.0f, 0.0f),
					stemHeight: 0.22f,
					stemRadius: 0.035f,
					capRadius: 0.16f,
					scale: 1.0f
				);

				AddMushroom(
					root,
					new Vector3(0.14f, 0.0f, 0.08f),
					stemHeight: 0.12f,
					stemRadius: 0.025f,
					capRadius: 0.09f,
					scale: 0.7f
				);
				break;

			case 3:
				AddMushroom(
					root,
					new Vector3(-0.14f, 0.0f, -0.02f),
					stemHeight: 0.26f,
					stemRadius: 0.04f,
					capRadius: 0.18f,
					scale: 1.1f
				);

				AddMushroom(
					root,
					new Vector3(0.16f, 0.0f, 0.06f),
					stemHeight: 0.24f,
					stemRadius: 0.038f,
					capRadius: 0.17f,
					scale: 1.0f
				);

				AddMushroom(
					root,
					new Vector3(0.02f, 0.0f, 0.16f),
					stemHeight: 0.12f,
					stemRadius: 0.025f,
					capRadius: 0.09f,
					scale: 0.65f
				);
				break;
		}
		if (plant != null && plant.IsMature)
		{
			AddProductionAura(root);
		}
		return root;
	}

	private static int GetMushroomStage(PlantInstance plant)
	{
		if (plant == null)
			return 1;

		if (plant.IsMature)
			return 3;

		if (plant.RemainingGrowthRounds == plant.Definition.GrowthRounds)
			return 1;

		return 2;
	}

	private static void AddMushroom(
		Node3D root,
		Vector3 offset,
		float stemHeight,
		float stemRadius,
		float capRadius,
		float scale
	)
	{
		Node3D mushroomRoot = new Node3D();
		mushroomRoot.Position = offset;
		mushroomRoot.Scale = new Vector3(scale, scale, scale);

		MeshInstance3D stem = VisualPrimitiveFactory.CreateCylinder(
			new Vector3(0.0f, stemHeight * 0.5f, 0.0f),
			stemRadius,
			stemRadius + 0.005f,
			stemHeight,
			new Color(0.82f, 0.74f, 0.60f)
		);

		MeshInstance3D cap = VisualPrimitiveFactory.CreateSphere(
			new Vector3(0.0f, stemHeight + capRadius * 0.45f, 0.0f),
			capRadius,
			new Color(0.55f, 0.28f, 0.22f),
			new Vector3(1.0f, 0.55f, 1.0f)
		);

		mushroomRoot.AddChild(stem);
		mushroomRoot.AddChild(cap);

		root.AddChild(mushroomRoot);
	}
	private static void AddProductionAura(Node3D root)
{
	CylinderMesh mesh = new CylinderMesh();
	mesh.TopRadius = 0.46f;
	mesh.BottomRadius = 0.46f;
	mesh.Height = 0.012f;
	mesh.RadialSegments = 48;

	MeshInstance3D aura = new MeshInstance3D();
	aura.Name = "ProductionAura";
	aura.Mesh = mesh;
	aura.Position = new Vector3(0.0f, 0.01f, 0.0f);

	StandardMaterial3D material = new StandardMaterial3D();
	material.AlbedoColor = new Color(0.30f, 0.85f, 0.38f, 0.28f);
	material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
	material.Roughness = 0.8f;

	material.EmissionEnabled = true;
	material.Emission = new Color(0.25f, 0.75f, 0.35f);
	material.EmissionEnergyMultiplier = 0.35f;

	aura.MaterialOverride = material;

	root.AddChild(aura);
}
}
