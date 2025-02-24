﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Unity.Profiling;
using Unity.Jobs;
namespace Sim {
	static public class Atmosphere {

		static float Sqr(float x)
		{
			return x * x;
		}

		static ProfilerMarker _ProfileAtmosphereTick = new ProfilerMarker("Atmosphere Tick");
		static ProfilerMarker _ProfileAtmosphereMoveH = new ProfilerMarker("Atmosphere Move H");
		static ProfilerMarker _ProfileAtmosphereMoveV = new ProfilerMarker("Atmosphere Move V");
		static ProfilerMarker _ProfileAtmosphereDiffusion = new ProfilerMarker("Atmosphere Diffusion");
		static ProfilerMarker _ProfileAtmosphereEnergyBudget = new ProfilerMarker("Atmosphere Energy Budget");
		static ProfilerMarker _ProfileAtmosphereSetup = new ProfilerMarker("Atmosphere Setup");
		static ProfilerMarker _ProfileAtmosphereFinal = new ProfilerMarker("Atmosphere Final");

		const float inversePI = 1.0f / Mathf.PI;
		const float PIOver2 = Mathf.PI / 2;

		static public void Tick(World world, World.State state, World.State nextState)
		{
			_ProfileAtmosphereTick.Begin();

			float globalEnergyGained = 0;
			float globalEnergyUpperAir = 0;
			float globalEnergyLowerAir = 0;
			float globalEnergyShallowWater = 0;
			float globalEnergyDeepWater = 0;
			float globalEnergyLand = 0;
			float globalIceMass = 0;

			float globalEnergyIncoming = 0;
			float globalEnergySolarReflectedAtmosphere = 0;
			float globalSolarEnergyReflectedClouds = 0;
			float globalEnergySolarReflectedSurface = 0;
			float globalEnergySolarAbsorbedClouds = 0;
			float globalEnergySolarAbsorbedAtmosphere = 0;
			float globalEnergySolarAbsorbedSurface = 0;
			float globalEnergySolarAbsorbedOcean = 0;
			float globalEnergyThermalOceanRadiation = 0;
			float globalEnergyOceanConduction = 0;
			float globalEnergyEvapotranspiration = 0;
			float globalEnergyThermalSurfaceRadiation = 0;
			float globalEnergyThermalBackRadiation = 0;
			float globalEnergySurfaceConduction = 0;
			float globalEnergyThermalOutAtmosphericWindow = 0;
			float globalEnergyThermalOutAtmosphere = 0;
			float globalEnergyThermalAbsorbedAtmosphere = 0;
			float globalOceanCoverage = 0;
			float globalCloudCoverage = 0;
			float globalOceanVolume = 0;
			float globalTemperature = 0;
			float globalEvaporation = 0;
			float globalRainfall = 0;
			float globalCloudMass = 0;
			float globalWaterVapor = 0;
			float atmosphericMass = 0;
			float seaLevel = 0;
			int seaLevelTiles = 0;

			float inverseFullCanopyCoverage = 1.0f / world.Data.FullCanopyCoverage;
			float inverseFullWaterCoverage = 1.0f / world.Data.FullWaterCoverage;
			float inverseFullIceCoverage = 1.0f / (world.Data.MassIce * world.Data.FullIceCoverage);
			float inverseSpecificHeatIce = 1.0f / world.Data.SpecificHeatIce;
			float inverseCloudMassFullAbsorption = 1.0f / world.Data.CloudMassFullAbsorption;
			float inverseWorldSize = 1.0f / world.Size;
			float inverseBoundaryZoneElevation = 1.0f / world.Data.BoundaryZoneElevation;
			float metersPerSecondToTilesPerTick = world.Data.SecondsPerTick / world.Data.MetersPerTile;
			float wattsToKJPerTick = world.Data.SecondsPerTick * 1000;
			float timeOfYear = world.GetTimeOfYear(state.Ticks);
			float declinationOfSun = GetDeclinationOfSun(state.PlanetTiltAngle, timeOfYear);
			float sunHitsAtmosphereBelowHorizonAmount = 0.055f;
			float inverseSunAtmosphereAmount = 1.0f / (1.0f + sunHitsAtmosphereBelowHorizonAmount);
			float inverseDewPointTemperatureRange = 1.0f / world.Data.DewPointTemperatureRange;
			float inverseEvapTemperatureRange = 1.0f / world.Data.EvapTemperatureRange;

			for (int y = 0; y < world.Size; y++)
			{
				float latitude = world.GetLatitude(y);
				float lengthOfDay = GetLengthOfDay(latitude, timeOfYear, declinationOfSun);

				for (int x = 0; x < world.Size; x++)
				{
					_ProfileAtmosphereSetup.Begin();

					int index = world.GetIndex(x, y);


					float elevation = state.Elevation[index];
					float waterDepth = state.WaterDepth[index];
					float iceMass = state.IceMass[index];
					float waterAndIceDepth = state.WaterAndIceDepth[index];
					float elevationOrSeaLevel = elevation+ waterAndIceDepth;
					float landEnergy = state.LandEnergy[index];
					float cloudMass = state.CloudMass[index];
					float rainDropMass = state.RainDropMass[index];
					float lowerAirTemperature = state.LowerAirTemperature[index];
					float lowerAirEnergy = state.LowerAirEnergy[index];
					float lowerAirPressure = state.LowerAirPressure[index];
					float lowerAirMass = state.LowerAirMass[index];
					float upperAirTemperature = state.UpperAirTemperature[index];
					float upperAirEnergy = state.UpperAirEnergy[index];
					float upperAirPressure = state.UpperAirPressure[index];
					float upperAirMass = state.UpperAirMass[index];
					float humidity = state.Humidity[index];
					float waterTableDepth = state.WaterTableDepth[index];
					float soilFertility = state.SoilFertility[index];
					float radiation = 0.001f;
					var lowerWind = state.LowerWind[index];
					var upperWind = state.UpperWind[index];
					var currentDeep = state.DeepWaterCurrent[index];
					var currentShallow = state.ShallowWaterCurrent[index];
					var shallowWaterFlow = state.ShallowWaterFlow[index];
					var flowDirectionGroundWater = state.FlowDirectionGroundWater[index];
					float groundWater = state.GroundWater[index];
					float canopy = state.Canopy[index];

					float deepWaterEnergy = 0;
					float shallowWaterEnergy = 0;
					float shallowWaterMass = 0;
					float deepWaterDensity =0;
					float deepSaltMass=0;
					float shallowSaltMass=0;
					float shallowWaterTemperature=0;
					float deepWaterTemperature=0;

					shallowWaterMass = state.ShallowWaterMass[index];
					if (shallowWaterMass > 0)
					{
						shallowWaterEnergy = state.ShallowWaterEnergy[index];
						shallowSaltMass = state.ShallowSaltMass[index];
						shallowWaterTemperature = state.ShallowWaterTemperature[index];
					}
					float deepWaterMass = state.DeepWaterMass[index];
					if (deepWaterMass > 0)
					{
						deepWaterEnergy = state.DeepWaterEnergy[index];
						deepSaltMass = state.DeepSaltMass[index];
						deepWaterDensity = state.DeepWaterDensity[index];
						deepWaterTemperature = GetWaterTemperature(world, deepWaterEnergy, deepWaterMass, deepSaltMass);
					}

					if (deepWaterMass > 0)
					{
						seaLevel += elevationOrSeaLevel;
						seaLevelTiles++;
					}

					float evaporation = 0;
					float newGroundWater = 0;
					float newIceMass = iceMass;
					float newRainfall = 0;
					float newRadiation = radiation;
					float inverseMassOfAtmosphericColumn = 1.0f / (upperAirMass + lowerAirMass);
					float iceCoverage = Mathf.Min(1.0f, Mathf.Pow(iceMass * inverseFullIceCoverage, 0.6667f));
					float waterCoverage = Mathf.Min(1.0f, Mathf.Pow(waterDepth * inverseFullWaterCoverage, 0.6667f));
					float canopyCoverage = Mathf.Min(1.0f, Mathf.Pow(canopy * inverseFullCanopyCoverage, 0.6667f));
					float relativeHumidity = GetRelativeHumidity(world, lowerAirTemperature, humidity, lowerAirMass, inverseDewPointTemperatureRange);
					float dewPoint = GetDewPoint(world, lowerAirTemperature, relativeHumidity);
					float cloudElevation = GetCloudElevation(world, upperAirTemperature, dewPoint, elevationOrSeaLevel);
					float cloudCoverage = Math.Min(1.0f, Mathf.Pow(cloudMass * inverseCloudMassFullAbsorption, 0.6667f)); // bottom surface of volume
					float longitude = (float)x * inverseWorldSize;
					float sunAngle;
					Vector3 sunVector;
					GetSunVector(world, state.PlanetTiltAngle, state.Ticks, latitude, longitude, out sunAngle, out sunVector);
					sunAngle = Math.Max(0, sunAngle);
					float waterSlopeAlbedo = Mathf.Pow(1.0f - Math.Max(0, sunVector.z), 9);
					//float groundWaterSaturation = Animals.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], soilFertility * world.Data.MaxSoilPorousness);
					float solarRadiationAbsorbed = 0;
					globalCloudCoverage += cloudCoverage;
					globalTemperature += lowerAirTemperature;
					float evapRate = GetEvaporationRate(world, iceMass, lowerAirTemperature, relativeHumidity, inverseEvapTemperatureRange);

					if (waterCoverage > 0)
					{
						globalOceanCoverage += waterCoverage;
						// TODO: this currently includes ice.... should it?
						globalOceanVolume += waterDepth * world.Data.MetersPerTile * world.Data.MetersPerTile / 1000000000;
					}


					float newLandEnergy = 0;
					float newLowerAirEnergy = 0;
					float newUpperAirEnergy = 0;
					float newLowerAirMass = 0;
					float newUpperAirMass = 0;
					float newHumidity = 0;
					float newCloudMass = 0;
					float newRainDropMass = 0;
					float newShallowWaterEnergy = 0;
					float newDeepWaterEnergy = 0;
					float newShallowSaltMass = 0;
					float newDeepSaltMass = 0;
					float newShallowWaterMass = 0;
					float newDeepWaterMass = 0;


					_ProfileAtmosphereSetup.End();


					_ProfileAtmosphereMoveH.Begin();
					{
						// Upper atmosphere

						Vector2 movePos = new Vector3(x, y, 0) + upperWind * metersPerSecondToTilesPerTick;
						movePos.x = RepeatExclusive(movePos.x, world.Size);
						movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
						int x0 = (int)movePos.x;
						int y0 = (int)movePos.y;
						int x1 = (x0 + 1) % world.Size;
						int y1 = Mathf.Min(y0 + 1, world.Size - 1);
						float xT = movePos.x - x0;
						float yT = movePos.y - y0;

						int i0 = world.GetIndex(x0, y0);
						int i1 = world.GetIndex(x0, y1);
						int i2 = world.GetIndex(x1, y0);
						int i3 = world.GetIndex(x1, y1);

						float move0 = (1.0f - xT) * (1.0f - yT);
						float move1 = (1.0f - xT) * yT;
						float move2 = xT * (1.0f - yT);
						float move3 = xT * yT;

						nextState.CloudMass[i0] += cloudMass * move0;
						nextState.CloudMass[i1] += cloudMass * move1;
						nextState.CloudMass[i2] += cloudMass * move2;
						nextState.CloudMass[i3] += cloudMass * move3;

						nextState.RainDropMass[i0] += rainDropMass * move0;
						nextState.RainDropMass[i1] += rainDropMass * move1;
						nextState.RainDropMass[i2] += rainDropMass * move2;
						nextState.RainDropMass[i3] += rainDropMass * move3;

						float contentMove = upperAirMass * world.Data.WindAirMovementHorizontal;
						nextState.UpperAirMass[index] += upperAirMass * (1.0f - world.Data.WindAirMovementHorizontal);
						nextState.UpperAirMass[i0] += contentMove * move0;
						nextState.UpperAirMass[i1] += contentMove * move1;
						nextState.UpperAirMass[i2] += contentMove * move2;
						nextState.UpperAirMass[i3] += contentMove * move3;

						contentMove = upperAirEnergy * world.Data.WindAirMovementHorizontal;
						nextState.UpperAirEnergy[index] += upperAirEnergy * (1.0f - world.Data.WindAirMovementHorizontal);
						nextState.UpperAirEnergy[i0] += contentMove * move0;
						nextState.UpperAirEnergy[i1] += contentMove * move1;
						nextState.UpperAirEnergy[i2] += contentMove * move2;
						nextState.UpperAirEnergy[i3] += contentMove * move3;

						// lower atmosphere
						movePos = new Vector3(x, y, 0) + lowerWind * metersPerSecondToTilesPerTick;
						movePos.x = RepeatExclusive(movePos.x, world.Size);
						movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
						x0 = (int)movePos.x;
						y0 = (int)movePos.y;
						x1 = (x0 + 1) % world.Size;
						y1 = Mathf.Min(y0 + 1, world.Size - 1);
						xT = movePos.x - x0;
						yT = movePos.y - y0;

						i0 = world.GetIndex(x0, y0);
						i1 = world.GetIndex(x0, y1);
						i2 = world.GetIndex(x1, y0);
						i3 = world.GetIndex(x1, y1);

						move0 = (1.0f - xT) * (1.0f - yT);
						move1 = (1.0f - xT) * yT;
						move2 = xT * (1.0f - yT);
						move3 = xT * yT;

						contentMove = lowerAirMass * world.Data.WindAirMovementHorizontal;
						nextState.LowerAirMass[index] += lowerAirMass * (1.0f - world.Data.WindAirMovementHorizontal);
						nextState.LowerAirMass[i0] += contentMove * move0;
						nextState.LowerAirMass[i1] += contentMove * move1;
						nextState.LowerAirMass[i2] += contentMove * move2;
						nextState.LowerAirMass[i3] += contentMove * move3;

						contentMove = lowerAirEnergy * world.Data.WindAirMovementHorizontal;
						nextState.LowerAirEnergy[index] += lowerAirEnergy * (1.0f - world.Data.WindAirMovementHorizontal);
						nextState.LowerAirEnergy[i0] += contentMove * move0;
						nextState.LowerAirEnergy[i1] += contentMove * move1;
						nextState.LowerAirEnergy[i2] += contentMove * move2;
						nextState.LowerAirEnergy[i3] += contentMove * move3;

						contentMove = humidity * world.Data.WindHumidityMovement;
						nextState.Humidity[index] += humidity * (1.0f - world.Data.WindHumidityMovement);
						nextState.Humidity[i0] += contentMove * move0;
						nextState.Humidity[i1] += contentMove * move1;
						nextState.Humidity[i2] += contentMove * move2;
						nextState.Humidity[i3] += contentMove * move3;



						//surface ocean
						if (shallowWaterMass > 0)
						{
							if (shallowWaterFlow.x > 0)
							{
								var nIndex = world.GetNeighborIndex(index, 0);
								nextState.ShallowWaterMass[nIndex] += shallowWaterFlow.x * shallowWaterMass;
								nextState.ShallowSaltMass[nIndex] += shallowWaterFlow.x * shallowSaltMass;
								nextState.ShallowWaterEnergy[nIndex] += shallowWaterFlow.x * shallowWaterEnergy;
							}
							if (shallowWaterFlow.y > 0)
							{
								var nIndex = world.GetNeighborIndex(index, 1);
								nextState.ShallowWaterMass[nIndex] += shallowWaterFlow.y * shallowWaterMass;
								nextState.ShallowSaltMass[nIndex] += shallowWaterFlow.y * shallowSaltMass;
								nextState.ShallowWaterEnergy[nIndex] += shallowWaterFlow.y * shallowWaterEnergy;
							}
							if (shallowWaterFlow.z > 0)
							{
								var nIndex = world.GetNeighborIndex(index, 2);
								nextState.ShallowWaterMass[nIndex] += shallowWaterFlow.z * shallowWaterMass;
								nextState.ShallowSaltMass[nIndex] += shallowWaterFlow.z * shallowSaltMass;
								nextState.ShallowWaterEnergy[nIndex] += shallowWaterFlow.z * shallowWaterEnergy;
							}
							if (shallowWaterFlow.w > 0)
							{
								var nIndex = world.GetNeighborIndex(index, 3);
								nextState.ShallowWaterMass[nIndex] += shallowWaterFlow.w * shallowWaterMass;
								nextState.ShallowSaltMass[nIndex] += shallowWaterFlow.w * shallowSaltMass;
								nextState.ShallowWaterEnergy[nIndex] += shallowWaterFlow.w * shallowWaterEnergy;
							}

							float shallowWaterMassFlowPercent = shallowWaterFlow.x + shallowWaterFlow.y + shallowWaterFlow.z + shallowWaterFlow.w;
							float shallowWaterMassCurrentPercent = world.Data.OceanCurrentSpeed * (1.0f - shallowWaterMassFlowPercent);

							nextState.ShallowWaterMass[index] += shallowWaterMass * (1.0f - (shallowWaterMassCurrentPercent + shallowWaterMassFlowPercent));
							nextState.ShallowSaltMass[index] += shallowSaltMass * (1.0f - (shallowWaterMassCurrentPercent + shallowWaterMassFlowPercent));
							nextState.ShallowWaterEnergy[index] += shallowWaterEnergy * (1.0f - (shallowWaterMassCurrentPercent + shallowWaterMassFlowPercent));

							movePos = new Vector3(x, y, 0) + currentShallow * metersPerSecondToTilesPerTick;
							movePos.x = RepeatExclusive(movePos.x, world.Size);
							movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
							x0 = (int)movePos.x;
							y0 = (int)movePos.y;
							x1 = (x0 + 1) % world.Size;
							y1 = Mathf.Min(y0 + 1, world.Size - 1);
							xT = movePos.x - x0;
							yT = movePos.y - y0;

							i0 = world.GetIndex(x0, y0);
							i1 = world.GetIndex(x0, y1);
							i2 = world.GetIndex(x1, y0);
							i3 = world.GetIndex(x1, y1);
							if (!world.IsOcean(state.WaterDepth[i0]) && state.Elevation[i0] >= elevationOrSeaLevel) i0 = index;
							if (!world.IsOcean(state.WaterDepth[i1]) && state.Elevation[i0] >= elevationOrSeaLevel) i1 = index;
							if (!world.IsOcean(state.WaterDepth[i2]) && state.Elevation[i0] >= elevationOrSeaLevel) i2 = index;
							if (!world.IsOcean(state.WaterDepth[i3]) && state.Elevation[i0] >= elevationOrSeaLevel) i3 = index;

							move0 = (1.0f - xT) * (1.0f - yT);
							move1 = (1.0f - xT) * yT;
							move2 = xT * (1.0f - yT);
							move3 = xT * yT;

							contentMove = shallowWaterMass * shallowWaterMassCurrentPercent;
							nextState.ShallowWaterMass[i0] += contentMove * move0;
							nextState.ShallowWaterMass[i1] += contentMove * move1;
							nextState.ShallowWaterMass[i2] += contentMove * move2;
							nextState.ShallowWaterMass[i3] += contentMove * move3;

							contentMove = shallowWaterEnergy * shallowWaterMassCurrentPercent;
							nextState.ShallowWaterEnergy[i0] += contentMove * move0;
							nextState.ShallowWaterEnergy[i1] += contentMove * move1;
							nextState.ShallowWaterEnergy[i2] += contentMove * move2;
							nextState.ShallowWaterEnergy[i3] += contentMove * move3;

							contentMove = shallowSaltMass * shallowWaterMassCurrentPercent;
							nextState.ShallowSaltMass[i0] += contentMove * move0;
							nextState.ShallowSaltMass[i1] += contentMove * move1;
							nextState.ShallowSaltMass[i2] += contentMove * move2;
							nextState.ShallowSaltMass[i3] += contentMove * move3;

							//deep ocean
							if (deepWaterMass > 0)
							{
								movePos = new Vector3(x, y, 0) + currentDeep * metersPerSecondToTilesPerTick;
								movePos.x = RepeatExclusive(movePos.x, world.Size);
								movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
								x0 = (int)movePos.x;
								y0 = (int)movePos.y;
								x1 = (x0 + 1) % world.Size;
								y1 = Mathf.Min(y0 + 1, world.Size - 1);
								xT = movePos.x - x0;
								yT = movePos.y - y0;

								i0 = world.GetIndex(x0, y0);
								i1 = world.GetIndex(x0, y1);
								i2 = world.GetIndex(x1, y0);
								i3 = world.GetIndex(x1, y1);
								if (!world.IsOcean(state.DeepWaterMass[i0])) i0 = index;
								if (!world.IsOcean(state.DeepWaterMass[i1])) i1 = index;
								if (!world.IsOcean(state.DeepWaterMass[i2])) i2 = index;
								if (!world.IsOcean(state.DeepWaterMass[i3])) i3 = index;

								move0 = (1.0f - xT) * (1.0f - yT);
								move1 = (1.0f - xT) * yT;
								move2 = xT * (1.0f - yT);
								move3 = xT * yT;

								contentMove = deepWaterMass * world.Data.OceanCurrentSpeed;
								nextState.DeepWaterMass[index] += deepWaterMass * (1.0f - world.Data.OceanCurrentSpeed);
								nextState.DeepWaterMass[i0] += contentMove * move0;
								nextState.DeepWaterMass[i1] += contentMove * move1;
								nextState.DeepWaterMass[i2] += contentMove * move2;
								nextState.DeepWaterMass[i3] += contentMove * move3;

								contentMove = deepWaterEnergy * world.Data.OceanCurrentSpeed;
								nextState.DeepWaterEnergy[index] += deepWaterEnergy * (1.0f - world.Data.OceanCurrentSpeed);
								nextState.DeepWaterEnergy[i0] += contentMove * move0;
								nextState.DeepWaterEnergy[i1] += contentMove * move1;
								nextState.DeepWaterEnergy[i2] += contentMove * move2;
								nextState.DeepWaterEnergy[i3] += contentMove * move3;

								contentMove = deepSaltMass * world.Data.OceanCurrentSpeed;
								nextState.DeepSaltMass[index] += deepSaltMass * (1.0f - world.Data.OceanCurrentSpeed);
								nextState.DeepSaltMass[i0] += contentMove * move0;
								nextState.DeepSaltMass[i1] += contentMove * move1;
								nextState.DeepSaltMass[i2] += contentMove * move2;
								nextState.DeepSaltMass[i3] += contentMove * move3;
							}
						}

						if (groundWater > 0)
						{
							//ground water
							movePos = new Vector2(x, y) + flowDirectionGroundWater * metersPerSecondToTilesPerTick;
							movePos.x = RepeatExclusive(movePos.x, world.Size);
							movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
							x0 = (int)movePos.x;
							y0 = (int)movePos.y;
							x1 = (x0 + 1) % world.Size;
							y1 = Mathf.Min(y0 + 1, world.Size - 1);
							xT = movePos.x - x0;
							yT = movePos.y - y0;

							i0 = world.GetIndex(x0, y0);
							i1 = world.GetIndex(x0, y1);
							i2 = world.GetIndex(x1, y0);
							i3 = world.GetIndex(x1, y1);

							move0 = (1.0f - xT) * (1.0f - yT);
							move1 = (1.0f - xT) * yT;
							move2 = xT * (1.0f - yT);
							move3 = xT * yT;

							// TODO: move ground water energy

							nextState.GroundWater[i0] += groundWater * move0;
							nextState.GroundWater[i1] += groundWater * move1;
							nextState.GroundWater[i2] += groundWater * move2;
							nextState.GroundWater[i3] += groundWater * move3;
						}
					}
					_ProfileAtmosphereMoveH.End();




					_ProfileAtmosphereEnergyBudget.Begin();

					// SOLAR RADIATION

					float solarRadiation = state.SolarRadiation * world.Data.SecondsPerTick * Mathf.Max(0, (sunVector.z + sunHitsAtmosphereBelowHorizonAmount) * inverseSunAtmosphereAmount);

					// get the actual atmospheric depth here based on radius of earth plus atmosphere
					//float inverseSunAngle = PIOver2 + sunAngle;
					//float angleFromSunToLatitudeAndAtmophereEdge = Mathf.Asin(state.PlanetRadius * Mathf.Sin(inverseSunAngle) / (state.PlanetRadius + world.Data.TropopauseElevation));
					//float angleFromPlanetCenterToLatitudeAndAtmosphereEdge = Mathf.PI - inverseSunAngle - angleFromSunToLatitudeAndAtmophereEdge;
					//float atmosphericDepthInMeters = Mathf.Sin(angleFromPlanetCenterToLatitudeAndAtmosphereEdge) * state.PlanetRadius / Mathf.Sin(angleFromSunToLatitudeAndAtmophereEdge);
					//float atmosphericDepth = Mathf.Max(1.0f, atmosphericDepthInMeters / world.Data.TropopauseElevation);

					float atmosphericDepth = 1.0f + sunVector.y;

					// These constants obtained here, dunno if I've interpreted them correctly
					// https://www.pveducation.org/pvcdrom/properties-of-sunlight/air-mass

					////// MAJOR TODO:
					///// USE THIS LINK: https://www.ftexploring.com/solar-energy/sun-angle-and-insolation2.htm
					/// With the sun 90 degrees above the horizon (SEA° = 90°), the air mass lowers the intensity of the sunlight from the 1,367 W / m2 that it is in outerspace down to about 1040 W / m2.
					//				float consumedByAtmosphere = 1.0f - Mathf.Pow(0.7f, Mathf.Pow(atmosphericDepth, 0.678f));


					if (solarRadiation > 0)
					{
						globalEnergyIncoming += solarRadiation;

						// TODO: reflect/absorb more in the atmosphere with a lower sun angle

						// reflect some rads off atmosphere and clouds
						// TODO: this process feels a little broken -- are we giving too much priority to reflecting/absorbing in certain layers?
						float energyReflectedAtmosphere = solarRadiation * Mathf.Min(1, world.Data.AtmosphericHeatReflection * (upperAirMass + lowerAirMass));
						solarRadiation -= energyReflectedAtmosphere;
						globalEnergySolarReflectedAtmosphere += energyReflectedAtmosphere;

						if (cloudMass > 0)
						{
							float minCloudFreezingTemperature = 243;
							float maxCloudFreezingTemperature = 303;
							float maxCloudSlopeAlbedo = 0.1f;
							float rainDropSizeAlbedoMin = 0.5f;
							float rainDropSizeAlbedoMax = 1.0f;
							float cloudAlbedo = 2.0f;
							float cloudTemperatureAlbedo = world.Data.AlbedoIce + (world.Data.AlbedoWater - world.Data.AlbedoIce) * Mathf.Clamp01((dewPoint - minCloudFreezingTemperature) / (maxCloudFreezingTemperature - minCloudFreezingTemperature));
							float rainDropSizeAlbedo = Mathf.Clamp01(1.0f - rainDropMass / cloudMass) * (rainDropSizeAlbedoMax - rainDropSizeAlbedoMin) + rainDropSizeAlbedoMin;
							float cloudReflectivity = Mathf.Min(1.0f, cloudAlbedo * cloudTemperatureAlbedo * cloudMass * rainDropSizeAlbedo / Mathf.Max(maxCloudSlopeAlbedo, 1.0f - waterSlopeAlbedo));
							float energyReflectedClouds = solarRadiation * cloudReflectivity;
							solarRadiation -= energyReflectedClouds;

							float absorbedByCloudsIncoming = solarRadiation * Mathf.Min(1.0f, world.Data.AtmosphericHeatAbsorption * cloudMass);
							solarRadiation -= absorbedByCloudsIncoming;
							newUpperAirEnergy += absorbedByCloudsIncoming;
							globalEnergySolarAbsorbedClouds += absorbedByCloudsIncoming;
							globalEnergySolarAbsorbedAtmosphere += absorbedByCloudsIncoming;
							globalSolarEnergyReflectedClouds += energyReflectedClouds;
						}

						// Absorbed by atmosphere
						// stratosphere accounts for about a quarter of atmospheric mass
						//	float absorbedByStratosphere = incomingRadiation * world.Data.AtmosphericHeatAbsorption * (state.StratosphereMass / massOfAtmosphericColumn);

						float upperAtmosphereAbsorptionRate = Mathf.Min(1, world.Data.AtmosphericHeatAbsorption * upperAirMass);
						float absorbedByUpperAtmosphereIncoming = solarRadiation * upperAtmosphereAbsorptionRate * atmosphericDepth;
						solarRadiation -= absorbedByUpperAtmosphereIncoming;
						newUpperAirEnergy += absorbedByUpperAtmosphereIncoming;


						float lowerAtmosphereAbsorptionRate = Mathf.Min(1, world.Data.AtmosphericHeatAbsorption * (lowerAirMass + humidity));
						float absorbedByLowerAtmosphereIncoming = solarRadiation * lowerAtmosphereAbsorptionRate * atmosphericDepth;

						newLowerAirEnergy += absorbedByLowerAtmosphereIncoming;
						solarRadiation -= absorbedByLowerAtmosphereIncoming;
						globalEnergySolarAbsorbedAtmosphere += absorbedByLowerAtmosphereIncoming + absorbedByUpperAtmosphereIncoming;

						// reflection off surface
						float energyReflected = 0;
						{
							if (iceCoverage > 0)
							{
								energyReflected += solarRadiation * iceCoverage * GetAlbedo(world.Data.AlbedoIce, 0);
							}
							if (waterCoverage > 0)
							{
								energyReflected += waterCoverage * solarRadiation * GetAlbedo(world.Data.AlbedoWater, waterSlopeAlbedo) * (1.0f - iceCoverage);
							}
							if (waterCoverage < 1 && iceCoverage < 1)
							{
								// reflect some incoming radiation
								float slopeAlbedo = 0;
								float soilReflectivity = GetAlbedo(world.Data.AlbedoLand - world.Data.AlbedoReductionSoilQuality * soilFertility, slopeAlbedo);
								float heatReflectedLand = canopyCoverage * world.Data.AlbedoFoliage + Math.Max(0, 1.0f - canopyCoverage) * soilReflectivity;
								energyReflected += solarRadiation * Mathf.Clamp01(heatReflectedLand) * (1.0f - iceCoverage) * (1.0f - waterCoverage);
							}
							solarRadiation -= energyReflected;

							// TODO: do we absorb some of this energy on the way back out of the atmosphere?
							//					newLowerAirEnergy += energyReflected;
							globalEnergySolarReflectedSurface += energyReflected;
						}

						solarRadiationAbsorbed += solarRadiation;

					}

					// THERMAL RADIATION


					float backRadiation = 0;
					float reflected = 0;

					// radiate heat from land
					// TODO: deal with the fact that this also incorporates ground water
					float shallowWaterRadiation = GetRadiationRate(world, shallowWaterTemperature, world.Data.EmissivityWater) * world.Data.SecondsPerTick * waterCoverage;
					float soilEnergy = landEnergy - groundWater * world.Data.maxGroundWaterTemperature * world.Data.SpecificHeatWater;
					float radiationRate = GetLandRadiationRate(world, landEnergy, groundWater, soilFertility, canopyCoverage);
					float thermalEnergyRadiatedLand = Mathf.Min(soilEnergy, radiationRate * world.Data.SecondsPerTick);
					newLandEnergy += state.GeothermalHeat * world.Data.SecondsPerTick - thermalEnergyRadiatedLand;

					float thermalEnergyRadiatedToIce = 0;
					float thermalEnergyRadiatedToShallowWater = 0;
					float thermalEnergyRadiatedToAir = 0;
					if (deepWaterMass > 0)
					{
						float deepWaterRadiation = GetRadiationRate(world, deepWaterTemperature, world.Data.EmissivityWater) * world.Data.SecondsPerTick;
						newDeepWaterEnergy += thermalEnergyRadiatedLand - deepWaterRadiation;
						newLandEnergy += deepWaterRadiation;
					}
					else
					{
						newShallowWaterEnergy -= shallowWaterRadiation;
						newLandEnergy += shallowWaterRadiation;

						thermalEnergyRadiatedToShallowWater += thermalEnergyRadiatedLand * waterCoverage;
						thermalEnergyRadiatedToIce += iceCoverage * (thermalEnergyRadiatedLand - thermalEnergyRadiatedToShallowWater);
						thermalEnergyRadiatedToAir += thermalEnergyRadiatedLand - thermalEnergyRadiatedToShallowWater - thermalEnergyRadiatedToIce;
						newShallowWaterEnergy += thermalEnergyRadiatedToShallowWater;
					}

					// radiate from ice to air above
					if (iceCoverage > 0)
					{
						float thermalEnergyRadiatedFromIce = iceCoverage * GetRadiationRate(world, world.Data.FreezingTemperature, world.Data.EmissivityIce) * world.Data.SecondsPerTick;
						thermalEnergyRadiatedToAir += thermalEnergyRadiatedFromIce;
						thermalEnergyRadiatedToIce -= thermalEnergyRadiatedFromIce;
					}

					// lose heat to air via conduction AND radiation
					if (iceCoverage < 1)
					{
						// radiate heat, will be absorbed by air
						// Net Back Radiation: The ocean transmits electromagnetic radiation into the atmosphere in proportion to the fourth power of the sea surface temperature(black-body radiation)
						// https://eesc.columbia.edu/courses/ees/climate/lectures/o_atm.html
						float oceanRadiation = Mathf.Min(shallowWaterEnergy, shallowWaterRadiation);
						newShallowWaterEnergy -= oceanRadiation;
						thermalEnergyRadiatedToIce += oceanRadiation * iceCoverage;
						thermalEnergyRadiatedToAir += oceanRadiation - thermalEnergyRadiatedToIce;
						globalEnergyThermalOceanRadiation += oceanRadiation;
					}

					// TODO: track and emit heat from ice

					float lowerAtmosphereHeight = world.Data.BoundaryZoneElevation;
					float upperAtmosphereHeight = world.Data.TropopauseElevation - world.Data.BoundaryZoneElevation - elevationOrSeaLevel;
					float upperAtmosphereEmissivity = GetAtmosphericEmissivity(world, upperAirMass, upperAirMass * state.CarbonDioxide, 0, cloudMass);
					float lowerAtmosphereEmissivity = GetAtmosphericEmissivity(world, lowerAirMass, lowerAirMass * state.CarbonDioxide, humidity, 0);
					upperAtmosphereEmissivity = 0.5f;
					lowerAtmosphereEmissivity = 0.5f;
					float upperAtmosphereInfraredAbsorption = upperAtmosphereEmissivity;
					float lowerAtmosphereInfraredAbsorption = lowerAtmosphereEmissivity;
					float emittedByAtmosphere = 0;
					float surfaceEnergyReflected = 0;

					// Thermal energy from surface to air, space, reflected off clouds
					{
						globalEnergyThermalSurfaceRadiation += thermalEnergyRadiatedToAir;
						float energyThroughAtmosphericWindow = thermalEnergyRadiatedToAir * world.Data.EnergyLostThroughAtmosphereWindow;
						thermalEnergyRadiatedToAir -= energyThroughAtmosphericWindow;
						globalEnergyThermalOutAtmosphericWindow += energyThroughAtmosphericWindow;

						float absorbed = thermalEnergyRadiatedToAir * lowerAtmosphereInfraredAbsorption;
						thermalEnergyRadiatedToAir -= absorbed;
						newLowerAirEnergy += absorbed;
						globalEnergyThermalAbsorbedAtmosphere += absorbed;

						absorbed = thermalEnergyRadiatedToAir * upperAtmosphereInfraredAbsorption;
						thermalEnergyRadiatedToAir -= absorbed;
						newUpperAirEnergy += absorbed;
						globalEnergyThermalAbsorbedAtmosphere += absorbed;

						surfaceEnergyReflected = thermalEnergyRadiatedToAir * cloudCoverage * world.Data.CloudOutgoingReflectionRate;
						reflected += surfaceEnergyReflected;
						globalEnergyThermalOutAtmosphere += thermalEnergyRadiatedToAir - surfaceEnergyReflected;
					}

					// lower atmosphere radiation
					{
						float lowerEnergyEmitted = GetRadiationRate(world, lowerAirTemperature, lowerAtmosphereEmissivity) * world.Data.SecondsPerTick;
						newLowerAirEnergy -= 2 * lowerEnergyEmitted;
						emittedByAtmosphere += 2 * lowerEnergyEmitted;

						backRadiation += lowerEnergyEmitted;

						float energyThroughAtmosphericWindow = lowerEnergyEmitted * world.Data.EnergyLostThroughAtmosphereWindow;
						lowerEnergyEmitted -= energyThroughAtmosphericWindow;
						globalEnergyThermalOutAtmosphericWindow += energyThroughAtmosphericWindow;

						float absorbed = lowerEnergyEmitted * upperAtmosphereInfraredAbsorption;
						newUpperAirEnergy += absorbed;
						lowerEnergyEmitted -= absorbed;

						float lowerEnergyReflected = lowerEnergyEmitted * cloudCoverage * world.Data.CloudOutgoingReflectionRate;
						reflected += lowerEnergyReflected;
						globalEnergyThermalOutAtmosphere += lowerEnergyEmitted - lowerEnergyReflected;
					}

					// upper atmosphere radiation
					{
						float upperEnergyEmittedDown = GetRadiationRate(world, upperAirTemperature, upperAtmosphereEmissivity) * world.Data.SecondsPerTick;
						float upperEnergyEmittedUp = upperEnergyEmittedDown;
						newUpperAirEnergy -= upperEnergyEmittedUp + upperEnergyEmittedDown;
						emittedByAtmosphere += upperEnergyEmittedUp + upperEnergyEmittedDown;

						float energyThroughAtmosphericWindow = upperEnergyEmittedDown * world.Data.EnergyLostThroughAtmosphereWindow;
						backRadiation += energyThroughAtmosphericWindow;
						upperEnergyEmittedDown -= energyThroughAtmosphericWindow;

						energyThroughAtmosphericWindow = upperEnergyEmittedUp * world.Data.EnergyLostThroughAtmosphereWindow;
						upperEnergyEmittedUp -= energyThroughAtmosphericWindow;
						globalEnergyThermalOutAtmosphericWindow += energyThroughAtmosphericWindow;

						float upperEnergyReflected = upperEnergyEmittedUp * cloudCoverage * world.Data.CloudOutgoingReflectionRate;
						reflected += upperEnergyReflected;
						globalEnergyThermalOutAtmosphere += upperEnergyEmittedUp - upperEnergyReflected;

						float absorbed = upperEnergyEmittedDown * lowerAtmosphereInfraredAbsorption;
						newLowerAirEnergy += absorbed;

						backRadiation += upperEnergyEmittedDown - absorbed;
					}

					// reflected thermal radiation
					{
						float absorbed = reflected * upperAtmosphereInfraredAbsorption;
						newUpperAirEnergy += absorbed;
						reflected -= absorbed;

						absorbed = reflected * lowerAtmosphereInfraredAbsorption;
						newLowerAirEnergy += absorbed;
						reflected -= absorbed;

						globalEnergyThermalAbsorbedAtmosphere += surfaceEnergyReflected * upperAtmosphereInfraredAbsorption + surfaceEnergyReflected * (1.0f - upperAtmosphereInfraredAbsorption) * lowerAtmosphereInfraredAbsorption;

						backRadiation += reflected;
					}

					globalEnergyThermalBackRadiation += backRadiation;
					globalEnergySolarAbsorbedSurface += solarRadiationAbsorbed;

					float radiationToSurface = solarRadiationAbsorbed + backRadiation;

					// ice
					float remainingIceMass = iceMass;
					{
						// melt ice at surface from air temp and incoming radiation
						if (iceMass > 0)
						{
							float radiationAbsorbedByIce = radiationToSurface * iceCoverage;
							radiationToSurface -= radiationAbsorbedByIce;
							radiationAbsorbedByIce += thermalEnergyRadiatedToIce;

							// world.Data.SpecificHeatIce * world.Data.MassIce == KJ required to raise one cubic meter by 1 degree
							if (radiationAbsorbedByIce > 0)
							{
								// Remove the latent heat from the incoming energy
								float iceMelted = Mathf.Min(remainingIceMass, radiationAbsorbedByIce / world.Data.LatentHeatWaterLiquid);
								newIceMass -= iceMelted;
								newShallowWaterMass += iceMelted;
								newShallowWaterEnergy += iceMelted * (world.Data.SpecificHeatWater * world.Data.FreezingTemperature);
								remainingIceMass -= iceMelted;
							} else
							{
								newShallowWaterEnergy += radiationAbsorbedByIce * waterCoverage;
								newLandEnergy += radiationAbsorbedByIce * (1.0f - waterCoverage);
							}
							if (lowerAirTemperature > world.Data.FreezingTemperature)
							{
								// Remove the latent heat of water from the air
								float temperatureDiff = lowerAirTemperature - world.Data.FreezingTemperature;
								float energyTransfer = Mathf.Min(lowerAirEnergy, temperatureDiff * world.Data.SecondsPerTick * iceCoverage * world.Data.IceAirConductionCooling);
								float iceMeltedFromConduction = remainingIceMass * Mathf.Clamp01(energyTransfer / world.Data.LatentHeatWaterLiquid);
								newLowerAirEnergy -= energyTransfer;
								newIceMass -= iceMeltedFromConduction;
								newShallowWaterMass += iceMeltedFromConduction;
								newShallowWaterEnergy += iceMeltedFromConduction * (world.Data.SpecificHeatWater * world.Data.FreezingTemperature);
								remainingIceMass -= iceMeltedFromConduction;
								globalEnergySurfaceConduction -= energyTransfer;
							}

						}

						// freeze the top meter based on surface temperature (plus incoming radiation)
						if (iceCoverage < 1)
						{
							if (shallowWaterMass > 0)
							{
								// world.Data.SpecificHeatIce * world.Data.MassIce == KJ required to raise one cubic meter by 1 degree
								float specificHeatWater = GetSpecificHeatOfWater(world, shallowWaterMass, shallowSaltMass);
								float seaWaterHeatingRate = world.Data.MassWater  / specificHeatWater;
								//float surfaceTemp = lowerAirTemperature + incomingRadiation * seaWaterHeatingRate;
								float localHeating = 0; // TODO: add in some local heating
								float surfaceTemp = (lowerAirTemperature + localHeating) * (1.0f - iceCoverage) + shallowWaterTemperature * iceCoverage;
								if (surfaceTemp < world.Data.FreezingTemperature)
								{
									float iceMassFrozen = Mathf.Min(shallowWaterMass, Mathf.Min(Mathf.Max(0, world.Data.FullIceCoverage * world.Data.MassIce - iceMass), (world.Data.FreezingTemperature - surfaceTemp) * seaWaterHeatingRate));
									newIceMass += iceMassFrozen;
									newShallowWaterMass -= iceMassFrozen;
									// TODO: shouldnt the latent heat be added to the air, not the water?
									newShallowWaterEnergy -= iceMassFrozen * (world.Data.SpecificHeatWater * surfaceTemp - world.Data.LatentHeatWaterLiquid);
									if (deepWaterMass > 0)
									{
										float saltTransfer = iceMassFrozen / shallowWaterMass * shallowSaltMass;
										newShallowSaltMass -= saltTransfer;
										newDeepSaltMass += saltTransfer;
									}
								}

								// TODO this should be using absolute pressure not barometric
								float inverseLowerAirPressure = 1.0f / lowerAirPressure;
								// evaporation
								if (evapRate > 0)
								{
									float evapotranspiration;
									// TODO: absorb incoming radiation as latent heat (rather than from the surrounding air)
									EvaporateWater(
										world,
										waterCoverage,
										evapRate,
										waterTableDepth,
										shallowWaterMass,
										shallowSaltMass,
										shallowWaterEnergy,
										shallowWaterTemperature,
										ref newHumidity,
										ref newLowerAirEnergy,
										ref newShallowWaterEnergy,
										ref newShallowWaterMass,
										out evaporation,
										out evapotranspiration);
									globalEnergyEvapotranspiration += evapotranspiration;
								}
							}
						}
					}


					// absorbed by surface
					{
						// absorb the remainder and radiate heat
						float absorbedByLand = (1.0f - waterCoverage) * radiationToSurface;
						newLandEnergy += absorbedByLand;
						if (waterCoverage > 0)
						{
							// absorb remaining incoming radiation (we've already absorbed radiation in surface ice above)
							float absorbedByWater = waterCoverage * radiationToSurface;
							newShallowWaterEnergy += absorbedByWater;
							globalEnergySolarAbsorbedOcean += absorbedByWater;

							// heat transfer (both ways) based on temperature differential
							// conduction to ice from below
							if (iceMass > 0 && shallowWaterTemperature > world.Data.FreezingTemperature)
							{
								float oceanConductionRate = (shallowWaterTemperature - world.Data.FreezingTemperature) * world.Data.OceanIceConduction * world.Data.SecondsPerTick * iceCoverage;

								float energyToIce = Math.Max(0, oceanConductionRate * iceCoverage);
								float iceMelted = Mathf.Min(remainingIceMass, energyToIce * inverseSpecificHeatIce);
								newIceMass -= iceMelted;
								newShallowWaterMass += iceMelted;
								newShallowWaterEnergy += iceMelted * (world.Data.SpecificHeatWater * world.Data.FreezingTemperature - world.Data.LatentHeatWaterLiquid);
								remainingIceMass -= iceMelted;
							}
							// lose heat to air via conduction AND radiation
							if (iceCoverage < 1)
							{
								// when ocean is warmer than air, it creates a convection current, which makes conduction more efficient)
								float oceanConduction = (shallowWaterTemperature - lowerAirTemperature) * world.Data.SecondsPerTick * (1.0f - iceCoverage) * Mathf.Min(1.0f, waterDepth / world.Data.WaterAirConductionDepth);
								if (oceanConduction > 0)
								{
									oceanConduction *= world.Data.OceanAirConductionWarming;
								}
								else
								{
									oceanConduction *= world.Data.OceanAirConductionCooling;
								}
								newLowerAirEnergy += oceanConduction;
								newShallowWaterEnergy -= oceanConduction;
								globalEnergyOceanConduction += oceanConduction;
								globalEnergySurfaceConduction += oceanConduction;
							}

							if (shallowWaterTemperature < world.Data.FreezingTemperature)
							{
								float specificHeatSaltWater = (world.Data.SpecificHeatWater * shallowWaterMass + world.Data.SpecificHeatWater * shallowSaltMass);
								float massFrozen = Mathf.Min(shallowWaterMass,
									specificHeatSaltWater * (shallowWaterTemperature - world.Data.FreezingTemperature) /
									(world.Data.LatentHeatWaterLiquid - world.Data.FreezingTemperature * (world.Data.SpecificHeatWater + world.Data.SpecificHeatIce)));

								newIceMass += massFrozen;
								newShallowWaterMass -= massFrozen;
								newShallowWaterEnergy -= massFrozen * (world.Data.SpecificHeatWater * world.Data.FreezingTemperature - world.Data.LatentHeatWaterLiquid);

								if (deepWaterMass > 0)
								{
									float saltTransfer = massFrozen / shallowWaterMass * shallowSaltMass;
									newShallowSaltMass -= saltTransfer;
									newDeepSaltMass += saltTransfer;
								}

							}
						}
					}


					_ProfileAtmosphereEnergyBudget.End();


					_ProfileAtmosphereMoveV.Begin();
					MoveWaterVaporVertically(
						world,
						humidity,
						relativeHumidity,
						lowerWind,
						upperWind,
						lowerAirTemperature,
						upperAirTemperature,
						cloudMass,
						rainDropMass,
						dewPoint,
						cloudElevation,
						elevationOrSeaLevel,
						evapRate,
						ref newIceMass,
						ref newHumidity,
						ref newCloudMass,
						ref newRainfall,
						ref newRainDropMass,
						ref newLowerAirEnergy,
						ref newUpperAirEnergy,
						ref newShallowWaterEnergy,
						ref newShallowWaterMass);

					if (lowerWind.z > 0)
					{
						float verticalTransfer = Mathf.Min(0.5f, lowerWind.z * world.Data.WindAirMovementVertical * inverseBoundaryZoneElevation * world.Data.SecondsPerTick);
						newUpperAirMass += verticalTransfer * lowerAirMass;
						newLowerAirMass -= verticalTransfer * lowerAirMass;
						newUpperAirEnergy += verticalTransfer * lowerAirEnergy;
						newLowerAirEnergy -= verticalTransfer * lowerAirEnergy;
					}
					else
					{
						float verticalTransfer = Mathf.Min(0.5f, -lowerWind.z * world.Data.WindAirMovementVertical * inverseBoundaryZoneElevation * world.Data.SecondsPerTick);
						newLowerAirMass += verticalTransfer * upperAirMass;
						newUpperAirMass -= verticalTransfer * upperAirMass;
						newLowerAirEnergy += verticalTransfer * upperAirEnergy;
						newUpperAirEnergy -= verticalTransfer * upperAirEnergy;
					}


					{
						float tempAtSeaLevelUpper = upperAirTemperature - world.Data.TemperatureLapseRate * (world.Data.BoundaryZoneElevation + elevationOrSeaLevel);
						float tempAtSeaLevelLower = lowerAirTemperature - world.Data.TemperatureLapseRate * elevationOrSeaLevel;
						if (tempAtSeaLevelLower > tempAtSeaLevelUpper)
						{
							float verticalTransfer = (tempAtSeaLevelUpper - tempAtSeaLevelLower) / tempAtSeaLevelLower * lowerAirEnergy * world.Data.AirEnergyDiffusionSpeedVertical;
							newLowerAirEnergy += verticalTransfer;
							newUpperAirEnergy -= verticalTransfer;
						} else
						{
							float verticalTransfer = (tempAtSeaLevelUpper - tempAtSeaLevelLower) / tempAtSeaLevelUpper * upperAirEnergy * world.Data.AirEnergyDiffusionSpeedVertical;
							newLowerAirEnergy += verticalTransfer;
							newUpperAirEnergy -= verticalTransfer;
						}
					}

					//{
					//	float verticalTransfer = (upperAirPressure - lowerAirPressure) / Mathf.Max(upperAirPressure, lowerAirPressure) * lowerAirEnergy * world.Data.AirMassDiffusionSpeedVertical;
					//	newLowerAirMass += verticalTransfer;
					//	newUpperAirMass -= verticalTransfer;
					//}

					if (deepWaterMass > 0 && shallowWaterMass > 0)
					{
						float shallowDepth = Mathf.Min(world.Data.DeepOceanDepth, waterDepth);
						float deepDepth = Mathf.Max(0, waterDepth - world.Data.DeepOceanDepth);
						MoveOceanVertically(
							world,
							state,
							iceMass,
							shallowWaterEnergy,
							deepWaterEnergy,
							shallowSaltMass,
							deepSaltMass,
							shallowWaterTemperature,
							deepWaterTemperature,
							currentShallow.z,
							shallowWaterMass,
							deepWaterMass,
							shallowDepth,
							deepDepth,
							ref newShallowWaterEnergy,
							ref newDeepWaterEnergy,
							ref newShallowSaltMass,
							ref newDeepSaltMass,
							ref newShallowWaterMass,
							ref newDeepWaterMass);
					}
					SeepWaterIntoGround(world, groundWater, landEnergy, shallowWaterMass, soilFertility, waterTableDepth, shallowWaterTemperature, ref newGroundWater, ref newShallowWaterMass, ref newLandEnergy, ref newShallowWaterEnergy);
					_ProfileAtmosphereMoveV.End();



					_ProfileAtmosphereDiffusion.Begin();

					// Diffusion step
					for (int i = 0; i < 4; i++)
					{
						int nIndex = world.GetNeighborIndex(index, i);
						float nElevation = state.Elevation[nIndex];
						float nWaterDepth = state.WaterAndIceDepth[nIndex];

						//TODO: make air diffuse faster at low density
						//Mixing Upper atmosphere
						{
							float nEnergy = state.UpperAirEnergy[nIndex];
							float nMass = state.UpperAirMass[nIndex];
							float upperAirDensityAtSeaLevel = upperAirMass / (world.Data.TropopauseElevation - (elevationOrSeaLevel + world.Data.BoundaryZoneElevation));
							float upperAirDensityAtSeaLevelNeighbor = nMass / (world.Data.TropopauseElevation - (state.Elevation[nIndex]+ nWaterDepth + world.Data.BoundaryZoneElevation));
							float massDiffusionSpeed = world.Data.AirMassDiffusionSpeedHorizontal * (upperAirDensityAtSeaLevelNeighbor - upperAirDensityAtSeaLevel) / Mathf.Max(upperAirDensityAtSeaLevelNeighbor, upperAirDensityAtSeaLevel);
							if (massDiffusionSpeed > 0)
							{
								newUpperAirMass += massDiffusionSpeed * nMass;
								newUpperAirEnergy += massDiffusionSpeed * nEnergy;
							} else
							{
								newUpperAirMass += massDiffusionSpeed * upperAirMass;
								newUpperAirEnergy += massDiffusionSpeed * upperAirEnergy;
							}

							float nTempAtSeaLevel = state.UpperAirTemperature[nIndex] - world.Data.TemperatureLapseRate * (world.Data.BoundaryZoneElevation + nElevation + nWaterDepth);
							float tempAtSeaLevel = upperAirTemperature - world.Data.TemperatureLapseRate * (world.Data.BoundaryZoneElevation + elevationOrSeaLevel);
							float energyDiffusionSpeed = world.Data.AirEnergyDiffusionSpeedHorizontal * (nTempAtSeaLevel - tempAtSeaLevel);
							if (energyDiffusionSpeed > 0)
							{
								newUpperAirEnergy += energyDiffusionSpeed / nTempAtSeaLevel * nEnergy;
							} else
							{
								newUpperAirEnergy += energyDiffusionSpeed / tempAtSeaLevel * upperAirEnergy;
							}
						}

						//mixing lower atmosphere
						{
							float pressureDiffusionSpeed = world.Data.AirMassDiffusionSpeedHorizontal * (state.LowerAirPressure[nIndex] - lowerAirPressure) / Mathf.Max(state.LowerAirPressure[nIndex], lowerAirPressure);
							float nEnergy = state.LowerAirEnergy[nIndex];
							float nMass = state.LowerAirMass[nIndex];
							if (pressureDiffusionSpeed > 0)
							{
								newLowerAirMass += pressureDiffusionSpeed * nMass;
								newLowerAirEnergy += pressureDiffusionSpeed * nEnergy;
							}
							else
							{
								newLowerAirMass += pressureDiffusionSpeed * lowerAirMass;
								newLowerAirEnergy += pressureDiffusionSpeed * lowerAirEnergy;
							}
							float nTempAtSeaLevel = state.LowerAirTemperature[nIndex] - world.Data.TemperatureLapseRate * (nElevation + nWaterDepth);
							float tempAtSeaLevel = lowerAirTemperature - world.Data.TemperatureLapseRate * elevationOrSeaLevel;
							float energyDiffusionSpeed = world.Data.AirEnergyDiffusionSpeedHorizontal * (nTempAtSeaLevel - tempAtSeaLevel);
							if (energyDiffusionSpeed > 0)
							{
								newLowerAirEnergy += energyDiffusionSpeed / nTempAtSeaLevel * nEnergy;
							}
							else
							{
								newLowerAirEnergy += energyDiffusionSpeed / tempAtSeaLevel * lowerAirEnergy;
							}
						}

						if (shallowWaterMass > 0)
						{
							//if (shallowWaterMass > 0)
							//{
							//	float nShallowWaterMass = state.ShallowWaterMass[nIndex];
							//	if (nShallowWaterMass > 0)
							//	{
							//		float shallowOceanMassInverse = 1.0f / (shallowWaterMass + shallowSaltMass);
							//		float nShallowMassInverse = 1.0f / (nShallowWaterMass + state.ShallowSaltMass[nIndex]);
							//		float nShallowSaltMass = state.ShallowSaltMass[nIndex];
							//		float nShallowWaterEnergy = state.ShallowWaterEnergy[nIndex];

							//		// Horizontal mixing
							//		float mixingMass = Math.Min(nShallowWaterMass, shallowWaterMass);
							//		newShallowWaterEnergy += (nShallowWaterEnergy * nShallowMassInverse - shallowWaterEnergy * shallowOceanMassInverse) * mixingMass * world.Data.OceanHorizontalMixingSpeed;
							//		newShallowSaltMass += (nShallowSaltMass * nShallowMassInverse - shallowSaltMass * shallowOceanMassInverse) * mixingMass * world.Data.OceanHorizontalMixingSpeed;

							//		float elevationDiff = nElevation + nWaterDepth - elevationOrSeaLevel;
							//		float elevationEqualizationSpeed = 0.1f;
							//		if (elevationDiff > 0)
							//		{
							//			elevationDiff = Mathf.Min(elevationDiff, nWaterDepth);
							//			float shallowWaterDensity = GetWaterDensityByTemperature(world, state.ShallowWaterTemperature[nIndex], nShallowSaltMass, nShallowWaterMass);
							//			float massToMove = elevationDiff * shallowWaterDensity * elevationEqualizationSpeed;
							//			float inverseShallowMass = 1.0f / (nShallowSaltMass + nShallowWaterMass);
							//			float saltRatio = nShallowSaltMass * inverseShallowMass;
							//			newShallowWaterMass += (1.0f - saltRatio) * massToMove;
							//			newShallowSaltMass += saltRatio * massToMove;
							//			newShallowWaterEnergy += massToMove * inverseShallowMass * nShallowWaterEnergy;
							//		}
							//		else if (elevationDiff < 0)
							//		{
							//			elevationDiff = Mathf.Max(elevationDiff, -waterDepth);
							//			float shallowWaterDensity = GetWaterDensityByTemperature(world, shallowWaterTemperature, shallowSaltMass, shallowWaterMass);
							//			float massToMove = elevationDiff * shallowWaterDensity * elevationEqualizationSpeed;
							//			float inverseShallowMass = 1.0f / (shallowSaltMass + shallowWaterMass);
							//			float saltRatio = shallowSaltMass * inverseShallowMass;
							//			newShallowWaterMass += (1.0f - saltRatio) * massToMove;
							//			newShallowSaltMass += saltRatio * massToMove;
							//			newShallowWaterEnergy += massToMove * inverseShallowMass * shallowWaterEnergy;
							//		}
							//	}
							//}
							//if (deepWaterMass > 0)
							//{
							//	float deepNeighborMass = state.DeepWaterMass[nIndex];
							//	if (deepNeighborMass > 0)
							//	{
							//		float deepOceanMassInverse = 1.0f / (deepWaterMass + deepSaltMass);
							//		float nDeepMassInverse = 1.0f / (deepNeighborMass + state.DeepSaltMass[nIndex]);

							//		// Horizontal mixing
							//		float mixingMass = Math.Min(deepNeighborMass, deepWaterMass);
							//		newDeepWaterEnergy += (state.DeepWaterEnergy[nIndex] * nDeepMassInverse - deepWaterEnergy * deepOceanMassInverse) * mixingMass * world.Data.OceanHorizontalMixingSpeed;
							//		newDeepSaltMass += (state.DeepSaltMass[nIndex] * nDeepMassInverse - deepSaltMass * deepOceanMassInverse) * mixingMass * world.Data.OceanHorizontalMixingSpeed;
							//		newDeepWaterMass += (state.DeepWaterMass[nIndex] - deepWaterMass) * world.Data.OceanHorizontalMixingSpeed;
							//	}
							//}
						}


					}
					_ProfileAtmosphereDiffusion.End();



					_ProfileAtmosphereFinal.Begin();

					nextState.LowerAirMass[index] = Mathf.Max(0, nextState.LowerAirMass[index] + newLowerAirMass);
					nextState.UpperAirMass[index] = Mathf.Max(0, nextState.UpperAirMass[index] + newUpperAirMass);
					nextState.LowerAirEnergy[index] = Mathf.Max(0, nextState.LowerAirEnergy[index] + newLowerAirEnergy);
					nextState.UpperAirEnergy[index] = Mathf.Max(0, nextState.UpperAirEnergy[index] + newUpperAirEnergy);
					nextState.Humidity[index] = Mathf.Max(0, nextState.Humidity[index] + newHumidity);
					nextState.CloudMass[index] = Mathf.Max(0, nextState.CloudMass[index] + newCloudMass);
					nextState.RainDropMass[index] = Mathf.Max(0, nextState.RainDropMass[index] + newRainDropMass);
					nextState.ShallowWaterMass[index] = Mathf.Max(0, nextState.ShallowWaterMass[index] + newShallowWaterMass);
					nextState.ShallowWaterEnergy[index] = nextState.ShallowWaterMass[index] == 0 ? 0 : Mathf.Max(0, nextState.ShallowWaterEnergy[index] + newShallowWaterEnergy);
					nextState.ShallowSaltMass[index] = Mathf.Max(0, nextState.ShallowSaltMass[index] + newShallowSaltMass);
					float shallowOceanDensity = GetWaterDensity(world, nextState.ShallowWaterEnergy[index], nextState.ShallowSaltMass[index], nextState.ShallowWaterMass[index]);
					nextState.DeepWaterMass[index] = Mathf.Max(0, nextState.DeepWaterMass[index] + newDeepWaterMass);
					nextState.DeepWaterEnergy[index] = Mathf.Max(0, nextState.DeepWaterEnergy[index] + newDeepWaterEnergy);
					nextState.DeepSaltMass[index] = Mathf.Max(0, nextState.DeepSaltMass[index] + newDeepSaltMass);
					nextState.GroundWater[index] = Mathf.Max(0, nextState.GroundWater[index] + newGroundWater);

					nextState.LandEnergy[index] = Mathf.Max(0, landEnergy + newLandEnergy);
					nextState.IceMass[index] = Mathf.Max(0, newIceMass);
					nextState.Radiation[index] = Mathf.Max(0, newRadiation);

					nextState.Evaporation[index] = evaporation;
					nextState.Rainfall[index] = newRainfall;
					nextState.EnergyAbsorbed[index] = solarRadiationAbsorbed;

					globalEnergyGained += solarRadiationAbsorbed;
					globalRainfall += newRainfall;
					globalEvaporation += evaporation;
					globalCloudMass += nextState.CloudMass[index];
					globalWaterVapor += nextState.Humidity[index];

					_ProfileAtmosphereFinal.End();

					if (float.IsNaN(nextState.LowerAirEnergy[index]) ||
						float.IsNaN(nextState.UpperAirEnergy[index]) ||
						float.IsNaN(nextState.LowerAirMass[index]) ||
						float.IsNaN(nextState.UpperAirMass[index]) ||
						float.IsNaN(nextState.Humidity[index]) ||
						float.IsNaN(nextState.CloudMass[index]) ||
						float.IsNaN(nextState.RainDropMass[index]))
					{
						return;
					}



				}
			}

			for (int index = 0; index < world.Size*world.Size; index++)
			{
				{
					float shallowOceanMass = nextState.ShallowWaterMass[index];
					float oceanEnergyShallow = nextState.ShallowWaterEnergy[index];
					float oceanSalinityShallow = nextState.ShallowSaltMass[index];

					float deepOceanMass = nextState.DeepWaterMass[index];
					float oceanEnergyDeep = nextState.DeepWaterEnergy[index];
					float oceanSalinityDeep = nextState.DeepSaltMass[index];

					float shallowOceanVolume = GetWaterVolume(world, shallowOceanMass, oceanSalinityShallow, oceanEnergyShallow);
					float moveFromShallow = shallowOceanVolume - world.Data.DeepOceanDepth;
					if (moveFromShallow < 0)
					{
						if (deepOceanMass > 0)
						{
							float deepOceanVolume = GetWaterVolume(world, deepOceanMass, oceanSalinityDeep, oceanEnergyDeep);
							moveFromShallow = Mathf.Max(moveFromShallow, -deepOceanVolume);
							float movePercent = moveFromShallow / deepOceanVolume;
							nextState.ShallowWaterMass[index] -= deepOceanMass * movePercent;
							nextState.ShallowWaterEnergy[index] -= oceanEnergyDeep * movePercent;
							nextState.ShallowSaltMass[index] -= oceanSalinityDeep * movePercent;
							nextState.DeepWaterMass[index] += deepOceanMass * movePercent;
							nextState.DeepWaterEnergy[index] += oceanEnergyDeep * movePercent;
							nextState.DeepSaltMass[index] += oceanSalinityDeep * movePercent;
						}
					}
					else if (shallowOceanVolume > 0)
					{
						float movePercent = moveFromShallow / shallowOceanVolume;
						nextState.ShallowWaterMass[index] -= shallowOceanMass * movePercent;
						nextState.ShallowWaterEnergy[index] -= oceanEnergyShallow * movePercent;
						nextState.ShallowSaltMass[index] -= oceanSalinityShallow * movePercent;
						nextState.DeepWaterMass[index] += shallowOceanMass * movePercent;
						nextState.DeepWaterEnergy[index] += oceanEnergyShallow * movePercent;
						nextState.DeepSaltMass[index] += oceanSalinityShallow * movePercent;
					}
					float shallowWaterTemperature = GetWaterTemperature(world, nextState.ShallowWaterEnergy[index], nextState.ShallowWaterMass[index], nextState.ShallowSaltMass[index]);
					float deepWaterTemperature = GetWaterTemperature(world, nextState.DeepWaterEnergy[index], nextState.DeepWaterMass[index], nextState.DeepSaltMass[index]);
					nextState.ShallowWaterTemperature[index] = shallowWaterTemperature;
					float waterDepth = Mathf.Max(0,
						GetWaterVolumeByTemperature(world, nextState.ShallowWaterMass[index], nextState.ShallowSaltMass[index], shallowWaterTemperature) +
						GetWaterVolumeByTemperature(world, nextState.DeepWaterMass[index], nextState.DeepSaltMass[index], deepWaterTemperature));
					nextState.WaterDepth[index] = waterDepth;
					nextState.WaterAndIceDepth[index] = waterDepth + nextState.IceMass[index] / world.Data.MassIce;
					nextState.DeepWaterDensity[index] = GetWaterDensity(world, nextState.DeepWaterEnergy[index], nextState.DeepSaltMass[index], nextState.DeepWaterMass[index]);

				}
				float elevation = nextState.Elevation[index];
				float elevationOrSeaLevel = elevation + nextState.WaterAndIceDepth[index];
				nextState.LowerAirTemperature[index] = GetAirTemperature(world, nextState.LowerAirEnergy[index], nextState.LowerAirMass[index], nextState.Humidity[index], world.Data.SpecificHeatWaterVapor);
				nextState.LowerAirPressure[index] = Mathf.Max(0, GetAirPressure(world, nextState.LowerAirMass[index] + nextState.UpperAirMass[index] + state.StratosphereMass + nextState.Humidity[index] + nextState.CloudMass[index], elevationOrSeaLevel, nextState.LowerAirTemperature[index], GetMolarMassAir(world, nextState.UpperAirMass[index] + nextState.StratosphereMass, nextState.CloudMass[index])));
				nextState.UpperAirTemperature[index] = GetAirTemperature(world, nextState.UpperAirEnergy[index], nextState.UpperAirMass[index], nextState.CloudMass[index], world.Data.SpecificHeatWater);
				nextState.UpperAirPressure[index] = Mathf.Max(0, GetAirPressure(world, nextState.UpperAirMass[index] + nextState.StratosphereMass + nextState.CloudMass[index], elevationOrSeaLevel + world.Data.BoundaryZoneElevation, nextState.UpperAirTemperature[index], GetMolarMassAir(world, nextState.LowerAirMass[index] + nextState.UpperAirMass[index] + nextState.StratosphereMass, nextState.Humidity[index] + nextState.CloudMass[index])));

				globalIceMass += nextState.IceMass[index];
				globalEnergyDeepWater += nextState.DeepWaterEnergy[index];
				globalEnergyShallowWater += nextState.ShallowWaterEnergy[index];
				globalEnergyLand += nextState.LandEnergy[index];
				globalEnergyLowerAir += nextState.LowerAirEnergy[index];
				globalEnergyUpperAir += nextState.UpperAirEnergy[index];
				atmosphericMass += nextState.LowerAirMass[index] + nextState.UpperAirMass[index];


				if (float.IsInfinity(nextState.LowerAirPressure[index]))
				{
					Debug.DebugBreak();
				}

			}


			nextState.GlobalEnergyGained = globalEnergyGained;
			nextState.GlobalEnergyUpperAir = globalEnergyUpperAir;
			nextState.GlobalEnergyLowerAir = globalEnergyLowerAir;
			nextState.GlobalEnergyShallowWater = globalEnergyShallowWater;
			nextState.GlobalEnergyDeepWater = globalEnergyDeepWater;
			nextState.GlobalEnergyLand = globalEnergyLand;
			nextState.GlobalEnergyIncoming = globalEnergyIncoming;
			nextState.GlobalEnergySolarReflectedAtmosphere = globalEnergySolarReflectedAtmosphere;
			nextState.GlobalEnergySolarReflectedCloud = globalSolarEnergyReflectedClouds;
			nextState.GlobalEnergySolarReflectedSurface = globalEnergySolarReflectedSurface;
			nextState.GlobalEnergySolarAbsorbedCloud = globalEnergySolarAbsorbedClouds;
			nextState.GlobalEnergySolarAbsorbedAtmosphere = globalEnergySolarAbsorbedAtmosphere;
			nextState.GlobalEnergySolarAbsorbedSurface = globalEnergySolarAbsorbedSurface;
			nextState.GlobalEnergySolarAbsorbedOcean = globalEnergySolarAbsorbedOcean;
			nextState.GlobalEnergyThermalOceanRadiation = globalEnergyThermalOceanRadiation;
			nextState.GlobalEnergyThermalBackRadiation = globalEnergyThermalBackRadiation;
			nextState.GlobalEnergyThermalSurfaceRadiation = globalEnergyThermalSurfaceRadiation;
			nextState.GlobalEnergyThermalOutAtmosphericWindow = globalEnergyThermalOutAtmosphericWindow;
			nextState.GlobalEnergyThermalOutAtmosphere = globalEnergyThermalOutAtmosphere;
			nextState.GlobalEnergyThermalAbsorbedAtmosphere = globalEnergyThermalAbsorbedAtmosphere;
			nextState.GlobalEnergyEvapotranspiration = globalEnergyEvapotranspiration;
			nextState.GlobalEnergyOceanConduction = globalEnergyOceanConduction;
			nextState.GlobalEnergySurfaceConduction = globalEnergySurfaceConduction;
			nextState.GlobalTemperature = globalTemperature;
			nextState.GlobalOceanCoverage = globalOceanCoverage * inverseWorldSize * inverseWorldSize;
			nextState.GlobalCloudCoverage = globalCloudCoverage * inverseWorldSize * inverseWorldSize;
			nextState.GlobalEvaporation = globalEvaporation;
			nextState.GlobalRainfall = globalRainfall;
			nextState.GlobalCloudMass = globalCloudMass;
			nextState.GlobalIceMass = globalIceMass;
			nextState.GlobalWaterVapor = globalWaterVapor;
			nextState.GlobalOceanVolume = globalOceanVolume;
			nextState.GlobalSeaLevel = seaLevel / seaLevelTiles;
			nextState.AtmosphericMass = atmosphericMass;

			_ProfileAtmosphereTick.End();
		}

