using System.Collections.Generic;
using Godot;

public partial class CameraRigController : Node3D
{
	private const string CameraSensitivitySetting =
		"gameplay/camera_sensitivity_multiplier";
	private const string ZoomSensitivitySetting =
		"gameplay/zoom_sensitivity_multiplier";
	private const string TileFocusDistanceSetting =
		"gameplay/tile_focus_distance";
	private const string BoardOverviewDistanceSetting =
		"gameplay/board_overview_distance_multiplier";
	private const string InvertVerticalSetting =
		"gameplay/invert_vertical_camera";

	[Export] public Camera3D Camera;

	[ExportGroup("Input")]
	[Export] public MouseButton YawButton = MouseButton.Right;
	[Export] public MouseButton PitchButton = MouseButton.Right;
	[Export] public float YawSensitivity = 0.18f;
	[Export] public float PitchSensitivity = 0.18f;
	[Export] public float SmoothSpeed = 10.0f;

	[ExportGroup("Focus")]
	[Export] public NodePath FocusTargetPath;
	[Export] public Vector3 FocusOffset = new Vector3(0.0f, 1.4f, 0.0f);

	[ExportGroup("Tile Focus")]
	[Export(PropertyHint.Range, "0.05,2.0,0.05")]
	public float FocusDuration = 0.55f;
	[Export(PropertyHint.Range, "0.0,4.0,0.1")]
	public float FocusHeightOffset = 1.0f;
	[Export(PropertyHint.Range, "2.0,30.0,0.5")]
	public float FocusMinDistance = 8.0f;
	[Export(PropertyHint.Range, "2.0,30.0,0.5")]
	public float FocusMaxDistance = 20.0f;
	[Export(PropertyHint.Range, "2.0,30.0,0.5")]
	public float FocusDistance = 14.0f;
	[Export(PropertyHint.Range, "2.0,30.0,0.5")]
	public float EdgeFocusMaxDistance = 12.0f;
	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	public float EdgeFocusStartRatio = 0.45f;
	[Export(PropertyHint.Range, "0.0,89.0,1.0")]
	public float FocusMinPitchDegrees = 20.0f;
	[Export(PropertyHint.Range, "0.0,89.0,1.0")]
	public float FocusMaxPitchDegrees = 60.0f;
	[Export(PropertyHint.Range, "1.0,89.0,1.0")]
	public float AllowedTreeSideAngleDegrees = 85.0f;

	[ExportGroup("Yaw Limits")]
	[Export] public bool LimitYaw = false;
	[Export] public float MinYawDegrees = -25.0f;
	[Export] public float MaxYawDegrees = 115.0f;

	[ExportGroup("Pitch Limits")]
	[Export] public float MinPitchDegrees = 20.0f;
	[Export] public float MaxPitchDegrees = 70.0f;

	[ExportGroup("Zoom")]
	[Export] public float MinDistance = 8.0f;
	[Export] public float MaxDistance = 28.0f;
	[Export] public float ZoomStep = 2.0f;
	[Export] public float ZoomSmoothSpeed = 10.0f;

	[ExportGroup("Pitch-dependent Zoom")]
	[Export] public bool EnablePitchDependentZoom = true;
	[Export(PropertyHint.Range, "0.0,90.0,1.0")]
	public float PitchZoomStartDegrees = 35.0f;
	[Export(PropertyHint.Range, "0.0,8.0,0.1")]
	public float MaxTopDownDistanceBonus = 1.5f;

	[ExportGroup("Collision")]
	[Export(PropertyHint.Range, "0.05,3.0,0.05")]
	public float CollisionSafetyDistance = 0.65f;
	[Export(PropertyHint.Range, "0.0,5.0,0.1")]
	public float GroundSafetyDistance = 1.0f;

	[ExportGroup("Start Values")]
	[Export] public float StartYawDegrees = 45.0f;
	[Export] public float StartPitchDegrees = 55.0f;
	[Export] public float StartDistance = 16.0f;

	[ExportGroup("Overview")]
	[Export(PropertyHint.Range, "0.0,89.0,1.0")]
	public float OverviewPitchDegrees = 18.0f;
	[Export(PropertyHint.Range, "0.5,1.5,0.025")]
	public float OverviewDistanceMultiplier = 0.875f;

