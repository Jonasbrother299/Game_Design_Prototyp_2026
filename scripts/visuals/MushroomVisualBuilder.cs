using System.Collections.Generic;
using Godot;

public static class MushroomVisualBuilder
{
	private const int StonePlacementCandidateCount = 48;
	private const float StonePlacementMaxRadius = 0.58f;
	private const float StoneClearancePadding = 0.025f;
	private const float GoldenAngle = 2.3999632f;
	private const int MinimumMatureModelCount = 4;
	private const int MinimumNeighborModelCount = 2;
	private const float NeighborClusterGroundHeight = 0.11f;

	private static readonly Vector3[] ClusterOffsets =
	{
		Vector3.Zero,
		new Vector3(0.28f, 0.0f, 0.12f),
		new Vector3(-0.25f, 0.0f, 0.16f),
		new Vector3(0.06f, 0.0f, -0.28f),
		new Vector3(-0.28f, 0.0f, -0.16f),
		new Vector3(0.30f, 0.0f, -0.15f)
	};

	private static readonly float[] ClusterRotations =
	{
		0.0f,
		120.0f,
		240.0f,
		45.0f,
		190.0f,
		315.0f
	};

	private static readonly float[] ClusterScaleMultipliers =
	{
		1.0f,
		0.82f,
		0.92f,
		0.76f,
		0.86f,
		0.72f
	};

	public static Node3D Create(
		PlantInstance plant,
		float modelScale,
		float animationSpeed,
		bool animateGrowth,
		HexTile tile)
	{
		HexCoord tileCoord = tile.Coord;
		Node3D root = new Node3D
		{
			Name = "Mushroom_Visual"
		};
		Node3D cluster = new Node3D
		{
			Name = "MushroomCluster",
			Position = new Vector3(
				GetSignedTileRandom(tileCoord, 11u) * 0.12f,
				-0.01f,
				GetSignedTileRandom(tileCoord, 17u) * 0.12f),
			RotationDegrees = new Vector3(
				0.0f,
				GetTileRandom(tileCoord, 23u) * 360.0f,
				0.0f)
		};
		root.AddChild(cluster);

		int matureModelCount = GetMatureModelCount(tileCoord);
		int visibleModelCount = GetVisibleModelCount(
			plant,
			matureModelCount);
		List<Node3D> mushroomModels = AddMushroomModels(
			cluster,
			plant?.Definition?.PlantScene,
			modelScale,
			tileCoord,
			matureModelCount,
			0u);
		Vector2 preferredPosition = new Vector2(
			cluster.Position.X,
			cluster.Position.Z);

		if (!PlaceClusterClearOfBlockers(
			cluster,
			tile,
			tileCoord,
			preferredPosition,
			31u,
			avoidPlantVisuals: false))
		{
			cluster.Visible = false;
			GD.PushWarning(
				$"{tile.Name}: Pilze konnten nicht außerhalb der Steine platziert werden.");
		}

		RemoveModelsAfter(mushroomModels, visibleModelCount);

		return root;
	}

	internal static void AnimateGrowth(
		Node3D visualRoot,
		PlantInstance plant,
		HexCoord tileCoord,
		int previousStage,
		int currentStage,
		float duration)
	{
		Node3D cluster =
			visualRoot?.GetNodeOrNull<Node3D>("MushroomCluster");
		if (cluster == null || plant == null || duration <= 0.0f)
			return;

		int stageCount = Mathf.Max(
			plant.Definition?.GrowthStageCount ?? 2,
			2);
		int matureModelCount = GetMatureModelCount(tileCoord);
		int startIndex = previousStage <= 0
			? 0
			: GetVisibleModelCountForStage(
				previousStage,
				stageCount,
				matureModelCount);
		int endIndex = GetVisibleModelCountForStage(
			currentStage,
			stageCount,
			matureModelCount);
		Tween growthTween = null;

		for (int index = startIndex; index < endIndex; index++)
		{
			Node3D mushroomModel = cluster.GetNodeOrNull<Node3D>(
				$"MushroomModel_{index + 1}");
			if (mushroomModel == null)
				continue;

			growthTween ??= CreateGrowthTween(visualRoot);
			TweenNodeScale(growthTween, mushroomModel, duration);
		}
	}

