using Godot;

public partial class RainLensCyaniluxOverlay : ColorRect
{
	[Export] public bool StartActive = true;
	[Export] public float DefaultIntensity = 0.78f;
	[Export] public float FadeSpeed = 1.8f;

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

		_currentIntensity = StartActive ? DefaultIntensity : 0.0f;
		_targetIntensity = _currentIntensity;
		_shaderMaterial.SetShaderParameter("intensity", _currentIntensity);
		Visible = _currentIntensity > 0.001f;
	}

	public override void _Process(double delta)
	{
		if (_shaderMaterial == null)
			return;

		_currentIntensity = Mathf.MoveToward(_currentIntensity, _targetIntensity, FadeSpeed * (float)delta);
		_shaderMaterial.SetShaderParameter("intensity", _currentIntensity);
		Visible = _currentIntensity > 0.001f || _targetIntensity > 0.001f;
	}

	public void StartRain(float intensity = -1.0f)
	{
		_targetIntensity = intensity < 0.0f ? DefaultIntensity : Mathf.Clamp(intensity, 0.0f, 1.0f);
	}

	public void StopRain()
	{
		_targetIntensity = 0.0f;
	}
}