		static public float GetAirPressure(World world, float mass, float elevation, float temperature, float molarMass)
		{
			float temperatureLapse = -world.Data.TemperatureLapseRate * elevation;
			float pressure = mass * world.Data.GravitationalAcceleration * Mathf.Pow(1.0f -(temperatureLapse) / (temperature + temperatureLapse), world.Data.PressureExponent * molarMass);
			return pressure;
		}

		static public float GetAirMass(World world, float pressure, float elevation, float temperature, float molarMass)
		{
			float temperatureLapse = -world.Data.TemperatureLapseRate * elevation;
			float mass = pressure / (world.Data.GravitationalAcceleration * Mathf.Pow(1.0f - (temperatureLapse) / (temperature + temperatureLapse), world.Data.PressureExponent * molarMass));
			return mass;
		}

		static public float GetMolarMassAir(World world, float airMass, float waterMass)
		{
			return (airMass * world.Data.MolarMassAir + waterMass * world.Data.MolarMassWater) / (airMass + waterMass);
		}

		static public float GetAirDensity(World world, float absolutePressure, float temperature, float molarMassAir)
		{
			return absolutePressure * molarMassAir / (world.Data.UniversalGasConstant * temperature);
		}

		static public float GetWaterDensity(World world, float oceanEnergy, float saltMass, float mass)
		{
			if (mass <= 0)
			{
				return 0;
			}
			return world.Data.waterDensity + (world.Data.OceanDensityPerSalinity * saltMass / (mass + saltMass) + world.Data.OceanDensityPerDegree * (GetWaterTemperature(world, oceanEnergy, mass, saltMass) - world.Data.FreezingTemperature));
		}

