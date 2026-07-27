using Godot;
using System.Collections.Generic;
using System.Text;

public partial class EncyclopediaMenu : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	private Button _plantsButton;
	private Button _eventsButton;
	private ItemList _entryList;
	private TextureRect _previewImage;
	private RichTextLabel _detailsText;
	private Button _backButton;

	private readonly List<PlantDefinition> _plants = new();
	private readonly List<EventDefinition> _events = new();
	private bool _showPlants = true;

	public override void _Ready()
	{
		_plantsButton = GetNode<Button>("%PlantsButton");
		_eventsButton = GetNode<Button>("%EventsButton");
		_entryList = GetNode<ItemList>("%EntryList");
		_previewImage = GetNode<TextureRect>("%PreviewImage");
		_detailsText = GetNode<RichTextLabel>("%DetailsText");
		_backButton = GetNode<Button>("%BackButton");

		_plantsButton.Pressed += ShowPlants;
		_eventsButton.Pressed += ShowEvents;
		_entryList.ItemSelected += OnEntrySelected;
		_backButton.Pressed += Close;

		LoadEntries();
		RefreshEntryList();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!Visible || !inputEvent.IsActionPressed("ui_cancel"))
			return;

		Close();
		GetViewport().SetInputAsHandled();
	}

	public void Open()
	{
		Show();
		_entryList.GrabFocus();
	}

	public void Close()
	{
		Hide();
		EmitSignal(SignalName.Closed);
	}

	private void LoadEntries()
	{
		_plants.Clear();
		_plants.AddRange(PlantDatabase.GetAll());
		_plants.Sort((left, right) => left.Type.CompareTo(right.Type));

		_events.Clear();
		_events.AddRange(EventDatabase.GetAll());
		_events.Sort((left, right) => left.Type.CompareTo(right.Type));
	}

	private void ShowPlants()
	{
		if (_showPlants)
			return;

		_showPlants = true;
		RefreshEntryList();
	}

	private void ShowEvents()
	{
		if (!_showPlants)
			return;

		_showPlants = false;
		RefreshEntryList();
	}

	private void RefreshEntryList()
	{
		_plantsButton.ButtonPressed = _showPlants;
		_eventsButton.ButtonPressed = !_showPlants;
		_entryList.Clear();

		if (_showPlants)
		{
			foreach (PlantDefinition plant in _plants)
				_entryList.AddItem(plant.DisplayName, plant.CardImage);
		}
		else
		{
			foreach (EventDefinition gameEvent in _events)
				_entryList.AddItem(gameEvent.DisplayName);
		}

		if (_entryList.ItemCount == 0)
		{
			_previewImage.Hide();
			_detailsText.Text = "Keine Einträge vorhanden.";
			return;
		}

		_entryList.Select(0);
		ShowEntry(0);
	}

	private void OnEntrySelected(long index)
	{
		ShowEntry((int)index);
	}

	private void ShowEntry(int index)
	{
		if (_showPlants)
		{
			if (index < 0 || index >= _plants.Count)
				return;

			ShowPlant(_plants[index]);
			return;
		}

		if (index < 0 || index >= _events.Count)
			return;

		ShowEvent(_events[index]);
	}

	private void ShowPlant(PlantDefinition plant)
	{
		_previewImage.Texture = plant.CardImage;
		_previewImage.Visible = plant.CardImage != null;

		StringBuilder text = new StringBuilder();
		text.AppendLine(FormatTitle(plant.DisplayName));
		text.AppendLine("[color=#a9c66e]PFLANZE[/color]");
		text.AppendLine();
		text.AppendLine(FormatValue("Wasserverbrauch", plant.WaterConsumption.ToString()));
		text.AppendLine(FormatValue("Wasserproduktion", plant.WaterProduction.ToString()));
		text.AppendLine(FormatValue("Wachstum", $"{plant.GrowthRounds} Runden"));
		text.AppendLine(FormatValue("Wachstumsstufen", plant.GrowthStageCount.ToString()));
		text.AppendLine(FormatValue(
			"Ausbreitung",
			FormatChance(plant.SpreadChanceDenominator)));
		text.AppendLine(FormatValue("Standort", FormatLightLevels(plant)));
		text.AppendLine();
		text.AppendLine("[color=#a9c66e][b]Effekt[/b][/color]");
		text.AppendLine(FormatPlantEffect(plant));

		if (!string.IsNullOrWhiteSpace(plant.Description))
		{
			text.AppendLine();
			text.AppendLine(plant.Description);
		}

		_detailsText.Text = text.ToString();
	}

	private void ShowEvent(EventDefinition gameEvent)
	{
		_previewImage.Texture = null;
		_previewImage.Hide();

		StringBuilder text = new StringBuilder();
		text.AppendLine(FormatTitle(gameEvent.DisplayName));
		text.AppendLine("[color=#a9c66e]WETTEREREIGNIS[/color]");
		text.AppendLine();
		text.AppendLine(FormatValue(
			"Dauer",
			gameEvent.DurationRounds == 1
				? "1 Runde"
				: $"{gameEvent.DurationRounds} Runden"));
		text.AppendLine(FormatValue(
			"Wasser pro Runde",
			FormatSignedNumber(gameEvent.WaterModifierPerRound)));

		if (gameEvent.SeedlingDeathChanceDenominator > 0)
		{
			string condition = gameEvent.SeedlingDeathRequiresSun
				? " auf Sonnenfeldern"
				: "";
			text.AppendLine(FormatValue(
				$"Setzlinge{condition}",
				FormatChance(gameEvent.SeedlingDeathChanceDenominator)));
		}

		if (gameEvent.MatureDeathChanceDenominator > 0)
		{
			string condition = gameEvent.MatureDeathRequiresMonoculture
				? " in Monokultur"
				: "";
			text.AppendLine(FormatValue(
				$"Ausgewachsene Pflanzen{condition}",
				FormatChance(gameEvent.MatureDeathChanceDenominator)));
		}

		if (!string.IsNullOrWhiteSpace(gameEvent.Description))
		{
			text.AppendLine();
			text.AppendLine("[color=#a9c66e][b]Wirkung[/b][/color]");
			text.AppendLine(gameEvent.Description);
		}

		_detailsText.Text = text.ToString();
	}

	private static string FormatTitle(string title)
	{
		return $"[font_size=36][color=#f0edba][b]{title}[/b][/color][/font_size]";
	}

	private static string FormatValue(string label, string value)
	{
		return $"[color=#c4caac]{label}:[/color]  {value}";
	}

	private static string FormatSignedNumber(int value)
	{
		if (value > 0)
			return $"+{value}";

		return value.ToString();
	}

	private static string FormatChance(int denominator)
	{
		if (denominator <= 0)
			return "Keine";

		int percent = Mathf.RoundToInt(100.0f / denominator);
		return $"1 zu {denominator} ({percent} %)";
	}

	private static string FormatLightLevels(PlantDefinition plant)
	{
		List<string> lightLevels = new();

		foreach (LightLevel lightLevel in plant.AllowedLightLevels)
		{
			lightLevels.Add(lightLevel switch
			{
				LightLevel.Sun => "Sonne",
				LightLevel.PartialShade => "Halbschatten",
				LightLevel.Shade => "Schatten",
				_ => lightLevel.ToString()
			});
		}

		return string.Join(", ", lightLevels);
	}

	private static string FormatPlantEffect(PlantDefinition plant)
	{
		return plant.EffectType switch
		{
			PlantEffectType.TreeShade => plant.ShadeRequiresMaturity
				? "Erzeugt Schatten, sobald die Pflanze ausgewachsen ist."
				: "Erzeugt sofort Schatten.",
			PlantEffectType.AdjacentPlantsProducePlusOne =>
				"Benachbarte Pflanzen produzieren +1 Wasser. Die Haupteiche ist ausgenommen.",
			PlantEffectType.SpreadChancePlusOneForNeighbors =>
				"Benachbarte Pflanzen verbreiten sich leichter.",
			_ => "Kein zusätzlicher Effekt."
		};
	}
}
