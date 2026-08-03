using Godot;
using System;

public partial class MenuMusicPlayer : PanelContainer
{
	private const string MusicBusName = "Music";
	private static readonly Vector2 ExpandedScreenMargin =
		new(72.0f, 78.0f);
	private static readonly Vector2 CollapsedScreenMargin =
		new(32.0f, 24.0f);

	[ExportGroup("Playlist")]
	[Export] public AudioStream[] Tracks = Array.Empty<AudioStream>();
	[Export] public string[] TrackTitles = Array.Empty<string>();
	[Export] public string[] TrackArtists = Array.Empty<string>();
	[Export] public Texture2D[] TrackArtwork = Array.Empty<Texture2D>();

	[ExportGroup("Layout")]
	[Export] public Vector2 ExpandedSize = new(760.0f, 350.0f);
	[Export] public Vector2 CollapsedSize = new(420.0f, 96.0f);
	[Export] public bool StartCollapsed;

	private AudioStreamPlayer _audioPlayer;
	private Control _content;
	private Control _collapsedContent;
	private TextureRect _coverImage;
	private Label _coverFallback;
	private Label _trackTitle;
	private Label _trackArtist;
	private Label _collapsedTrackTitle;
	private Button _previousButton;
	private Button _stopButton;
	private Button _nextButton;
	private Button _muteButton;
	private Button _collapseButton;
	private Button _expandButton;
	private ProgressBar _progressBar;
	private Label _elapsedTimeLabel;
	private Label _durationLabel;
	private int _currentTrackIndex;
	private int _musicBusIndex = -1;
	private bool _isCollapsed;

	public override void _Ready()
	{
		_audioPlayer = GetNode<AudioStreamPlayer>("%AudioPlayer");
		_content = GetNode<Control>("%Content");
		_collapsedContent = GetNode<Control>("%CollapsedContent");
		_coverImage = GetNode<TextureRect>("%CoverImage");
		_coverFallback = GetNode<Label>("%CoverFallback");
		_trackTitle = GetNode<Label>("%TrackTitle");
		_trackArtist = GetNode<Label>("%TrackArtist");
		_collapsedTrackTitle = GetNode<Label>("%CollapsedTrackTitle");
		_previousButton = GetNode<Button>("%PreviousButton");
		_stopButton = GetNode<Button>("%StopButton");
		_nextButton = GetNode<Button>("%NextButton");
		_muteButton = GetNode<Button>("%MuteButton");
		_collapseButton = GetNode<Button>("%CollapseButton");
		_expandButton = GetNode<Button>("%ExpandButton");
		_progressBar = GetNode<ProgressBar>("%TrackProgress");
		_elapsedTimeLabel = GetNode<Label>("%ElapsedTimeLabel");
		_durationLabel = GetNode<Label>("%DurationLabel");

		EnsureMusicBus();
		_previousButton.Pressed += PlayPreviousTrack;
		_stopButton.Pressed += StopPlayback;
		_nextButton.Pressed += PlayNextTrack;
		_muteButton.Pressed += ToggleMute;
		_collapseButton.Pressed += CollapsePlayer;
		_expandButton.Pressed += ExpandPlayer;
		_audioPlayer.Finished += PlayNextTrack;

		SetCollapsed(StartCollapsed);
		UpdatePlayerState();
		if (TryFindPlayableTrack(_currentTrackIndex, 1, out int firstTrackIndex))
			PlayTrack(firstTrackIndex);
	}

	public override void _ExitTree()
	{
		if (_previousButton != null)
			_previousButton.Pressed -= PlayPreviousTrack;
		if (_stopButton != null)
			_stopButton.Pressed -= StopPlayback;
		if (_nextButton != null)
			_nextButton.Pressed -= PlayNextTrack;
		if (_muteButton != null)
			_muteButton.Pressed -= ToggleMute;
		if (_collapseButton != null)
			_collapseButton.Pressed -= CollapsePlayer;
		if (_expandButton != null)
			_expandButton.Pressed -= ExpandPlayer;
		if (_audioPlayer != null)
			_audioPlayer.Finished -= PlayNextTrack;
	}

	public override void _Process(double _)
	{
		UpdateProgress();
		UpdateMuteButton();
	}

	private void PlayPreviousTrack()
	{
		if (!TryFindPlayableTrack(
			_currentTrackIndex - 1,
			-1,
			out int previousTrackIndex))
			return;

		PlayTrack(previousTrackIndex);
	}

	private void PlayNextTrack()
	{
		if (!TryFindPlayableTrack(
			_currentTrackIndex + 1,
			1,
			out int nextTrackIndex))
			return;

		PlayTrack(nextTrackIndex);
	}

	private void StopPlayback()
	{
		if (_audioPlayer == null)
			return;

		_audioPlayer.Stop();
		UpdateProgress();
	}

	private void CollapsePlayer()
	{
		SetCollapsed(true);
	}

	private void ExpandPlayer()
	{
		SetCollapsed(false);
	}

	private void SetCollapsed(bool collapsed)
	{
		_isCollapsed = collapsed;
		_content.Visible = !_isCollapsed;
		_collapseButton.Visible = !_isCollapsed;
		_collapsedContent.Visible = _isCollapsed;
		ApplyAnchoredSize(
			_isCollapsed ? CollapsedSize : ExpandedSize,
			_isCollapsed ? CollapsedScreenMargin : ExpandedScreenMargin);
	}

	private void ApplyAnchoredSize(Vector2 playerSize, Vector2 screenMargin)
	{
		CustomMinimumSize = playerSize;
		AnchorLeft = 1.0f;
		AnchorTop = 1.0f;
		AnchorRight = 1.0f;
		AnchorBottom = 1.0f;
		OffsetRight = -screenMargin.X;
		OffsetBottom = -screenMargin.Y;
		OffsetLeft = OffsetRight - playerSize.X;
		OffsetTop = OffsetBottom - playerSize.Y;
	}