		static public float GetWaterDensityByTemperature(World world, float temperature, float saltMass, float mass)
		{
			if (mass <= 0)
			{
				return 0;
			}
			return world.Data.waterDensity + (world.Data.OceanDensityPerSalinity * saltMass / (mass + saltMass) + world.Data.OceanDensityPerDegree * (temperature - world.Data.FreezingTemperature));
		}

		static public float GetWaterVolume(World world, float mass, float salt, float energy)
		{
			if (mass <= 0)
			{
				return 0;
			}
			return (mass + salt) / GetWaterDensity(world, energy, salt, mass);
		}

		static public float GetWaterVolumeByTemperature(World world, float mass, float salt, float temperature)
		{
			if (mass <= 0)
			{
				return 0;
			}
			return (mass + salt) / GetWaterDensityByTemperature(world, temperature, salt, mass);
		}



		static public float GetPressureAtElevation(World world, World.State state, int index, float elevation, float molarMass)
		{
			// Units: Pascals
			// Barometric Formula
			// Pressure = StaticPressure * (StdTemp / (StdTemp + StdTempLapseRate * (Elevation - ElevationAtBottomOfAtmLayer)) ^ (GravitationalAcceleration * MolarMassOfEarthAir / (UniversalGasConstant * StdTempLapseRate))
			// https://en.wikipedia.org/wiki/Barometric_formula
			// For the bottom layer of atmosphere ( < 11000 meters), ElevationAtBottomOfAtmLayer == 0)

			//	float standardPressure = Data.StaticPressure * (float)Math.Pow(Data.StdTemp / (Data.StdTemp + Data.StdTempLapseRate * elevation), Data.PressureExponent);
			float pressure = world.Data.StaticPressure * (float)Math.Pow(world.Data.StdTemp / (world.Data.StdTemp + world.Data.TemperatureLapseRate * elevation), world.Data.PressureExponent * molarMass);
			return pressure;
		}




