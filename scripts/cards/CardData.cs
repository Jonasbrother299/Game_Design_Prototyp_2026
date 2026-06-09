public class CardData
{
	public string CardName { get; private set; }

	public CardType CardType { get; private set; }

	public PlantType PlantType { get; private set; }
	public GameEventType EventType { get; private set; }

	private CardData(
		string cardName,
		CardType cardType,
		PlantType plantType,
		GameEventType eventType
	)
	{
		CardName = cardName;
		CardType = cardType;
		PlantType = plantType;
		EventType = eventType;
	}

	public static CardData CreatePlantCard(PlantType plantType)
	{
		PlantDefinition definition = PlantDatabase.Get(plantType);

		return new CardData(
			definition.DisplayName,
			CardType.Plant,
			plantType,
			GameEventType.None
		);
	}

	public static CardData CreateEventCard(GameEventType eventType)
	{
		EventDefinition definition = EventDatabase.Get(eventType);

		return new CardData(
			definition.DisplayName,
			CardType.Event,
			PlantType.None,
			eventType
		);
	}
}
