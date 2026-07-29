using Godot;

public partial class CameraRigController : Node3D
{
	[Export] public Camera3D Camera;

	[ExportGroup("Input")]
	[Export] public MouseButton YawButton = MouseButton.Left;
	[Export] public MouseButton PitchButton = MouseButton.Right;
	[Export] public float YawSensitivity = 0.18f;
	[Export] public float PitchSensitivity = 0.18f;
	[Export] public float SmoothSpeed = 10.0f;

	[ExportGroup("Focus")]
	[Export] public NodePath FocusTargetPath;
	[Export] public Vector3 FocusOffset = new Vector3(0.0f, 1.4f, 0.0f);

	[ExportGroup("Yaw Limits")]
	[Export] public bool LimitYaw = true;
	[Export] public float MinYawDegrees = -25.0f;
	[Export] public float MaxYawDegrees = 115.0f;

	[ExportGroup("Pitch Limits")]
	[Export] public float MinPitchDegrees = 35.0f;
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
	public float MaxTopDownDistanceBonus = 3.0f;

	[ExportGroup("Start Values")]
	[Export] public float StartYawDegrees = 45.0f;
	[Export] public float StartPitchDegrees = 55.0f;
	[Export] public float StartDistance = 16.0f;

	private bool _isYawDragging = false;
	private bool _isPitchDragging = false;

	private float _targetYaw;
	private float _targetPitch;
	private float _targetDistance;

	private float _currentYaw;
	private float _currentPitch;
	private float _currentDistance;
	private Node3D _focusTarget;

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
		_targetPitch = Mathf.DegToRad(Mathf.Clamp(StartPitchDegrees, MinPitchDegrees, MaxPitchDegrees));
		_targetDistance = Mathf.Clamp(
			StartDistance,
			MinDistance,
			GetMaximumDistanceForPitch(_targetPitch));

		_currentYaw = _targetYaw;
		_currentPitch = _targetPitch;
		_currentDistance = _targetDistance;

		UpdateCameraPosition();
	}

	public override void _Process(double delta)
	{
		if (Camera == null)
			return;

		float smoothT = 1.0f - Mathf.Exp(-SmoothSpeed * (float)delta);
		float zoomT = 1.0f - Mathf.Exp(-ZoomSmoothSpeed * (float)delta);

		_currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, smoothT);
		_currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, smoothT);
		_currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, zoomT);

		UpdateCameraPosition();
	}

	// Use _Input so the camera receives input events before GUI nodes mark them handled.
	public override void _Input(InputEvent inputEvent)
	{
		HandleMouseButtons(inputEvent);
		HandleMouseMotion(inputEvent);
		HandleZoom(inputEvent);
	}

	public void SetInteractionEnabled(bool isEnabled)
	{
		SetProcessInput(isEnabled);

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

		if (_isYawDragging)
		{
			_targetYaw -= Mathf.DegToRad(delta.X * YawSensitivity);
			_targetYaw = ClampYaw(_targetYaw);
		}

		if (_isPitchDragging)
		{
			_targetPitch -= Mathf.DegToRad(delta.Y * PitchSensitivity);

			float minPitch = Mathf.DegToRad(MinPitchDegrees);
			float maxPitch = Mathf.DegToRad(MaxPitchDegrees);

			_targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
			_targetDistance = Mathf.Min(
				_targetDistance,
				GetMaximumDistanceForPitch(_targetPitch));
		}
	}

	private void HandleZoom(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseButton)
			return;

		if (!mouseButton.Pressed)
			return;

		if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			_targetDistance -= ZoomStep;
		else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			_targetDistance += ZoomStep;
		else
			return;

		_targetDistance = Mathf.Clamp(
			_targetDistance,
			MinDistance,
			GetMaximumDistanceForPitch(_targetPitch));
	}

	private float GetMaximumDistanceForPitch(float pitch)
	{
		if (!EnablePitchDependentZoom || MaxTopDownDistanceBonus <= 0.0f)
			return MaxDistance;

		float startPitch = Mathf.DegToRad(PitchZoomStartDegrees);
		float maximumPitch = Mathf.DegToRad(MaxPitchDegrees);
		float pitchRange = maximumPitch - startPitch;

		if (pitchRange <= 0.001f)
			return MaxDistance;

		float topDownProgress = Mathf.Clamp(
			(pitch - startPitch) / pitchRange,
			0.0f,
			1.0f);

		return MaxDistance + MaxTopDownDistanceBonus * topDownProgress;
	}

	private float ClampYaw(float yaw)
	{
		if (!LimitYaw)
			return yaw;

		float minYaw = Mathf.DegToRad(Mathf.Min(MinYawDegrees, MaxYawDegrees));
		float maxYaw = Mathf.DegToRad(Mathf.Max(MinYawDegrees, MaxYawDegrees));
		return Mathf.Clamp(yaw, minYaw, maxYaw);
	}

	private Vector3 GetFocusPosition()
	{
		if (_focusTarget != null && IsInstanceValid(_focusTarget))
			return _focusTarget.GlobalPosition + FocusOffset;

		return GlobalPosition + FocusOffset;
	}

	private void UpdateCameraPosition()
	{
		float horizontalDistance = Mathf.Cos(_currentPitch) * _currentDistance;
		float height = Mathf.Sin(_currentPitch) * _currentDistance;
		Vector3 focusPosition = GetFocusPosition();

		Vector3 cameraOffset = new Vector3(
			Mathf.Sin(_currentYaw) * horizontalDistance,
			height,
			Mathf.Cos(_currentYaw) * horizontalDistance
		);

		Camera.GlobalPosition = focusPosition + cameraOffset;
		Camera.LookAt(focusPosition, Vector3.Up);
	}
}
