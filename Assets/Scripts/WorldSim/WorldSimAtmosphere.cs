﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

namespace Sim {
	static public class Atmosphere {


		static public void Tick(World world, World.State state, World.State nextState)
		{
			float timeOfYear = world.GetTimeOfYear(state.Ticks);
			float declinationOfSun = GetDeclinationOfSun(world.Data.planetTiltAngle, timeOfYear);
			for (int y = 0; y < world.Size; y++)
			{
				float latitude = world.GetLatitude(y);
				var sunVector = GetSunVector(world, state.Ticks, latitude);
				float sunAngle = Math.Max(0, sunVector.z);

				float lengthOfDay = GetLengthOfDay(latitude, timeOfYear, declinationOfSun);

				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);

					float elevation = state.Elevation[index];
					float elevationOrSeaLevel = Math.Max(state.SeaLevel, elevation);
					float cloudCover = state.CloudCover[index];
					float landEnergy = state.LandEnergy[index];
					float airTemperature = state.AirTemperature[index];
					float airEnergy = state.AirEnergy[index];
					float pressure = state.Pressure[index];
					var gradient = state.FlowDirection[index];
					var terrainNormal = state.Normal[index];
					float surfaceWater = state.SurfaceWater[index];
					float groundWater = state.GroundWater[index];
					float humidity = state.Humidity[index];
					float cloudElevation = state.CloudElevation[index];
					float waterTableDepth = state.WaterTableDepth[index];
					float soilFertility = state.SoilFertility[index];
					float surfaceIce = state.SurfaceIce[index];
					float radiation = 0.001f;
					float oceanEnergyDeep = state.OceanEnergyDeep[index];
					float oceanEnergyShallow = state.OceanEnergyShallow[index];
					float oceanDensity = state.OceanDensityDeep[index];
					float oceanSalinityDeep = state.OceanSalinityDeep[index];
					float oceanSalinityShallow = state.OceanSalinityShallow[index];
					float canopy = state.Canopy[index];
					var wind = state.Wind[index];
					var currentDeep = state.OceanCurrentDeep[index];
					var currentShallow = state.OceanCurrentShallow[index];
					float oceanTemperatureShallow = GetWaterTemperature(world, oceanEnergyShallow, world.Data.DeepOceanDepth);
					float oceanTemperatureDeep = GetWaterTemperature(world, oceanEnergyDeep, Math.Max(0, state.SeaLevel - elevation));


					float newEvaporation;
					float newGroundWater = groundWater;
					float newHumidity = humidity;
					float newPressure = pressure;
					float newCloudCover = cloudCover;
					float newSurfaceWater = surfaceWater;
					float newSurfaceIce = surfaceIce;
					float newAirEnergy = airEnergy;
					float newLandEnergy = landEnergy;
					float newCloudElevation = cloudElevation;
					float rainfall = 0;
					float newRadiation = radiation;
					float newOceanEnergyDeep = oceanEnergyDeep;
					float newOceanEnergyShallow = oceanEnergyShallow;
					float newOceanSalinityDeep = oceanSalinityDeep;
					float newOceanSalinityShallow = oceanSalinityShallow;
					float windSpeed = wind.magnitude;
					float newTemperature;


					float atmosphereMass = GetAtmosphereMass(world, elevation, elevationOrSeaLevel);
					float airPressureInverse = world.Data.StaticPressure / pressure;
					float evapRate = GetEvaporationRate(world, surfaceIce, airTemperature, humidity, wind.magnitude, cloudElevation, elevationOrSeaLevel);
					float cloudOpacity = Math.Min(1.0f, cloudCover / world.Data.cloudContentFullAbsorption);
					float groundWaterSaturation = Animals.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], soilFertility * world.Data.MaxSoilPorousness);
					float incomingRadiation = world.Data.SolarRadiation * sunAngle * lengthOfDay;
					float outgoingRadiation = 0;

					// reflect some rads off atmosphere and clouds
					incomingRadiation -= incomingRadiation * (world.Data.AtmosphericHeatReflection + world.Data.cloudReflectionRate * cloudOpacity);

					// absorb some rads directly in the atmosphere
					float absorbedByAtmosphereIncoming = incomingRadiation * world.Data.AtmosphericHeatAbsorption;
					newAirEnergy += absorbedByAtmosphereIncoming;
					incomingRadiation -= absorbedByAtmosphereIncoming;



					float heatReflected = 0;
					if (surfaceIce > 0)
					{
						heatReflected += incomingRadiation * Math.Min(1.0f, surfaceIce) * world.Data.AlbedoIce;
					}
					if (world.IsOcean(elevation, state.SeaLevel))
					{
						heatReflected = incomingRadiation * world.Data.AlbedoWater * Math.Max(0, (1.0f - surfaceIce));
					} else
					{
						// reflect some incoming radiation
						var slope = Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
						float waterReflectivity = surfaceWater * world.Data.AlbedoWater;
						float soilReflectivity = world.Data.AlbedoLand - world.Data.AlbedoReductionSoilQuality * soilFertility;
						float heatReflectedLand = canopy * world.Data.AlbedoFoliage + Math.Max(0, 1.0f - canopy) * (surfaceWater * world.Data.AlbedoWater + Math.Max(0, 1.0f - surfaceWater) * soilReflectivity);
						heatReflected += incomingRadiation * heatReflectedLand * Math.Max(0, (1.0f - surfaceIce));
					}
					incomingRadiation -= heatReflected;
					newAirEnergy += heatReflected;

