using Godot;

public static class LoadProfiler
{
	public const string LogPath = "user://load-profile.log";

	private static FileAccess _logFile;
	private static ulong _sessionStartedUsec;
	private static bool _sessionActive;

	public static void StartSession()
	{
		_logFile?.Dispose();
		_logFile = FileAccess.Open(LogPath, FileAccess.ModeFlags.Write);
		_sessionStartedUsec = Time.GetTicksUsec();
		_sessionActive = true;

		Write("Ladeprofil gestartet");
		Write($"Protokolldatei: {ProjectSettings.GlobalizePath(LogPath)}");
	}

	public static ulong BeginPhase(string phase)
	{
		EnsureSession();
		Write($"START | {phase}");
		return Time.GetTicksUsec();
	}

	public static void EndPhase(string phase, ulong phaseStartedUsec)
	{
		ulong elapsedUsec = Time.GetTicksUsec() - phaseStartedUsec;
		Write($"ENDE  | {phase} | Dauer {FormatDuration(elapsedUsec)}");
	}

	public static void Mark(string message)
	{
		EnsureSession();
		Write(message);
	}

	public static void FinishSession(string message)
	{
		if (!_sessionActive)
			return;

		Write(message);
		_logFile?.Dispose();
		_logFile = null;
		_sessionActive = false;
	}

	private static void EnsureSession()
	{
		if (!_sessionActive)
			StartSession();
	}

	private static void Write(string message)
	{
		ulong elapsedUsec = _sessionActive
			? Time.GetTicksUsec() - _sessionStartedUsec
			: 0;
		string line =
			$"[LoadProfile +{elapsedUsec / 1_000_000.0:0.000}s] {message}";

		GD.Print(line);
		if (_logFile == null)
			return;

		_logFile.StoreLine(line);
		_logFile.Flush();
	}

	private static string FormatDuration(ulong elapsedUsec)
	{
		return $"{elapsedUsec / 1_000_000.0:0.000}s";
	}
}
