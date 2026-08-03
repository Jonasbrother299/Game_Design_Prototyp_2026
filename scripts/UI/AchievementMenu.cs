using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class AchievementMenu : Control
{
	private enum AchievementMenuPage
	{
		Achievements,
		Leaderboard
	}

	[Signal]
	public delegate void ClosedEventHandler();

	private readonly GlobalStatisticsManager _statisticsManager = new();
	private readonly DeveloperLeaderboardManager _leaderboardManager = new();

	private Label _levelLabel;
	private ProgressBar _experienceBar;
	private Label _experienceLabel;
	private Label _achievementCountLabel;
	private GridContainer _achievementGrid;
	private Label _statusLabel;
	private Button _achievementsTabButton;
	private Button _leaderboardTabButton;
	private VBoxContainer _achievementPage;
	private VBoxContainer _leaderboardPage;
	private LineEdit _developerNameInput;
	private Button _synchronizeButton;
	private Button _refreshLeaderboardButton;
	private VBoxContainer _leaderboardList;
	private Label _leaderboardStatusLabel;
	private Button _backButton;
	private GlobalStatisticsData _statisticsData = new();
	private bool _hasLoadedStatistics;
	private bool _isLeaderboardRequestRunning;

	public override void _Ready()
	{
		_levelLabel = GetNode<Label>("%LevelLabel");
		_experienceBar = GetNode<ProgressBar>("%ExperienceBar");
		_experienceLabel = GetNode<Label>("%ExperienceLabel");
		_achievementCountLabel = GetNode<Label>("%AchievementCount");
		_achievementGrid = GetNode<GridContainer>("%AchievementGrid");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_achievementsTabButton = GetNode<Button>("%AchievementsTabButton");
		_leaderboardTabButton = GetNode<Button>("%LeaderboardTabButton");
		_achievementPage = GetNode<VBoxContainer>("%AchievementPage");
		_leaderboardPage = GetNode<VBoxContainer>("%LeaderboardPage");
		_developerNameInput = GetNode<LineEdit>("%DeveloperNameInput");
		_synchronizeButton = GetNode<Button>("%SynchronizeButton");
		_refreshLeaderboardButton = GetNode<Button>("%RefreshLeaderboardButton");
		_leaderboardList = GetNode<VBoxContainer>("%LeaderboardList");
		_leaderboardStatusLabel = GetNode<Label>("%LeaderboardStatusLabel");
		_backButton = GetNode<Button>("%BackButton");

		_backButton.Pressed += Close;
		_achievementsTabButton.Pressed += OnAchievementsTabPressed;
		_leaderboardTabButton.Pressed += OnLeaderboardTabPressed;
		_synchronizeButton.Pressed += OnSynchronizeButtonPressed;
		_refreshLeaderboardButton.Pressed += OnRefreshLeaderboardButtonPressed;
		SetPage(AchievementMenuPage.Achievements);
	}

	public override void _ExitTree()
	{
		if (_backButton != null)
			_backButton.Pressed -= Close;
		if (_achievementsTabButton != null)
			_achievementsTabButton.Pressed -= OnAchievementsTabPressed;
		if (_leaderboardTabButton != null)
			_leaderboardTabButton.Pressed -= OnLeaderboardTabPressed;
		if (_synchronizeButton != null)
			_synchronizeButton.Pressed -= OnSynchronizeButtonPressed;
		if (_refreshLeaderboardButton != null)
			_refreshLeaderboardButton.Pressed -= OnRefreshLeaderboardButtonPressed;
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
		RefreshDeveloperProfile();
		SetPage(AchievementMenuPage.Achievements);
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
			_hasLoadedStatistics = true;
			_statusLabel.Hide();
		}
		catch (Exception exception)
		{
			data = new GlobalStatisticsData();
			_hasLoadedStatistics = false;
			_statusLabel.Text = "Die Erfolge konnten nicht geladen werden.";
			_statusLabel.Show();
			GD.PushWarning(
				$"AchievementMenu: Die Statistik konnte nicht gelesen werden: {exception.Message}");
		}
		_statisticsData = data;

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

	private void RefreshDeveloperProfile()
	{
		try
		{
			DeveloperProfileConfiguration profile = _leaderboardManager.LoadProfile();
			_developerNameInput.Text = profile.DisplayName;
			ShowLeaderboardStatus(
				profile.IsConnectionConfigured
					? "Profil bereit. Synchronisiere, um die Rangliste zu aktualisieren."
					: "Die Rangliste ist vorbereitet. Ergänze zuerst den Publishable Key in user://developer_leaderboard.cfg.",
				isWarning: !profile.IsConnectionConfigured);
		}
		catch (Exception exception)
		{
			ShowLeaderboardStatus("Das lokale Entwicklerprofil konnte nicht geladen werden.", isWarning: true);
			GD.PushWarning(
				$"AchievementMenu: Das Entwicklerprofil konnte nicht gelesen werden: {exception.Message}");
		}
	}

	private void OnAchievementsTabPressed()
	{
		SetPage(AchievementMenuPage.Achievements);
	}

	private async void OnLeaderboardTabPressed()
	{
		SetPage(AchievementMenuPage.Leaderboard);
		await RefreshLeaderboardAsync(synchronizeProfile: _hasLoadedStatistics);
	}

	private async void OnSynchronizeButtonPressed()
	{
		if (!_hasLoadedStatistics)
		{
			ShowLeaderboardStatus(
				"Die lokale Statistik ist nicht verfügbar und kann nicht synchronisiert werden.",
				isWarning: true);
			return;
		}

		await RefreshLeaderboardAsync(synchronizeProfile: true);
	}

	private async void OnRefreshLeaderboardButtonPressed()
	{
		await RefreshLeaderboardAsync(synchronizeProfile: false);
	}

	private async Task RefreshLeaderboardAsync(bool synchronizeProfile)
	{
		if (_isLeaderboardRequestRunning)
			return;

		_isLeaderboardRequestRunning = true;
		SetLeaderboardButtonsDisabled(true);
		ShowLeaderboardStatus(
			synchronizeProfile ? "Profil wird synchronisiert …" : "Rangliste wird geladen …",
			isWarning: false);

		try
		{
			IReadOnlyList<DeveloperLeaderboardEntry> entries = synchronizeProfile
				? await _leaderboardManager.SynchronizeAsync(
					_statisticsData,
					_developerNameInput.Text)
				: await _leaderboardManager.GetLeaderboardAsync();
			if (!IsInstanceValid(this))
				return;

			UpdateLeaderboard(entries);
			ShowLeaderboardStatus(
				synchronizeProfile
					? "Profil synchronisiert."
					: "Rangliste aktualisiert.",
				isWarning: false);
		}
		catch (Exception exception)
		{
			if (!IsInstanceValid(this))
				return;

			ShowLeaderboardStatus(
				synchronizeProfile
					? "Das Profil konnte nicht synchronisiert werden."
					: "Die Rangliste konnte nicht geladen werden.",
				isWarning: true);
			GD.PushWarning($"AchievementMenu: {exception.Message}");
		}
		finally
		{
			if (IsInstanceValid(this))
			{
				_isLeaderboardRequestRunning = false;
				SetLeaderboardButtonsDisabled(false);
			}
		}
	}

	private void SetPage(AchievementMenuPage page)
	{
		bool showAchievements = page == AchievementMenuPage.Achievements;
		_achievementPage.Visible = showAchievements;
		_leaderboardPage.Visible = !showAchievements;
		_achievementsTabButton.ButtonPressed = showAchievements;
		_leaderboardTabButton.ButtonPressed = !showAchievements;
	}

	private void SetLeaderboardButtonsDisabled(bool disabled)
	{
		_synchronizeButton.Disabled = disabled;
		_refreshLeaderboardButton.Disabled = disabled;
		_developerNameInput.Editable = !disabled;
	}

	private void ShowLeaderboardStatus(string message, bool isWarning)
	{
		_leaderboardStatusLabel.Text = message;
		_leaderboardStatusLabel.AddThemeColorOverride(
			"font_color",
			isWarning ? new Color(0.62f, 0.17f, 0.12f) : new Color(0.34f, 0.23f, 0.14f));
	}

	private void UpdateLeaderboard(IReadOnlyList<DeveloperLeaderboardEntry> entries)
	{
		foreach (Node child in _leaderboardList.GetChildren())
			child.QueueFree();

		if (entries.Count == 0)
		{
			_leaderboardList.AddChild(CreateLabel(
				"Noch kein Entwicklerprofil synchronisiert.",
				19,
				new Color(0.38f, 0.29f, 0.19f)));
			return;
		}

		for (int index = 0; index < entries.Count; index++)
			_leaderboardList.AddChild(CreateLeaderboardRow(entries[index], index + 1));
	}

	private static PanelContainer CreateLeaderboardRow(DeveloperLeaderboardEntry entry, int rank)
	{
		Color rankColor = rank switch
		{
			1 => new Color(0.98f, 0.76f, 0.28f),
			2 => new Color(0.76f, 0.82f, 0.82f),
			3 => new Color(0.78f, 0.50f, 0.26f),
			_ => new Color(0.42f, 0.54f, 0.26f)
		};
		PanelContainer row = new()
		{
			CustomMinimumSize = new Vector2(0.0f, 68.0f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		row.AddThemeStyleboxOverride("panel", CreateLeaderboardRowStyle(rankColor));

		HBoxContainer content = new()
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		content.AddThemeConstantOverride("separation", 12);
		row.AddChild(content);

		PanelContainer rankBadge = new()
		{
			CustomMinimumSize = new Vector2(46.0f, 46.0f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		rankBadge.AddThemeStyleboxOverride("panel", CreateBadgeStyle(rankColor, isUnlocked: true));
		content.AddChild(rankBadge);
		Label rankLabel = CreateLabel($"{rank}.", 20, rankColor);
		rankLabel.HorizontalAlignment = HorizontalAlignment.Center;
		rankLabel.VerticalAlignment = VerticalAlignment.Center;
		rankBadge.AddChild(rankLabel);

		VBoxContainer profile = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		content.AddChild(profile);
		profile.AddChild(CreateLabel(
			string.IsNullOrWhiteSpace(entry.DisplayName) ? "Entwickler" : entry.DisplayName,
			22,
			new Color(0.25f, 0.105f, 0.055f)));
		profile.AddChild(CreateLabel(
			$"{entry.CompletedGames} Partien · {entry.Wins} Siege · {entry.UnlockedAchievementIds.Count} Abzeichen",
			16,
			new Color(0.35f, 0.24f, 0.14f)));

		Label score = CreateLabel(
			$"STUFE {entry.CurrentLevel}\n{entry.TotalExperience} EP",
			18,
			rankColor);
		score.HorizontalAlignment = HorizontalAlignment.Right;
		score.VerticalAlignment = VerticalAlignment.Center;
		content.AddChild(score);

		return row;
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

	private static StyleBoxFlat CreateLeaderboardRowStyle(Color rankColor)
	{
		return new StyleBoxFlat
		{
			ContentMarginLeft = 12.0f,
			ContentMarginTop = 9.0f,
			ContentMarginRight = 14.0f,
			ContentMarginBottom = 9.0f,
			BgColor = new Color(0.83f, 0.68f, 0.49f, 1.0f),
			BorderWidthLeft = 3,
			BorderWidthTop = 3,
			BorderWidthRight = 3,
			BorderWidthBottom = 3,
			BorderColor = rankColor,
			CornerRadiusTopLeft = 15,
			CornerRadiusTopRight = 15,
			CornerRadiusBottomRight = 15,
			CornerRadiusBottomLeft = 15,
			ShadowColor = new Color(0.18f, 0.075f, 0.035f, 0.25f),
			ShadowSize = 4,
			ShadowOffset = new Vector2(0.0f, 3.0f)
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
