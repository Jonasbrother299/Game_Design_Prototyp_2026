using Godot;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public partial class EncyclopediaMenu : Control
{
	private enum EntryFilter
	{
		Plants,
		Events
	}

	private const int LockedCardCount = 5;
	private static readonly Color DefaultTitleColor = new Color(0.27f, 0.115f, 0.06f);
	private static readonly Color DefaultCategoryColor = new Color(0.43f, 0.42f, 0.18f);
	private static readonly Color SelectedTextColor = new Color(0.18f, 0.34f, 0.12f);

	[Signal]
	public delegate void ClosedEventHandler();

	private Button _plantsButton;
	private Button _eventsButton;
	private Label _entryCountLabel;
	private GridContainer _cardGrid;
	private ScrollContainer _cardScroll;
	private Button _entryCardTemplate;
	private Button _lockedCardTemplate;
	private RichTextLabel _detailsText;
	private TextureRect _descriptionCard;
	private Button _backButton;
	private Button _firstCardButton;
	private Button _selectedCardButton;
	private MarginContainer _menuLayout;

	private readonly List<PlantDefinition> _plants = new();
	private readonly List<EventDefinition> _events = new();
	private EntryFilter _activeFilter = EntryFilter.Plants;

	public override void _Ready()
	{
		_plantsButton = GetNode<Button>("%PlantsButton");
		_eventsButton = GetNode<Button>("%EventsButton");
		_entryCountLabel = GetNode<Label>("%EntryCountLabel");
		_cardGrid = GetNode<GridContainer>("%CardGrid");
		_cardScroll = GetNode<ScrollContainer>("%CardScroll");
		_cardScroll.Resized += UpdateCardColumns;
		_entryCardTemplate = GetNode<Button>("%EntryCardTemplate");
		_lockedCardTemplate = GetNode<Button>("%LockedCardTemplate");
		_detailsText = GetNode<RichTextLabel>("%DetailsText");
		_descriptionCard = GetNode<TextureRect>("%DescriptionCard");
		_backButton = GetNode<Button>("%BackButton");
		_menuLayout = GetNode<MarginContainer>("CenterContainer");
		Resized += UpdateMenuLayout;

		_plantsButton.Pressed += ShowPlants;
		_eventsButton.Pressed += ShowEvents;
		_backButton.Pressed += Close;

		LoadEntries();
		RefreshCards();
		UpdateMenuLayout();
	}

	public override void _ExitTree()
	{
		Resized -= UpdateMenuLayout;
		_cardScroll.Resized -= UpdateCardColumns;
	}

	private void UpdateCardColumns()
	{
		if (_cardScroll.Size.X <= 0.0f)
			return;

		float availableWidth = _cardScroll.Size.X
			- _cardScroll.GetVScrollBar().GetCombinedMinimumSize().X - 16.0f;
		int separation = _cardGrid.GetThemeConstant("h_separation");
		float cardWidth = _entryCardTemplate.CustomMinimumSize.X;
		_cardGrid.Columns = Mathf.Max(1,
			Mathf.FloorToInt((availableWidth + separation) / (cardWidth + separation)));
	}

	private void UpdateMenuLayout()
	{
		if (Size.X <= 0.0f || Size.Y <= 0.0f)
			return;

		float scale = Mathf.Min(Size.X / 1920.0f, Size.Y / 1080.0f);
		_menuLayout.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
		_menuLayout.Size = Size / scale;
		_menuLayout.Scale = Vector2.One * scale;
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
		UpdateMenuLayout();
		ResetDetailView();
		(_firstCardButton ?? _plantsButton).GrabFocus();
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
		_plantsButton.ButtonPressed = _activeFilter == EntryFilter.Plants;
		_eventsButton.ButtonPressed = _activeFilter == EntryFilter.Events;

		foreach (Node child in _cardGrid.GetChildren())
		{
			_cardGrid.RemoveChild(child);
			child.QueueFree();
		}

		_firstCardButton = null;
		_selectedCardButton = null;

		if (_activeFilter == EntryFilter.Plants)
		{
			foreach (PlantDefinition plant in _plants)
				AddPlantCard(plant);

			AddLockedCards();
		}
		else
		{
			foreach (EventDefinition gameEvent in _events)
				AddEventCard(gameEvent);
		}

		UpdateEntryCount();
		ResetDetailView();
		UpdateCardColumns();
	}

	private void AddPlantCard(PlantDefinition plant)
	{
		Button card = CreateEntryCard(
			plant.DisplayName,
			"PFLANZE",
			plant.CardImage,
			plant.Type == PlantType.Oak ? "♣" : "PFLANZE");
		if (plant.CardImage != null)
		{
			StyleBoxEmpty emptyStyle = new();
			foreach (string state in new[] { "normal", "hover" })
				card.AddThemeStyleboxOverride(state, emptyStyle);
			card.GetNode<Control>("CardContent").SetAnchorsAndOffsetsPreset(
				LayoutPreset.FullRect);
			card.GetNode<Label>("CardContent/Category").Hide();
			card.GetNode<Label>("CardContent/Title").Hide();
			PanelContainer artFrame = card.GetNode<PanelContainer>("CardContent/ArtFrame");
			artFrame.CustomMinimumSize = new Vector2(0.0f, card.CustomMinimumSize.Y);
			artFrame.SizeFlagsVertical = SizeFlags.ExpandFill;
			artFrame.AddThemeStyleboxOverride("panel", emptyStyle);
			card.GetNode<TextureRect>("CardContent/ArtFrame/CardImage").TextureFilter =
				TextureFilterEnum.Linear;
		}
		card.TooltipText = $"Details zu {plant.DisplayName} anzeigen";
		card.Pressed += () => SelectCard(card, plant);
	}

	private void AddEventCard(EventDefinition gameEvent)
	{
		Button card = CreateEntryCard(
			gameEvent.DisplayName,
			"EREIGNIS",
			GetEventIcon(gameEvent.Type),
			"!");
		ConfigureEventCard(card);
		card.TooltipText = $"Details zu {gameEvent.DisplayName} anzeigen";
		card.Pressed += () => SelectCard(card, gameEvent);
	}

	private static Texture2D GetEventIcon(GameEventType eventType)
	{
		string iconPath = eventType switch
		{
			GameEventType.Rain => "res://assets/wetter_Icons/Regen-Original.svg",
			GameEventType.HeavyRain => "res://assets/wetter_Icons/Unwetter2-Vektor.svg",
			GameEventType.Drought => "res://assets/wetter_Icons/Dürre-Vektor.svg",
			GameEventType.HeatDay => "res://assets/wetter_Icons/Hitzetag-Vektor.svg",
			GameEventType.Wind => "res://assets/wetter_Icons/Wind-Vektor.svg",
			GameEventType.Pests => "res://assets/wetter_Icons/Schädlinge-Vektor.svg",
			_ => "res://assets/wetter_Icons/Sonne 1.svg"
		};

		return GD.Load<Texture2D>(iconPath);
	}

	private static Texture2D GetEventDescriptionCard(GameEventType eventType)
	{
		string cardPath = eventType switch
		{
			GameEventType.Rain => "res://assets/wetter_Icons/Regen-Beschreibung.svg",
			GameEventType.HeavyRain => "res://assets/wetter_Icons/Unwetter-Beschreibung.svg",
			GameEventType.Drought => "res://assets/wetter_Icons/Dürre-Beschreibung.svg",
			GameEventType.HeatDay => "res://assets/wetter_Icons/Hitzetag-Beschreibung.svg",
			GameEventType.Wind => "res://assets/wetter_Icons/Wind-Beschreibung.svg",
			GameEventType.Pests => "res://assets/wetter_Icons/Schädlinge-Beschreibung.svg",
			_ => null
		};

		return cardPath == null ? null : GD.Load<Texture2D>(cardPath);
	}

	private static void ConfigureEventCard(Button card)
	{
		StyleBoxEmpty emptyStyle = new StyleBoxEmpty();
		card.CustomMinimumSize = new Vector2(216.0f, 216.0f);
		card.AddThemeStyleboxOverride("normal", emptyStyle);
		card.AddThemeStyleboxOverride("hover", emptyStyle);
		card.AddThemeStyleboxOverride("pressed", emptyStyle);
		card.AddThemeStyleboxOverride("hover_pressed", emptyStyle);

		VBoxContainer content = card.GetNode<VBoxContainer>("CardContent");
		content.OffsetLeft = 8.0f;
		content.OffsetTop = 8.0f;
		content.OffsetRight = -8.0f;
		content.OffsetBottom = -8.0f;
		content.AddThemeConstantOverride("separation", 8);
		card.GetNode<Label>("CardContent/Category").Visible = false;
		PanelContainer artFrame = card.GetNode<PanelContainer>("CardContent/ArtFrame");
		artFrame.CustomMinimumSize = new Vector2(0.0f, 160.0f);
		artFrame.AddThemeStyleboxOverride("panel", emptyStyle);
		card.GetNode<TextureRect>("CardContent/ArtFrame/CardImage").TextureFilter =
			CanvasItem.TextureFilterEnum.Linear;
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
		Label title = card.GetNode<Label>("CardContent/Title");
		Label category = card.GetNode<Label>("CardContent/Category");
		card.GetNode<Control>("SelectionDecor").Visible = selected && category.Visible;
		title.AddThemeColorOverride(
			"font_color",
			selected ? SelectedTextColor : DefaultTitleColor);
		category.AddThemeColorOverride(
			"font_color",
			selected ? SelectedTextColor : DefaultCategoryColor);
	}

	private void UpdateEntryCount()
	{
		_entryCountLabel.Text = _activeFilter == EntryFilter.Plants
			? $"{_plants.Count} Pflanzen · {LockedCardCount} noch nicht entdeckt"
			: $"{_events.Count} Ereignisse";
	}

	private void ResetDetailView()
	{
		if (_selectedCardButton != null)
		{
			_selectedCardButton.ButtonPressed = false;
			SetCardSelectedState(_selectedCardButton, false);
		}

		_selectedCardButton = null;
		_descriptionCard.Visible = false;
		_descriptionCard.Texture = null;
		_detailsText.Visible = true;
		_detailsText.Text =
			"[font_size=26][color=#75432d][b]Noch keine Karte ausgewählt[/b]" +
			"[/color][/font_size]\n\n" +
			"Wähle links eine bekannte Karte, um Werte, Wirkung und " +
			"Beschreibung anzuzeigen.";
	}

	private void ShowPlant(PlantDefinition plant)
	{
		_detailsText.ScrollToLine(0);
		_descriptionCard.Visible = false;
		_descriptionCard.Texture = null;
		_detailsText.Visible = true;

		StringBuilder text = new StringBuilder();
		text.AppendLine(FormatTitle(plant.DisplayName));
		text.AppendLine();

		if (plant.Type == PlantType.Oak)
		{
			GameConfig config = GameConfig.LoadDefault();
			text.AppendLine(FormatSection("★", "Spielziel"));
			text.AppendLine("Baue ein stabiles Ökosystem auf und sichere den Wasservorrat.");
			text.AppendLine(FormatValue("Sieg", $"Erreiche {config.WinWaterLimit} Wasser"));
			text.AppendLine(FormatValue("Niederlage", $"Bei {config.LoseWaterLimit} Wasser"));
			text.AppendLine();
			text.AppendLine(FormatSection("♣", "Haupteiche"));
			text.AppendLine("Steht zu Spielbeginn auf dem Feld, verbraucht und produziert " +
				"kein Wasser und erzeugt sofort Schatten.");
		}
		else
		{
			text.AppendLine(FormatSection("●", "Wasser je Runde"));
			text.AppendLine(FormatValue("Verbrauch", plant.WaterConsumption.ToString()));
			text.AppendLine(FormatValue("Produktion ausgewachsen", plant.WaterProduction.ToString()));
			text.AppendLine(FormatValue("Bilanz ausgewachsen",
				FormatSignedNumber(plant.WaterProduction - plant.WaterConsumption)));
			text.AppendLine();
			text.AppendLine(FormatSection("◆", "Entwicklung"));
			text.AppendLine(FormatValue("Ausgewachsen nach",
				$"{plant.GrowthRounds} Runden") + " · " +
				FormatValue("Stufen", plant.GrowthStageCount.ToString()));
			text.AppendLine(FormatValue("Ausbreitung",
				FormatChance(plant.SpreadChanceDenominator)));
			if (plant.EventDeathResistancePerGrowthStage > 0)
			{
				text.AppendLine(FormatValue("Ereigniswiderstand je Stufe",
					$"+{plant.EventDeathResistancePerGrowthStage}"));
			}
		}

		text.AppendLine();
		text.AppendLine(FormatSection("▸", "Standort"));
		text.AppendLine(FormatLightLevels(plant));
		text.AppendLine();
		text.AppendLine(FormatSection("✦", "Wirkung"));
		text.AppendLine(FormatPlantEffect(plant));

		if (!string.IsNullOrWhiteSpace(plant.Description))
		{
			text.AppendLine();
			text.AppendLine(FormatSection("▤", "Beschreibung"));
			text.AppendLine(plant.Description);
		}

		_detailsText.Text = text.ToString();
	}

	private void ShowEvent(EventDefinition gameEvent)
	{
		_detailsText.ScrollToLine(0);
		Texture2D descriptionTexture = GetEventDescriptionCard(gameEvent.Type);
		_descriptionCard.Texture = descriptionTexture;
		_descriptionCard.Visible = descriptionTexture != null;
		_detailsText.Visible = descriptionTexture == null;

		if (descriptionTexture != null)
			return;

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
		return $"[font_size=30][color=#75432d][b]{title}[/b][/color][/font_size]";
	}

	private static string FormatValue(string label, string value)
	{
		return $"[color=#754d32]{label}:[/color]  {value}";
	}

	private static string FormatSection(string icon, string title)
	{
		return $"[font_size=22][color=#77752f][b]{icon}  {title}[/b][/color][/font_size]";
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
