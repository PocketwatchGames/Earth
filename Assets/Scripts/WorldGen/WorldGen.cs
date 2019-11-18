using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public partial class World {

	private float GetPerlinMinMax(FastNoise noise, int x, int y, float frequency, float hash, float min, float max)
	{
		return (noise.GetPerlin((float)x / Size * frequency + hash, (float)y / Size * frequency) + 1.0f) * (max - min) / 2 + min;
	}
	private float GetPerlinNormalized(FastNoise noise, int x, int y, float frequency, float hash)
	{
		return (noise.GetPerlin((float)x / Size * frequency + hash, (float)y / Size * frequency) + 1.0f) / 2;
	}
	private float GetPerlin(FastNoise noise, int x, int y, float frequency, float hash)
	{
		return noise.GetPerlin((float)x / Size * frequency + hash, (float)y / Size * frequency);
	}
	public void Generate(List<Sprite> speciesSprites)
	{
		ref var state = ref States[0];
		FastNoise noise = new FastNoise(67687);
		noise.SetFrequency(10);
		state.SeaLevel = 0;
		int numPlates = 12;

		int numSpecies = 4;
		SpeciesDisplay[0].Name = "Hot Herb";
		SpeciesDisplay[0].Color = new Color(100, 60, 20);
		SpeciesDisplay[0].Sprite = speciesSprites[0];
		state.Species[0].Food = SpeciesType.FoodType.Herbivore;
		state.Species[0].Lifespan = 10 * Data.TicksPerYear;
		state.Species[0].MovementSpeed = 0.1f / Data.tileSize * Data.SecondsPerTick;
		state.Species[0].speciesMaxPopulation = 10000;
		state.Species[0].RestingTemperature = Data.FreezingTemperature + 50;
		state.Species[0].TemperatureRange = 5000;
		state.Species[0].speciesGrowthRate = 1.0f / Data.TicksPerYear;
		state.Species[0].speciesEatRate = 1.0f / Data.TicksPerYear / 10000;
		state.Species[0].starvationSpeed = 12.0f / Data.TicksPerYear;
		state.Species[0].dehydrationSpeed = 12.0f / Data.TicksPerYear;

		SpeciesDisplay[1].Name = "Basic Beast";
		SpeciesDisplay[1].Color = new Color(120, 100, 20);
		SpeciesDisplay[1].Sprite = speciesSprites[0];
		state.Species[1].Food = SpeciesType.FoodType.Herbivore;
		state.Species[1].RestingTemperature = Data.FreezingTemperature + 35;
		state.Species[1].Lifespan = 20 * Data.TicksPerYear;
		state.Species[1].MovementSpeed = 0.1f / Data.tileSize * Data.SecondsPerTick;
		state.Species[1].speciesMaxPopulation = 10000;
		state.Species[1].TemperatureRange = 3000;
		state.Species[1].speciesGrowthRate = 1.0f / Data.TicksPerYear;
		state.Species[1].speciesEatRate = 1.0f / Data.TicksPerYear / 10000;
		state.Species[1].starvationSpeed = 12.0f / Data.TicksPerYear;
		state.Species[1].dehydrationSpeed = 12.0f / Data.TicksPerYear;

		SpeciesDisplay[2].Name = "Supacold";
		SpeciesDisplay[2].Color = new Color(60, 20, 100);
		SpeciesDisplay[2].Sprite = speciesSprites[0];
		state.Species[2].Food = SpeciesType.FoodType.Herbivore;
		state.Species[2].Lifespan = 15 * Data.TicksPerYear;
		state.Species[2].MovementSpeed = 0.1f / Data.tileSize * Data.SecondsPerTick;
		state.Species[2].speciesMaxPopulation = 10000;
		state.Species[2].RestingTemperature = Data.FreezingTemperature + 20;
		state.Species[2].TemperatureRange = 3000;
		state.Species[2].speciesGrowthRate = 1.0f / Data.TicksPerYear;
		state.Species[2].speciesEatRate = 1.0f / Data.TicksPerYear / 10000;
		state.Species[2].starvationSpeed = 12.0f / Data.TicksPerYear;
		state.Species[2].dehydrationSpeed = 12.0f / Data.TicksPerYear;

		SpeciesDisplay[3].Name = "Eatasaurus";
		SpeciesDisplay[3].Color = new Color(255, 0, 50);
		SpeciesDisplay[3].Sprite = speciesSprites[0];
		state.Species[3].Food = SpeciesType.FoodType.Carnivore;
		state.Species[3].RestingTemperature = Data.FreezingTemperature + 30;
		state.Species[3].Lifespan = 15 * Data.TicksPerYear;
		state.Species[3].MovementSpeed = 0.1f / Data.tileSize * Data.SecondsPerTick;
		state.Species[3].speciesMaxPopulation = 10000;
		state.Species[3].TemperatureRange = 4000;
		state.Species[3].speciesGrowthRate = 1.0f / Data.TicksPerYear;
		state.Species[3].speciesEatRate = 1.0f / Data.TicksPerYear;
		state.Species[3].starvationSpeed = 12.0f / Data.TicksPerYear;
		state.Species[3].dehydrationSpeed = 12.0f / Data.TicksPerYear;
		int animalCount = 0;

		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				int index = GetIndex(x, y);
				var e =
					GetPerlinMinMax(noise, x, y, 0.25f, 0, Data.MinElevation, Data.MaxElevation) * 0.4f +
					GetPerlinMinMax(noise, x, y, 0.5f, 10, Data.MinElevation, Data.MaxElevation) * 0.3f +
					GetPerlinMinMax(noise, x, y, 1.0f, 20, Data.MinElevation, Data.MaxElevation) * 0.2f +
					GetPerlinMinMax(noise, x, y, 2.0f, 30, Data.MinElevation, Data.MaxElevation) * 0.1f;
				state.Elevation[index] = e;
				float latitude = GetLatitude(y);
				state.Temperature[index] = (1.0f - Mathf.Clamp(e - state.SeaLevel, 0, Data.MaxElevation) / (Data.MaxElevation - state.SeaLevel)) * (1.0f - latitude * latitude) * (Data.MaxTemperature - Data.MinTemperature) + Data.MinTemperature;
				state.CloudCover[index] = GetPerlinMinMax(noise, x, y, 3.0f, 2000, 0, 2);
				state.Humidity[index] = GetPerlinMinMax(noise, x, y, 3.0f, 3000, 0, 2);
				state.CloudElevation[index] = state.Elevation[index] + 1000;
				state.WaterTableDepth[index] = GetPerlinMinMax(noise, x, y, 1.0f, 200, Data.MinWaterTableDepth, Data.MaxWaterTableDepth);
				state.SoilFertility[index] = GetPerlinNormalized(noise, x, y, 1.0f, 400);
				state.Pressure[index] = GetPressureAtElevation(state, index, Math.Max(state.SeaLevel, e));
				if (e >= 0)
				{
					state.SurfaceWater[index] = GetPerlinMinMax(noise, x, y, 1.0f, 100, 0, 10.0f);
					state.GroundWater[index] = GetPerlinMinMax(noise, x, y, 1.0f, 300, 0, state.WaterTableDepth[index] * state.SoilFertility[index] * Data.MaxSoilPorousness);
					state.Canopy[index] = GetPerlinNormalized(noise, x, y, 2.0f, 1000);
				} else
				{
					state.OceanTemperatureShallow[index] = (state.Temperature[index] + Data.FreezingTemperature) / 2;
					state.OceanTemperatureDeep[index] = (state.OceanTemperatureShallow[index] + Data.FreezingTemperature) / 2;
					state.OceanSalinityShallow[index] = (1.0f - Math.Abs(latitude)) * Data.DeepOceanDepth;
					float deepOceanVolume = state.SeaLevel - state.Elevation[index];
					state.OceanSalinityDeep[index] = (Math.Abs(latitude)+1) * deepOceanVolume;
					state.OceanDensityDeep[index] = GetOceanDensity(state.OceanTemperatureDeep[index], state.OceanSalinityDeep[index], deepOceanVolume);
				}
			}
		}

		int numHerds = 20;
		for (int h = 0; h < numHerds; h++)
		{
			int s = (int)(numSpecies * (noise.GetWhiteNoiseInt(h, 0) / 2 + 0.5f));
			int p = (int)((noise.GetWhiteNoiseInt(h, 1) * 0.5f + 0.5f) * state.Species[s].speciesMaxPopulation);
			if (p > 0)
			{
				Vector2Int position = new Vector2Int((int)((noise.GetWhiteNoiseInt(h, 2) * 0.5f + 0.5f) * Size), (int)((noise.GetWhiteNoiseInt(h, 3) * 0.5f + 0.5f) * Size));
				state.Herds[h] = new Herd() {
					SpeciesIndex = s,
					DesiredMutationHealth = 0.5f,
					DesiredMutationReproduction = 0.5f,
					DesiredMutationSize = 0.5f,
					MutationHealth = noise.GetWhiteNoiseInt(h, 4) * 0.5f + 0.5f,
					MutationReproduction = noise.GetWhiteNoiseInt(h, 5) * 0.5f + 0.5f,
					MutationSize = noise.GetWhiteNoiseInt(h, 6) * 0.5f + 0.5f,
					EvolutionProgress = noise.GetWhiteNoiseInt(h, 7) * 0.5f + 0.5f,
					ActiveTiles = new Vector2Int[Herd.MaxActiveTiles],
					TilePopulation = new float[Herd.MaxActiveTiles],
					DesiredTiles = new Vector2Int[Herd.MaxDesiredTiles],
					Population = p,
					Food = 0.5f,
					Water = 0.3f,
					Social = 0.1f,
					Disease = 0.05f,
					Comfort = 0.8f,

					Status = new Herd.DisplayStatus() {
						Position = new Vector2(position.x+0.5f,position.y+0.5f)
					}
				};

				state.Herds[h].ActiveTileCount = 2;
				state.Herds[h].DesiredTileCount = 2;
				state.Herds[h].ActiveTiles[0] = position;
				state.Herds[h].DesiredTiles[0] = position;
				state.Herds[h].ActiveTiles[1] = position + new Vector2Int(0,1);
				state.Herds[h].DesiredTiles[1] = position + new Vector2Int(0, 1);

			}
		}



		for (int i = 1; i < numPlates; i++)
		{
			const float MaxPlateRadius = 40;
			const float MinPlateRadius = 2;
			Vector2Int plateCenter = new Vector2Int((int)(Size * (noise.GetWhiteNoiseInt(i, 0)) / 2 + 0.5f), (int)(Size * (noise.GetWhiteNoiseInt(i, 1) / 2 + 0.5f)));
			float radius = (noise.GetWhiteNoiseInt(i, 2) / 2 + 0.5f) * (MaxPlateRadius - MinPlateRadius) + MinPlateRadius;
			for (int x = (int)-Math.Ceiling(radius); x < (int)Math.Ceiling(radius); x++)
			{
				for (int y = (int)-Math.Ceiling(radius); y < (int)Math.Ceiling(radius); y++)
				{
					Vector2Int pos = new Vector2Int(WrapX(plateCenter.x + x), WrapY(plateCenter.y+ y));
					Vector2 diff = new Vector2(x, y);
					if (diff.SqrMagnitude() <= radius * radius)
					{
						state.Plate[GetIndex(pos.x, pos.y)] = i;
					}
				}
			}
		}

		for (int i = 1; i < StateCount; i++)
		{
			States[i] = (State)States[0].Clone();
		}
	}
}
