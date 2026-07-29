using Godot;

public partial class GhibliBushBackdrop : Node3D
{
	[Export] public PackedScene BushScene;

	[ExportGroup("Distribution")]
	[Export(PropertyHint.Range, "1,40,1")]
	public int BushCount = 20;

	[Export] public Vector3 LineStart =
		new Vector3(-8.7f, -0.72f, -0.58f);
	[Export] public Vector3 LineEnd =
		new Vector3(-0.18f, -0.72f, -9.11f);

	[Export(PropertyHint.Range, "1,3,1")]
	public int RowCount = 2;

	[Export(PropertyHint.Range, "0.0,4.0,0.05")]
	public float SecondRowDepth = 0.65f;

	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	public float PositionJitter = 0.08f;

	[Export(PropertyHint.Range, "0.0,0.5,0.01")]
	public float HeightJitter = 0.06f;

	[ExportGroup("Inset Fill")]
	[Export] public bool AddInsetFill = true;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float InsetProgress = 0.5f;

	[Export(PropertyHint.Range, "0.0,2.0,0.05")]
	public float InsetTowardBoard = 0.55f;

	[Export(PropertyHint.Range, "0.0,2.0,0.05")]
	public float InsetRearDepth = 0.4f;

	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	public float InsetStagger = 0.3f;

	[Export(PropertyHint.Range, "-0.5,0.5,0.01")]
	public float InsetHeightOffset = -0.04f;

	[ExportGroup("Bush Variation")]
	[Export(PropertyHint.Range, "0.05,0.5,0.01")]
	public float MinimumScale = 0.27f;

	[Export(PropertyHint.Range, "0.05,0.5,0.01")]
	public float MaximumScale = 0.40f;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float ColorVariationStrength = 0.9f;

	[Export(PropertyHint.Range, "0.0,0.35,0.01")]
	public float HorizontalShapeVariation = 0.18f;

	[Export(PropertyHint.Range, "0.0,0.35,0.01")]
	public float VerticalShapeVariation = 0.12f;

	[Export] public int RandomSeed = 1847;

	public override void _Ready()
	{
		CreateBackdrop();
	}

	private void CreateBackdrop()
	{
		if (BushScene == null)
		{
			GD.PushWarning("GhibliBushBackdrop: BushScene fehlt.");
			return;
		}

		int count = Mathf.Max(BushCount, 1);
		int rowCount = Mathf.Clamp(RowCount, 1, count);
		int bushesPerRow = Mathf.CeilToInt(count / (float)rowCount);
		float lineLength = LineStart.DistanceTo(LineEnd);
		Vector3 lineDirection = lineLength > 0.001f
			? (LineEnd - LineStart) / lineLength
			: Vector3.Right;
		Vector3 backdropDirection =
			new Vector3(lineDirection.Z, 0.0f, -lineDirection.X);
		float bushSpacing = bushesPerRow <= 1
			? 0.0f
			: lineLength / (bushesPerRow - 1);
		RandomNumberGenerator random = new RandomNumberGenerator
		{
			Seed = (ulong)Mathf.Max(RandomSeed, 1)
		};

		for (int index = 0; index < count; index++)
		{
			int row = index % rowCount;
			int rowIndex = index / rowCount;
			float progress = bushesPerRow <= 1
				? 0.5f
				: rowIndex / (float)(bushesPerRow - 1);

			Vector3 position = LineStart.Lerp(LineEnd, progress);
			float centeredRow = row - (rowCount - 1) * 0.5f;
			position += lineDirection *
				centeredRow * bushSpacing * 0.5f;
			position += backdropDirection * row * SecondRowDepth;
			position += backdropDirection *
				random.RandfRange(-PositionJitter, PositionJitter);
			position.Y += random.RandfRange(-HeightJitter, HeightJitter);

			CreateBush(
				$"BackdropBush_{index + 1:00}",
				position,
				random);
		}

		if (!AddInsetFill)
			return;

		Vector3 insetBase = LineStart.Lerp(
			LineEnd,
			Mathf.Clamp(InsetProgress, 0.0f, 1.0f));
		insetBase.Y += InsetHeightOffset;

		CreateBush(
			"InsetBush_Front",
			insetBase -
				backdropDirection * InsetTowardBoard -
				lineDirection * InsetStagger,
			random);
		CreateBush(
			"InsetBush_Rear",
			insetBase +
				backdropDirection * InsetRearDepth +
				lineDirection * InsetStagger,
			random);
	}

	private void CreateBush(
		string bushName,
		Vector3 position,
		RandomNumberGenerator random)
	{
		GhibliBush bush = BushScene.Instantiate<GhibliBush>();
		if (bush == null)
			return;

		bush.Name = bushName;
		bush.Position = position;
		bush.Rotation = new Vector3(
			0.0f,
			random.RandfRange(-0.55f, 0.55f),
			0.0f);
		bush.VisualScale = random.RandfRange(
			Mathf.Min(MinimumScale, MaximumScale),
			Mathf.Max(MinimumScale, MaximumScale));
		bush.ColorVariation = random.RandfRange(
			-ColorVariationStrength,
			ColorVariationStrength);
		bush.Scale = new Vector3(
			1.0f + random.RandfRange(
				-HorizontalShapeVariation,
				HorizontalShapeVariation),
			1.0f + random.RandfRange(
				-VerticalShapeVariation,
				VerticalShapeVariation),
			1.0f + random.RandfRange(
				-HorizontalShapeVariation,
				HorizontalShapeVariation));

		AddChild(bush);
	}
}
