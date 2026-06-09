using Godot;

public partial class CameraRigController : Node3D
{
	[Export] public Camera3D Camera;

	[Export] public float MoveSpeed = 8.0f;
	[Export] public float DragSpeed = 0.025f;

	[Export] public float MinZoom = 6.0f;
	[Export] public float MaxZoom = 18.0f;
	[Export] public float ZoomStep = 1.0f;

	[Export] public float MinX = -8.0f;
	[Export] public float MaxX = 8.0f;
	[Export] public float MinZ = -8.0f;
	[Export] public float MaxZ = 8.0f;

	private bool _isDragging = false;

	public override void _Ready()
	{
		if (Camera == null)
		{
			Camera = GetNodeOrNull<Camera3D>("Camera3D");
		}

		if (Camera == null)
		{
			GD.PrintErr("CameraRigController: Camera3D not found.");
		}
	}

	public override void _Process(double delta)
	{
		HandleKeyboardMovement((float)delta);
		ClampPosition();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		HandleZoom(inputEvent);
		HandleMouseDrag(inputEvent);
	}

	private void HandleKeyboardMovement(float delta)
	{
		Vector3 direction = Vector3.Zero;

		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
			direction.Z -= 1.0f;

		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
			direction.Z += 1.0f;

		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
			direction.X -= 1.0f;

		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
			direction.X += 1.0f;

		if (direction == Vector3.Zero)
			return;

		direction = direction.Normalized();

		Position += direction * MoveSpeed * delta;
	}

	private void HandleZoom(InputEvent inputEvent)
	{
		if (Camera == null)
			return;

		if (inputEvent is not InputEventMouseButton mouseButton)
			return;

		if (!mouseButton.Pressed)
			return;

		if (mouseButton.ButtonIndex == MouseButton.WheelUp)
		{
			Camera.Size -= ZoomStep;
		}

		if (mouseButton.ButtonIndex == MouseButton.WheelDown)
		{
			Camera.Size += ZoomStep;
		}

		Camera.Size = Mathf.Clamp(Camera.Size, MinZoom, MaxZoom);
	}

	private void HandleMouseDrag(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Middle)
			{
				_isDragging = mouseButton.Pressed;
			}
		}

		if (inputEvent is InputEventMouseMotion mouseMotion && _isDragging)
		{
			Vector2 movement = mouseMotion.Relative;

			Position += new Vector3(
				-movement.X * DragSpeed,
				0.0f,
				-movement.Y * DragSpeed
			);

			ClampPosition();
		}
	}

	private void ClampPosition()
	{
		Position = new Vector3(
			Mathf.Clamp(Position.X, MinX, MaxX),
			Position.Y,
			Mathf.Clamp(Position.Z, MinZ, MaxZ)
		);
	}
}
