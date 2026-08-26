using Godot;
using System;

public partial class PlantPlacementWindIndicator : Node3D
{
	private const string RibbonShaderPath =
		"res://shaders/plant-placement-wind-ribbons.gdshader";
	private const string SparkShaderPath =
		"res://shaders/plant-placement-aura-sparks.gdshader";
	private const string FillShaderPath =
		"res://shaders/plant-placement-wind-fill.gdshader";
	private const int RibbonCount = 7;
	private const int RibbonSegments = 40;
	private const int SparkCount = 12;
	private const float AuraRadius = 1.18f;

	private static Shader _sharedRibbonShader;
	private static Shader _sharedSparkShader;
	private static Shader _sharedFillShader;
	private static MultiMesh _sharedRibbonMultiMesh;
	private static MultiMesh _sharedSparkMultiMesh;
	private static PlaneMesh _sharedGroundFillMesh;

	private MultiMeshInstance3D _ribbons;
	private MultiMeshInstance3D _sparks;
	private MeshInstance3D _groundFill;
	private ShaderMaterial _ribbonMaterial;
	private ShaderMaterial _sparkMaterial;
	private ShaderMaterial _fillMaterial;
	private Color _effectColor = new Color(0.94f, 0.90f, 0.72f, 1.0f);
	private Color _fillColor = new Color(0.48f, 0.66f, 0.38f, 1.0f);
	private float _opacity = 1.0f;
	private float _emissionStrength = 1.0f;
	private float _fillOpacity;
	private bool _isSetup;

	public override void _Ready()
	{
		Setup();
	}

	public void Setup()
	{
		if (_isSetup)
			return;

		_isSetup = true;
		BuildVisual();
		Visible = false;
	}

	public void Display(
		Color effectColor,
		float opacity = 1.0f,
		float emissionStrength = 1.0f,
		float fillOpacity = 0.0f,
		Color? fillColor = null)
	{
		Setup();
		_effectColor = effectColor;
		_fillColor = fillColor ?? effectColor;
		_opacity = Mathf.Max(opacity, 0.0f);
		_emissionStrength = Mathf.Max(emissionStrength, 0.0f);
		_fillOpacity = Mathf.Max(fillOpacity, 0.0f);
		ApplyStyle();
		Visible = true;
	}

	public void SetIntensity(float opacity, float emissionStrength)
	{
		_opacity = Mathf.Max(opacity, 0.0f);
		_emissionStrength = Mathf.Max(emissionStrength, 0.0f);
		ApplyStyle();
	}

	public void Conceal()
	{
		Visible = false;
	}

	private void BuildVisual()
	{
		Shader ribbonShader = GetRibbonShader();
		Shader sparkShader = GetSparkShader();
		Shader fillShader = GetFillShader();

		if (ribbonShader == null || sparkShader == null || fillShader == null)
		{
			GD.PushWarning(
				"PlantPlacementWindIndicator: Aura-Shader fehlen.");
			return;
		}

		_ribbonMaterial = new ShaderMaterial
		{
			Shader = ribbonShader
		};
		_sparkMaterial = new ShaderMaterial
		{
			Shader = sparkShader
		};
		_fillMaterial = new ShaderMaterial
		{
			Shader = fillShader
		};

		_groundFill = new MeshInstance3D
		{
			Name = "SubtleGroundFill",
			Mesh = GetGroundFillMesh(),
			MaterialOverride = _fillMaterial,
			Position = new Vector3(0.0f, 0.065f, 0.0f),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			ExtraCullMargin = 1.0f
		};
		AddChild(_groundFill);

		_ribbons = new MultiMeshInstance3D
		{
			Name = "RisingAuraRibbons",
			Multimesh = GetRibbonMultiMesh(),
			MaterialOverride = _ribbonMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			ExtraCullMargin = 2.0f
		};
		AddChild(_ribbons);

		_sparks = new MultiMeshInstance3D
		{
			Name = "RisingAuraSparks",
			Multimesh = GetSparkMultiMesh(),
			MaterialOverride = _sparkMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			ExtraCullMargin = 2.0f
		};
		AddChild(_sparks);

		ApplyStyle();
	}

	private void ApplyStyle()
	{
		if (_ribbonMaterial != null)
		{
			_ribbonMaterial.SetShaderParameter("effect_color", _effectColor);
			_ribbonMaterial.SetShaderParameter("opacity", _opacity);
			_ribbonMaterial.SetShaderParameter(
				"emission_strength",
				_emissionStrength);
		}

		if (_sparkMaterial != null)
		{
			_sparkMaterial.SetShaderParameter("effect_color", _effectColor);
			_sparkMaterial.SetShaderParameter("opacity", _opacity * 0.82f);
			_sparkMaterial.SetShaderParameter(
				"emission_strength",
				_emissionStrength * 1.18f);
		}

		if (_fillMaterial != null)
		{
			_fillMaterial.SetShaderParameter("fill_color", _fillColor);
			_fillMaterial.SetShaderParameter("opacity", _fillOpacity);
		}
	}

	private static Shader GetRibbonShader()
	{
		_sharedRibbonShader ??= GD.Load<Shader>(RibbonShaderPath);
		return _sharedRibbonShader;
	}

	private static Shader GetSparkShader()
	{
		_sharedSparkShader ??= GD.Load<Shader>(SparkShaderPath);
		return _sharedSparkShader;
	}

