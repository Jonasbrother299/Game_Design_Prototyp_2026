using Godot;
using System.Collections.Generic;

public partial class WaterDisplayUI : Control
{
	[Export] public int WaterFlowCap { get; set; } = 80;

	private const float ReservoirTop = 790.0f / 1656.0f;
	private const float ReservoirBottom = 1439.0f / 1656.0f;
	private static readonly Color NeutralColor = new(0.435f, 0.263f, 0.176f, 1.0f);
	private static readonly Color ProductionColor = new(0.439f, 0.694f, 0.757f, 1.0f);
	private static readonly Color ProductionPreviewColor = new(0.678f, 0.824f, 0.863f, 1.0f);
	private static readonly Color ConsumptionColor = new(0.773f, 0.294f, 0.329f, 1.0f);

	private Label _carryValue;
	private ColorRect _waterFill;
	private ColorRect _waterPreview;
	private Line2D _productionArc;
	private Line2D _consumptionArc;
	private Vector2[] _productionArcPoints;
	private Vector2[] _consumptionArcPoints;
	private int _currentWater;

	public override void _Ready()
	{
		_carryValue = GetNode<Label>("WaterCarryValue");
		_waterFill = GetNode<ColorRect>("WaterMask/WaterFill");
		_waterPreview = GetNode<ColorRect>("WaterMask/WaterPreview");
		_productionArc = GetNode<Line2D>("ProductionArc");
		_consumptionArc = GetNode<Line2D>("ConsumptionArc");
		_productionArcPoints = _productionArc.Points;
		_consumptionArcPoints = _consumptionArc.Points;
		ShowCurrentState(0, 50);
	}

	public void ShowCurrentState(int currentWater, int targetWater)
	{
		UpdateDisplay(0, currentWater, targetWater, updateWaterProgress: true);
		UpdateWaterArcs(0, 0);
	}

	public void ShowPreview(
		int transfer,
		int targetWater,
		int production,
		int consumption)
	{
		UpdateDisplay(transfer, _currentWater, targetWater, updateWaterProgress: false);
		UpdateWaterArcs(production, consumption);
	}

	public void ShowWaterResult(WaterPhaseResult result, int targetWater)
	{
		if (result == null)
			return;

		UpdateDisplay(result.NetChange, result.EndingWater, targetWater,
			updateWaterProgress: true);
		UpdateWaterArcs(
			result.PlantWaterProduction + Mathf.Max(result.EventWaterModifier, 0),
			result.PlantWaterConsumption + Mathf.Max(-result.EventWaterModifier, 0));
	}

	private void UpdateWaterArcs(int production, int consumption)
	{
		SetArcFill(_productionArc, _productionArcPoints, production, fromEnd: true);
		SetArcFill(_consumptionArc, _consumptionArcPoints, consumption, fromEnd: false);
	}

	private void SetArcFill(Line2D arc, Vector2[] fullPoints, int amount, bool fromEnd)
	{
		float fraction = Mathf.Clamp(amount / (float)Mathf.Max(WaterFlowCap, 1), 0.0f, 1.0f);
		arc.Visible = fraction > 0.0f && fullPoints.Length >= 2;
		if (!arc.Visible)
			return;

		float fullLength = 0.0f;
		for (int i = 1; i < fullPoints.Length; i++)
			fullLength += fullPoints[i - 1].DistanceTo(fullPoints[i]);

		float remainingLength = fullLength * fraction;
		int direction = fromEnd ? -1 : 1;
		int start = fromEnd ? fullPoints.Length - 1 : 0;
		List<Vector2> visiblePoints = new() { fullPoints[start] };
		for (int i = start + direction; i >= 0 && i < fullPoints.Length; i += direction)
		{
			Vector2 previous = fullPoints[i - direction];
			float segmentLength = previous.DistanceTo(fullPoints[i]);
			if (segmentLength > remainingLength)
			{
				visiblePoints.Add(previous.Lerp(fullPoints[i], remainingLength / segmentLength));
				break;
			}

			visiblePoints.Add(fullPoints[i]);
			remainingLength -= segmentLength;
		}

		if (fromEnd)
			visiblePoints.Reverse();
		arc.Points = visiblePoints.ToArray();
	}

	private void UpdateDisplay(
		int transfer,
		int waterProgress,
		int targetWater,
		bool updateWaterProgress)
	{
		int safeTarget = Mathf.Max(targetWater, 1);
		if (updateWaterProgress)
			_currentWater = Mathf.Clamp(waterProgress, 0, safeTarget);

		int displayedWater = Mathf.Clamp(_currentWater, 0, safeTarget);
		Color balanceColor = transfer > 0
			? ProductionColor
			: transfer < 0 ? ConsumptionColor : NeutralColor;
		_carryValue.Text = transfer >= 0 ? $"+{transfer}" : transfer.ToString();
		_carryValue.AddThemeColorOverride("font_color", balanceColor);

		SetWaterRange(_waterFill, 0, displayedWater, safeTarget);
		_waterFill.Visible = displayedWater > 0;

		int previewWater = Mathf.Clamp(displayedWater + transfer, 0, safeTarget);
		_waterPreview.Visible = !updateWaterProgress && previewWater != displayedWater;
		_waterPreview.Color = transfer > 0 ? ProductionPreviewColor : ConsumptionColor;
		SetWaterRange(_waterPreview,
			Mathf.Min(displayedWater, previewWater),
			Mathf.Max(displayedWater, previewWater),
			safeTarget);
	}

	private static void SetWaterRange(
		ColorRect fill,
		int lowerWater,
		int upperWater,
		int targetWater)
	{
		float reservoirHeight = ReservoirBottom - ReservoirTop;
		float top = ReservoirBottom - reservoirHeight * upperWater / targetWater;
		float bottom = ReservoirBottom - reservoirHeight * lowerWater / targetWater;
		fill.SetAnchorAndOffset(Side.Top, top, 0.0f);
		fill.SetAnchorAndOffset(Side.Bottom, bottom, 0.0f);
	}
}