	internal static void AnimateNeighborGrowth(
		Node3D visualRoot,
		float duration)
	{
		Node3D cluster = visualRoot?.GetNodeOrNull<Node3D>(
			"MushroomNeighborCluster");
		if (cluster == null || duration <= 0.0f)
			return;

		Tween growthTween = null;

		foreach (Node child in cluster.GetChildren())
		{
			if (child is not Node3D mushroomModel)
				continue;

			growthTween ??= CreateGrowthTween(visualRoot);
			TweenNodeScale(growthTween, mushroomModel, duration);
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

	public static Node3D CreateNeighborDecoration(
		PlantInstance mushroom,
		float modelScale,
		HexCoord sourceCoord,
		HexTile tile)
	{
		PackedScene mushroomScene = mushroom?.Definition?.PlantScene;
		if (mushroomScene == null || tile == null)
			return null;

		HexCoord targetCoord = tile.Coord;
		uint pairSalt = GetCoordHash(sourceCoord, 211u);
		int modelCount = GetTileRandom(targetCoord, pairSalt) < 0.5f ? 2 : 3;
		Vector2 sourceDirection = GetDirectionToSource(targetCoord, sourceCoord);
		Vector2 perpendicular = new Vector2(
			-sourceDirection.Y,
			sourceDirection.X);
		Vector2 preferredPosition =
			sourceDirection * 0.42f +
			perpendicular *
			GetSignedTileRandom(targetCoord, pairSalt + 1u) * 0.12f;
		Node3D root = new Node3D
		{
			Name = "MushroomNeighborVisual"
		};
		Node3D cluster = new Node3D
		{
			Name = "MushroomNeighborCluster",
			Position = new Vector3(
				0.0f,
				NeighborClusterGroundHeight,
				0.0f),
			RotationDegrees = new Vector3(
				0.0f,
				GetTileRandom(targetCoord, pairSalt + 2u) * 360.0f,
				0.0f)
		};
		root.AddChild(cluster);

		List<Node3D> mushroomModels = AddMushroomModels(
			cluster,
			mushroomScene,
			modelScale * 0.85f,
			targetCoord,
			modelCount,
			pairSalt + 10u);

		bool wasPlaced =
			mushroomModels.Count >= MinimumNeighborModelCount &&
			PlaceNeighborModelsIndividually(
				cluster,
				mushroomModels,
				tile,
				targetCoord,
				preferredPosition,
				pairSalt + 40u);

		if (!wasPlaced)
		{
			root.Free();
			return null;
		}

		return root;
	}

	private static bool PlaceNeighborModelsIndividually(
		Node3D cluster,
		IReadOnlyList<Node3D> mushroomModels,
		HexTile tile,
		HexCoord tileCoord,
		Vector2 preferredPosition,
		uint candidateSalt)
	{
		cluster.Position = new Vector3(
			0.0f,
			cluster.Position.Y,
			0.0f);
		cluster.Rotation = Vector3.Zero;

		List<Vector2> occupiedCenters = new();
		List<float> occupiedRadii = new();
		List<Node3D> unplacedModels = new();
		int placedModelCount = 0;

		for (int modelIndex = 0;
			modelIndex < mushroomModels.Count;
			modelIndex++)
		{
			Node3D model = mushroomModels[modelIndex];
			Vector3 originalPosition = model.Position;
			model.Position = new Vector3(
				0.0f,
				originalPosition.Y,
				0.0f);

			List<Vector2> footprintCenters = new();
			List<float> footprintRadii = new();
			CollectCollisionFootprints(
				model,
				Transform3D.Identity,
				footprintCenters,
				footprintRadii);

			if (footprintCenters.Count == 0)
			{
				unplacedModels.Add(model);
				continue;
			}

			for (int footprintIndex = 0;
				footprintIndex < footprintRadii.Count;
				footprintIndex++)
			{
				footprintRadii[footprintIndex] += StoneClearancePadding;
			}

			Vector2 modelPreferredPosition = preferredPosition +
				new Vector2(originalPosition.X, originalPosition.Z) * 0.5f;
			List<Vector2> candidates = BuildPlacementCandidates(
				tileCoord,
				modelPreferredPosition,
				candidateSalt + (uint)modelIndex * 17u);
			List<Vector2> clearCandidates = FilterCandidatesClearOfFootprints(
				candidates,
				footprintCenters,
				footprintRadii,
				occupiedCenters,
				occupiedRadii);

			if (!tile.TryFindMushroomClusterPosition(
				clearCandidates,
				footprintCenters,
				footprintRadii,
				avoidPlantVisuals: true,
				out Vector2 clearPosition))
			{
				unplacedModels.Add(model);
				continue;
			}

			model.Position = new Vector3(
				clearPosition.X,
				originalPosition.Y,
				clearPosition.Y);
			placedModelCount++;

			for (int footprintIndex = 0;
				footprintIndex < footprintCenters.Count;
				footprintIndex++)
			{
				occupiedCenters.Add(
					clearPosition + footprintCenters[footprintIndex]);
				occupiedRadii.Add(footprintRadii[footprintIndex]);
			}
		}

		if (placedModelCount < MinimumNeighborModelCount)
			return false;

		foreach (Node3D model in unplacedModels)
			model.Free();

		return true;
	}

	private static List<Vector2> FilterCandidatesClearOfFootprints(
		IReadOnlyList<Vector2> candidates,
		IReadOnlyList<Vector2> footprintCenters,
		IReadOnlyList<float> footprintRadii,
		IReadOnlyList<Vector2> occupiedCenters,
		IReadOnlyList<float> occupiedRadii)
	{
		List<Vector2> clearCandidates = new(candidates.Count);

		foreach (Vector2 candidate in candidates)
		{
			bool overlapsPlacedModel = false;

			for (int footprintIndex = 0;
				footprintIndex < footprintCenters.Count &&
				!overlapsPlacedModel;
				footprintIndex++)
			{
				Vector2 footprintPosition =
					candidate + footprintCenters[footprintIndex];

				for (int occupiedIndex = 0;
					occupiedIndex < occupiedCenters.Count;
					occupiedIndex++)
				{
					float minimumDistance =
						footprintRadii[footprintIndex] +
						occupiedRadii[occupiedIndex];
					if (footprintPosition.DistanceSquaredTo(
						occupiedCenters[occupiedIndex]) >=
						minimumDistance * minimumDistance)
					{
						continue;
					}

					overlapsPlacedModel = true;
					break;
				}
			}

			if (!overlapsPlacedModel)
				clearCandidates.Add(candidate);
		}

		return clearCandidates;
	}

	private static bool PlaceClusterClearOfBlockers(
		Node3D cluster,
		HexTile tile,
		HexCoord tileCoord,
		Vector2 preferredPosition,
		uint candidateSalt,
		bool avoidPlantVisuals)
	{
		List<Vector2> footprintCenters = new();
		List<float> footprintRadii = new();

		foreach (Node child in cluster.GetChildren())
		{
			CollectCollisionFootprints(
				child,
				Transform3D.Identity,
				footprintCenters,
				footprintRadii);
		}

		if (footprintCenters.Count == 0)
			return false;

		for (int index = 0; index < footprintCenters.Count; index++)
		{
			Vector3 rotatedCenter = cluster.Transform.Basis * new Vector3(
				footprintCenters[index].X,
				0.0f,
				footprintCenters[index].Y);
			footprintCenters[index] = new Vector2(
				rotatedCenter.X,
				rotatedCenter.Z);
			footprintRadii[index] += StoneClearancePadding;
		}

		List<Vector2> candidates = BuildPlacementCandidates(
			tileCoord,
			preferredPosition,
			candidateSalt);

		if (tile.TryFindMushroomClusterPosition(
			candidates,
			footprintCenters,
			footprintRadii,
			avoidPlantVisuals,
			out Vector2 clearPosition))
		{
			cluster.Position = new Vector3(
				clearPosition.X,
				cluster.Position.Y,
				clearPosition.Y);
			return true;
		}

		return false;
	}

	private static List<Vector2> BuildPlacementCandidates(
		HexCoord tileCoord,
		Vector2 preferredPosition,
		uint candidateSalt)
	{
		List<Vector2> candidates = new(StonePlacementCandidateCount + 1)
		{
			preferredPosition
		};
		float angleOffset = GetTileRandom(tileCoord, candidateSalt) * Mathf.Tau;

		for (int index = 0; index < StonePlacementCandidateCount; index++)
		{
			float progress = (index + 1.0f) / StonePlacementCandidateCount;
			float radius = Mathf.Sqrt(progress) * StonePlacementMaxRadius;
			float angle = angleOffset + index * GoldenAngle;
			candidates.Add(new Vector2(
				Mathf.Cos(angle),
				Mathf.Sin(angle)) * radius);
		}

		return candidates;
	}

	private static void CollectCollisionFootprints(
		Node node,
		Transform3D parentToCluster,
		List<Vector2> centers,
		List<float> radii)
	{
		Transform3D nodeToCluster = node is Node3D node3D
			? parentToCluster * node3D.Transform
			: parentToCluster;

		if (node is CollisionShape3D collisionShape &&
			!collisionShape.Disabled &&
			collisionShape.Shape is ConcavePolygonShape3D concaveShape)
		{
			AddCollisionFootprint(
				concaveShape.GetFaces(),
				nodeToCluster,
				centers,
				radii);
		}

		foreach (Node child in node.GetChildren())
		{
			CollectCollisionFootprints(
				child,
				nodeToCluster,
				centers,
				radii);
		}
	}

	private static void AddCollisionFootprint(
		Vector3[] faces,
		Transform3D collisionToCluster,
		List<Vector2> centers,
		List<float> radii)
	{
		if (faces == null || faces.Length == 0)
			return;

		float minX = float.PositiveInfinity;
		float maxX = float.NegativeInfinity;
		float minZ = float.PositiveInfinity;
		float maxZ = float.NegativeInfinity;

		for (int index = 0; index < faces.Length; index++)
		{
			Vector3 vertex = collisionToCluster * faces[index];
			Vector2 projectedVertex = new Vector2(vertex.X, vertex.Z);
			minX = Mathf.Min(minX, projectedVertex.X);
			maxX = Mathf.Max(maxX, projectedVertex.X);
			minZ = Mathf.Min(minZ, projectedVertex.Y);
			maxZ = Mathf.Max(maxZ, projectedVertex.Y);
		}

		Vector2 center = new Vector2(
			(minX + maxX) * 0.5f,
			(minZ + maxZ) * 0.5f);
		float radiusSquared = 0.0f;

		foreach (Vector3 face in faces)
		{
			Vector3 vertex = collisionToCluster * face;
			radiusSquared = Mathf.Max(
				radiusSquared,
				center.DistanceSquaredTo(new Vector2(vertex.X, vertex.Z)));
		}

		centers.Add(center);
		radii.Add(Mathf.Sqrt(radiusSquared));
	}

	private static List<Node3D> AddMushroomModels(
		Node3D root,
		PackedScene mushroomScene,
		float modelScale,
		HexCoord tileCoord,
		int modelCount,
		uint variationSalt)
	{
		List<Node3D> mushroomModels = new();
		if (mushroomScene == null)
			return mushroomModels;

		int safeModelCount = Mathf.Clamp(
			modelCount,
			1,
			ClusterOffsets.Length);
		float safeModelScale = Mathf.Max(0.1f, modelScale);

		for (int i = 0; i < safeModelCount; i++)
		{
			Node instance = mushroomScene.Instantiate();
			if (instance is not Node3D mushroomModel)
			{
				instance?.Free();
				continue;
			}

			uint slotSalt = variationSalt + (uint)(i + 1) * 101u;
			Vector3 positionJitter = new Vector3(
				GetSignedTileRandom(tileCoord, slotSalt + 1u) * 0.035f,
				0.0f,
				GetSignedTileRandom(tileCoord, slotSalt + 2u) * 0.035f);
			float scaleVariation = Mathf.Lerp(
				0.92f,
				1.08f,
				GetTileRandom(tileCoord, slotSalt + 3u));

			mushroomModel.Name = $"MushroomModel_{i + 1}";
			mushroomModel.Position = ClusterOffsets[i] + positionJitter;
			mushroomModel.RotationDegrees =
				new Vector3(
					0.0f,
					ClusterRotations[i] +
					GetSignedTileRandom(tileCoord, slotSalt + 4u) * 28.0f,
					0.0f);
			mushroomModel.Scale *=
				safeModelScale *
				ClusterScaleMultipliers[i] *
				scaleVariation;
			root.AddChild(mushroomModel);
			mushroomModels.Add(mushroomModel);
		}

		return mushroomModels;
	}

	private static int GetMatureModelCount(HexCoord tileCoord)
	{
		int variationCount =
			ClusterOffsets.Length - MinimumMatureModelCount + 1;
		int variation = Mathf.Min(
			Mathf.FloorToInt(
				GetTileRandom(tileCoord, 307u) * variationCount),
			variationCount - 1);
		return MinimumMatureModelCount + variation;
	}

	private static int GetVisibleModelCount(
		PlantInstance plant,
		int matureModelCount)
	{
		int stageCount = Mathf.Max(plant?.Definition?.GrowthStageCount ?? 2, 2);
		return GetVisibleModelCountForStage(
			plant?.VisualGrowthStage ?? 1,
			stageCount,
			matureModelCount);
	}

	private static int GetVisibleModelCountForStage(
		int growthStage,
		int stageCount,
		int matureModelCount)
	{
		int safeStageCount = Mathf.Max(stageCount, 2);
		int safeGrowthStage = Mathf.Clamp(
			growthStage,
			1,
			safeStageCount);
		return Mathf.Clamp(
			Mathf.CeilToInt(
				matureModelCount *
				safeGrowthStage /
				(float)safeStageCount),
			1,
			matureModelCount);
	}

	private static void RemoveModelsAfter(
		List<Node3D> models,
		int visibleModelCount)
	{
		for (int index = models.Count - 1;
			index >= visibleModelCount;
			index--)
		{
			Node3D model = models[index];
			model.GetParent()?.RemoveChild(model);
			model.Free();
		}
	}

	private static Vector2 GetDirectionToSource(
		HexCoord targetCoord,
		HexCoord sourceCoord)
	{
		int deltaQ = sourceCoord.Q - targetCoord.Q;
		int deltaR = sourceCoord.R - targetCoord.R;
		Vector2 direction = new Vector2(
			1.5f * deltaQ,
			Mathf.Sqrt(3.0f) * (deltaR + deltaQ / 2.0f));
		return direction.LengthSquared() > 0.0f
			? direction.Normalized()
			: Vector2.Right;
	}

	private static uint GetCoordHash(HexCoord coord, uint salt)
	{
		unchecked
		{
			uint value = (uint)coord.Q * 0x9E3779B9u;
			value ^= (uint)coord.R * 0x85EBCA6Bu;
			value ^= salt * 0xC2B2AE35u;
			return value;
		}
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
