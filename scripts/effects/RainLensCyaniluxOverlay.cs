using Godot;

[Tool]
public partial class RainLensCyaniluxOverlay : ColorRect
{
	[ExportGroup("Activation")]
	[Export] public bool StartActive = true;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float DefaultIntensity = 0.78f;
	[Export(PropertyHint.Range, "0.05,5.0,0.05")]
	public float FadeSpeed = 1.8f;

	[ExportGroup("Live Preview")]
	[Export] public bool PreviewEffect = false;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float PreviewIntensity = 0.78f;

	private float _dropScale = 18.0f;
	[ExportGroup("Droplets")]
	[Export(PropertyHint.Range, "5.0,100.0,0.5")]
	public float DropScale
	{
		get => _dropScale;
		set => SetShaderFloat(ref _dropScale, "drop_scale", value);
	}

	private float _dropDensity = 0.18f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float DropDensity
	{
		get => _dropDensity;
		set => SetShaderFloat(ref _dropDensity, "drop_density", value);
	}

	private float _speed = 0.13f;
	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float Speed
	{
		get => _speed;
		set => SetShaderFloat(ref _speed, "speed", value);
	}

	private float _dropRadius = 0.15f;
	[Export(PropertyHint.Range, "0.01,0.30,0.005")]
	public float DropRadius
	{
		get => _dropRadius;
		set => SetShaderFloat(ref _dropRadius, "drop_radius", value);
	}

	private float _dropStretch = 2.35f;
	[Export(PropertyHint.Range, "1.0,4.0,0.05")]
	public float DropStretch
	{
		get => _dropStretch;
		set => SetShaderFloat(ref _dropStretch, "drop_stretch", value);
	}

	private float _driftStrength = 0.035f;
	[Export(PropertyHint.Range, "0.0,0.15,0.005")]
	public float DriftStrength
	{
		get => _driftStrength;
		set => SetShaderFloat(ref _driftStrength, "drift_strength", value);
	}

	private float _movingDropChance = 1.0f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float MovingDropChance
	{
		get => _movingDropChance;
		set => SetShaderFloat(ref _movingDropChance, "moving_drop_chance", value);
	}

	private float _slideDuration = 0.24f;
	[Export(PropertyHint.Range, "0.05,0.60,0.01")]
	public float SlideDuration
	{
		get => _slideDuration;
		set => SetShaderFloat(ref _slideDuration, "slide_duration", value);
	}

	private float _fastDropChance = 0.18f;
	[ExportGroup("Fast Droplets")]
	[Export(PropertyHint.Range, "0.0,0.5,0.01")]
	public float FastDropChance
	{
		get => _fastDropChance;
		set => SetShaderFloat(ref _fastDropChance, "fast_drop_chance", value);
	}

	private float _fastSpeedMultiplier = 1.7f;
	[Export(PropertyHint.Range, "1.0,5.0,0.05")]
	public float FastSpeedMultiplier
	{
		get => _fastSpeedMultiplier;
		set => SetShaderFloat(ref _fastSpeedMultiplier, "fast_speed_multiplier", value);
	}

	private float _fastSpeedVariation = 0.22f;
	[Export(PropertyHint.Range, "0.0,0.5,0.01")]
	public float FastSpeedVariation
	{
		get => _fastSpeedVariation;
		set => SetShaderFloat(ref _fastSpeedVariation, "fast_speed_variation", value);
	}

	private float _fastColumnCount = 11.0f;
	[Export(PropertyHint.Range, "4.0,24.0,1.0")]
	public float FastColumnCount
	{
		get => _fastColumnCount;
		set => SetShaderFloat(ref _fastColumnCount, "fast_column_count", value);
	}

	private float _fastWidthScale = 0.52f;
	[Export(PropertyHint.Range, "0.20,1.0,0.01")]
	public float FastWidthScale
	{
		get => _fastWidthScale;
		set => SetShaderFloat(ref _fastWidthScale, "fast_width_scale", value);
	}

	private float _fastWidthVariation = 0.18f;
	[Export(PropertyHint.Range, "0.0,0.5,0.01")]
	public float FastWidthVariation
	{
		get => _fastWidthVariation;
		set => SetShaderFloat(ref _fastWidthVariation, "fast_width_variation", value);
	}

	private float _fastCurveStrength = 0.11f;
	[Export(PropertyHint.Range, "0.0,0.30,0.005")]
	public float FastCurveStrength
	{
		get => _fastCurveStrength;
		set => SetShaderFloat(ref _fastCurveStrength, "fast_curve_strength", value);
	}

	private float _lifeFadeWidth = 0.20f;
	[Export(PropertyHint.Range, "0.05,0.35,0.01")]
	public float LifeFadeWidth
	{
		get => _lifeFadeWidth;
		set => SetShaderFloat(ref _lifeFadeWidth, "life_fade_width", value);
	}