		static private void MoveWaterVaporVertically(
			World world,
			float humidity,
			float relativeHumidity,
			Vector3 lowerWind,
			Vector3 upperWind,
			float lowerAirTemperature,
			float upperAirTemperature,
			float cloudMass, 
			float rainDropMass, 
			float dewPoint,
			float cloudElevation,
			float elevationOrSeaLevel,
			float evapRate,
			ref float newIceMass, 
			ref float newHumidity, 
			ref float newCloudMass, 
			ref float newRainfall, 
			ref float newRainDropMass,
			ref float newLowerAirEnergy,
			ref float newUpperAirEnergy,
			ref float newShallowWaterEnergy,
			ref float newShallowWaterMass)
		{


			// condensation
			if (relativeHumidity > 1)
			{
				float condensationMass = humidity * (relativeHumidity - 1.0f) / relativeHumidity;
				newHumidity -= condensationMass;
				newLowerAirEnergy -= condensationMass * (world.Data.SpecificHeatWaterVapor * lowerAirTemperature - world.Data.LatentHeatWaterVapor);
				if (lowerAirTemperature <= world.Data.FreezingTemperature)
				{
					newIceMass += condensationMass;
				}
				else
				{
					newShallowWaterMass += condensationMass;
					newShallowWaterEnergy += condensationMass * (world.Data.SpecificHeatWater * lowerAirTemperature);
				}
			}

			if (lowerWind.z > 0)
			{
				float humidityToCloud = Mathf.Min(1.0f, lowerWind.z * world.Data.SecondsPerTick / (cloudElevation - elevationOrSeaLevel)) * humidity * world.Data.HumidityToCloudPercent;
				newCloudMass += humidityToCloud;
				newHumidity -= humidityToCloud;
				// We're moving the latent heat of water vapor here since we want it to heat up the upper air around the cloud
				newLowerAirEnergy -= humidityToCloud * world.Data.SpecificHeatWaterVapor * lowerAirTemperature;
				newUpperAirEnergy += humidityToCloud * (world.Data.SpecificHeatWater * lowerAirTemperature + world.Data.LatentHeatWaterVapor);
			}

			if (cloudMass > 0)
			{

				// TODO: airDesntiy and rainDensity should probably be cleaned up (derived from other data?)
				float rainDropVolume = Mathf.Max(world.Data.rainDropMinSize, rainDropMass / (cloudMass * world.Data.waterDensity));
				float rainDropRadius = Mathf.Min(Mathf.Pow(rainDropVolume, 0.333f), world.Data.rainDropMaxSize);
				float rainDropVelocity = lowerWind.z - Mathf.Sqrt(8 * rainDropRadius * world.Data.waterDensity * world.Data.GravitationalAcceleration / (3 * world.Data.airDensity * world.Data.rainDropDragCoefficient));

				newRainDropMass += cloudMass * (world.Data.RainDropFormationSpeedTemperature / dewPoint * Mathf.Pow(Mathf.Max(0, -rainDropVelocity) * world.Data.RainDropCoalescenceWind, 2));

				if (rainDropVelocity < 0 && rainDropMass > 0)
				{
					newRainfall = cloudMass * Mathf.Clamp01(-rainDropVelocity * world.Data.RainfallRate);
					newCloudMass -= newRainfall;
					newRainDropMass -= newRainfall / cloudMass;

					{
						float rainDropFallTime = -cloudElevation / rainDropVelocity;
						// evap rate is based on full tile surface coverage, an occurs in the top millimeter
						float rainDropSurfaceArea = 4 * Mathf.PI * rainDropRadius * rainDropRadius;
						float totalRainSurfaceArea = newRainfall / (world.Data.waterDensity * rainDropVolume) * rainDropSurfaceArea;
						float rainEvapRate = evapRate * totalRainSurfaceArea * 1000 * world.Data.InverseMetersPerTile * world.Data.InverseMetersPerTile;
						float rainDropMassToHumidity = Mathf.Min(newRainfall, rainDropFallTime * rainEvapRate * world.Data.TicksPerSecond);
						newRainfall -= rainDropMassToHumidity;
						newHumidity += rainDropMassToHumidity;
						// This sucks heat out of the lower atmosphere in the form of latent heat of water vapor
						newUpperAirEnergy -= rainDropMassToHumidity * upperAirTemperature * world.Data.SpecificHeatWater;
						newLowerAirEnergy += rainDropMassToHumidity * (upperAirTemperature * world.Data.SpecificHeatWaterVapor - world.Data.LatentHeatWaterVapor);
					}
					if (newRainfall > 0)
					{
						newShallowWaterMass += newRainfall;
						// No real state change here
						float energyTransfer = newRainfall * upperAirTemperature * world.Data.SpecificHeatWater;
						newShallowWaterEnergy += energyTransfer;
						newUpperAirEnergy -= energyTransfer;
					}
				}

				// dissapation
				float dissapationSpeed = Mathf.Min(1.0f, world.Data.CloudDissapationRateWind * Mathf.Max(0, -lowerWind.z) + world.Data.CloudDissapationRateDryAir) * (1.0f - relativeHumidity);
				float dissapationMass = cloudMass * dissapationSpeed;
				newRainDropMass -= dissapationSpeed;
				newCloudMass -= dissapationMass;
				newHumidity += dissapationMass;
				newUpperAirEnergy -= dissapationMass * (upperAirTemperature * world.Data.SpecificHeatWater + world.Data.LatentHeatWaterVapor);
				newLowerAirEnergy += dissapationMass * world.Data.SpecificHeatWaterVapor * upperAirTemperature;
			}

		}