	public bool HasTileFocus => _focusedTile != null && IsInstanceValid(_focusedTile);
	public bool InteractionEnabled => _interactionEnabled;

	private bool _interactionEnabled = true;
	private bool _isYawDragging;
	private bool _isPitchDragging;

	private float _targetYaw;
	private float _targetPitch;
	private float _targetDistance;
	private Vector3 _targetPivot;

	private float _currentYaw;
	private float _currentPitch;
	private float _currentDistance;
	private Vector3 _currentPivot;

	private Node3D _focusTarget;
	private HexTile _focusedTile;
	private HexTile _mainTreeTile;
	private Vector3 _treeFrontDirection;
	private Vector3 _boardCenter;
	private Vector3 _boardSideDirection;
	private float _boardSideExtent;
	private float _boardFrontExtent;
	private float _boardGroundY;
	private bool _hasBoardContext;
	private bool _isBoardOverviewActive;
	private readonly HashSet<HexTile> _overviewTiles = new();

	private float _generalYaw;
	private float _generalPitch;
	private float _generalDistance;

	private bool _isFocusTransitionActive;
	private float _focusTransitionElapsed;
	private Vector3 _focusStartPivot;
	private Vector3 _focusEndPivot;
	private float _focusStartYaw;
	private float _focusEndYaw;
	private float _focusStartPitch;
	private float _focusEndPitch;
	private float _focusStartDistance;
	private float _focusEndDistance;

	private float _resolvedCollisionDistance;
	private readonly Godot.Collections.Array<Rid> _collisionExclusions = new();

	public override void _Ready()
	{
		if (Camera == null)
			Camera = GetNodeOrNull<Camera3D>("Camera3D");

		if (Camera == null)
		{
			GD.PrintErr("CameraRigController: Camera3D not found.");
			return;
		}

		Camera.Projection = Camera3D.ProjectionType.Perspective;

		if (FocusTargetPath != null)
			_focusTarget = GetNodeOrNull<Node3D>(FocusTargetPath);

		_targetYaw = ClampYaw(Mathf.DegToRad(StartYawDegrees));
		_targetPitch = Mathf.DegToRad(Mathf.Clamp(
			StartPitchDegrees,
			MinPitchDegrees,
			MaxPitchDegrees));
		_targetDistance = Mathf.Clamp(
			StartDistance,
			MinDistance,
			GetMaximumDistanceForPitch(_targetPitch));
		_targetPivot = GetDefaultPivotPosition();

		_currentYaw = _targetYaw;
		_currentPitch = _targetPitch;
		_currentDistance = _targetDistance;
		_currentPivot = _targetPivot;
		_resolvedCollisionDistance = _currentDistance;

		RefreshCollisionExclusions(_focusTarget);
		UpdateCameraPosition(0.0f);
	}

