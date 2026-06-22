using Godot;
using System.Collections.Generic;

public static class MushroomEffectVisualBuilder
{
	public static Node3D Create(HexTile sourceTile, BoardManager boardManager)
	{
		Node3D root = new Node3D();
		root.Name = "MushroomEffectVisual";

		if (sourceTile == null || boardManager == null)
			return root;

		if (sourceTile.Data == null || sourceTile.Data.Plant == null)
			return root;

		if (sourceTile.Data.Plant.Definition.Type != PlantType.Mushroom)
			return root;

		if (!sourceTile.Data.Plant.IsMature)
			return root;

		List<HexTileData> neighbors = boardManager.GetNeighborData(sourceTile.Coord);

		foreach (HexTileData neighborData in neighbors)
		{
			if (neighborData == null)
				continue;

			if (neighborData.Plant == null)
				continue;

			HexTile neighborTile = boardManager.GetTileView(neighborData.Coord);

			if (neighborTile == null)
				continue;

			Node3D connection = CreateCurvedRootConnection(sourceTile, neighborTile);

			if (connection != null)
			{
				root.AddChild(connection);
			}
		}

		return root;
	}

	private static Node3D CreateCurvedRootConnection(HexTile sourceTile, HexTile targetTile)
	{
		Node3D connectionRoot = new Node3D();
		connectionRoot.Name = $"RootTo_{targetTile.Name}";

		Vector3 start = new Vector3(0.0f, 0.06f, 0.0f);
		Vector3 end = sourceTile.ToLocal(targetTile.GlobalPosition) + new Vector3(0.0f, 0.04f, 0.0f);

		Vector3 flatDirection = end - start;
		flatDirection.Y = 0.0f;

		float distance = flatDirection.Length();

		if (distance < 0.1f)
			return connectionRoot;

		Vector3 side = flatDirection.Cross(Vector3.Up).Normalized();

		if (side == Vector3.Zero)
			side = Vector3.Right;

		Vector3 control1 = start
			+ flatDirection * 0.28f
			+ Vector3.Up * 0.02f
			+ side * 0.18f;

		Vector3 control2 = start
			+ flatDirection * 0.72f
			+ Vector3.Up * 0.015f
			- side * 0.10f;

		StandardMaterial3D material = CreateRootMaterial();

		int segmentCount = 10;
		float startRadius = 0.09f;
		float endRadius = 0.015f;

		for (int i = 0; i < segmentCount; i++)
		{
			float t0 = (float)i / segmentCount;
			float t1 = (float)(i + 1) / segmentCount;

			Vector3 p0 = GetBezierPoint(start, control1, control2, end, t0);
			Vector3 p1 = GetBezierPoint(start, control1, control2, end, t1);

			float radius0 = Mathf.Lerp(startRadius, endRadius, t0);
			float radius1 = Mathf.Lerp(startRadius, endRadius, t1);

			MeshInstance3D segment = CreateRootSegment(p0, p1, radius0, radius1, material);

			if (segment != null)
			{
				connectionRoot.AddChild(segment);
			}
		}

		return connectionRoot;
	}

	private static Vector3 GetBezierPoint(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
	{
		float u = 1.0f - t;

		return
			u * u * u * a +
			3.0f * u * u * t * b +
			3.0f * u * t * t * c +
			t * t * t * d;
	}

	private static MeshInstance3D CreateRootSegment(
		Vector3 start,
		Vector3 end,
		float bottomRadius,
		float topRadius,
		StandardMaterial3D material
	)
	{
		Vector3 direction = end - start;
		float length = direction.Length();

		if (length < 0.001f)
			return null;

		CylinderMesh mesh = new CylinderMesh();
		mesh.BottomRadius = bottomRadius;
		mesh.TopRadius = topRadius;
		mesh.Height = length;
		mesh.RadialSegments = 10;
		mesh.Rings = 1;

		MeshInstance3D meshInstance = new MeshInstance3D();
		meshInstance.Mesh = mesh;
		meshInstance.MaterialOverride = material;

		Vector3 midpoint = (start + end) * 0.5f;
		Transform3D transform = CreateTransformAlignedToDirection(midpoint, direction.Normalized());

		meshInstance.Transform = transform;

		return meshInstance;
	}

	private static Transform3D CreateTransformAlignedToDirection(Vector3 position, Vector3 direction)
	{
		Vector3 from = Vector3.Up;
		Vector3 to = direction;

		float dot = Mathf.Clamp(from.Dot(to), -1.0f, 1.0f);
		Basis basis;

		if (dot > 0.9999f)
		{
			basis = Basis.Identity;
		}
		else if (dot < -0.9999f)
		{
			basis = new Basis(Vector3.Right, Mathf.Pi);
		}
		else
		{
			Vector3 axis = from.Cross(to).Normalized();
			float angle = Mathf.Acos(dot);
			basis = new Basis(new Quaternion(axis, angle));
		}

		return new Transform3D(basis, position);
	}

	private static StandardMaterial3D CreateRootMaterial()
	{
		StandardMaterial3D material = new StandardMaterial3D();

		material.AlbedoColor = new Color(0.67f, 0.82f, 0.58f, 0.52f);
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.Roughness = 1.0f;
		material.Metallic = 0.0f;
		material.EmissionEnabled = true;
		material.Emission = new Color(0.48f, 0.74f, 0.40f);
		material.EmissionEnergyMultiplier = 0.25f;
		material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;

		return material;
	}
}
