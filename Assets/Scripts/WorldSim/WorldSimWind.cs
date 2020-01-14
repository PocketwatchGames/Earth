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
		static public Vector2 GetHorizontalWind(World world, World.State state, int x, int y, Vector3 curWind, float latitude, float planetRotationSpeed, float coriolisParam, float inverseCoriolisParam, float coriolisInfluenceAtElevation, float[] worldPressure, float[] worldTemperature, bool isUpper, float thisPressure, float landElevation, float windElevation, float friction, float density, float molarMass)
		{
			float inverseDensity = 1.0f / density;
			float altitude = Mathf.Max(0, windElevation - landElevation);
			float complementFrictionAtElevation = 1.0f - friction * Mathf.Max(0, (world.Data.BoundaryZoneElevation - altitude) / world.Data.BoundaryZoneElevation);
			var pressureGradientForce = GetPressureGradient(world, state, x, y, worldPressure, worldTemperature, isUpper, thisPressure, windElevation, molarMass);
			pressureGradientForce.x *= world.Data.GravitationalAcceleration * world.Data.InverseMetersPerTile;
			pressureGradientForce.y *= world.Data.GravitationalAcceleration * world.Data.InverseMetersPerTile;
			Vector2 wind = Vector2.zero;

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
				float geostrophicInfluence = Mathf.Clamp01(Mathf.Pow(Mathf.Abs(coriolisParam) * 2, 2)) * complementFrictionAtElevation * coriolisInfluenceAtElevation;
				var geostrophicWind = new Vector2(-pressureGradientForce.y, pressureGradientForce.x) * inverseCoriolisParam / planetRotationSpeed;
				wind = (geostrophicWind * geostrophicInfluence + pressureWind * (1.0f - geostrophicInfluence));
			} else
			{
				wind = pressureWind;
			}
			wind *= complementFrictionAtElevation * inverseDensity;


			//wind += inertialWind * world.Data.windInertia;
			return wind;
		}
		static public Vector2 GetCurrentHorizontal(World world, int x, int y, float latitude, float planetRotationSpeed, float coriolisParam, float inverseCoriolisParam, Vector2 pressureGradientForce)
		{
			float coriolisPower = Mathf.Abs(coriolisParam);
			Vector2 w = pressureGradientForce * world.Data.WindToOceanCurrentFactor;
			if (coriolisPower > 0)
			{
				float geostrophicInfluence = Mathf.Clamp01(Mathf.Pow(coriolisPower * 2, 2)) * world.Data.GlobalCoriolisInfluenceOcean;
				var geostrophicCurrent = new Vector2(-w.y, w.x) * world.Data.GravitationalAcceleration * world.Data.InverseMetersPerTile * inverseCoriolisParam / planetRotationSpeed;
				w = geostrophicCurrent * geostrophicInfluence + (1.0f - geostrophicInfluence) * w;
			}
			return w;
		}
		static public void Tick(World world, World.State state, World.State nextState)
		{
			_ProfileWindTick.Begin();

			float inverseFullIceCoverage = 1.0f / world.Data.FullIceCoverage;
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
					float waterDepth = state.WaterDepth[index];
					float waterAndIceDepth = state.WaterAndIceDepth[index];
					float elevationOrSeaLevel = elevation + waterAndIceDepth;
					var normal = state.Normal[index];
					float iceCoverage = state.IceMass[index] * inverseFullIceCoverage;
					float friction;
					if (waterDepth > 0)
					{
						friction = world.Data.WindOceanFriction;
					} else
					{
						friction = world.Data.WindLandFriction;
					}
					friction = Mathf.Clamp01(Mathf.Lerp(friction, world.Data.WindIceFriction, iceCoverage));
					float lowerTemperatureAtSeaLevel = lowerTemperature - world.Data.TemperatureLapseRate * elevationOrSeaLevel;
					float molarMassLowerAir = Atmosphere.GetMolarMassAir(world, state.LowerAirMass[index], state.Humidity[index]);
					float molarMassUpperAir = Atmosphere.GetMolarMassAir(world, state.UpperAirMass[index] + state.StratosphereMass, state.CloudMass[index]);
					float surfaceAirDensity = Atmosphere.GetAirDensity(world, lowerPressure, lowerTemperatureAtSeaLevel, molarMassLowerAir);
					float boundaryElevation = elevationOrSeaLevel + world.Data.BoundaryZoneElevation;
					float upperTemperatureAtSeaLevel = upperTemperature - world.Data.TemperatureLapseRate * boundaryElevation;
					float tropopausePressure = upperPressure * Mathf.Pow(1 + world.Data.TemperatureLapseRate / upperTemperatureAtSeaLevel * world.Data.TropopauseElevation, -world.Data.PressureExponent * molarMassUpperAir);
					float tropopauseDensity = Atmosphere.GetAirDensity(world, tropopausePressure, upperTemperature + world.Data.TemperatureLapseRate * (world.Data.TropopauseElevation - boundaryElevation), molarMassUpperAir);

					var upperWindH = GetHorizontalWind(world, state, x, y, state.UpperWind[index], latitude, state.PlanetRotationSpeed, windInfo.coriolisParam, windInfo.inverseCoriolisParam, world.Data.GlobalCoriolisInfluenceWindUpper, state.UpperAirPressure, state.UpperAirTemperature, true, tropopausePressure, elevationOrSeaLevel, world.Data.TropopauseElevation, 0, tropopauseDensity, molarMassUpperAir);
					var lowerWindH = GetHorizontalWind(world, state, x, y, state.LowerWind[index], latitude, state.PlanetRotationSpeed, windInfo.coriolisParam, windInfo.inverseCoriolisParam, world.Data.GlobalCoriolisInfluenceWindLower, state.LowerAirPressure, state.LowerAirTemperature, false, lowerPressure, elevationOrSeaLevel, elevationOrSeaLevel, friction, surfaceAirDensity, molarMassLowerAir);

					// within 1 km of the ground, frictional forces slow wind down
					float neighborPressureDifferential = 0;
					float neighborElevationDifferential = 0;
					if (lowerWindH.x < 0)
					{
						int neighborIndex = world.GetNeighborIndex(x, y, 0);
						neighborPressureDifferential += -lowerWindH.x * (lowerPressure - state.LowerAirPressure[neighborIndex]);
						neighborElevationDifferential += -lowerWindH.x * (state.Elevation[neighborIndex] + state.WaterAndIceDepth[neighborIndex] - elevationOrSeaLevel);
					}
					else
					{
						var neighborIndex = world.GetNeighborIndex(x, y, 1);
						neighborPressureDifferential += lowerWindH.x * (lowerPressure - state.LowerAirPressure[neighborIndex]);
						neighborElevationDifferential += lowerWindH.x * (state.Elevation[neighborIndex] + state.WaterAndIceDepth[neighborIndex] - elevationOrSeaLevel);
					}
					if (lowerWindH.y < 0)
					{
						var neighborIndex = world.GetNeighborIndex(x, y, 3);
						neighborPressureDifferential += -lowerWindH.y * (lowerPressure - state.LowerAirPressure[neighborIndex]);
						neighborElevationDifferential += -lowerWindH.y * (state.Elevation[neighborIndex] + state.WaterAndIceDepth[neighborIndex] - elevationOrSeaLevel);
					}
					else
					{
						var neighborIndex = world.GetNeighborIndex(x, y, 2);
						neighborPressureDifferential += lowerWindH.y * (lowerPressure - state.LowerAirPressure[neighborIndex]);
						neighborElevationDifferential += lowerWindH.y * (state.Elevation[neighborIndex] + state.WaterAndIceDepth[neighborIndex] - elevationOrSeaLevel);
					}
					var verticalTemperatureDifferential = lowerTemperatureAtSeaLevel - upperTemperatureAtSeaLevel;

					float lowerWindSpeedH = lowerWindH.magnitude;
					float lowerWindV = neighborElevationDifferential * world.Data.MountainUpdraftWindSpeed;
					lowerWindV += neighborPressureDifferential * world.Data.DestinationPressureDifferentialToVerticalWindSpeed; // thermal
					lowerWindV += (lowerPressure - upperPressure) * world.Data.PressureToVerticalWindSpeed; // thermal


					nextState.LowerWind[index] = new Vector3(lowerWindH.x, lowerWindH.y, lowerWindV);
					nextState.UpperWind[index] = new Vector3(upperWindH.x, upperWindH.y, 0);

					if (float.IsNaN(nextState.UpperWind[index].x) || float.IsNaN(nextState.LowerWind[index].x) || float.IsNaN(nextState.LowerWind[index].z))
					{
						Debug.DebugBreak();
					}

					if (world.IsOcean(state.WaterDepth[index]))
					{
						Vector2 densityDifferential = Vector2.zero;
						Vector2 shallowCurrentH;
						float shallowCurrentV = 0;

						if (iceCoverage < 1)
						{
							shallowCurrentH = GetCurrentHorizontal(world, x, y, latitude, state.PlanetRotationSpeed, windInfo.coriolisParam, windInfo.inverseCoriolisParam, lowerWindH);
							if (iceCoverage > 0)
							{
								shallowCurrentH *= 1.0f - iceCoverage;
							}
						}
						else
						{
							shallowCurrentH = Vector2.zero;
						}

						if (state.DeepWaterMass[index] > 0)
						{
							float density = state.DeepWaterDensity[index];
							for (int i = 0; i < 4; i++)
							{
								var neighbor = world.GetNeighbor(x, y, i);
								int nIndex = world.GetIndex(neighbor.x, neighbor.y);
								if (state.DeepWaterMass[nIndex] > 0)
								{
									//var neighborWind = state.Wind[nIndex];
									//nWind += neighborWind;

									switch (i)
									{
										case 0:
											densityDifferential.x += state.DeepWaterDensity[nIndex] - density;
											break;
										case 1:
											densityDifferential.x -= state.DeepWaterDensity[nIndex] - density;
											break;
										case 2:
											densityDifferential.y -= state.DeepWaterDensity[nIndex] - density;
											break;
										case 3:
											densityDifferential.y += state.DeepWaterDensity[nIndex] - density;
											break;
									}
								}
								else
								{
									//switch (i)
									//{
									//	case 0:
									//		shallowCurrentV += shallowCurrentH.x;
									//		if (shallowCurrentH.x < 0)
									//		{
									//			shallowCurrentH.x = 0;
									//		}
									//		break;
									//	case 1:
									//		shallowCurrentV -= shallowCurrentH.x;
									//		if (shallowCurrentH.x > 0)
									//		{
									//			shallowCurrentH.x = 0;
									//		}
									//		break;
									//	case 2:
									//		shallowCurrentV -= shallowCurrentH.y;
									//		if (shallowCurrentH.y > 0)
									//		{
									//			shallowCurrentH.y = 0;
									//		}
									//		break;
									//	case 3:
									//		shallowCurrentV += shallowCurrentH.y;
									//		if (shallowCurrentH.y < 0)
									//		{
									//			shallowCurrentH.y = 0;
									//		}
									//		break;
									//}
								}
							}
						}
						densityDifferential *= world.Data.OceanDensityCurrentSpeed;
						nextState.DeepWaterCurrent[index] = new Vector3(densityDifferential.x, densityDifferential.y, 0);
						nextState.ShallowWaterCurrent[index] = new Vector3(shallowCurrentH.x, shallowCurrentH.y, shallowCurrentV);
					} else
					{
						nextState.DeepWaterCurrent[index] = Vector3.zero;
						nextState.ShallowWaterCurrent[index] = Vector3.zero;
					}
				}
			}


			_ProfileWindTick.End();

		}


		static private Vector2 GetPressureGradient(World world, World.State state, int x, int y, float[] neighborPressure, float[] neighborTemperature, bool isUpper, float pressureAtWindElevation, float windElevation, float molarMass)
		{
			Vector2 pressureDifferential = Vector2.zero;
			Vector2 nWind = Vector2.zero;
			for (int i = 0; i < 4; i++)
			{
				var nIndex = world.GetNeighborIndex(x, y, i);
				// see bottom of: https://en.wikipedia.org/wiki/Vertical_pressure_variation
				float neighborElevation = state.Elevation[nIndex] + state.WaterAndIceDepth[nIndex];
				float neighborTemperatureElevation;
				if (isUpper)
				{
					neighborTemperatureElevation = neighborElevation + world.Data.BoundaryZoneElevation;
				} else
				{
					neighborTemperatureElevation = neighborElevation;
				}
				float neighborTemperatureAtSeaLevel = neighborTemperature[nIndex] - neighborTemperatureElevation * world.Data.TemperatureLapseRate;
				float neighborElevationAtPressure = neighborTemperatureAtSeaLevel / world.Data.TemperatureLapseRate * (Mathf.Pow(pressureAtWindElevation/ neighborPressure[nIndex], -1.0f / (world.Data.PressureExponent * molarMass)) - 1);


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
			return pressureDifferential;

		}

	}
}