﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Unity.Profiling;

namespace Sim {
	static public class Geology {

		static ProfilerMarker _ProfileEarthTick = new ProfilerMarker("Earth Tick");

		static public void Tick(World world, World.State state, World.State nextState)
		{
			_ProfileEarthTick.Begin();
			for (int y = 0; y < world.Size; y++)
			{
				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);
					float elevation = state.Elevation[index];
					Vector2 newTerrainGradient, newSurfaceGradient;
					Vector3 newNormal;
					GetGradientAndNormal(world, state, x, y, index, elevation, out newTerrainGradient, out newSurfaceGradient, out newNormal);
					nextState.TerrainGradient[index] = newTerrainGradient;
					nextState.SurfaceGradient[index] = newSurfaceGradient;
					nextState.Normal[index] = newNormal;
				}
			}
			_ProfileEarthTick.End();
		}

		static public void MovePlate(World world, World.State state, World.State nextState, int plateIndex, Vector2Int direction)
		{
			// TODO: enforce conservation of mass

			for (int y = 0; y < world.Size; y++)
			{
				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);
					if (state.Plate[index] == plateIndex)
					{
						Vector2Int newPoint = new Vector2Int(world.WrapX(x + direction.x), world.WrapY(y + direction.y));
						int newIndex = world.GetIndex(newPoint.x, newPoint.y);
						if (state.Plate[newIndex] == plateIndex)
						{
							MoveTile(world, state, nextState, index, newIndex);

							Vector2Int divergentPoint = new Vector2Int(world.WrapX(x - direction.x), world.WrapY(y - direction.y));
							int divergentIndex = world.GetIndex(divergentPoint.x, divergentPoint.y);
							if (state.Plate[divergentIndex] != plateIndex)
							{
								// divergent zone
								//								if (state.Elevation[index] > state.Elevation[divergentIndex])
								{
									nextState.Plate[index] = state.Plate[divergentIndex];
								}
								nextState.Elevation[index] = (state.Elevation[index] + state.Elevation[divergentIndex]) / 2 - 100;
								nextState.WaterTableDepth[index] = (state.WaterTableDepth[index] + state.WaterTableDepth[divergentIndex]) / 2;
								nextState.SoilFertility[index] = (state.SoilFertility[index] + state.SoilFertility[divergentIndex]) / 2;
							}
						}
						else
						{
							float startElevation = state.Elevation[index];
							float endElevation = state.Elevation[newIndex];
							if (!world.IsOcean(state.WaterDepth[index]) && !world.IsOcean(state.WaterDepth[index])) // TODO: this is broken now that sealevel isnt a constant
							{
								// continental collision
								nextState.Elevation[newIndex] += 50;
								nextState.Elevation[index] += 50;
								nextState.Plate[newIndex] = plateIndex;
							}
							else
							{
								// subduction
								if (!world.IsOcean(state.WaterDepth[index]))
								{
									// We are moving OVER the adjacent tile
									MoveTile(world, state, nextState, index, newIndex);
									Vector2Int subductionPoint = new Vector2Int(world.WrapX(newPoint.x + direction.x), world.WrapY(newPoint.y + direction.y));
									int subductionIndex = world.GetIndex(subductionPoint.x, subductionPoint.y);
									nextState.Elevation[newIndex] = (state.Elevation[newIndex] + state.Elevation[subductionIndex]) / 2;
									nextState.Elevation[subductionIndex] -= 100;
								}
								else
								{
									// we are moving UNDER the adjacent tile
									nextState.Elevation[newIndex] = (state.Elevation[newIndex] + state.Elevation[index]) / 2;
									nextState.Elevation[index] -= 100;
								}
							}
						}
					}
				}
			}

			for (int y = 0; y < world.Size; y++)
			{
				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);
					float elevation = state.Elevation[index];
					Vector2 newSurfaceGradient, newTerrainGradient;
					Vector3 newNormal;
					GetGradientAndNormal(world, nextState, x, y, index, elevation, out newTerrainGradient, out newSurfaceGradient, out newNormal);
					nextState.TerrainGradient[index] = newTerrainGradient;
					nextState.SurfaceGradient[index] = newSurfaceGradient;
					nextState.Normal[index] = newNormal;
				}
			}

		}

		static public void MoveTile(World world, World.State state, World.State nextState, int index, int newIndex)
		{
			nextState.Plate[newIndex] = state.Plate[index];
			nextState.Elevation[newIndex] = state.Elevation[index];
			nextState.Canopy[newIndex] = state.Canopy[index];
			nextState.WaterTableDepth[newIndex] = state.WaterTableDepth[index];
			nextState.GroundWater[newIndex] = state.GroundWater[index];
			nextState.ShallowSaltMass[newIndex] = state.ShallowSaltMass[index];
			nextState.DeepSaltMass[newIndex] = state.DeepSaltMass[index];
			nextState.ShallowWaterTemperature[newIndex] = state.ShallowWaterTemperature[index];
			nextState.ShallowWaterEnergy[newIndex] = state.ShallowWaterEnergy[index];
			nextState.DeepWaterEnergy[newIndex] = state.DeepWaterEnergy[index];
			nextState.IceMass[newIndex] = state.IceMass[index];
			nextState.SoilFertility[newIndex] = state.SoilFertility[index];

		}


		static public void GetGradientAndNormal(World world, World.State state, int x, int y, int index, float elevation, out Vector2 terrainGradient, out Vector2 surfaceGradient, out Vector3 normal)
		{
			int indexW = world.GetIndex(world.WrapX(x - 1), y);
			int indexE = world.GetIndex(world.WrapX(x + 1), y);
			int indexN = world.GetIndex(x, world.WrapY(y + 1));
			int indexS = world.GetIndex(x, world.WrapY(y - 1));
			float e = state.Elevation[index];
			float west = e - state.Elevation[indexW];
			float east = e - state.Elevation[indexE];
			float north = e - state.Elevation[indexN];
			float south = e - state.Elevation[indexS];

			Vector2 g = new Vector2(east > west ? Mathf.Max(0, east) : -Mathf.Max(0, west), north > south ? Mathf.Max(0, north) : -Mathf.Max(0, south));
			terrainGradient = g * world.Data.InverseMetersPerTile;

			float depth = state.WaterDepth[index];
			west += depth - state.WaterDepth[indexW];
			east += depth - state.WaterDepth[indexE];
			north += depth - state.WaterDepth[indexN];
			south += depth - state.WaterDepth[indexS];

			g = new Vector2(east > west ? Mathf.Max(0, east) : -Mathf.Max(0, west), north > south ? Mathf.Max(0, north) : -Mathf.Max(0, south));
			surfaceGradient = g * world.Data.InverseMetersPerTile;

			normal = Vector3.Normalize(new Vector3((west - east) / 2, (south - north) / 2, world.Data.MetersPerTile));
		}


	}
}