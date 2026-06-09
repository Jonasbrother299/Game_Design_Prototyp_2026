using Godot;

public partial class HexTile : Node3D
{
	public HexTileData Data { get; private set; }

	public HexCoord Coord => Data.Coord;

	private MeshInstance3D _tileMesh;
	private Node3D _plantAnchor;
	private Node3D _plantVisualRoot;

	public void Setup(HexTileData data)
	{
		Data = data;
		Name = $"HexTile_{data.Coord.Q}_{data.Coord.R}";

		_tileMesh = GetNode<MeshInstance3D>("TileMesh");
		_plantAnchor = GetNode<Node3D>("PlantAnchor");

		UpdateVisualState();
	}

	public bool CanPlacePlant(PlantDefinition plantDefinition)
	{
		if (Data == null)
			return false;

		return Data.CanPlacePlant(plantDefinition);
	}

	public void PlacePlant(PlantInstance plant)
	{
		if (Data == null)
			return;

		Data.PlacePlant(plant);
		UpdateVisualState();

		GD.Print($"Plant placed: {plant.Definition.DisplayName} on {Coord}");
	}

	public void UpdateVisualState()
	{
		if (Data == null)
			return;

		UpdateTileMaterial();
		RebuildPlantVisual();

		if (Data.Plant != null)
		{
			GD.Print($"{Name} | Light: {Data.LightLevel} | Plant: {Data.Plant.Definition.DisplayName}");
		}
	}

	private void UpdateTileMaterial()
	{
		if (_tileMesh == null)
			return;

		StandardMaterial3D material = _tileMesh.MaterialOverride as StandardMaterial3D;

		if (material == null)
		{
			material = new StandardMaterial3D();
			_tileMesh.MaterialOverride = material;
		}

		material.Roughness = 1.0f;
		material.Metallic = 0.0f;

		switch (Data.LightLevel)
		{
			case LightLevel.Sun:
				material.AlbedoColor = new Color("9fbe8c");
				break;

			case LightLevel.PartialShade:
				material.AlbedoColor = new Color("7fa07a");
				break;

			case LightLevel.Shade:
				material.AlbedoColor = new Color("5f7c72");
				break;
		}

		if (Data.IsBlocked)
		{
			material.AlbedoColor = new Color("6a5a5a");
		}
	}

	private void RebuildPlantVisual()
	{
		if (_plantVisualRoot != null)
		{
			_plantVisualRoot.QueueFree();
			_plantVisualRoot = null;
		}

		if (Data.Plant == null || _plantAnchor == null)
			return;

		_plantVisualRoot = CreatePlantVisual(Data.Plant);
		_plantAnchor.AddChild(_plantVisualRoot);
	}

	private Node3D CreatePlantVisual(PlantInstance plant)
	{
		Node3D root = new Node3D();

		switch (plant.Definition.Type)
		{
			case PlantType.Oak:
				root.AddChild(CreateCylinder(
					new Vector3(0, 0.35f, 0),
					0.12f,
					0.16f,
					0.7f,
					new Color("6b4f2d")
				));
				root.AddChild(CreateSphere(
					new Vector3(0, 0.9f, 0),
					0.35f,
					new Color("4d7f45")
				));
				break;

			case PlantType.Moss:
				root.AddChild(CreateSphere(
					new Vector3(0, 0.08f, 0),
					0.22f,
					new Color("5a8f45"),
					new Vector3(1.4f, 0.35f, 1.4f)
				));
				break;

			case PlantType.Flower:
				root.AddChild(CreateCylinder(
					new Vector3(0, 0.25f, 0),
					0.03f,
					0.03f,
					0.5f,
					new Color("4b7d3b")
				));
				root.AddChild(CreateSphere(
					new Vector3(0, 0.55f, 0),
					0.12f,
					new Color("d9c14a")
				));
				break;

			case PlantType.Mushroom:
				root.AddChild(CreateCylinder(
					new Vector3(0, 0.12f, 0),
					0.04f,
					0.05f,
					0.24f,
					new Color("d8c7aa")
				));
				root.AddChild(CreateSphere(
					new Vector3(0, 0.28f, 0),
					0.16f,
					new Color("9a5c47"),
					new Vector3(1.0f, 0.6f, 1.0f)
				));
				break;

			case PlantType.Birch:
				root.AddChild(CreateCylinder(
					new Vector3(0, 0.45f, 0),
					0.08f,
					0.1f,
					0.9f,
					new Color("d7d2c8")
				));
				root.AddChild(CreateSphere(
					new Vector3(0, 1.0f, 0),
					0.28f,
					new Color("82a85f")
				));
				break;
		}

		if (!plant.IsMature)
		{
			root.Scale = new Vector3(0.65f, 0.65f, 0.65f);
		}

		return root;
	}

	private MeshInstance3D CreateCylinder(
		Vector3 position,
		float topRadius,
		float bottomRadius,
		float height,
		Color color
	)
	{
		MeshInstance3D meshInstance = new MeshInstance3D();
		CylinderMesh mesh = new CylinderMesh();

		mesh.TopRadius = topRadius;
		mesh.BottomRadius = bottomRadius;
		mesh.Height = height;

		meshInstance.Mesh = mesh;
		meshInstance.Position = position;
		meshInstance.MaterialOverride = CreateMaterial(color);

		return meshInstance;
	}

	private MeshInstance3D CreateSphere(
		Vector3 position,
		float radius,
		Color color,
		Vector3? scaleOverride = null
	)
	{
		MeshInstance3D meshInstance = new MeshInstance3D();
		SphereMesh mesh = new SphereMesh();

		mesh.Radius = radius;
		mesh.Height = radius * 2.0f;

		meshInstance.Mesh = mesh;
		meshInstance.Position = position;
		meshInstance.MaterialOverride = CreateMaterial(color);

		if (scaleOverride.HasValue)
		{
			meshInstance.Scale = scaleOverride.Value;
		}

		return meshInstance;
	}

	private StandardMaterial3D CreateMaterial(Color color)
	{
		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = color;
		material.Roughness = 1.0f;
		material.Metallic = 0.0f;
		return material;
	}
}
