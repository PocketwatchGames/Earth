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
	public static void Generate(World world, List<Sprite> speciesSprites, WorldData data, WorldGenData worldGenData, int seed)
	{
		world.Data = data;
		world.Init(worldGenData.Size, world.Data);

		ref var state = ref world.States[0];
		FastNoise noise = new FastNoise(seed);
		noise.SetFrequency(10);

		int numSpecies = 4;
		world.SpeciesDisplay[0].Name = "Hot Herb";
		world.SpeciesDisplay[0].Color = new Color(100, 60, 20);
		world.SpeciesDisplay[0].Sprite = speciesSprites[0];
		state.Species[0].Food = SpeciesType.FoodType.Herbivore;
		state.Species[0].Lifespan = 10 * data.TicksPerYear;
		state.Species[0].MovementSpeed = 0.1f / data.MetersPerTile * data.SecondsPerTick;
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
		state.Species[1].MovementSpeed = 0.1f / data.MetersPerTile * data.SecondsPerTick;
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
		state.Species[2].MovementSpeed = 0.1f / data.MetersPerTile * data.SecondsPerTick;
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
		state.Species[3].MovementSpeed = 0.1f / data.MetersPerTile * data.SecondsPerTick;
		state.Species[3].speciesMaxPopulation = 10000;
		state.Species[3].TemperatureRange = 4000;
		state.Species[3].speciesGrowthRate = 1.0f / data.TicksPerYear;
		state.Species[3].speciesEatRate = 1.0f / data.TicksPerYear;
		state.Species[3].starvationSpeed = 12.0f / data.TicksPerYear;
		state.Species[3].dehydrationSpeed = 12.0f / data.TicksPerYear;

		float inverseDewPointTemperatureRange = 1.0f / world.Data.DewPointTemperatureRange;

		double totalWaterVolume = 1350000000000000000;
		float waterVolumePerTile = (float)(totalWaterVolume / (world.Size * world.Size) * world.Data.InverseMetersPerTile * world.Data.InverseMetersPerTile);

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
				float depth = Mathf.Max(0, -e);
				//float depth = waterVolumePerTile;
				state.Elevation[index] = e;
				state.WaterDepth[index] = depth;
				state.WaterAndIceDepth[index] = depth;
				float latitude = world.GetLatitude(y);

				float elevationOrSeaLevel = e + state.WaterAndIceDepth[index];

				float troposphereColumnHeight = data.TropopauseElevation - elevationOrSeaLevel;
				float upperAirColumnHeight = troposphereColumnHeight - data.BoundaryZoneElevation;
				float upperAirVolume = data.TropopauseElevation - elevationOrSeaLevel - data.BoundaryZoneElevation;
				float lowerAirVolume = data.BoundaryZoneElevation;
				float upperAirElevation = elevationOrSeaLevel + data.BoundaryZoneElevation;

				float regionalTemperatureVariation =
					GetPerlinMinMax(world, noise, x, y, 0.25f, 60, -5, 5) +
					GetPerlinMinMax(world, noise, x, y, 0.5f, 60, -10, 10) +
					GetPerlinMinMax(world, noise, x, y, 2.0f, 60, -10, 10);
				state.LowerAirTemperature[index] =
					regionalTemperatureVariation + GetPerlinMinMax(world, noise, x, y, 0.25f, 80, -5, 5) +
					(1.0f - latitude * latitude) * (worldGenData.MaxTemperature - worldGenData.MinTemperature) + worldGenData.MinTemperature + data.TemperatureLapseRate * elevationOrSeaLevel;
				state.UpperAirTemperature[index] =
					GetPerlinMinMax(world, noise, x, y, 0.25f, 90, -5, 5) +
					state.LowerAirTemperature[index] + data.TemperatureLapseRate * upperAirElevation;

				state.UpperAirPressure[index] = data.StaticPressure - regionalTemperatureVariation * 1000;
				state.LowerAirPressure[index] = data.StaticPressure - regionalTemperatureVariation * 1000;

				state.PlanetRotationSpeed = worldGenData.PlanetRotationSpeed;
				state.PlanetRadius = worldGenData.PlanetRadius;
				state.SolarRadiation = worldGenData.SolarRadiation;
				state.StratosphereMass = worldGenData.StratosphereMass;
				state.CarbonDioxide = worldGenData.CarbonDioxide;
				state.PlanetTiltAngle = Mathf.Deg2Rad * worldGenData.PlanetTiltAngle;
				state.UpperAirMass[index] = Atmosphere.GetAirMass(world, state.UpperAirPressure[index], upperAirElevation, state.UpperAirTemperature[index]) - state.StratosphereMass;
				state.LowerAirMass[index] = Atmosphere.GetAirMass(world, state.LowerAirPressure[index], elevationOrSeaLevel, state.LowerAirTemperature[index]) - state.StratosphereMass - state.UpperAirMass[index];


				state.CloudMass[index] = GetPerlinMinMax(world, noise, x, y, 3.0f, 2000, 0, 1) * data.CloudMassFullAbsorption;
				float relativeHumidity = Mathf.Pow(GetPerlinNormalized(world, noise, x, y, 1.0f, 400), 3);
				state.Humidity[index] = Atmosphere.GetAbsoluteHumidity(world, state.LowerAirTemperature[index], relativeHumidity, state.LowerAirMass[index], inverseDewPointTemperatureRange);
				state.WaterTableDepth[index] = GetPerlinMinMax(world, noise, x, y, 1.0f, 200, data.MinWaterTableDepth, data.MaxWaterTableDepth);
				state.SoilFertility[index] = GetPerlinNormalized(world, noise, x, y, 1.0f, 400);
				state.IceMass[index] = 0;
				float maxGroundWater = state.WaterTableDepth[index] * state.SoilFertility[index] * world.Data.MassWater * world.Data.MaxSoilPorousness;
				if (depth > 0)
				{
					state.GroundWater[index] = maxGroundWater;
				} else
				{
					state.GroundWater[index] = maxGroundWater * 0.2f;
				}
				state.LandEnergy[index] = state.GroundWater[index] * (world.Data.SpecificHeatWater * world.Data.maxGroundWaterTemperature + world.Data.LatentHeatWaterLiquid);

				state.UpperAirEnergy[index] = Atmosphere.GetAirEnergy(world, state.UpperAirTemperature[index], state.UpperAirMass[index], state.CloudMass[index], world.Data.LatentHeatWaterLiquid, world.Data.SpecificHeatWater);
				state.LowerAirEnergy[index] = Atmosphere.GetAirEnergy(world, state.LowerAirTemperature[index], state.LowerAirMass[index], state.Humidity[index], world.Data.LatentHeatWaterVapor, world.Data.SpecificHeatWaterVapor);


				float shallowDepth = Mathf.Min(data.DeepOceanDepth, depth);
				float deepDepth = Mathf.Max(0, depth - data.DeepOceanDepth);
				float shallowSalinity = shallowDepth == 0 ? 0 : (1.0f - Math.Abs(latitude)) * (worldGenData.MaxSalinity - worldGenData.MinSalinity) + worldGenData.MinSalinity;
				float deepSalinity = deepDepth == 0 ? 0 : Math.Abs(latitude) * (worldGenData.MaxSalinity - worldGenData.MinSalinity) + worldGenData.MinSalinity;

				state.ShallowWaterTemperature[index] = state.LowerAirTemperature[index] + 2;
				float shallowOceanMass = GetWaterMass(world, shallowDepth, state.ShallowWaterTemperature[index], shallowSalinity);
				state.ShallowWaterMass[index] = shallowOceanMass * (1.0f - shallowSalinity);
				state.ShallowSaltMass[index] = shallowOceanMass * shallowSalinity;
				state.ShallowWaterEnergy[index] = Atmosphere.GetWaterEnergy(world, state.ShallowWaterTemperature[index], state.ShallowWaterMass[index], state.ShallowSaltMass[index]);
				float shallowOceanDensity = Atmosphere.GetWaterDensity(world, state.ShallowWaterEnergy[index], state.ShallowSaltMass[index], state.ShallowWaterMass[index]);

				float deepOceanTemperature = data.FreezingTemperature + 3;
				float deepOceanMass = GetWaterMass(world, deepDepth, deepOceanTemperature, deepSalinity);
				state.DeepWaterMass[index] = deepOceanMass * (1.0f - deepSalinity);
				state.DeepSaltMass[index] = deepOceanMass * deepSalinity;
				state.DeepWaterEnergy[index] = Atmosphere.GetWaterEnergy(world, deepOceanTemperature, state.DeepWaterMass[index], state.DeepSaltMass[index]);
				state.DeepWaterDensity[index] = Atmosphere.GetWaterDensity(world, state.DeepWaterEnergy[index], state.DeepSaltMass[index], state.DeepWaterMass[index]);

				Vector2 newTerrainGradient, newSurfaceGradient;
				Vector3 newNormal;
				Geology.GetGradientAndNormal(world, state, x, y, index, e, out newTerrainGradient, out newSurfaceGradient, out newNormal);
				state.TerrainGradient[index] = newTerrainGradient;
				state.SurfaceGradient[index] = newSurfaceGradient;
				state.Normal[index] = newNormal;


				float waterCoverage = Mathf.Clamp01(state.WaterDepth[index] / world.Data.FullWaterCoverage);
				float iceCoverage = Mathf.Clamp01(state.IceMass[index] / (world.Data.MassIce * world.Data.FullIceCoverage));

				state.Canopy[index] = state.SoilFertility[index] * (state.GroundWater[index]) * (1.0f - waterCoverage) * (1.0f - iceCoverage) * Mathf.Clamp01((state.LowerAirTemperature[index] - world.Data.MinTemperatureCanopy) / (world.Data.MaxTemperatureCanopy - world.Data.MinTemperatureCanopy));

				
			}
		}

		for (int h = 0; h < worldGenData.NumHerds; h++)
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



		for (int i = 1; i < worldGenData.NumPlates; i++)
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
			world.States[i].CopyFrom(world.States[0]);
		}
	}

	static public float GetWaterMass(World world, float depth, float temperature, float salinityPSU)
	{
		float density = world.Data.waterDensity + world.Data.OceanDensityPerDegree * (temperature - world.Data.FreezingTemperature) + world.Data.OceanDensityPerSalinity * salinityPSU;
		return depth * density;
	}

}
