using Godot;

public partial class PlantInspectionController : CanvasLayer
{
	private const string OverlayShaderPath =
		"res://shaders/plant_inspection_overlay.gdshader";
	private const string InspectionIconPath = "res://assets/ui/inspection/";
	private const int PlantForegroundRenderLayer = 19;
	private static readonly string[] HexPointShaderParameters =
	{
		"hex_point_0",
		"hex_point_1",
		"hex_point_2",
		"hex_point_3",
		"hex_point_4",
		"hex_point_5"
	};

	[ExportGroup("Scene Paths")]
	[Export] public NodePath GameManagerPath = new NodePath("../GameManager");
	[Export] public NodePath CameraRigPath = new NodePath("../CameraRig");
	[Export] public NodePath GameUiPath = new NodePath("../UI/CanvasLayer");

	[ExportGroup("Presentation")]
	[Export(PropertyHint.Range, "0.0,0.85,0.01")]
	public float WorldDarkness = 0.50f;
	[Export(PropertyHint.Range, "0.85,1.15,0.005")]
	public float HexMaskRadiusScale = 1.0f;
	[Export(PropertyHint.Range, "-0.2,0.5,0.01")]
	public float HexMaskWorldHeight = 0.04f;
	[Export(PropertyHint.Range, "0.0,12.0,0.5")]
	public float HexEdgeFeatherPixels = 1.0f;
	[Export(PropertyHint.Range, "0.05,1.0,0.05")]
	public float FadeDuration = 0.30f;

	public bool IsActive { get; private set; }

	private GameManager _gameManager;
	private CameraRigController _cameraRig;
	private Camera3D _camera;
	private CanvasLayer _gameUi;
	private Control _root;
	private ColorRect _darkOverlay;
	private SubViewport _plantForegroundViewport;
	private Camera3D _plantForegroundCamera;
	private Control _badgeGroup;
	private readonly Panel[] _badges = new Panel[3];
	private readonly Line2D[] _badgePointers = new Line2D[3];
	private Rect2 _badgeBounds;
	private ShaderMaterial _overlayMaterial;
	private ShaderMaterial _inactiveWaterMaterial;
	private Texture2D _consumptionIcon;
	private Texture2D _productionIcon;
	private Texture2D _growthIcon;
	private Texture2D _inactiveGrowthIcon;
	private HexTile _selectedTile;
	private PlantInstance _selectedPlant;
	private float _selectedHexRadius = 1.0f;
	private bool _gameUiWasVisible;
	private bool _isClosing;
	private Tween _transitionTween;

	public override void _Ready()
	{
		_gameManager = GetNodeOrNull<GameManager>(GameManagerPath);
		_cameraRig = GetNodeOrNull<CameraRigController>(CameraRigPath);
		_camera = _cameraRig?.Camera;
		_gameUi = GetNodeOrNull<CanvasLayer>(GameUiPath);
		_consumptionIcon = GD.Load<Texture2D>(InspectionIconPath + "water_consumption.png");
		_productionIcon = GD.Load<Texture2D>(InspectionIconPath + "water_production.png");
		_growthIcon = GD.Load<Texture2D>(InspectionIconPath + "growth_active.png");
		_inactiveGrowthIcon = GD.Load<Texture2D>(InspectionIconPath + "growth_inactive.png");

		BuildInterface();
		SetProcess(false);
		SetProcessInput(false);

		if (_gameManager == null)
		{
			GD.PrintErr(
				"PlantInspectionController: GameManager fehlt am erwarteten Pfad.");
			return;
		}

		if (_cameraRig == null || _camera == null)
		{
			GD.PrintErr(
				"PlantInspectionController: CameraRig oder Camera3D fehlt.");
			return;
		}

		ProcessPriority = _cameraRig.ProcessPriority + 1;

		_gameManager.TileInformationRequested += OpenInspection;
	}

	public override void _Process(double delta)
	{
		if (!IsActive)
			return;

		if (_selectedTile == null ||
			!IsInstanceValid(_selectedTile) ||
			_selectedTile.Data?.Plant == null ||
			!ReferenceEquals(_selectedTile.Data.Plant, _selectedPlant))
		{
			CloseInspection();
			return;
		}

		SyncPlantForegroundView();
		UpdateHexMask();
		UpdateBadgePositions();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!IsActive || _isClosing)
			return;

