using Godot;
using System;
using System.Collections.Generic;

public partial class FishSchoolController : Node3D
{
	private const int MaximumFishCount = 56;
	private const int FishSpawnPlacementAttempts = 24;
	private const int FishOverlapResolutionIterations = 3;
	private const float MaximumFishSimulationStep = 0.05f;
	private const float FishSeparationDistance = 1.0f;
	private const float FishSeparationSteerSpeed = 5.0f;
	private const float MinimumForwardSpeed = 0.05f;
	private const float MinimumRestInterval = 4.0f;
	private const float MaximumRestInterval = 10.0f;
	private const float MinimumRestDuration = 0.25f;
	private const float MaximumRestDuration = 0.65f;
	private const float RestMotionFactor = 0.35f;
	private const float HexTileBoundaryOffset = 2f / 3f;
	private const string AnimatedFishAnimation = "Take 01";
	private const string ResetAnimationName = "RESET";

	[ExportGroup("General")]
	[Export] public bool FishEnabled = true;

	[Export(PropertyHint.Range, "0,56,1")]
	public int FishCount = 40;

	[Export] public int LayoutSeed = 7259;
	[Export] public PackedScene FishModelA;
	[Export] public PackedScene FishModelB;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float ModelAChance = 0.5f;

	[ExportGroup("Model A")]
	[Export(PropertyHint.Range, "0.01,1.0,0.005")]
	public float ModelAScale = 0.115f;

	[Export(PropertyHint.Range, "-180.0,180.0,1.0")]
	public float ModelAForwardYawDegrees = 180f;

	[ExportGroup("Model B")]
	[Export(PropertyHint.Range, "0.01,1.0,0.005")]
	public float ModelBScale = 0.115f;

	[Export(PropertyHint.Range, "-180.0,180.0,1.0")]
	public float ModelBForwardYawDegrees = 180f;

	[ExportGroup("Underwater Appearance")]
	[Export] public Color UnderwaterTint = new(0.20f, 0.50f, 0.56f, 1.0f);

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float UnderwaterTintStrength = 0.46f;

	[Export(PropertyHint.Range, "0.2,1.0,0.01")]
	public float UnderwaterBrightness = 0.72f;

	[ExportGroup("Water Area")]
	[Export(PropertyHint.Range, "0.0,30.0,0.1")]
	public float InnerSwimRadius = 9.8f;

	[Export(PropertyHint.Range, "0.5,40.0,0.1")]
	public float OuterSwimRadius = 18.5f;

	[Export]
	public bool UseStoneBorderBounds = true;

	[Export(PropertyHint.Range, "0.0,3.0,0.05")]
	public float StoneBorderClearance = 0.75f;

	[Export(PropertyHint.Range, "0.05,4.0,0.05")]
	public float MinDepth = 0.75f;

	[Export(PropertyHint.Range, "0.05,5.0,0.05")]
	public float MaxDepth = 1.35f;

	[Export(PropertyHint.Range, "0.1,6.0,0.1")]
	public float EdgeSteerDistance = 2.0f;

	[Export(PropertyHint.Range, "0.1,8.0,0.1")]
	public float EdgeSteerSpeed = 2.2f;

	[ExportGroup("Normal Swimming")]
	[Export(PropertyHint.Range, "0.05,5.0,0.05")]
	public float MinNormalSpeed = 0.45f;

	[Export(PropertyHint.Range, "0.05,6.0,0.05")]
	public float MaxNormalSpeed = 0.8f;

	[Export(PropertyHint.Range, "0.0,80.0,1.0")]
	public float MaxWanderTurnDegrees = 22.0f;

	[Export(PropertyHint.Range, "0.1,10.0,0.1")]
	public float TurnSmoothing = 2.4f;

	[Export(PropertyHint.Range, "0.1,10.0,0.1")]
	public float MinDirectionDuration = 1.4f;

	[Export(PropertyHint.Range, "0.1,15.0,0.1")]
	public float MaxDirectionDuration = 4.2f;

	[ExportGroup("Short Sprints")]
	[Export(PropertyHint.Range, "0.5,60.0,0.5")]
	public float MinSprintInterval = 8.0f;

	[Export(PropertyHint.Range, "0.5,90.0,0.5")]
	public float MaxSprintInterval = 19.0f;

