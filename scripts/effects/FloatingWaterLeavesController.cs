using Godot;
using System;
using System.Collections.Generic;

public partial class FloatingWaterLeavesController : Node3D
{
	private const int MaximumLeafCount = 64;
	private const float HexTileBoundaryOffset = 2f / 3f;

	[ExportGroup("General")]
	[Export] public bool LeavesEnabled { get; set; } = true;

	[Export(PropertyHint.Range, "0,64,1")]
	public int LeafCount { get; set; } = 18;

	[Export] public int LayoutSeed { get; set; } = 1847;
	[Export] public Mesh LeafMesh { get; set; }

	[ExportGroup("Water Area")]
	[Export(PropertyHint.Range, "0,30,0.1")]
	public float InnerRadius { get; set; } = 10.4f;

	[Export(PropertyHint.Range, "0.1,30,0.1")]
	public float OuterRadius { get; set; } = 18.2f;

	[Export]
	public bool UseStoneBorderBounds { get; set; } = true;

	[Export(PropertyHint.Range, "0,2,0.05")]
	public float StoneBorderClearance { get; set; } = 0.45f;

	[Export(PropertyHint.Range, "0,0.35,0.005")]
	public float SurfaceOffset { get; set; } = 0.125f;

	[Export(PropertyHint.Range, "0.1,5,0.05")]
	public float EdgeSteerDistance { get; set; } = 1.25f;

	[Export(PropertyHint.Range, "0,3,0.05")]
	public float EdgeSteerStrength { get; set; } = 1.15f;

	[ExportGroup("Drift")]
	[Export(PropertyHint.Range, "0,2,0.05")]
	public float OverallMotionSpeed { get; set; } = 0.4f;

	[Export(PropertyHint.Range, "-180,180,1")]
	public float FlowDirectionDegrees { get; set; } = 24f;

	[Export(PropertyHint.Range, "0,60,1")]
	public float FlowVariationDegrees { get; set; } = 17f;

	[Export(PropertyHint.Range, "0,1,0.005")]
	public float MinDriftSpeed { get; set; } = 0.045f;

	[Export(PropertyHint.Range, "0,1,0.005")]
	public float MaxDriftSpeed { get; set; } = 0.12f;

	[Export(PropertyHint.Range, "0,70,1")]
	public float MeanderDegrees { get; set; } = 24f;

	[Export(PropertyHint.Range, "0.01,2,0.01")]
	public float MinMeanderSpeed { get; set; } = 0.16f;

	[Export(PropertyHint.Range, "0.01,2,0.01")]
	public float MaxMeanderSpeed { get; set; } = 0.38f;

	[Export(PropertyHint.Range, "0.05,5,0.05")]
	public float TurnSmoothing { get; set; } = 0.75f;

	[ExportGroup("Floating Motion")]
	[Export(PropertyHint.Range, "0,0.1,0.002")]
	public float BobHeight { get; set; } = 0.006f;

	[Export(PropertyHint.Range, "0.05,3,0.05")]
	public float MinBobSpeed { get; set; } = 0.35f;

	[Export(PropertyHint.Range, "0.05,3,0.05")]
	public float MaxBobSpeed { get; set; } = 0.7f;

	[Export(PropertyHint.Range, "0,20,0.5")]
	public float MaxTiltDegrees { get; set; } = 4.5f;

	[Export(PropertyHint.Range, "0,60,0.5")]
	public float MinSpinDegreesPerSecond { get; set; } = 1.5f;

	[Export(PropertyHint.Range, "0,60,0.5")]
	public float MaxSpinDegreesPerSecond { get; set; } = 8f;

	[ExportGroup("Appearance")]
	[Export(PropertyHint.Range, "0.1,3,0.05")]
	public float MinScale { get; set; } = 0.4f;

	[Export(PropertyHint.Range, "0.1,3,0.05")]
	public float MaxScale { get; set; } = 0.75f;

	[Export] public Color TintA { get; set; } = new(0.50f, 0.74f, 0.25f, 1f);
	[Export] public Color TintB { get; set; } = new(0.82f, 0.57f, 0.20f, 1f);

	[ExportGroup("Visibility Culling")]
	[Export] public bool EnableVisibilityCulling { get; set; } = true;

	[Export(PropertyHint.Range, "0,80,1")]
	public float LeafVisibilityRange { get; set; } = 0f;

	[Export(PropertyHint.Range, "0,12,0.5")]
	public float LeafVisibilityMargin { get; set; } = 4f;

	[Export(PropertyHint.Range, "0,12,0.5")]
	public float BehindCameraMargin { get; set; } = 3f;

