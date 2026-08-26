using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class StylizedWaterController : MeshInstance3D
{
	private const float RiverRockBankSampleChance = 0.82f;
	private const float RiverRockBankBandFraction = 0.22f;
	private const float RiverRockEmptyPatchScale = 0.22f;
	private const float RiverRockEmptyPatchThreshold = 0.34f;
	private const float RiverRockSurfaceClearance = 0.05f;
	private const string RiverRockCausticShaderPath =
		"res://shaders/stylized_water_caustic_overlay.gdshader";
	private static readonly string[] DefaultRiverRockScenePaths =
	{
		"res://scenes/board/tiles/rocks/rock_1.tscn",
		"res://scenes/board/tiles/rocks/rock_2.tscn",
		"res://scenes/board/tiles/rocks/rock_3.tscn",
		"res://scenes/board/tiles/rocks/rock_4.tscn"
	};

	[ExportGroup("Water Appearance")]
	[Export] public Color SurfaceColor = new(0.14f, 0.46f, 0.54f, 1.0f);
	[Export] public Color UnderlayColor = new(0.12f, 0.27f, 0.24f, 1.0f);
	[Export] public Color LineColor = new(0.32f, 0.72f, 0.76f, 1.0f);
	[Export] public Color FoamColor = Colors.White;

	[Export(PropertyHint.Range, "0.2,8.0,0.01")]
	public float SurfaceCellScale = 2.30f;

	[Export(PropertyHint.Range, "0.2,8.0,0.01")]
	public float UnderlayCellScale = 1.55f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float PatternSpeed = 0.07f;

	[ExportGroup("Cell Refraction")]
	[Export(PropertyHint.Range, "0.0,0.05,0.0005")]
	public float RefractionStrength = 0.008f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float RefractionMix = 0.32f;

	[Export(PropertyHint.Range, "0.02,1.5,0.01")]
	public float RefractionEdgeReach = 0.55f;

	[Export(PropertyHint.Range, "0.1,6.0,0.05")]
	public float RefractionEdgeFalloff = 1.35f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float RefractionCenterStrength = 0.16f;

	[Export(PropertyHint.Range, "0.1,8.0,0.05")]
	public float RefractionScale = 1.30f;

	[Export(PropertyHint.Range, "0.0,3.0,0.01")]
	public float RefractionSpeed = 0.16f;

	[ExportGroup("Line Motion")]
	[Export(PropertyHint.Range, "0.0,0.6,0.01")]
	public float LineWobbleStrength = 0.26f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float LineBulgeStrength = 0.82f;

	[Export(PropertyHint.Range, "0.0,3.0,0.01")]
	public float LineBulgeSize = 1.45f;

	[Export(PropertyHint.Range, "0.1,3.0,0.01")]
	public float LineBulgeScale = 1.8f;

	[Export(PropertyHint.Range, "0.2,4.0,0.01")]
	public float LineDistortionScale = 1.4f;

	[ExportGroup("Shore Ripples")]
	[Export(PropertyHint.Range, "0.0,0.6,0.01")]
	public float ShoreRippleStrength = 0.46f;

	[Export(PropertyHint.Range, "4.0,96.0,1.0")]
	public float ShoreRippleReach = 36.0f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float ShoreRippleSpeed = 0.18f;

	[ExportGroup("Rain Ripples")]
	[Export] public Color RainRippleColor =
		new(0.72f, 0.92f, 0.96f, 1.0f);

	[Export(PropertyHint.Range, "0.1,6.0,0.01")]
	public float RainRippleDensity = 1.35f;

	[Export(PropertyHint.Range, "0.04,1.6,0.01")]
	public float RainRippleSize = 0.19f;

	[Export(PropertyHint.Range, "0.05,6.0,0.01")]
	public float RainRippleSpeed = 1.64f;

	[Export(PropertyHint.Range, "0.0,3.0,0.01")]
	public float RainRippleBrightness = 0.31f;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float NormalRainIntensity = 0.72f;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float HeavyRainIntensity = 1.52f;

	[Export(PropertyHint.Range, "0.0,8.0,0.05")]
	public float RainTransitionDuration = 0.65f;

	[ExportGroup("Rain Editor Preview")]
	[Export] public bool ShowRainInEditor = true;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float EditorRainIntensity = 1.0f;

	[ExportGroup("Water Shape")]
	[Export(PropertyHint.Range, "0.0,0.05,0.001")]
	public float WaveHeight = 0.002f;

	[Export(PropertyHint.Range, "4.0,40.0,0.1")]
	public float WaterRadius = 16.0f;

	[Export(PropertyHint.Range, "0.1,4.0,0.1")]
	public float EdgeFade = 1.2f;

	[Export(PropertyHint.Range, "0.005,1.0,0.005")]
	public float ContactWidth = 0.045f;

	[Export(PropertyHint.Range, "0.005,1.0,0.005")]
	public float ContactSoftness = 0.075f;

	[Export] public Vector2 WaterCenter = Vector2.Zero;

	[ExportGroup("Underwater Ground")]
	[Export(PropertyHint.Range, "-4.0,-0.2,0.05")]
	public float UnderwaterGroundDepth = -1.35f;

	[Export(PropertyHint.Range, "0.0,2.5,0.05")]
	public float UnderwaterBasinCenterDepth = 0.72f;

	[Export(PropertyHint.Range, "0.0,2.5,0.05")]
	public float UnderwaterBasinEdgeDepth = 0.18f;

	[Export(PropertyHint.Range, "0.0,0.5,0.01")]
	public float UnderwaterGroundVariation = 0.14f;

	[Export(PropertyHint.Range, "0.05,2.0,0.01")]
	public float UnderwaterGroundVariationScale = 0.28f;

	[ExportGroup("River Rocks")]
	[Export] public bool ShowRiverRocks = true;
	[Export] public Godot.Collections.Array<PackedScene> RiverRockScenes = new();

	[Export(PropertyHint.Range, "0,512,1")]
	public int RiverRockCount = 420;

	[Export(PropertyHint.Range, "0.0,30.0,0.1")]
	public float RiverRockInnerRadius = 10.5f;

	[Export(PropertyHint.Range, "0.0,30.0,0.1")]
	public float RiverRockOuterRadius = 18.0f;

	[Export(PropertyHint.Range, "0.0,5.0,0.05")]
	public float RiverRockMinimumSpacing = 0.55f;

	[Export(PropertyHint.Range, "0.05,3.0,0.05")]
	public float RiverRockMinimumScale = 0.38f;

	[Export(PropertyHint.Range, "0.05,3.0,0.05")]
	public float RiverRockMaximumScale = 0.82f;

	[Export(PropertyHint.Range, "-1.0,1.0,0.01")]
	public float RiverRockGroundOffset = 0.04f;

	[Export(PropertyHint.Range, "0,1000000,1")]
	public int RiverRockRandomSeed = 48137;

	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float RiverRockCausticStrength = 0.62f;

	private sealed class RiverRockMeshTemplate
	{
		public Mesh Mesh;
		public Transform3D LocalTransform;
		public GeometryInstance3D.ShadowCastingSetting CastShadow;
		public uint Layers;
		public Material MaterialOverride;
	}

	private ShaderMaterial _waterMaterial;
	private ShaderMaterial _underlayMaterial;
	private ShaderMaterial _riverRockCausticMaterial;
	private MeshInstance3D _underlay;
	private Node3D _riverRockRoot;
	private readonly List<PackedScene> _activeRiverRockScenes = new();
	private readonly Dictionary<PackedScene, float> _riverRockTopHeights = new();
	private int _riverRockConfigurationHash = int.MinValue;
	private float _rainIntensity;
	private float _targetRainIntensity;

	public override void _Ready()
	{
		PrepareMaterials();
		ApplyDimensions();
		ApplyAppearance();
		RebuildRiverRocksIfNeeded(force: true);
		SetProcess(Engine.IsEditorHint());
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			_rainIntensity = ShowRainInEditor
				? Mathf.Clamp(EditorRainIntensity, 0.0f, 2.0f)
				: 0.0f;
			ApplyDimensions();
			ApplyAppearance();
			RebuildRiverRocksIfNeeded();
			return;
		}

		if (Mathf.IsEqualApprox(_rainIntensity, _targetRainIntensity))
		{
			_rainIntensity = _targetRainIntensity;
			ApplyRainIntensity();
			SetProcess(false);
			return;
		}

		float duration = Mathf.Max(RainTransitionDuration, 0.001f);
		_rainIntensity = Mathf.MoveToward(
			_rainIntensity,
			_targetRainIntensity,
			(float)delta / duration);
		ApplyRainIntensity();
	}

	public void SetRainState(
		bool hasRain,
		bool hasHeavyRain,
		bool immediate = false)
	{
		float intensity = hasHeavyRain
			? HeavyRainIntensity
			: hasRain
				? NormalRainIntensity
				: 0.0f;
		SetRainIntensity(intensity, immediate);
	}

	public void SetRainIntensity(float intensity, bool immediate = false)
	{
		_targetRainIntensity = Mathf.Clamp(intensity, 0.0f, 2.0f);

		if (immediate || RainTransitionDuration <= 0.0f)
		{
			_rainIntensity = _targetRainIntensity;
			ApplyRainIntensity();

			if (!Engine.IsEditorHint())
				SetProcess(false);

			return;
		}

		if (!Engine.IsEditorHint())
			SetProcess(true);
	}

	private void PrepareMaterials()
	{
		_waterMaterial = DuplicateShaderMaterial(this);

		_underlay = GetNodeOrNull<MeshInstance3D>("CausticUnderlay");
		if (_underlay != null)
			_underlayMaterial = DuplicateShaderMaterial(_underlay);

		Shader causticShader = GD.Load<Shader>(RiverRockCausticShaderPath);
		if (causticShader != null)
			_riverRockCausticMaterial = new ShaderMaterial { Shader = causticShader };
	}

	private static ShaderMaterial DuplicateShaderMaterial(
		MeshInstance3D meshInstance)
	{
		Material sourceMaterial = meshInstance.MaterialOverride;

		if (sourceMaterial == null &&
			meshInstance.Mesh != null &&
			meshInstance.Mesh.GetSurfaceCount() > 0)
		{
			sourceMaterial = meshInstance.Mesh.SurfaceGetMaterial(0);
		}

		if (sourceMaterial is not ShaderMaterial shaderMaterial)
			return null;

		ShaderMaterial duplicate = shaderMaterial.Duplicate() as ShaderMaterial;
		meshInstance.MaterialOverride = duplicate;
		return duplicate;
	}

	private void ApplyDimensions()
	{
		if (_waterMaterial != null)
		{
			_waterMaterial.SetShaderParameter("water_center", WaterCenter);
			_waterMaterial.SetShaderParameter("water_radius", WaterRadius);
			_waterMaterial.SetShaderParameter("edge_fade", EdgeFade);
			_waterMaterial.SetShaderParameter("contact_width", ContactWidth);
			_waterMaterial.SetShaderParameter(
				"contact_softness",
				ContactSoftness);
		}

		if (_underlay != null)
		{
			Vector3 underlayPosition = _underlay.Position;
			underlayPosition.Y = UnderwaterGroundDepth;
			_underlay.Position = underlayPosition;
		}

		if (_underlayMaterial != null)
		{
			_underlayMaterial.SetShaderParameter("water_center", WaterCenter);
			_underlayMaterial.SetShaderParameter(
				"underlay_radius",
				WaterRadius + 3.0f);
			_underlayMaterial.SetShaderParameter(
				"basin_center_depth",
				UnderwaterBasinCenterDepth);
			_underlayMaterial.SetShaderParameter(
				"basin_edge_depth",
				UnderwaterBasinEdgeDepth);
			_underlayMaterial.SetShaderParameter(
				"ground_variation",
				UnderwaterGroundVariation);
			_underlayMaterial.SetShaderParameter(
				"ground_variation_scale",
				UnderwaterGroundVariationScale);
		}

		if (_riverRockCausticMaterial != null)
		{
			_riverRockCausticMaterial.SetShaderParameter("water_center", WaterCenter);
			_riverRockCausticMaterial.SetShaderParameter(
				"water_radius",
				WaterRadius + 3.0f);
		}

	}

	private void ApplyAppearance()
	{
		if (_waterMaterial != null)
		{
			_waterMaterial.SetShaderParameter("surface_color", SurfaceColor);
			_waterMaterial.SetShaderParameter("line_color", LineColor);
			_waterMaterial.SetShaderParameter("foam_color", FoamColor);
			_waterMaterial.SetShaderParameter("cell_scale", SurfaceCellScale);
			_waterMaterial.SetShaderParameter("animation_speed", PatternSpeed);
			_waterMaterial.SetShaderParameter("wave_height", WaveHeight);
			_waterMaterial.SetShaderParameter(
				"refraction_strength",
				RefractionStrength);
			_waterMaterial.SetShaderParameter(
				"refraction_mix",
				RefractionMix);
			_waterMaterial.SetShaderParameter(
				"refraction_edge_reach",
				RefractionEdgeReach);
			_waterMaterial.SetShaderParameter(
				"refraction_edge_falloff",
				RefractionEdgeFalloff);
			_waterMaterial.SetShaderParameter(
				"refraction_center_strength",
				RefractionCenterStrength);
			_waterMaterial.SetShaderParameter(
				"refraction_scale",
				RefractionScale);
			_waterMaterial.SetShaderParameter(
				"refraction_speed",
				RefractionSpeed);
			_waterMaterial.SetShaderParameter(
				"line_wobble_strength",
				LineWobbleStrength);
			_waterMaterial.SetShaderParameter(
				"line_bulge_strength",
				LineBulgeStrength);
			_waterMaterial.SetShaderParameter(
				"line_bulge_size",
				LineBulgeSize);
			_waterMaterial.SetShaderParameter(
				"line_bulge_scale",
				LineBulgeScale);
			_waterMaterial.SetShaderParameter(
				"line_distortion_scale",
				LineDistortionScale);
			_waterMaterial.SetShaderParameter(
				"shore_ripple_strength",
				ShoreRippleStrength);
			_waterMaterial.SetShaderParameter(
				"shore_ripple_reach",
				ShoreRippleReach);
			_waterMaterial.SetShaderParameter(
				"shore_ripple_speed",
				ShoreRippleSpeed);
			_waterMaterial.SetShaderParameter(
				"rain_ripple_color",
				RainRippleColor);
			_waterMaterial.SetShaderParameter(
				"rain_ripple_density",
				RainRippleDensity);
			_waterMaterial.SetShaderParameter(
				"rain_ripple_size",
				RainRippleSize);
			_waterMaterial.SetShaderParameter(
				"rain_ripple_speed",
				RainRippleSpeed);
			_waterMaterial.SetShaderParameter(
				"rain_ripple_brightness",
				RainRippleBrightness);
			ApplyRainIntensity();
		}

		if (_underlayMaterial != null)
		{
			_underlayMaterial.SetShaderParameter("underlay_color", UnderlayColor);
			_underlayMaterial.SetShaderParameter("caustic_color", LineColor);
			_underlayMaterial.SetShaderParameter("cell_scale", UnderlayCellScale);
			_underlayMaterial.SetShaderParameter("animation_speed", PatternSpeed);
			_underlayMaterial.SetShaderParameter(
				"line_wobble_strength",
				LineWobbleStrength);
			_underlayMaterial.SetShaderParameter(
				"line_bulge_strength",
				LineBulgeStrength);
			_underlayMaterial.SetShaderParameter(
				"line_bulge_size",
				LineBulgeSize);
			_underlayMaterial.SetShaderParameter(
				"line_bulge_scale",
				LineBulgeScale);
			_underlayMaterial.SetShaderParameter(
				"line_distortion_scale",
				LineDistortionScale);
		}

		if (_riverRockCausticMaterial != null)
		{
			_riverRockCausticMaterial.SetShaderParameter("caustic_color", LineColor);
			_riverRockCausticMaterial.SetShaderParameter(
				"caustic_strength",
				RiverRockCausticStrength);
			_riverRockCausticMaterial.SetShaderParameter(
				"cell_scale",
				UnderlayCellScale);
			_riverRockCausticMaterial.SetShaderParameter(
				"animation_speed",
				PatternSpeed);
			_riverRockCausticMaterial.SetShaderParameter(
				"line_wobble_strength",
				LineWobbleStrength);
			_riverRockCausticMaterial.SetShaderParameter(
				"line_bulge_strength",
				LineBulgeStrength);
			_riverRockCausticMaterial.SetShaderParameter(
				"line_bulge_size",
				LineBulgeSize);
			_riverRockCausticMaterial.SetShaderParameter(
				"line_bulge_scale",
				LineBulgeScale);
			_riverRockCausticMaterial.SetShaderParameter(
				"line_distortion_scale",
				LineDistortionScale);
		}

	}

	private void RebuildRiverRocksIfNeeded(bool force = false)
	{
		int configurationHash = GetRiverRockConfigurationHash();
		if (!force && configurationHash == _riverRockConfigurationHash)
			return;

		_riverRockConfigurationHash = configurationHash;
		ClearRiverRocks();

		if (!ShowRiverRocks || RiverRockCount <= 0)
			return;

		SetupRiverRockScenes();
		if (_activeRiverRockScenes.Count == 0)
			return;

		_riverRockRoot = new Node3D { Name = "RiverRocks" };
		AddChild(_riverRockRoot);

		Dictionary<PackedScene, List<Transform3D>> batches =
			CreateRiverRockTransforms();
		BuildRiverRockMultiMeshes(batches);
	}

	private int GetRiverRockConfigurationHash()
	{
		HashCode hash = new();
		hash.Add(ShowRiverRocks);
		hash.Add(RiverRockCount);
		hash.Add(RiverRockInnerRadius);
		hash.Add(RiverRockOuterRadius);
		hash.Add(RiverRockMinimumSpacing);
		hash.Add(RiverRockMinimumScale);
		hash.Add(RiverRockMaximumScale);
		hash.Add(RiverRockGroundOffset);
		hash.Add(RiverRockRandomSeed);
		hash.Add(WaterCenter);
		hash.Add(WaterRadius);
		hash.Add(UnderwaterGroundDepth);
		hash.Add(UnderwaterBasinCenterDepth);
		hash.Add(UnderwaterBasinEdgeDepth);
		hash.Add(UnderwaterGroundVariation);
		hash.Add(UnderwaterGroundVariationScale);

		if (RiverRockScenes != null)
		{
			foreach (PackedScene scene in RiverRockScenes)
				hash.Add(scene?.ResourcePath);
		}

		return hash.ToHashCode();
	}

	private void ClearRiverRocks()
	{
		if (_riverRockRoot == null || !IsInstanceValid(_riverRockRoot))
			_riverRockRoot = GetNodeOrNull<Node3D>("RiverRocks");

		if (_riverRockRoot != null && IsInstanceValid(_riverRockRoot))
			_riverRockRoot.Free();

		_riverRockRoot = null;
	}

	private void SetupRiverRockScenes()
	{
		_activeRiverRockScenes.Clear();
		_riverRockTopHeights.Clear();

		if (RiverRockScenes != null)
		{
			foreach (PackedScene scene in RiverRockScenes)
			{
				if (scene != null)
					_activeRiverRockScenes.Add(scene);
			}
		}

		if (_activeRiverRockScenes.Count > 0)
			return;

		foreach (string path in DefaultRiverRockScenePaths)
		{
			PackedScene scene = GD.Load<PackedScene>(path);
			if (scene != null)
				_activeRiverRockScenes.Add(scene);
		}
	}

	private Dictionary<PackedScene, List<Transform3D>> CreateRiverRockTransforms()
	{
		Dictionary<PackedScene, List<Transform3D>> batches = new();
		List<Vector2> acceptedPositions = new();
		RandomNumberGenerator random = new()
		{
			Seed = (ulong)Math.Max(RiverRockRandomSeed, 0)
		};

		float innerRadius = Mathf.Max(
			Mathf.Min(RiverRockInnerRadius, RiverRockOuterRadius),
			0.0f);
		float outerRadius = Mathf.Max(
			Mathf.Max(RiverRockInnerRadius, RiverRockOuterRadius),
			innerRadius + 0.01f);
		float minimumScale = Mathf.Max(
			Mathf.Min(RiverRockMinimumScale, RiverRockMaximumScale),
			0.01f);
		float maximumScale = Mathf.Max(
			Mathf.Max(RiverRockMinimumScale, RiverRockMaximumScale),
			minimumScale);
		float minimumSpacingSquared =
			Mathf.Max(RiverRockMinimumSpacing, 0.0f) *
			Mathf.Max(RiverRockMinimumSpacing, 0.0f);
		Vector2 patchOffset = new(
			random.RandfRange(-64.0f, 64.0f),
			random.RandfRange(-64.0f, 64.0f));
		int attemptLimit = Math.Max(RiverRockCount * 40, 64);

		for (int attempt = 0;
			attempt < attemptLimit && acceptedPositions.Count < RiverRockCount;
			attempt++)
		{
			float angle = random.RandfRange(0.0f, Mathf.Tau);
			float radius = SampleRiverRockRadius(
				random,
				innerRadius,
				outerRadius);
			Vector2 position = WaterCenter + new Vector2(
				Mathf.Cos(angle),
				Mathf.Sin(angle)) * radius;

			if (IsRiverRockEmptyPatch(
				position - WaterCenter,
				patchOffset))
			{
				continue;
			}

			if (!HasMinimumRiverRockSpacing(
				acceptedPositions,
				position,
				minimumSpacingSquared))
			{
				continue;
			}

			acceptedPositions.Add(position);
			PackedScene scene = _activeRiverRockScenes[
				random.RandiRange(0, _activeRiverRockScenes.Count - 1)];
			if (!batches.TryGetValue(scene, out List<Transform3D> transforms))
			{
				transforms = new List<Transform3D>();
				batches.Add(scene, transforms);
			}

			float scale = random.RandfRange(minimumScale, maximumScale);
			float groundHeight = GetRiverRockGroundHeight(position, radius);
			float visibleTopHeight = Mathf.Min(
				groundHeight + RiverRockGroundOffset,
				-RiverRockSurfaceClearance);
			Basis basis = new Basis(
				Vector3.Up,
				random.RandfRange(0.0f, Mathf.Tau)).Scaled(Vector3.One * scale);
			Vector3 origin = new(
				position.X,
				visibleTopHeight - GetRiverRockTopHeight(scene) * scale,
				position.Y);
			transforms.Add(new Transform3D(basis, origin));
		}

		if (acceptedPositions.Count < RiverRockCount)
		{
			GD.PushWarning(
				$"Flusssteine: {acceptedPositions.Count} von " +
				$"{RiverRockCount} Positionen erfüllen den Mindestabstand.");
		}

		return batches;
	}

	private static float SampleRiverRockRadius(
		RandomNumberGenerator random,
		float innerRadius,
		float outerRadius)
	{
		float areaRadius = Mathf.Sqrt(Mathf.Lerp(
			innerRadius * innerRadius,
			outerRadius * outerRadius,
			random.Randf()));
		if (random.Randf() >= RiverRockBankSampleChance)
			return areaRadius;

		float radiusSpan = outerRadius - innerRadius;
		float bankBandWidth = radiusSpan * RiverRockBankBandFraction;
		float bankInset = Mathf.Min(0.18f, radiusSpan * 0.08f);
		float bankDistance = bankInset +
			Mathf.Pow(random.Randf(), 1.8f) *
			Mathf.Max(bankBandWidth - bankInset, 0.0f);
		float combinedRadius = innerRadius + outerRadius;
		float innerBankChance = combinedRadius > 0.0f
			? innerRadius / combinedRadius
			: 0.5f;

		return random.Randf() < innerBankChance
			? innerRadius + bankDistance
			: outerRadius - bankDistance;
	}

	private static bool IsRiverRockEmptyPatch(
		Vector2 relativePosition,
		Vector2 patchOffset)
	{
		float broadNoise = GetRiverGroundValueNoise(
			relativePosition * RiverRockEmptyPatchScale + patchOffset);
		float detailNoise = GetRiverGroundValueNoise(
			relativePosition * RiverRockEmptyPatchScale * 2.35f +
			patchOffset * 1.91f +
			new Vector2(8.7f, -13.1f));
		float patchValue = Mathf.Lerp(broadNoise, detailNoise, 0.22f);

		return patchValue < RiverRockEmptyPatchThreshold;
	}

	private static bool HasMinimumRiverRockSpacing(
		List<Vector2> acceptedPositions,
		Vector2 candidate,
		float minimumSpacingSquared)
	{
		foreach (Vector2 position in acceptedPositions)
		{
			if (position.DistanceSquaredTo(candidate) < minimumSpacingSquared)
				return false;
		}

		return true;
	}

	private float GetRiverRockGroundHeight(Vector2 position, float radius)
	{
		float underlayRadius = Mathf.Max(WaterRadius + 3.0f, 0.001f);
		float radialDistance = Mathf.Clamp(radius / underlayRadius, 0.0f, 1.0f);
		float edgeBlend = Mathf.Clamp((radialDistance - 0.12f) / 0.88f, 0.0f, 1.0f);
		edgeBlend = edgeBlend * edgeBlend * (3.0f - 2.0f * edgeBlend);
		float basinDepth = Mathf.Lerp(
			UnderwaterBasinCenterDepth,
			UnderwaterBasinEdgeDepth,
			edgeBlend);
		Vector2 relativePosition = position - WaterCenter;
		float broadVariation = GetRiverGroundValueNoise(
			relativePosition * UnderwaterGroundVariationScale +
			new Vector2(12.7f, 4.3f));
		float detailVariation = GetRiverGroundValueNoise(
			relativePosition * UnderwaterGroundVariationScale * 2.35f +
			new Vector2(3.1f, 19.6f));
		float naturalVariation =
			(broadVariation * 0.72f + detailVariation * 0.28f) * 2.0f - 1.0f;
		float displacedDepth = Mathf.Max(
			0.0f,
			basinDepth + naturalVariation * UnderwaterGroundVariation);
		return UnderwaterGroundDepth - displacedDepth;
	}

	private static float GetRiverGroundValueNoise(Vector2 point)
	{
		Vector2 cell = new(Mathf.Floor(point.X), Mathf.Floor(point.Y));
		Vector2 localPosition = new(
			Fract(point.X),
			Fract(point.Y));
		float blendX = localPosition.X * localPosition.X *
			(3.0f - 2.0f * localPosition.X);
		float blendY = localPosition.Y * localPosition.Y *
			(3.0f - 2.0f * localPosition.Y);
		float bottomLeft = GetRiverGroundHash(cell);
		float bottomRight = GetRiverGroundHash(cell + Vector2.Right);
		float topLeft = GetRiverGroundHash(cell + Vector2.Down);
		float topRight = GetRiverGroundHash(cell + Vector2.One);
		float bottom = Mathf.Lerp(bottomLeft, bottomRight, blendX);
		float top = Mathf.Lerp(topLeft, topRight, blendX);
		return Mathf.Lerp(bottom, top, blendY);
	}

	private static float GetRiverGroundHash(Vector2 point)
	{
		float hashX = Fract(point.X * 0.1031f);
		float hashY = Fract(point.Y * 0.1030f);
		float hashZ = Fract(point.X * 0.0973f);
		float offset =
			hashX * (hashY + 33.33f) +
			hashY * (hashZ + 33.33f) +
			hashZ * (hashX + 33.33f);
		hashX += offset;
		hashY += offset;
		hashZ += offset;
		return Fract((hashX + hashY) * hashZ);
	}

	private static float Fract(float value)
	{
		return value - Mathf.Floor(value);
	}

	private float GetRiverRockTopHeight(PackedScene scene)
	{
		if (_riverRockTopHeights.TryGetValue(scene, out float topHeight))
			return topHeight;

		Node sceneInstance = scene.Instantiate();
		float maximumY = float.NegativeInfinity;
		IncludeRiverRockTopHeight(
			sceneInstance,
			Transform3D.Identity,
			ref maximumY);
		sceneInstance.Free();

		topHeight = float.IsNegativeInfinity(maximumY) ? 0.0f : maximumY;
		_riverRockTopHeights.Add(scene, topHeight);
		return topHeight;
	}

	private static void IncludeRiverRockTopHeight(
		Node node,
		Transform3D parentTransform,
		ref float maximumY)
	{
		Transform3D localTransform = parentTransform;
		if (node is Node3D node3D)
			localTransform = parentTransform * node3D.Transform;

		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			Aabb bounds = meshInstance.GetAabb();
			for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
			{
				Vector3 corner = bounds.Position + new Vector3(
					(cornerIndex & 1) == 0 ? 0.0f : bounds.Size.X,
					(cornerIndex & 2) == 0 ? 0.0f : bounds.Size.Y,
					(cornerIndex & 4) == 0 ? 0.0f : bounds.Size.Z);
				maximumY = Mathf.Max(maximumY, (localTransform * corner).Y);
			}
		}

		foreach (Node child in node.GetChildren())
			IncludeRiverRockTopHeight(child, localTransform, ref maximumY);
	}

	private void BuildRiverRockMultiMeshes(
		Dictionary<PackedScene, List<Transform3D>> batches)
	{
		int batchIndex = 0;

		foreach ((PackedScene scene, List<Transform3D> transforms) in batches)
		{
			Node sceneInstance = scene.Instantiate();
			List<RiverRockMeshTemplate> meshTemplates = new();
			CollectRiverRockMeshes(
				sceneInstance,
				Transform3D.Identity,
				meshTemplates);
			sceneInstance.Free();

			for (int meshIndex = 0; meshIndex < meshTemplates.Count; meshIndex++)
			{
				RiverRockMeshTemplate template = meshTemplates[meshIndex];
				MultiMesh multiMesh = new()
				{
					TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
					Mesh = template.Mesh
				};
				multiMesh.InstanceCount = transforms.Count;
				multiMesh.VisibleInstanceCount = -1;

				for (int instanceIndex = 0;
					instanceIndex < transforms.Count;
					instanceIndex++)
				{
					multiMesh.SetInstanceTransform(
						instanceIndex,
						transforms[instanceIndex] * template.LocalTransform);
				}

				MultiMeshInstance3D multiMeshInstance = new()
				{
					Name = $"RiverRocks_{batchIndex}_{meshIndex}",
					Multimesh = multiMesh,
					CastShadow = template.CastShadow,
					Layers = template.Layers,
					MaterialOverride = template.MaterialOverride,
					MaterialOverlay = _riverRockCausticMaterial
				};
				_riverRockRoot.AddChild(multiMeshInstance);
			}

			batchIndex++;
		}
	}

	private static void CollectRiverRockMeshes(
		Node node,
		Transform3D parentTransform,
		List<RiverRockMeshTemplate> meshTemplates)
	{
		Transform3D localTransform = parentTransform;
		if (node is Node3D node3D)
			localTransform = parentTransform * node3D.Transform;

		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			meshTemplates.Add(new RiverRockMeshTemplate
			{
				Mesh = CreateRiverRockBatchMesh(meshInstance),
				LocalTransform = localTransform,
				CastShadow = meshInstance.CastShadow,
				Layers = meshInstance.Layers,
				MaterialOverride = meshInstance.MaterialOverride
			});
		}

		foreach (Node child in node.GetChildren())
			CollectRiverRockMeshes(child, localTransform, meshTemplates);
	}

	private static Mesh CreateRiverRockBatchMesh(MeshInstance3D meshInstance)
	{
		Mesh sourceMesh = meshInstance.Mesh;
		bool hasSurfaceOverride = false;

		for (int surfaceIndex = 0;
			surfaceIndex < sourceMesh.GetSurfaceCount();
			surfaceIndex++)
		{
			if (meshInstance.GetSurfaceOverrideMaterial(surfaceIndex) != null)
			{
				hasSurfaceOverride = true;
				break;
			}
		}

		if (!hasSurfaceOverride)
			return sourceMesh;

		Mesh batchMesh = sourceMesh.Duplicate() as Mesh;
		if (batchMesh == null)
			return sourceMesh;

		for (int surfaceIndex = 0;
			surfaceIndex < batchMesh.GetSurfaceCount();
			surfaceIndex++)
		{
			Material surfaceOverride =
				meshInstance.GetSurfaceOverrideMaterial(surfaceIndex);
			if (surfaceOverride != null)
				batchMesh.SurfaceSetMaterial(surfaceIndex, surfaceOverride);
		}

		return batchMesh;
	}

	private void ApplyRainIntensity()
	{
		_waterMaterial?.SetShaderParameter(
			"rain_intensity",
			_rainIntensity);
	}
}
