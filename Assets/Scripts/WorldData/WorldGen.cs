using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Sim;

public static class WorldGen {

	private static float GetPerlinMinMax(World world, FastNoise noise, int x, int y, float frequency, float hash, float min, float max)
	{
		return (noise.GetPerlin((float)x / world.Size * frequency + hash, (float)y / world.Size * frequency) + 1.0f) * (max - min) / 2 + min;
	}
	private static float GetPerlinNormalized(World world, FastNoise noise, int x, int y, float frequency, float hash)
	{
		return (noise.GetPerlin((float)x / world.Size * frequency + hash, (float)y / world.Size * frequency) + 1.0f) / 2;
	}
	private static float GetPerlin(World world, FastNoise noise, int x, int y, float frequency, float hash)
	{
		return noise.GetPerlin((float)x / world.Size * frequency + hash, (float)y / world.Size * frequency);
	}
	public static void Generate(World world, List<Sprite> speciesSprites, WorldData data, WorldGenData worldGenData)
	{
		world.Data = data;
		world.Init(worldGenData.Size, world.Data);

		ref var state = ref world.States[0];
		FastNoise noise = new FastNoise(67687);
		noise.SetFrequency(10);
		state.SeaLevel = 0;
		int numPlates = 12;

		int numSpecies = 4;
		world.SpeciesDisplay[0].Name = "Hot Herb";
		world.SpeciesDisplay[0].Color = new Color(100, 60, 20);
		world.SpeciesDisplay[0].Sprite = speciesSprites[0];
		state.Species[0].Food = SpeciesType.FoodType.Herbivore;
		state.Species[0].Lifespan = 10 * data.TicksPerYear;
		state.Species[0].MovementSpeed = 0.1f / data.tileSize * data.SecondsPerTick;
		state.Species[0].speciesMaxPopulation = 10000;
		state.Species[0].RestingTemperature = data.FreezingTemperature + 50;
		state.Species[0].TemperatureRange = 5000;
		state.Species[0].speciesGrowthRate = 1.0f / data.TicksPerYear;
		state.Species[0].speciesEatRate = 1.0f / data.TicksPerYear / 10000;
		state.Species[0].starvationSpeed = 12.0f / data.TicksPerYear;
		state.Species[0].dehydrationSpeed = 12.0f / data.TicksPerYear;

		world.SpeciesDisplay[1].Name = "Basic Beast";
		world.SpeciesDisplay[1].Color = new Color(120, 100, 20);
		world.SpeciesDisplay[1].Sprite = speciesSprites[0];
		state.Species[1].Food = SpeciesType.FoodType.Herbivore;
		state.Species[1].RestingTemperature = data.FreezingTemperature + 35;
		state.Species[1].Lifespan = 20 * data.TicksPerYear;
		state.Species[1].MovementSpeed = 0.1f / data.tileSize * data.SecondsPerTick;
		state.Species[1].speciesMaxPopulation = 10000;
		state.Species[1].TemperatureRange = 3000;
		state.Species[1].speciesGrowthRate = 1.0f / data.TicksPerYear;
		state.Species[1].speciesEatRate = 1.0f / data.TicksPerYear / 10000;
		state.Species[1].starvationSpeed = 12.0f / data.TicksPerYear;
		state.Species[1].dehydrationSpeed = 12.0f / data.TicksPerYear;

		world.SpeciesDisplay[2].Name = "Supacold";
		world.SpeciesDisplay[2].Color = new Color(60, 20, 100);
		world.SpeciesDisplay[2].Sprite = speciesSprites[0];
		state.Species[2].Food = SpeciesType.FoodType.Herbivore;
		state.Species[2].Lifespan = 15 * data.TicksPerYear;
		state.Species[2].MovementSpeed = 0.1f / data.tileSize * data.SecondsPerTick;
		state.Species[2].speciesMaxPopulation = 10000;
		state.Species[2].RestingTemperature = data.FreezingTemperature + 20;
		state.Species[2].TemperatureRange = 3000;
		state.Species[2].speciesGrowthRate = 1.0f / data.TicksPerYear;
		state.Species[2].speciesEatRate = 1.0f / data.TicksPerYear / 10000;
		state.Species[2].starvationSpeed = 12.0f / data.TicksPerYear;
		state.Species[2].dehydrationSpeed = 12.0f / data.TicksPerYear;

		world.SpeciesDisplay[3].Name = "Eatasaurus";
		world.SpeciesDisplay[3].Color = new Color(255, 0, 50);
		world.SpeciesDisplay[3].Sprite = speciesSprites[0];
		state.Species[3].Food = SpeciesType.FoodType.Carnivore;
		state.Species[3].RestingTemperature = data.FreezingTemperature + 30;
		state.Species[3].Lifespan = 15 * data.TicksPerYear;
		state.Species[3].MovementSpeed = 0.1f / data.tileSize * data.SecondsPerTick;
		state.Species[3].speciesMaxPopulation = 10000;
		state.Species[3].TemperatureRange = 4000;
		state.Species[3].speciesGrowthRate = 1.0f / data.TicksPerYear;
		state.Species[3].speciesEatRate = 1.0f / data.TicksPerYear;
		state.Species[3].starvationSpeed = 12.0f / data.TicksPerYear;
		state.Species[3].dehydrationSpeed = 12.0f / data.TicksPerYear;
		int animalCount = 0;

		for (int y = 0; y < world.Size; y++)
		{
			for (int x = 0; x < world.Size; x++)
			{
				int index = world.GetIndex(x, y);
				var e =
					GetPerlinMinMax(world, noise, x, y, 0.25f, 0, worldGenData.MinElevation, worldGenData.MaxElevation) * 0.4f +
					GetPerlinMinMax(world, noise, x, y, 0.5f, 10, worldGenData.MinElevation, worldGenData.MaxElevation) * 0.3f +
					GetPerlinMinMax(world, noise, x, y, 1.0f, 20, worldGenData.MinElevation, worldGenData.MaxElevation) * 0.2f +
					GetPerlinMinMax(world, noise, x, y, 2.0f, 30, worldGenData.MinElevation, worldGenData.MaxElevation) * 0.1f;
				state.Elevation[index] = e;
				float latitude = world.GetLatitude(y);

				float elevationOrSeaLevel = Math.Max(0, e);
				float troposphereColumnHeight = data.troposphereElevation - elevationOrSeaLevel;
				float troposphereMass = worldGenData.TroposphereMass * troposphereColumnHeight / data.troposphereElevation;
				float upperAirColumnHeight = troposphereColumnHeight - data.BoundaryZoneElevation;
				float upperAirVolume = data.troposphereElevation - elevationOrSeaLevel - data.BoundaryZoneElevation;
				float lowerAirVolume = data.BoundaryZoneElevation;

				state.LowerAirTemperature[index] = (1.0f - Mathf.Clamp(e - state.SeaLevel, 0, worldGenData.MaxElevation) / (worldGenData.MaxElevation - state.SeaLevel)) * (1.0f - latitude * latitude) * (worldGenData.MaxTemperature - worldGenData.MinTemperature) + worldGenData.MinTemperature;
				state.UpperAirTemperature[index] = state.LowerAirTemperature[index] + data.temperatureLapseRate * (data.troposphereElevation - elevationOrSeaLevel);

				float lowerDensity = data.LowerAirDensity - (data.LowerAirDensity - data.UpperAirDensity) * (elevationOrSeaLevel / data.troposphereElevation);
				float upperDensity = data.UpperAirDensity;

				//upperPressure == lowerPressure;

				//upperMass + lowerMass = troposphereMass;

				//upperMass * upperTemperature * world.data.MassEarthAir / (upperDensity * upperAirVolume) ==
				//lowerMass * lowerTemperature * world.data.MassEarthAir / (lowerDensity * lowerAirVolume);

				//upperMass / lowerMass =
				//(lowerTemperature / (lowerDensity * lowerAirVolume)) / 
				//(upperTemperature / (upperDensity * upperAirVolume));

				//upperMass = troposphereMass - lowerMass;
				//upperMass / lowerMass = troposphereMass / lowerMass - 1;
				//lowerMass = troposphereMass / (upperMass / lowerMass + 1);

				//lowerMass =
				//troposphereMass / (1 +
				//(lowerTemperature / (lowerDensity * lowerAirVolume)) /
				//(upperTemperature / (upperDensity * upperAirVolume)));

				float lowerMass =
				troposphereMass / (1 +
				(state.LowerAirTemperature[index] / (lowerDensity * lowerAirVolume)) /
				(state.UpperAirTemperature[index] / (upperDensity * upperAirVolume)));

				state.SolarRadiation = worldGenData.SolarRadiation;
				state.StratosphereMass = worldGenData.StratosphereMass;
				state.CarbonDioxide = worldGenData.CarbonDioxide;
				state.PlanetTiltAngle = Mathf.Deg2Rad * worldGenData.PlanetTiltAngle;
				state.UpperAirMass[index] = troposphereMass - lowerMass;
				state.LowerAirMass[index] = lowerMass;

				state.UpperAirEnergy[index] = Atmosphere.GetAirEnergy(world, state.UpperAirTemperature[index], state.UpperAirMass[index]);
				state.LowerAirEnergy[index] = Atmosphere.GetAirEnergy(world, state.LowerAirTemperature[index], state.LowerAirMass[index]);

				state.UpperAirPressure[index] = Atmosphere.GetAirPressure(world, state.UpperAirMass[index], state.UpperAirTemperature[index], data.troposphereElevation, upperAirVolume);
				state.LowerAirPressure[index] = Atmosphere.GetAirPressure(world, state.LowerAirMass[index], state.LowerAirTemperature[index], elevationOrSeaLevel, lowerAirVolume);

				state.CloudCover[index] = GetPerlinMinMax(world, noise, x, y, 3.0f, 2000, 0, 2);
				state.Humidity[index] = GetPerlinMinMax(world, noise, x, y, 3.0f, 3000, 0, 2);
				state.CloudElevation[index] = state.Elevation[index] + 1000;
				state.WaterTableDepth[index] = GetPerlinMinMax(world, noise, x, y, 1.0f, 200, data.MinWaterTableDepth, data.MaxWaterTableDepth);
				state.SoilFertility[index] = GetPerlinNormalized(world, noise, x, y, 1.0f, 400);
				state.SurfaceIce[index] = 0;
				if (e >= 0)
				{
					state.SurfaceWater[index] = GetPerlinMinMax(world, noise, x, y, 1.0f, 100, 0, 10.0f);
					state.GroundWater[index] = GetPerlinMinMax(world, noise, x, y, 1.0f, 300, 0, state.WaterTableDepth[index] * state.SoilFertility[index] * data.MaxSoilPorousness);
					state.Canopy[index] = GetPerlinNormalized(world, noise, x, y, 2.0f, 1000);
				} else
				{
					state.OceanTemperatureShallow[index] = Math.Max(data.FreezingTemperature, state.LowerAirTemperature[index]);
					state.OceanEnergyShallow[index] = Atmosphere.GetWaterEnergy(world, state.OceanTemperatureShallow[index], data.DeepOceanDepth);
					state.OceanEnergyDeep[index] = Atmosphere.GetWaterEnergy(world, data.FreezingTemperature + 3, Math.Max(0, -e));
					state.OceanSalinityShallow[index] = (1.0f - Math.Abs(latitude)) * data.DeepOceanDepth;
					float deepOceanVolume = state.SeaLevel - state.Elevation[index];
					state.OceanSalinityDeep[index] = (Math.Abs(latitude)+1) * deepOceanVolume;
					state.OceanDensityDeep[index] = Atmosphere.GetOceanDensity(world, state.OceanEnergyDeep[index], state.OceanSalinityDeep[index], deepOceanVolume);
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
				Vector2Int position = new Vector2Int((int)((noise.GetWhiteNoiseInt(h, 2) * 0.5f + 0.5f) * world.Size), (int)((noise.GetWhiteNoiseInt(h, 3) * 0.5f + 0.5f) * world.Size));
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
			Vector2Int plateCenter = new Vector2Int((int)(world.Size * (noise.GetWhiteNoiseInt(i, 0)) / 2 + 0.5f), (int)(world.Size * (noise.GetWhiteNoiseInt(i, 1) / 2 + 0.5f)));
			float radius = (noise.GetWhiteNoiseInt(i, 2) / 2 + 0.5f) * (MaxPlateRadius - MinPlateRadius) + MinPlateRadius;
			for (int x = (int)-Math.Ceiling(radius); x < (int)Math.Ceiling(radius); x++)
			{
				for (int y = (int)-Math.Ceiling(radius); y < (int)Math.Ceiling(radius); y++)
				{
					Vector2Int pos = new Vector2Int(world.WrapX(plateCenter.x + x), world.WrapY(plateCenter.y+ y));
					Vector2 diff = new Vector2(x, y);
					if (diff.SqrMagnitude() <= radius * radius)
					{
						state.Plate[world.GetIndex(pos.x, pos.y)] = i;
					}
				}
			}
		}

		for (int i = 1; i < World.StateCount; i++)
		{
			world.States[i] = (World.State)world.States[0].Clone();
		}
	}
}
