using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class StylizedBridgeController : Node3D
{
	private const float HexEdgeBoundaryOffset = 0.5f;
	private const float PlankDepthToSpacing = 0.88f;
	private const int MaximumPlankCount = 48;
	private const int MaximumPostPairCount = 12;
	private const string PainterlyWoodShaderPath =
		"res://shaders/stylized_bridge_wood.gdshader";
	private const string EditorPreviewNodePath = "EditorPreview";

	[ExportGroup("Connections")]
	[Export] public NodePath BoardManagerPath =
		new NodePath("../BoardManager");

	[ExportGroup("Placement")]
	[Export] public bool BridgeEnabled = true;
	[Export] public bool AutomaticPlacement = true;

	[Export(PropertyHint.Range, "0,360,1")]
	public float DirectionDegrees = 315.0f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float InnerFieldClearance = 0.12f;

	[Export(PropertyHint.Range, "0,2,0.05")]
	public float OuterShoreOverlap = 0.45f;

	[Export(PropertyHint.Range, "0,1.5,0.05")]
	public float LandingClearance = 0.55f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float DeckElevation = 0.18f;

	[ExportGroup("Deck")]
	[Export(PropertyHint.Range, "0.8,4,0.05")]
	public float BridgeWidth = 2.15f;

	[Export(PropertyHint.Range, "6,48,1")]
	public int PlankCount = 18;

	[Export(PropertyHint.Range, "0.05,0.4,0.01")]
	public float PlankThickness = 0.16f;

	[Export(PropertyHint.Range, "0,1.5,0.05")]
	public float ArchHeight = 0.48f;

	[Export(PropertyHint.Range, "0,0.15,0.005")]
	public float PlankPositionJitter = 0.035f;

	[Export(PropertyHint.Range, "0,8,0.25")]
	public float PlankRotationJitterDegrees = 2.5f;

	[Export] public int LayoutSeed = 9271;
	[Export] public Color PlankColorA = new(0.36f, 0.16f, 0.055f);
	[Export] public Color PlankColorB = new(0.62f, 0.34f, 0.11f);

	[ExportGroup("Railings")]
	[Export] public bool ShowRailings = true;

	[Export(PropertyHint.Range, "2,12,1")]
	public int PostPairCount = 6;

	[Export(PropertyHint.Range, "0.3,1.8,0.05")]
	public float PostHeight = 0.82f;

	[Export(PropertyHint.Range, "0.05,0.35,0.01")]
	public float PostThickness = 0.14f;

	[Export(PropertyHint.Range, "0.05,0.35,0.01")]
	public float BeamThickness = 0.13f;

	[Export(PropertyHint.Range, "0.01,0.12,0.005")]
	public float RopeRadius = 0.035f;

	[Export(PropertyHint.Range, "0,0.5,0.01")]
	public float RopeSag = 0.13f;

	[Export(PropertyHint.Range, "1,6,1")]
	public int RopeSegmentsPerSpan = 3;

	[Export] public Color StructureColorA = new(0.20f, 0.075f, 0.025f);
	[Export] public Color StructureColorB = new(0.33f, 0.13f, 0.04f);
	[Export] public Color RopeColor = new(0.70f, 0.48f, 0.23f);

	private readonly RandomNumberGenerator _random = new();
	private readonly List<Node> _generatedNodes = new();
	private bool _isRebuilding;

	public override void _Ready()
	{
		Node3D editorPreview =
			GetNodeOrNull<Node3D>(EditorPreviewNodePath);

		if (Engine.IsEditorHint())
		{
			if (editorPreview != null)
				editorPreview.Visible = BridgeEnabled;
			return;
		}

		if (editorPreview != null)
			editorPreview.Visible = false;

		RebuildVisuals();
	}

	private void RebuildVisuals()
	{
		_isRebuilding = true;
		ClearGeneratedNodes();
		Visible = BridgeEnabled;
		if (!BridgeEnabled)
		{
			_isRebuilding = false;
			return;
		}

		BoardManager boardManager =
			GetNodeOrNull<BoardManager>(BoardManagerPath);
		GameConfig balance = boardManager?.Balance;

		if (boardManager == null || balance == null)
		{
			if (!Engine.IsEditorHint())
			{
				Visible = false;
				GD.PushWarning(
					$"{Name}: BoardManager oder Balance-Resource fehlt.");
			}
			_isRebuilding = false;
			return;
		}

		if (balance.UseRectangularBoard ||
			!boardManager.ShowDecorativeOuterRing)
		{
			Visible = false;
			_isRebuilding = false;
			return;
		}

		float bridgeLength = AutomaticPlacement
			? ConfigurePlacement(boardManager, balance)
			: CalculateBridgeLength(
				boardManager,
				balance,
				GetBoardDirectionFromTransform(boardManager));
		BuildDeck(bridgeLength);
		BuildStructure(bridgeLength);

		if (ShowRailings)
			BuildRopes(bridgeLength);

		_isRebuilding = false;
	}

	internal bool ContainsLandingFootprint(
		BoardManager boardManager,
		Vector3 boardLocalPosition,
		float additionalClearance)
	{
		GameConfig balance = boardManager?.Balance;
		if (!BridgeEnabled ||
			boardManager == null ||
			balance == null ||
			balance.UseRectangularBoard ||
			!boardManager.ShowDecorativeOuterRing)
		{
			return false;
		}

		float clearance = Mathf.Max(
			LandingClearance + additionalClearance,
			0.0f);
		float halfWidth = Mathf.Max(BridgeWidth, 0.1f) * 0.5f + clearance;

		if (AutomaticPlacement)
		{
			Vector2 boardDirection = GetAutomaticBoardDirection();
			GetPlacementDistances(
				boardManager,
				balance,
				boardDirection,
				out float innerDeckEdge,
				out float outerDeckEdge);
			Vector2 point = new(boardLocalPosition.X, boardLocalPosition.Z);
			float along = point.Dot(boardDirection);
			float lateral = Mathf.Abs(
				point.X * boardDirection.Y - point.Y * boardDirection.X);

			return along >= innerDeckEdge - clearance &&
				along <= outerDeckEdge + clearance &&
				lateral <= halfWidth;
		}

		Vector2 manualDirection = GetBoardDirectionFromTransform(boardManager);
		float bridgeLength = CalculateBridgeLength(
			boardManager,
			balance,
			manualDirection);
		int plankCount = Mathf.Clamp(PlankCount, 6, MaximumPlankCount);
		float coverageFactor = 1.0f +
			PlankDepthToSpacing / Mathf.Max(plankCount - 1, 1);
		float halfLength = bridgeLength * coverageFactor * 0.5f + clearance;
		Vector3 bridgeLocalPosition = ToLocal(
			boardManager.ToGlobal(boardLocalPosition));

		return Mathf.Abs(bridgeLocalPosition.Z) <= halfLength &&
			Mathf.Abs(bridgeLocalPosition.X) <= halfWidth;
	}

	private float ConfigurePlacement(
		BoardManager boardManager,
		GameConfig balance)
	{
		Vector2 boardDirection = GetAutomaticBoardDirection();
		GetPlacementDistances(
			boardManager,
			balance,
			boardDirection,
			out float innerDeckEdge,
			out float outerDeckEdge);
		float bridgeLength = CalculateBridgeLength(
			boardManager,
			balance,
			boardDirection);
		float centerDistance = (innerDeckEdge + outerDeckEdge) * 0.5f;
		Vector3 boardCenter = new(
			boardDirection.X * centerDistance,
			0.0f,
			boardDirection.Y * centerDistance);

		Vector3 globalDirection = boardManager.GlobalTransform.Basis *
			new Vector3(boardDirection.X, 0.0f, boardDirection.Y);
		globalDirection.Y = 0.0f;
		globalDirection = globalDirection.Normalized();
		Vector3 sideAxis = Vector3.Up.Cross(globalDirection).Normalized();
		Basis bridgeBasis = new(sideAxis, Vector3.Up, globalDirection);
		Vector3 bridgeOrigin = boardManager.ToGlobal(boardCenter);
		float innerGroundHeight = boardManager.GlobalPosition.Y;
		float outerGroundHeight = boardManager.ToGlobal(
			Vector3.Up * boardManager.DecorativeGroundHeight).Y;
		bridgeOrigin.Y = Mathf.Max(innerGroundHeight, outerGroundHeight) +
			Mathf.Max(DeckElevation, 0.0f);
		GlobalTransform = new Transform3D(bridgeBasis, bridgeOrigin);

		return bridgeLength;
	}

	private void GetPlacementDistances(
		BoardManager boardManager,
		GameConfig balance,
		Vector2 boardDirection,
		out float innerDeckEdge,
		out float outerDeckEdge)
	{
		boardDirection = boardDirection.Normalized();
		float hexSize = Mathf.Max(boardManager.HexSize, 0.1f);
		Vector2 axialDirection = BoardPlaneToAxial(
			boardDirection,
			hexSize);
		float distancePerWorldUnit = Mathf.Max(
			GetHexDistance(axialDirection.X, axialDirection.Y),
			0.001f);
		int boardRadius = Math.Max(balance.BoardRadius, 1);
		int waterGap = Math.Max(boardManager.WaterGapRings, 1);
		float innerShoreDistance =
			(boardRadius + HexEdgeBoundaryOffset) /
			distancePerWorldUnit;
		float outerShoreDistance =
			(boardRadius + waterGap + HexEdgeBoundaryOffset) /
			distancePerWorldUnit;
		innerDeckEdge = innerShoreDistance +
			Mathf.Max(InnerFieldClearance, 0.0f);
		outerDeckEdge = outerShoreDistance +
			Mathf.Max(OuterShoreOverlap, 0.0f);
	}

	private float CalculateBridgeLength(
		BoardManager boardManager,
		GameConfig balance,
		Vector2 boardDirection)
	{
		GetPlacementDistances(
			boardManager,
			balance,
			boardDirection,
			out float innerDeckEdge,
			out float outerDeckEdge);
		int plankCount = Mathf.Clamp(PlankCount, 6, MaximumPlankCount);
		float deckCoverageLength = Mathf.Max(
			outerDeckEdge - innerDeckEdge,
			1.0f);
		float coverageFactor = 1.0f +
			PlankDepthToSpacing / Mathf.Max(plankCount - 1, 1);

		return deckCoverageLength / coverageFactor;
	}

	private Vector2 GetAutomaticBoardDirection()
	{
		float directionRadians = Mathf.DegToRad(DirectionDegrees);
		return new Vector2(
			Mathf.Cos(directionRadians),
			Mathf.Sin(directionRadians)).Normalized();
	}

	private Vector2 GetBoardDirectionFromTransform(BoardManager boardManager)
	{
		Vector3 globalDirection = GlobalTransform.Basis.Z;
		globalDirection.Y = 0.0f;
		if (globalDirection.IsZeroApprox())
			return GetAutomaticBoardDirection();

		Vector3 boardDirection = boardManager.GlobalTransform.Basis.Inverse() *
			globalDirection.Normalized();
		Vector2 direction = new(boardDirection.X, boardDirection.Z);

		return direction.IsZeroApprox()
			? GetAutomaticBoardDirection()
			: direction.Normalized();
	}

	private void ClearGeneratedNodes()
	{
		foreach (Node node in _generatedNodes)
		{
			if (!IsInstanceValid(node))
				continue;

			if (node.GetParent() == this)
				RemoveChild(node);
			node.QueueFree();
		}

		_generatedNodes.Clear();
	}

	private void RegisterGeneratedNode(Node node)
	{
		_generatedNodes.Add(node);
	}

	private void BuildDeck(float bridgeLength)
	{
		int plankCount = Mathf.Clamp(PlankCount, 6, MaximumPlankCount);
		float plankDepth = bridgeLength /
			Mathf.Max(plankCount - 1, 1) * PlankDepthToSpacing;
		Material plankMaterial = CreatePainterlyMaterial(
			Colors.White,
			0.86f,
			7.2f,
			0.24f,
			0.18f,
			0.32f,
			0.20f);
		BoxMesh plankMesh = new()
		{
			Size = new Vector3(
				Mathf.Max(BridgeWidth, 0.1f),
				Mathf.Max(PlankThickness, 0.01f),
				Mathf.Max(plankDepth, 0.04f)),
			Material = plankMaterial
		};
		MultiMesh multiMesh = CreateMultiMesh(plankMesh, plankCount, true);
		_random.Seed = (ulong)Math.Abs((long)LayoutSeed) + 1UL;

		for (int index = 0; index < plankCount; index++)
		{
			float progress = index / (float)(plankCount - 1);
			float z = Mathf.Lerp(-bridgeLength * 0.5f,
				bridgeLength * 0.5f,
				progress);
			float pitch = GetDeckPitch(progress, bridgeLength);
			float yaw = Mathf.DegToRad(_random.RandfRange(
				-PlankRotationJitterDegrees,
				PlankRotationJitterDegrees));
			float roll = Mathf.DegToRad(_random.RandfRange(
				-PlankRotationJitterDegrees * 0.45f,
				PlankRotationJitterDegrees * 0.45f));
			Basis basis = new Basis(Vector3.Right, pitch)
				.Rotated(Vector3.Up, yaw)
				.Rotated(Vector3.Forward, roll)
				.Scaled(new Vector3(
					_random.RandfRange(0.94f, 1.06f),
					_random.RandfRange(0.92f, 1.08f),
					_random.RandfRange(0.92f, 1.04f)));
			Vector3 position = new(
				_random.RandfRange(
					-PlankPositionJitter,
					PlankPositionJitter),
				GetDeckHeight(progress) +
					_random.RandfRange(
						-PlankPositionJitter * 0.25f,
						PlankPositionJitter * 0.25f),
				z);

			multiMesh.SetInstanceTransform(
				index,
				new Transform3D(basis, position));
			multiMesh.SetInstanceColor(
				index,
				PlankColorA.Lerp(PlankColorB, _random.Randf()));
		}

		AddMultiMesh("BridgeDeckPlanks", multiMesh);
	}

	private void BuildStructure(float bridgeLength)
	{
		int postPairs = Mathf.Clamp(
			PostPairCount,
			2,
			MaximumPostPairCount);
		int beamSegments = Mathf.Max(postPairs * 2, 6);
		List<Transform3D> transforms = new();
		List<Color> colors = new();
		float beamOffset = Mathf.Max(BridgeWidth, 0.1f) * 0.36f;
		float beamThickness = Mathf.Max(BeamThickness, 0.02f);

		for (int segment = 0; segment < beamSegments; segment++)
		{
			float startProgress = segment / (float)beamSegments;
			float endProgress = (segment + 1) / (float)beamSegments;

			for (int side = -1; side <= 1; side += 2)
			{
				Vector3 start = GetBridgePoint(
					bridgeLength,
					startProgress,
					side * beamOffset,
					-PlankThickness * 0.85f);
				Vector3 end = GetBridgePoint(
					bridgeLength,
					endProgress,
					side * beamOffset,
					-PlankThickness * 0.85f);
				transforms.Add(CreateBoxSegmentTransform(
					start,
					end,
					beamThickness));
				colors.Add(StructureColorA.Lerp(
					StructureColorB,
					(segment % 3) / 2.0f));
			}
		}

		float postHeight = Mathf.Max(PostHeight, 0.1f);
		float postThickness = Mathf.Max(PostThickness, 0.02f);
		float postOffset = Mathf.Max(BridgeWidth, 0.1f) * 0.5f -
			postThickness * 0.55f;

		if (ShowRailings)
		{
			for (int postIndex = 0; postIndex < postPairs; postIndex++)
			{
				float progress = postIndex / (float)(postPairs - 1);
				float z = Mathf.Lerp(-bridgeLength * 0.5f,
					bridgeLength * 0.5f,
					progress);

				for (int side = -1; side <= 1; side += 2)
				{
					Basis postBasis = Basis.Identity.Scaled(new Vector3(
						postThickness,
						postHeight,
						postThickness));
					Vector3 postPosition = new(
						side * postOffset,
						GetDeckHeight(progress) + postHeight * 0.5f,
						z);
					transforms.Add(new Transform3D(
						postBasis,
						postPosition));
					colors.Add(StructureColorA.Lerp(
						StructureColorB,
						postIndex / (float)(postPairs - 1)));
				}
			}
		}

		Material structureMaterial = CreatePainterlyMaterial(
			Colors.White,
			0.92f,
			4.6f,
			0.18f,
			0.12f,
			0.38f,
			0.14f);
		BoxMesh structureMesh = new()
		{
			Size = Vector3.One,
			Material = structureMaterial
		};
		MultiMesh multiMesh = CreateMultiMesh(
			structureMesh,
			transforms.Count,
			true);

		for (int index = 0; index < transforms.Count; index++)
		{
			multiMesh.SetInstanceTransform(index, transforms[index]);
			multiMesh.SetInstanceColor(index, colors[index]);
		}

		AddMultiMesh("BridgeStructure", multiMesh);
	}

	private void BuildRopes(float bridgeLength)
	{
		int postPairs = Mathf.Clamp(
			PostPairCount,
			2,
			MaximumPostPairCount);
		int ropeSegments = Mathf.Clamp(RopeSegmentsPerSpan, 1, 6);
		float postOffset = Mathf.Max(BridgeWidth, 0.1f) * 0.5f -
			Mathf.Max(PostThickness, 0.02f) * 0.55f;
		List<Transform3D> ropeTransforms = new();

		for (int postIndex = 0; postIndex < postPairs - 1; postIndex++)
		{
			float spanStart = postIndex / (float)(postPairs - 1);
			float spanEnd = (postIndex + 1) / (float)(postPairs - 1);

			for (int side = -1; side <= 1; side += 2)
			{
				AddRopeSpan(
					ropeTransforms,
					bridgeLength,
					spanStart,
					spanEnd,
					side * postOffset,
					1.0f,
					ropeSegments);
				AddRopeSpan(
					ropeTransforms,
					bridgeLength,
					spanStart,
					spanEnd,
					side * postOffset,
					0.54f,
					ropeSegments);
			}
		}

		Material ropeMaterial = CreatePainterlyMaterial(
			RopeColor,
			0.96f,
			10.0f,
			0.08f,
			0.06f,
			0.10f,
			0.08f);
		CylinderMesh ropeMesh = new()
		{
			TopRadius = Mathf.Max(RopeRadius, 0.005f),
			BottomRadius = Mathf.Max(RopeRadius, 0.005f),
			Height = 1.0f,
			RadialSegments = 6,
			Rings = 1,
			Material = ropeMaterial
		};
		MultiMesh multiMesh = CreateMultiMesh(
			ropeMesh,
			ropeTransforms.Count,
			false);

		for (int index = 0; index < ropeTransforms.Count; index++)
			multiMesh.SetInstanceTransform(index, ropeTransforms[index]);

		AddMultiMesh("BridgeRopes", multiMesh);
	}

	private void AddRopeSpan(
		List<Transform3D> transforms,
		float bridgeLength,
		float spanStart,
		float spanEnd,
		float sideOffset,
		float heightFactor,
		int segmentCount)
	{
		for (int segment = 0; segment < segmentCount; segment++)
		{
			float startFactor = segment / (float)segmentCount;
			float endFactor = (segment + 1) / (float)segmentCount;
			float startProgress = Mathf.Lerp(
				spanStart,
				spanEnd,
				startFactor);
			float endProgress = Mathf.Lerp(
				spanStart,
				spanEnd,
				endFactor);
			Vector3 start = GetRopePoint(
				bridgeLength,
				startProgress,
				spanStart,
				spanEnd,
				sideOffset,
				heightFactor);
			Vector3 end = GetRopePoint(
				bridgeLength,
				endProgress,
				spanStart,
				spanEnd,
				sideOffset,
				heightFactor);
			transforms.Add(CreateCylinderSegmentTransform(start, end));
		}
	}

	private Vector3 GetRopePoint(
		float bridgeLength,
		float progress,
		float spanStart,
		float spanEnd,
		float sideOffset,
		float heightFactor)
	{
		float spanProgress = Mathf.InverseLerp(
			spanStart,
			spanEnd,
			progress);
		float sag = 4.0f * spanProgress * (1.0f - spanProgress) *
			Mathf.Max(RopeSag, 0.0f);
		return GetBridgePoint(
			bridgeLength,
			progress,
			sideOffset,
			Mathf.Max(PostHeight, 0.1f) * heightFactor - sag);
	}

	private Vector3 GetBridgePoint(
		float bridgeLength,
		float progress,
		float sideOffset,
		float heightOffset)
	{
		return new Vector3(
			sideOffset,
			GetDeckHeight(progress) + heightOffset,
			Mathf.Lerp(-bridgeLength * 0.5f,
				bridgeLength * 0.5f,
				progress));
	}

	private float GetDeckHeight(float progress)
	{
		return Mathf.Sin(Mathf.Pi * Mathf.Clamp(progress, 0.0f, 1.0f)) *
			Mathf.Max(ArchHeight, 0.0f);
	}

	private float GetDeckPitch(float progress, float bridgeLength)
	{
		float slope = Mathf.Pi * Mathf.Max(ArchHeight, 0.0f) *
			Mathf.Cos(Mathf.Pi * progress) /
			Mathf.Max(bridgeLength, 0.001f);
		return -Mathf.Atan(slope);
	}

	private static Transform3D CreateBoxSegmentTransform(
		Vector3 start,
		Vector3 end,
		float thickness)
	{
		Vector3 delta = end - start;
		float length = Mathf.Max(delta.Length(), 0.001f);
		Vector3 zAxis = delta / length;
		Vector3 xAxis = Vector3.Up.Cross(zAxis).Normalized();

		if (xAxis.IsZeroApprox())
			xAxis = Vector3.Right;

		Vector3 yAxis = zAxis.Cross(xAxis).Normalized();
		Basis basis = new(
			xAxis * thickness,
			yAxis * thickness,
			zAxis * length);
		return new Transform3D(basis, (start + end) * 0.5f);
	}

	private static Transform3D CreateCylinderSegmentTransform(
		Vector3 start,
		Vector3 end)
	{
		Vector3 delta = end - start;
		float length = Mathf.Max(delta.Length(), 0.001f);
		Vector3 yAxis = delta / length;
		Vector3 reference = Mathf.Abs(yAxis.Dot(Vector3.Up)) > 0.98f
			? Vector3.Right
			: Vector3.Up;
		Vector3 xAxis = reference.Cross(yAxis).Normalized();
		Vector3 zAxis = xAxis.Cross(yAxis).Normalized();
		Basis basis = new(xAxis, yAxis * length, zAxis);
		return new Transform3D(basis, (start + end) * 0.5f);
	}

	private static MultiMesh CreateMultiMesh(
		Mesh mesh,
		int instanceCount,
		bool useColors)
	{
		MultiMesh multiMesh = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = useColors,
			Mesh = mesh
		};
		multiMesh.InstanceCount = Math.Max(instanceCount, 0);
		multiMesh.VisibleInstanceCount = -1;
		return multiMesh;
	}

	private void AddMultiMesh(string nodeName, MultiMesh multiMesh)
	{
		MultiMeshInstance3D instance = new()
		{
			Name = nodeName,
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.On
		};
		AddChild(instance);
		RegisterGeneratedNode(instance);
	}

	private static Material CreatePainterlyMaterial(
		Color baseTint,
		float roughness,
		float grainScale,
		float grainStrength,
		float brushStrength,
		float edgeDarkening,
		float instanceVariation)
	{
		Shader shader = GD.Load<Shader>(PainterlyWoodShaderPath);
		if (shader == null)
		{
			return new StandardMaterial3D
			{
				AlbedoColor = baseTint,
				VertexColorUseAsAlbedo = true,
				Roughness = roughness,
				Metallic = 0.0f
			};
		}

		ShaderMaterial material = new() { Shader = shader };
		material.SetShaderParameter("base_tint", baseTint);
		material.SetShaderParameter("roughness", roughness);
		material.SetShaderParameter("grain_scale", grainScale);
		material.SetShaderParameter("grain_strength", grainStrength);
		material.SetShaderParameter("brush_strength", brushStrength);
		material.SetShaderParameter("edge_darkening", edgeDarkening);
		material.SetShaderParameter(
			"instance_variation",
			instanceVariation);
		return material;
	}

	private static Vector2 BoardPlaneToAxial(
		Vector2 position,
		float hexSize)
	{
		float q = position.X / (1.5f * hexSize);
		float r = position.Y / (Mathf.Sqrt(3.0f) * hexSize) - q * 0.5f;
		return new Vector2(q, r);
	}

	private static float GetHexDistance(float q, float r)
	{
		return Mathf.Max(
			Mathf.Max(Mathf.Abs(q), Mathf.Abs(r)),
			Mathf.Abs(-q - r));
	}
}