	private readonly struct WaterBounds
	{
		public WaterBounds(
			bool usesStoneBorders,
			float innerDistance,
			float outerDistance,
			float hexSize)
		{
			UsesStoneBorders = usesStoneBorders;
			InnerDistance = innerDistance;
			OuterDistance = outerDistance;
			HexSize = hexSize;
		}

		public bool UsesStoneBorders { get; }
		public float InnerDistance { get; }
		public float OuterDistance { get; }
		public float HexSize { get; }
	}

	private sealed class LeafState
	{
		public Vector2 Position;
		public float DirectionAngle;
		public float FlowBias;
		public float SpeedFactor;
		public float MeanderFactor;
		public float MeanderPhase;
		public float BobFactor;
		public float BobPhase;
		public float SpinFactor;
		public float SpinDirection;
		public float SpinAngle;
		public float ScaleFactor;
		public float TintFactor;
	}

	private readonly List<LeafState> _leaves = new();
	private readonly RandomNumberGenerator _random = new();
	private MultiMeshInstance3D _leafInstance;
	private BoardManager _boardManager;
	private float _animationTime;
	private int _builtCount = -1;
	private int _builtSeed;
	private Mesh _builtMesh;
	private Color _builtTintA;
	private Color _builtTintB;

	public override void _Ready()
	{
		_boardManager = GetNodeOrNull<BoardManager>("../BoardManager");
		RebuildLeaves();
	}

	public override void _Process(double delta)
	{
		if (NeedsRebuild())
			RebuildLeaves();

		if (_leafInstance == null)
			return;

		_leafInstance.Visible = LeavesEnabled;
		if (!LeavesEnabled)
			return;

		float motionDelta = (float)delta * Mathf.Max(OverallMotionSpeed, 0f);
		_animationTime += motionDelta;
		UpdateLeaves(motionDelta);
	}

	private bool NeedsRebuild()
	{
		return _builtCount != Mathf.Clamp(LeafCount, 0, MaximumLeafCount) ||
			_builtSeed != LayoutSeed ||
			!ReferenceEquals(_builtMesh, LeafMesh) ||
			_builtTintA != TintA ||
			_builtTintB != TintB;
	}

	private void RebuildLeaves()
	{
		if (_leafInstance != null)
		{
			RemoveChild(_leafInstance);
			_leafInstance.QueueFree();
			_leafInstance = null;
		}

		_leaves.Clear();
		_builtCount = Mathf.Clamp(LeafCount, 0, MaximumLeafCount);
		_builtSeed = LayoutSeed;
		_builtMesh = LeafMesh;
		_builtTintA = TintA;
		_builtTintB = TintB;

		if (_builtCount == 0 || LeafMesh == null)
			return;

		MultiMesh multiMesh = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			Mesh = LeafMesh
		};
		multiMesh.InstanceCount = _builtCount;
		multiMesh.VisibleInstanceCount = -1;

		WaterBounds bounds = GetWaterBounds();
		float aabbRadius = GetAabbRadius(bounds);
		_leafInstance = new MultiMeshInstance3D
		{
			Name = "FloatingLeaves",
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			CustomAabb = new Aabb(
				new Vector3(-aabbRadius, -0.5f, -aabbRadius),
				new Vector3(aabbRadius * 2f, 1f, aabbRadius * 2f))
		};
		AddChild(_leafInstance);
		VisibilityRangeUtility.Configure(
			_leafInstance,
			EnableVisibilityCulling,
			endDistance: 0.0f,
			endMargin: 0.0f,
			extraCullMargin: BehindCameraMargin,
			enableRearCameraCulling: false);

		_random.Seed = (ulong)Math.Abs((long)LayoutSeed) + 1UL;
		for (int index = 0; index < _builtCount; index++)
		{
			LeafState state = CreateLeafState(bounds, index, _builtCount);
			_leaves.Add(state);
			multiMesh.SetInstanceColor(index, TintA.Lerp(TintB, state.TintFactor));
		}