	[Export(PropertyHint.Range, "1.0,8.0,0.1")]
	public float MinSprintMultiplier = 2.0f;

	[Export(PropertyHint.Range, "1.0,10.0,0.1")]
	public float MaxSprintMultiplier = 3.0f;

	[Export(PropertyHint.Range, "0.1,10.0,0.1")]
	public float MinSprintSlowdown = 1.4f;

	[Export(PropertyHint.Range, "0.1,12.0,0.1")]
	public float MaxSprintSlowdown = 2.8f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float SprintTurnReduction = 0.72f;

	[ExportGroup("Body Motion")]
	[Export(PropertyHint.Range, "0.1,3.0,0.05")]
	public float MinAnimationPlaybackSpeed = 0.85f;

	[Export(PropertyHint.Range, "0.1,3.0,0.05")]
	public float MaxAnimationPlaybackSpeed = 1.15f;

	[Export(PropertyHint.Range, "0.0,1.5,0.05")]
	public float SprintAnimationSpeedBoost = 0.45f;

	[Export(PropertyHint.Range, "0.0,20.0,0.1")]
	public float BodySwayDegrees = 4.0f;

	[Export(PropertyHint.Range, "0.1,8.0,0.1")]
	public float MinBodySwaySpeed = 1.5f;

	[Export(PropertyHint.Range, "0.1,10.0,0.1")]
	public float MaxBodySwaySpeed = 3.2f;

	[Export(PropertyHint.Range, "0.0,0.5,0.005")]
	public float BobHeight = 0.035f;

	[Export(PropertyHint.Range, "0.1,8.0,0.1")]
	public float MinBobSpeed = 0.8f;

	[Export(PropertyHint.Range, "0.1,10.0,0.1")]
	public float MaxBobSpeed = 1.7f;

	[Export(PropertyHint.Range, "0.0,30.0,0.5")]
	public float MaximumBankDegrees = 8.0f;

	private readonly List<FishState> _fish = new();
	private readonly RandomNumberGenerator _random = new();
	private BoardManager _boardManager;

	private int _builtCount = -1;
	private int _builtSeed;
	private PackedScene _builtModelA;
	private PackedScene _builtModelB;
	private float _builtModelAChance;
	private float _builtModelAScale;
	private float _builtModelBScale;
	private float _builtModelAYaw;
	private float _builtModelBYaw;
	private Color _builtUnderwaterTint;
	private float _builtUnderwaterTintStrength;
	private float _builtUnderwaterBrightness;

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

	private sealed class FishState
	{
		public Node3D Root;
		public Node3D VisualPivot;
		public readonly List<AnimationPlayer> AnimationPlayers = new();
		public float AnimationSpeedFactor;
		public float AnimationPhase;
		public float Heading;
		public float BaseSpeed;
		public float TurnRate;
		public float TargetTurnRate;
		public float DirectionTimer;
		public float SprintTimer;
		public float SprintBonus;
		public float SprintStartBonus;
		public float SprintSlowdown;
		public float RestTimer;
		public float RestRemaining;
		public float Depth;
		public float BobPhase;
		public float BobSpeed;
		public float SwayPhase;
		public float SwaySpeed;
	}

	public override void _Ready()
	{
		_boardManager = GetNodeOrNull<BoardManager>("../BoardManager");
		SetFishEnabled(SettingsMenu.IsFishEnabled());
	}

	public void SetFishEnabled(bool enabled)
	{
		FishEnabled = enabled;
		Visible = enabled;

		if (NeedsRebuild())
			RebuildFish();
	}

	public override void _Process(double delta)
	{
		if (NeedsRebuild())
		{
			RebuildFish();
		}

		Visible = FishEnabled;
		if (!FishEnabled || _fish.Count == 0)
		{
			return;
		}

		float remainingDelta = (float)delta;
		while (remainingDelta > 0f)
		{
			float simulationStep = Mathf.Min(
				remainingDelta,
				MaximumFishSimulationStep);
			for (int index = 0; index < _fish.Count; index++)
			{
				UpdateFish(_fish[index], index, simulationStep);
			}

			ResolveFishOverlaps();
			remainingDelta -= simulationStep;
		}
	}