		static private void SeepWaterIntoGround(World world, float groundWater, float groundEnergy, float shallowWaterMass, float soilFertility, float waterTableDepth, float shallowWaterTemperature, ref float newGroundWater, ref float newShallowWater, ref float newGroundEnergy, ref float newShallowEnergy)
		{
			float maxGroundWater = soilFertility * waterTableDepth * world.Data.MaxSoilPorousness * world.Data.MassWater;
			if (groundWater >= maxGroundWater && groundWater > 0)
			{
				float massTransfer = groundWater - maxGroundWater;
				newShallowWater += massTransfer;
				newGroundWater -= massTransfer;
				float energyTransfer = massTransfer / groundWater * groundEnergy; // TODO: this isn't great, some of that ground energy is in the terrain, not just in the water
				newShallowEnergy += energyTransfer;
				newGroundEnergy -= energyTransfer;
			} else if (shallowWaterMass > 0)
			{
				float massTransfer = Mathf.Min(shallowWaterMass, Math.Min(soilFertility * world.Data.GroundWaterReplenishmentSpeed * world.Data.SecondsPerTick, maxGroundWater - groundWater));
				newGroundWater += massTransfer;
				newShallowWater -= massTransfer;
				float energyTransfer = massTransfer * shallowWaterTemperature * world.Data.SpecificHeatWater;
				newShallowEnergy -= energyTransfer;
				newGroundEnergy += energyTransfer;
			}
		}

