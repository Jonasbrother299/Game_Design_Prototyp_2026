using Godot;

public partial class WeatherIconRainOverlay : Control
{
	private static readonly Rect2[] RainDropSourceRects =
	{
		new Rect2(225.0f, 416.0f, 64.0f, 105.0f),
		new Rect2(565.0f, 482.0f, 60.0f, 106.0f),
		new Rect2(375.0f, 472.0f, 66.0f, 105.0f),
		new Rect2(481.0f, 410.0f, 58.0f, 106.0f),
		new Rect2(116.0f, 460.0f, 58.0f, 109.0f),
		new Rect2(291.0f, 535.0f, 60.0f, 107.0f)
	};
	private static readonly float[] RainDropPhaseOffsets =
	{
		0.0f,
		0.5f,
		0.25f,
		0.75f,
		0.12f,
		0.62f
	};
	private static readonly Rect2[] HeavyRainDropSourceRects =
	{
		new Rect2(161.0f, 507.0f, 62.0f, 94.0f),
		new Rect2(578.0f, 417.0f, 61.0f, 93.0f),
		new Rect2(551.0f, 514.0f, 59.0f, 93.0f),
		new Rect2(223.0f, 415.0f, 62.0f, 93.0f),
		new Rect2(89.0f, 398.0f, 59.0f, 93.0f),
		new Rect2(468.0f, 423.0f, 58.0f, 92.0f),
		new Rect2(349.0f, 467.0f, 54.0f, 81.0f),
		new Rect2(433.0f, 568.0f, 59.0f, 93.0f),
		new Rect2(284.0f, 573.0f, 55.0f, 83.0f)
	};
	private static readonly float[] HeavyRainDropPhaseOffsets =
	{
		0.0f,
		0.43f,
		0.2f,
		0.68f,
		0.1f,
		0.55f,
		0.47f,
		0.82f,
		0.34f
	};
	private const float IconCenterX = 375.0f;
	private const float RainIconCenterY = 358.5f;
	private const float HeavyRainIconCenterY = 357.5f;
	private const float OuterCircleInnerRadius = 339.0f;
	private const float RainDropEmergenceY = 425.0f;
	private const float HeavyRainDropEmergenceY = 410.0f;
	private const float HeavyRainHorizontalDrift = 36.0f;

	private float _elapsedTime;
	private bool _isHeavyRain;
	private Texture2D _rainDropTexture;
	private Texture2D _heavyRainDropTexture;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ProcessMode = ProcessModeEnum.Always;
		ZIndex = 1;
		Visible = false;
		SetProcess(false);
	}

	public override void _Process(double delta)
	{
		_elapsedTime += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Texture2D dropTexture = _isHeavyRain
			? _heavyRainDropTexture
			: _rainDropTexture;
		if (!Visible ||
			dropTexture == null ||
			Size.X <= 0.0f ||
			Size.Y <= 0.0f)
			return;

		Vector2 textureSize = dropTexture.GetSize();
		if (textureSize.X <= 0.0f || textureSize.Y <= 0.0f)
			return;

		float textureScale = Mathf.Min(
			Size.X / textureSize.X,
			Size.Y / textureSize.Y);
		Vector2 iconOrigin = (Size - (textureSize * textureScale)) * 0.5f;
		Rect2[] sourceRects = _isHeavyRain
			? HeavyRainDropSourceRects
			: RainDropSourceRects;
		float[] phaseOffsets = _isHeavyRain
			? HeavyRainDropPhaseOffsets
			: RainDropPhaseOffsets;
		for (int index = 0; index < sourceRects.Length; index++)
		{
			float phase = Mathf.PosMod(
				(_elapsedTime * 0.9f) + phaseOffsets[index],
				1.0f);
			Rect2 sourceRect = sourceRects[index];
			float clipTopY = _isHeavyRain
				? HeavyRainDropEmergenceY
				: RainDropEmergenceY;
			float animatedX = sourceRect.Position.X;
			if (_isHeavyRain)
				animatedX += HeavyRainHorizontalDrift * phase;
			float clipBottomY = GetOuterCircleBottom(
				animatedX,
				sourceRect.Size.X);
			float startY = clipTopY - sourceRect.Size.Y;
			float animatedY = Mathf.Lerp(startY, clipBottomY, phase);

			float visibleTopY = Mathf.Max(animatedY, clipTopY);
			float visibleBottomY = Mathf.Min(
				animatedY + sourceRect.Size.Y,
				clipBottomY);
			if (visibleBottomY <= visibleTopY)
				continue;

			float clippedTop = visibleTopY - animatedY;
			float visibleHeight = visibleBottomY - visibleTopY;
			Rect2 visibleSourceRect = new Rect2(
				sourceRect.Position + new Vector2(0.0f, clippedTop),
				new Vector2(sourceRect.Size.X, visibleHeight));
			Rect2 targetRect = new Rect2(
				iconOrigin +
				(new Vector2(animatedX, visibleTopY) * textureScale),
				visibleSourceRect.Size * textureScale);

			DrawTextureRectRegion(
				dropTexture,
				targetRect,
				visibleSourceRect);
		}
	}

	private float GetOuterCircleBottom(float dropX, float dropWidth)
	{
		float leftDistance = Mathf.Abs(dropX - IconCenterX);
		float rightDistance = Mathf.Abs(
			dropX + dropWidth - IconCenterX);
		float horizontalDistance = Mathf.Max(
			leftDistance,
			rightDistance);
		float verticalDistance = Mathf.Sqrt(
			Mathf.Max(
				0.0f,
				(OuterCircleInnerRadius * OuterCircleInnerRadius) -
				(horizontalDistance * horizontalDistance)));
		float centerY = _isHeavyRain
			? HeavyRainIconCenterY
			: RainIconCenterY;
		return centerY + verticalDistance;
	}

	public void SetDropTextures(
		Texture2D rainDropTexture,
		Texture2D heavyRainDropTexture)
	{
		_rainDropTexture = rainDropTexture;
		_heavyRainDropTexture = heavyRainDropTexture;
		QueueRedraw();
	}

	public void SetRainMode(bool active, bool heavyRain)
	{
		_isHeavyRain = heavyRain;
		Visible = active;
		SetProcess(active);

		if (!active)
			_elapsedTime = 0.0f;

		QueueRedraw();
	}
}
