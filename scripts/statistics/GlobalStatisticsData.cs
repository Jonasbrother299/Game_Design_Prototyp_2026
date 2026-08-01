using System;
using System.Collections.Generic;

public sealed class GlobalStatisticsData
{
	public const int CurrentSaveVersion = 1;

	public int SaveVersion { get; set; } = CurrentSaveVersion;
	public List<CompletedGameStatisticsEntry> CompletedGames { get; set; } = new();
	public List<SpecialStatisticsEvent> SpecialEvents { get; set; } = new();
	public List<string> UnlockedAchievementIds { get; set; } = new();
}

public sealed class CompletedGameStatisticsEntry
{
	public DateTimeOffset CompletedAt { get; set; }
	public bool HasWon { get; set; }
	public int FinalRound { get; set; }
	public int FinalWater { get; set; }
	public int MainTreeProgress { get; set; }
	public int LivingPlantCount { get; set; }
	public int PlantsDiedTotal { get; set; }
	public int CardsPlayedTotal { get; set; }
	public double PlayTimeSeconds { get; set; }
}

public sealed class SpecialStatisticsEvent
{
	public DateTimeOffset OccurredAt { get; set; }
	public string EventTypeId { get; set; } = "";
	public int RoundNumber { get; set; }
	public int Value { get; set; }
}

public enum AchievementBadgeTier
{
	Bronze,
	Silver,
	Gold,
	Verdant,
	Copper
}

public sealed class AchievementDefinition
{
	public string Id { get; init; } = "";
	public string DisplayName { get; init; } = "";
	public string Description { get; init; } = "";
	public AchievementBadgeTier BadgeTier { get; init; }
	public int ExperienceReward { get; init; }
}

public sealed class AchievementProgress
{
	private readonly HashSet<string> _unlockedAchievementIds;

	public int TotalExperience { get; }
	public int CurrentLevel { get; }
	public int ExperienceInCurrentLevel { get; }
	public int ExperienceForNextLevel { get; }
	public int UnlockedAchievementCount { get; }

	public AchievementProgress(
		IEnumerable<string> unlockedAchievementIds,
		int totalExperience,
		int currentLevel,
		int experienceInCurrentLevel,
		int experienceForNextLevel,
		int unlockedAchievementCount)
	{
		_unlockedAchievementIds = new HashSet<string>(
			unlockedAchievementIds,
			StringComparer.Ordinal);
		TotalExperience = totalExperience;
		CurrentLevel = currentLevel;
		ExperienceInCurrentLevel = experienceInCurrentLevel;
		ExperienceForNextLevel = experienceForNextLevel;
		UnlockedAchievementCount = unlockedAchievementCount;
	}

	public bool IsUnlocked(string achievementId)
	{
		return _unlockedAchievementIds.Contains(achievementId);
	}
}

public static class AchievementCatalog
{
	private const string MassPlantDeathEventId = "mass_plant_death";

	private static readonly IReadOnlyList<AchievementDefinition> Definitions =
		new List<AchievementDefinition>
		{
			new()
			{
				Id = "first_forest_path",
				DisplayName = "Erster Waldpfad",
				Description = "Schließe eine Partie ab.",
				BadgeTier = AchievementBadgeTier.Bronze,
				ExperienceReward = 100
			},
			new()
			{
				Id = "grove_guardian",
				DisplayName = "Hüter des Hains",
				Description = "Schließe 15 Partien ab.",
				BadgeTier = AchievementBadgeTier.Silver,
				ExperienceReward = 350
			},
			new()
			{
				Id = "ancient_forest_chronicler",
				DisplayName = "Chronist des Urwalds",
				Description = "Schließe 50 Partien ab.",
				BadgeTier = AchievementBadgeTier.Gold,
				ExperienceReward = 700
			},
			new()
			{
				Id = "springkeeper",
				DisplayName = "Quellhüter",
				Description = "Gewinne eine Partie.",
				BadgeTier = AchievementBadgeTier.Verdant,
				ExperienceReward = 200
			},
			new()
			{
				Id = "storm_chronicler",
				DisplayName = "Sturmchronist",
				Description = "Verliere 15 Pflanzen in einer Runde.",
				BadgeTier = AchievementBadgeTier.Copper,
				ExperienceReward = 150
			}
		};

	public static IReadOnlyList<AchievementDefinition> All => Definitions;

	public static IReadOnlyList<AchievementDefinition> UnlockNewAchievements(
		GlobalStatisticsData data)
	{
		if (data == null)
			throw new ArgumentNullException(nameof(data));

		data.UnlockedAchievementIds ??= new List<string>();
		HashSet<string> unlockedIds = new(
			data.UnlockedAchievementIds,
			StringComparer.Ordinal);
		List<AchievementDefinition> newlyUnlocked = new();

		foreach (AchievementDefinition definition in Definitions)
		{
			if (unlockedIds.Contains(definition.Id) || !MeetsRequirement(definition.Id, data))
				continue;

			data.UnlockedAchievementIds.Add(definition.Id);
			unlockedIds.Add(definition.Id);
			newlyUnlocked.Add(definition);
		}

		return newlyUnlocked;
	}

	public static AchievementProgress GetProgress(GlobalStatisticsData data)
	{
		if (data == null)
			throw new ArgumentNullException(nameof(data));

		data.UnlockedAchievementIds ??= new List<string>();
		HashSet<string> unlockedIds = new(
			data.UnlockedAchievementIds,
			StringComparer.Ordinal);
		int totalExperience = 0;
		int unlockedAchievementCount = 0;

		foreach (AchievementDefinition definition in Definitions)
		{
			if (!unlockedIds.Contains(definition.Id))
				continue;

			totalExperience += definition.ExperienceReward;
			unlockedAchievementCount++;
		}

		int currentLevel = 1;
		int experienceInCurrentLevel = totalExperience;
		int experienceForNextLevel = GetExperienceRequiredForLevel(currentLevel);

		while (experienceInCurrentLevel >= experienceForNextLevel)
		{
			experienceInCurrentLevel -= experienceForNextLevel;
			currentLevel++;
			experienceForNextLevel = GetExperienceRequiredForLevel(currentLevel);
		}

		return new AchievementProgress(
			unlockedIds,
			totalExperience,
			currentLevel,
			experienceInCurrentLevel,
			experienceForNextLevel,
			unlockedAchievementCount);
	}

	private static bool MeetsRequirement(string achievementId, GlobalStatisticsData data)
	{
		return achievementId switch
		{
			"first_forest_path" => data.CompletedGames.Count >= 1,
			"grove_guardian" => data.CompletedGames.Count >= 15,
			"ancient_forest_chronicler" => data.CompletedGames.Count >= 50,
			"springkeeper" => HasWonGame(data.CompletedGames),
			"storm_chronicler" => HasMassPlantDeath(data.SpecialEvents),
			_ => false
		};
	}

	private static bool HasWonGame(IReadOnlyList<CompletedGameStatisticsEntry> completedGames)
	{
		foreach (CompletedGameStatisticsEntry entry in completedGames)
		{
			if (entry.HasWon)
				return true;
		}

		return false;
	}

	private static bool HasMassPlantDeath(IReadOnlyList<SpecialStatisticsEvent> specialEvents)
	{
		foreach (SpecialStatisticsEvent entry in specialEvents)
		{
			if (entry.EventTypeId == MassPlantDeathEventId && entry.Value >= 15)
				return true;
		}

		return false;
	}

	private static int GetExperienceRequiredForLevel(int level)
	{
		return 250 + (level - 1) * 100;
	}
}
