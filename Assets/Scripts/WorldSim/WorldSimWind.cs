using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Unity.Profiling;

namespace Sim {
	static public class Wind {

		static ProfilerMarker _ProfileWindTick = new ProfilerMarker("Wind Tick");
		static public Vector3 GetWind(World world, World.State state, int x, int y, Vector3 curWind, float latitude, float planetRotationSpeed, float coriolisParam, float[] worldPressure, float[] worldTemperature, bool isUpper, float thisPressure, float landElevation, float windElevation, float friction, float density)
		{
			float altitude = Mathf.Max(0, windElevation - landElevation);
			float complementFrictionAtElevation = 1.0f - friction * Mathf.Max(0, (world.Data.BoundaryZoneElevation - altitude) / world.Data.BoundaryZoneElevation);
			float inverseDensity = 1.0f / density;
			var pressureGradientForce = GetPressureGradient(world, state, x, y, worldPressure, worldTemperature, isUpper, thisPressure, windElevation);
			pressureGradientForce.x *= world.Data.GravitationalAcceleration / world.Data.tileSize;
			pressureGradientForce.y *= world.Data.GravitationalAcceleration / world.Data.tileSize;
			Vector3 wind = Vector3.zero;

			//for (int i = 0; i < 4; i++)
			//{
			//	var nIndex = world.GetNeighborIndex(x, y, i);
			//	var neighborWind = state.UpperWind[nIndex];
			//	float nWindSpeed = Mathf.Sqrt(neighborWind.x * neighborWind.x + neighborWind.y * neighborWind.y);
			//	switch (i)
			//	{
			//		case 0:
			//			if (neighborWind.x > 0)
			//			{
			//				wind += neighborWind.x / nWindSpeed * new Vector3(neighborWind.x, neighborWind.y, 0);
			//			}
			//			break;
			//		case 1:
			//			if (neighborWind.x < 0)
			//			{
			//				wind += -neighborWind.x / nWindSpeed * new Vector3(neighborWind.x, neighborWind.y, 0);
			//			}
			//			break;
			//		case 2:
			//			if (neighborWind.y < 0)
			//			{
			//				wind += -neighborWind.y / nWindSpeed * new Vector3(neighborWind.x, neighborWind.y, 0);
			//			}
			//			break;
			//		case 3:
			//			if (neighborWind.y > 0)
			//			{
			//				wind += neighborWind.y / nWindSpeed * new Vector3(neighborWind.x, neighborWind.y, 0);
			//			}
			//			break;
			//	}
			//}

			var pressureWind = pressureGradientForce * world.Data.PressureGradientWindMultiplier;
			if (coriolisParam != 0)
			{
				float geostrophicInfluence = Mathf.Clamp01(Mathf.Pow(Mathf.Abs(coriolisParam) * 2, 2)) * complementFrictionAtElevation;
				var geostrophicWind = new Vector3(-pressureGradientForce.y, pressureGradientForce.x, 0) / (planetRotationSpeed * coriolisParam);
				wind = (geostrophicWind * geostrophicInfluence + pressureWind * (1.0f - geostrophicInfluence));
			} else
			{
				wind = pressureWind;
			}
			wind *= complementFrictionAtElevation * inverseDensity;


			//wind += inertialWind * world.Data.windInertia;
			return wind;
		}
		static public Vector3 GetCurrent(World world, int x, int y, float latitude, float planetRotationSpeed, float coriolisParam, Vector3 pressureGradientForce)
		{
			float pressureGradientZ = pressureGradientForce.z;
			float coriolisPower = Mathf.Abs(coriolisParam);
			Vector3 w = pressureGradientForce * world.Data.WindToOceanCurrentFactor;
			if (coriolisPower > 0)
			{
				float geostrophicInfluence = Mathf.Clamp01(Mathf.Pow(coriolisPower * 2, 2)) * 0.5f;
				var geostrophicCurrent = new Vector3(-w.y, w.x, 0) * world.Data.GravitationalAcceleration / (world.Data.tileSize * planetRotationSpeed * coriolisParam);
				w = geostrophicCurrent * geostrophicInfluence + (1.0f - geostrophicInfluence) * w;
			}
			w.z = pressureGradientZ;
			return w;
		}
		static public void Tick(World world, World.State state, World.State nextState)
		{
			_ProfileWindTick.Begin();

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
					float friction;
					if (elevation < state.SeaLevel)
					{
						friction = world.Data.WindOceanFriction;
					} else
					{
						friction = world.Data.WindLandFriction;
					}
					friction = Mathf.Clamp01(Mathf.Lerp(friction, world.Data.WindIceFriction, state.Ice[index] / world.Data.FullIceCoverage));
					float lowerTemperatureAtSeaLevel = lowerTemperature - world.Data.TemperatureLapseRate * elevationOrSeaLevel;
					float surfaceAirDensity = Atmosphere.GetAirDensity(world, lowerPressure, lowerTemperatureAtSeaLevel);
					float boundaryElevation = elevationOrSeaLevel + world.Data.BoundaryZoneElevation;
					float upperTemperatureAtSeaLevel = upperTemperature - world.Data.TemperatureLapseRate * boundaryElevation;
					float tropopausePressure = upperPressure * Mathf.Pow(1 + world.Data.TemperatureLapseRate / upperTemperatureAtSeaLevel * world.Data.TropopauseElevation, -world.Data.PressureExponent);
					float tropopauseDensity = Atmosphere.GetAirDensity(world, tropopausePressure, upperTemperature + world.Data.TemperatureLapseRate * (world.Data.TropopauseElevation - boundaryElevation));

					var upperWind = GetWind(world, state, x, y, state.UpperWind[index], latitude, state.PlanetRotationSpeed, windInfo.coriolisParam, state.UpperAirPressure, state.UpperAirTemperature, true, tropopausePressure, elevationOrSeaLevel, world.Data.TropopauseElevation, 0, tropopauseDensity);
					var lowerWind = GetWind(world, state, x, y, state.LowerWind[index], latitude, state.PlanetRotationSpeed, windInfo.coriolisParam, state.LowerAirPressure, state.LowerAirTemperature, false, lowerPressure, elevationOrSeaLevel, elevationOrSeaLevel, friction, surfaceAirDensity);

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
					var verticalTemperatureDifferential = upperTemperatureAtSeaLevel - lowerTemperatureAtSeaLevel;

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
							shallowCurrent = GetCurrent(world, x, y, latitude, state.PlanetRotationSpeed, windInfo.coriolisParam, lowerWind);
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
						nextState.OceanCurrentDeep[index] = new Vector3(densityDifferential.x, densityDifferential.y, 0) * world.Data.OceanDensityCurrentSpeed;
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

			_ProfileWindTick.End();

		}


