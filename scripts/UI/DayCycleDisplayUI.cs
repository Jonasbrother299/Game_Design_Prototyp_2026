using Godot;

public partial class DayCycleDisplayUI : TextureRect
{
	private enum WeatherState
	{
		Clear,
		Rain,
		HeavyRain
	}

	private static readonly Vector2[] StarPositions =
	{
		new(30.0f, 31.0f),
		new(47.0f, 22.0f),
		new(92.0f, 29.0f),
		new(104.0f, 51.0f),
		new(29.0f, 64.0f),
		new(91.0f, 67.0f)
	};

	private const float IconSize = 128.0f;
	private const float NightStart = 0.56f;
	private float _nightness;
	private float _transitionStart;
	private float _transitionTarget;
	private float _transitionDuration;
	private float _transitionElapsed;
	private bool _isSunrise;
	private bool _isTransitioning;
	private WeatherState _weatherState;

	public override void _Ready()
	{
		Texture = null;
		MouseFilter = MouseFilterEnum.Ignore;
		ProcessMode = ProcessModeEnum.Always;
		ShowDay();
	}

	public override void _Process(double delta)
	{
		if (!_isTransitioning)
			return;

		_transitionElapsed += (float)delta;
		float progress = Mathf.Clamp(
			_transitionElapsed / _transitionDuration,
			0.0f,
			1.0f);
		SetNightness(Mathf.Lerp(
			_transitionStart,
			_transitionTarget,
			EaseInOut(progress)));

		if (progress < 1.0f)
			return;

		_isTransitioning = false;
		if (_transitionTarget <= 0.0f)
			ShowDay();
		else
			ShowNight();
	}

	public void ShowDay()
	{
		_isTransitioning = false;
		_isSunrise = false;
		SetNightness(0.0f);
	}

	public void PlaySunset(float duration)
	{
		_isSunrise = false;
		StartTransition(1.0f, duration);
	}

	public void ShowNight()
	{
		_isTransitioning = false;
		_isSunrise = false;
		SetNightness(1.0f);
	}

	public void PlaySunrise(float duration)
	{
		_isSunrise = true;
		StartTransition(0.0f, duration);
	}

	public void SetWeather(bool hasRain, bool hasHeavyRain)
	{
		WeatherState weatherState = hasHeavyRain
			? WeatherState.HeavyRain
			: hasRain
				? WeatherState.Rain
				: WeatherState.Clear;
		if (_weatherState == weatherState)
			return;

		_weatherState = weatherState;
		UpdateTooltip();
		QueueRedraw();
	}

	private void StartTransition(float targetNightness, float duration)
	{
		_transitionStart = _nightness;
		_transitionTarget = Mathf.Clamp(targetNightness, 0.0f, 1.0f);
		_transitionDuration = Mathf.Max(duration, 0.01f);
		_transitionElapsed = 0.0f;
		_isTransitioning = true;
	}

	private void SetNightness(float nightness)
	{
		_nightness = Mathf.Clamp(nightness, 0.0f, 1.0f);
		UpdateTooltip();
		QueueRedraw();
	}

	private void UpdateTooltip()
	{
		if (_nightness <= 0.01f)
			TooltipText = "Tag";
		else if (_nightness >= 0.99f)
			TooltipText = "Nacht";
		else
			TooltipText = _isSunrise ? "Sonnenaufgang" : "Sonnenuntergang";

		if (_weatherState == WeatherState.Rain)
			TooltipText += " – Regen";
		else if (_weatherState == WeatherState.HeavyRain)
			TooltipText += " – Starkregen";
	}

	public override void _Draw()
	{
		Vector2 scale = Size / IconSize;
		DrawSetTransform(Vector2.Zero, 0.0f, scale);

		DrawCircle(new Vector2(64.0f, 64.0f), 62.0f,
			new Color(0.19f, 0.24f, 0.18f));
		DrawCircle(new Vector2(64.0f, 64.0f), 57.0f, GetSkyColor());

		DrawStars();
		DrawSun();
		DrawMoon();
		DrawWeather();
		DrawHorizon();
		DrawArc(
			new Vector2(64.0f, 64.0f),
			59.0f,
			0.0f,
			Mathf.Tau,
			64,
			new Color(0.96f, 0.86f, 0.65f),
			3.0f,
			true);

		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
	}

	private Color GetSkyColor()
	{
		Color daySky = new(0.37f, 0.72f, 0.94f);
		Color sunsetSky = new(0.83f, 0.37f, 0.28f);
		Color nightSky = new(0.055f, 0.14f, 0.36f);

		if (_nightness < NightStart)
		{
			return daySky.Lerp(
				sunsetSky,
				_nightness / NightStart);
		}

		return sunsetSky.Lerp(
			nightSky,
			(_nightness - NightStart) / (1.0f - NightStart));
	}