	public override void _Process(double delta)
	{
		if (Camera == null)
			return;

		float deltaSeconds = (float)delta;

		if (_isFocusTransitionActive)
		{
			UpdateFocusTransition(deltaSeconds);
		}
		else
		{
			float smoothT = 1.0f - Mathf.Exp(-SmoothSpeed * deltaSeconds);
			float zoomT = 1.0f - Mathf.Exp(-ZoomSmoothSpeed * deltaSeconds);

			_currentPivot = _currentPivot.Lerp(_targetPivot, smoothT);
			_currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, smoothT);
			_currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, smoothT);
			_currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, zoomT);
		}

		UpdateCameraPosition(deltaSeconds);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		HandleMouseButtons(inputEvent);
		HandleMouseMotion(inputEvent);
		HandleZoom(inputEvent);
	}

	public void ConfigureBoardContext(BoardManager boardManager, HexTile mainTreeTile)
	{
		if (boardManager == null || mainTreeTile == null)
			return;

		_mainTreeTile = mainTreeTile;
		_boardGroundY = boardManager.GlobalPosition.Y;

		Vector3 frontDirection = boardManager.GlobalPosition - mainTreeTile.GlobalPosition;
		frontDirection.Y = 0.0f;

		if (frontDirection.LengthSquared() <= 0.0001f)
		{
			frontDirection = Camera.GlobalPosition - mainTreeTile.GlobalPosition;
			frontDirection.Y = 0.0f;
		}

		if (frontDirection.LengthSquared() <= 0.0001f)
			frontDirection = Vector3.Forward;

		_treeFrontDirection = frontDirection.Normalized();
		_hasBoardContext = true;
		UpdateBoardSpatialContext(boardManager);

		_targetYaw = ClampYaw(_targetYaw);
		_currentYaw = ClampYaw(_currentYaw);
		UpdateTransitionTargets();
	}

	public bool FocusTile(HexTile tile)
	{
		if (!_interactionEnabled || tile == null || !IsInstanceValid(tile))
			return false;

		if (_overviewTiles.Contains(tile))
			return ShowBoardOverview();

		if (!HasTileFocus)
		{
			_generalYaw = _targetYaw;
			_generalPitch = _targetPitch;
			_generalDistance = _targetDistance;
		}

		_isBoardOverviewActive = false;
		_focusedTile = tile;
		_isYawDragging = false;
		_isPitchDragging = false;

		float minPitch = Mathf.DegToRad(GetMinimumPitchDegrees());
		float maxPitch = Mathf.DegToRad(GetMaximumPitchDegrees());
		float focusDistance = Mathf.Clamp(
			GetConfiguredFocusDistance(),
			GetMinimumDistance(),
			GetMaximumDistanceForPitch(_targetPitch));

		BeginFocusTransition(
			tile.GlobalPosition + Vector3.Up * FocusHeightOffset,
			ClampYaw(_targetYaw),
			Mathf.Clamp(_targetPitch, minPitch, maxPitch),
			focusDistance);
		RefreshCollisionExclusions(tile);
		return true;
	}

	public bool ShowBoardOverview()
	{
		if (!_interactionEnabled)
			return false;

		_focusedTile = null;
		_isBoardOverviewActive = true;
		_isYawDragging = false;
		_isPitchDragging = false;

		float overviewYaw = _hasBoardContext
			? ClampYaw(Mathf.Atan2(_treeFrontDirection.X, _treeFrontDirection.Z))
			: ClampYaw(Mathf.DegToRad(StartYawDegrees));
		float overviewPitch = Mathf.DegToRad(Mathf.Clamp(
			OverviewPitchDegrees,
			GetMinimumPitchDegrees(),
			GetMaximumPitchDegrees()));
		float overviewDistance = GetMaximumDistanceForPitch(overviewPitch);

		BeginFocusTransition(
			GetDefaultPivotPosition(),
			overviewYaw,
			overviewPitch,
			overviewDistance);
		RefreshCollisionExclusions(_focusTarget);
		return true;
	}

	public bool ClearTileFocus()
	{
		if (!HasTileFocus)
			return false;

		_focusedTile = null;
		_isBoardOverviewActive = false;
		_isYawDragging = false;
		_isPitchDragging = false;

		float minPitch = Mathf.DegToRad(GetMinimumPitchDegrees());
		float maxPitch = Mathf.DegToRad(GetMaximumPitchDegrees());
		float generalPitch = Mathf.Clamp(_generalPitch, minPitch, maxPitch);

		BeginFocusTransition(
			GetDefaultPivotPosition(),
			ClampYaw(_generalYaw),
			generalPitch,
			Mathf.Clamp(
				_generalDistance,
				GetMinimumDistance(),
				GetMaximumDistanceForPitch(generalPitch)));
		RefreshCollisionExclusions(_focusTarget);
		return true;
	}

	public void SetInteractionEnabled(bool isEnabled)
	{
		_interactionEnabled = isEnabled;
		SetProcessUnhandledInput(isEnabled);

		if (isEnabled)
			return;

		_isYawDragging = false;
		_isPitchDragging = false;
	}

	private void HandleMouseButtons(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseButton)
			return;

		if (mouseButton.ButtonIndex == YawButton)
			_isYawDragging = mouseButton.Pressed;

		if (mouseButton.ButtonIndex == PitchButton)
			_isPitchDragging = mouseButton.Pressed;
	}

	private void HandleMouseMotion(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseMotion mouseMotion)
			return;

		Vector2 delta = mouseMotion.Relative;
		float sensitivityMultiplier = GetSettingMultiplier(
			CameraSensitivitySetting);

		if (_isYawDragging)
		{
			_targetYaw -= Mathf.DegToRad(
				delta.X * YawSensitivity * sensitivityMultiplier);
			_targetYaw = ClampYaw(_targetYaw);
		}

		if (_isPitchDragging)
		{
			float verticalDirection = GetInvertVerticalSetting() ? -1.0f : 1.0f;
			_targetPitch -= Mathf.DegToRad(
				delta.Y *
				PitchSensitivity *
				sensitivityMultiplier *
				verticalDirection);

			float minPitch = Mathf.DegToRad(GetMinimumPitchDegrees());
			float maxPitch = Mathf.DegToRad(GetMaximumPitchDegrees());

			_targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
			_targetDistance = Mathf.Min(
				_targetDistance,
				GetMaximumDistanceForPitch(_targetPitch));
		}

		UpdateTransitionTargets();
	}

	private void HandleZoom(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
			return;

		float zoomStep = ZoomStep * GetSettingMultiplier(ZoomSensitivitySetting);

		if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			_targetDistance -= zoomStep;
		else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			_targetDistance += zoomStep;
		else
			return;

		_targetDistance = Mathf.Clamp(
			_targetDistance,
			GetMinimumDistance(),
			GetMaximumDistanceForPitch(_targetPitch));
		UpdateTransitionTargets();
	}

	private void BeginFocusTransition(
		Vector3 pivot,
		float yaw,
		float pitch,
		float distance)
	{
		_focusTransitionElapsed = 0.0f;
		_focusStartPivot = _currentPivot;
		_focusEndPivot = pivot;
		_focusStartYaw = _currentYaw;
		_focusEndYaw = yaw;
		_focusStartPitch = _currentPitch;
		_focusEndPitch = pitch;
		_focusStartDistance = _currentDistance;
		_focusEndDistance = distance;

		_targetPivot = pivot;
		_targetYaw = yaw;
		_targetPitch = pitch;
		_targetDistance = distance;
		_isFocusTransitionActive = FocusDuration > 0.0f;

		if (_isFocusTransitionActive)
			return;

		_currentPivot = pivot;
		_currentYaw = yaw;
		_currentPitch = pitch;
		_currentDistance = distance;
	}

	private void UpdateFocusTransition(float delta)
	{
		_focusTransitionElapsed += delta;

		float progress = Mathf.Clamp(
			_focusTransitionElapsed / Mathf.Max(FocusDuration, 0.001f),
			0.0f,
			1.0f);
		float easedProgress = progress * progress * (3.0f - 2.0f * progress);

		_currentPivot = _focusStartPivot.Lerp(_focusEndPivot, easedProgress);
		_currentYaw = Mathf.LerpAngle(_focusStartYaw, _focusEndYaw, easedProgress);
		_currentPitch = Mathf.Lerp(_focusStartPitch, _focusEndPitch, easedProgress);
		_currentDistance = Mathf.Lerp(
			_focusStartDistance,
			_focusEndDistance,
			easedProgress);

		if (progress < 1.0f)
			return;

		_isFocusTransitionActive = false;
		_currentPivot = _targetPivot;
		_currentYaw = _targetYaw;
		_currentPitch = _targetPitch;
		_currentDistance = _targetDistance;
	}

	private void UpdateTransitionTargets()
	{
		if (!_isFocusTransitionActive)
			return;

		_focusEndYaw = _targetYaw;
		_focusEndPitch = _targetPitch;
		_focusEndDistance = _targetDistance;
	}

	private float GetMinimumPitchDegrees()
	{
		return HasTileFocus
			? Mathf.Min(FocusMinPitchDegrees, FocusMaxPitchDegrees)
			: Mathf.Min(MinPitchDegrees, MaxPitchDegrees);
	}

	private float GetMaximumPitchDegrees()
	{
		return HasTileFocus
			? Mathf.Max(FocusMinPitchDegrees, FocusMaxPitchDegrees)
			: Mathf.Max(MinPitchDegrees, MaxPitchDegrees);
	}

	private float GetMinimumDistance()
	{
		return HasTileFocus
			? Mathf.Min(FocusMinDistance, FocusMaxDistance)
			: Mathf.Min(MinDistance, MaxDistance);
	}

	private float GetMaximumDistanceForPitch(float pitch)
	{
		if (HasTileFocus)
			return GetMaximumFocusDistance();

		float maximumDistance = MaxDistance;

		if (EnablePitchDependentZoom && MaxTopDownDistanceBonus > 0.0f)
		{
			float startPitch = Mathf.DegToRad(PitchZoomStartDegrees);
			float maximumPitch = Mathf.DegToRad(GetMaximumPitchDegrees());
			float pitchRange = maximumPitch - startPitch;

			if (pitchRange > 0.001f)
			{
				float topDownProgress = Mathf.Clamp(
					(pitch - startPitch) / pitchRange,
					0.0f,
					1.0f);

				maximumDistance += MaxTopDownDistanceBonus * topDownProgress;
			}
		}

		if (_isBoardOverviewActive)
		{
			maximumDistance *= Mathf.Clamp(
				GetConfiguredOverviewDistanceMultiplier(),
				0.5f,
				1.5f);
		}

		return maximumDistance;
	}

	private float GetMaximumFocusDistance()
	{
		float maximumDistance = Mathf.Max(FocusMinDistance, FocusMaxDistance);

		if (!_hasBoardContext ||
			(_boardSideExtent <= 0.001f && _boardFrontExtent <= 0.001f) ||
			_focusedTile == null ||
			!IsInstanceValid(_focusedTile))
		{
			return maximumDistance;
		}

		Vector3 offsetFromCenter = _focusedTile.GlobalPosition - _boardCenter;
		offsetFromCenter.Y = 0.0f;

		float sideRatio = _boardSideExtent > 0.001f
			? Mathf.Clamp(
				Mathf.Abs(offsetFromCenter.Dot(_boardSideDirection)) /
					_boardSideExtent,
				0.0f,
				1.0f)
			: 0.0f;
		float frontRatio = _boardFrontExtent > 0.001f
			? Mathf.Clamp(
				Mathf.Max(
					offsetFromCenter.Dot(_treeFrontDirection),
					0.0f) /
					_boardFrontExtent,
				0.0f,
				1.0f)
			: 0.0f;
		float edgeRatio = Mathf.Max(sideRatio, frontRatio);
		float edgeStart = Mathf.Clamp(EdgeFocusStartRatio, 0.0f, 0.99f);
		float edgeProgress = Mathf.Clamp(
			Mathf.InverseLerp(edgeStart, 1.0f, edgeRatio),
			0.0f,
			1.0f);
		float edgeMaximum = Mathf.Clamp(
			EdgeFocusMaxDistance,
			FocusMinDistance,
			maximumDistance);

		return Mathf.Lerp(maximumDistance, edgeMaximum, edgeProgress);
	}

	private void UpdateBoardSpatialContext(BoardManager boardManager)
	{
		_overviewTiles.Clear();
		_overviewTiles.Add(_mainTreeTile);

		List<HexTile> tileViews = new();
		Vector3 positionSum = Vector3.Zero;

		foreach (HexCoord coord in boardManager.BoardData.Tiles.Keys)
		{
			HexTile tileView = boardManager.GetTileView(coord);

			if (tileView == null || !IsInstanceValid(tileView))
				continue;

			tileViews.Add(tileView);
			positionSum += tileView.GlobalPosition;
		}

		_boardCenter = tileViews.Count > 0
			? positionSum / tileViews.Count
			: boardManager.GlobalPosition;
		_boardSideDirection = new Vector3(
			_treeFrontDirection.Z,
			0.0f,
			-_treeFrontDirection.X).Normalized();
		_boardSideExtent = 0.0f;
		_boardFrontExtent = 0.0f;

		List<(HexTile Tile, float DistanceSquared, float SideDistance)>
			overviewCandidates = new();

		foreach (HexTile tileView in tileViews)
		{
			Vector3 centerOffset = tileView.GlobalPosition - _boardCenter;
			centerOffset.Y = 0.0f;
			_boardSideExtent = Mathf.Max(
				_boardSideExtent,
				Mathf.Abs(centerOffset.Dot(_boardSideDirection)));
			_boardFrontExtent = Mathf.Max(
				_boardFrontExtent,
				centerOffset.Dot(_treeFrontDirection));

			if (tileView == _mainTreeTile)
				continue;

			Vector3 treeOffset = tileView.GlobalPosition - _mainTreeTile.GlobalPosition;
			treeOffset.Y = 0.0f;

			if (treeOffset.Dot(_treeFrontDirection) <= 0.001f)
				continue;

			overviewCandidates.Add((
				tileView,
				treeOffset.LengthSquared(),
				Mathf.Abs(treeOffset.Dot(_boardSideDirection))));
		}

		overviewCandidates.Sort((left, right) =>
		{
			float distanceDifference =
				left.DistanceSquared - right.DistanceSquared;

			if (Mathf.Abs(distanceDifference) > 0.001f)
				return distanceDifference < 0.0f ? -1 : 1;

			return left.SideDistance.CompareTo(right.SideDistance);
		});

		int overviewTileCount = Mathf.Min(3, overviewCandidates.Count);

		for (int index = 0; index < overviewTileCount; index++)
			_overviewTiles.Add(overviewCandidates[index].Tile);
	}

	private float ClampYaw(float yaw)
	{
		if (!LimitYaw)
			return yaw;

		float clampedYaw = ClampConfiguredYaw(yaw);

		if (!_hasBoardContext)
			return clampedYaw;

		float treeFrontYaw = Mathf.Atan2(
			_treeFrontDirection.X,
			_treeFrontDirection.Z);
		float maximumSideAngle = Mathf.DegToRad(Mathf.Clamp(
			AllowedTreeSideAngleDegrees,
			1.0f,
			89.0f));
		float relativeYaw = Mathf.Wrap(
			clampedYaw - treeFrontYaw,
			-Mathf.Pi,
			Mathf.Pi);

		clampedYaw = treeFrontYaw + Mathf.Clamp(
			relativeYaw,
			-maximumSideAngle,
			maximumSideAngle);
		return ClampConfiguredYaw(clampedYaw);
	}

	private float ClampConfiguredYaw(float yaw)
	{
		if (!LimitYaw)
			return yaw;

		float minYaw = Mathf.DegToRad(Mathf.Min(MinYawDegrees, MaxYawDegrees));
		float maxYaw = Mathf.DegToRad(Mathf.Max(MinYawDegrees, MaxYawDegrees));
		return Mathf.Clamp(yaw, minYaw, maxYaw);
	}

	private Vector3 GetDefaultPivotPosition()
	{
		if (_focusTarget != null && IsInstanceValid(_focusTarget))
			return _focusTarget.GlobalPosition + FocusOffset;

		return GlobalPosition + FocusOffset;
	}

	private void UpdateCameraPosition(float delta)
	{
		float horizontalDistance = Mathf.Cos(_currentPitch) * _currentDistance;
		float height = Mathf.Sin(_currentPitch) * _currentDistance;

		Vector3 cameraOffset = new Vector3(
			Mathf.Sin(_currentYaw) * horizontalDistance,
			height,
			Mathf.Cos(_currentYaw) * horizontalDistance);
		Vector3 desiredCameraPosition = _currentPivot + cameraOffset;

		desiredCameraPosition = ClampToBoardView(desiredCameraPosition);
		Vector3 cameraDirection = desiredCameraPosition - _currentPivot;
		float desiredDistance = cameraDirection.Length();

		if (desiredDistance <= 0.001f)
			return;

		cameraDirection /= desiredDistance;
		float safeDistance = GetCollisionSafeDistance(
			_currentPivot,
			desiredCameraPosition,
			desiredDistance);

		if (safeDistance < _resolvedCollisionDistance || delta <= 0.0f)
		{
			_resolvedCollisionDistance = safeDistance;
		}
		else
		{
			float recoveryT = 1.0f - Mathf.Exp(-SmoothSpeed * delta);
			_resolvedCollisionDistance = Mathf.Lerp(
				_resolvedCollisionDistance,
				safeDistance,
				recoveryT);
		}

		float resolvedDistance = Mathf.Min(
			desiredDistance,
			_resolvedCollisionDistance);
		Camera.GlobalPosition = _currentPivot + cameraDirection * resolvedDistance;
		Camera.LookAt(_currentPivot, Vector3.Up);
	}

	private Vector3 ClampToBoardView(Vector3 cameraPosition)
	{
		if (!_hasBoardContext)
			return cameraPosition;

		cameraPosition.Y = Mathf.Max(
			cameraPosition.Y,
			_boardGroundY + GroundSafetyDistance);

		if (!LimitYaw ||
			_mainTreeTile == null ||
			!IsInstanceValid(_mainTreeTile))
			return cameraPosition;

		float treePlaneDistance = (
			cameraPosition - _mainTreeTile.GlobalPosition
		).Dot(_treeFrontDirection);

		if (treePlaneDistance < CollisionSafetyDistance)
		{
			cameraPosition += _treeFrontDirection * (
				CollisionSafetyDistance - treePlaneDistance);
		}

		return cameraPosition;
	}

	private float GetCollisionSafeDistance(
		Vector3 rayOrigin,
		Vector3 rayEnd,
		float desiredDistance)
	{
		World3D world = GetViewport()?.World3D;

		if (world == null)
			return desiredDistance;

		Vector3 collisionRayOrigin = GetCollisionRayOrigin(rayOrigin);
		float collisionRayLength = collisionRayOrigin.DistanceTo(rayEnd);

		if (collisionRayLength <= 0.001f)
			return desiredDistance;

		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(collisionRayOrigin, rayEnd);
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;
		query.Exclude = _collisionExclusions;

		Godot.Collections.Dictionary result =
			world.DirectSpaceState.IntersectRay(query);

		if (result.Count == 0)
			return desiredDistance;

		Vector3 hitPosition = result["position"].AsVector3();
		float safeRayProgress = Mathf.Clamp(
			(collisionRayOrigin.DistanceTo(hitPosition) -
				CollisionSafetyDistance) /
			collisionRayLength,
			0.0f,
			1.0f);
		return Mathf.Max(
			0.5f,
			desiredDistance * safeRayProgress);
	}

	private Vector3 GetCollisionRayOrigin(Vector3 pivot)
	{
		float boardSurfaceY = _hasBoardContext
			? _boardGroundY
			: _focusTarget?.GlobalPosition.Y ?? GlobalPosition.Y;

		pivot.Y = Mathf.Max(
			pivot.Y,
			boardSurfaceY + GroundSafetyDistance);
		return pivot;
	}

	private void RefreshCollisionExclusions(Node focusNode)
	{
		_collisionExclusions.Clear();
		AddCollisionExclusions(focusNode);
	}

	private void AddCollisionExclusions(Node node)
	{
		if (node == null)
			return;

		if (node is CollisionObject3D collisionObject)
			_collisionExclusions.Add(collisionObject.GetRid());

		foreach (Node child in node.GetChildren())
			AddCollisionExclusions(child);
	}

	private static float GetSettingMultiplier(string settingName)
	{
		return Mathf.Clamp(
			ProjectSettings.GetSetting(settingName, 1.0f).AsSingle(),
			0.5f,
			2.0f);
	}

	private float GetConfiguredFocusDistance()
	{
		return ProjectSettings
			.GetSetting(TileFocusDistanceSetting, FocusDistance)
			.AsSingle();
	}

	private float GetConfiguredOverviewDistanceMultiplier()
	{
		return ProjectSettings
			.GetSetting(BoardOverviewDistanceSetting, OverviewDistanceMultiplier)
			.AsSingle();
	}

	private static bool GetInvertVerticalSetting()
	{
		return ProjectSettings
			.GetSetting(InvertVerticalSetting, false)
			.AsBool();
	}
}