		static private float GetEvaporationRate(World world, float iceCoverage, float temperature, float relativeHumidity, float inverseEvapTemperatureRange)
		{
			float evapTemperature = Mathf.Clamp01((temperature - world.Data.EvapMinTemperature) * inverseEvapTemperatureRange);

			return Mathf.Clamp01((1.0f - iceCoverage) * (1.0f - relativeHumidity) * Sqr(evapTemperature)) * world.Data.EvaporationRate * world.Data.MassWater;
		}

		static public float GetTemperatureAtElevation(World world, float elevation, float lowerTemperature, float upperTemperature, float elevationOrSeaLevel)
		{
			float temperatureLapseRate = (upperTemperature - lowerTemperature) / world.Data.BoundaryZoneElevation;
			return (elevation - elevationOrSeaLevel) * temperatureLapseRate + lowerTemperature;
		}

		static public float GetRelativeHumidity(World world, float temperature, float humidity, float airMass, float inverseDewPointTemperatureRange)
		{
			float maxWaterVaporPerKilogramAir = world.Data.WaterVaporMassToAirMassAtDewPoint * Sqr(Mathf.Max(0, (temperature - world.Data.DewPointZero) * inverseDewPointTemperatureRange));
			float maxHumidity = maxWaterVaporPerKilogramAir * airMass;
			if (maxHumidity <= 0)
			{
				return humidity > 0 ? 10000 : 0;
			}
			float relativeHumidity = humidity / maxHumidity;
			return relativeHumidity;
		}