					// lose some energy to space
					//float cloudReflectionFactor = world.Data.cloudReflectionRate * cloudOpacity;
					//float humidityPercentage = humidity / atmosphereMass;
					//float heatLossFactor = (1.0f - world.Data.carbonDioxide * world.Data.heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
					//float loss = airEnergy * (1.0f - cloudReflectionFactor) * (world.Data.heatLoss * heatLossFactor * airPressureInverse);
					newAirEnergy -= world.Data.AtmosphericHeatLossToSpace * airEnergy;

					// melt or freeze ice
					float iceMelted = 0;
					if (airTemperature <= world.Data.FreezingTemperature)
					{
						float frozen = world.Data.iceFreezeRate * (world.Data.FreezingTemperature - airTemperature) * (1.0f - (float)Math.Pow(Math.Min(1.0f, surfaceIce / world.Data.maxIce), 2));
						if (!world.IsOcean(elevation, state.SeaLevel))
						{
							frozen = Math.Min(frozen, surfaceWater);
							newSurfaceWater -= frozen;
						}
						newSurfaceIce += frozen;
					}
					else if (surfaceIce > 0)
					{
						if (!world.IsOcean(elevation, state.SeaLevel))
						{
							// add to surface water
							float meltRate = (airTemperature - world.Data.FreezingTemperature) * world.Data.iceMeltRate;
							iceMelted = Math.Min(surfaceIce, meltRate);
						} else
						{
							float meltRate = (oceanTemperatureShallow - world.Data.FreezingTemperature) * world.Data.iceMeltRate;
							iceMelted = Math.Min(surfaceIce, meltRate);
						}
					}

					// melt some ice via direct radiation
					iceMelted = Math.Min(surfaceIce, iceMelted + incomingRadiation * world.Data.iceMeltRadiationRate);


					// absorb all incoming radiation
					incomingRadiation *= Math.Max(0, 1.0f - surfaceIce);

					// reduce ice
					newSurfaceIce -= iceMelted;
					if (!world.IsOcean(elevation, state.SeaLevel))
					{
						// add to surface water
						newSurfaceWater += iceMelted;
					}
					else
					{
						// cool ocean down
						oceanTemperatureShallow += (world.Data.FreezingTemperature - oceanTemperatureShallow) * iceMelted / world.Data.DeepOceanDepth;
					}

					// TODO: melt ice based on radiation from land or ocean

					// absorb the remainder and radiate heat
					if (world.IsOcean(elevation, state.SeaLevel))
					{
						// absorb
						newOceanEnergyShallow += incomingRadiation;

						// radiate heat
						float oceanRadiation = oceanEnergyShallow * world.Data.OceanHeatRadiation;
						newAirEnergy += oceanRadiation;
						newOceanEnergyShallow -= oceanRadiation;

						// heat transfer (both ways) based on temperature differential
						float oceanConduction = (oceanTemperatureShallow - airTemperature) * world.Data.OceanAirConduction;
						newAirEnergy += oceanConduction;
						newOceanEnergyShallow -= oceanConduction;
					}
					else
					{
						newAirEnergy += incomingRadiation;

						//// absorb
						//newLandEnergy += incomingRadiation;

						//// radiate
						//float landRadiation = landEnergy * world.Data.LandRadiation;
						//newLandEnergy -= landRadiation;
						//newAirEnergy += landRadiation;
					}
					newTemperature = GetAirTemperature(world, newAirEnergy, elevation);
					
