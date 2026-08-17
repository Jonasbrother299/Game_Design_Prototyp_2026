using Godot;

public static class PlantVisualFactory
{
	public static Node3D CreateVisual(
		PlantInstance plant,
		HexTile tile,
		bool animateGrowth = true,
		bool showTreeShadow = true)
	{
		if (plant == null)
		{
			return new Node3D();
		}

		switch (plant.Definition.Type)
		{
			case PlantType.Mushroom:
				return MushroomVisualBuilder.Create(
					plant,
					tile.MushroomModelScale,
					tile.MushroomGrowthAnimationSpeed,
					animateGrowth,
					tile);

			case PlantType.Moss:
				return MossVisualBuilder.Create(
					plant,
					tile.Coord);

			case PlantType.Flower:
				return FlowerVisualBuilder.Create(
					plant,
					tile.FlowerModelScale,
					tile.MatureFlowerCount);

			case PlantType.Birch:
				Node3D birchVisual = BirchVisualBuilder.Create(
					plant,
					tile.BirchModelScale);

				if (showTreeShadow && plant.IsMature)
				{
					return AddTreeShadow(
						birchVisual,
						tile.TreeShadowColor,
						tile.BirchShadowSize,
						tile.BirchShadowOffset);
				}

				return birchVisual;

			case PlantType.Oak:
				if (IsStartingOakTile(tile))
				{
					Node3D startingOakVisual =
						StartingOakVisualBuilder.Create(
						plant,
						tile.StartingOakScale);

					if (showTreeShadow)
					{
						return AddTreeShadow(
							startingOakVisual,
							tile.TreeShadowColor,
							tile.StartingOakShadowSize,
							tile.StartingOakShadowOffset);
					}

					return startingOakVisual;
				}

				Node3D oakVisual = OakVisualBuilder.Create(plant);

				if (showTreeShadow && plant.IsMature)
				{
					return AddTreeShadow(
						oakVisual,
						tile.TreeShadowColor,
						tile.StartingOakShadowSize,
						tile.StartingOakShadowOffset);
				}

				return oakVisual;
		}

		return null;
	}

	private static bool IsStartingOakTile(HexTile tile)
	{
		if (tile == null)
			return false;

		return tile.Name == "HexTile_0_0";
	}

	private static Node3D AddTreeShadow(
		Node3D treeVisual,
		Color shadowColor,
		float canopySize,
		Vector2 canopyOffset)
	{
		Node3D root = new Node3D
		{
			Name = $"{treeVisual.Name}_WithShadow"
		};

		root.AddChild(TreeCanopyShadowBuilder.Create(
			shadowColor,
			canopySize,
			canopyOffset));
		root.AddChild(treeVisual);

		return root;
	}
}
