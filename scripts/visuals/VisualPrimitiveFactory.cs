using Godot;

public static class VisualPrimitiveFactory
{
	public static MeshInstance3D CreateCylinder(
		Vector3 position,
		float topRadius,
		float bottomRadius,
		float height,
		Color color
	)
	{
		CylinderMesh mesh = new CylinderMesh();
		mesh.TopRadius = topRadius;
		mesh.BottomRadius = bottomRadius;
		mesh.Height = height;
		mesh.RadialSegments = 12;

		MeshInstance3D meshInstance = new MeshInstance3D();
		meshInstance.Mesh = mesh;
		meshInstance.Position = position;
		meshInstance.MaterialOverride = CreateMaterial(color);

		return meshInstance;
	}

	public static MeshInstance3D CreateSphere(
		Vector3 position,
		float radius,
		Color color,
		Vector3? scaleOverride = null
	)
	{
		SphereMesh mesh = new SphereMesh();
		mesh.Radius = radius;
		mesh.Height = radius * 2.0f;
		mesh.RadialSegments = 12;
		mesh.Rings = 6;

		MeshInstance3D meshInstance = new MeshInstance3D();
		meshInstance.Mesh = mesh;
		meshInstance.Position = position;
		meshInstance.MaterialOverride = CreateMaterial(color);

		if (scaleOverride.HasValue)
		{
			meshInstance.Scale = scaleOverride.Value;
		}

		return meshInstance;
	}

	public static StandardMaterial3D CreateMaterial(Color color)
	{
		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = color;
		material.Roughness = 0.9f;

		return material;
	}
}