	private float _trailChance = 0.30f;
	[ExportGroup("Trails")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float TrailChance
	{
		get => _trailChance;
		set => SetShaderFloat(ref _trailChance, "trail_chance", value);
	}

	private float _trailLength = 0.28f;
	[Export(PropertyHint.Range, "0.05,0.60,0.01")]
	public float TrailLength
	{
		get => _trailLength;
		set => SetShaderFloat(ref _trailLength, "trail_length", value);
	}

	private float _trailStrength = 0.28f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float TrailStrength
	{
		get => _trailStrength;
		set => SetShaderFloat(ref _trailStrength, "trail_strength", value);
	}

	private float _distortionStrength = 0.04f;
	[ExportGroup("Refraction")]
	[Export(PropertyHint.Range, "0.0,0.06,0.001")]
	public float DistortionStrength
	{
		get => _distortionStrength;
		set => SetShaderFloat(ref _distortionStrength, "distortion_strength", value);
	}

	private float _refractionMix = 0.92f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float RefractionMix
	{
		get => _refractionMix;
		set => SetShaderFloat(ref _refractionMix, "refraction_mix", value);
	}

	private float _blurLod = 0.55f;
	[Export(PropertyHint.Range, "0.0,2.0,0.05")]
	public float BlurLod
	{
		get => _blurLod;
		set => SetShaderFloat(ref _blurLod, "blur_lod", value);
	}

	private float _highlightStrength = 0.20f;
	[ExportGroup("Highlights")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float HighlightStrength
	{
		get => _highlightStrength;
		set => SetShaderFloat(ref _highlightStrength, "highlight_strength", value);
	}

	private float _ringStrength = 0.12f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float RingStrength
	{
		get => _ringStrength;
		set => SetShaderFloat(ref _ringStrength, "ring_strength", value);
	}

	private ShaderMaterial _shaderMaterial;
	private float _currentIntensity = 0.0f;
	private float _targetIntensity = 0.0f;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		OffsetLeft = 0.0f;
		OffsetTop = 0.0f;
		OffsetRight = 0.0f;
		OffsetBottom = 0.0f;

		_shaderMaterial = Material as ShaderMaterial;

		if (_shaderMaterial == null)
		{
			GD.PrintErr("RainLensCyaniluxOverlay needs a ShaderMaterial.");
			return;
		}

		_currentIntensity = !Engine.IsEditorHint() && StartActive
			? DefaultIntensity
			: 0.0f;
		_targetIntensity = _currentIntensity;
		ApplyShaderSettings();
		UpdateDisplayedIntensity();
	}

	public override void _Process(double delta)
	{
		if (_shaderMaterial == null)
			return;

		_currentIntensity = Mathf.MoveToward(
			_currentIntensity,
			_targetIntensity,
			FadeSpeed * (float)delta);
		UpdateDisplayedIntensity();
	}

	public void StartRain(float intensity = -1.0f)
	{
		_targetIntensity = intensity < 0.0f ? DefaultIntensity : Mathf.Clamp(intensity, 0.0f, 1.0f);
	}

	public void StopRain()
	{
		_targetIntensity = 0.0f;
	}

	private void ApplyShaderSettings()
	{
		_shaderMaterial.SetShaderParameter("drop_scale", DropScale);
		_shaderMaterial.SetShaderParameter("drop_density", DropDensity);
		_shaderMaterial.SetShaderParameter("speed", Speed);
		_shaderMaterial.SetShaderParameter("drop_radius", DropRadius);
		_shaderMaterial.SetShaderParameter("drop_stretch", DropStretch);
		_shaderMaterial.SetShaderParameter("drift_strength", DriftStrength);
		_shaderMaterial.SetShaderParameter("moving_drop_chance", MovingDropChance);
		_shaderMaterial.SetShaderParameter("slide_duration", SlideDuration);
		_shaderMaterial.SetShaderParameter("fast_drop_chance", FastDropChance);
		_shaderMaterial.SetShaderParameter("fast_speed_multiplier", FastSpeedMultiplier);
		_shaderMaterial.SetShaderParameter("fast_speed_variation", FastSpeedVariation);
		_shaderMaterial.SetShaderParameter("fast_column_count", FastColumnCount);
		_shaderMaterial.SetShaderParameter("fast_width_scale", FastWidthScale);
		_shaderMaterial.SetShaderParameter("fast_width_variation", FastWidthVariation);
		_shaderMaterial.SetShaderParameter("fast_curve_strength", FastCurveStrength);
		_shaderMaterial.SetShaderParameter("life_fade_width", LifeFadeWidth);
		_shaderMaterial.SetShaderParameter("trail_chance", TrailChance);
		_shaderMaterial.SetShaderParameter("trail_length", TrailLength);
		_shaderMaterial.SetShaderParameter("trail_strength", TrailStrength);
		_shaderMaterial.SetShaderParameter("distortion_strength", DistortionStrength);
		_shaderMaterial.SetShaderParameter("refraction_mix", RefractionMix);
		_shaderMaterial.SetShaderParameter("blur_lod", BlurLod);
		_shaderMaterial.SetShaderParameter("highlight_strength", HighlightStrength);
		_shaderMaterial.SetShaderParameter("ring_strength", RingStrength);
	}

	private void SetShaderFloat(ref float field, StringName parameter, float value)
	{
		field = value;
		_shaderMaterial?.SetShaderParameter(parameter, value);
	}

	private void UpdateDisplayedIntensity()
	{
		bool showEditorPreview = Engine.IsEditorHint() && PreviewEffect;
		float displayedIntensity = showEditorPreview
			? PreviewIntensity
			: _currentIntensity;
		_shaderMaterial.SetShaderParameter("intensity", displayedIntensity);
		Visible = showEditorPreview ||
			displayedIntensity > 0.001f ||
			_targetIntensity > 0.001f;
	}
}