	private void DrawSun()
	{
		float sunProgress = Mathf.Clamp(_nightness / 0.62f, 0.0f, 1.0f);
		float visibility = 1.0f - Mathf.Clamp(
			(_nightness - 0.56f) / 0.06f,
			0.0f,
			1.0f);
		if (visibility <= 0.0f)
			return;

		Vector2 sunPosition = new(
			64.0f,
			Mathf.Lerp(42.0f, 96.0f, sunProgress));
		Color sunColor = new Color(1.0f, 0.90f, 0.32f).Lerp(
			new Color(0.96f, 0.31f, 0.16f),
			Mathf.Clamp(_nightness / 0.72f, 0.0f, 1.0f));
		sunColor.A = visibility;

		DrawCircle(sunPosition, 26.0f, new Color(0.65f, 0.19f, 0.10f, visibility));
		DrawCircle(sunPosition, 21.0f, sunColor);

		for (int rayIndex = 0; rayIndex < 8; rayIndex++)
		{
			float angle = Mathf.Tau * rayIndex / 8.0f;
			Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
			float rayStart = 28.0f;
			float rayEnd = Mathf.Min(
				35.0f,
				GetDistanceToCircleEdge(sunPosition, direction, 55.0f));
			if (rayEnd <= rayStart)
				continue;

			DrawLine(
				sunPosition + (direction * rayStart),
				sunPosition + (direction * rayEnd),
				new Color(1.0f, 0.79f, 0.28f, visibility),
				3.0f,
				true);
		}
	}

	private void DrawMoon()
	{
		float moonProgress = Mathf.Clamp(
			(_nightness - 0.62f) / 0.38f,
			0.0f,
			1.0f);
		if (moonProgress <= 0.0f)
			return;

		Vector2 moonPosition = new(
			64.0f,
			Mathf.Lerp(27.0f, 38.0f, moonProgress));
		Color moonColor = new(0.93f, 0.95f, 1.0f, moonProgress);
		DrawCircle(moonPosition, 22.0f, moonColor);
		DrawCircle(
			moonPosition + new Vector2(9.0f, -4.0f),
			20.0f,
			GetSkyColor());
	}

	private void DrawStars()
	{
		float starVisibility = Mathf.Clamp(
			(_nightness - 0.66f) / 0.34f,
			0.0f,
			1.0f);
		if (starVisibility <= 0.0f)
			return;

		foreach (Vector2 starPosition in StarPositions)
		{
			DrawCircle(
				starPosition,
				2.4f,
				new Color(0.94f, 0.96f, 1.0f, starVisibility));
		}
	}

	private void DrawWeather()
	{
		if (_weatherState == WeatherState.Clear)
			return;

		bool isHeavyRain = _weatherState == WeatherState.HeavyRain;
		Color cloudOutline = isHeavyRain
			? new Color(0.035f, 0.055f, 0.10f)
			: new Color(0.24f, 0.32f, 0.38f);
		Color cloudColor = isHeavyRain
			? new Color(0.13f, 0.18f, 0.29f)
			: new Color(0.57f, 0.66f, 0.73f);
		float cloudScale = isHeavyRain ? 1.10f : 1.0f;
		Vector2 cloudCenter = isHeavyRain
			? new Vector2(65.0f, 62.0f)
			: new Vector2(76.0f, 62.0f);

		DrawCircle(
			cloudCenter + new Vector2(-17.0f, 7.0f) * cloudScale,
			18.0f * cloudScale,
			cloudOutline);
		DrawCircle(
			cloudCenter + new Vector2(0.0f, -4.0f) * cloudScale,
			23.0f * cloudScale,
			cloudOutline);
		DrawCircle(
			cloudCenter + new Vector2(20.0f, 6.0f) * cloudScale,
			17.0f * cloudScale,
			cloudOutline);
		DrawRect(
			new Rect2(
				cloudCenter + new Vector2(-35.0f, 3.0f) * cloudScale,
				new Vector2(71.0f, 23.0f) * cloudScale),
			cloudOutline);

		DrawCircle(
			cloudCenter + new Vector2(-16.0f, 6.0f) * cloudScale,
			15.0f * cloudScale,
			cloudColor);
		DrawCircle(
			cloudCenter + new Vector2(0.0f, -4.0f) * cloudScale,
			20.0f * cloudScale,
			cloudColor);
		DrawCircle(
			cloudCenter + new Vector2(19.0f, 5.0f) * cloudScale,
			14.0f * cloudScale,
			cloudColor);
		DrawRect(
			new Rect2(
				cloudCenter + new Vector2(-31.0f, 3.0f) * cloudScale,
				new Vector2(63.0f, 18.0f) * cloudScale),
			cloudColor);
	}

	private void DrawHorizon()
	{
		Color distantHill = new Color(0.20f, 0.36f, 0.25f).Lerp(
			new Color(0.035f, 0.09f, 0.16f),
			_nightness);
		Color foregroundHill = new Color(0.12f, 0.25f, 0.15f).Lerp(
			new Color(0.02f, 0.055f, 0.10f),
			_nightness);

		DrawCircle(new Vector2(45.0f, 96.0f), 17.0f, distantHill);
		DrawCircle(new Vector2(82.0f, 98.0f), 18.0f, distantHill);
		DrawCircle(new Vector2(64.0f, 105.0f), 12.0f, distantHill);
		DrawCircle(new Vector2(49.0f, 105.0f), 12.0f, foregroundHill);
		DrawCircle(new Vector2(76.0f, 107.0f), 13.0f, foregroundHill);
		DrawCircle(new Vector2(64.0f, 112.0f), 8.0f, foregroundHill);
	}

	private static float EaseInOut(float value)
	{
		return value * value * (3.0f - (2.0f * value));
	}

	private static float GetDistanceToCircleEdge(
		Vector2 position,
		Vector2 direction,
		float radius)
	{
		Vector2 offset = position - new Vector2(64.0f, 64.0f);
		float projection = offset.Dot(direction);
		float discriminant = (projection * projection) +
			(radius * radius) -
			offset.LengthSquared();
		if (discriminant <= 0.0f)
			return 0.0f;

		return -projection + Mathf.Sqrt(discriminant);
	}
}
