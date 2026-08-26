using Godot;

public static class VisibilityRangeUtility
{
	public static void Configure(
		Node root,
		bool enabled,
		float endDistance,
		float endMargin,
		float extraCullMargin,
		bool enableRearCameraCulling = true)
	{
		ConfigureGeometryRanges(
			root,
			enabled,
			endDistance,
			endMargin,
			extraCullMargin);

		if (root is Node3D root3D)
		{
			CameraRearCullingManager.Configure(
				root3D,
				enabled && enableRearCameraCulling,
				extraCullMargin);
		}
	}

	private static void ConfigureGeometryRanges(
		Node root,
		bool enabled,
		float endDistance,
		float endMargin,
		float extraCullMargin)
	{
		if (root is GeometryInstance3D geometry)
		{
			float rangeEnd = enabled ? Mathf.Max(endDistance, 0.0f) : 0.0f;
			geometry.VisibilityRangeEnd = rangeEnd;
			geometry.VisibilityRangeEndMargin = rangeEnd > 0.0f
				? Mathf.Clamp(endMargin, 0.0f, rangeEnd)
				: 0.0f;
			geometry.VisibilityRangeFadeMode =
				GeometryInstance3D.VisibilityRangeFadeModeEnum.Disabled;

			if (enabled)
			{
				geometry.ExtraCullMargin = Mathf.Max(
					geometry.ExtraCullMargin,
					Mathf.Max(extraCullMargin, 0.0f));
			}
		}

		foreach (Node child in root.GetChildren())
		{
			ConfigureGeometryRanges(
				child,
				enabled,
				endDistance,
				endMargin,
				extraCullMargin);
		}
	}
}
