using Godot;

public partial class DayCycleDisplayUI : TextureRect
{
	private const bool HeavyRainIconAnimationEnabled = false;

	[Export] public Texture2D SunIcon;
	[Export] public Texture2D RainIcon;
	[Export] public Texture2D RainDropsIcon;
	[Export] public Texture2D HeavyRainIcon;
	[Export] public Texture2D HeavyRainDropsIcon;
	[Export] public Texture2D DroughtIcon;
	[Export] public Texture2D HeatDayIcon;
	[Export] public Texture2D WindIcon;
	[Export] public Texture2D PestsIcon;

	private float _nightness;
	private float _transitionStart;
	private float _transitionTarget;
	private float _transitionDuration;
	private float _transitionElapsed;
	private bool _isSunrise;
	private bool _isTransitioning;
	private GameEventType _eventType;
	private WeatherIconRainOverlay _rainOverlay;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ProcessMode = ProcessModeEnum.Always;
		ClipContents = true;

		_rainOverlay = new WeatherIconRainOverlay
		{
			Name = "RainAnimation"
		};
		AddChild(_rainOverlay);
		_rainOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_rainOverlay.SetDropTextures(RainDropsIcon, HeavyRainDropsIcon);

		ShowDay();
		UpdateIcon();
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

	public void SetEvent(GameEventType eventType)
	{
		if (_eventType == eventType)
			return;

		_eventType = eventType;
		UpdateIcon();
		UpdateTooltip();
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
	}

	private void UpdateIcon()
	{
		Texture = _eventType switch
		{
			GameEventType.Rain => RainIcon,
			GameEventType.HeavyRain => HeavyRainIcon,
			GameEventType.Drought => DroughtIcon,
			GameEventType.HeatDay => HeatDayIcon,
			GameEventType.Wind => WindIcon,
			GameEventType.Pests => PestsIcon,
			_ => SunIcon
		};

		_rainOverlay?.SetRainMode(
			_eventType == GameEventType.Rain ||
			(_eventType == GameEventType.HeavyRain &&
				HeavyRainIconAnimationEnabled),
			_eventType == GameEventType.HeavyRain);
	}

	private void UpdateTooltip()
	{
		if (_nightness <= 0.01f)
			TooltipText = "Tag";
		else if (_nightness >= 0.99f)
			TooltipText = "Nacht";
		else
			TooltipText = _isSunrise ? "Sonnenaufgang" : "Sonnenuntergang";

		string eventName = _eventType switch
		{
			GameEventType.Rain => "Regen",
			GameEventType.HeavyRain => "Starkregen",
			GameEventType.Drought => "Dürre",
			GameEventType.HeatDay => "Hitzetag",
			GameEventType.Wind => "Wind",
			GameEventType.Pests => "Schädlinge",
			_ => ""
		};

		if (!string.IsNullOrEmpty(eventName))
			TooltipText += $" – {eventName}";
	}

	private static float EaseInOut(float value)
	{
		return value * value * (3.0f - (2.0f * value));
	}
}