	private bool NeedsRebuild()
	{
		int desiredCount = FishEnabled
			? Mathf.Clamp(FishCount, 0, MaximumFishCount)
			: 0;
		return desiredCount != _builtCount
			|| LayoutSeed != _builtSeed
			|| FishModelA != _builtModelA
			|| FishModelB != _builtModelB
			|| !Mathf.IsEqualApprox(ModelAChance, _builtModelAChance)
			|| !Mathf.IsEqualApprox(ModelAScale, _builtModelAScale)
			|| !Mathf.IsEqualApprox(ModelBScale, _builtModelBScale)
			|| !Mathf.IsEqualApprox(ModelAForwardYawDegrees, _builtModelAYaw)
			|| !Mathf.IsEqualApprox(ModelBForwardYawDegrees, _builtModelBYaw)
			|| UnderwaterTint != _builtUnderwaterTint
			|| !Mathf.IsEqualApprox(
				UnderwaterTintStrength,
				_builtUnderwaterTintStrength)
			|| !Mathf.IsEqualApprox(
				UnderwaterBrightness,
				_builtUnderwaterBrightness);
	}

	private void RebuildFish()
	{
		for (int index = 0; index < _fish.Count; index++)
		{
			_fish[index].Root.QueueFree();
		}
		_fish.Clear();

		_builtCount = FishEnabled
			? Mathf.Clamp(FishCount, 0, MaximumFishCount)
			: 0;
		_builtSeed = LayoutSeed;
		_builtModelA = FishModelA;
		_builtModelB = FishModelB;
		_builtModelAChance = ModelAChance;
		_builtModelAScale = ModelAScale;
		_builtModelBScale = ModelBScale;
		_builtModelAYaw = ModelAForwardYawDegrees;
		_builtModelBYaw = ModelBForwardYawDegrees;
		_builtUnderwaterTint = UnderwaterTint;
		_builtUnderwaterTintStrength = UnderwaterTintStrength;
		_builtUnderwaterBrightness = UnderwaterBrightness;

		_random.Seed = (ulong)Math.Abs((long)LayoutSeed) + 1UL;
		for (int index = 0; index < _builtCount; index++)
		{
			CreateFish(index);
		}
	}

	private void CreateFish(int index)
	{
		bool useModelA = ChooseModelA();
		PackedScene modelScene = useModelA ? FishModelA : FishModelB;
		if (modelScene == null)
		{
			return;
		}

		Node modelNode = modelScene.Instantiate();
		Node3D fishRoot = new()
		{
			Name = $"Fish_{index + 1:00}"
		};
		Node3D visualPivot = new()
		{
			Name = "VisualPivot"
		};
		fishRoot.AddChild(visualPivot);
		visualPivot.AddChild(modelNode);
		AddChild(fishRoot);

		if (modelNode is Node3D modelRoot)
		{
			float modelScale = useModelA ? ModelAScale : ModelBScale;
			float modelYaw = useModelA
				? ModelAForwardYawDegrees
				: ModelBForwardYawDegrees;
			modelRoot.Scale = Vector3.One * Mathf.Max(0.001f, modelScale);
			modelRoot.Rotation = new Vector3(
				0f,
				Mathf.DegToRad(modelYaw),
				0f);
		}

		ApplyUnderwaterAppearance(modelNode);
		DisableShadows(modelNode);

		Vector2 spawnPosition = CreateSpawnPosition(GetWaterBounds());
		float heading = _random.RandfRange(0f, Mathf.Tau);
		float depthMin = Mathf.Min(MinDepth, MaxDepth);
		float depthMax = Mathf.Max(MinDepth, MaxDepth);

		FishState fish = new()
		{
			Root = fishRoot,
			VisualPivot = visualPivot,
			Heading = heading,
			BaseSpeed = RandomOrderedRange(MinNormalSpeed, MaxNormalSpeed),
			TargetTurnRate = RandomTurnRate(),
			DirectionTimer = RandomOrderedRange(
				MinDirectionDuration,
				MaxDirectionDuration),
			SprintTimer = RandomOrderedRange(
				MinSprintInterval,
				MaxSprintInterval),
			RestTimer = RandomOrderedRange(
				MinimumRestInterval,
				MaximumRestInterval),
			Depth = _random.RandfRange(depthMin, depthMax),
			BobPhase = _random.RandfRange(0f, Mathf.Tau),
			BobSpeed = RandomOrderedRange(MinBobSpeed, MaxBobSpeed),
			SwayPhase = _random.RandfRange(0f, Mathf.Tau),
			SwaySpeed = RandomOrderedRange(
				MinBodySwaySpeed,
				MaxBodySwaySpeed)
		};
		fish.AnimationSpeedFactor = Mathf.PosMod(fish.SwayPhase / Mathf.Tau, 1f);
		fish.AnimationPhase = Mathf.PosMod(fish.BobPhase / Mathf.Tau, 1f);
		StartImportedAnimation(modelNode, fish);
		UpdateAnimationPlayback(fish, 0f, false);

		fishRoot.Position = new Vector3(
			spawnPosition.X,
			-fish.Depth,
			spawnPosition.Y);
		fishRoot.Rotation = new Vector3(
			0f,
			-heading - (Mathf.Pi * 0.5f),
			0f);
		_fish.Add(fish);
	}

