﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public partial class World
{
	public void TickAnimals(State state, State nextState)
	{
		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				int index = GetIndex(x, y);

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
						float groundWaterSaturation = GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], sf * Data.MaxSoilPorousness);
						float surfaceWater = state.SurfaceWater[index];
						freshWaterAvailability = GetFreshWaterAvailability(surfaceWater, groundWaterSaturation);

						float desiredCanopy = sf * Math.Min(groundWaterSaturation + surfaceWater, 1.0f) * Math.Max(0, (t - Data.MinTemperature) / (Data.MaxTemperature - Data.MinTemperature));
						float canopyGrowth = (desiredCanopy - canopy) * Data.canopyGrowthRate;
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

	public float GetGroundWaterSaturation(float groundWater, float waterTableDepth, float soilPorousness)
	{
		return groundWater / (waterTableDepth * soilPorousness);
	}

	public float GetFreshWaterAvailability(float surfaceWater, float groundWaterSaturation)
	{
		return surfaceWater > 0 ? 1.0f : Math.Min(1.0f, groundWaterSaturation);
	}



}
