using Godot;
using System;

public partial class PestCameraEdgeOverlay : Control
{
	public int InsectCount { get; set; } = 5;
	public int LayoutSeed { get; set; } = 9421;
	public float CrawlSpeed { get; set; } = 75.0f;
	public float InsectSize { get; set; } = 10.0f;
	public float EdgeInset { get; set; } = 22.0f;
	public float EdgeWobble { get; set; } = 6.0f;
	public float EffectOpacity { get; set; } = 0.78f;
	public float EffectBlend { get; set; }
	public Color InsectColor { get; set; } =
		new Color(0.045f, 0.025f, 0.012f);
	public Color OutlineColor { get; set; } =
		new Color(0.85f, 0.5f, 0.14f);

	private float _animationTime;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		if (!Visible)
			return;

		_animationTime += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		int count = Math.Clamp(InsectCount, 0, 12);
		float blend = Mathf.Clamp(EffectBlend, 0.0f, 1.0f);
		if (count == 0 || blend <= 0.001f || Size.X <= 1.0f || Size.Y <= 1.0f)
			return;

		float safeSize = Mathf.Max(InsectSize, 2.0f);
		float safeWobble = Mathf.Max(EdgeWobble, 0.0f);
		float safeInset = Mathf.Max(
			EdgeInset,
			safeSize * 1.4f + safeWobble);
		float horizontalLength = Mathf.Max(Size.X - safeInset * 2.0f, 1.0f);
		float verticalLength = Mathf.Max(Size.Y - safeInset * 2.0f, 1.0f);
		float perimeterLength = (horizontalLength + verticalLength) * 2.0f;
		float opacity = Mathf.Clamp(EffectOpacity, 0.0f, 1.0f) * blend;
		Color bodyColor = WithOpacity(InsectColor, opacity);
		Color outlineColor = WithOpacity(OutlineColor, opacity * 0.9f);

