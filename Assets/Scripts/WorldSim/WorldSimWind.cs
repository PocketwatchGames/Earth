using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

namespace Sim {
	static public class Wind {

		static public Vector3 GetWind(World world, int x, int y, float latitude, float planetRotationSpeed, float coriolisParam, float[] worldPressure, float thisPressure, float landElevation, float windElevation, float friction, float density)
		{
			float elevationAboveLand = Mathf.Max(0, windElevation - landElevation);
			float inverseFrictionAtElevation = 1.0f - Mathf.Max(0, (world.Data.BoundaryZoneElevation - elevationAboveLand) / world.Data.BoundaryZoneElevation * friction);
			float boundaryElevation = landElevation + world.Data.BoundaryZoneElevation;
			float middleOfUpperAtmosphere = (world.Data.troposphereElevation - boundaryElevation) / 2 + boundaryElevation;
			float middleOfBoundaryZone = landElevation + world.Data.BoundaryZoneElevation / 2;
			float inverseDensity = 1.0f / density;
			var pressureGradientForce = GetPressureGradient(world, x, y, worldPressure, thisPressure);
			pressureGradientForce.x *= world.Data.GravitationalAcceleration / world.Data.tileSize;
			pressureGradientForce.y *= world.Data.GravitationalAcceleration / world.Data.tileSize;
			var wind = pressureGradientForce * inverseFrictionAtElevation * inverseDensity;
			if (coriolisParam != 0)
			{
				float coriolisSpeed = 1.0f / (planetRotationSpeed * coriolisParam);
				float rossbyNumber = Mathf.Abs(coriolisSpeed) * Mathf.Sqrt(wind.x * wind.x + wind.y * wind.y);
				var geostrophicWind = new Vector3(-wind.y * coriolisSpeed, wind.x  * coriolisSpeed, 0);
				float geostrophicInfluence = 1.0f - Mathf.Clamp01(0.5f + Mathf.Log10(rossbyNumber) * 0.25f);
				wind = geostrophicWind * geostrophicInfluence + wind * (1.0f - geostrophicInfluence);
			}
			//Vector3 inertialWind = ;
			//wind += inertialWind * world.Data.windInertia;
			return wind;
		}
		static public Vector3 GetCurrent(World world, int x, int y, float latitude, float planetRotationSpeed, float coriolisParam, Vector3 pressureGradientForce)
		{
			float pressureGradientZ = pressureGradientForce.z;
			float coriolisPower = Mathf.Abs(coriolisParam);
			var w = pressureGradientForce * (1.0f - coriolisPower) * world.Data.WindToOceanCurrentFactor;
			if (coriolisPower > 0)
			{
				w += coriolisPower * new Vector3(-pressureGradientForce.y * world.Data.GravitationalAcceleration / (planetRotationSpeed * coriolisParam * world.Data.tileSize), pressureGradientForce.x * world.Data.GravitationalAcceleration / (planetRotationSpeed * coriolisParam * world.Data.tileSize), 0);
			}
			w.z = pressureGradientZ;
			return w;
		}
		static public void Tick(World world, World.State state, World.State nextState)
		{
			for (int y = 0; y < world.Size; y++)
			{
				float latitude = world.Data.windInfo[y].latitude;
				var windInfo = world.Data.windInfo[y];

				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);

					float upperPressure = state.UpperAirPressure[index];
					float lowerPressure = state.LowerAirPressure[index];
					float upperTemperature = state.UpperAirTemperature[index];
					float lowerTemperature = state.LowerAirTemperature[index];
					float elevation = state.Elevation[index];
					float elevationOrSeaLevel = Math.Max(state.SeaLevel, elevation);
					var normal = state.Normal[index];
					float friction = Mathf.Clamp01(world.Data.WindLandFrictionMinimum + (1.0f - world.Data.WindLandFrictionMinimum) * Mathf.Clamp01((1.0f - normal.z) / world.Data.MaxTerrainNormalForFriction));
					float upperAirDensity = 0.3639f;
					float surfaceAirDensity = Atmosphere.GetAirDensity(world, lowerPressure, upperPressure, elevationOrSeaLevel, elevationOrSeaLevel, lowerTemperature, upperTemperature);

					var upperWind = GetWind(world, x, y, latitude, state.PlanetRotationSpeed, windInfo.coriolisParam, state.UpperAirPressure, upperPressure, elevationOrSeaLevel, world.Data.troposphereElevation, 0, upperAirDensity);
					var lowerWind = GetWind(world, x, y, latitude, state.PlanetRotationSpeed, windInfo.coriolisParam, state.LowerAirPressure, lowerPressure, elevationOrSeaLevel, elevationOrSeaLevel, friction, surfaceAirDensity);

					// within 1 km of the ground, frictional forces slow wind down
					float neighborTemperatureDifferential = 0;
					float neighborElevationDifferential = 0;
					if (lowerWind.x < 0)
					{
						int neighborIndex = world.GetNeighborIndex(x, y, 0);
						neighborTemperatureDifferential += -lowerWind.x * (lowerTemperature - state.LowerAirTemperature[neighborIndex]);
						neighborElevationDifferential += (Mathf.Max(state.SeaLevel, state.Elevation[neighborIndex]) - elevationOrSeaLevel) * -lowerWind.x;
					}
					else
					{
						var neighborIndex = world.GetNeighborIndex(x, y, 1);
						neighborTemperatureDifferential += lowerWind.x * (lowerTemperature - state.LowerAirTemperature[neighborIndex]);
						neighborElevationDifferential += (Mathf.Max(state.SeaLevel, state.Elevation[neighborIndex]) - elevationOrSeaLevel) * lowerWind.x;
					}
					if (lowerWind.y < 0)
					{
						var neighborIndex = world.GetNeighborIndex(x, y, 3);
						neighborTemperatureDifferential += -lowerWind.y * (lowerTemperature - state.LowerAirTemperature[neighborIndex]);
						neighborElevationDifferential += (Mathf.Max(state.SeaLevel, state.Elevation[neighborIndex]) - elevationOrSeaLevel) * -lowerWind.y;
					}
					else
					{
						var neighborIndex = world.GetNeighborIndex(x, y, 2);
						neighborTemperatureDifferential += lowerWind.y * (lowerTemperature - state.LowerAirTemperature[neighborIndex]);
						neighborElevationDifferential += (Mathf.Max(state.SeaLevel, state.Elevation[neighborIndex]) - elevationOrSeaLevel) * lowerWind.y;
					}
					var verticalTemperatureDifferential = (lowerTemperature + elevationOrSeaLevel * world.Data.temperatureLapseRate) - (upperTemperature + world.Data.troposphereElevation * world.Data.temperatureLapseRate);

					float lowerWindSpeedXY = Mathf.Sqrt(lowerWind.x * lowerWind.x + lowerWind.y * lowerWind.y);
					lowerWind.z = neighborElevationDifferential * world.Data.MountainUpdraftWindSpeed;
					lowerWind.z += neighborTemperatureDifferential * world.Data.DestinationTemperatureDifferentialVerticalWindSpeed;
					lowerWind.z += (lowerPressure - upperPressure) * world.Data.PressureToVerticalWindSpeed; // convection
					lowerWind.z += verticalTemperatureDifferential * world.Data.TemperatureDifferentialToVerticalWindSpeed; // thermal

					nextState.LowerWind[index] = lowerWind;
					nextState.UpperWind[index] = upperWind;

					if (world.IsOcean(state.Elevation[index], state.SeaLevel))
					{
						float ice = state.Ice[index];
						Vector3 shallowCurrent;
						if (ice < world.Data.FullIceCoverage)
						{
							shallowCurrent = GetCurrent(world, x, y, latitude, state.PlanetRotationSpeed, windInfo.coriolisParam * 0.5f, lowerWind);
							if (ice > 0)
							{
								shallowCurrent *= 1.0f - ice / world.Data.FullIceCoverage;
							}
						}
						else
						{
							shallowCurrent = Vector3.zero;
						}
						nextState.OceanCurrentShallow[index] = shallowCurrent;

						float density = state.OceanDensityDeep[index];
						Vector2 densityDifferential = Vector2.zero;
						for (int i = 0; i < 4; i++)
						{
							var neighbor = world.GetNeighbor(x, y, i);
							int nIndex = world.GetIndex(neighbor.x, neighbor.y);
							if (world.IsOcean(state.Elevation[nIndex], state.SeaLevel))
							{
								//var neighborWind = state.Wind[nIndex];
								//nWind += neighborWind;

								switch (i)
								{
									case 0:
										densityDifferential.x += state.OceanDensityDeep[nIndex] - density;
										break;
									case 1:
										densityDifferential.x -= state.OceanDensityDeep[nIndex] - density;
										break;
									case 2:
										densityDifferential.y -= state.OceanDensityDeep[nIndex] - density;
										break;
									case 3:
										densityDifferential.y += state.OceanDensityDeep[nIndex] - density;
										break;
								}
							}
						}
						nextState.OceanCurrentDeep[index] = new Vector3(densityDifferential.x, densityDifferential.y, 0);
					}
				}
			}

