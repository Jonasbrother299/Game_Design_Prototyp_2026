using Godot;

[GlobalClass]
public partial class CardData : Resource
{
	[Export] public string CardName = "";
	[Export] public Texture2D CardImage;

	// Nicht exportieren. Godot macht hier gerade Stress.
	public CardType CardType = global::CardType.Plant;

	[Export] public PlantType PlantType;
	[Export] public PlantDefinition PlantDefinition;

	public static CardData CreatePlantCard(PlantType plantType)
	{
		PlantDefinition definition = PlantDatabase.Get(plantType);

		CardData card = new CardData();

		card.CardType = global::CardType.Plant;
		card.PlantType = plantType;
		card.PlantDefinition = definition;
		card.CardName = definition.DisplayName;
		card.CardImage = definition.CardImage;

		return card;
	}
}
