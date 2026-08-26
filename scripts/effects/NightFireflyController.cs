using System.Collections.Generic;
using Godot;

public partial class NightFireflyController : Node3D
{
	[ExportGroup("Fireflies")]
	[Export] public bool FirefliesEnabled = true;
	[Export(PropertyHint.Range, "4,40,1")]
	public int FireflyCount = 20;
	[Export] public int LayoutSeed = 8241;
	[Export(PropertyHint.Range, "2.0,24.0,0.5")]
	public float SpawnRadius = 11.0f;
	[Export(PropertyHint.Range, "0.1,4.0,0.1")]
	public float MinimumHeight = 0.8f;
	[Export(PropertyHint.Range, "0.2,6.0,0.1")]
	public float MaximumHeight = 5.2f;
	[Export(PropertyHint.Range, "0.04,0.4,0.01")]
	public float GlowSize = 0.10f;
	[Export] public Color FireflyColor =
		new Color(1.0f, 0.99f, 0.94f);
	[Export(PropertyHint.Range, "0.0,12.0,0.1")]
	public float GlowEnergy = 7.0f;

	[ExportGroup("Movement")]
	[Export(PropertyHint.Range, "0.05,2.0,0.05")]
	public float MinimumMovementSpeed = 0.28f;
	[Export(PropertyHint.Range, "0.05,2.0,0.05")]
	public float MaximumMovementSpeed = 0.62f;
	[Export(PropertyHint.Range, "0.05,1.5,0.05")]
	public float MinimumWanderRadius = 0.24f;
	[Export(PropertyHint.Range, "0.05,2.5,0.05")]
	public float MaximumWanderRadius = 0.72f;
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float BobHeight = 0.45f;

	[ExportGroup("Local Light")]
	[Export(PropertyHint.Range, "0,8,1")]
	public int LightCount = 4;
	[Export(PropertyHint.Range, "0.0,2.0,0.01")]
	public float LightEnergy = 0.32f;
	[Export(PropertyHint.Range, "0.5,8.0,0.1")]
	public float LightRange = 2.6f;

	private sealed class FireflyState
	{
		public Vector3 Origin;
		public float Phase;
		public float MovementSpeed;
		public float WanderRadius;
		public float BobSpeed;
		public float TwinkleSpeed;
	}

	private readonly List<FireflyState> _fireflies = new();
	private readonly List<OmniLight3D> _lights = new();
	private MultiMeshInstance3D _swarm;
	private StandardMaterial3D _glowMaterial;
	private float _nightAmount;
	private float _visibility;
	private double _animationTime;

	public override void _Ready()
	{
		BuildSwarm();
		SetNightAmount(_nightAmount);
	}

	public override void _Process(double delta)
	{
		if (!FirefliesEnabled || _visibility <= 0.001f)
			return;

		_animationTime += delta;
		UpdateFireflies((float)_animationTime);
	}

	public void SetNightAmount(float amount)
	{
		_nightAmount = Mathf.Clamp(amount, 0.0f, 1.0f);
		float normalized = Mathf.Clamp(
			(_nightAmount - 0.12f) / 0.48f,
			0.0f,
			1.0f);
		_visibility = normalized * normalized *
			(3.0f - (2.0f * normalized));

		bool active = FirefliesEnabled && _visibility > 0.001f;
		Visible = active;
		SetProcess(active);

		if (_swarm != null)
			_swarm.Transparency = 1.0f - _visibility;

		if (!active)
		{
			foreach (OmniLight3D light in _lights)
				light.LightEnergy = 0.0f;
		}
	}

