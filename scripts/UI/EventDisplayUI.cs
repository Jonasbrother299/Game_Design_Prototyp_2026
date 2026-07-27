using Godot;
using System.Collections.Generic;

public partial class EventDisplayUI : PanelContainer
{
	private Label _titleLabel;
	private Label _descriptionLabel;
	private Label _resultLabel;

	public override void _Ready()
	{
		_titleLabel = GetNodeOrNull<Label>("Margin/VBox/Title");
		_descriptionLabel = GetNodeOrNull<Label>("Margin/VBox/Description");
		_resultLabel = GetNodeOrNull<Label>("Margin/VBox/Result");
		Visible = false;
	}

	public void ShowActivated(EventDefinition definition)
	{
		if (definition == null)
			return;

		SetText(
			definition.DisplayName,
			definition.Description,
			$"Aktiv ab der nächsten Runde · Dauer: {definition.DurationRounds}");
	}

	public void ShowWaterResult(WaterPhaseResult result)
	{
		if (result == null || result.EventWaterModifier == 0 || _resultLabel == null)
			return;

		string sign = result.EventWaterModifier > 0 ? "+" : "";
		_resultLabel.Text =
			$"Wettereinfluss: {sign}{result.EventWaterModifier} Wasser";
		Visible = true;
	}

	public void ShowPhaseResult(EventPhaseResult result)
	{
		if (result == null)
			return;

		if (result.ActivatedEvent.HasValue)
		{
			EventDefinition activated = EventDatabase.Get(result.ActivatedEvent.Value);
			ShowActivated(activated);

			if (result.PlantDeaths.Count > 0 && _resultLabel != null)
			{
				_resultLabel.Text =
					$"{FormatPlantDeaths(result.PlantDeaths)} · " +
					$"Danach: {activated.DisplayName}";
			}

			return;
		}

		if (result.PlantDeaths.Count > 0)
		{
			GameEventType cause = result.PlantDeaths[0].Cause;
			EventDefinition definition = EventDatabase.Get(cause);

			SetText(
				definition?.DisplayName ?? "Ereignis",
				definition?.Description ?? "",
				FormatPlantDeaths(result.PlantDeaths));
			return;
		}

		if (result.ActiveEvents.Count > 0)
		{
			ShowActivated(EventDatabase.Get(result.ActiveEvents[0]));
			return;
		}

		if (result.FinishedEvents.Count > 0)
		{
			EventDefinition finished = EventDatabase.Get(result.FinishedEvents[0]);
			SetText(
				finished?.DisplayName ?? "Ereignis",
				"",
				"Ereignis beendet");
			return;
		}

		Visible = false;
	}

	private void SetText(string title, string description, string result)
	{
		if (_titleLabel != null)
			_titleLabel.Text = title;

		if (_descriptionLabel != null)
			_descriptionLabel.Text = description;

		if (_resultLabel != null)
			_resultLabel.Text = result;

		Visible = true;
	}

	private static string FormatPlantDeaths(
		IReadOnlyList<PlantDeathResult> deaths)
	{
		int blockedRounds = deaths[0].BlockedRounds;
		string roundText = blockedRounds == 1 ? "Runde" : "Runden";

		return deaths.Count == 1
			? $"1 Pflanze ist gestorben. Das Feld bleibt {blockedRounds} {roundText} blockiert."
			: $"{deaths.Count} Pflanzen sind gestorben. Die Felder bleiben {blockedRounds} {roundText} blockiert.";
	}
}
