using Godot;

public partial class PlantInspectionController : CanvasLayer
{
	private const string OverlayShaderPath =
		"res://shaders/plant_inspection_overlay.gdshader";
	private const string TitleFontPath =
		"res://assets/ui/fonts/OhnoBlazefaceDemo-36Point.otf";
	private const string BodyFontPath =
		"res://assets/ui/fonts/OhnoBlazefaceDemo-18Point.otf";
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
	private PanelContainer _informationCard;
	private ShaderMaterial _overlayMaterial;
	private Label _plantNameLabel;
	private Label _growthLabel;
	private Label _waterLabel;
	private Label _effectLabel;
	private Label _descriptionLabel;
	private Font _titleFont;
	private Font _bodyFont;
	private float _interfaceScale = 1.0f;
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
		_titleFont = GD.Load<Font>(TitleFontPath);
		_bodyFont = GD.Load<Font>(BodyFontPath);

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
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!IsActive || _isClosing)
			return;

		bool closeWithEscape = inputEvent is InputEventKey keyEvent &&
			keyEvent.Keycode == Key.Escape &&
			keyEvent.Pressed &&
			!keyEvent.Echo;
		bool closeWithRightClick = inputEvent is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Right &&
			mouseButton.Pressed;

		if (!closeWithEscape && !closeWithRightClick)
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
			_informationCard,
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
		_informationCard.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		SetProcess(true);
		SetProcessInput(true);
		SyncPlantForegroundView();
		UpdateHexMask();

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
			_informationCard,
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
		int stageCount = Mathf.Max(definition.GrowthStageCount, 2);
		int consumption = plant.GetWaterConsumption();
		int production = plant.GetWaterProduction();
		int balance = production - consumption;
		string remainingRounds = plant.RemainingGrowthRounds == 1
			? "noch 1 Runde"
			: $"noch {plant.RemainingGrowthRounds} Runden";

		_plantNameLabel.Text = definition.DisplayName;
		_growthLabel.Text = plant.IsMature
			? $"Ausgewachsen · Stufe {plant.VisualGrowthStage} von {stageCount}"
			: $"Stufe {plant.VisualGrowthStage} von {stageCount} · " +
				remainingRounds;
		_waterLabel.Text =
			$"Verbrauch: {consumption}\n" +
			$"Produktion: {production}\n" +
			$"Bilanz: {FormatSignedNumber(balance)}";
		_effectLabel.Text = FormatEffect(plant);
		_descriptionLabel.Text = definition.Description;
		_descriptionLabel.Visible =
			!string.IsNullOrWhiteSpace(definition.Description);
	}

	private static string FormatSignedNumber(int value)
	{
		return value > 0 ? $"+{value}" : value.ToString();
	}

	private static string FormatEffect(PlantInstance plant)
	{
		PlantDefinition definition = plant.Definition;
		bool isActive = plant.IsMature ||
			(definition.EffectType == PlantEffectType.TreeShade &&
				!definition.ShadeRequiresMaturity);
		string status = definition.EffectType == PlantEffectType.None
			? "Kein zusätzlicher Effekt."
			: isActive ? "Aktiv" : "Aktiv nach dem Auswachsen";

		string effect = definition.EffectType switch
		{
			PlantEffectType.TreeShade => "Erzeugt Schatten auf umliegenden Feldern.",
			PlantEffectType.AdjacentPlantsProducePlusOne =>
				$"Benachbarte Pflanzen außer Eichen und Birken produzieren " +
				$"+{Mathf.Max(definition.AdjacentWaterProductionBonus, 0)} Wasser.",
			PlantEffectType.SpreadChancePlusOneForNeighbors =>
				"Benachbarte Pflanzen außer Blumen verbreiten sich leichter.",
			_ => ""
		};

		return string.IsNullOrEmpty(effect) ? status : $"{status}\n{effect}";
	}

	private void BuildInterface()
	{
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		_interfaceScale = Mathf.Clamp(
			viewportSize.Y / 1080.0f,
			1.0f,
			1.5f);

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

		_informationCard = new PanelContainer
		{
			Name = "PlantInformationCard",
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		_root.AddChild(_informationCard);
		_informationCard.AnchorLeft = 0.67f;
		_informationCard.AnchorTop = 0.08f;
		_informationCard.AnchorRight = 0.985f;
		_informationCard.AnchorBottom = 0.92f;
		_informationCard.OffsetLeft = 0.0f;
		_informationCard.OffsetTop = 0.0f;
		_informationCard.OffsetRight = 0.0f;
		_informationCard.OffsetBottom = 0.0f;
		_informationCard.AddThemeStyleboxOverride(
			"panel",
			CreateInformationCardStyle());

		MarginContainer margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", ScaleUi(38));
		margin.AddThemeConstantOverride("margin_top", ScaleUi(30));
		margin.AddThemeConstantOverride("margin_right", ScaleUi(38));
		margin.AddThemeConstantOverride("margin_bottom", ScaleUi(30));
		_informationCard.AddChild(margin);

		VBoxContainer content = new VBoxContainer();
		content.AddThemeConstantOverride("separation", ScaleUi(17));
		margin.AddChild(content);

		HBoxContainer header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", ScaleUi(16));
		content.AddChild(header);

		_plantNameLabel = CreateLabel(40, new Color("4b2718"), true);
		_plantNameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		header.AddChild(_plantNameLabel);

		Button closeButton = new Button
		{
			Text = "×",
			TooltipText = "Inspektion schließen",
			CustomMinimumSize = new Vector2(ScaleUi(62), ScaleUi(56)),
			FocusMode = Control.FocusModeEnum.None
		};
		ApplyCloseButtonStyle(closeButton);
		closeButton.Pressed += CloseInspection;
		header.AddChild(closeButton);

		Label category = CreateLabel(18, new Color("77752f"));
		category.Text = "PFLANZEN-INSPEKTION";
		content.AddChild(category);
		content.AddChild(new HSeparator());

		content.AddChild(CreateSectionLabel("WACHSTUM"));
		_growthLabel = CreateLabel(24, new Color("573422"));
		content.AddChild(_growthLabel);

		content.AddChild(CreateSectionLabel("WASSER PRO RUNDE"));
		_waterLabel = CreateLabel(24, new Color("315f65"));
		content.AddChild(_waterLabel);

		content.AddChild(CreateSectionLabel("EFFEKT"));
		_effectLabel = CreateLabel(23, new Color("573422"));
		content.AddChild(_effectLabel);

		_descriptionLabel = CreateLabel(20, new Color("6d4a35"));
		content.AddChild(_descriptionLabel);

		Control spacer = new Control
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		content.AddChild(spacer);

		Label closeHint = CreateLabel(16, new Color("7a634d"));
		closeHint.Text = "Schließen: ×, Esc oder Rechtsklick";
		closeHint.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(closeHint);
	}

	private Label CreateSectionLabel(string text)
	{
		Label label = CreateLabel(18, new Color("8b3f34"));
		label.Text = text;
		label.AddThemeConstantOverride("outline_size", 1);
		return label;
	}

	private Label CreateLabel(int fontSize, Color color, bool useTitleFont = false)
	{
		Label label = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		Font font = useTitleFont ? _titleFont : _bodyFont;
		if (font != null)
			label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", ScaleUi(fontSize));
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeConstantOverride("line_spacing", ScaleUi(3));
		return label;
	}

	private StyleBoxFlat CreateInformationCardStyle()
	{
		return new StyleBoxFlat
		{
			BgColor = new Color("ead6aa"),
			BorderColor = new Color("6a3823"),
			BorderWidthLeft = ScaleUi(4),
			BorderWidthTop = ScaleUi(4),
			BorderWidthRight = ScaleUi(4),
			BorderWidthBottom = ScaleUi(4),
			CornerRadiusTopLeft = ScaleUi(22),
			CornerRadiusTopRight = ScaleUi(22),
			CornerRadiusBottomRight = ScaleUi(22),
			CornerRadiusBottomLeft = ScaleUi(22),
			ShadowColor = new Color(0.08f, 0.04f, 0.02f, 0.55f),
			ShadowSize = ScaleUi(12),
			ShadowOffset = new Vector2(0.0f, ScaleUi(7))
		};
	}

	private void ApplyCloseButtonStyle(Button button)
	{
		if (_bodyFont != null)
			button.AddThemeFontOverride("font", _bodyFont);
		button.AddThemeFontSizeOverride("font_size", ScaleUi(26));
		button.AddThemeColorOverride("font_color", new Color("5a2f20"));
		button.AddThemeColorOverride("font_hover_color", new Color("fff1ce"));
		button.AddThemeStyleboxOverride(
			"normal",
			CreateButtonStyle(new Color("d5b77e")));
		button.AddThemeStyleboxOverride(
			"hover",
			CreateButtonStyle(new Color("a34d3c")));
		button.AddThemeStyleboxOverride(
			"pressed",
			CreateButtonStyle(new Color("7e352c")));
	}

	private StyleBoxFlat CreateButtonStyle(Color color)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			BorderColor = new Color("6a3823"),
			BorderWidthLeft = ScaleUi(2),
			BorderWidthTop = ScaleUi(2),
			BorderWidthRight = ScaleUi(2),
			BorderWidthBottom = ScaleUi(2),
			CornerRadiusTopLeft = ScaleUi(12),
			CornerRadiusTopRight = ScaleUi(12),
			CornerRadiusBottomRight = ScaleUi(12),
			CornerRadiusBottomLeft = ScaleUi(12)
		};
	}

	private int ScaleUi(int value)
	{
		return Mathf.RoundToInt(value * _interfaceScale);
	}
}