	private Vector2 CreateSpawnPosition(WaterBounds bounds)
	{
		Vector2 candidate = Vector2.Zero;
		for (int attempt = 0; attempt < FishSpawnPlacementAttempts; attempt++)
		{
			candidate = CreateRandomSpawnPosition(bounds);
			if (HasFishSpawnClearance(candidate))
				return candidate;
		}

		return candidate;
	}

	private Vector2 CreateRandomSpawnPosition(WaterBounds bounds)
	{
		float angle = _random.RandfRange(0f, Mathf.Tau);
		float distanceSquared = _random.RandfRange(
			bounds.InnerDistance * bounds.InnerDistance,
			bounds.OuterDistance * bounds.OuterDistance);
		float distance = Mathf.Sqrt(distanceSquared);

		if (!bounds.UsesStoneBorders)
		{
			return new Vector2(
				Mathf.Cos(angle) * distance,
				Mathf.Sin(angle) * distance);
		}

		Vector2 boardDirection = new(Mathf.Cos(angle), Mathf.Sin(angle));
		Vector2 axialDirection = BoardPlaneToAxial(
			boardDirection,
			bounds.HexSize);
		float directionDistance = GetHexDistance(
			axialDirection.X,
			axialDirection.Y);
		Vector2 axial = axialDirection * (distance / directionDistance);
		return BoardToControllerPlane(
			AxialToBoardPlane(axial.X, axial.Y, bounds.HexSize));
	}

	private bool HasFishSpawnClearance(Vector2 candidate)
	{
		float minimumDistanceSquared =
			FishSeparationDistance * FishSeparationDistance;
		for (int index = 0; index < _fish.Count; index++)
		{
			Vector3 position = _fish[index].Root.Position;
			Vector2 horizontal = new(position.X, position.Z);
			if (horizontal.DistanceSquaredTo(candidate) < minimumDistanceSquared)
				return false;
		}

		return true;
	}

	private bool ChooseModelA()
	{
		if (FishModelA == null)
		{
			return false;
		}
		if (FishModelB == null)
		{
			return true;
		}
		return _random.Randf() < Mathf.Clamp(ModelAChance, 0f, 1f);
	}