		static public float GetAbsoluteHumidity(World world, float temperature, float relativeHumidity, float totalAtmosphericMass, float inverseDewPointTemperatureRange)
		{
			float maxWaterVaporPerKilogramAtmosphere = world.Data.WaterVaporMassToAirMassAtDewPoint * Sqr(Mathf.Max(0, (temperature - world.Data.DewPointZero) * inverseDewPointTemperatureRange)) / (1.0f + world.Data.WaterVaporMassToAirMassAtDewPoint);
			float maxHumidity = maxWaterVaporPerKilogramAtmosphere * totalAtmosphericMass;
			if (maxHumidity <= 0)
			{
				return 0;
			}
			float humidity = relativeHumidity * maxHumidity;
			return humidity;
		}

		static private void EvaporateWater(
			World world, 
			float waterCoverage, 
			float evapRate, 
			float waterTableDepth, 
			float shallowWaterMass,
			float shallowSaltMass,
			float shallowWaterEnergy,
			float shallowWaterTemperature,
			ref float newHumidity, 
			ref float newLowerAirEnergy, 
			ref float newShallowWaterEnergy, 
			ref float newShallowWaterMass,
			out float evaporation, 
			out float evapotranspiration)
		{
			evaporation = 0;
			evapotranspiration = 0;


			if (waterCoverage > 0)
			{
				float evapMass = Mathf.Min(shallowWaterMass, waterCoverage * evapRate);
				newHumidity += evapMass;
				newShallowWaterMass -= evapMass;
				evaporation += evapMass;
				// this sucks energy out of the lower atmosphere since it uses up some energy to fill up the latent heat of water vapor
				newShallowWaterEnergy -= evapMass * world.Data.SpecificHeatWater * shallowWaterTemperature;
				newLowerAirEnergy += evapMass * (world.Data.SpecificHeatWaterVapor * shallowWaterTemperature - world.Data.LatentHeatWaterVapor);
				evapotranspiration = evapMass * (world.Data.LatentHeatWaterVapor + world.Data.SpecificHeatWaterVapor * shallowWaterTemperature);
			}
		}

