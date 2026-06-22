using Godot;

public partial class HexTile : Node3D
{
	public HexTileData Data { get; private set; }

	public HexCoord Coord => Data.Coord;

	private static Shader _placementPreviewShader;

	private MeshInstance3D _tileMesh;

	private Node3D _plantAnchor;
	private Node3D _plantVisualRoot;
	
	private Node3D _effectVisualRoot;
	private Node3D _placementIndicatorRoot;
	private MeshInstance3D _placementIndicatorMesh;

	private StandardMaterial3D _tileMaterial;
	private Material _validPreviewMaterial;
	private Material _invalidPreviewMaterial;

	public void Setup(HexTileData data)
	{
		Data = data;
		Name = $"HexTile_{data.Coord.Q}_{data.Coord.R}";

		_tileMesh = GetNodeOrNull<MeshInstance3D>("hex_tile/MeshInstance3D");

		if (_tileMesh == null)
		{
			_tileMesh = GetNodeOrNull<MeshInstance3D>("TileMesh");
		}

		_plantAnchor = GetNodeOrNull<Node3D>("PlantAnchor");

		if (_plantAnchor == null)
		{
			GD.PrintErr($"{Name}: PlantAnchor not found. Creating fallback PlantAnchor.");
			_plantAnchor = new Node3D();
			_plantAnchor.Name = "PlantAnchor";
			AddChild(_plantAnchor);
		}

		SetupPlacementIndicator();
		SetupUniqueTileMaterial();
		EnsureCollision();
		UpdateVisualState();
	}

	private void SetupPlacementIndicator()
	{
		_validPreviewMaterial = CreatePlacementIndicatorMaterial(true);
		_invalidPreviewMaterial = CreatePlacementIndicatorMaterial(false);

		_placementIndicatorRoot = GetNodeOrNull<Node3D>("HandCardPlacementIndicator");

		if (_placementIndicatorRoot == null)
		{
			_placementIndicatorRoot = GetNodeOrNull<Node3D>("HandCardPlacmentIndicator");
		}

		if (_placementIndicatorRoot == null)
		{
			_placementIndicatorRoot = FindNodeByNamePart(this, "placement");
		}

		if (_placementIndicatorRoot == null)
		{
			_placementIndicatorRoot = FindNodeByNamePart(this, "placment");
		}

		if (_placementIndicatorRoot == null)
		{
			GD.PrintErr($"{Name}: No placement indicator found. Creating fallback indicator.");
			_placementIndicatorRoot = CreateFallbackPlacementIndicatorRoot();
			AddChild(_placementIndicatorRoot);
		}

		_placementIndicatorRoot.Visible = true;

		_placementIndicatorMesh = FindFirstMeshInstance(_placementIndicatorRoot);

		if (_placementIndicatorMesh == null)
		{
			GD.PrintErr($"{Name}: Placement indicator root found, but no MeshInstance3D inside. Creating fallback mesh.");
			_placementIndicatorMesh = CreateFallbackPlacementIndicatorMesh();
			_placementIndicatorRoot.AddChild(_placementIndicatorMesh);
		}

		_placementIndicatorMesh.Visible = false;
		_placementIndicatorMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		_placementIndicatorMesh.MaterialOverride = _validPreviewMaterial;
	}

	private Node3D CreateFallbackPlacementIndicatorRoot()
	{
		Node3D root = new Node3D();

		root.Name = "HandCardPlacementIndicator";
		root.Position = new Vector3(0.0f, 0.35f, 0.0f);
		root.RotationDegrees = new Vector3(0.0f, 30.0f, 0.0f);
		root.Scale = Vector3.One;

		return root;
	}

	private MeshInstance3D CreateFallbackPlacementIndicatorMesh()
	{
		MeshInstance3D meshInstance = new MeshInstance3D();
		CylinderMesh mesh = new CylinderMesh();

		mesh.TopRadius = 0.55f;
		mesh.BottomRadius = 0.55f;
		mesh.Height = 0.45f;

		meshInstance.Name = "PlacementDebugMesh";
		meshInstance.Mesh = mesh;
		meshInstance.Position = Vector3.Zero;
		meshInstance.MaterialOverride = _validPreviewMaterial;

		return meshInstance;
	}

