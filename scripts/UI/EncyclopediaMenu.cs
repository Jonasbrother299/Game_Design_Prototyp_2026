using Godot;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public partial class EncyclopediaMenu : Control
{
	private enum EntryFilter
	{
		All,
		Plants,
		Events
	}

	private const int LockedCardCount = 5;
	private static readonly Color DefaultTitleColor = new Color(0.27f, 0.115f, 0.06f);
	private static readonly Color DefaultCategoryColor = new Color(0.43f, 0.42f, 0.18f);
	private static readonly Color SelectedTextColor = new Color(0.18f, 0.34f, 0.12f);

	[Signal]
	public delegate void ClosedEventHandler();

	private Button _allButton;
	private Button _plantsButton;
	private Button _eventsButton;
	private Label _entryCountLabel;
	private GridContainer _cardGrid;
	private Button _entryCardTemplate;
	private Button _lockedCardTemplate;
	private RichTextLabel _detailsText;
	private Button _backButton;
	private Button _firstCardButton;
	private Button _selectedCardButton;

	private readonly List<PlantDefinition> _plants = new();
	private readonly List<EventDefinition> _events = new();
	private EntryFilter _activeFilter = EntryFilter.All;

	public override void _Ready()
	{
		_allButton = GetNode<Button>("%AllButton");
		_plantsButton = GetNode<Button>("%PlantsButton");
		_eventsButton = GetNode<Button>("%EventsButton");
		_entryCountLabel = GetNode<Label>("%EntryCountLabel");
		_cardGrid = GetNode<GridContainer>("%CardGrid");
		_entryCardTemplate = GetNode<Button>("%EntryCardTemplate");
		_lockedCardTemplate = GetNode<Button>("%LockedCardTemplate");
		_detailsText = GetNode<RichTextLabel>("%DetailsText");
		_backButton = GetNode<Button>("%BackButton");

		_allButton.Pressed += ShowAll;
		_plantsButton.Pressed += ShowPlants;
		_eventsButton.Pressed += ShowEvents;
		_backButton.Pressed += Close;

		LoadEntries();
		RefreshCards();
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
		ResetDetailView();
		(_firstCardButton ?? _allButton).GrabFocus();
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

	private void ShowAll()
	{
		SetFilter(EntryFilter.All);
	}

	private void ShowPlants()
	{
		SetFilter(EntryFilter.Plants);
	}

	private void ShowEvents()
	{
		SetFilter(EntryFilter.Events);
	}

	private void SetFilter(EntryFilter filter)
	{
		_activeFilter = filter;
		RefreshCards();
	}

	private void RefreshCards()
	{
		_allButton.ButtonPressed = _activeFilter == EntryFilter.All;
		_plantsButton.ButtonPressed = _activeFilter == EntryFilter.Plants;
		_eventsButton.ButtonPressed = _activeFilter == EntryFilter.Events;

		foreach (Node child in _cardGrid.GetChildren())
		{
			_cardGrid.RemoveChild(child);
			child.QueueFree();
		}

		_firstCardButton = null;
		_selectedCardButton = null;

		if (_activeFilter != EntryFilter.Events)
		{
			foreach (PlantDefinition plant in _plants)
				AddPlantCard(plant);
		}

		if (_activeFilter != EntryFilter.Plants)
		{
			foreach (EventDefinition gameEvent in _events)
				AddEventCard(gameEvent);
		}

		if (_activeFilter == EntryFilter.All)
			AddLockedCards();

		UpdateEntryCount();
		ResetDetailView();
	}

	private void AddPlantCard(PlantDefinition plant)
	{
		Button card = CreateEntryCard(
			plant.DisplayName,
			"PFLANZE",
			plant.CardImage,
			plant.Type == PlantType.Oak ? "♣" : "PFLANZE");
		card.TooltipText = $"Details zu {plant.DisplayName} anzeigen";
		card.Pressed += () => SelectCard(card, plant);
	}

	private void AddEventCard(EventDefinition gameEvent)
	{
		Button card = CreateEntryCard(
			gameEvent.DisplayName,
			"EREIGNIS",
			null,
			"!");
		card.TooltipText = $"Details zu {gameEvent.DisplayName} anzeigen";
		card.Pressed += () => SelectCard(card, gameEvent);
	}

	private Button CreateEntryCard(
		string title,
		string category,
		Texture2D image,
		string imageFallback)
	{
		Button card = (Button)_entryCardTemplate.Duplicate();
		card.Visible = true;
		card.GetNode<Label>("CardContent/Category").Text = category;
		card.GetNode<Label>("CardContent/Title").Text = title;

		TextureRect cardImage = card.GetNode<TextureRect>(
			"CardContent/ArtFrame/CardImage");
		Label fallback = card.GetNode<Label>(
			"CardContent/ArtFrame/ImageFallback");
		cardImage.Texture = image;
		cardImage.Visible = image != null;
		fallback.Text = imageFallback;
		fallback.Visible = image == null;

		_cardGrid.AddChild(card);
		_firstCardButton ??= card;
		return card;
	}

	private void AddLockedCards()
	{
		for (int index = 0; index < LockedCardCount; index++)
		{
			Button card = (Button)_lockedCardTemplate.Duplicate();
			card.Visible = true;
			card.TooltipText = "Dieser Eintrag wurde noch nicht entdeckt.";
			_cardGrid.AddChild(card);
		}
	}

	private void SelectCard(Button card, PlantDefinition plant)
	{
		MarkCardSelected(card);
		ShowPlant(plant);
	}

	private void SelectCard(Button card, EventDefinition gameEvent)
	{
		MarkCardSelected(card);
		ShowEvent(gameEvent);
	}

	private void MarkCardSelected(Button card)
	{
		if (_selectedCardButton != null)
		{
			_selectedCardButton.ButtonPressed = false;
			SetCardSelectedState(_selectedCardButton, false);
		}

		_selectedCardButton = card;
		_selectedCardButton.ButtonPressed = true;
		SetCardSelectedState(_selectedCardButton, true);
	}

	private static void SetCardSelectedState(Button card, bool selected)
	{
		card.GetNode<Control>("SelectionDecor").Visible = selected;

		Label title = card.GetNode<Label>("CardContent/Title");
		Label category = card.GetNode<Label>("CardContent/Category");
		title.AddThemeColorOverride(
			"font_color",
			selected ? SelectedTextColor : DefaultTitleColor);
		category.AddThemeColorOverride(
			"font_color",
			selected ? SelectedTextColor : DefaultCategoryColor);
	}

	private void UpdateEntryCount()
	{
		_entryCountLabel.Text = _activeFilter switch
		{
			EntryFilter.Plants => $"{_plants.Count} Pflanzen",
			EntryFilter.Events => $"{_events.Count} Ereignisse",
			_ => $"{_plants.Count + _events.Count} bekannt · " +
				$"{LockedCardCount} noch nicht entdeckt"
		};
	}

	private void ResetDetailView()
	{
		if (_selectedCardButton != null)
		{
			_selectedCardButton.ButtonPressed = false;
			SetCardSelectedState(_selectedCardButton, false);
		}

		_selectedCardButton = null;
		_detailsText.Text =
			"[font_size=38][color=#5f2d1d][b]Noch keine Karte ausgewählt[/b]" +
			"[/color][/font_size]\n\n" +
			"Wähle links eine bekannte Karte, um Werte, Wirkung und " +
			"Beschreibung anzuzeigen.";
	}

	private void ShowPlant(PlantDefinition plant)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine(FormatTitle(plant.DisplayName));
		text.AppendLine(plant.Type == PlantType.Oak
			? "[color=#77752f]PFLANZE · HAUPTEICHE[/color]"
			: "[color=#77752f]PFLANZE[/color]");
		text.AppendLine();

		if (plant.Type == PlantType.Oak)
		{
			GameConfig config = GameConfig.LoadDefault();
			text.AppendLine(FormatSection("★", "SPIELZIEL"));
			text.AppendLine("Baue rund um die Haupteiche ein stabiles Ökosystem auf und sichere den Wasservorrat.");
			text.AppendLine();
			text.AppendLine(FormatValue("Sieg", $"Erreiche {config.WinWaterLimit} Wasser"));
			text.AppendLine(FormatValue(
				"Niederlage",
				$"Bei {config.LoseWaterLimit} Wasser ist das Spiel verloren"));
			text.AppendLine();
			text.AppendLine(FormatSection("♣", "ROLLE DER HAUPTEICHE"));
			text.AppendLine("Die Haupteiche steht zu Spielbeginn auf dem Feld. " +
				"Sie verbraucht und produziert kein Wasser und erzeugt sofort Schatten.");
			text.AppendLine();
		}
		else
		{
			text.AppendLine(FormatSection("●", "WASSERHAUSHALT"));
			text.AppendLine($"Verbraucht pro Runde {plant.WaterConsumption} Wasser. " +
				$"Nach dem Auswachsen produziert die Pflanze {plant.WaterProduction} Wasser pro Runde.");
			text.AppendLine();
			text.AppendLine(FormatValue("Verbrauch pro Runde", plant.WaterConsumption.ToString()));
			text.AppendLine(FormatValue("Produktion ausgewachsen", plant.WaterProduction.ToString()));
			text.AppendLine(FormatValue(
				"Bilanz ausgewachsen",
				FormatSignedNumber(plant.WaterProduction - plant.WaterConsumption)));
			text.AppendLine();

			text.AppendLine(FormatSection("◆", "ENTWICKLUNG"));
			text.AppendLine(FormatValue(
				"Ausgewachsen nach",
				$"{plant.GrowthRounds} Runden"));
			text.AppendLine(FormatValue(
				"Wachstumsstufen",
				plant.GrowthStageCount.ToString()));
			text.AppendLine(FormatValue(
				"Ausbreitung",
				FormatChance(plant.SpreadChanceDenominator)));
			if (plant.EventDeathResistancePerGrowthStage > 0)
			{
				text.AppendLine(FormatValue(
					"Ereigniswiderstand je Stufe",
					$"+{plant.EventDeathResistancePerGrowthStage}"));
			}
		}

		text.AppendLine();
		text.AppendLine(FormatSection("▸", "STANDORT"));
		text.AppendLine(FormatValue("Standort", FormatLightLevels(plant)));
		text.AppendLine();
		text.AppendLine(FormatSection("✦", "WIRKUNG"));
		text.AppendLine(FormatPlantEffect(plant));

		if (!string.IsNullOrWhiteSpace(plant.Description))
		{
			text.AppendLine();
			text.AppendLine(FormatSection("▤", "BESCHREIBUNG"));
			text.AppendLine(plant.Description);
		}

		_detailsText.Text = text.ToString();
	}

	private void ShowEvent(EventDefinition gameEvent)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine(FormatTitle(gameEvent.DisplayName));
		text.AppendLine("[color=#77752f]WETTEREREIGNIS[/color]");
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
			text.AppendLine("[color=#77752f][b]Wirkung[/b][/color]");
			text.AppendLine(gameEvent.Description);
		}

		_detailsText.Text = text.ToString();
	}

	private static string FormatTitle(string title)
	{
		return $"[font_size=40][color=#5f2d1d][b]{title}[/b][/color][/font_size]";
	}

	private static string FormatValue(string label, string value)
	{
		return $"[color=#754d32]{label}:[/color]  {value}";
	}

	private static string FormatSection(string icon, string title)
	{
		return $"[font_size=28][color=#77752f][b]{icon}  {title}[/b][/color][/font_size]";
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
				$"Benachbarte Pflanzen außer Eichen und Birken produzieren " +
				$"+{GetAdjacentWaterProductionBonus(plant)} Wasser.",
			PlantEffectType.SpreadChancePlusOneForNeighbors =>
				"Benachbarte Pflanzen außer Blumen verbreiten sich leichter.",
			_ => "Kein zusätzlicher Effekt."
		};
	}

	private static int GetAdjacentWaterProductionBonus(PlantDefinition plant)
	{
		FieldInfo bonusField = typeof(PlantDefinition).GetField(
			"AdjacentWaterProductionBonus");
		if (bonusField?.GetValue(plant) is int bonus)
			return bonus;

		return 1;
	}
}
