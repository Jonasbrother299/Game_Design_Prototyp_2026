using Godot;

public static class TreeCanopyShadowBuilder
{
	internal const uint ReceiverLayerMask = 1u << 19;

	private const string LeafTexturePath =
		"res://assets/textures/leaves/tree_canopy_shadow.svg";

	private static Texture2D _leafTexture;

	public static Node3D Create(
		Color shadowColor,
		float canopySize,
		Vector2 canopyOffset)
	{
		Node3D root = new Node3D
		{
			Name = "TreeCanopyShadow",
			Position = new Vector3(canopyOffset.X, 0.0f, canopyOffset.Y)
		};

		_leafTexture ??= GD.Load<Texture2D>(LeafTexturePath);

		if (_leafTexture == null)
		{
			GD.PushWarning(
				$"TreeCanopyShadowBuilder: Blatttextur fehlt: {LeafTexturePath}");
			return root;
		}

		float safeCanopySize = Mathf.Max(0.1f, canopySize);

		Decal canopyShadow = new Decal
		{
			Name = "CanopyShadow",
			TextureAlbedo = _leafTexture,
			Modulate = shadowColor,
			AlbedoMix = 1.0f,
			Size = new Vector3(
				safeCanopySize,
				4.0f,
				safeCanopySize),
			Position = new Vector3(0.0f, 1.5f, 0.0f),
			RotationDegrees = new Vector3(0.0f, -12.0f, 0.0f),
			CullMask = ReceiverLayerMask
		};

		root.AddChild(canopyShadow);
		return root;
	}
}
