using Godot;

public partial class WaterDisplayUI : Control
{
	[Export(PropertyHint.Range, "1,50,1")]
	public int SideBarMaximum = 20;

	private Label _productionValue;
	private Label _carryValue;
	private Label _consumptionValue;
	private ProgressBar _productionProgress;
	private ProgressBar _waterProgress;
	private ProgressBar _consumptionProgress;

	public override void _Ready()
	{
		_productionValue = GetNodeOrNull<Label>("WaterProductionValue");
		_carryValue = GetNodeOrNull<Label>("WaterCarryValue");
		_consumptionValue = GetNodeOrNull<Label>("WaterConsumptionValue");
		_productionProgress = GetNodeOrNull<ProgressBar>(
			"MarginContainer/BarMask/Control/HBoxContainer/" +
			"WaterProduction/WaterProductionProgress");
		_waterProgress = GetNodeOrNull<ProgressBar>(
			"MarginContainer/BarMask/Control/HBoxContainer/" +
			"Tree/TreeProgress");
		_consumptionProgress = GetNodeOrNull<ProgressBar>(
			"MarginContainer/BarMask/Control/HBoxContainer/" +
			"WaterExpenditure/WaterExpenditureProgress");

		UpdateDisplay(
			transfer: 0,
			waterProgress: 0,
			targetWater: 50,
			production: 0,
			consumption: 0,
			updateWaterProgress: true);
	}

	public void ShowCurrentState(int currentWater, int targetWater)
	{
		UpdateDisplay(
			transfer: 0,
			waterProgress: currentWater,
			targetWater: targetWater,
			production: 0,
			consumption: 0,
			updateWaterProgress: true);
	}

	public void ShowPreview(
		int transfer,
		int targetWater,
		int production,
		int consumption)
	{
		UpdateDisplay(
			transfer: transfer,
			waterProgress: 0,
			targetWater: targetWater,
			production: production,
			consumption: consumption,
			updateWaterProgress: false);
	}

	public void ShowWaterResult(WaterPhaseResult result, int targetWater)
	{
		if (result == null)
			return;

		int production =
			result.PlantWaterProduction +
			Mathf.Max(result.EventWaterModifier, 0);
		int consumption =
			result.PlantWaterConsumption +
			Mathf.Max(-result.EventWaterModifier, 0);

		UpdateDisplay(
			transfer: result.NetChange,
			waterProgress: result.EndingWater,
			targetWater: targetWater,
			production: production,
			consumption: consumption,
			updateWaterProgress: true);
	}

	private void UpdateDisplay(
		int transfer,
		int waterProgress,
		int targetWater,
		int production,
		int consumption,
		bool updateWaterProgress)
	{
		int safeTarget = Mathf.Max(targetWater, 1);
		int safeProduction = Mathf.Max(production, 0);
		int safeConsumption = Mathf.Max(consumption, 0);

		if (_productionValue != null)
			_productionValue.Text = safeProduction.ToString();

		if (_carryValue != null)
			_carryValue.Text = FormatSignedValue(transfer);

		if (_consumptionValue != null)
			_consumptionValue.Text = safeConsumption.ToString();

		SetProgress(
			_productionProgress,
			safeProduction,
			Mathf.Max(SideBarMaximum, safeProduction));
		if (updateWaterProgress)
		{
			SetProgress(
				_waterProgress,
				Mathf.Clamp(waterProgress, 0, safeTarget),
				safeTarget);
		}
		SetProgress(
			_consumptionProgress,
			safeConsumption,
			Mathf.Max(SideBarMaximum, safeConsumption));
	}

	private static string FormatSignedValue(int value)
	{
		return value > 0 ? $"+{value}" : value.ToString();
	}

	private static void SetProgress(
		ProgressBar progressBar,
		int value,
		int maximum)
	{
		if (progressBar == null)
			return;

		progressBar.MinValue = 0;
		progressBar.MaxValue = Mathf.Max(maximum, 1);
		progressBar.Value = Mathf.Clamp(value, 0, maximum);
	}
}