		bool closeWithEscape = inputEvent is InputEventKey keyEvent &&
			keyEvent.Keycode == Key.Escape &&
			keyEvent.Pressed &&
			!keyEvent.Echo;
		bool closeWithClick = inputEvent is InputEventMouseButton mouseButton &&
			(mouseButton.ButtonIndex == MouseButton.Left ||
				mouseButton.ButtonIndex == MouseButton.Right) &&
			mouseButton.Pressed;

		if (!closeWithEscape && !closeWithClick)
			return;

		CloseInspection();
		GetViewport().SetInputAsHandled();
	}

	public override void _ExitTree()
	{
		if (_gameManager != null)
			_gameManager.TileInformationRequested -= OpenInspection;

		_transitionTween?.Kill();
		ReleasePlantForeground();
	}

	public void CloseInspection()
	{
		if (!IsActive || _isClosing)
			return;

		_isClosing = true;
		_cameraRig?.EndInspectionFocus();
		_transitionTween?.Kill();
		_transitionTween = CreateTween();
		_transitionTween.SetParallel(true);

		if (_overlayMaterial != null)
		{
			_transitionTween.TweenProperty(
				_overlayMaterial,
				"shader_parameter/darkness",
				0.0f,
				FadeDuration);
		}

		_transitionTween.TweenProperty(
			_badgeGroup,
			"modulate:a",
			0.0f,
			FadeDuration * 0.7f);
		_transitionTween.Chain().TweenCallback(Callable.From(FinishClose));
	}

	private void OpenInspection(HexTile tile)
	{
		if (IsActive ||
			tile == null ||
			!IsInstanceValid(tile) ||
			tile.Data?.Plant == null ||
			_cameraRig == null ||
			!_cameraRig.BeginInspectionFocus(tile))
		{
			return;
		}

		PlantInstance plant = tile.Data.Plant;
		if (!tile.EnablePlantInspectionRenderLayer(
			plant,
			PlantForegroundRenderLayer))
		{
			_cameraRig.EndInspectionFocus();
			return;
		}

		_selectedTile = tile;
		_selectedPlant = plant;
		_selectedHexRadius = ResolveHexWorldRadius(tile);

		RefreshInformation(plant);
		_gameUiWasVisible = _gameUi?.Visible ?? false;
		if (_gameUi != null)
			_gameUi.Visible = false;

		_gameManager.SetPlantInspectionInputLocked(true);
		IsActive = true;
		_isClosing = false;
		_root.Visible = true;
		_plantForegroundViewport.RenderTargetUpdateMode =
			SubViewport.UpdateMode.Always;
		_badgeGroup.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		SetProcess(true);
		SetProcessInput(true);
		SyncPlantForegroundView();
		UpdateHexMask();
		UpdateBadgePositions();

		_transitionTween?.Kill();
		_transitionTween = CreateTween();
		_transitionTween.SetParallel(true);

		if (_overlayMaterial != null)
		{
			_overlayMaterial.SetShaderParameter("darkness", 0.0f);
			_transitionTween.TweenProperty(
				_overlayMaterial,
				"shader_parameter/darkness",
				WorldDarkness,
				FadeDuration);
		}

		_transitionTween.TweenProperty(
			_badgeGroup,
			"modulate:a",
			1.0f,
			FadeDuration * 0.8f);
	}

	private void FinishClose()
	{
		ReleasePlantForeground();
		_root.Visible = false;
		if (_gameUi != null)
			_gameUi.Visible = _gameUiWasVisible;

		_gameManager?.SetPlantInspectionInputLocked(false);
		_selectedTile = null;
		_selectedPlant = null;
		IsActive = false;
		_isClosing = false;
		SetProcess(false);
		SetProcessInput(false);
	}

	private void ReleasePlantForeground()
	{
		if (_selectedTile != null && IsInstanceValid(_selectedTile))
			_selectedTile.DisablePlantInspectionRenderLayer();

		if (_plantForegroundViewport != null)
		{
			_plantForegroundViewport.RenderTargetUpdateMode =
				SubViewport.UpdateMode.Disabled;
		}
	}

	private void SyncPlantForegroundView()
	{
		if (_plantForegroundViewport == null ||
			_plantForegroundCamera == null ||
			_camera == null)
		{
			return;
		}

		Viewport mainViewport = GetViewport();
		Vector2 visibleSize = mainViewport.GetVisibleRect().Size;
		Vector2I targetSize = new Vector2I(
			Mathf.Max(Mathf.RoundToInt(visibleSize.X), 2),
			Mathf.Max(Mathf.RoundToInt(visibleSize.Y), 2));

		if (_plantForegroundViewport.Size != targetSize)
			_plantForegroundViewport.Size = targetSize;

		if (_plantForegroundViewport.World3D != mainViewport.World3D)
			_plantForegroundViewport.World3D = mainViewport.World3D;

		_plantForegroundCamera.GlobalTransform = _camera.GlobalTransform;
		_plantForegroundCamera.Projection = _camera.Projection;
		_plantForegroundCamera.Fov = _camera.Fov;
		_plantForegroundCamera.Size = _camera.Size;
		_plantForegroundCamera.Near = _camera.Near;
		_plantForegroundCamera.Far = _camera.Far;
		_plantForegroundCamera.KeepAspect = _camera.KeepAspect;
		_plantForegroundCamera.FrustumOffset = _camera.FrustumOffset;
		_plantForegroundCamera.HOffset = _camera.HOffset;
		_plantForegroundCamera.VOffset = _camera.VOffset;
		_plantForegroundCamera.Environment = _camera.Environment;
		_plantForegroundCamera.Attributes = _camera.Attributes;
		_plantForegroundCamera.Compositor = _camera.Compositor;
	}

	private float ResolveHexWorldRadius(HexTile tile)
	{
		float boardHexSize = tile.GetParent() is BoardManager boardManager
			? boardManager.HexSize
			: 1.0f;

		return Mathf.Max(boardHexSize * HexMaskRadiusScale, 0.1f);
	}

	private void UpdateHexMask()
	{
		if (_overlayMaterial == null ||
			_camera == null ||
			_selectedTile == null ||
			!IsInstanceValid(_selectedTile))
			return;

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		if (viewportSize.X <= 0.0f || viewportSize.Y <= 0.0f)
			return;

		_overlayMaterial.SetShaderParameter("viewport_size", viewportSize);
		_overlayMaterial.SetShaderParameter(
			"edge_feather",
			Mathf.Max(HexEdgeFeatherPixels, 0.0f));

		for (int cornerIndex = 0; cornerIndex < 6; cornerIndex++)
		{
			float angle = cornerIndex * Mathf.Pi / 3.0f;
			Vector3 localCorner = new Vector3(
				Mathf.Cos(angle) * _selectedHexRadius,
				HexMaskWorldHeight,
				Mathf.Sin(angle) * _selectedHexRadius);
			Vector2 cornerPixels = _camera.UnprojectPosition(
				_selectedTile.ToGlobal(localCorner));
			Vector2 normalizedCorner = new Vector2(
				cornerPixels.X / viewportSize.X,
				cornerPixels.Y / viewportSize.Y);

			_overlayMaterial.SetShaderParameter(
				HexPointShaderParameters[cornerIndex],
				normalizedCorner);
		}
	}

	private void RefreshInformation(PlantInstance plant)
	{
		PlantDefinition definition = plant.Definition;
		int production = plant.GetWaterProduction();
		if (_selectedTile.GetParent() is BoardManager boardManager)
		{
			WaterBalanceCalculation balance = WaterBalanceCalculator.Calculate(
				boardManager, activeEvents: null);
			foreach (PlantWaterResult result in balance.Plants)
			{
				if (!result.Coord.Equals(_selectedTile.Data.Coord))
					continue;

				production = result.Production + result.AdjacentProductionBonus;
				break;
			}
		}

		int consumption = plant.GetWaterConsumption();
		SetBadgeIcons(
			_badges[0], definition.WaterConsumption, consumption,
			_consumptionIcon, _productionIcon, false);
		bool showConsumption = consumption > 0;
		_badges[0].Visible = showConsumption;
		_badgePointers[0].Visible = showConsumption;
		SetBadgeIcons(
			_badges[1], definition.WaterProduction, production,
			_productionIcon, _productionIcon, false);
		bool showProduction = definition.WaterProduction > 0 || production > 0;
		_badges[1].Visible = showProduction;
		_badgePointers[1].Visible = showProduction;
		SetBadgeIcons(
			_badges[2], Mathf.Max(definition.GrowthStageCount, 2),
			plant.VisualGrowthStage, _growthIcon, _inactiveGrowthIcon, true);

		Vector2[] centers =
		{
			new Vector2(-240.0f, -110.0f),
			new Vector2(0.0f, -188.0f),
			new Vector2(252.0f, -38.0f)
		};
		bool hasBadgeBounds = false;
		for (int index = 0; index < _badges.Length; index++)
		{
			Panel badge = _badges[index];
			if (!badge.Visible)
				continue;

			badge.Position = centers[index] - badge.Size * 0.5f;
			Rect2 bounds = new Rect2(badge.Position, badge.Size);
			_badgeBounds = hasBadgeBounds ? _badgeBounds.Merge(bounds) : bounds;
			hasBadgeBounds = true;

			Vector2 direction = -centers[index].Normalized();
			Vector2 halfSize = badge.Size * 0.5f;
			float edgeDistance = Mathf.Min(
				halfSize.X / Mathf.Max(Mathf.Abs(direction.X), 0.001f),
				halfSize.Y / Mathf.Max(Mathf.Abs(direction.Y), 0.001f));
			Vector2 start = centers[index] + direction * edgeDistance;
			_badgePointers[index].Points = new[]
			{
				start,
				start + direction * 54.0f
			};
		}
	}

	private void SetBadgeIcons(
		Panel badge,
		int total,
		int active,
		Texture2D activeIcon,
		Texture2D inactiveIcon,
		bool isGrowth)
	{
		foreach (Node child in badge.GetChildren())
		{
			badge.RemoveChild(child);
			child.QueueFree();
		}

		int count = Mathf.Max(Mathf.Max(total, active), 0);
		float iconHeight = isGrowth ? 96.0f : 112.0f;
		Texture2D texture = activeIcon ?? inactiveIcon;
		float aspect = texture == null
			? 0.625f
			: (float)texture.GetWidth() / Mathf.Max(texture.GetHeight(), 1);
		float iconWidth = iconHeight * aspect;
		float advance = isGrowth ? 40.0f : 44.0f;
		float contentWidth = count > 0 ? iconWidth + (count - 1) * advance : 0.0f;
		float width = Mathf.Max(contentWidth + 32.0f, 144.0f);
		badge.Size = new Vector2(width, 144.0f);

		for (int index = 0; index < count; index++)
		{
			bool isActive = index < active;
			TextureRect icon = new TextureRect
			{
				Texture = isActive ? activeIcon : inactiveIcon,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				TextureFilter = CanvasItem.TextureFilterEnum.Linear,
				Size = new Vector2(iconWidth, iconHeight),
				Position = new Vector2(
					(width - contentWidth) * 0.5f + index * advance,
					(144.0f - iconHeight) * 0.5f)
			};
			if (!isActive && !isGrowth)
				icon.Material = _inactiveWaterMaterial;
			badge.AddChild(icon);
		}
	}

	private void UpdateBadgePositions()
	{
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		if (viewportSize.X <= 0.0f || viewportSize.Y <= 0.0f)
			return;

		Vector3 anchor = _selectedTile.ToGlobal(
			new Vector3(0.0f, HexMaskWorldHeight, 0.0f));
		_badgeGroup.Visible = !_camera.IsPositionBehind(anchor);
		float scale = Mathf.Min(viewportSize.Y / 720.0f, 1.5f);
		float margin = Mathf.Min(18.0f, Mathf.Min(viewportSize.X, viewportSize.Y) * 0.05f);
		scale = Mathf.Min(scale,
			(viewportSize.X - margin * 2.0f) / _badgeBounds.Size.X);
		scale = Mathf.Min(scale,
			(viewportSize.Y - margin * 2.0f) / _badgeBounds.Size.Y);
		_badgeGroup.Scale = Vector2.One * scale;

		Vector2 position = _camera.UnprojectPosition(anchor);
		Vector2 minPosition = Vector2.One * margin - _badgeBounds.Position * scale;
		Vector2 maxPosition = viewportSize - Vector2.One * margin - _badgeBounds.End * scale;
		_badgeGroup.Position = new Vector2(
			Mathf.Clamp(position.X, minPosition.X, maxPosition.X),
			Mathf.Clamp(position.Y, minPosition.Y, maxPosition.Y));
	}

	private void BuildInterface()
	{
		_root = new Control
		{
			Name = "InspectionRoot",
			MouseFilter = Control.MouseFilterEnum.Stop,
			Visible = false
		};
		AddChild(_root);
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		_darkOverlay = new ColorRect
		{
			Name = "WorldDarkening",
			Color = Colors.White,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_root.AddChild(_darkOverlay);
		_darkOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		Shader overlayShader = GD.Load<Shader>(OverlayShaderPath);
		if (overlayShader != null)
		{
			_overlayMaterial = new ShaderMaterial
			{
				Shader = overlayShader
			};
			_darkOverlay.Material = _overlayMaterial;
		}
		else
		{
			GD.PrintErr(
				$"PlantInspectionController: Shader fehlt: {OverlayShaderPath}");
			_darkOverlay.Color = new Color(0.0f, 0.0f, 0.0f, WorldDarkness);
		}

		_plantForegroundViewport = new SubViewport
		{
			Name = "PlantForegroundViewport",
			Size = new Vector2I(2, 2),
			TransparentBg = true,
			GuiDisableInput = true,
			RenderTargetClearMode = SubViewport.ClearMode.Always,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
			World3D = GetViewport().World3D
		};
		_root.AddChild(_plantForegroundViewport);

		_plantForegroundCamera = new Camera3D
		{
			Name = "PlantForegroundCamera",
			CullMask = 0u,
			Current = true
		};
		_plantForegroundCamera.SetCullMaskValue(
			PlantForegroundRenderLayer,
			true);
		_plantForegroundViewport.AddChild(_plantForegroundCamera);

		_overlayMaterial?.SetShaderParameter(
			"plant_foreground_mask",
			_plantForegroundViewport.GetTexture());

		_inactiveWaterMaterial = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = @"shader_type canvas_item;
void fragment() {
	float gray = dot(COLOR.rgb, vec3(0.299, 0.587, 0.114));
	float blue_fill = smoothstep(0.0, 0.12, COLOR.b - COLOR.r);
	COLOR.rgb = mix(COLOR.rgb, vec3(gray), blue_fill);
}"
			}
		};
		_badgeGroup = new Control
		{
			Name = "PlantStatusBadges",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_root.AddChild(_badgeGroup);

		for (int index = 0; index < _badgePointers.Length; index++)
		{
			_badgePointers[index] = new Line2D
			{
				Width = 4.0f,
				DefaultColor = new Color("775139"),
				Antialiased = true
			};
			_badgeGroup.AddChild(_badgePointers[index]);
		}

		string[] names = { "WaterConsumption", "WaterProduction", "Growth" };
		for (int index = 0; index < _badges.Length; index++)
		{
			_badges[index] = new Panel
			{
				Name = names[index],
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			_badges[index].AddThemeStyleboxOverride("panel", CreateBadgeStyle());
			_badgeGroup.AddChild(_badges[index]);
		}
	}

	private static StyleBoxFlat CreateBadgeStyle()
	{
		return new StyleBoxFlat
		{
			BgColor = new Color("d8bc94"),
			BorderColor = new Color("775139"),
			BorderWidthLeft = 5,
			BorderWidthTop = 5,
			BorderWidthRight = 5,
			BorderWidthBottom = 5,
			CornerRadiusTopLeft = 18,
			CornerRadiusTopRight = 18,
			CornerRadiusBottomRight = 18,
			CornerRadiusBottomLeft = 18
		};
	}
}
