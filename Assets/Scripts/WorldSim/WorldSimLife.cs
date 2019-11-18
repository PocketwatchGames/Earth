using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

namespace Sim {
	static public class Animals {
		static public void Tick(World world, World.State state, World.State nextState)
		{
			for (int y = 0; y < world.Size; y++)
			{
				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);

					// Foliage
					float freshWaterAvailability = 0;
					float canopy = state.Canopy[index];
					float temperature = state.Temperature[index];

					float newCanopy = canopy;
					if (canopy > 0)
					{
						if (state.Elevation[index] <= state.SeaLevel)
						{
							newCanopy = 0;
						}
						else
						{
							float t = state.Temperature[index];
							float sf = state.SoilFertility[index];
							float groundWaterSaturation = GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], sf * world.Data.MaxSoilPorousness);
							float surfaceWater = state.SurfaceWater[index];
							freshWaterAvailability = GetFreshWaterAvailability(surfaceWater, groundWaterSaturation);

							float desiredCanopy = sf * Math.Min(groundWaterSaturation + surfaceWater, 1.0f) * Math.Max(0, (t - world.Data.MinTemperature) / (world.Data.MaxTemperature - world.Data.MinTemperature));
							float canopyGrowth = (desiredCanopy - canopy) * world.Data.canopyGrowthRate;
							newCanopy += canopyGrowth;

							//float expansion = canopy * canopyGrowth * 0.25f;
							//for (int i = 0; i < 4; i++)
							//{
							//	var n = GetNeighbor(x, y, i);
							//	int neighborIndex = GetIndex(n.x, n.y);
							//	if (state.Elevation[neighborIndex] > state.SeaLevel)
							//	{
							//		nextState.Canopy[neighborIndex] = Math.Min(1.0f, nextState.Canopy[neighborIndex] + expansion);
							//	}
							//}
						}
						nextState.Canopy[index] = Math.Max(0, newCanopy);

					}
				}
			}

			for (int i = 0; i < world.MaxHerds; i++)
			{
				var species = state.Species[state.Herds[i].SpeciesIndex];

				// Migrate tiles
				nextState.Herds[i].ActiveTileCount = state.Herds[i].DesiredTileCount;
				for (int j = 0; j < Herd.MaxActiveTiles; j++)
				{
					if (j < state.Herds[i].DesiredTileCount)
					{
						nextState.Herds[i].ActiveTiles[j] = state.Herds[i].DesiredTiles[j];
					}
				}

				// Generate tile-based shared herd resources
				float water = 0;
				float comfort = 0;
				float food = 0;
				float social = 0;
				float populationDensity = 0;
				int herdPopulation = state.Herds[i].Population;
				float radiation = 0;
				if (state.Herds[i].ActiveTileCount > 0)
				{
					for (int j = 0; j < state.Herds[i].ActiveTileCount; j++)
					{
						var tile = state.Herds[i].ActiveTiles[j];
						if (tile.x < 0 || tile.y < 0 || tile.x >= world.Size || tile.y >= world.Size)
						{
							// TODO: the active tiles should prob be a list, not a sparse array
							continue;
						}
						int tileIndex = world.GetIndex(tile.x, tile.y);
						water += state.SurfaceWater[tileIndex];
						comfort += Mathf.Clamp01((species.RestingTemperature + species.TemperatureRange - state.Temperature[tileIndex]) / species.TemperatureRange);
						food += state.Canopy[tileIndex];
						radiation += state.Radiation[tileIndex];
					}
				}
				populationDensity = (float)state.Herds[i].Population / state.Herds[i].ActiveTileCount;
				social += populationDensity;


				int population = nextState.Herds[i].Population;

				// Consume water
				float waterConsumptionRate = 0.1f;
				float waterConsumed = Mathf.Clamp(population * waterConsumptionRate, water, GetMaxWaterHeld() - state.Herds[i].Water);
				nextState.Herds[i].Water += waterConsumed;
				water -= waterConsumed;

				// Consume food
				float foodConsumptionRate = 0.1f;
				float foodConsumed = Mathf.Clamp(population * foodConsumptionRate, food, GetMaxFoodHeld() - state.Herds[i].Food);
				nextState.Herds[i].Food += foodConsumed;
				food -= foodConsumed;

				nextState.Herds[i].Social = social;

				// Update unit resources (disease)
				float immuneSystem = 1;
				// Immune system strength is based on water/food/comfort consumed
				//				if (state.Herds[i].Units[j].Maturity)
				//nextState.Herds[i].Units[j].Disease += immuneSystem * populationDensity;

				// Death
				float lifeExpectancy = 10 * world.Data.TicksPerYear;
				float deathRate = 1.0f / lifeExpectancy;
				float starvationRate = 1.0f * world.Data.TicksPerYear;
				float birthRate = 10.0f * world.Data.TicksPerYear;

				if (nextState.Herds[i].Water <= 0)
				{
					deathRate += starvationRate;
				}
				if (nextState.Herds[i].Food <= 0)
				{
					deathRate += starvationRate;
				}
				herdPopulation = (int)Mathf.Max(0, herdPopulation * (1.0f - deathRate));

				// Birth
				if (nextState.Herds[i].Social > 0 &&
					nextState.Herds[i].Water > 0 &&
					nextState.Herds[i].Food > 0)
				{
					int births = 0;
					nextState.Herds[i].Birth += social;
					if (nextState.Herds[i].Birth > birthRate)
					{
						births = (int)(nextState.Herds[i].Birth / birthRate);
						nextState.Herds[i].Birth -= birthRate * births;
						herdPopulation += births;
					}

					// Mutation and evolution
					float mutationSpeed = radiation * births;
					nextState.Herds[i].EvolutionProgress += mutationSpeed;

					float mutationHealthDelta = state.Herds[i].DesiredMutationHealth - state.Herds[i].MutationHealth;
					nextState.Herds[i].MutationHealth += Math.Sign(mutationHealthDelta) * Math.Min(mutationSpeed, Math.Abs(mutationHealthDelta));

					float mutationReproductionDelta = state.Herds[i].DesiredMutationReproduction - state.Herds[i].MutationReproduction;
					nextState.Herds[i].MutationReproduction += Math.Sign(mutationReproductionDelta) * Math.Min(mutationSpeed, Math.Abs(mutationReproductionDelta));

					float mutationSizeDelta = state.Herds[i].DesiredMutationSize - state.Herds[i].MutationSize;
					nextState.Herds[i].MutationSize += Math.Sign(mutationSizeDelta) * Math.Min(mutationSpeed, Math.Abs(mutationSizeDelta));
				}

				nextState.Herds[i].Population = herdPopulation;

				// Collapse units if they can be combined
				// Split units if they hit population cap

			}

			//for (int i = 0;i<MaxHerds;i++)
			//{
			//	float population = state.Herds[i].Population;
			//	if (population > 0)
			//	{
			//		int tileIndex = GetIndex((int)state.Herds[i].Position.x, (int)state.Herds[i].Position.y);
			//		float newPopulation = population;

			//		if (state.Elevation[tileIndex] <= state.SeaLevel)
			//		{
			//			newPopulation = 0;
			//		}
			//		else
			//		{

			//			var species = state.Species[state.Herds[i].Species];
			//			float populationDensity = population / species.speciesMaxPopulation;

			//			float babiesBorn = population * species.speciesGrowthRate;
			//			newPopulation += babiesBorn;

			//			float diedOfOldAge = Math.Max(0, population * 1.0f / (species.Lifespan * Data.TicksPerYear));
			//			newPopulation -= diedOfOldAge;

			//			float diedFromTemperature = population * Math.Abs((state.Temperature[tileIndex] - species.RestingTemperature) / species.TemperatureRange);
			//			newPopulation -= diedFromTemperature;

			//			float freshWaterAvailability = GetFreshWaterAvailability(state.SurfaceWater[tileIndex], GetGroundWaterSaturation(state.GroundWater[tileIndex], state.WaterTableDepth[tileIndex], state.SoilFertility[tileIndex] * Data.MaxSoilPorousness));
			//			float diedOfDehydration = Math.Max(0, population * (populationDensity - freshWaterAvailability / Data.freshWaterMaxAvailability) * species.dehydrationSpeed);
			//			newPopulation -= diedOfDehydration;

			//			if (species.Food == SpeciesType.FoodType.Herbivore)
			//			{
			//				float canopy = nextState.Canopy[tileIndex];
			//				float diedOfStarvation = Math.Max(0, population * (populationDensity - canopy) * species.starvationSpeed);
			//				newPopulation -= diedOfStarvation;
			//				canopy -= population * species.speciesEatRate;
			//				nextState.Canopy[tileIndex] = canopy;
			//			}
			//			else
			//			{
			//				float availableMeat = 0;
			//				for (int m = 0; m < MaxGroupsPerTile; m++)
			//				{
			//					int meatGroupIndex = state.AnimalsPerTile[tileIndex * MaxGroupsPerTile + m];
			//					if (meatGroupIndex >= 0 && state.Species[state.Herds[meatGroupIndex].Species].Food == SpeciesType.FoodType.Herbivore)
			//					{
			//						availableMeat += state.Herds[meatGroupIndex].Population;
			//					}
			//				}
			//				float diedOfStarvation = Math.Max(0, population * (population - availableMeat) * species.starvationSpeed);
			//				newPopulation -= diedOfStarvation;

			//				float meatEaten = Math.Min(availableMeat, population * species.speciesEatRate);
			//				for (int m = 0; m < MaxGroupsPerTile; m++)
			//				{
			//					int meatGroupIndex = state.AnimalsPerTile[tileIndex * MaxGroupsPerTile + m];
			//					if (meatGroupIndex >= 0 && state.Species[state.Herds[meatGroupIndex].Species].Food == SpeciesType.FoodType.Herbivore)
			//					{
			//						float meatPop = nextState.Herds[meatGroupIndex].Population;
			//						nextState.Herds[meatGroupIndex].Population = Math.Max(0, meatPop - meatEaten * meatPop / availableMeat);
			//					}
			//				}
			//			}

			//			if (state.Herds[i].Destination != state.Herds[i].Position)
			//			{
			//				Vector2 diff = state.Herds[i].Destination - state.Herds[i].Position;
			//				float dist = diff.magnitude;
			//				var dir = diff.normalized;
			//				Vector2 move = dist <= species.MovementSpeed ? diff : (species.MovementSpeed * dir);
			//				Vector2 newPos = state.Herds[i].Position + move;
			//				Vector2Int newTile = new Vector2Int((int)newPos.x, (int)newPos.y);
			//				int newTileIndex = GetIndex(newTile.x, newTile.y);

			//				// Don't walk into water
			//				Vector2 nextPos = state.Herds[i].Position + dir;
			//				if (state.Elevation[GetIndex((int)nextPos.x, (int)nextPos.y)] <= state.SeaLevel)
			//				{
			//					nextState.Herds[i].Destination = new Vector2((int)state.Herds[i].Position.x + 0.5f, (int)state.Herds[i].Position.y + 0.5f);
			//				}

			//				// move tiles
			//				if ((int)state.Herds[i].Position.x != newTile.x || (int)state.Herds[i].Position.y != newTile.y)
			//				{
			//					for (int j = 0; j < MaxGroupsPerTile; j++)
			//					{
			//						int newGroupIndex = newTileIndex * MaxGroupsPerTile + j;
			//						if (nextState.AnimalsPerTile[newGroupIndex] == -1)
			//						{
			//							nextState.AnimalsPerTile[newGroupIndex] = i;
			//							nextState.Herds[i].Position = newPos;
			//							for (int k = 0; k < MaxGroupsPerTile; k++)
			//							{
			//								int groupIndex = tileIndex * MaxGroupsPerTile + k;
			//								if (nextState.AnimalsPerTile[groupIndex] == i)
			//								{
			//									nextState.AnimalsPerTile[groupIndex] = -1;
			//									break;
			//								}
			//							}
			//							break;
			//						}
			//					}
			//				}
			//				else
			//				{
			//					nextState.Herds[i].Position = newPos;
			//				}
			//			}

			//		}
			//		nextState.Herds[i].Population = Math.Max(0, newPopulation);
			//		if (newPopulation <= 0)
			//		{
			//			for (int j = 0; j < MaxGroupsPerTile; j++)
			//			{
			//				int groupIndex = tileIndex * MaxGroupsPerTile + j;
			//				if (nextState.AnimalsPerTile[groupIndex] == i)
			//				{
			//					nextState.AnimalsPerTile[groupIndex] = -1;
			//				}
			//			}
			//		}

			//	}
			//}
		}

		static public float GetGroundWaterSaturation(float groundWater, float waterTableDepth, float soilPorousness)
		{
			return groundWater / (waterTableDepth * soilPorousness);
		}

		static public float GetFreshWaterAvailability(float surfaceWater, float groundWaterSaturation)
		{
			return surfaceWater > 0 ? 1.0f : Math.Min(1.0f, groundWaterSaturation);
		}

		static public float GetMaxFoodHeld()
		{
			return 1.0f;
		}
		static public float GetMaxWaterHeld()
		{
			return 1.0f;
		}
		static public float GetMaxComfortHeld()
		{
			return 1.0f;
		}
		static public float GetMaxPopulationDensity()
		{
			return 100.0f;
		}

	}
}