	private void UpdateFish(FishState fish, int fishIndex, float delta)
	{
		bool isResting = UpdateRest(fish, delta);
		float sprintAmount = 0f;
		if (!isResting)
		{
			fish.DirectionTimer -= delta;
			if (fish.DirectionTimer <= 0f)
			{
				fish.TargetTurnRate = RandomTurnRate();
				fish.DirectionTimer = RandomOrderedRange(
					MinDirectionDuration,
					MaxDirectionDuration);
			}

			UpdateSprint(fish, delta);
			sprintAmount = fish.SprintStartBonus > 0.001f
				? Mathf.Clamp(fish.SprintBonus / fish.SprintStartBonus, 0f, 1f)
				: 0f;
			float turnReduction = Mathf.Lerp(
				1f,
				1f - Mathf.Clamp(SprintTurnReduction, 0f, 1f),
				sprintAmount);
			float desiredTurnRate = fish.TargetTurnRate * turnReduction;
			fish.TurnRate = Mathf.Lerp(
				fish.TurnRate,
				desiredTurnRate,
				1f - MathF.Exp(-Mathf.Max(0.01f, TurnSmoothing) * delta));

			SteerAwayFromLand(fish, delta);
			SteerAwayFromFish(fish, fishIndex, delta);
			fish.Heading += fish.TurnRate * delta;
		}
		else
		{
			fish.TurnRate = Mathf.Lerp(
				fish.TurnRate,
				0f,
				1f - MathF.Exp(-Mathf.Max(0.01f, TurnSmoothing) * delta));
		}

		float speed = isResting
			? 0f
			: Mathf.Max(
				MinimumForwardSpeed,
				fish.BaseSpeed + fish.SprintBonus);
		UpdateAnimationPlayback(fish, sprintAmount, isResting);
		Vector3 direction = new(
			Mathf.Cos(fish.Heading),
			0f,
			Mathf.Sin(fish.Heading));
		Vector3 position = fish.Root.Position + (direction * speed * delta);
		ConstrainToWater(ref position, fish);

		float motionFactor = isResting ? RestMotionFactor : 1f;
		fish.BobPhase += fish.BobSpeed * delta * motionFactor;
		position.Y = -fish.Depth + (Mathf.Sin(fish.BobPhase) * BobHeight);
		fish.Root.Position = position;
		fish.Root.Rotation = new Vector3(
			0f,
			-fish.Heading - (Mathf.Pi * 0.5f),
			0f);

		fish.SwayPhase += fish.SwaySpeed * delta * motionFactor;
		float sway = Mathf.Sin(fish.SwayPhase)
			* Mathf.DegToRad(BodySwayDegrees);
		float maximumBank = Mathf.DegToRad(MaximumBankDegrees);
		float bank = Mathf.Clamp(-fish.TurnRate * 0.35f, -maximumBank, maximumBank);
		fish.VisualPivot.Rotation = new Vector3(0f, sway, bank);
	}

	private void UpdateAnimationPlayback(
		FishState fish,
		float sprintAmount,
		bool isResting)
	{
		float minimumSpeed = Mathf.Max(
			0.05f,
			Mathf.Min(MinAnimationPlaybackSpeed, MaxAnimationPlaybackSpeed));
		float maximumSpeed = Mathf.Max(
			minimumSpeed,
			Mathf.Max(MinAnimationPlaybackSpeed, MaxAnimationPlaybackSpeed));
		float basePlaybackSpeed = Mathf.Lerp(
			minimumSpeed,
			maximumSpeed,
			fish.AnimationSpeedFactor);
		float sprintSpeed = 1f + (
			Mathf.Max(0f, SprintAnimationSpeedBoost) *
			Mathf.Clamp(sprintAmount, 0f, 1f));

		for (int index = 0; index < fish.AnimationPlayers.Count; index++)
		{
			AnimationPlayer animationPlayer = fish.AnimationPlayers[index];
			if (GodotObject.IsInstanceValid(animationPlayer))
			{
				animationPlayer.SpeedScale =
					basePlaybackSpeed * sprintSpeed *
					(isResting ? RestMotionFactor : 1f);
			}
		}
	}

	private bool UpdateRest(FishState fish, float delta)
	{
		if (fish.RestRemaining > 0f)
		{
			fish.RestRemaining = Mathf.Max(0f, fish.RestRemaining - delta);
			return fish.RestRemaining > 0f;
		}

		if (fish.SprintBonus > 0.01f)
			return false;

		fish.RestTimer -= delta;
		if (fish.RestTimer > 0f)
			return false;

		fish.RestRemaining = RandomOrderedRange(
			MinimumRestDuration,
			MaximumRestDuration);
		fish.RestTimer = RandomOrderedRange(
			MinimumRestInterval,
			MaximumRestInterval);
		return true;
	}

	private void UpdateSprint(FishState fish, float delta)
	{
		if (fish.SprintBonus > 0.01f)
		{
			float slowdown = Mathf.Max(0.1f, fish.SprintSlowdown);
			fish.SprintBonus *= MathF.Exp(-delta / slowdown);
			return;
		}

		fish.SprintBonus = 0f;
		fish.SprintStartBonus = 0f;
		fish.SprintTimer -= delta;
		if (fish.SprintTimer > 0f)
		{
			return;
		}

		float multiplier = RandomOrderedRange(
			MinSprintMultiplier,
			MaxSprintMultiplier);
		fish.SprintBonus = fish.BaseSpeed * Mathf.Max(0f, multiplier - 1f);
		fish.SprintStartBonus = fish.SprintBonus;
		fish.SprintSlowdown = RandomOrderedRange(
			MinSprintSlowdown,
			MaxSprintSlowdown);
		fish.SprintTimer = RandomOrderedRange(
			MinSprintInterval,
			MaxSprintInterval);
	}

