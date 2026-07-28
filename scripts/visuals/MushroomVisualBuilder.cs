using Godot;
using System.Collections.Generic;

public static class MushroomVisualBuilder
{
	private const int GrowthModelCount = 3;

	public static Node3D Create(
		PlantInstance plant,
		float modelScale,
		float animationSpeed,
		bool animateGrowth)
	{
		Node3D root = new Node3D();
		root.Name = "Mushroom_Visual";

		if (!AddGrowthModels(
				root,
				plant,
				modelScale,
				animationSpeed,
				animateGrowth))
			AddFallbackMushrooms(root, plant);

		if (plant != null && plant.IsMature)
		{
			AddProductionAura(root);
		}

		return root;
	}

	private static bool AddGrowthModels(
		Node3D root,
		PlantInstance plant,
		float modelScale,
		float animationSpeed,
		bool animateGrowth)
	{
		Godot.Collections.Array<PackedScene> growthScenes =
			plant?.Definition?.GrowthStageScenes;

		if (growthScenes == null || growthScenes.Count != GrowthModelCount)
			return false;

		foreach (PackedScene growthScene in growthScenes)
		{
			if (growthScene == null)
				return false;
		}

		int visualStage = Mathf.Clamp(
			plant.VisualGrowthStage,
			1,
			GrowthModelCount + 1);
		int visibleModelCount = Mathf.Min(visualStage, GrowthModelCount);
		float safeModelScale = Mathf.Max(0.1f, modelScale);
		List<Node3D> models = new();

		for (int i = 0; i < GrowthModelCount; i++)
		{
			Node instance = growthScenes[i].Instantiate();
			if (instance is not Node3D model)
			{
				instance.Free();
				FreeModels(models);
				return false;
			}

			model.Name = $"MushroomGrowthModel_{i + 1}";
			model.Visible = i < visibleModelCount;

			List<Node3D> renderNodes = new();
			CollectRenderNodes(model, renderNodes);
			CompensateModelScale(renderNodes, safeModelScale);

			model.Position = new Vector3(-0.22f, -0.14f, 0.40f);
			model.Scale *= safeModelScale;

			bool shouldAnimate =
				animateGrowth &&
				visualStage <= GrowthModelCount &&
				i == visualStage - 1;
			bool shouldShowEndState =
				i < visibleModelCount &&
				!shouldAnimate;

			AnimationPlayer animationPlayer = FindAnimationPlayer(model);
			ConfigureStageAnimationWhenInsideTree(
				animationPlayer,
				shouldShowEndState,
				shouldAnimate,
				animationSpeed);

			models.Add(model);
		}

		foreach (Node3D model in models)
			root.AddChild(model);

		return true;
	}

	private static void CollectRenderNodes(
		Node node,
		List<Node3D> renderNodes)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D meshInstance)
				renderNodes.Add(meshInstance);

			CollectRenderNodes(child, renderNodes);
		}
	}

	private static AnimationPlayer FindAnimationPlayer(Node node)
	{
		if (node is AnimationPlayer animationPlayer)
			return animationPlayer;

		foreach (Node child in node.GetChildren())
		{
			AnimationPlayer found = FindAnimationPlayer(child);
			if (found != null)
				return found;
		}

		return null;
	}

	private static void ConfigureStageAnimationWhenInsideTree(
		AnimationPlayer animationPlayer,
		bool shouldShowEndState,
		bool shouldAnimate,
		float animationSpeed)
	{
		if (animationPlayer == null ||
			(!shouldShowEndState && !shouldAnimate))
			return;

		if (animationPlayer.IsInsideTree())
		{
			ConfigureStageAnimation(
				animationPlayer,
				shouldShowEndState,
				shouldAnimate,
				animationSpeed);
			return;
		}

		void OnTreeEntered()
		{
			animationPlayer.TreeEntered -= OnTreeEntered;
			ConfigureStageAnimation(
				animationPlayer,
				shouldShowEndState,
				shouldAnimate,
				animationSpeed);
		}

		animationPlayer.TreeEntered += OnTreeEntered;
	}

	private static void ConfigureStageAnimation(
		AnimationPlayer animationPlayer,
		bool shouldShowEndState,
		bool shouldAnimate,
		float animationSpeed)
	{
		StringName growthAnimation = FindGrowthAnimation(animationPlayer);
		if (IsAnimationNameMissing(growthAnimation))
			return;

		if (shouldAnimate)
		{
			animationPlayer.SpeedScale = Mathf.Max(0.1f, animationSpeed);
			animationPlayer.Play(growthAnimation);
			animationPlayer.Advance(0.0);
			return;
		}

		if (shouldShowEndState)
			ApplyAnimationEnd(animationPlayer, growthAnimation);
	}

	private static StringName FindGrowthAnimation(AnimationPlayer animationPlayer)
	{
		foreach (StringName animationName in animationPlayer.GetAnimationList())
		{
			if (IsAnimationNameMissing(animationName))
				continue;

			if (animationName.ToString() != "RESET")
				return animationName;
		}

		return default;
	}

	private static void ApplyAnimationEnd(
		AnimationPlayer animationPlayer,
		StringName animationName)
	{
		if (IsAnimationNameMissing(animationName))
			return;

		Animation animation = animationPlayer.GetAnimation(animationName);
		if (animation == null)
			return;

		animationPlayer.Play(animationName);
		animationPlayer.Seek(animation.Length, update: true);
		animationPlayer.Stop(keepState: true);
	}

	private static bool IsAnimationNameMissing(StringName animationName)
	{
		return animationName == null ||
			string.IsNullOrEmpty(animationName.ToString());
	}

	private static void CompensateModelScale(
		List<Node3D> nodes,
		float modelScale)
	{
		foreach (Node3D node in nodes)
		{
			node.Position /= modelScale;
		}
	}

	private static void FreeModels(List<Node3D> models)
	{
		foreach (Node3D model in models)
			model.Free();
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
