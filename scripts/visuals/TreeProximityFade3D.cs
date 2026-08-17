using Godot;
using System.Collections.Generic;

public partial class TreeProximityFade3D : Node
{
	private const float FullFadeProximityStart = 0.75f;
	private readonly List<GeometryState> _geometryStates = new();
	private Vector3 _boundsMinimum;
	private Vector3 _boundsMaximum;
	private Vector3 _canopyBoundsMinimum;
	private Vector3 _canopyBoundsMaximum;
	private float _currentFade;
	private bool _hasBounds;
	private bool _hasCanopyBounds;
	private float _lastAppliedFade = -1.0f;
	private bool _lastAppliedFullyHidden;

	public float FadeStartDistance { get; set; } = 3.0f;
	public float FadeFullDistance { get; set; } = 0.6f;
	public float MaximumTransparency { get; set; } = 0.8f;
	public float FadeSpeed { get; set; } = 1.2f;

	private readonly struct GeometryState
	{
		public GeometryInstance3D Geometry { get; }
		public float BaseTransparency { get; }
		public bool BaseVisible { get; }
		public bool UsesCanopyShaderFade { get; }

		public GeometryState(
			GeometryInstance3D geometry,
			float baseTransparency,
			bool baseVisible,
			bool usesCanopyShaderFade)
		{
			Geometry = geometry;
			BaseTransparency = baseTransparency;
			BaseVisible = baseVisible;
			UsesCanopyShaderFade = usesCanopyShaderFade;
		}
	}

	public override void _Ready()
	{
		if (GetParent() is Node3D treeRoot)
			CollectGeometry(treeRoot);

		SetProcess(_hasBounds && _geometryStates.Count > 0);
	}

	public override void _Process(double delta)
	{
		Camera3D camera = GetViewport()?.GetCamera3D();
		bool fullyHidden = camera != null &&
			IsInsideCanopyBounds(camera.GlobalPosition);
		float targetFade = camera == null
			? 0.0f
			: GetTargetFade(camera.GlobalPosition);

		_currentFade = Mathf.MoveToward(
			_currentFade,
			targetFade,
			Mathf.Max(FadeSpeed, 0.01f) * (float)delta);

		ApplyFade(_currentFade, fullyHidden);
	}

	private void CollectGeometry(Node node)
	{
		CollectGeometry(node, false);
	}

	private void CollectGeometry(Node node, bool isInsideCanopy)
	{
		bool isCanopyNode =
			isInsideCanopy || node.Name.ToString() == "BirchLeaves";

		foreach (Node child in node.GetChildren())
		{
			bool isCanopyChild =
				isCanopyNode || child.Name.ToString() == "BirchLeaves";

			if (child is GeometryInstance3D geometry)
			{
				_geometryStates.Add(new GeometryState(
					geometry,
					geometry.Transparency,
					geometry.Visible,
					isCanopyChild));

				if (geometry.IsVisibleInTree())
					IncludeGeometryBounds(geometry, isCanopyChild);
			}

			CollectGeometry(child, isCanopyChild);
		}
	}

	private void IncludeGeometryBounds(
		GeometryInstance3D geometry,
		bool includeInCanopyBounds)
	{
		Aabb localBounds = geometry.GetAabb();
		Vector3 origin = localBounds.Position;
		Vector3 size = localBounds.Size;

		if (size.LengthSquared() <= 0.000001f)
			return;

		for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
		{
			Vector3 localCorner = origin + new Vector3(
				(cornerIndex & 1) == 0 ? 0.0f : size.X,
				(cornerIndex & 2) == 0 ? 0.0f : size.Y,
				(cornerIndex & 4) == 0 ? 0.0f : size.Z);
			Vector3 worldCorner = geometry.GlobalTransform * localCorner;

			if (!_hasBounds)
			{
				_boundsMinimum = worldCorner;
				_boundsMaximum = worldCorner;
				_hasBounds = true;
			}
			else
			{
				_boundsMinimum = _boundsMinimum.Min(worldCorner);
				_boundsMaximum = _boundsMaximum.Max(worldCorner);
			}

			if (!includeInCanopyBounds)
				continue;

			if (!_hasCanopyBounds)
			{
				_canopyBoundsMinimum = worldCorner;
				_canopyBoundsMaximum = worldCorner;
				_hasCanopyBounds = true;
			}
			else
			{
				_canopyBoundsMinimum = _canopyBoundsMinimum.Min(worldCorner);
				_canopyBoundsMaximum = _canopyBoundsMaximum.Max(worldCorner);
			}
		}
	}

	private float GetTargetFade(Vector3 cameraPosition)
	{
		if (IsInsideCanopyBounds(cameraPosition))
			return 1.0f;

		Vector3 closestPoint = new Vector3(
			Mathf.Clamp(cameraPosition.X, _boundsMinimum.X, _boundsMaximum.X),
			Mathf.Clamp(cameraPosition.Y, _boundsMinimum.Y, _boundsMaximum.Y),
			Mathf.Clamp(cameraPosition.Z, _boundsMinimum.Z, _boundsMaximum.Z));
		float distance = cameraPosition.DistanceTo(closestPoint);
		float fullDistance = Mathf.Max(FadeFullDistance, 0.0f);
		float startDistance = Mathf.Max(FadeStartDistance, fullDistance + 0.01f);
		float proximity = Mathf.Clamp(
			(startDistance - distance) / (startDistance - fullDistance),
			0.0f,
			1.0f);
		float smoothedProximity =
			proximity * proximity * (3.0f - 2.0f * proximity);
		float configuredFade = smoothedProximity *
			Mathf.Clamp(MaximumTransparency, 0.0f, 0.8f);
		float fullFadeProgress = Mathf.Clamp(
			(proximity - FullFadeProximityStart) /
			(1.0f - FullFadeProximityStart),
			0.0f,
			1.0f);
		float smoothedFullFadeProgress =
			fullFadeProgress * fullFadeProgress *
			(3.0f - 2.0f * fullFadeProgress);

		return Mathf.Lerp(
			configuredFade,
			1.0f,
			smoothedFullFadeProgress);
	}

	private bool IsInsideCanopyBounds(Vector3 position)
	{
		return _hasCanopyBounds &&
			position.X >= _canopyBoundsMinimum.X &&
			position.X <= _canopyBoundsMaximum.X &&
			position.Y >= _canopyBoundsMinimum.Y &&
			position.Y <= _canopyBoundsMaximum.Y &&
			position.Z >= _canopyBoundsMinimum.Z &&
			position.Z <= _canopyBoundsMaximum.Z;
	}

	private void ApplyFade(float fade, bool fullyHidden)
	{
		if (Mathf.IsEqualApprox(_lastAppliedFade, fade) &&
			_lastAppliedFullyHidden == fullyHidden)
			return;

		_lastAppliedFade = fade;
		_lastAppliedFullyHidden = fullyHidden;

		foreach (GeometryState state in _geometryStates)
		{
			if (!GodotObject.IsInstanceValid(state.Geometry))
				continue;

			state.Geometry.Visible =
				state.BaseVisible && !fullyHidden;

			if (fullyHidden)
				continue;

			if (state.UsesCanopyShaderFade)
			{
				state.Geometry.SetInstanceShaderParameter(
					"birch_proximity_fade",
					fade);
				state.Geometry.Transparency =
					state.BaseTransparency;
				continue;
			}

			state.Geometry.Transparency = Mathf.Lerp(
				state.BaseTransparency,
				1.0f,
				fade);
		}
	}
}