			for (int y = 0; y < world.Size; y++)
			{
				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);
					if (world.IsOcean(state.Elevation[index], state.SeaLevel))
					{
						float vertCurrent = 0;
						for (int i = 0; i < 4; i++)
						{
							var neighbor = world.GetNeighbor(x, y, i);
							int nIndex = world.GetIndex(neighbor.x, neighbor.y);
							if (!world.IsOcean(state.Elevation[nIndex], state.SeaLevel))
							{
								switch (i)
								{
									case 0:
										vertCurrent += nextState.OceanCurrentShallow[index].x;
										break;
									case 1:
										vertCurrent -= nextState.OceanCurrentShallow[index].x;
										break;
									case 2:
										vertCurrent -= nextState.OceanCurrentShallow[index].y;
										break;
									case 3:
										vertCurrent += nextState.OceanCurrentShallow[index].y;
										break;
								}
							}
						}
						nextState.OceanCurrentShallow[index].z = vertCurrent;
					}
				}
			}

		}


		static private Vector3 GetPressureGradient(World world, int x, int y, float[] neighbors, float pressure)
		{
			Vector2 pressureDifferential = Vector2.zero;
			Vector3 nWind = Vector3.zero;
			for (int i = 0; i < 4; i++)
			{
				var neighbor = world.GetNeighbor(x, y, i);
				int nIndex = world.GetIndex(neighbor.x, neighbor.y);
				//var neighborWind = state.Wind[nIndex];
				//nWind += neighborWind;

				switch (i)
				{
					case 0:
						pressureDifferential.x += neighbors[nIndex] - pressure;
						break;
					case 1:
						pressureDifferential.x -= neighbors[nIndex] - pressure;
						break;
					case 2:
						pressureDifferential.y -= neighbors[nIndex] - pressure;
						break;
					case 3:
						pressureDifferential.y += neighbors[nIndex] - pressure;
						break;
				}
			}
			return new Vector3(pressureDifferential.x, pressureDifferential.y, 0);

		}

	}
}