	private void BuildSwarm()
	{
		_fireflies.Clear();
		RandomNumberGenerator random = new()
		{
			Seed = (ulong)Mathf.Max(LayoutSeed, 1)
		};
		int count = Mathf.Clamp(FireflyCount, 4, 40);
		float minimumHeight = Mathf.Min(MinimumHeight, MaximumHeight);
		float maximumHeight = Mathf.Max(MinimumHeight, MaximumHeight);
		float minimumSpeed = Mathf.Min(
			MinimumMovementSpeed,
			MaximumMovementSpeed);
		float maximumSpeed = Mathf.Max(
			MinimumMovementSpeed,
			MaximumMovementSpeed);
		float minimumWander = Mathf.Min(
			MinimumWanderRadius,
			MaximumWanderRadius);
		float maximumWander = Mathf.Max(
			MinimumWanderRadius,
			MaximumWanderRadius);

		for (int index = 0; index < count; index++)
		{
			float angle = random.RandfRange(0.0f, Mathf.Tau);
			float radius = Mathf.Sqrt(random.Randf()) *
				Mathf.Max(SpawnRadius, 0.1f);
			_fireflies.Add(new FireflyState
			{
				Origin = new Vector3(
					Mathf.Cos(angle) * radius,
					random.RandfRange(minimumHeight, maximumHeight),
					Mathf.Sin(angle) * radius),
				Phase = random.RandfRange(0.0f, Mathf.Tau),
				MovementSpeed = random.RandfRange(
					minimumSpeed,
					maximumSpeed),
				WanderRadius = random.RandfRange(
					minimumWander,
					maximumWander),
				BobSpeed = random.RandfRange(0.55f, 1.15f),
				TwinkleSpeed = random.RandfRange(1.2f, 2.8f)
			});
		}

		_glowMaterial = new StandardMaterial3D
		{
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = FireflyColor,
			EmissionEnabled = true,
			Emission = FireflyColor,
			EmissionEnergyMultiplier = Mathf.Max(GlowEnergy, 0.0f)
		};
		float glowSize = Mathf.Max(GlowSize, 0.01f);
		SphereMesh glowMesh = new()
		{
			Radius = glowSize * 0.5f,
			Height = glowSize,
			RadialSegments = 8,
			Rings = 4,
			Material = _glowMaterial
		};
		MultiMesh multiMesh = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			Mesh = glowMesh,
			InstanceCount = count,
			VisibleInstanceCount = count
		};
		_swarm = new MultiMeshInstance3D
		{
			Name = "GlowPoints",
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			ExtraCullMargin = Mathf.Max(SpawnRadius, 0.0f) +
				maximumHeight + 2.0f
		};
		AddChild(_swarm);

		int localLightCount = Mathf.Clamp(LightCount, 0, count);
		for (int index = 0; index < localLightCount; index++)
		{
			OmniLight3D light = new()
			{
				Name = $"FireflyLight_{index}",
				LightColor = FireflyColor,
				LightEnergy = 0.0f,
				OmniRange = Mathf.Max(LightRange, 0.1f),
				OmniAttenuation = 1.35f,
				ShadowEnabled = false
			};
			AddChild(light);
			_lights.Add(light);
		}

		UpdateFireflies(0.0f);
	}

	private void UpdateFireflies(float time)
	{
		MultiMesh multiMesh = _swarm?.Multimesh;
		if (multiMesh == null)
			return;

		for (int index = 0; index < _fireflies.Count; index++)
		{
			FireflyState firefly = _fireflies[index];
			float movementTime = time * firefly.MovementSpeed;
			Vector3 offset = new Vector3(
				Mathf.Sin(movementTime + firefly.Phase) *
					firefly.WanderRadius,
				Mathf.Sin((time * firefly.BobSpeed) + firefly.Phase) *
					Mathf.Max(BobHeight, 0.0f),
				Mathf.Cos((movementTime * 0.83f) + firefly.Phase) *
					firefly.WanderRadius);
			Vector3 position = firefly.Origin + offset;
			float twinkle = 0.78f + (0.22f * Mathf.Sin(
				(time * firefly.TwinkleSpeed) + firefly.Phase));
			float scale = Mathf.Lerp(0.72f, 1.12f, twinkle);

			multiMesh.SetInstanceTransform(
				index,
				new Transform3D(
					Basis.Identity.Scaled(Vector3.One * scale),
					position));

			if (index >= _lights.Count)
				continue;

			OmniLight3D light = _lights[index];
			light.Position = position;
			light.LightEnergy = Mathf.Max(LightEnergy, 0.0f) *
				_visibility * twinkle;
		}
	}
}
