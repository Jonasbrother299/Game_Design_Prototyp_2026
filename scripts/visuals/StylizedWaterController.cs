using Godot;

[Tool]
public partial class StylizedWaterController : MeshInstance3D
{
	[ExportGroup("Water Appearance")]
	[Export] public Color SurfaceColor = new(0.14f, 0.46f, 0.54f, 1.0f);
	[Export] public Color UnderlayColor = new(0.16f, 0.50f, 0.57f, 1.0f);
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

	private ShaderMaterial _waterMaterial;
	private ShaderMaterial _underlayMaterial;
	private float _rainIntensity;
	private float _targetRainIntensity;

	public override void _Ready()
	{
		PrepareMaterials();
		ApplyDimensions();
		ApplyAppearance();
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

		MeshInstance3D underlay = GetNodeOrNull<MeshInstance3D>(
			"CausticUnderlay");
		if (underlay != null)
			_underlayMaterial = DuplicateShaderMaterial(underlay);
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
		if (_waterMaterial == null)
			return;

		_waterMaterial.SetShaderParameter("water_center", WaterCenter);
		_waterMaterial.SetShaderParameter("water_radius", WaterRadius);
		_waterMaterial.SetShaderParameter("edge_fade", EdgeFade);
		_waterMaterial.SetShaderParameter("contact_width", ContactWidth);
		_waterMaterial.SetShaderParameter(
			"contact_softness",
			ContactSoftness);
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

		if (_underlayMaterial == null)
			return;

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

	private void ApplyRainIntensity()
	{
		_waterMaterial?.SetShaderParameter(
			"rain_intensity",
			_rainIntensity);
	}
}
