using Godot;

public partial class FallingLeavesController : GpuParticles3D
{
	private const float HexTileBoundaryOffset = 2.0f / 3.0f;

	[Export(PropertyHint.Range, "0,2,0.05")]
	public float StoneBorderClearance { get; set; } = 0.45f;

	public override void _Ready()
	{
		ConfigureLandingMaterial();
	}

	private void ConfigureLandingMaterial()
	{
		if (ProcessMaterial is not ShaderMaterial sourceMaterial)
			return;

		ShaderMaterial material = sourceMaterial.Duplicate(true) as ShaderMaterial;
		if (material == null)
			return;

		ProcessMaterial = material;

		Transform3D particleToWorld = GlobalTransform;
		material.SetShaderParameter("particle_to_world", particleToWorld);
		material.SetShaderParameter(
			"world_to_particle",
			particleToWorld.AffineInverse());

		float fallbackGroundHeight = GetParent() is Node3D parent
			? parent.GlobalPosition.Y
			: GlobalPosition.Y;
		material.SetShaderParameter("landing_enabled", true);
		material.SetShaderParameter("water_bounds_enabled", false);
		material.SetShaderParameter("inner_ground_height", fallbackGroundHeight);
		material.SetShaderParameter("outer_ground_height", fallbackGroundHeight);

		BoardManager boardManager = FindBoardManager();
		GameConfig balance = boardManager?.Balance;
		if (boardManager == null || balance == null || balance.UseRectangularBoard)
			return;

		float hexSize = Mathf.Max(boardManager.HexSize, 0.1f);
		float clearance = Mathf.Max(StoneBorderClearance, 0.0f) /
			(hexSize * 1.5f);
		int boardRadius = System.Math.Max(balance.BoardRadius, 1);
		int waterGap = System.Math.Max(boardManager.WaterGapRings, 1);
		float innerWaterDistance =
			boardRadius + HexTileBoundaryOffset + clearance;
		float outerWaterDistance =
			boardRadius + waterGap + 1.0f -
			HexTileBoundaryOffset - clearance;

		if (outerWaterDistance <= innerWaterDistance + 0.1f)
			return;

		Node3D water = boardManager.GetParent()?.GetNodeOrNull<Node3D>(
			"StylizedWater");
		float waterHeight = water?.GlobalPosition.Y ??
			boardManager.GlobalPosition.Y - 0.13f;
		float innerGroundHeight = boardManager.GlobalPosition.Y;
		float outerGroundHeight = boardManager.ToGlobal(
			Vector3.Up * boardManager.DecorativeGroundHeight).Y;

		material.SetShaderParameter(
			"world_to_board",
			boardManager.GlobalTransform.AffineInverse());
		material.SetShaderParameter("board_hex_size", hexSize);
		material.SetShaderParameter(
			"water_hex_bounds",
			new Vector2(innerWaterDistance, outerWaterDistance));
		material.SetShaderParameter("water_height", waterHeight);
		material.SetShaderParameter("inner_ground_height", innerGroundHeight);
		material.SetShaderParameter("outer_ground_height", outerGroundHeight);
		material.SetShaderParameter("water_bounds_enabled", true);
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
}
