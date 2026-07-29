using Godot;
using System.Collections.Generic;

public partial class GhibliBush : Node3D
{
	private const string DefaultModelPath =
		"res://assets/models/Test/Ghibli_Tree.glb";
	private const string DefaultMaterialPath =
		"res://assets/materials/ghibli_bush.tres";

	[ExportGroup("Source")]
	[Export] public PackedScene SourceScene;
	[Export] public Material BushMaterial;

	[ExportGroup("Visual")]
	[Export(PropertyHint.Range, "0,2,1")]
	public int SourceMeshIndex = 0;

	[Export(PropertyHint.Range, "0.01,1.0,0.01")]
	public float VisualScale = 0.26f;

	[Export(PropertyHint.Range, "-1.0,1.0,0.01")]
	public float GroundOffset = 0.02f;

	[Export(PropertyHint.Range, "-1.0,1.0,0.01")]
	public float ColorVariation = 0.0f;

	[Export] public bool CastShadow = false;

	public override void _Ready()
	{
		SourceScene ??= GD.Load<PackedScene>(DefaultModelPath);
		BushMaterial ??= GD.Load<Material>(DefaultMaterialPath);

		CreateBushVisual();
	}

	private void CreateBushVisual()
	{
		if (SourceScene == null)
		{
			GD.PushError("GhibliBush: Source model could not be loaded.");
			return;
		}

		Node3D sourceRoot = SourceScene.Instantiate<Node3D>();
		List<MeshInstance3D> sourceMeshes = new List<MeshInstance3D>();
		CollectMeshes(sourceRoot, sourceMeshes);

		if (sourceMeshes.Count == 0)
		{
			GD.PushError("GhibliBush: Source model contains no mesh.");
			sourceRoot.Free();
			return;
		}

		int meshIndex = Mathf.Clamp(
			SourceMeshIndex,
			0,
			sourceMeshes.Count - 1);
		Mesh sourceMesh = sourceMeshes[meshIndex].Mesh;

		if (sourceMesh == null)
		{
			GD.PushError(
				$"GhibliBush: Mesh at index {meshIndex} is missing.");
			sourceRoot.Free();
			return;
		}

		Aabb bounds = sourceMesh.GetAabb();
		Vector3 center = bounds.GetCenter();
		float scale = Mathf.Max(VisualScale, 0.01f);

		MeshInstance3D visual = new MeshInstance3D
		{
			Name = "BushVisual",
			Mesh = sourceMesh,
			MaterialOverride = CreateMaterial(),
			Scale = Vector3.One * scale,
			Position = new Vector3(
				-center.X * scale,
				-bounds.Position.Y * scale + GroundOffset,
				-center.Z * scale),
			CastShadow = CastShadow
				? GeometryInstance3D.ShadowCastingSetting.On
				: GeometryInstance3D.ShadowCastingSetting.Off
		};

		AddChild(visual);
		sourceRoot.Free();
	}

	private Material CreateMaterial()
	{
		if (BushMaterial is not ShaderMaterial shaderMaterial)
			return BushMaterial;

		ShaderMaterial instanceMaterial =
			shaderMaterial.Duplicate() as ShaderMaterial;

		if (instanceMaterial == null)
			return BushMaterial;

		instanceMaterial.SetShaderParameter(
			"instance_variation",
			Mathf.Clamp(ColorVariation, -1.0f, 1.0f));
		return instanceMaterial;
	}

	private static void CollectMeshes(
		Node node,
		List<MeshInstance3D> meshes)
	{
		if (node is MeshInstance3D mesh)
			meshes.Add(mesh);

		foreach (Node child in node.GetChildren())
			CollectMeshes(child, meshes);
	}
}