		static private void MoveOceanVertically(
		World world,
		World.State state,
		float ice,
		float shallowWaterEnergy,
		float oceanEnergyDeep,
		float shallowSaltMass,
		float deepSaltMass,
		float oceanTemperatureShallow,
		float oceanTemperatureDeep,
		float currentShallowZ,
		float shallowWaterMass,
		float deepWaterMass,
		float shallowWaterDepth,
		float deepWaterDepth,
		ref float newShallowWaterEnergy,
		ref float newDeepWaterEnergy,
		ref float newShallowSaltMass,
		ref float newDeepSaltMass,
		ref float newShallowWaterMass,
		ref float newDeepWaterMass)
		{
			{

				//if (oceanTemperatureShallow <= world.Data.FreezingTemperature + 5)
				//{
				//	//float salinityExchange = oceanSalinityShallow;
				//	//newOceanSalinityDeep += salinityExchange;
				//	//newOceanSalinityShallow -= salinityExchange;
				//	//newOceanEnergyDeep = world.Data.FreezingTemperature;
				//}
				//else
				{
					float salinityExchange = (deepSaltMass / (deepWaterMass + deepSaltMass) - shallowSaltMass / (shallowWaterMass + shallowSaltMass)) * world.Data.SalinityVerticalMixingSpeed * (shallowSaltMass + deepSaltMass) * world.Data.SecondsPerTick;
					newDeepSaltMass -= salinityExchange * deepWaterMass / (deepWaterMass + shallowWaterMass);
					newShallowSaltMass += salinityExchange * shallowWaterMass / (deepWaterMass + shallowWaterMass);

					float deepWaterMixingDepth = Math.Min(shallowWaterDepth, deepWaterDepth);
					float deepWaterMixingTemperature = (oceanTemperatureDeep - oceanTemperatureShallow) * deepWaterMixingDepth / deepWaterDepth + oceanTemperatureShallow;
					float heatExchange = (deepWaterMixingTemperature - oceanTemperatureShallow) * deepWaterMixingDepth * world.Data.OceanTemperatureVerticalMixingSpeed;
					newShallowWaterEnergy += heatExchange;
					newDeepWaterEnergy -= heatExchange;
				}

				if (currentShallowZ < 0)
				{
					float downwelling = Math.Min(0.5f, -currentShallowZ * world.Data.OceanUpwellingSpeed);
					float energyExchange = shallowWaterEnergy * downwelling;
					newShallowWaterEnergy -= energyExchange;
					newDeepWaterEnergy += energyExchange;
					float salinityExchange = shallowSaltMass * downwelling;
					newDeepSaltMass += salinityExchange;
					newShallowSaltMass -= salinityExchange;
				}
				else if (currentShallowZ > 0)
				{
					float upwelling = Math.Min(0.5f, currentShallowZ * world.Data.OceanUpwellingSpeed);
					float mixingDepth = Math.Min(shallowWaterMass, deepWaterMass) / (shallowWaterMass + deepWaterMass);
					float energyExchange = oceanEnergyDeep * mixingDepth * upwelling;
					newShallowWaterEnergy += energyExchange;
					newDeepWaterEnergy -= energyExchange;
					float salinityExchange = deepSaltMass * mixingDepth * upwelling;
					newDeepSaltMass -= salinityExchange;
					newShallowSaltMass += salinityExchange;
				}
			}

		}

		static public float GetDeclinationOfSun(float plantaryTilt, float timeOfYear)
		{
			const float vernalEquinox = 0.25f;
			float timeSinceVernalEquinox;
			if (timeOfYear < vernalEquinox)
			{
				timeSinceVernalEquinox = (1.0f - vernalEquinox) + timeOfYear;
			}
			else
			{
				timeSinceVernalEquinox = timeOfYear - vernalEquinox;
			}
			return -plantaryTilt * Mathf.Sin(timeSinceVernalEquinox * Mathf.PI * 2);
		}
		static public void GetSunVector(World world, float planetTiltAngle, int ticks, float latitude, float longitude, out float elevationAngle, out Vector3 sunVec)
		{
			float latitudeRadians = latitude * PIOver2;
			float angleOfInclination = planetTiltAngle * (float)Math.Sin(Math.PI * 2 * (world.GetTimeOfYear(ticks) - 0.25f));

			float timeOfDay = (world.GetTimeOfDay(ticks, longitude) - 0.5f) * Mathf.PI * 2;
			float azimuth = (float)Math.Atan2(Math.Sin(timeOfDay), Math.Cos(timeOfDay) * Math.Sin(latitudeRadians) - Math.Tan(angleOfInclination) * Math.Cos(latitudeRadians));
			elevationAngle = (float)Math.Asin((Math.Sin(latitudeRadians) * Math.Sin(angleOfInclination) + Math.Cos(latitudeRadians) * Math.Cos(angleOfInclination) * Math.Cos(timeOfDay)));

			float cosOfElevation = (float)Math.Cos(elevationAngle);
			sunVec = new Vector3((float)Math.Sin(azimuth) * cosOfElevation, (float)Math.Cos(azimuth) * cosOfElevation, (float)Math.Sin(elevationAngle));
		}
		static public float GetLengthOfDay(float latitude, float timeOfYear, float declinationOfSun)
		{
			float latitudeAngle = latitude * PIOver2;
			if ((latitude > 0) != (declinationOfSun > 0))
			{
				float hemisphere = Mathf.Sign(latitude);
				float noSunsetLatitude = (PIOver2 + hemisphere * declinationOfSun);
				if (latitudeAngle * hemisphere >= noSunsetLatitude)
				{
					return 1;
				}
			} else if ((latitude > 0) == (declinationOfSun > 0))
			{
				float hemisphere = Mathf.Sign(latitude);
				float noSunsetLatitude = PIOver2 - hemisphere * declinationOfSun;
				if (latitudeAngle * hemisphere >= noSunsetLatitude)
				{
					return 0;
				}
			}

			float hourAngle = Mathf.Acos(-Mathf.Tan(-latitudeAngle) * Mathf.Tan(declinationOfSun));
			float lengthOfDay = hourAngle / inversePI;
			return lengthOfDay;
		}

		static public float GetAirTemperature(World world, float energy, float mass, float waterMass, float waterSpecificHeat)
		{
			return energy / (mass * world.Data.SpecificHeatAtmosphere + waterMass * waterSpecificHeat);
		}
		static public float GetAirEnergy(World world, float temperature, float mass, float waterMass, float waterSpecificHeat)
		{
			return temperature * (mass * world.Data.SpecificHeatAtmosphere + waterMass * waterSpecificHeat);
		}

		static public float GetWaterTemperature(World world, float energy, float waterMass, float saltMass)
		{
			if (waterMass ==0)
			{
				return 0;
			}
			return Mathf.Max(0, energy / (waterMass * world.Data.SpecificHeatWater + saltMass * world.Data.SpecificHeatSalt));
		}
		static public float GetWaterEnergy(World world, float temperature, float waterMass, float saltMass)
		{
			return temperature * (world.Data.SpecificHeatWater * waterMass + world.Data.SpecificHeatSalt * saltMass);
		}
		static public float GetAlbedo(float surfaceAlbedo, float slope)
		{
			return surfaceAlbedo + (1.0f - surfaceAlbedo) * slope;
		}
		static public float GetDewPoint(World world, float lowerAirTemperature, float relativeHumidity)
		{
			return lowerAirTemperature - (1.0f - relativeHumidity) * world.Data.DewPointTemperaturePerRelativeHumidity;
		}
		static public float GetCloudElevation(World world, float upperAirTemperature, float dewPoint, float elevationOrSeaLevel)
		{
			return elevationOrSeaLevel + Mathf.Max(0, world.Data.BoundaryZoneElevation + (upperAirTemperature - dewPoint) * world.Data.DewPointElevationPerDegree);
		}

		static public float GetSpecificHeatOfWater(World world, float waterMass, float saltMass)
		{
			return (world.Data.SpecificHeatWater * waterMass + world.Data.SpecificHeatSalt * saltMass) / (waterMass + saltMass);
		}

		static public float GetAtmosphericEmissivity(World world, float airMass, float greenhouseGasMass, float humidity, float cloudMass)
		{
			return airMass * world.Data.AbsorptivityAir +
				greenhouseGasMass * world.Data.AbsorptivityCarbonDioxide +
				humidity * world.Data.AbsorptivityWaterVapor +
				cloudMass * world.Data.AbsorptivityWaterLiquid;
		}

		static public float GetLandRadiationRate(World world, float landEnergy, float groundWater, float soilFertility, float canopyCoverage)
		{
			float soilTemperature = GetLandTemperature(world, landEnergy, groundWater, soilFertility, canopyCoverage);
			return GetRadiationRate(world, soilTemperature, world.Data.EmissivityDirt) * (1.0f - canopyCoverage * 0.5f);
		}

		static public float GetRadiationRate(World world, float temperature, float emissivity)
		{
			return temperature * temperature * temperature * temperature * emissivity * 0.001f * world.Data.StefanBoltzmannConstant;
		}

		static public float GetLandTemperature(World world, float landEnergy, float groundWater, float soilFertility, float canopyCoverage)
		{
			float soilEnergy = landEnergy - groundWater * world.Data.maxGroundWaterTemperature * world.Data.SpecificHeatWater;
			float landMass = (world.Data.MassSand - world.Data.MassSoil) * soilFertility + world.Data.MassSoil;
			float heatingDepth = soilFertility * world.Data.SoilHeatDepth;
			return Mathf.Max(0, soilEnergy / (world.Data.SpecificHeatSoil * heatingDepth * landMass));
		}

		static public float RepeatExclusive(float x, float y)
		{
			while (x < 0)
			{
				x += y;
			}
			while (x >= y)
			{
				x -= y;
			}
			return x;
		}
	}
}