		for (int index = 0; index < count; index++)
		{
			int seed = LayoutSeed + index * 317;
			float phase = Hash01(seed);
			float speedVariation = Mathf.Lerp(0.72f, 1.28f, Hash01(seed + 23));
			float direction = Hash01(seed + 47) < 0.34f ? -1.0f : 1.0f;
			float progress = Fract(
				phase +
				_animationTime * Mathf.Max(CrawlSpeed, 0.0f) *
					speedVariation * direction / perimeterLength);
			GetPerimeterPose(
				progress * perimeterLength,
				safeInset,
				horizontalLength,
				verticalLength,
				out Vector2 position,
				out Vector2 tangent);

			Vector2 inwardNormal = new Vector2(-tangent.Y, tangent.X);
			float wobble = Mathf.Sin(
				_animationTime * Mathf.Lerp(1.2f, 2.4f, Hash01(seed + 71)) +
				Hash01(seed + 89) * Mathf.Tau);
			position += inwardNormal * wobble * safeWobble;
			float size = safeSize * Mathf.Lerp(0.78f, 1.2f, Hash01(seed + 107));
			DrawInsect(
				position,
				tangent.Angle(),
				size,
				bodyColor,
				outlineColor);
		}

		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
	}

	private void DrawInsect(
		Vector2 position,
		float rotation,
		float size,
		Color bodyColor,
		Color outlineColor)
	{
		DrawSetTransform(position, rotation, Vector2.One);
		float outlineWidth = Mathf.Max(size * 0.16f, 1.2f);
		float legWidth = Mathf.Max(size * 0.09f, 0.9f);

		for (int legIndex = 0; legIndex < 3; legIndex++)
		{
			float x = Mathf.Lerp(-0.34f, 0.22f, legIndex / 2.0f) * size;
			float forwardOffset = (legIndex - 1) * size * 0.12f;
			Vector2 upperStart = new Vector2(x, -size * 0.10f);
			Vector2 lowerStart = new Vector2(x, size * 0.10f);
			Vector2 upperEnd = new Vector2(
				x + forwardOffset,
				-size * 0.58f);
			Vector2 lowerEnd = new Vector2(
				x + forwardOffset,
				size * 0.58f);
			DrawLine(
				upperStart,
				upperEnd,
				outlineColor,
				outlineWidth,
				true);
			DrawLine(
				lowerStart,
				lowerEnd,
				outlineColor,
				outlineWidth,
				true);
			DrawLine(
				upperStart,
				upperEnd,
				bodyColor,
				legWidth,
				true);
			DrawLine(
				lowerStart,
				lowerEnd,
				bodyColor,
				legWidth,
				true);
		}

		Vector2 antennaStartA = new Vector2(size * 0.35f, -size * 0.12f);
		Vector2 antennaStartB = new Vector2(size * 0.35f, size * 0.12f);
		Vector2 antennaEndA = new Vector2(size * 0.78f, -size * 0.42f);
		Vector2 antennaEndB = new Vector2(size * 0.78f, size * 0.42f);
		DrawLine(
			antennaStartA,
			antennaEndA,
			outlineColor,
			outlineWidth,
			true);
		DrawLine(
			antennaStartB,
			antennaEndB,
			outlineColor,
			outlineWidth,
			true);
		DrawLine(
			antennaStartA,
			antennaEndA,
			bodyColor,
			legWidth,
			true);
		DrawLine(
			antennaStartB,
			antennaEndB,
			bodyColor,
			legWidth,
			true);

		DrawCircle(
			new Vector2(-size * 0.32f, 0.0f),
			size * 0.37f,
			outlineColor,
			true,
			-1.0f,
			true);
		DrawCircle(
			new Vector2(size * 0.05f, 0.0f),
			size * 0.25f,
			outlineColor,
			true,
			-1.0f,
			true);
		DrawCircle(
			new Vector2(size * 0.38f, 0.0f),
			size * 0.24f,
			outlineColor,
			true,
			-1.0f,
			true);
		DrawCircle(
			new Vector2(-size * 0.32f, 0.0f),
			size * 0.27f,
			bodyColor,
			true,
			-1.0f,
			true);
		DrawCircle(
			new Vector2(size * 0.05f, 0.0f),
			size * 0.17f,
			bodyColor,
			true,
			-1.0f,
			true);
		DrawCircle(
			new Vector2(size * 0.38f, 0.0f),
			size * 0.16f,
			bodyColor,
			true,
			-1.0f,
			true);
	}

	private void GetPerimeterPose(
		float distance,
		float inset,
		float horizontalLength,
		float verticalLength,
		out Vector2 position,
		out Vector2 tangent)
	{
		if (distance < horizontalLength)
		{
			position = new Vector2(inset + distance, inset);
			tangent = Vector2.Right;
			return;
		}

		distance -= horizontalLength;
		if (distance < verticalLength)
		{
			position = new Vector2(Size.X - inset, inset + distance);
			tangent = Vector2.Down;
			return;
		}

		distance -= verticalLength;
		if (distance < horizontalLength)
		{
			position = new Vector2(Size.X - inset - distance, Size.Y - inset);
			tangent = Vector2.Left;
			return;
		}

		distance -= horizontalLength;
		position = new Vector2(inset, Size.Y - inset - distance);
		tangent = Vector2.Up;
	}

	private static Color WithOpacity(Color color, float opacity)
	{
		return new Color(
			color.R,
			color.G,
			color.B,
			color.A * opacity);
	}

	private static float Hash01(int value)
	{
		uint hash = unchecked((uint)value);
		hash ^= hash >> 16;
		hash *= 0x7feb352d;
		hash ^= hash >> 15;
		hash *= 0x846ca68b;
		hash ^= hash >> 16;
		return (hash & 0x00ffffff) / 16777215.0f;
	}

	private static float Fract(float value)
	{
		return value - Mathf.Floor(value);
	}
}
