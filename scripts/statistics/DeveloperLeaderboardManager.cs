using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public sealed class DeveloperLeaderboardManager
{
	private const int LeaderboardEntryLimit = 20;
	private static readonly HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromSeconds(10)
	};
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	private readonly DeveloperProfileSettings _profileSettings = new();

	public DeveloperProfileConfiguration LoadProfile()
	{
		return _profileSettings.Load();
	}

	public DeveloperProfileConfiguration SaveDisplayName(string displayName)
	{
		return _profileSettings.SaveDisplayName(displayName);
	}

	public async Task<IReadOnlyList<DeveloperLeaderboardEntry>> SynchronizeAsync(
		GlobalStatisticsData statistics,
		string displayName)
	{
		if (statistics == null)
			throw new ArgumentNullException(nameof(statistics));

		DeveloperProfileConfiguration profile = SaveDisplayName(displayName);
		EnsureConnectionIsConfigured(profile);

		AchievementProgress progress = AchievementCatalog.GetProgress(statistics);
		DeveloperLeaderboardEntry entry = new()
		{
			DeveloperId = profile.DeveloperId,
			DisplayName = profile.DisplayName,
			TotalExperience = progress.TotalExperience,
			CurrentLevel = progress.CurrentLevel,
			CompletedGames = statistics.CompletedGames.Count,
			Wins = CountWins(statistics.CompletedGames),
			UnlockedAchievementIds = new List<string>(statistics.UnlockedAchievementIds),
			UpdatedAt = DateTimeOffset.UtcNow
		};

		await UpsertProfileAsync(profile, entry);
		return await GetLeaderboardAsync(profile);
	}

	public async Task<IReadOnlyList<DeveloperLeaderboardEntry>> GetLeaderboardAsync()
	{
		DeveloperProfileConfiguration profile = LoadProfile();
		EnsureConnectionIsConfigured(profile);
		return await GetLeaderboardAsync(profile);
	}

	private static async Task UpsertProfileAsync(
		DeveloperProfileConfiguration profile,
		DeveloperLeaderboardEntry entry)
	{
		using HttpRequestMessage request = CreateRequest(
			HttpMethod.Post,
			$"{profile.ApiUrl}/developer_profiles?on_conflict=developer_id",
			profile.PublishableKey);
		request.Headers.TryAddWithoutValidation(
			"Prefer",
			"resolution=merge-duplicates,return=minimal");
		request.Content = new StringContent(
			JsonSerializer.Serialize(entry, SerializerOptions),
			Encoding.UTF8,
			"application/json");

		using HttpResponseMessage response = await HttpClient.SendAsync(request);
		await EnsureSuccessAsync(response, "Das Entwicklerprofil konnte nicht synchronisiert werden");
	}

	private static async Task<IReadOnlyList<DeveloperLeaderboardEntry>> GetLeaderboardAsync(
		DeveloperProfileConfiguration profile)
	{
		string requestUrl =
			$"{profile.ApiUrl}/developer_profiles?select=developer_id,display_name,total_experience," +
			$"current_level,completed_games,wins,unlocked_achievement_ids,updated_at&" +
			$"order=current_level.desc,total_experience.desc,updated_at.asc&limit={LeaderboardEntryLimit}";
		using HttpRequestMessage request = CreateRequest(
			HttpMethod.Get,
			requestUrl,
			profile.PublishableKey);
		using HttpResponseMessage response = await HttpClient.SendAsync(request);
		await EnsureSuccessAsync(response, "Die Entwickler-Rangliste konnte nicht geladen werden");

		string json = await response.Content.ReadAsStringAsync();
		List<DeveloperLeaderboardEntry> entries = JsonSerializer.Deserialize<
			List<DeveloperLeaderboardEntry>>(json, SerializerOptions);
		return entries ?? new List<DeveloperLeaderboardEntry>();
	}

	private static HttpRequestMessage CreateRequest(
		HttpMethod method,
		string requestUrl,
		string publishableKey)
	{
		HttpRequestMessage request = new(method, requestUrl);
		request.Headers.Add("apikey", publishableKey);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", publishableKey);
		return request;
	}

	private static async Task EnsureSuccessAsync(HttpResponseMessage response, string message)
	{
		if (response.IsSuccessStatusCode)
			return;

		string responseBody = await response.Content.ReadAsStringAsync();
		throw new InvalidOperationException(
			$"{message}: {(int)response.StatusCode} {response.ReasonPhrase}. {responseBody}");
	}

	private static int CountWins(IReadOnlyList<CompletedGameStatisticsEntry> completedGames)
	{
		int wins = 0;
		foreach (CompletedGameStatisticsEntry entry in completedGames)
		{
			if (entry.HasWon)
				wins++;
		}

		return wins;
	}

	private static void EnsureConnectionIsConfigured(DeveloperProfileConfiguration profile)
	{
		if (!profile.IsConnectionConfigured)
		{
			throw new InvalidOperationException(
				"Die Supabase-Verbindung ist nicht konfiguriert. " +
				"Lege den Publishable Key in user://developer_leaderboard.cfg fest.");
		}
	}
}

public sealed class DeveloperLeaderboardEntry
{
	[JsonPropertyName("developer_id")]
	public string DeveloperId { get; set; } = "";

	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; } = "";

	[JsonPropertyName("total_experience")]
	public int TotalExperience { get; set; }

	[JsonPropertyName("current_level")]
	public int CurrentLevel { get; set; }

	[JsonPropertyName("completed_games")]
	public int CompletedGames { get; set; }

	[JsonPropertyName("wins")]
	public int Wins { get; set; }

	[JsonPropertyName("unlocked_achievement_ids")]
	public List<string> UnlockedAchievementIds { get; set; } = new();

	[JsonPropertyName("updated_at")]
	public DateTimeOffset UpdatedAt { get; set; }
}
