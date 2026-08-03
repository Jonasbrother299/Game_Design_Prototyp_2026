using Godot;
using System;
using System.IO;

public sealed class DeveloperProfileSettings
{
	private const string SettingsPath = "user://developer_leaderboard.cfg";
	private const string ConnectionSection = "connection";
	private const string ProfileSection = "profile";
	private const string ApiUrlKey = "api_url";
	private const string PublishableKeyKey = "publishable_key";
	private const string DeveloperIdKey = "developer_id";
	private const string DisplayNameKey = "display_name";
	private const string DefaultApiUrl = "https://twytuvtoiieyrenqdegi.supabase.co/rest/v1";
	private const string DefaultPublishableKey = "sb_publishable_qdmHiVWB04dABuW0dkUzSw_huLolL1T";
	private const string PublishableKeyEnvironmentVariable =
		"ECOSYSTEM_SUPABASE_PUBLISHABLE_KEY";

	public DeveloperProfileConfiguration Load()
	{
		ConfigFile config = LoadOrCreateConfig();
		string developerId = config
			.GetValue(ProfileSection, DeveloperIdKey, "")
			.AsString();
		if (!Guid.TryParse(developerId, out _))
		{
			developerId = Guid.NewGuid().ToString();
			config.SetValue(ProfileSection, DeveloperIdKey, developerId);
			SaveConfig(config);
		}

		string publishableKey = OS.GetEnvironment(PublishableKeyEnvironmentVariable);
		if (string.IsNullOrWhiteSpace(publishableKey))
		{
			publishableKey = config
				.GetValue(ConnectionSection, PublishableKeyKey, "")
				.AsString();
			if (string.IsNullOrWhiteSpace(publishableKey))
				publishableKey = DefaultPublishableKey;
		}

		return new DeveloperProfileConfiguration
		{
			DeveloperId = developerId,
			DisplayName = config
				.GetValue(ProfileSection, DisplayNameKey, "Entwickler")
				.AsString(),
			ApiUrl = config
				.GetValue(ConnectionSection, ApiUrlKey, DefaultApiUrl)
				.AsString()
				.TrimEnd('/'),
			PublishableKey = publishableKey.Trim()
		};
	}

	public DeveloperProfileConfiguration SaveDisplayName(string displayName)
	{
		ConfigFile config = LoadOrCreateConfig();
		string normalizedDisplayName = string.IsNullOrWhiteSpace(displayName)
			? "Entwickler"
			: displayName.Trim();
		config.SetValue(ProfileSection, DisplayNameKey, normalizedDisplayName);
		SaveConfig(config);
		return Load();
	}

	private static ConfigFile LoadOrCreateConfig()
	{
		ConfigFile config = new();
		Error loadError = config.Load(SettingsPath);
		if (loadError == Error.FileNotFound)
		{
			config.SetValue(ConnectionSection, ApiUrlKey, DefaultApiUrl);
			config.SetValue(ConnectionSection, PublishableKeyKey, DefaultPublishableKey);
			config.SetValue(ProfileSection, DeveloperIdKey, Guid.NewGuid().ToString());
			config.SetValue(ProfileSection, DisplayNameKey, "Entwickler");
			SaveConfig(config);
			return config;
		}

		if (loadError != Error.Ok)
		{
			throw new IOException(
				$"Die Entwicklerprofil-Konfiguration konnte nicht geladen werden: {loadError}");
		}

		return config;
	}

	private static void SaveConfig(ConfigFile config)
	{
		Error saveError = config.Save(SettingsPath);
		if (saveError != Error.Ok)
		{
			throw new IOException(
				$"Die Entwicklerprofil-Konfiguration konnte nicht gespeichert werden: {saveError}");
		}
	}
}

public sealed class DeveloperProfileConfiguration
{
	public string DeveloperId { get; init; } = "";
	public string DisplayName { get; init; } = "Entwickler";
	public string ApiUrl { get; init; } = "";
	public string PublishableKey { get; init; } = "";

	public bool IsConnectionConfigured =>
		!string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrWhiteSpace(PublishableKey);
}
