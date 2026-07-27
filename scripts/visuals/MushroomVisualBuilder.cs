using Godot;
using System.Collections.Generic;

public static class MushroomVisualBuilder
{
	private const float ModelMeshScaleMultiplier = 12.0f;

	public static Node3D Create(PlantInstance plant)
	{
		Node3D root = new Node3D();
		root.Name = "Mushroom_Visual";

		Node3D model = CreateModel(plant);
		if (model != null)
		{
			root.AddChild(model);
		}
		else
		{
			AddFallbackMushrooms(root, plant);
		}

		if (plant != null && plant.IsMature)
		{
			AddProductionAura(root);
		}

		return root;
	}

	private static Node3D CreateModel(PlantInstance plant)
	{
		PackedScene plantScene = plant?.Definition?.PlantScene;
		if (plantScene == null)
			return null;

		Node instance = plantScene.Instantiate();
		if (instance is not Node3D model)
		{
			instance.Free();
			return null;
		}

		List<Node3D> stageOneNodes = new();
		List<Node3D> stageTwoNodes = new();
		List<Node3D> stageThreeNodes = new();

		CollectStageNodes(model, stageOneNodes, stageTwoNodes, stageThreeNodes);

		if (stageOneNodes.Count + stageTwoNodes.Count + stageThreeNodes.Count == 0)
		{
			model.Free();
			return null;
		}

		stageTwoNodes.Sort(CompareNodeNames);

		int visualStage = Mathf.Clamp(plant.VisualGrowthStage, 1, 4);
		SetNodesVisible(stageOneNodes, true);

		for (int i = 0; i < stageTwoNodes.Count; i++)
		{
			int requiredStage = i == 0 ? 2 : 3;
			stageTwoNodes[i].Visible = visualStage >= requiredStage;
		}

		SetNodesVisible(stageThreeNodes, visualStage >= 4);

		model.Name = "MushroomModel";
		model.Position = new Vector3(-0.22f, -0.14f, 0.40f);
		return model;
	}

	private static void CollectStageNodes(
		Node node,
		List<Node3D> stageOneNodes,
		List<Node3D> stageTwoNodes,
		List<Node3D> stageThreeNodes)
	{
		foreach (Node child in node.GetChildren())
		{
			string name = child.Name.ToString().ToLowerInvariant();

			if (child is Node3D node3D)
			{
				if (name.Contains("stage1"))
				{
					PrepareModelNode(node3D);
					stageOneNodes.Add(node3D);
					continue;
				}

				if (name.Contains("stage2"))
				{
					PrepareModelNode(node3D);
					stageTwoNodes.Add(node3D);
					continue;
				}

				if (name.Contains("stage3"))
				{
					PrepareModelNode(node3D);
					stageThreeNodes.Add(node3D);
					continue;
				}
			}

			CollectStageNodes(child, stageOneNodes, stageTwoNodes, stageThreeNodes);
		}
	}

	private static void PrepareModelNode(Node3D node)
	{
		node.Scale *= ModelMeshScaleMultiplier;
	}

	private static int CompareNodeNames(Node3D first, Node3D second)
	{
		return string.CompareOrdinal(first.Name.ToString(), second.Name.ToString());
	}

	private static void SetNodesVisible(List<Node3D> nodes, bool visible)
	{
		foreach (Node3D node in nodes)
		{
			node.Visible = visible;
		}
	}

	private static void AddFallbackMushrooms(Node3D root, PlantInstance plant)
	{
		int stage = Mathf.Clamp(plant?.VisualGrowthStage ?? 1, 1, 4);

		AddFallbackMushroom(root, Vector3.Zero, 0.75f);

		if (stage >= 2)
			AddFallbackMushroom(root, new Vector3(0.14f, 0.0f, 0.08f), 0.70f);

		if (stage >= 3)
			AddFallbackMushroom(root, new Vector3(-0.16f, 0.0f, 0.08f), 0.90f);

		if (stage >= 4)
			AddFallbackMushroom(root, new Vector3(0.02f, 0.0f, -0.16f), 1.05f);
	}

	private static void AddFallbackMushroom(
		Node3D root,
		Vector3 offset,
		float scale)
	{
		float stemHeight = 0.22f * scale;

		root.AddChild(VisualPrimitiveFactory.CreateCylinder(
			offset + new Vector3(0.0f, stemHeight * 0.5f, 0.0f),
			0.035f * scale,
			0.04f * scale,
			stemHeight,
			new Color(0.82f, 0.74f, 0.60f)));

		root.AddChild(VisualPrimitiveFactory.CreateSphere(
			offset + new Vector3(0.0f, stemHeight + 0.07f * scale, 0.0f),
			0.14f * scale,
			new Color(0.55f, 0.28f, 0.22f),
			new Vector3(1.0f, 0.52f, 1.0f)));
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