	private void SteerAwayFromLand(FishState fish, float delta)
	{
		WaterBounds bounds = GetWaterBounds();
		Vector2 position = new(fish.Root.Position.X, fish.Root.Position.Z);
		float boundaryDistance = GetBoundaryDistance(position, bounds);
		float steerDistance = bounds.UsesStoneBorders
			? Mathf.Max(0.1f, EdgeSteerDistance) /
				Mathf.Max(bounds.HexSize * 1.5f, 0.1f)
			: Mathf.Max(0.1f, EdgeSteerDistance);
		Vector2 radial = GetBoundaryDirection(position, bounds);
		float desiredHeading;
		float edgeAmount;

		if (boundaryDistance < bounds.InnerDistance + steerDistance)
		{
			desiredHeading = Mathf.Atan2(radial.Y, radial.X);
			edgeAmount = 1f - Mathf.Clamp(
				(boundaryDistance - bounds.InnerDistance) / steerDistance,
				0f,
				1f);
		}
		else if (boundaryDistance > bounds.OuterDistance - steerDistance)
		{
			desiredHeading = Mathf.Atan2(-radial.Y, -radial.X);
			edgeAmount = 1f - Mathf.Clamp(
				(bounds.OuterDistance - boundaryDistance) / steerDistance,
				0f,
				1f);
		}
		else
		{
			return;
		}

		float blend = 1f - MathF.Exp(
			-Mathf.Max(0.1f, EdgeSteerSpeed) * edgeAmount * delta);
		fish.Heading = Mathf.LerpAngle(fish.Heading, desiredHeading, blend);
	}

	private void SteerAwayFromFish(
		FishState fish,
		int fishIndex,
		float delta)
	{
		Vector3 rootPosition = fish.Root.Position;
		Vector2 position = new(rootPosition.X, rootPosition.Z);
		float minimumDistanceSquared =
			FishSeparationDistance * FishSeparationDistance;
		Vector2 avoidance = Vector2.Zero;
		float strongestInfluence = 0f;

		for (int otherIndex = 0; otherIndex < _fish.Count; otherIndex++)
		{
			if (otherIndex == fishIndex)
				continue;

			Vector3 otherRootPosition = _fish[otherIndex].Root.Position;
			Vector2 offset = position - new Vector2(
				otherRootPosition.X,
				otherRootPosition.Z);
			float distanceSquared = offset.LengthSquared();
			if (distanceSquared >= minimumDistanceSquared)
				continue;

			float distance = Mathf.Sqrt(distanceSquared);
			Vector2 direction = distance > 0.001f
				? offset / distance
				: GetFishPairSeparationDirection(fishIndex, otherIndex);
			float influence = 1f - Mathf.Clamp(
				distance / FishSeparationDistance,
				0f,
				1f);
			avoidance += direction * influence * influence;
			strongestInfluence = Mathf.Max(strongestInfluence, influence);
		}

		if (avoidance.LengthSquared() <= 0.0001f)
			return;

		float desiredHeading = Mathf.Atan2(avoidance.Y, avoidance.X);
		float blend = 1f - MathF.Exp(
			-FishSeparationSteerSpeed *
			(0.35f + strongestInfluence) * delta);
		fish.Heading = Mathf.LerpAngle(fish.Heading, desiredHeading, blend);
	}