	private static Shader GetFillShader()
	{
		_sharedFillShader ??= GD.Load<Shader>(FillShaderPath);
		return _sharedFillShader;
	}

	private static PlaneMesh GetGroundFillMesh()
	{
		_sharedGroundFillMesh ??= new PlaneMesh
		{
			Size = new Vector2(
				AuraRadius * 2.0f,
				AuraRadius * 2.0f)
		};
		return _sharedGroundFillMesh;
	}

	private static MultiMesh GetRibbonMultiMesh()
	{
		if (_sharedRibbonMultiMesh != null)
			return _sharedRibbonMultiMesh;

		_sharedRibbonMultiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseCustomData = true,
			Mesh = CreateRibbonMesh(),
			InstanceCount = RibbonCount,
			VisibleInstanceCount = -1
		};

		for (int index = 0; index < RibbonCount; index++)
		{
			uint seed = unchecked((uint)index * 0x9E3779B9u) + 0xA511E9B3u;
			float phase = Mathf.PosMod(
				index / (float)RibbonCount +
				(HashToUnit(seed ^ 0x63D83595u) - 0.5f) * 0.055f,
				1.0f);

			_sharedRibbonMultiMesh.SetInstanceTransform(
				index,
				Transform3D.Identity);
			_sharedRibbonMultiMesh.SetInstanceCustomData(
				index,
				new Color(
					phase,
					HashToUnit(seed ^ 0xC2B2AE35u),
					HashToUnit(seed ^ 0x27D4EB2Fu),
					HashToUnit(seed ^ 0x165667B1u)));
		}

		return _sharedRibbonMultiMesh;
	}

	private static MultiMesh GetSparkMultiMesh()
	{
		if (_sharedSparkMultiMesh != null)
			return _sharedSparkMultiMesh;

		ArrayMesh sparkMesh = CreateLeafMesh();
		_sharedSparkMultiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseCustomData = true,
			Mesh = sparkMesh,
			InstanceCount = SparkCount,
			VisibleInstanceCount = -1
		};

		for (int index = 0; index < SparkCount; index++)
		{
			uint seed = unchecked((uint)index * 0x9E3779B9u) + 0xB5297A4Du;
			_sharedSparkMultiMesh.SetInstanceTransform(
				index,
				Transform3D.Identity);
			_sharedSparkMultiMesh.SetInstanceCustomData(
				index,
				new Color(
					HashToUnit(seed ^ 0x68E31DA4u),
					HashToUnit(seed ^ 0x1B56C4E9u),
					HashToUnit(seed ^ 0xC2B2AE35u),
					HashToUnit(seed ^ 0x27D4EB2Fu)));
		}

		return _sharedSparkMultiMesh;
	}

	private static ArrayMesh CreateLeafMesh()
	{
		SurfaceTool surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		AddLeafPlane(surface, Vector3.Right, Vector3.Up);
		AddLeafPlane(surface, new Vector3(0.0f, 0.0f, 1.0f), Vector3.Up);

		return surface.Commit();
	}

	private static void AddLeafPlane(
		SurfaceTool surface,
		Vector3 horizontalAxis,
		Vector3 verticalAxis)
	{
		Vector3 center = verticalAxis * 0.008f;
		Vector3[] outline =
		{
			verticalAxis * -0.056f,
			horizontalAxis * -0.024f + verticalAxis * -0.024f,
			horizontalAxis * -0.038f + verticalAxis * 0.012f,
			horizontalAxis * -0.021f + verticalAxis * 0.048f,
			verticalAxis * 0.070f,
			horizontalAxis * 0.021f + verticalAxis * 0.048f,
			horizontalAxis * 0.038f + verticalAxis * 0.012f,
			horizontalAxis * 0.024f + verticalAxis * -0.024f
		};

		for (int index = 0; index < outline.Length; index++)
		{
			surface.AddVertex(center);
			surface.AddVertex(outline[index]);
			surface.AddVertex(outline[(index + 1) % outline.Length]);
		}
	}

	private static ArrayMesh CreateRibbonMesh()
	{
		SurfaceTool surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		for (int segment = 0; segment < RibbonSegments; segment++)
		{
			float start = segment / (float)RibbonSegments;
			float end = (segment + 1) / (float)RibbonSegments;

			AddRibbonVertex(surface, start, 0.0f);
			AddRibbonVertex(surface, start, 1.0f);
			AddRibbonVertex(surface, end, 1.0f);

			AddRibbonVertex(surface, start, 0.0f);
			AddRibbonVertex(surface, end, 1.0f);
			AddRibbonVertex(surface, end, 0.0f);
		}

		return surface.Commit();
	}

	private static void AddRibbonVertex(
		SurfaceTool surface,
		float along,
		float across)
	{
		surface.SetNormal(Vector3.Up);
		surface.SetUV(new Vector2(along, across));
		surface.AddVertex(new Vector3(along - 0.5f, 0.0f, across - 0.5f));
	}

	private static float HashToUnit(uint value)
	{
		value ^= value >> 16;
		value *= 0x7FEB352Du;
		value ^= value >> 15;
		value *= 0x846CA68Bu;
		value ^= value >> 16;
		return (value & 0x00FFFFFFu) / 16777215.0f;
	}
}