	private Node3D FindNodeByNamePart(Node node, string namePart)
	{
		string search = namePart.ToLowerInvariant();

		foreach (Node child in node.GetChildren())
		{
			string childName = child.Name.ToString().ToLowerInvariant();

			if (child is Node3D childNode3D && childName.Contains(search))
			{
				return childNode3D;
			}

			Node3D found = FindNodeByNamePart(child, namePart);

			if (found != null)
			{
				return found;
			}
		}

		return null;
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

	private void SetupUniqueTileMaterial()
	{
		if (_tileMesh == null)
			return;

		_tileMaterial = new StandardMaterial3D();

		_tileMaterial.AlbedoColor = new Color("d8dbd5");
		_tileMaterial.Roughness = 1.0f;
		_tileMaterial.Metallic = 0.0f;
		_tileMaterial.EmissionEnabled = false;

		_tileMesh.MaterialOverride = _tileMaterial;
	}

	private void EnsureCollision()
	{
		StaticBody3D body = GetNodeOrNull<StaticBody3D>("StaticBody3D");

		if (body == null)
		{
			body = GetNodeOrNull<StaticBody3D>("TileCollisionBody");
		}

		if (body == null)
		{
			body = new StaticBody3D();
			body.Name = "StaticBody3D";
			AddChild(body);
		}

		body.CollisionLayer = 1;
		body.CollisionMask = 1;

		CollisionShape3D collisionShape = body.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

		if (collisionShape == null)
		{
			collisionShape = new CollisionShape3D();
			collisionShape.Name = "CollisionShape3D";
			body.AddChild(collisionShape);
		}

		CylinderShape3D shape = new CylinderShape3D();
		shape.Radius = 1.05f;
		shape.Height = 0.55f;

		collisionShape.Shape = shape;
		collisionShape.Disabled = false;
		collisionShape.Position = new Vector3(0.0f, 0.2f, 0.0f);
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

	public void SetPlacementPreview(bool isValid)
	{
		if (_placementIndicatorMesh == null)
		{
			GD.PrintErr($"{Name}: Cannot show tile placement indicator because mesh is null.");
			return;
		}

		if (_placementIndicatorRoot != null)
		{
			_placementIndicatorRoot.Visible = true;
		}

		_placementIndicatorMesh.Visible = true;
		_placementIndicatorMesh.MaterialOverride = isValid ? _validPreviewMaterial : _invalidPreviewMaterial;
	}

	public void ClearPlacementPreview()
	{
		if (_placementIndicatorMesh == null)
			return;

		_placementIndicatorMesh.Visible = false;
	}

	private Material CreatePlacementIndicatorMaterial(bool isValid)
	{
		ShaderMaterial material = new ShaderMaterial();

		material.Shader = GetPlacementPreviewShader();

		Color color = isValid
			? new Color(0.25f, 1.0f, 0.45f, 1.0f)
			: new Color(1.0f, 0.15f, 0.15f, 1.0f);

		material.SetShaderParameter("base_color", color);
		material.SetShaderParameter("bottom_y", -0.25f);
		material.SetShaderParameter("top_y", 0.75f);
		material.SetShaderParameter("max_alpha", 0.28f);
		material.SetShaderParameter("min_alpha", 0.0f);
		material.SetShaderParameter("fade_power", 1.35f);
		material.SetShaderParameter("emission_strength", 0.45f);

		return material;
	}

	private Shader GetPlacementPreviewShader()
	{
		if (_placementPreviewShader != null)
			return _placementPreviewShader;

		_placementPreviewShader = new Shader();

		_placementPreviewShader.Code = @"
shader_type spatial;
render_mode blend_mix, unshaded, depth_prepass_alpha;

uniform vec4 base_color : source_color = vec4(0.25, 1.0, 0.45, 1.0);

uniform float bottom_y = -0.25;
uniform float top_y = 0.75;

uniform float max_alpha = 0.28;
uniform float min_alpha = 0.0;
uniform float fade_power = 1.35;
uniform float emission_strength = 0.45;

varying float local_height;

void vertex() {
    local_height = VERTEX.y;
}

void fragment() {
    float height_range = max(top_y - bottom_y, 0.001);
    float height_factor = clamp((local_height - bottom_y) / height_range, 0.0, 1.0);
    float fade = pow(height_factor, fade_power);

    float alpha = mix(max_alpha, min_alpha, fade);

    ALBEDO = base_color.rgb;
    EMISSION = base_color.rgb * emission_strength;
    ALPHA = alpha;
}
";

		return _placementPreviewShader;
	}

	public void UpdateVisualState()
	{
		if (Data == null)
			return;

		UpdateTileMaterial();
		RebuildPlantVisual();
		RebuildEffectVisual();

		if (Data.Plant != null)
		{
			GD.Print($"{Name} | Light: {Data.LightLevel} | Plant: {Data.Plant.Definition.DisplayName}");
		}
	}

	private void UpdateTileMaterial()
	{
		if (_tileMesh == null)
			return;

		if (_tileMaterial == null)
		{
			SetupUniqueTileMaterial();
		}

		switch (Data.LightLevel)
		{
			case LightLevel.Sun:
				_tileMaterial.AlbedoColor = new Color("d8dbd5");
				break;

			case LightLevel.PartialShade:
				_tileMaterial.AlbedoColor = new Color("cfd4cc");
				break;

			case LightLevel.Shade:
				_tileMaterial.AlbedoColor = new Color("bec6bc");
				break;
		}

		if (Data.IsBlocked)
		{
			_tileMaterial.AlbedoColor = new Color("8f8a84");
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
		_plantVisualRoot.Position = Vector3.Zero;
		_plantVisualRoot.Rotation = Vector3.Zero;
		_plantVisualRoot.Scale = Vector3.One;

		_plantAnchor.AddChild(_plantVisualRoot);
	}
	private void RebuildEffectVisual()
{
	if (_effectVisualRoot != null)
	{
		_effectVisualRoot.QueueFree();
		_effectVisualRoot = null;
	}

	if (Data == null || Data.Plant == null)
		return;

	if (Data.Plant.Definition.Type != PlantType.Mushroom)
		return;

	if (!Data.Plant.IsMature)
		return;

	BoardManager boardManager = FindBoardManager();

	if (boardManager == null)
		return;

	_effectVisualRoot = MushroomEffectVisualBuilder.Create(this, boardManager);

	if (_effectVisualRoot != null)
	{
		AddChild(_effectVisualRoot);
	}
}

private BoardManager FindBoardManager()
{
	Node current = GetParent();

	while (current != null)
	{
		if (current is BoardManager boardManager)
			return boardManager;

		current = current.GetParent();
	}

	return null;
}

private Node3D CreatePlantVisual(PlantInstance plant)
{
	Node3D root = new Node3D();
	root.Name = $"{plant.Definition.Type}_Visual";

	Node3D factoryVisual = PlantVisualFactory.CreateVisual(plant, this);

	 if (factoryVisual != null)
	{
		return factoryVisual;
	}

	switch (plant.Definition.Type)
	{
		case PlantType.Oak:
			CreateOakVisual(root, plant);
			break;

		case PlantType.Moss:
			CreateMossVisual(root, plant);
			break;

		case PlantType.Flower:
			CreateFlowerVisual(root, plant);
			break;

		case PlantType.Birch:
			CreateBirchVisual(root, plant);
			break;
	}
		return root;
	}

	private void CreateOakVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.35f, 0.0f),
			0.11f,
			0.15f,
			0.7f,
			new Color("6b4f2d")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.92f, 0.0f),
			0.33f,
			new Color("4d7f45"),
			new Vector3(1.1f, 0.9f, 1.1f)
		));

		root.AddChild(CreateSphere(
			new Vector3(-0.18f, 0.82f, 0.06f),
			0.22f,
			new Color("5d914d"),
			new Vector3(1.0f, 0.85f, 1.0f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.2f, 0.82f, -0.05f),
			0.2f,
			new Color("3f6f39"),
			new Vector3(1.0f, 0.8f, 1.0f)
		));
	}

	private void CreateBirchVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.42f, 0.0f),
			0.075f,
			0.095f,
			0.85f,
			new Color("d7d2c8")
		));

		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.44f, 0.0f),
			0.083f,
			0.103f,
			0.18f,
			new Color("3b332e")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.98f, 0.0f),
			0.28f,
			new Color("82a85f"),
			new Vector3(1.0f, 0.9f, 1.0f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.18f, 0.9f, 0.04f),
			0.18f,
			new Color("6f9652"),
			new Vector3(1.0f, 0.85f, 1.0f)
		));
	}

	private void CreateMossVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.055f, 0.0f),
			0.22f,
			new Color("5a8f45"),
			new Vector3(1.5f, 0.28f, 1.2f)
		));

		root.AddChild(CreateSphere(
			new Vector3(-0.18f, 0.065f, 0.1f),
			0.14f,
			new Color("6ca252"),
			new Vector3(1.3f, 0.25f, 1.1f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.18f, 0.06f, -0.08f),
			0.13f,
			new Color("497d39"),
			new Vector3(1.25f, 0.25f, 1.0f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.05f, 0.075f, 0.18f),
			0.11f,
			new Color("7fb35f"),
			new Vector3(1.2f, 0.23f, 1.0f)
		));
	}

	private void CreateFlowerVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.24f, 0.0f),
			0.025f,
			0.035f,
			0.48f,
			new Color("4b7d3b")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.52f, 0.0f),
			0.08f,
			new Color("d9c14a"),
			new Vector3(1.0f, 1.0f, 1.0f)
		));

		Color petalColor = new Color("d88cc8");

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.52f, 0.09f),
			0.055f,
			petalColor,
			new Vector3(1.0f, 0.65f, 1.4f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.52f, -0.09f),
			0.055f,
			petalColor,
			new Vector3(1.0f, 0.65f, 1.4f)
		));

		root.AddChild(CreateSphere(
			new Vector3(0.09f, 0.52f, 0.0f),
			0.055f,
			petalColor,
			new Vector3(1.4f, 0.65f, 1.0f)
		));

		root.AddChild(CreateSphere(
			new Vector3(-0.09f, 0.52f, 0.0f),
			0.055f,
			petalColor,
			new Vector3(1.4f, 0.65f, 1.0f)
		));
	}

	private void CreateMushroomVisual(Node3D root, PlantInstance plant)
	{
		root.AddChild(CreateCylinder(
			new Vector3(0.0f, 0.14f, 0.0f),
			0.045f,
			0.06f,
			0.28f,
			new Color("d8c7aa")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.0f, 0.31f, 0.0f),
			0.16f,
			new Color("9a5c47"),
			new Vector3(1.2f, 0.55f, 1.2f)
		));

		root.AddChild(CreateSphere(
			new Vector3(-0.05f, 0.36f, 0.04f),
			0.025f,
			new Color("eadcc8")
		));

		root.AddChild(CreateSphere(
			new Vector3(0.06f, 0.36f, -0.03f),
			0.018f,
			new Color("eadcc8")
		));
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
