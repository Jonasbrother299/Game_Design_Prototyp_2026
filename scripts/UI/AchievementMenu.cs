using Godot;
using System;

public partial class AchievementMenu : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	private readonly GlobalStatisticsManager _statisticsManager = new();

	private Label _levelLabel;
	private ProgressBar _experienceBar;
	private Label _experienceLabel;
	private Label _achievementCountLabel;
	private GridContainer _achievementGrid;
	private Label _statusLabel;
	private Button _backButton;

	public override void _Ready()
	{
		_levelLabel = GetNode<Label>("%LevelLabel");
		_experienceBar = GetNode<ProgressBar>("%ExperienceBar");
		_experienceLabel = GetNode<Label>("%ExperienceLabel");
		_achievementCountLabel = GetNode<Label>("%AchievementCount");
		_achievementGrid = GetNode<GridContainer>("%AchievementGrid");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_backButton = GetNode<Button>("%BackButton");

		_backButton.Pressed += Close;
	}

	public override void _ExitTree()
	{
		if (_backButton != null)
			_backButton.Pressed -= Close;
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
		RefreshProgress();
		Show();
		_backButton.GrabFocus();
	}

	public void Close()
	{
		Hide();
		EmitSignal(SignalName.Closed);
	}

	private void RefreshProgress()
	{
		GlobalStatisticsData data;
		try
		{
			data = _statisticsManager.GetStatistics();
			_statusLabel.Hide();
		}
		catch (Exception exception)
		{
			data = new GlobalStatisticsData();
			_statusLabel.Text = "Die Erfolge konnten nicht geladen werden.";
			_statusLabel.Show();
			GD.PushWarning(
				$"AchievementMenu: Die Statistik konnte nicht gelesen werden: {exception.Message}");
		}

		AchievementProgress progress = AchievementCatalog.GetProgress(data);
		_levelLabel.Text = $"STUFE\n{progress.CurrentLevel}";
		_experienceBar.MaxValue = progress.ExperienceForNextLevel;
		_experienceBar.Value = progress.ExperienceInCurrentLevel;
		_experienceLabel.Text =
			$"{progress.ExperienceInCurrentLevel} / {progress.ExperienceForNextLevel} EP bis Stufe {progress.CurrentLevel + 1}";
		_achievementCountLabel.Text =
			$"{progress.UnlockedAchievementCount} von {AchievementCatalog.All.Count} Abzeichen freigeschaltet · {progress.TotalExperience} EP gesammelt";

		foreach (Node child in _achievementGrid.GetChildren())
			child.QueueFree();

		foreach (AchievementDefinition achievement in AchievementCatalog.All)
			_achievementGrid.AddChild(CreateAchievementCard(achievement, progress.IsUnlocked(achievement.Id)));
	}

	private static PanelContainer CreateAchievementCard(
		AchievementDefinition achievement,
		bool isUnlocked)
	{
		Color badgeColor = GetBadgeColor(achievement.BadgeTier);
		PanelContainer card = new()
		{
			CustomMinimumSize = new Vector2(320.0f, 230.0f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		card.AddThemeStyleboxOverride(
			"panel",
			CreateAchievementCardStyle(badgeColor, isUnlocked));

		VBoxContainer content = new()
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		content.AddThemeConstantOverride("separation", 7);
		card.AddChild(content);

		HBoxContainer header = new()
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		header.AddThemeConstantOverride("separation", 10);
		content.AddChild(header);

		PanelContainer badge = new()
		{
			CustomMinimumSize = new Vector2(54.0f, 54.0f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		badge.AddThemeStyleboxOverride("panel", CreateBadgeStyle(badgeColor, isUnlocked));
		header.AddChild(badge);

		Label icon = CreateLabel(
			isUnlocked ? "✦" : "?",
			30,
			isUnlocked ? badgeColor : new Color(0.60f, 0.52f, 0.42f));
		icon.HorizontalAlignment = HorizontalAlignment.Center;
		icon.VerticalAlignment = VerticalAlignment.Center;
		icon.AddThemeColorOverride(
			"font_outline_color",
			new Color(0.11f, 0.07f, 0.03f, 0.94f));
		icon.AddThemeConstantOverride("outline_size", 3);
		badge.AddChild(icon);

		VBoxContainer headerText = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		header.AddChild(headerText);
		headerText.AddChild(CreateLabel(
			isUnlocked ? $"{GetBadgeLabel(achievement.BadgeTier)}-ABZEICHEN" : "NOCH VERSCHLOSSEN",
			15,
			isUnlocked ? badgeColor : new Color(0.46f, 0.38f, 0.29f)));
		headerText.AddChild(CreateLabel(
			isUnlocked ? "FREIGESCHALTET" : "ERFÜLLE DIE BEDINGUNG",
			14,
			isUnlocked ? new Color(0.30f, 0.47f, 0.22f) : new Color(0.42f, 0.34f, 0.26f)));

		Label title = CreateLabel(
			isUnlocked ? achievement.DisplayName : "Unbekannter Pfad",
			24,
			isUnlocked ? new Color(0.25f, 0.105f, 0.055f) : new Color(0.35f, 0.28f, 0.22f));
		title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		content.AddChild(title);

		Label description = CreateLabel(
			achievement.Description,
			17,
			isUnlocked ? new Color(0.34f, 0.23f, 0.14f) : new Color(0.43f, 0.35f, 0.27f));
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		description.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		content.AddChild(description);

		Label reward = CreateLabel(
			$"BELOHNUNG · {achievement.ExperienceReward} EP",
			16,
			isUnlocked ? badgeColor : new Color(0.47f, 0.38f, 0.29f));
		reward.HorizontalAlignment = HorizontalAlignment.Right;
		content.AddChild(reward);

		return card;
	}

	private static Label CreateLabel(string text, int fontSize, Color fontColor)
	{
		Label label = new()
		{
			Text = text,
			MouseFilter = MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", fontColor);
		return label;
	}

	private static StyleBoxFlat CreateAchievementCardStyle(Color badgeColor, bool isUnlocked)
	{
		return new StyleBoxFlat
		{
			ContentMarginLeft = 18.0f,
			ContentMarginTop = 16.0f,
			ContentMarginRight = 18.0f,
			ContentMarginBottom = 16.0f,
			BgColor = isUnlocked
				? new Color(0.82f, 0.67f, 0.48f, 1.0f)
				: new Color(0.58f, 0.49f, 0.39f, 0.94f),
			BorderWidthLeft = 3,
			BorderWidthTop = 3,
			BorderWidthRight = 3,
			BorderWidthBottom = 3,
			BorderColor = isUnlocked ? badgeColor : new Color(0.39f, 0.31f, 0.23f),
			CornerRadiusTopLeft = 18,
			CornerRadiusTopRight = 18,
			CornerRadiusBottomRight = 18,
			CornerRadiusBottomLeft = 18,
			ShadowColor = new Color(0.18f, 0.075f, 0.035f, 0.36f),
			ShadowSize = 6,
			ShadowOffset = new Vector2(0.0f, 4.0f)
		};
	}

	private static StyleBoxFlat CreateBadgeStyle(Color badgeColor, bool isUnlocked)
	{
		return new StyleBoxFlat
		{
			BgColor = isUnlocked
				? new Color(0.20f, 0.30f, 0.12f, 1.0f)
				: new Color(0.30f, 0.23f, 0.17f, 1.0f),
			BorderWidthLeft = 3,
			BorderWidthTop = 3,
			BorderWidthRight = 3,
			BorderWidthBottom = 3,
			BorderColor = isUnlocked ? badgeColor : new Color(0.45f, 0.36f, 0.27f),
			CornerRadiusTopLeft = 27,
			CornerRadiusTopRight = 27,
			CornerRadiusBottomRight = 27,
			CornerRadiusBottomLeft = 27
		};
	}

	private static Color GetBadgeColor(AchievementBadgeTier badgeTier)
	{
		return badgeTier switch
		{
			AchievementBadgeTier.Bronze => new Color(0.78f, 0.50f, 0.26f),
			AchievementBadgeTier.Silver => new Color(0.76f, 0.82f, 0.82f),
			AchievementBadgeTier.Gold => new Color(0.98f, 0.76f, 0.28f),
			AchievementBadgeTier.Verdant => new Color(0.48f, 0.81f, 0.39f),
			AchievementBadgeTier.Copper => new Color(0.86f, 0.39f, 0.24f),
			_ => new Color(0.84f, 0.91f, 0.62f)
		};
	}

	private static string GetBadgeLabel(AchievementBadgeTier badgeTier)
	{
		return badgeTier switch
		{
			AchievementBadgeTier.Bronze => "BRONZE",
			AchievementBadgeTier.Silver => "SILBER",
			AchievementBadgeTier.Gold => "GOLD",
			AchievementBadgeTier.Verdant => "WALD",
			AchievementBadgeTier.Copper => "KUPFER",
			_ => "WALD"
		};
	}
}