		UpdateLeaves(0f);
	}

	private LeafState CreateLeafState(WaterBounds bounds, int index, int leafCount)
	{
		Vector2 position = bounds.UsesStoneBorders
			? CreatePositionInsideStoneBorders(bounds, index, leafCount)
			: CreatePositionInsideCircularBounds(bounds, index, leafCount);
		float tintFactor = index % 2 == 0
			? _random.RandfRange(0f, 0.15f)
			: _random.RandfRange(0.85f, 1f);

		return new LeafState
		{
			Position = position,
			DirectionAngle = Mathf.DegToRad(FlowDirectionDegrees) +
				_random.RandfRange(-Mathf.Pi, Mathf.Pi),
			FlowBias = Mathf.DegToRad(
				_random.RandfRange(-FlowVariationDegrees, FlowVariationDegrees)),
			SpeedFactor = _random.Randf(),
			MeanderFactor = _random.Randf(),
			MeanderPhase = _random.RandfRange(-Mathf.Pi, Mathf.Pi),
			BobFactor = _random.Randf(),
			BobPhase = _random.RandfRange(-Mathf.Pi, Mathf.Pi),
			SpinFactor = _random.Randf(),
			SpinDirection = _random.Randf() < 0.5f ? -1f : 1f,
			SpinAngle = _random.RandfRange(-Mathf.Pi, Mathf.Pi),
			ScaleFactor = _random.Randf(),
			TintFactor = tintFactor
		};
	}

	private Vector2 CreatePositionInsideCircularBounds(
		WaterBounds bounds,
		int index,
		int leafCount)
	{
		float angleStep = Mathf.Tau / Math.Max(leafCount, 1);
		float angle = angleStep * (index + _random.RandfRange(0.2f, 0.8f));
		float channelFactor = ((index % 3) + _random.RandfRange(0.25f, 0.75f)) / 3f;
		float radius = Mathf.Lerp(
			bounds.InnerDistance,
			bounds.OuterDistance,
			channelFactor);
		return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
	}

	private Vector2 CreatePositionInsideStoneBorders(
		WaterBounds bounds,
		int index,
		int leafCount)
	{
		float angleStep = Mathf.Tau / Math.Max(leafCount, 1);
		float angle = angleStep * (index + _random.RandfRange(0.2f, 0.8f));
		Vector2 boardDirection = new(Mathf.Cos(angle), Mathf.Sin(angle));
		Vector2 axialDirection = BoardPlaneToAxial(boardDirection, bounds.HexSize);
		float directionDistance = GetHexDistance(axialDirection.X, axialDirection.Y);
		float channelFactor = ((index % 3) + _random.RandfRange(0.25f, 0.75f)) / 3f;
		float distance = Mathf.Lerp(
			bounds.InnerDistance,
			bounds.OuterDistance,
			channelFactor);
		Vector2 axial = axialDirection * (distance / directionDistance);
		return BoardToControllerPlane(
			AxialToBoardPlane(axial.X, axial.Y, bounds.HexSize));
	}

	private void UpdateLeaves(float delta)
	{
		if (_leafInstance?.Multimesh == null)
			return;

		WaterBounds bounds = GetWaterBounds();
		float flowAngle = Mathf.DegToRad(FlowDirectionDegrees);
		float meanderAmount = Mathf.DegToRad(MeanderDegrees);
		float maxTilt = Mathf.DegToRad(MaxTiltDegrees);
		Camera3D activeCamera = EnableVisibilityCulling
			? GetViewport()?.GetCamera3D()
			: null;

		for (int index = 0; index < _leaves.Count; index++)
		{
			LeafState state = _leaves[index];
			float meanderSpeed = Mathf.Lerp(
				MinMeanderSpeed,
				MaxMeanderSpeed,
				state.MeanderFactor);
			float meander = Mathf.Sin(
				_animationTime * meanderSpeed + state.MeanderPhase) * meanderAmount;
			meander += Mathf.Sin(
				_animationTime * meanderSpeed * 0.37f + state.MeanderPhase * 1.7f) *
				meanderAmount * 0.28f;

			float desiredAngle = flowAngle + state.FlowBias + meander;
			Vector2 desiredDirection = new(
				Mathf.Cos(desiredAngle),
				Mathf.Sin(desiredAngle));
			ApplyEdgeSteering(
				state.Position,
				bounds,
				ref desiredDirection);

			float targetAngle = Mathf.Atan2(desiredDirection.Y, desiredDirection.X);
			state.DirectionAngle = Mathf.LerpAngle(
				state.DirectionAngle,
				targetAngle,
				Mathf.Clamp(TurnSmoothing * delta, 0f, 1f));

			float speed = Mathf.Lerp(MinDriftSpeed, MaxDriftSpeed, state.SpeedFactor);
			state.Position += new Vector2(
				Mathf.Cos(state.DirectionAngle),
				Mathf.Sin(state.DirectionAngle)) * speed * delta;
			ConstrainToWater(state, bounds);

			float spinSpeed = Mathf.Lerp(
				MinSpinDegreesPerSecond,
				MaxSpinDegreesPerSecond,
				state.SpinFactor);
			state.SpinAngle += Mathf.DegToRad(spinSpeed) * state.SpinDirection * delta;

			float bobSpeed = Mathf.Lerp(MinBobSpeed, MaxBobSpeed, state.BobFactor);
			float bobPhase = _animationTime * bobSpeed + state.BobPhase;
			float bob = Mathf.Sin(bobPhase) * BobHeight;
			float tiltX = Mathf.Sin(bobPhase * 0.83f) * maxTilt;
			float tiltZ = Mathf.Cos(bobPhase * 0.61f + state.MeanderPhase) *
				maxTilt * 0.7f;
			float scale = Mathf.Lerp(MinScale, MaxScale, state.ScaleFactor);

			Basis basis = Basis.Identity
				.Rotated(Vector3.Up, state.SpinAngle)
				.Rotated(Vector3.Right, tiltX)
				.Rotated(Vector3.Forward, tiltZ)
				.Scaled(Vector3.One * scale);
			Vector3 leafPosition = new(
				state.Position.X,
				SurfaceOffset + bob,
				state.Position.Y);
			float visibility = GetLeafVisibility(activeCamera, leafPosition);
			if (visibility <= 0.001f)
				basis = basis.Scaled(Vector3.Zero);

			Color tint = TintA.Lerp(TintB, state.TintFactor);
			tint.A *= visibility;
			_leafInstance.Multimesh.SetInstanceColor(index, tint);
			_leafInstance.Multimesh.SetInstanceTransform(
				index,
				new Transform3D(basis, leafPosition));
		}
	}

	private float GetLeafVisibility(Camera3D camera, Vector3 leafPosition)
	{
		if (!EnableVisibilityCulling || camera == null)
			return 1.0f;

		Vector3 toLeaf = ToGlobal(leafPosition) - camera.GlobalPosition;
		float visibility = 1.0f;
		float range = Mathf.Max(LeafVisibilityRange, 0.0f);
		float rangeMargin = Mathf.Max(LeafVisibilityMargin, 0.0f);

		if (range > 0.0f)
		{
			float distance = toLeaf.Length();
			visibility = rangeMargin > 0.0f
				? Mathf.Min(
					visibility,
					Mathf.Clamp(
						(range + rangeMargin - distance) / rangeMargin,
						0.0f,
						1.0f))
				: distance <= range ? visibility : 0.0f;
		}

		float behindMargin = Mathf.Max(BehindCameraMargin, 0.0f);
		Vector3 cameraForward = -camera.GlobalBasis.Z.Normalized();
		float forwardDistance = cameraForward.Dot(toLeaf);
		float cameraVisibility = behindMargin > 0.0f
			? Mathf.Clamp(
				(forwardDistance + behindMargin) / behindMargin,
				0.0f,
				1.0f)
			: forwardDistance >= 0.0f ? 1.0f : 0.0f;

		return Mathf.Min(visibility, cameraVisibility);
	}

	private void ApplyEdgeSteering(
		Vector2 position,
		WaterBounds bounds,
		ref Vector2 desiredDirection)
	{
		float boundaryDistance = GetBoundaryDistance(position, bounds);
		if (boundaryDistance <= 0.0001f)
			return;

		Vector2 radial = GetBoundaryDirection(position, bounds);
		float edgeDistance = bounds.UsesStoneBorders
			? Mathf.Max(EdgeSteerDistance, 0.01f) /
				Mathf.Max(bounds.HexSize * 1.5f, 0.01f)
			: Mathf.Max(EdgeSteerDistance, 0.01f);
		float outerStrength = 1f - Mathf.Clamp(
			(bounds.OuterDistance - boundaryDistance) / edgeDistance,
			0f,
			1f);
		float innerStrength = 1f - Mathf.Clamp(
			(boundaryDistance - bounds.InnerDistance) / edgeDistance,
			0f,
			1f);
		Vector2 steering = -radial * outerStrength + radial * innerStrength;

		if (steering.LengthSquared() > 0.0001f)
			desiredDirection = (desiredDirection + steering * EdgeSteerStrength).Normalized();
	}

	private void ConstrainToWater(LeafState state, WaterBounds bounds)
	{
		if (!bounds.UsesStoneBorders)
		{
			float radius = state.Position.Length();
			if (radius <= 0.0001f)
			{
				state.Position = Vector2.Right * bounds.InnerDistance;
				return;
			}

			float constrainedRadius = Mathf.Clamp(
				radius,
				bounds.InnerDistance,
				bounds.OuterDistance);
			if (!Mathf.IsEqualApprox(radius, constrainedRadius))
				state.Position = state.Position / radius * constrainedRadius;
			return;
		}

		Vector2 boardPosition = ControllerToBoardPlane(state.Position);
		Vector2 axial = BoardPlaneToAxial(boardPosition, bounds.HexSize);
		float distance = GetHexDistance(axial.X, axial.Y);
		if (distance <= 0.0001f)
		{
			state.Position = BoardToControllerPlane(
				AxialToBoardPlane(bounds.InnerDistance, 0f, bounds.HexSize));
			return;
		}

		float constrainedDistance = Mathf.Clamp(
			distance,
			bounds.InnerDistance,
			bounds.OuterDistance);
		if (!Mathf.IsEqualApprox(distance, constrainedDistance))
		{
			axial *= constrainedDistance / distance;
			state.Position = BoardToControllerPlane(
				AxialToBoardPlane(axial.X, axial.Y, bounds.HexSize));
		}
	}

	private WaterBounds GetWaterBounds()
	{
		GetWaterRadii(out float innerRadius, out float outerRadius);
		if (!UseStoneBorderBounds ||
			_boardManager?.Balance == null ||
			_boardManager.Balance.UseRectangularBoard)
		{
			return new WaterBounds(false, innerRadius, outerRadius, 0f);
		}

		float hexSize = Mathf.Max(_boardManager.HexSize, 0.1f);
		float clearance = Mathf.Max(StoneBorderClearance, 0f) /
			(hexSize * 1.5f);
		int boardRadius = Math.Max(_boardManager.Balance.BoardRadius, 1);
		int waterGap = Math.Max(_boardManager.WaterGapRings, 1);
		float innerDistance = boardRadius + HexTileBoundaryOffset + clearance;
		float outerDistance = boardRadius + waterGap + 1f -
			HexTileBoundaryOffset - clearance;

		if (outerDistance <= innerDistance + 0.1f)
			return new WaterBounds(false, innerRadius, outerRadius, 0f);

		return new WaterBounds(true, innerDistance, outerDistance, hexSize);
	}

	private float GetBoundaryDistance(Vector2 position, WaterBounds bounds)
	{
		if (!bounds.UsesStoneBorders)
			return position.Length();

		Vector2 axial = BoardPlaneToAxial(
			ControllerToBoardPlane(position),
			bounds.HexSize);
		return GetHexDistance(axial.X, axial.Y);
	}

	private Vector2 GetBoundaryDirection(Vector2 position, WaterBounds bounds)
	{
		if (!bounds.UsesStoneBorders)
			return position.Normalized();

		Vector2 boardPosition = ControllerToBoardPlane(position);
		if (boardPosition.LengthSquared() <= 0.0001f)
			return Vector2.Right;

		Vector2 localOrigin = BoardToControllerPlane(Vector2.Zero);
		Vector2 localDirectionPoint = BoardToControllerPlane(
			boardPosition.Normalized());
		return (localDirectionPoint - localOrigin).Normalized();
	}

	private Vector2 ControllerToBoardPlane(Vector2 position)
	{
		Vector3 boardPosition = _boardManager.ToLocal(
			ToGlobal(new Vector3(position.X, 0f, position.Y)));
		return new Vector2(boardPosition.X, boardPosition.Z);
	}

	private Vector2 BoardToControllerPlane(Vector2 position)
	{
		Vector3 controllerPosition = ToLocal(
			_boardManager.ToGlobal(new Vector3(position.X, 0f, position.Y)));
		return new Vector2(controllerPosition.X, controllerPosition.Z);
	}

	private static Vector2 BoardPlaneToAxial(Vector2 position, float hexSize)
	{
		float q = position.X / (1.5f * hexSize);
		float r = position.Y / (Mathf.Sqrt(3f) * hexSize) - q * 0.5f;
		return new Vector2(q, r);
	}

	private static Vector2 AxialToBoardPlane(float q, float r, float hexSize)
	{
		return new Vector2(
			hexSize * 1.5f * q,
			hexSize * Mathf.Sqrt(3f) * (r + q * 0.5f));
	}

	private static float GetHexDistance(float q, float r)
	{
		return Mathf.Max(
			Mathf.Max(Mathf.Abs(q), Mathf.Abs(r)),
			Mathf.Abs(-q - r));
	}

	private static float GetAabbRadius(WaterBounds bounds)
	{
		if (!bounds.UsesStoneBorders)
			return bounds.OuterDistance;

		return bounds.OuterDistance * Mathf.Sqrt(3f) * bounds.HexSize + 0.5f;
	}

	private void GetWaterRadii(out float innerRadius, out float outerRadius)
	{
		innerRadius = Mathf.Max(0f, Mathf.Min(InnerRadius, OuterRadius));
		outerRadius = Mathf.Max(
			innerRadius + 0.1f,
			Mathf.Max(InnerRadius, OuterRadius));
	}
}
