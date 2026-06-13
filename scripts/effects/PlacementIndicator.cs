using Godot;

public partial class PlacementIndicator : Node3D
{
	private MeshInstance3D _mesh;

	private StandardMaterial3D _validMaterial;
	private StandardMaterial3D _invalidMaterial;

	public override void _Ready()
	{
		_mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");

		if (_mesh == null)
		{
			_mesh = FindFirstMeshInstance(this);
		}

		if (_mesh == null)
		{
			GD.PrintErr("PlacementIndicator: No MeshInstance3D found. Creating debug mesh.");
			_mesh = CreateDebugMesh();
			AddChild(_mesh);
		}

		_validMaterial = CreateMaterial(new Color(0.0f, 1.0f, 0.25f, 0.85f));
		_invalidMaterial = CreateMaterial(new Color(1.0f, 0.0f, 0.2f, 0.85f));

		_mesh.Visible = true;
		_mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		_mesh.MaterialOverride = _validMaterial;

		Visible = false;

	}
	

	public void ShowAt(Vector3 worldPosition, bool isValid)
	{
		if (_mesh == null)
		{
			GD.PrintErr("PlacementIndicator: Cannot show because no MeshInstance3D was found.");
			return;
		}

		GlobalPosition = new Vector3(
			worldPosition.X,
			worldPosition.Y + 0.45f,
			worldPosition.Z
		);

		Visible = true;
		_mesh.Visible = true;
		_mesh.MaterialOverride = isValid ? _validMaterial : _invalidMaterial;

		GD.Print($"PlacementIndicator shown at {GlobalPosition}. Valid: {isValid}");
	}

	public void Hide()
	{
		Visible = false;
	}

	private MeshInstance3D CreateDebugMesh()
	{
		MeshInstance3D meshInstance = new MeshInstance3D();
		CylinderMesh mesh = new CylinderMesh();

		mesh.TopRadius = 0.55f;
		mesh.BottomRadius = 0.55f;
		mesh.Height = 0.04f;

		meshInstance.Name = "DebugPlacementMesh";
		meshInstance.Mesh = mesh;
		meshInstance.Position = Vector3.Zero;

		return meshInstance;
	}

	private MeshInstance3D FindFirstMeshInstance(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D meshInstance)
			{
				return meshInstance;
			}

			MeshInstance3D found = FindFirstMeshInstance(child);

			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private StandardMaterial3D CreateMaterial(Color color)
	{
		StandardMaterial3D material = new StandardMaterial3D();

		material.AlbedoColor = color;
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		material.EmissionEnabled = true;
		material.Emission = new Color(color.R, color.G, color.B);
		material.EmissionEnergyMultiplier = 2.5f;
		material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
		material.Roughness = 1.0f;
		material.Metallic = 0.0f;

		return material;
	}
}
