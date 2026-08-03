using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Godot;

public sealed class GlobalStatisticsManager
{
	private const string StatisticsDirectoryUri = "user://statistics";
	private const string StatisticsFileName = "global_statistics.json";
	private const string BackupFileName = "global_statistics.backup.json";
	private const string TemporaryFileName = "global_statistics.tmp";
	private const string MassPlantDeathEventId = "mass_plant_death";

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly string _statisticsDirectoryPath;

	public GlobalStatisticsManager(string statisticsDirectoryPath = null)
	{
		_statisticsDirectoryPath = string.IsNullOrWhiteSpace(statisticsDirectoryPath)
			? ProjectSettings.GlobalizePath(StatisticsDirectoryUri)
			: statisticsDirectoryPath;
	}

	public GlobalStatisticsData GetStatistics()
	{
		return ReadStatistics();
	}

	public IReadOnlyList<AchievementDefinition> RecordCompletedGame(
		CompletedGameStatisticsEntry entry)
	{
		if (entry == null)
			throw new ArgumentNullException(nameof(entry));

		GlobalStatisticsData data = ReadStatistics();
		data.CompletedGames.Add(entry);
		IReadOnlyList<AchievementDefinition> newlyUnlocked =
			AchievementCatalog.UnlockNewAchievements(data);
		WriteStatistics(data);
		return newlyUnlocked;
	}

	public IReadOnlyList<AchievementDefinition> RecordMassPlantDeath(
		RoundStatisticsEntry entry)
	{
		if (entry == null)
			throw new ArgumentNullException(nameof(entry));

		GlobalStatisticsData data = ReadStatistics();
		data.SpecialEvents.Add(new SpecialStatisticsEvent
		{
			OccurredAt = entry.CompletedAt.ToUniversalTime(),
			EventTypeId = MassPlantDeathEventId,
			RoundNumber = entry.RoundNumber,
			Value = entry.PlantsDiedThisRound
		});
		IReadOnlyList<AchievementDefinition> newlyUnlocked =
			AchievementCatalog.UnlockNewAchievements(data);
		WriteStatistics(data);
		return newlyUnlocked;
	}

	private GlobalStatisticsData ReadStatistics()
	{
		string primaryPath = GetPath(StatisticsFileName);
		string backupPath = GetPath(BackupFileName);
		bool hasPrimaryFile = File.Exists(primaryPath);
		bool hasBackupFile = File.Exists(backupPath);

		if (TryReadAndValidate(primaryPath, out GlobalStatisticsData primaryData,
			out string primaryError))
		{
			return primaryData;
		}

		if (hasPrimaryFile)
		{
			GD.PushWarning(
				$"GlobalStatisticsManager: Die Statistikdatei ist ungültig: {primaryError}");
		}

		if (TryReadAndValidate(backupPath, out GlobalStatisticsData backupData,
			out string backupError))
		{
			GD.PushWarning(
				"GlobalStatisticsManager: Die Statistik wird aus dem Backup fortgesetzt.");
			return backupData;
		}

		if (!hasPrimaryFile && !hasBackupFile)
			return new GlobalStatisticsData();

		throw new InvalidDataException(
			$"Die Statistikdatei und ihr Backup sind ungültig. Hauptdatei: {primaryError}; " +
			$"Backup: {backupError}");
	}

	private void WriteStatistics(GlobalStatisticsData data)
	{
		Validate(data);
		Directory.CreateDirectory(_statisticsDirectoryPath);

		string primaryPath = GetPath(StatisticsFileName);
		string backupPath = GetPath(BackupFileName);
		string temporaryPath = GetPath(TemporaryFileName);

		try
		{
			File.WriteAllText(
				temporaryPath,
				JsonSerializer.Serialize(data, SerializerOptions),
				Encoding.UTF8);

			if (!TryReadAndValidate(temporaryPath, out _, out string temporaryError))
			{
				throw new InvalidDataException(
					$"Die temporäre Statistikdatei ist ungültig: {temporaryError}");
			}

			if (TryReadAndValidate(primaryPath, out _, out _))
				File.Copy(primaryPath, backupPath, overwrite: true);

			File.Move(temporaryPath, primaryPath, overwrite: true);
		}
		finally
		{
			if (File.Exists(temporaryPath))
				File.Delete(temporaryPath);
		}
	}

	private bool TryReadAndValidate(
		string path,
		out GlobalStatisticsData data,
		out string error)
	{
		data = null;
		error = "";

		try
		{
			if (!File.Exists(path))
			{
				error = "Datei fehlt.";
				return false;
			}

			string json = File.ReadAllText(path, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(json))
			{
				error = "Datei ist leer.";
				return false;
			}

			data = JsonSerializer.Deserialize<GlobalStatisticsData>(json, SerializerOptions);
			Normalize(data);
			Validate(data);
			return true;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
			JsonException or InvalidDataException)
		{
			error = exception.Message;
			data = null;
			return false;
		}
	}

	private static void Validate(GlobalStatisticsData data)
	{
		if (data == null)
			throw new InvalidDataException("Die Statistikdatei enthält keine Daten.");

		if (data.SaveVersion != GlobalStatisticsData.CurrentSaveVersion)
		{
			throw new InvalidDataException(
				$"Die Statistikversion {data.SaveVersion} wird nicht unterstützt.");
		}

		if (data.CompletedGames == null || data.SpecialEvents == null ||
			data.UnlockedAchievementIds == null)
			throw new InvalidDataException("Die Statistikdatei enthält unvollständige Listen.");
	}

	private static void Normalize(GlobalStatisticsData data)
	{
		if (data != null)
			data.UnlockedAchievementIds ??= new List<string>();
	}

	private string GetPath(string fileName)
	{
		return Path.Combine(_statisticsDirectoryPath, fileName);
	}
}
