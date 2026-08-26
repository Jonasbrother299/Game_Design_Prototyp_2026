using Godot;
using System;
using System.Collections.Generic;

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

	[ExportGroup("Layout")]
	[Export] public Vector2 ExpandedSize = new(680.0f, 500.0f);
	[Export] public Vector2 CollapsedSize = new(460.0f, 94.0f);
	[Export] public bool StartCollapsed;

	[ExportGroup("Retro Animation")]
	[Export(PropertyHint.Range, "0.0,90.0,1.0")]
	public float RecordRotationDegreesPerSecond = 22.0f;

	[Export(PropertyHint.Range, "1.0,8.0,0.5")]
	public float ButtonPressDepth = 5.0f;

	[Export(PropertyHint.Range, "0.03,0.2,0.01")]
	public float ButtonPressDuration = 0.07f;

	private AudioStreamPlayer _audioPlayer;
	private Control _content;
	private Control _collapsedContent;
	private Label _trackTitle;
	private Label _trackArtist;
	private Label _collapsedTrackTitle;
	private Button _previousButton;
	private Button _playPauseButton;
	private Button _stopButton;
	private Button _nextButton;
	private Button _muteButton;
	private Button _collapseButton;
	private Button _expandButton;
	private ProgressBar _progressBar;
	private Label _elapsedTimeLabel;
	private Label _durationLabel;
	private TextureRect _recordDisc;
	private Control _playbackLamp;
	private Button[] _animatedButtons = Array.Empty<Button>();
	private readonly Dictionary<Button, Vector2> _buttonRestPositions = new();
	private readonly Dictionary<Button, Tween> _buttonTweens = new();
	private readonly Dictionary<Button, Action> _buttonDownHandlers = new();
	private readonly Dictionary<Button, Action> _buttonUpHandlers = new();
	private int _currentTrackIndex;
	private int _musicBusIndex = -1;
	private bool _isCollapsed;
	private bool _retroPartsPrepared;

	public override void _Ready()
	{
		_audioPlayer = GetNode<AudioStreamPlayer>("%AudioPlayer");
		_content = GetNode<Control>("%Content");
		_collapsedContent = GetNode<Control>("%CollapsedContent");
		_trackTitle = GetNode<Label>("%TrackTitle");
		_trackArtist = GetNode<Label>("%TrackArtist");
		_collapsedTrackTitle = GetNode<Label>("%CollapsedTrackTitle");
		_previousButton = GetNode<Button>("%PreviousButton");
		_playPauseButton = GetNode<Button>("%PlayPauseButton");
		_stopButton = GetNode<Button>("%StopButton");
		_nextButton = GetNode<Button>("%NextButton");
		_muteButton = GetNode<Button>("%MuteButton");
		_collapseButton = GetNode<Button>("%CollapseButton");
		_expandButton = GetNode<Button>("%ExpandButton");
		_progressBar = GetNode<ProgressBar>("%TrackProgress");
		_elapsedTimeLabel = GetNode<Label>("%ElapsedTimeLabel");
		_durationLabel = GetNode<Label>("%DurationLabel");
		_recordDisc = GetNode<TextureRect>("%RecordDisc");
		_playbackLamp = GetNode<Control>("%PlaybackLamp");
		_animatedButtons = new[]
		{
			_previousButton,
			_playPauseButton,
			_stopButton,
			_nextButton,
			_muteButton,
			_collapseButton,
			_expandButton
		};

		EnsureMusicBus();
		_previousButton.Pressed += PlayPreviousTrack;
		_playPauseButton.Pressed += TogglePlayback;
		_stopButton.Pressed += StopPlayback;
		_nextButton.Pressed += PlayNextTrack;
		_muteButton.Pressed += ToggleMute;
		_collapseButton.Pressed += CollapsePlayer;
		_expandButton.Pressed += ExpandPlayer;
		_audioPlayer.Finished += PlayNextTrack;
		foreach (Button button in _animatedButtons)
			BindButtonAnimation(button);

		SetCollapsed(StartCollapsed);
		UpdatePlayerState();
		if (TryFindPlayableTrack(_currentTrackIndex, 1, out int firstTrackIndex))
			PlayTrack(firstTrackIndex);
	}

	public override void _ExitTree()
	{
		if (_previousButton != null)
			_previousButton.Pressed -= PlayPreviousTrack;
		if (_playPauseButton != null)
			_playPauseButton.Pressed -= TogglePlayback;
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

		foreach (Button button in _animatedButtons)
		{
			if (_buttonDownHandlers.TryGetValue(button, out Action downHandler))
				button.ButtonDown -= downHandler;
			if (_buttonUpHandlers.TryGetValue(button, out Action upHandler))
				button.ButtonUp -= upHandler;
		}

		foreach (Tween tween in _buttonTweens.Values)
		{
			if (tween != null && tween.IsValid())
				tween.Kill();
		}
	}

	public override void _Process(double delta)
	{
		PrepareRetroParts();
		AnimateRetroMechanics((float)delta);
		UpdateProgress();
		UpdatePlaybackButton();
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

		_audioPlayer.StreamPaused = false;
		_audioPlayer.Stop();
		UpdateProgress();
		UpdatePlaybackButton();
	}

	private void TogglePlayback()
	{
		if (_audioPlayer?.Stream == null)
			return;

		if (_audioPlayer.StreamPaused)
		{
			_audioPlayer.StreamPaused = false;
		}
		else if (_audioPlayer.Playing)
		{
			_audioPlayer.StreamPaused = true;
		}
		else
		{
			_audioPlayer.StreamPaused = false;
			_audioPlayer.Play();
		}

		UpdatePlaybackButton();
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
		_audioPlayer.StreamPaused = false;
		_audioPlayer.Play();
		UpdatePlayerState();
	}

	private void UpdatePlayerState()
	{
		bool hasTracks = HasPlayableTracks();
		_previousButton.Disabled = !hasTracks;
		_playPauseButton.Disabled = !hasTracks;
		_stopButton.Disabled = !hasTracks;
		_nextButton.Disabled = !hasTracks;

		if (!hasTracks)
		{
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
		UpdateProgress();
		UpdateMuteButton();
	}

	private void UpdatePlaybackButton()
	{
		bool isActivelyPlaying =
			_audioPlayer?.Playing == true && !_audioPlayer.StreamPaused;
		_playPauseButton.Text = isActivelyPlaying ? "Ⅱ" : "▶";
		_playPauseButton.TooltipText = isActivelyPlaying
			? "Musik pausieren"
			: "Musik abspielen";
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
			? "KLANG AUS"
			: "KLANG AN";
		_muteButton.TooltipText = isMuted
			? "Musik einschalten"
			: "Musik stummschalten";
	}

	private void PrepareRetroParts()
	{
		if (_retroPartsPrepared || _recordDisc.Size.X <= 0.0f)
			return;

		_recordDisc.PivotOffset = _recordDisc.Size * 0.5f;
		foreach (Button button in _animatedButtons)
		{
			button.PivotOffset = button.Size * 0.5f;
			_buttonRestPositions[button] = button.Position;
		}

		_retroPartsPrepared = true;
	}

	private void AnimateRetroMechanics(float delta)
	{
		bool isPlaying =
			_audioPlayer?.Playing == true && !_audioPlayer.StreamPaused;
		if (isPlaying)
		{
			_recordDisc.Rotation = Mathf.PosMod(
				_recordDisc.Rotation +
				Mathf.DegToRad(RecordRotationDegreesPerSecond) * delta,
				Mathf.Tau);
		}

		_playbackLamp.SelfModulate = isPlaying
			? new Color(0.82f, 0.32f, 0.15f, 1.0f)
			: new Color(0.28f, 0.16f, 0.09f, 0.72f);
	}

	private void BindButtonAnimation(Button button)
	{
		Action downHandler = () => AnimateButtonPress(button, pressed: true);
		Action upHandler = () => AnimateButtonPress(button, pressed: false);
		_buttonDownHandlers[button] = downHandler;
		_buttonUpHandlers[button] = upHandler;
		button.ButtonDown += downHandler;
		button.ButtonUp += upHandler;
	}

	private void AnimateButtonPress(Button button, bool pressed)
	{
		button.PivotOffset = button.Size * 0.5f;
		if (!_buttonRestPositions.TryGetValue(button, out Vector2 restPosition))
		{
			restPosition = button.Position;
			_buttonRestPositions[button] = restPosition;
		}

		if (_buttonTweens.TryGetValue(button, out Tween previousTween) &&
			previousTween != null && previousTween.IsValid())
		{
			previousTween.Kill();
		}

		float duration = Mathf.Max(ButtonPressDuration, 0.01f);
		Tween tween = CreateTween()
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(pressed ? Tween.EaseType.Out : Tween.EaseType.InOut)
			.SetParallel(true);
		tween.TweenProperty(
			button,
			"position",
			pressed
				? restPosition + new Vector2(0.0f, ButtonPressDepth)
				: restPosition,
			duration);
		tween.TweenProperty(
			button,
			"scale",
			pressed ? new Vector2(0.98f, 0.90f) : Vector2.One,
			duration);
		tween.TweenProperty(
			button,
			"self_modulate",
			pressed ? new Color(0.82f, 0.72f, 0.56f, 1.0f) : Colors.White,
			duration);
		_buttonTweens[button] = tween;
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