	private void ToggleMute()
	{
		if (_musicBusIndex < 0)
			return;

		AudioServer.SetBusMute(
			_musicBusIndex,
			!AudioServer.IsBusMute(_musicBusIndex));
		UpdateMuteButton();
	}

	private void PlayTrack(int trackIndex)
	{
		if (Tracks == null || trackIndex < 0 || trackIndex >= Tracks.Length)
			return;

		AudioStream track = Tracks[trackIndex];
		if (track == null)
		{
			GD.PushWarning(
				$"MenuMusicPlayer: Titel {trackIndex + 1} enthält keinen Audiostream.");
			return;
		}

		_currentTrackIndex = trackIndex;
		_audioPlayer.Stream = track;
		_audioPlayer.Play();
		UpdatePlayerState();
	}

	private void UpdatePlayerState()
	{
		bool hasTracks = HasPlayableTracks();
		_previousButton.Disabled = !hasTracks;
		_stopButton.Disabled = !hasTracks;
		_nextButton.Disabled = !hasTracks;

		if (!hasTracks)
		{
			_coverImage.Texture = null;
			_coverImage.Hide();
			_coverFallback.Show();
			_trackTitle.Text = "KEIN TITEL AUSGEWÄHLT";
			_trackArtist.Text = "Musikdateien folgen";
			_progressBar.Value = 0.0;
			_elapsedTimeLabel.Text = "00:00";
			_durationLabel.Text = "00:00";
			_collapsedTrackTitle.Text = "MUSIKPLAYER";
			UpdateMuteButton();
			return;
		}

		_trackTitle.Text = GetTrackTitle(_currentTrackIndex);
		_trackArtist.Text = GetTrackArtist(_currentTrackIndex);
		_collapsedTrackTitle.Text =
			$"MUSIK  •  {GetTrackTitle(_currentTrackIndex)}";
		Texture2D artwork = GetTrackArtwork(_currentTrackIndex);
		_coverImage.Texture = artwork;
		_coverImage.Visible = artwork != null;
		_coverFallback.Visible = artwork == null;
		UpdateProgress();
		UpdateMuteButton();
	}

	private void UpdateProgress()
	{
		if (_audioPlayer?.Stream == null)
			return;

		float duration = Mathf.Max(
			(float)_audioPlayer.Stream.GetLength(),
			0.0f);
		float elapsed = Mathf.Clamp(
			(float)_audioPlayer.GetPlaybackPosition(),
			0.0f,
			duration);
		_progressBar.MaxValue = Mathf.Max(duration, 0.01f);
		_progressBar.Value = elapsed;
		_elapsedTimeLabel.Text = FormatTime(elapsed);
		_durationLabel.Text = FormatTime(duration);
	}

	private void UpdateMuteButton()
	{
		bool isMuted =
			_musicBusIndex >= 0 && AudioServer.IsBusMute(_musicBusIndex);
		_muteButton.Text = isMuted
			? "♪ WALDKLANG AUS"
			: "♪ WALDKLANG AN";
		_muteButton.TooltipText = isMuted
			? "Musik einschalten"
			: "Musik stummschalten";
	}

	private bool HasPlayableTracks()
	{
		return TryFindPlayableTrack(0, 1, out _);
	}

	private bool TryFindPlayableTrack(
		int startIndex,
		int direction,
		out int trackIndex)
	{
		trackIndex = -1;
		if (Tracks == null || Tracks.Length == 0)
			return false;

		int step = direction < 0 ? -1 : 1;
		int normalizedStart = NormalizeTrackIndex(startIndex);
		for (int offset = 0; offset < Tracks.Length; offset++)
		{
			int candidateIndex = NormalizeTrackIndex(
				normalizedStart + (offset * step));
			if (Tracks[candidateIndex] == null)
				continue;

			trackIndex = candidateIndex;
			return true;
		}

		return false;
	}

	private int NormalizeTrackIndex(int index)
	{
		int normalizedIndex = index % Tracks.Length;
		return normalizedIndex < 0
			? normalizedIndex + Tracks.Length
			: normalizedIndex;
	}

	private string GetTrackTitle(int trackIndex)
	{
		if (TrackTitles != null &&
			trackIndex < TrackTitles.Length &&
			!string.IsNullOrWhiteSpace(TrackTitles[trackIndex]))
		{
			return TrackTitles[trackIndex].ToUpperInvariant();
		}

		return $"WALDKLANG {trackIndex + 1}";
	}

	private string GetTrackArtist(int trackIndex)
	{
		if (TrackArtists != null &&
			trackIndex < TrackArtists.Length &&
			!string.IsNullOrWhiteSpace(TrackArtists[trackIndex]))
		{
			return TrackArtists[trackIndex];
		}

		return "ECOSYSTEM-SOUNDTRACK";
	}

	private Texture2D GetTrackArtwork(int trackIndex)
	{
		if (TrackArtwork == null || trackIndex >= TrackArtwork.Length)
			return null;

		return TrackArtwork[trackIndex];
	}

	private void EnsureMusicBus()
	{
		_musicBusIndex = AudioServer.GetBusIndex(MusicBusName);
		if (_musicBusIndex >= 0)
			return;

		AudioServer.AddBus();
		_musicBusIndex = AudioServer.BusCount - 1;
		AudioServer.SetBusName(_musicBusIndex, MusicBusName);
		AudioServer.SetBusSend(_musicBusIndex, "Master");
	}

	private static string FormatTime(float seconds)
	{
		int totalSeconds = Mathf.Max(Mathf.FloorToInt(seconds), 0);
		return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
	}
}
