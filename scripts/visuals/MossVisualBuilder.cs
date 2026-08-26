using Godot;

public static class MossVisualBuilder
{
	private static readonly StringName GrassBlockerGroup = "grass_blocker";
	private static readonly string[] MossPartNames =
	{
		"Moss1",
		"Moss2",
		"Moss3"
	};

	public static Node3D Create(
		PlantInstance plant,
		HexCoord tileCoord)
	{
		int stage = GetStage(plant);
		Node3D modelVisual = CreateModelVisual(plant, stage, tileCoord);
		modelVisual ??= CreateFallbackVisual(stage);
		ApplyModelVariation(modelVisual, tileCoord);

		Node3D root = new Node3D
		{
			Name = "Moss_Visual"
		};
		root.AddChild(modelVisual);

		return root;
	}

	internal static void AnimateGrowth(
		Node3D visualRoot,
		HexCoord tileCoord,
		int previousStage,
		int currentStage,
		float duration)
	{
		Node3D modelVisual =
			visualRoot?.GetNodeOrNull<Node3D>("MossModel");
		if (modelVisual == null || duration <= 0.0f)
			return;

		string[] partOrder = GetPartOrder(tileCoord);
		int startIndex = Mathf.Clamp(previousStage, 0, partOrder.Length);
		int endIndex = Mathf.Clamp(currentStage, 0, partOrder.Length);
		Tween growthTween = null;

		for (int index = startIndex; index < endIndex; index++)
		{
			Node3D part = modelVisual.GetNodeOrNull<Node3D>(partOrder[index]);
			if (part == null || !part.Visible)
				continue;

			growthTween ??= CreateGrowthTween(visualRoot);
			TweenNodeScale(growthTween, part, duration);
		}

		if (growthTween == null)
		{
			growthTween = CreateGrowthTween(visualRoot);
			TweenNodeScale(growthTween, modelVisual, duration);
		}
	}

	private static Tween CreateGrowthTween(Node3D visualRoot)
	{
		return visualRoot.CreateTween()
			.SetParallel()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private static void TweenNodeScale(
		Tween tween,
		Node3D node,
		float duration)
	{
		Vector3 targetScale = node.Scale;
		node.Scale = new Vector3(
			targetScale.X * 0.55f,
			targetScale.Y * 0.08f,
			targetScale.Z * 0.55f);
		tween.TweenProperty(
			node,
			"scale",
			targetScale,
			duration);
	}

	private static Node3D CreateModelVisual(
		PlantInstance plant,
		int stage,
		HexCoord tileCoord)
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
		SetPartVisible(root, "Moss4", false);

		string[] partOrder = GetPartOrder(tileCoord);
		for (int index = 0; index < partOrder.Length; index++)
			SetPartVisible(root, partOrder[index], index < stage);

		return root;
	}

	private static string[] GetPartOrder(HexCoord tileCoord)
	{
		string[] partOrder = (string[])MossPartNames.Clone();
		for (int index = partOrder.Length - 1; index > 0; index--)
		{
			int swapIndex = Mathf.Min(
				Mathf.FloorToInt(
					GetTileRandom(tileCoord, 101u + (uint)index)
					* (index + 1)),
				index);
			(partOrder[index], partOrder[swapIndex]) =
				(partOrder[swapIndex], partOrder[index]);
		}

		return partOrder;
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
			GetSignedTileRandom(tileCoord, 31u) * 0.11f,
			-0.06f,
			GetSignedTileRandom(tileCoord, 37u) * 0.11f);
		modelVisual.RotationDegrees += new Vector3(
			0.0f,
			GetTileRandom(tileCoord, 41u) * 360.0f,
			0.0f);

		for (int index = 0; index < MossPartNames.Length; index++)
		{
			ApplyPartVariation(
				modelVisual,
				MossPartNames[index],
				tileCoord,
				61u + (uint)index * 17u);
		}
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

		int partIndex = Mathf.Max(
			System.Array.IndexOf(MossPartNames, nodeName),
			0);
		float arrangementAngle =
			GetTileRandom(tileCoord, 47u) * Mathf.Tau +
			partIndex * Mathf.Tau / MossPartNames.Length;
		float minimumScale = partIndex switch
		{
			0 => 1.34f,
			1 => 1.18f,
			_ => 1.02f
		};
		float maximumScale = partIndex switch
		{
			0 => 1.52f,
			1 => 1.36f,
			_ => 1.18f
		};
		float minimumPlacementRadius = partIndex switch
		{
			0 => 0.72f,
			1 => 0.58f,
			_ => 0.38f
		};
		float maximumPlacementRadius = partIndex switch
		{
			0 => 0.88f,
			1 => 0.74f,
			_ => 0.52f
		};
		float placementRadius = Mathf.Lerp(
			minimumPlacementRadius,
			maximumPlacementRadius,
			GetTileRandom(tileCoord, salt));
		Vector3 targetCenter = new Vector3(
			Mathf.Cos(arrangementAngle) * placementRadius,
			0.0f,
			Mathf.Sin(arrangementAngle) * placementRadius);

		part.Rotation += Vector3.Up * (
			arrangementAngle +
			Mathf.Pi * 0.5f +
			GetSignedTileRandom(tileCoord, salt + 1u) * 0.22f);
		float scale = Mathf.Lerp(
			minimumScale,
			maximumScale,
			GetTileRandom(tileCoord, salt + 2u));
		part.Scale *= Vector3.One * scale;

		float scaledHeight = 0.0f;
		Vector3 visualCenter = part.Position;
		if (part is GeometryInstance3D geometry)
		{
			Aabb bounds = geometry.GetAabb();
			Vector3 localCenter = bounds.Position + bounds.Size * 0.5f;
			visualCenter += part.Basis * localCenter;
			scaledHeight = bounds.Size.Y * Mathf.Abs(part.Scale.Y);
		}

		part.Position += new Vector3(
			targetCenter.X - visualCenter.X,
			0.0f,
			targetCenter.Z - visualCenter.Z);
		float burialDepth =
			Mathf.Max(0.0f, scaledHeight - 0.40f) * 1.20f +
			Mathf.InverseLerp(minimumScale, maximumScale, scale) * 0.08f;
		part.Position -= Vector3.Up * burialDepth;
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
