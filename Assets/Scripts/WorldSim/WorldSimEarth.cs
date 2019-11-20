using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

namespace Sim {
	static public class Geology {
		static public void Tick(World world, World.State state, World.State nextState)
		{
			for (int y = 0; y < world.Size; y++)
			{
				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);
					float elevation = state.Elevation[index];
					Vector2 newFlowDirection;
					Vector3 newNormal;
					UpdateFlowDirectionAndNormal(world, state, nextState, x, y, index, elevation, out newFlowDirection, out newNormal);
					nextState.FlowDirection[index] = newFlowDirection;
					nextState.Normal[index] = newNormal;
				}
			}
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
							if (!world.IsOcean(startElevation, state.SeaLevel) && !world.IsOcean(endElevation, state.SeaLevel))
							{
								// continental collision
								nextState.Elevation[newIndex] += 50;
								nextState.Elevation[index] += 50;
								nextState.Plate[newIndex] = plateIndex;
							}
							else
							{
								// subduction
								if (!world.IsOcean(startElevation, state.SeaLevel))
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
					Vector2 newFlowDirection;
					Vector3 newNormal;
					UpdateFlowDirectionAndNormal(world, nextState, nextState, x, y, index, elevation, out newFlowDirection, out newNormal);
					nextState.FlowDirection[index] = newFlowDirection;
					nextState.Normal[index] = newNormal;

					if (nextState.SurfaceWater[index] > 0 && world.IsOcean(nextState.Elevation[index], state.SeaLevel))
					{
						nextState.SurfaceWater[index] = 0;
					}
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
			nextState.SurfaceWater[newIndex] = state.SurfaceWater[index];
			nextState.OceanSalinityShallow[newIndex] = state.OceanSalinityShallow[index];
			nextState.OceanSalinityDeep[newIndex] = state.OceanSalinityDeep[index];
			nextState.OceanEnergyShallow[newIndex] = state.OceanEnergyShallow[index];
			nextState.OceanEnergyDeep[newIndex] = state.OceanEnergyDeep[index];
			nextState.SurfaceIce[newIndex] = state.SurfaceIce[index];
			nextState.SubmergedIce[newIndex] = state.SubmergedIce[index];
			nextState.SoilFertility[newIndex] = state.SoilFertility[index];

		}


		static private void UpdateFlowDirectionAndNormal(World world, World.State state, World.State nextState, int x, int y, int index, float elevation, out Vector2 flowDirection, out Vector3 normal)
		{
			if (world.IsOcean(elevation, state.SeaLevel))
			{
				flowDirection = Vector2.zero;
				normal = new Vector3(0, 0, 1);
			}
			else
			{
				int indexW = world.GetIndex(world.WrapX(x - 1), y);
				int indexE = world.GetIndex(world.WrapX(x + 1), y);
				int indexN = world.GetIndex(x, world.WrapY(y - 1));
				int indexS = world.GetIndex(x, world.WrapY(y + 1));
				float e = state.Elevation[index];
				float west = state.Elevation[indexW];
				float east = state.Elevation[indexE];
				float north = state.Elevation[indexN];
				float south = state.Elevation[indexS];

				e += state.SurfaceWater[index];
				west += state.SurfaceWater[indexW];
				east += state.SurfaceWater[indexE];
				north += state.SurfaceWater[indexN];
				south += state.SurfaceWater[indexS];

				Vector2 g;
				if (west < e && west < east && west < north && west < south)
				{
					g = new Vector2(west - e, 0);
				}
				else if (east < e && east < west && east < north && east < south)
				{
					g = new Vector2(e - east, 0);
				}
				else if (north < e && north < west && north < east && north < south)
				{
					g = new Vector2(0, north - e);
				}
				else if (south < e && south < west && south < north && south < east)
				{
					g = new Vector2(0, e - south);
				}
				else
				{
					g = Vector2.zero;
				}

				flowDirection = new Vector2(Math.Sign(g.x) * (1.0f + (float)Math.Pow(Math.Abs(g.x) / world.Data.tileSize, world.Data.FlowSpeedExponent)), Math.Sign(g.y) * (1.0f + (float)Math.Pow(Math.Abs(g.x) / world.Data.tileSize, world.Data.FlowSpeedExponent)));

				// TODO: this is wong, gradient is just steepest downhill direction
				normal = Vector3.Normalize(new Vector3(g.x, g.y, world.Data.tileSize));

			}
		}


	}
}