		static private Vector3 GetPressureGradient(World world, World.State state, int x, int y, float[] neighborPressure, float[] neighborTemperature, bool isUpper, float pressureAtWindElevation, float windElevation)
		{
			Vector2 pressureDifferential = Vector2.zero;
			Vector3 nWind = Vector3.zero;
			for (int i = 0; i < 4; i++)
			{
				var nIndex = world.GetNeighborIndex(x, y, i);
				// see bottom of: https://en.wikipedia.org/wiki/Vertical_pressure_variation
				float neighborElevation = Mathf.Max(state.SeaLevel, state.Elevation[nIndex]);
				float neighborTemperatureElevation;
				if (isUpper)
				{
					neighborTemperatureElevation = neighborElevation + world.Data.BoundaryZoneElevation;
				} else
				{
					neighborTemperatureElevation = neighborElevation;
				}
				float neighborTemperatureAtSeaLevel = neighborTemperature[nIndex] - neighborTemperatureElevation * world.Data.TemperatureLapseRate;
				float neighborElevationAtPressure = neighborTemperatureAtSeaLevel / world.Data.TemperatureLapseRate * (Mathf.Pow(pressureAtWindElevation/ neighborPressure[nIndex], -1.0f / world.Data.PressureExponent) - 1);


				switch (i)
				{
					case 0:
						pressureDifferential.x += neighborElevationAtPressure - windElevation;
						break;
					case 1:
						pressureDifferential.x -= neighborElevationAtPressure - windElevation;
						break;
					case 2:
						pressureDifferential.y -= neighborElevationAtPressure - windElevation;
						break;
					case 3:
						pressureDifferential.y += neighborElevationAtPressure - windElevation;
						break;
				}
			}
			return new Vector3(pressureDifferential.x, pressureDifferential.y, 0);

		}

	}
}