	private void ResolveFishOverlaps()
	{
		float minimumDistanceSquared =
			FishSeparationDistance * FishSeparationDistance;

		for (int iteration = 0;
			iteration < FishOverlapResolutionIterations;
			iteration++)
		{
			bool adjustedPosition = false;
			for (int firstIndex = 0; firstIndex < _fish.Count; firstIndex++)
			{
				for (int secondIndex = firstIndex + 1;
					secondIndex < _fish.Count;
					secondIndex++)
				{
					FishState firstFish = _fish[firstIndex];
					FishState secondFish = _fish[secondIndex];
					Vector3 firstPosition = firstFish.Root.Position;
					Vector3 secondPosition = secondFish.Root.Position;
					Vector2 offset = new(
						firstPosition.X - secondPosition.X,
						firstPosition.Z - secondPosition.Z);
					float distanceSquared = offset.LengthSquared();
					if (distanceSquared >= minimumDistanceSquared)
						continue;

					float distance = Mathf.Sqrt(distanceSquared);
					Vector2 direction = distance > 0.001f
						? offset / distance
						: GetFishPairSeparationDirection(
							firstIndex,
							secondIndex);
					Vector2 correction = direction *
						((FishSeparationDistance - distance) * 0.5f);
					firstPosition.X += correction.X;
					firstPosition.Z += correction.Y;
					secondPosition.X -= correction.X;
					secondPosition.Z -= correction.Y;
					firstFish.Root.Position = firstPosition;
					secondFish.Root.Position = secondPosition;
					adjustedPosition = true;
				}
			}

			if (!adjustedPosition)
				return;

			for (int index = 0; index < _fish.Count; index++)
			{
				FishState fish = _fish[index];
				Vector3 position = fish.Root.Position;
				ConstrainToWater(ref position, fish);
				fish.Root.Position = position;
				fish.Root.Rotation = new Vector3(
					0f,
					-fish.Heading - (Mathf.Pi * 0.5f),
					0f);
			}
		}
	}

	private static Vector2 GetFishPairSeparationDirection(
		int fishIndex,
		int otherIndex)
	{
		int firstIndex = Math.Min(fishIndex, otherIndex);
		int secondIndex = Math.Max(fishIndex, otherIndex);
		float angle = Mathf.PosMod(
			((firstIndex + 1) * 2.399963f) +
			((secondIndex + 1) * 1.618034f),
			Mathf.Tau);
		Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
		return fishIndex == firstIndex ? direction : -direction;
	}

	private void ConstrainToWater(ref Vector3 position, FishState fish)
	{
		WaterBounds bounds = GetWaterBounds();
		Vector2 horizontal = new(position.X, position.Z);
		float boundaryDistance = GetBoundaryDistance(horizontal, bounds);
		if (boundaryDistance >= bounds.InnerDistance &&
			boundaryDistance <= bounds.OuterDistance)
		{
			return;
		}

		Vector2 constrainedPosition;
		if (boundaryDistance <= 0.001f)
		{
			constrainedPosition = bounds.UsesStoneBorders
				? BoardToControllerPlane(AxialToBoardPlane(
					bounds.InnerDistance,
					0f,
					bounds.HexSize))
				: Vector2.Right * bounds.InnerDistance;
		}
		else if (bounds.UsesStoneBorders)
		{
			Vector2 boardPosition = ControllerToBoardPlane(horizontal);
			Vector2 axial = BoardPlaneToAxial(boardPosition, bounds.HexSize);
			float constrainedDistance = Mathf.Clamp(
				boundaryDistance,
				bounds.InnerDistance,
				bounds.OuterDistance);
			axial *= constrainedDistance / boundaryDistance;
			constrainedPosition = BoardToControllerPlane(
				AxialToBoardPlane(axial.X, axial.Y, bounds.HexSize));
		}
		else
		{
			float constrainedDistance = Mathf.Clamp(
				boundaryDistance,
				bounds.InnerDistance,
				bounds.OuterDistance);
			constrainedPosition = horizontal / boundaryDistance * constrainedDistance;
		}

		position.X = constrainedPosition.X;
		position.Z = constrainedPosition.Y;
		Vector2 normal = GetBoundaryDirection(constrainedPosition, bounds);
		fish.Heading = boundaryDistance < bounds.InnerDistance
			? Mathf.Atan2(normal.Y, normal.X)
			: Mathf.Atan2(-normal.Y, -normal.X);
	}

