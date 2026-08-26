using Godot;

public partial class EventDisplayUI : PanelContainer
{
	private TextureRect _card;

	public override void _Ready()
	{
		_card = GetNodeOrNull<TextureRect>("Card");
		Visible = false;
	}

	public void ShowActivated(EventDefinition definition)
	{
		if (definition == null)
		{
			Visible = false;
			return;
		}

		ShowEventCard(definition.Type);
	}

	public void ShowWaterResult(WaterPhaseResult result)
	{
	}

	public void ShowPhaseResult(EventPhaseResult result)
	{
		if (result == null)
			return;

		if (result.ActivatedEvent.HasValue)
		{
			ShowEventCard(result.ActivatedEvent.Value);
			return;
		}

		if (result.ActiveEvents.Count > 0)
		{
			ShowActivated(EventDatabase.Get(result.ActiveEvents[0]));
			return;
		}

		Visible = false;
	}

	private void ShowEventCard(GameEventType eventType)
	{
		string cardPath = eventType switch
		{
			GameEventType.Rain =>
				"res://assets/wetter_Icons/Regen-Beschreibung.svg",
			GameEventType.Drought =>
				"res://assets/wetter_Icons/Dürre-Beschreibung.svg",
			GameEventType.HeatDay =>
				"res://assets/wetter_Icons/Hitzetag-Beschreibung.svg",
			GameEventType.Pests =>
				"res://assets/wetter_Icons/Schädlinge-Beschreibung.svg",
			GameEventType.Wind =>
				"res://assets/wetter_Icons/Wind-Beschreibung.svg",
			GameEventType.HeavyRain =>
				"res://assets/wetter_Icons/Unwetter-Beschreibung.svg",
			_ => null
		};

		if (_card == null || cardPath == null)
		{
			Visible = false;
			return;
		}

		_card.Texture = GD.Load<Texture2D>(cardPath);
		Visible = _card.Texture != null;
	}
}
