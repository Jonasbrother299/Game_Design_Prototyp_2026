using System.Collections.Generic;
using Godot;

public static class MushroomVisualBuilder
{
	private const int StonePlacementCandidateCount = 48;
	private const float StonePlacementMaxRadius = 0.58f;
	private const float StoneClearancePadding = 0.025f;
	private const float GoldenAngle = 2.3999632f;

	private static readonly Vector3[] ClusterOffsets =
	{
		Vector3.Zero,
		new Vector3(0.28f, 0.0f, 0.12f),
		new Vector3(-0.25f, 0.0f, 0.16f),
		new Vector3(0.06f, 0.0f, -0.28f)
	};

	private static readonly float[] ClusterRotations =
	{
		0.0f,
		120.0f,
		240.0f,
		45.0f
	};

	private static readonly float[] ClusterScaleMultipliers =
	{
		1.0f,
		0.82f,
		0.92f,
		0.76f
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

		AddMushroomModels(cluster, plant, modelScale, tileCoord);
		PlaceClusterClearOfStones(cluster, tile, tileCoord);

		return root;
	}

	private static void PlaceClusterClearOfStones(
		Node3D cluster,
		HexTile tile,
		HexCoord tileCoord)
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
			return;

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

		Vector2 preferredPosition = new Vector2(
			cluster.Position.X,
			cluster.Position.Z);
		List<Vector2> candidates = BuildStonePlacementCandidates(
			tileCoord,
			preferredPosition);

		if (tile.TryFindStoneFreeClusterPosition(
			candidates,
			footprintCenters,
			footprintRadii,
			out Vector2 clearPosition))
		{
			cluster.Position = new Vector3(
				clearPosition.X,
				cluster.Position.Y,
				clearPosition.Y);
			return;
		}

		cluster.Visible = false;
		GD.PushWarning(
			$"{tile.Name}: Pilze konnten nicht außerhalb der Steine platziert werden.");
	}

	private static List<Vector2> BuildStonePlacementCandidates(
		HexCoord tileCoord,
		Vector2 preferredPosition)
	{
		List<Vector2> candidates = new(StonePlacementCandidateCount + 1)
		{
			preferredPosition
		};
		float angleOffset = GetTileRandom(tileCoord, 31u) * Mathf.Tau;

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

	private static void AddMushroomModels(
		Node3D root,
		PlantInstance plant,
		float modelScale,
		HexCoord tileCoord)
	{
		PackedScene mushroomScene = plant?.Definition?.PlantScene;
		if (mushroomScene == null)
			return;

		int visibleModelCount = Mathf.Clamp(
			plant?.VisualGrowthStage ?? 1,
			1,
			ClusterOffsets.Length);
		float safeModelScale = Mathf.Max(0.1f, modelScale);

		for (int i = 0; i < visibleModelCount; i++)
		{
			Node instance = mushroomScene.Instantiate();
			if (instance is not Node3D mushroomModel)
			{
				instance?.Free();
				continue;
			}

			uint slotSalt = (uint)(i + 1) * 101u;
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
