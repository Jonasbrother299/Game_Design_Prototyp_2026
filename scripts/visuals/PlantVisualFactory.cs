using Godot;

public static class PlantVisualFactory
{
	public static Node3D CreateVisual(
		PlantInstance plant,
		HexTile tile,
		bool animateGrowth = true)
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
					animateGrowth);

			case PlantType.Moss:
				return MossVisualBuilder.Create(plant);

			case PlantType.Flower:
				return FlowerVisualBuilder.Create(plant);

			case PlantType.Birch:
				return BirchVisualBuilder.Create(plant);

			case PlantType.Oak:
				if (IsStartingOakTile(tile))
				{
					return StartingOakVisualBuilder.Create(
						plant,
						tile.StartingOakScale);
				}

				return OakVisualBuilder.Create(plant);
		}

		return null;
	}

	private static bool IsStartingOakTile(HexTile tile)
	{
		if (tile == null)
			return false;

		return tile.Name == "HexTile_0_0";
	}
}