	private WaterBounds GetWaterBounds()
	{
		GetSwimRadii(out float innerRadius, out float outerRadius);
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
			return position.LengthSquared() > 0.0001f
				? position.Normalized()
				: Vector2.Right;

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

	private void GetSwimRadii(out float innerRadius, out float outerRadius)
	{
		innerRadius = Mathf.Max(0f, Mathf.Min(InnerSwimRadius, OuterSwimRadius));
		outerRadius = Mathf.Max(
			innerRadius + 0.1f,
			Mathf.Max(InnerSwimRadius, OuterSwimRadius));
	}

	private float RandomTurnRate()
	{
		float maximumRadians = Mathf.DegToRad(
			Mathf.Max(0f, MaxWanderTurnDegrees));
		return _random.RandfRange(-maximumRadians, maximumRadians);
	}

	private float RandomOrderedRange(float first, float second)
	{
		return _random.RandfRange(
			Mathf.Min(first, second),
			Mathf.Max(first, second));
	}

	private static void DisableShadows(Node node)
	{
		if (node is GeometryInstance3D geometry)
		{
			geometry.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}
		foreach (Node child in node.GetChildren())
		{
			DisableShadows(child);
		}
	}

	private void ApplyUnderwaterAppearance(Node node)
	{
		if (node is GeometryInstance3D geometry)
		{
			if (geometry.MaterialOverride != null)
			{
				Material underwaterMaterial = CreateUnderwaterMaterial(
					geometry.MaterialOverride);
				if (underwaterMaterial != null)
				{
					geometry.MaterialOverride = underwaterMaterial;
				}
			}
			else if (node is MeshInstance3D meshInstance &&
				meshInstance.Mesh != null)
			{
				for (int surfaceIndex = 0;
					surfaceIndex < meshInstance.Mesh.GetSurfaceCount();
					surfaceIndex++)
				{
					Material sourceMaterial =
						meshInstance.GetSurfaceOverrideMaterial(surfaceIndex) ??
						meshInstance.Mesh.SurfaceGetMaterial(surfaceIndex);
					Material underwaterMaterial = CreateUnderwaterMaterial(
						sourceMaterial);
					if (underwaterMaterial != null)
					{
						meshInstance.SetSurfaceOverrideMaterial(
							surfaceIndex,
							underwaterMaterial);
					}
				}
			}
		}

		foreach (Node child in node.GetChildren())
		{
			ApplyUnderwaterAppearance(child);
		}
	}

	private Material CreateUnderwaterMaterial(Material sourceMaterial)
	{
		if (sourceMaterial == null)
		{
			return null;
		}

		Material underwaterMaterial = sourceMaterial.Duplicate(true) as Material;
		if (underwaterMaterial is not BaseMaterial3D baseMaterial)
		{
			return null;
		}

		float tintStrength = Mathf.Clamp(UnderwaterTintStrength, 0.0f, 1.0f);
		float brightness = Mathf.Clamp(UnderwaterBrightness, 0.2f, 1.0f);
		Color sourceColor = baseMaterial.AlbedoColor;
		Color multipliedTint = new(
			sourceColor.R * UnderwaterTint.R,
			sourceColor.G * UnderwaterTint.G,
			sourceColor.B * UnderwaterTint.B,
			sourceColor.A);
		Color tintedColor = sourceColor.Lerp(multipliedTint, tintStrength);
		baseMaterial.AlbedoColor = new Color(
			tintedColor.R * brightness,
			tintedColor.G * brightness,
			tintedColor.B * brightness,
			tintedColor.A);
		baseMaterial.Roughness = Mathf.Lerp(
			baseMaterial.Roughness,
			0.82f,
			tintStrength);
		return underwaterMaterial;
	}

	private void StartImportedAnimation(Node node, FishState fish)
	{
		if (node is AnimationPlayer animationPlayer)
		{
			string animationName = FindSwimAnimation(animationPlayer);
			if (!string.IsNullOrEmpty(animationName))
			{
				Animation animation = animationPlayer.GetAnimation(animationName);
				if (animation != null)
				{
					animation.LoopMode = Animation.LoopModeEnum.Linear;
					animationPlayer.Play(animationName);
					animationPlayer.Seek(
						animation.Length * fish.AnimationPhase,
						true);
					fish.AnimationPlayers.Add(animationPlayer);
				}
			}
		}
		foreach (Node child in node.GetChildren())
		{
			StartImportedAnimation(child, fish);
		}
	}

	private static string FindSwimAnimation(AnimationPlayer animationPlayer)
	{
		if (animationPlayer.HasAnimation(AnimatedFishAnimation))
			return AnimatedFishAnimation;

		foreach (string animationName in animationPlayer.GetAnimationList())
		{
			if (!string.Equals(
				animationName,
				ResetAnimationName,
				StringComparison.OrdinalIgnoreCase))
			{
				return animationName;
			}
		}

		return null;
	}
}