					MoveOceanOnCurrent(world, state, x, y, elevation, surfaceIce, oceanEnergyShallow, oceanEnergyDeep, oceanSalinityShallow, oceanSalinityDeep, oceanTemperatureShallow, oceanTemperatureDeep, oceanDensity, currentShallow, currentDeep, ref newOceanEnergyShallow, ref newOceanEnergyDeep, ref newOceanSalinityShallow, ref newOceanSalinityDeep, ref newAirEnergy);
					MoveAtmosphereOnWind(world, state, x, y, elevationOrSeaLevel, airEnergy, humidity, wind, ref newHumidity, ref newAirEnergy);
					FlowWater(world, state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
					SeepWaterIntoGround(world, elevation, state.SeaLevel, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);
					EvaporateWater(world, evapRate, elevation, state.SeaLevel, groundWater, waterTableDepth, ref newHumidity, ref newAirEnergy, ref newOceanEnergyShallow, ref newGroundWater, ref newSurfaceWater, out newEvaporation);
					//			MoveHumidityToClouds(elevation, humidity, tempWithSunAtGround, cloudElevation, windAtSurface, ref newHumidity, ref newCloudCover);
					if (cloudCover > 0)
					{
						UpdateCloudElevation(world, elevationOrSeaLevel, airTemperature, humidity, atmosphereMass, wind, ref newCloudElevation);
						MoveClouds(world, state, x, y, wind, cloudCover, ref newCloudCover);
						rainfall = UpdateRainfall(world, state, elevation, cloudCover, airTemperature, cloudElevation, ref newSurfaceWater, ref newCloudCover);
					}
					UpdatePressure(world, state, y, x, index, elevationOrSeaLevel, pressure, airTemperature, newTemperature, wind, out newPressure);

					if (float.IsNaN(newAirEnergy) || float.IsNaN(newEvaporation) || float.IsNaN(newSurfaceWater) || float.IsNaN(newSurfaceIce) || float.IsNaN(newGroundWater) || float.IsNaN(newHumidity) || float.IsNaN(newCloudCover) || float.IsNaN(newCloudElevation) || float.IsNaN(newPressure))
					{
						break;
					}
					nextState.AirEnergy[index] = newAirEnergy;
					nextState.LandEnergy[index] = newLandEnergy;
					nextState.Evaporation[index] = newEvaporation;
					nextState.SurfaceWater[index] = newSurfaceWater;
					nextState.SurfaceIce[index] = newSurfaceIce;
					nextState.GroundWater[index] = newGroundWater;
					nextState.Humidity[index] = newHumidity;
					nextState.Rainfall[index] = rainfall;
					nextState.CloudCover[index] = newCloudCover;
					nextState.CloudElevation[index] = newCloudElevation;
					nextState.Radiation[index] = newRadiation;
					nextState.Pressure[index] = newPressure;
					nextState.OceanEnergyShallow[index] = newOceanEnergyShallow;
					nextState.OceanEnergyDeep[index] = newOceanEnergyDeep;
					nextState.OceanSalinityDeep[index] = newOceanSalinityDeep;
					nextState.OceanSalinityShallow[index] = newOceanSalinityShallow;
					nextState.OceanDensityDeep[index] = GetOceanDensity(world, newOceanEnergyDeep, newOceanSalinityDeep, state.SeaLevel - elevation);
					nextState.AirTemperature[index] = newTemperature;

				}
			}

		}

		static public float GetOceanDensity(World world, float oceanTemperature, float oceanSalinity, float volume)
		{
			if (volume <= 0)
			{
				return 0;
			}
			return world.Data.OceanSalinityDensity * (oceanSalinity / volume) + world.Data.OceanTemperatureDensity * (world.Data.FreezingTemperature / oceanTemperature);
		}

		static private void UpdatePressure(World world, World.State state, int y, int x, int index, float elevationOrSeaLevel, float pressure, float temperature, float newTemperature, Vector3 wind, out float newPressure)
		{
			newPressure = pressure;
			newPressure += (world.Data.StaticPressure + (temperature - world.Data.StdTemp) * world.Data.temperatureToPressure - pressure) * world.Data.pressureEqualizationSpeed;

			newPressure -= wind.z * world.Data.verticalWindPressureAdjustment;
			newPressure += (newTemperature - temperature) * world.Data.temperatureDeltaToPressure;


		}

		static public float GetLocalTemperature(World world, float sunAngle, float cloudCover, float temperature, float lengthOfDay)
		{
			return temperature + (1.0f - Math.Min(cloudCover / world.Data.cloudContentFullAbsorption, 1.0f)) * sunAngle * world.Data.localSunHeat * lengthOfDay;
		}

		static public float GetPressureAtElevation(World world, World.State state, int index, float elevation)
		{
			// Units: Pascals
			// Barometric Formula
			// Pressure = StaticPressure * (StdTemp / (StdTemp + StdTempLapseRate * (Elevation - ElevationAtBottomOfAtmLayer)) ^ (GravitationalAcceleration * MolarMassOfEarthAir / (UniversalGasConstant * StdTempLapseRate))
			// https://en.wikipedia.org/wiki/Barometric_formula
			// For the bottom layer of atmosphere ( < 11000 meters), ElevationAtBottomOfAtmLayer == 0)

			//	float standardPressure = Data.StaticPressure * (float)Math.Pow(Data.StdTemp / (Data.StdTemp + Data.StdTempLapseRate * elevation), Data.PressureExponent);
			float pressure = world.Data.StaticPressure * (float)Math.Pow(world.Data.StdTemp / (world.Data.StdTemp + world.Data.StdTempLapseRate * elevation), world.Data.PressureExponent);
			return pressure;
		}


		static public Vector3 GetSunVector(World world, int ticks, float latitude)
		{

			float angleOfInclination = world.Data.planetTiltAngle * (float)Math.Sin(Math.PI * 2 * (world.GetTimeOfYear(ticks) - 0.25f));
			//float timeOfDay = (-sunPhase + 0.5f) * Math.PI * 2;
			float timeOfDay = (float)0;
			float azimuth = (float)Math.Atan2(Math.Sin(timeOfDay), Math.Cos(timeOfDay) * Math.Sin(latitude * Math.PI) - Math.Tan(angleOfInclination) * Math.Cos(latitude * Math.PI));
			float elevation = (float)Math.Asin((Math.Sin(latitude) * Math.Sin(angleOfInclination) + Math.Cos(latitude) * Math.Cos(angleOfInclination) * Math.Cos(timeOfDay)));

			float cosOfElevation = (float)Math.Cos(elevation);
			Vector3 sunVec = new Vector3((float)Math.Sin(azimuth) * cosOfElevation, (float)Math.Cos(azimuth) * cosOfElevation, (float)Math.Sin(elevation));
			return sunVec;
		}



		static private void MoveHumidityToClouds(World world, float elevationOrSeaLevel, float humidity, float localTemperature, float cloudElevation, Vector3 windAtSurface, ref float newHumidity, ref float newCloudCover)
		{
			float humidityToCloud = Mathf.Clamp(windAtSurface.z / cloudElevation + Math.Max(0, 1.0f - GetRelativeHumidity(world, localTemperature, humidity, cloudElevation, elevationOrSeaLevel)), 0, humidity);
			newHumidity -= humidityToCloud;
			newCloudCover += humidityToCloud;
		}

		static private void UpdateCloudElevation(World world, float elevationOrSeaLevel, float temperature, float humidity, float atmosphereMass, Vector3 windAtCloudElevation, ref float newCloudElevation)
		{
			float dewPointTemp = (float)Math.Pow(humidity / (world.Data.dewPointRange * atmosphereMass), 0.25f) * world.Data.dewPointTemperatureRange + world.Data.dewPointZero;
			float dewPointElevation = Math.Max(0, (dewPointTemp - temperature) / world.Data.temperatureLapseRate) + elevationOrSeaLevel;

			float desiredDeltaZ = dewPointElevation - newCloudElevation;
			newCloudElevation = elevationOrSeaLevel + 1000;
			//		newCloudElevation = newCloudElevation + desiredDeltaZ* Data.cloudElevationDeltaSpeed + windAtCloudElevation.z * Data.windVerticalCloudSpeedMultiplier;
			//	newCloudElevation = Mathf.Clamp(newCloudElevation, elevationOrSeaLevel+1, Data.stratosphereElevation);
		}

		static private void MoveClouds(World world, World.State state, int x, int y, Vector3 windAtCloudElevation, float cloudCover, ref float newCloudCover)
		{
			if (cloudCover > 0)
			{
				if (windAtCloudElevation.x != 0 || windAtCloudElevation.y != 0)
				{
					float cloudMove = Math.Min(cloudCover, (Math.Abs(windAtCloudElevation.x) + Math.Abs(windAtCloudElevation.y)) * world.Data.cloudMovementFromWind);
					newCloudCover -= cloudMove;

					for (int i = 0; i < 4; i++)
					{
						var neighborPoint = world.GetNeighbor(x, y, i);
						int neighborIndex = world.GetIndex(neighborPoint.x, neighborPoint.y);
						float nCloudCover = state.CloudCover[neighborIndex];
						if (nCloudCover > 0)
						{
							var nWindAtCloudElevation = state.Wind[neighborIndex];
							switch (i)
							{
								case 0:
									if (nWindAtCloudElevation.x > 0)
									{
										float nCloudMove = Math.Min(nCloudCover, (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y)) * world.Data.cloudMovementFromWind);
										newCloudCover += nCloudMove * nWindAtCloudElevation.x / (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y));
									}
									break;
								case 1:
									if (nWindAtCloudElevation.x < 0)
									{
										float nCloudMove = Math.Min(nCloudCover, (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y)) * world.Data.cloudMovementFromWind);
										newCloudCover += nCloudMove * -nWindAtCloudElevation.x / (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y));
									}
									break;
								case 2:
									if (nWindAtCloudElevation.y < 0)
									{
										float nCloudMove = Math.Min(nCloudCover, (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y)) * world.Data.cloudMovementFromWind);
										newCloudCover += nCloudMove * -nWindAtCloudElevation.y / (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y));
									}
									break;
								case 3:
									if (nWindAtCloudElevation.y > 0)
									{
										float nCloudMove = Math.Min(nCloudCover, (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y)) * world.Data.cloudMovementFromWind);
										newCloudCover += nCloudMove * nWindAtCloudElevation.y / (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y));
									}
									break;
							}
						}
					}
				}


			}
		}

		static private float UpdateRainfall(World world, World.State state, float elevation, float cloudCover, float temperature, float cloudElevation, ref float newSurfaceWater, ref float newCloudCover)
		{
			float temperatureAtCloudElevation = cloudElevation * world.Data.temperatureLapseRate + temperature;
			float rainPoint = Math.Max(0, (temperatureAtCloudElevation - world.Data.dewPointZero) * world.Data.rainPointTemperatureMultiplier);
			if (cloudCover > rainPoint)
			{
				float rainfall = (cloudCover - rainPoint) * world.Data.RainfallRate;
				newCloudCover -= rainfall;
				if (!world.IsOcean(elevation, state.SeaLevel))
				{
					newSurfaceWater += rainfall;
				}
				return rainfall;
			}
			return 0;
		}

		static private void FlowWater(World world, World.State state, int x, int y, Vector2 gradient, float soilFertility, ref float surfaceWater, ref float groundWater)
		{
			float flow = Math.Min(surfaceWater, (Math.Abs(gradient.x) + Math.Abs(gradient.y)));
			surfaceWater = Math.Max(surfaceWater - flow * world.Data.FlowSpeed, 0);
			groundWater = Math.Max(groundWater - world.Data.GroundWaterFlowSpeed * soilFertility, 0);


			for (int i = 0; i < 4; i++)
			{
				var neighborPoint = world.GetNeighbor(x, y, i);
				int neighborIndex = world.GetIndex(neighborPoint.x, neighborPoint.y);
				float nWater = state.SurfaceWater[neighborIndex];
				float nGroundWater = state.GroundWater[neighborIndex];
				if (nWater > 0)
				{
					var nGradient = state.FlowDirection[neighborIndex];
					var nGroundFlow = world.Data.GroundWaterFlowSpeed * state.SoilFertility[neighborIndex];
					switch (i)
					{
						case 0:
							if (nGradient.x > 0)
							{
								surfaceWater += nGradient.x * nWater * world.Data.FlowSpeed;
								groundWater += nGroundWater * nGroundFlow;
							}
							break;
						case 1:
							if (nGradient.x < 0)
							{
								surfaceWater += nGradient.x * nWater * world.Data.FlowSpeed;
							}
							break;
						case 2:
							if (nGradient.y < 0)
							{
								surfaceWater += nGradient.x * nWater * world.Data.FlowSpeed;
							}
							break;
						case 3:
							if (nGradient.y > 0)
							{
								surfaceWater += nGradient.x * nWater * world.Data.FlowSpeed;
							}
							break;
					}
				}
			}


		}

		static private void SeepWaterIntoGround(World world, float elevation, float seaLevel, float soilFertility, float waterTableDepth, ref float groundWater, ref float surfaceWater)
		{
			float maxGroundWater = soilFertility * waterTableDepth * world.Data.MaxSoilPorousness;
			if (!world.IsOcean(elevation, seaLevel))
			{
				float seepage = Math.Min(surfaceWater * soilFertility * world.Data.GroundWaterReplenishmentSpeed, maxGroundWater - groundWater);
				groundWater += seepage;
				surfaceWater -= seepage;
			}
			else
			{
				groundWater = maxGroundWater;
				surfaceWater = 0;
			}
		}

		static private float GetEvaporationRate(World world, float ice, float temperature, float humidity, float windSpeedAtSurface, float cloudElevation, float elevationOrSeaLevel)
		{
			if (ice > 0)
			{
				return 0;
			}
			float evapTemperature = 1.0f - Mathf.Clamp((temperature - world.Data.evapMinTemperature) / world.Data.evapTemperatureRange, 0, 1);
			float evapRate = world.Data.EvapRateTemperature * (1.0f - evapTemperature * evapTemperature);
			evapRate += world.Data.EvapRateWind * windSpeedAtSurface;

			float relativeHumidity = GetRelativeHumidity(world, temperature, humidity, cloudElevation, elevationOrSeaLevel);

			evapRate *= Math.Max(0.0f, 1.0f - relativeHumidity);
			return evapRate;
		}

		static public float GetRelativeHumidity(World world, float localTemperature, float humidity, float cloudElevation, float elevationOrSeaLevel)
		{
			float atmosphereMass = (cloudElevation - elevationOrSeaLevel) * world.Data.MolarMassEarthAir;
			float maxHumidity = atmosphereMass * world.Data.dewPointRange * Mathf.Clamp((localTemperature - world.Data.dewPointZero) / world.Data.dewPointTemperatureRange, 0, 1);
			float relativeHumidity = humidity / maxHumidity;
			return relativeHumidity;
		}

		static private void EvaporateWater(World world, float evapRate, float elevation, float seaLevel, float groundWater, float waterTableDepth, ref float humidity, ref float newAirEnergy, ref float newOceanEnergy, ref float newGroundWater, ref float surfaceWater, out float evaporation)
		{
			evaporation = 0;
			if (evapRate <= 0)
			{
				return;
			}
			if (world.IsOcean(elevation, seaLevel))
			{
				humidity += evapRate;
				evaporation += evapRate;
			}
			else
			{
				if (surfaceWater > 0)
				{
					float waterSurfaceArea = Math.Min(1.0f, (float)Math.Sqrt(surfaceWater));
					float evap = Math.Max(0, Math.Min(surfaceWater, waterSurfaceArea * evapRate));
					surfaceWater -= evap;
					humidity += evap;
					evaporation += evap;
				}
				var groundWaterEvap = Math.Max(0, Math.Min(newGroundWater, groundWater / waterTableDepth * evapRate));
				newGroundWater -= groundWaterEvap;
				humidity += groundWaterEvap;
				evaporation += groundWaterEvap;
			}

			float evapotranspiration = evaporation * world.Data.EvaporativeHeatLoss;
			newAirEnergy += evapotranspiration;
			newOceanEnergy -= evapotranspiration;

		}

		static private void MoveAtmosphereOnWind(World world, World.State state, int x, int y, float elevationOrSeaLevel, float airEnergy, float humidity, Vector3 windAtSurface, ref float newHumidity, ref float newEnergy)
		{

			// in high pressure systems, air from the upper atmosphere will cool us
			//if (windAtSurface.z < 0)
			//{
			//	newEnergy += world.Data.upperAtmosphereCoolingRate * windAtSurface.z;
			//}
			float atmosphereHeight = world.Data.stratosphereElevation - elevationOrSeaLevel;
			
			newEnergy -= (windAtSurface.x + windAtSurface.y) * airEnergy * world.Data.energyWindMovement;
			if (windAtSurface.x != 0 || windAtSurface.y != 0)
			{
				newHumidity = Math.Max(0, newHumidity - humidity * Math.Min(1.0f, (Math.Abs(windAtSurface.x) + Math.Abs(windAtSurface.y)) * world.Data.humidityLossFromWind));
			}
			for (int i = 0; i < 4; i++)
			{
				var neighbor = world.GetNeighbor(x, y, i);
				int nIndex = world.GetIndex(neighbor.x, neighbor.y);
				float nAirEnergy = state.AirEnergy[nIndex];
				float nHumidity = state.Humidity[nIndex];
				var nWind = state.Wind[nIndex];
				float nElevationOrSeaLevel = Math.Max(0, state.Elevation[nIndex]);
				float nAtmosphereHeight = world.Data.stratosphereElevation - nElevationOrSeaLevel;

				// Mixing
				float mixingElevation = Math.Min(atmosphereHeight, nAtmosphereHeight);
				float energyTransfer = (nAirEnergy / nAtmosphereHeight - airEnergy / atmosphereHeight) * world.Data.airEnergyDispersalSpeed * mixingElevation;
				newEnergy += energyTransfer;
				newHumidity += (nHumidity - humidity) * world.Data.humidityDispersalSpeed;

				// Blowing on wind
				switch (i)
				{
					case 0:
						if (nWind.x > 0)
						{
							newEnergy += nAirEnergy * Math.Min(0.5f, nWind.x * world.Data.energyWindMovement);
							newHumidity += nHumidity * Math.Min(0.5f, nWind.x * world.Data.humidityLossFromWind);
						}
						break;
					case 1:
						if (nWind.x < 0)
						{
							newEnergy += nAirEnergy * Math.Min(0.5f, -nWind.x * world.Data.energyWindMovement);
							newHumidity += nHumidity * Math.Min(0.5f, -windAtSurface.y * world.Data.humidityLossFromWind);
						}
						break;
					case 2:
						if (nWind.y < 0)
						{
							newEnergy += nAirEnergy * Math.Min(0.5f, -nWind.y * world.Data.energyWindMovement);
							newHumidity += nHumidity * Math.Min(0.5f, -windAtSurface.y * world.Data.humidityLossFromWind);
						}
						break;
					case 3:
						if (nWind.y > 0)
						{
							newEnergy += nAirEnergy * Math.Min(0.5f, nWind.y * world.Data.energyWindMovement);
							newHumidity += nHumidity * Math.Min(0.5f, windAtSurface.y * world.Data.humidityLossFromWind);
						}
						break;
				}
			}
		}


		static private void MoveOceanOnCurrent(
		World world,
		World.State state,
		int x,
		int y,
		float elevation,
		float ice,
		float oceanEnergyShallow,
		float oceanEnergyDeep,
		float oceanSalinityShallow,
		float oceanSalinityDeep,
		float oceanTemperatureShallow,
		float oceanTemperatureDeep,
		float oceanDensity,
		Vector3 currentShallow,
		Vector3 currentDeep,
		ref float newOceanEnergyShallow,
		ref float newOceanEnergyDeep,
		ref float newOceanSalinityShallow,
		ref float newOceanSalinityDeep,
		ref float newTemperature)
		{
			float depth = state.SeaLevel - elevation;
			if (depth <= 0)
			{
				return;
			}

			float salinityDeepPercentage = oceanSalinityDeep / depth;

			//		if (depth > surfaceTemperatureDepth)
			{
				//			float surfacePercent = surfaceTemperatureDepth / depth;
				//			float surfacePercent = 0.1f;
				float surfaceDensity = GetOceanDensity(world, oceanEnergyShallow, oceanSalinityShallow, world.Data.DeepOceanDepth);


				if (oceanEnergyShallow <= world.Data.FreezingTemperature + 5)
				{
					float salinityExchange = oceanSalinityShallow * world.Data.oceanSalinityIncrease;
					newOceanSalinityDeep += salinityExchange;
					newOceanSalinityShallow -= salinityExchange;
					newOceanEnergyDeep = world.Data.FreezingTemperature;
				}
				else
				{
					float salinityExchange = (oceanSalinityDeep / depth - oceanSalinityShallow / world.Data.DeepOceanDepth) * world.Data.salinityMixingSpeed * (oceanSalinityShallow + oceanSalinityDeep);
					newOceanSalinityDeep -= salinityExchange * depth / (depth + world.Data.DeepOceanDepth);
					newOceanSalinityShallow += salinityExchange * world.Data.DeepOceanDepth / (depth + world.Data.DeepOceanDepth);

					float deepWaterMixingDepth = Math.Min(world.Data.DeepOceanDepth, depth);
					float heatExchange = (oceanTemperatureDeep - oceanTemperatureShallow) * deepWaterMixingDepth * world.Data.SpecificHeatSeaWater * world.Data.temperatureMixingSpeed;
					newOceanEnergyShallow += heatExchange;
					newOceanEnergyDeep -= heatExchange;
				}

				if (currentShallow.z < 0)
				{
					float downwelling = Math.Min(0.5f, -currentShallow.z * world.Data.downwellingSpeed);
					float energyExchange = oceanEnergyShallow * downwelling;
					newOceanEnergyShallow -= energyExchange;
					newOceanEnergyDeep += energyExchange;
					float salinityExchange = oceanSalinityShallow * downwelling;
					newOceanSalinityDeep += salinityExchange;
					newOceanSalinityShallow -= salinityExchange;
				}
				else if (currentShallow.z > 0)
				{
					float upwelling = Math.Min(0.5f, currentShallow.z * world.Data.upwellingSpeed);
					float mixingDepth = Math.Min(depth, world.Data.DeepOceanDepth) / depth;
					float energyExchange = oceanEnergyDeep * mixingDepth * upwelling;
					newOceanEnergyShallow += energyExchange;
					newOceanEnergyDeep -= energyExchange;
					float salinityExchange = oceanSalinityDeep * mixingDepth * upwelling;
					newOceanSalinityDeep -= salinityExchange;
					newOceanSalinityShallow += salinityExchange;
				}
			}

			for (int i = 0; i < 4; i++)
			{
				var neighbor = world.GetNeighbor(x, y, i);
				int nIndex = world.GetIndex(neighbor.x, neighbor.y);
				float neighborDepth = state.SeaLevel - state.Elevation[nIndex];
				if (neighborDepth > 0)
				{
					var neighborCurrentDeep = state.OceanCurrentDeep[nIndex];
					var neighborCurrentShallow = state.OceanCurrentShallow[nIndex];

					float nEnergyShallow = state.OceanEnergyShallow[nIndex];
					float nEnergyDeep = state.OceanEnergyDeep[nIndex];
					float nTemperatureShallow = GetWaterTemperature(world, nEnergyShallow, world.Data.DeepOceanDepth);
					float nTemperatureDeep = GetWaterTemperature(world, nEnergyDeep, neighborDepth);
					float nSalinityShallow = state.OceanSalinityShallow[nIndex];
					float nSalinityDeep = state.OceanSalinityDeep[nIndex];

					// Horizontal mixing
					float mixingDepth = Math.Min(neighborDepth, depth);
					newOceanEnergyShallow += world.Data.SpecificHeatSeaWater * world.Data.DeepOceanDepth * (nTemperatureShallow - oceanTemperatureShallow) * world.Data.horizontalMixing;
					newOceanEnergyDeep += world.Data.SpecificHeatSeaWater * mixingDepth * (nTemperatureDeep - oceanTemperatureDeep) * world.Data.horizontalMixing;

					float nSalinityDeepPercentage = nSalinityDeep / neighborDepth;
					newOceanSalinityDeep += (nSalinityDeepPercentage - salinityDeepPercentage) * world.Data.horizontalMixing * Math.Min(neighborDepth, depth);
					newOceanSalinityShallow += (nSalinityShallow - oceanSalinityShallow) * world.Data.horizontalMixing;

					switch (i)
					{
						case 0:
							if (neighborCurrentShallow.x > 0)
							{
								float absX = Math.Abs(neighborCurrentShallow.x);
								newOceanEnergyShallow += nEnergyShallow * Math.Min(0.25f, absX * world.Data.oceanEnergyMovement);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (currentShallow.x < 0)
							{
								float absX = Math.Abs(currentShallow.x);
								newOceanEnergyShallow -= oceanEnergyShallow * Math.Min(0.25f, absX * world.Data.oceanEnergyMovement);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (neighborCurrentDeep.x > 0)
							{
								float absX = Math.Abs(neighborCurrentDeep.x);
								newOceanEnergyDeep += nEnergyDeep * Math.Min(0.25f, absX * world.Data.oceanEnergyMovement);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (currentDeep.x < 0)
							{
								float absX = Math.Abs(currentDeep.x);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							break;
						case 1:
							if (neighborCurrentShallow.x < 0)
							{
								float absX = Math.Abs(neighborCurrentShallow.x);
								newOceanEnergyShallow += nEnergyShallow * Math.Min(0.25f, absX * world.Data.oceanEnergyMovement);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (currentShallow.x > 0)
							{
								float absX = Math.Abs(currentShallow.x);
								newOceanEnergyShallow -= oceanEnergyShallow * Math.Min(0.25f, absX * world.Data.oceanEnergyMovement);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (neighborCurrentDeep.x < 0)
							{
								float absX = Math.Abs(neighborCurrentDeep.x);
								newOceanEnergyDeep += nEnergyDeep * Math.Min(0.25f, absX * world.Data.oceanEnergyMovement);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (currentDeep.x > 0)
							{
								float absX = Math.Abs(currentDeep.x);
								newOceanEnergyDeep -= oceanEnergyDeep * Math.Min(0.25f, absX * world.Data.oceanEnergyMovement);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							break;
						case 2:
							if (neighborCurrentShallow.y < 0)
							{
								float absY = Math.Abs(neighborCurrentShallow.y);
								newOceanEnergyShallow += nEnergyShallow * Math.Min(0.25f, absY * world.Data.oceanEnergyMovement);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (currentShallow.y > 0)
							{
								float absY = Math.Abs(currentShallow.y);
								newOceanEnergyShallow -= oceanEnergyShallow * Math.Min(0.25f, absY * world.Data.oceanEnergyMovement);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (neighborCurrentDeep.y < 0)
							{
								float absY = Math.Abs(neighborCurrentDeep.y);
								newOceanEnergyDeep += nEnergyDeep * Math.Min(0.25f, absY * world.Data.oceanEnergyMovement);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (currentDeep.y > 0)
							{
								float absY = Math.Abs(currentDeep.y);
								newOceanEnergyDeep -= oceanEnergyDeep * Math.Min(0.25f, absY * world.Data.oceanEnergyMovement);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							break;
						case 3:
							if (neighborCurrentShallow.y > 0)
							{
								float absY = Math.Abs(neighborCurrentShallow.y);
								newOceanEnergyShallow += nEnergyShallow * Math.Min(0.25f, absY * world.Data.oceanEnergyMovement);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (currentShallow.y < 0)
							{
								float absY = Math.Abs(currentShallow.y);
								newOceanEnergyShallow -= oceanEnergyShallow * Math.Min(0.25f, absY * world.Data.oceanEnergyMovement);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (neighborCurrentDeep.y > 0)
							{
								float absY = Math.Abs(neighborCurrentDeep.y);
								newOceanEnergyDeep += nEnergyDeep * Math.Min(0.25f, absY * world.Data.oceanEnergyMovement);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (currentDeep.y < 0)
							{
								float absY = Math.Abs(currentDeep.y);
								newOceanEnergyDeep -= oceanEnergyDeep * Math.Min(0.25f, absY * world.Data.oceanEnergyMovement);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							break;
					}
				}
			}
			newOceanSalinityDeep = Math.Max(0, newOceanSalinityDeep);
			newOceanSalinityShallow = Math.Max(0, newOceanSalinityShallow);
		}

		static private float GetAtmosphereMass(World world, float elevation, float elevationOrSeaLevel)
		{
			float atmosphereMass;
			if (elevation <= world.Data.troposphereElevation)
			{
				atmosphereMass = (world.Data.troposphereElevation - elevationOrSeaLevel) / world.Data.troposphereAtmosphereContent;
			}
			else
			{
				atmosphereMass = world.Data.troposphereElevation + (world.Data.stratosphereElevation - elevationOrSeaLevel) * (1.0f - world.Data.troposphereAtmosphereContent) * world.Data.troposphereElevation;
			}

			return atmosphereMass;
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
		static public float GetLengthOfDay(float latitude, float timeOfYear, float declinationOfSun)
		{
			float latitudeAngle = latitude * Mathf.PI / 2;
			if ((latitude > 0) != (declinationOfSun > 0))
			{
				float hemisphere = Mathf.Sign(latitude);
				float noSunsetLatitude = (Mathf.PI / 2 + hemisphere * declinationOfSun);
				if (latitudeAngle * hemisphere >= noSunsetLatitude)
				{
					return 1;
				}
			} else if ((latitude > 0) == (declinationOfSun > 0))
			{
				float hemisphere = Mathf.Sign(latitude);
				float noSunsetLatitude = Mathf.PI / 2 - hemisphere * declinationOfSun;
				if (latitudeAngle * hemisphere >= noSunsetLatitude)
				{
					return 0;
				}
			}

			float hourAngle = Mathf.Acos(-Mathf.Tan(-latitudeAngle) * Mathf.Tan(declinationOfSun));
			float lengthOfDay = hourAngle / Mathf.PI;
			return lengthOfDay;
		}

		static public float GetAirTemperature(World world, float energy, float elevationOrSeaLevel)
		{
			return energy / (GetAirMass(world, elevationOrSeaLevel) * world.Data.SpecificHeatAtmosphere);
		}
		static public float GetAirEnergy(World world, float temperature, float elevationOrSeaLevel)
		{
			return temperature * GetAirMass(world, elevationOrSeaLevel) * world.Data.SpecificHeatAtmosphere;
		}

		static public float GetWaterTemperature(World world, float energy, float depth)
		{
			return energy / (world.Data.SpecificHeatSeaWater * depth);
		}
		static public float GetWaterEnergy(World world, float temperature, float depth)
		{
			return temperature * (world.Data.SpecificHeatSeaWater * depth);
		}
		static public float GetAirMass(World world, float elevationOrSeaLevel)
		{
			return world.Data.stratosphereElevation - elevationOrSeaLevel; 
		}
	}
}