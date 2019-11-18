using System;
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
			for (int y = 0; y < world.Size; y++)
			{
				float latitude = world.GetLatitude(y);
				var sunVector = GetSunVector(world, state.Ticks, latitude);
				float sunAngle = Math.Max(0, sunVector.z);

				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);

					float elevation = state.Elevation[index];
					float elevationOrSeaLevel = Math.Max(state.SeaLevel, elevation);
					float cloudCover = state.CloudCover[index];
					float temperature = state.Temperature[index];
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
					float oceanTemperatureDeep = state.OceanTemperatureDeep[index];
					float oceanTemperatureShallow = state.OceanTemperatureShallow[index];
					float oceanDensity = state.OceanDensityDeep[index];
					float oceanSalinityDeep = state.OceanSalinityDeep[index];
					float oceanSalinityShallow = state.OceanSalinityShallow[index];
					var wind = state.Wind[index];
					var currentDeep = state.OceanCurrentDeep[index];
					var currentShallow = state.OceanCurrentShallow[index];


					float newEvaporation;
					float newGroundWater = groundWater;
					float newHumidity = humidity;
					float newPressure;
					float newCloudCover = cloudCover;
					float newSurfaceWater = surfaceWater;
					float newSurfaceIce = surfaceIce;
					float newTemperature = temperature;
					float newCloudElevation = cloudElevation;
					float rainfall = 0;
					float newRadiation = radiation;
					float newOceanTemperatureDeep = oceanTemperatureDeep;
					float newOceanTemperatureShallow = oceanTemperatureShallow;
					float newOceanSalinityDeep = oceanSalinityDeep;
					float newOceanSalinityShallow = oceanSalinityShallow;
					float windSpeed = wind.magnitude;


					float atmosphereMass = GetAtmosphereMass(world, elevation, elevationOrSeaLevel);
					float airPressureInverse = world.Data.StaticPressure / pressure;
					float tempWithSunAtGround = GetLocalTemperature(world, sunAngle, cloudCover, temperature);
					float evapRate = GetEvaporationRate(world, surfaceIce, tempWithSunAtGround, humidity, wind.magnitude, cloudElevation, elevationOrSeaLevel);
					float cloudOpacity = Math.Min(1.0f, cloudCover / world.Data.cloudContentFullAbsorption);
					float heatFromSun = GetHeatFromSun(world, state.SeaLevel, elevation, surfaceIce, cloudOpacity, terrainNormal, humidity, atmosphereMass, sunAngle, sunVector);

					UpdateTemperature(world, heatFromSun, cloudOpacity, temperature, airPressureInverse, humidity, atmosphereMass, ref newTemperature);
					MoveOceanOnCurrent(world, state, x, y, elevation, heatFromSun, temperature, surfaceIce, oceanTemperatureShallow, oceanTemperatureDeep, oceanSalinityShallow, oceanSalinityDeep, oceanDensity, currentShallow, currentDeep, ref newOceanTemperatureShallow, ref newOceanTemperatureDeep, ref newOceanSalinityShallow, ref newOceanSalinityDeep, ref newTemperature);
					MoveAtmosphereOnWind(world, state, x, y, temperature, humidity, wind, ref newHumidity, ref newTemperature);
					SimulateIce(world, elevation, state.SeaLevel, tempWithSunAtGround, ref newSurfaceWater, ref newSurfaceIce);
					FlowWater(world, state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
					SeepWaterIntoGround(world, elevation, state.SeaLevel, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);
					EvaporateWater(world, evapRate, elevation, state.SeaLevel, groundWater, waterTableDepth, ref newHumidity, ref newTemperature, ref newGroundWater, ref newSurfaceWater, out newEvaporation);
					//			MoveHumidityToClouds(elevation, humidity, tempWithSunAtGround, cloudElevation, windAtSurface, ref newHumidity, ref newCloudCover);
					if (cloudCover > 0)
					{
						UpdateCloudElevation(world, elevationOrSeaLevel, temperature, humidity, atmosphereMass, wind, ref newCloudElevation);
						MoveClouds(world, state, x, y, wind, cloudCover, ref newCloudCover);
						rainfall = UpdateRainfall(world, state, elevation, cloudCover, temperature, cloudElevation, ref newSurfaceWater, ref newCloudCover);
					}
					UpdatePressure(world, state, y, x, index, elevationOrSeaLevel, pressure, temperature, newTemperature, wind, out newPressure);

					if (float.IsNaN(newTemperature) || float.IsNaN(newEvaporation) || float.IsNaN(newSurfaceWater) || float.IsNaN(newSurfaceIce) || float.IsNaN(newGroundWater) || float.IsNaN(newHumidity) || float.IsNaN(newCloudCover) || float.IsNaN(newCloudElevation) || float.IsNaN(newPressure))
					{
						break;
					}
					nextState.Temperature[index] = newTemperature;
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
					nextState.OceanTemperatureShallow[index] = newOceanTemperatureShallow;
					nextState.OceanTemperatureDeep[index] = newOceanTemperatureDeep;
					nextState.OceanSalinityDeep[index] = newOceanSalinityDeep;
					nextState.OceanSalinityShallow[index] = newOceanSalinityShallow;
					nextState.OceanDensityDeep[index] = GetOceanDensity(world, newOceanTemperatureDeep, newOceanSalinityDeep, state.SeaLevel - elevation);

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
			newPressure = world.Data.StaticPressure;

			newPressure -= wind.z * world.Data.verticalWindPressureAdjustment;
			newPressure -= (temperature - (world.Data.StdTemp - world.Data.StdTempLapseRate)) * world.Data.temperatureToPressure;
		}

		static public float GetLocalTemperature(World world, float sunAngle, float cloudCover, float temperature)
		{
			return temperature + (1.0f - Math.Min(cloudCover / world.Data.cloudContentFullAbsorption, 1.0f)) * sunAngle * world.Data.localSunHeat;
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
				if (elevation > state.SeaLevel)
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

		static private void SimulateIce(World world, float elevation, float seaLevel, float localTemperature, ref float surfaceWater, ref float surfaceIce)
		{
			if (localTemperature <= world.Data.FreezingTemperature)
			{
				float frozen = world.Data.iceFreezeRate * (world.Data.FreezingTemperature - localTemperature) * (1.0f - (float)Math.Pow(Math.Min(1.0f, surfaceIce / world.Data.maxIce), 2));
				if (elevation > seaLevel)
				{
					frozen = Math.Min(frozen, surfaceWater);
					surfaceWater -= frozen;
				}
				surfaceIce += frozen;
			}
			else if (surfaceIce > 0)
			{
				float meltRate = (localTemperature - world.Data.FreezingTemperature) * world.Data.iceMeltRate;
				float melted = Math.Min(surfaceIce, meltRate);
				surfaceIce -= melted;
				if (elevation > seaLevel)
				{
					surfaceWater += melted;
				}
			}
		}
		static private void SeepWaterIntoGround(World world, float elevation, float seaLevel, float soilFertility, float waterTableDepth, ref float groundWater, ref float surfaceWater)
		{
			float maxGroundWater = soilFertility * waterTableDepth * world.Data.MaxSoilPorousness;
			if (elevation > seaLevel)
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

		static private float GetEvaporationRate(World world, float ice, float localTemperature, float humidity, float windSpeedAtSurface, float cloudElevation, float elevationOrSeaLevel)
		{
			if (ice > 0)
			{
				return 0;
			}
			float evapTemperature = 1.0f - Mathf.Clamp((localTemperature - world.Data.evapMinTemperature) / world.Data.evapTemperatureRange, 0, 1);
			float evapRate = world.Data.EvapRateTemperature * (1.0f - evapTemperature * evapTemperature);
			evapRate += world.Data.EvapRateWind * windSpeedAtSurface;

			float relativeHumidity = GetRelativeHumidity(world, localTemperature, humidity, cloudElevation, elevationOrSeaLevel);

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

		static private void EvaporateWater(World world, float evapRate, float elevation, float seaLevel, float groundWater, float waterTableDepth, ref float humidity, ref float temperature, ref float newGroundWater, ref float surfaceWater, out float evaporation)
		{
			evaporation = 0;
			if (evapRate <= 0)
			{
				return;
			}
			if (elevation <= seaLevel)
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

			//	temperature -= evaporation * Data.EvaporativeCoolingRate;
		}

		static private void MoveAtmosphereOnWind(World world, World.State state, int x, int y, float temperature, float humidity, Vector3 windAtSurface, ref float newHumidity, ref float newTemperature)
		{
			float temperatureDispersalSpeed = 0.01f;
			float humidityDispersalSpeed = 0.01f;

			// in high pressure systems, air from the upper atmosphere will cool us
			if (windAtSurface.z < 0)
			{
				newTemperature += world.Data.upperAtmosphereCoolingRate * windAtSurface.z;
			}

			if (windAtSurface.x != 0 || windAtSurface.y != 0)
			{
				newHumidity = Math.Max(0, newHumidity - humidity * Math.Min(1.0f, (Math.Abs(windAtSurface.x) + Math.Abs(windAtSurface.y)) * world.Data.humidityLossFromWind));
			}
			for (int i = 0; i < 4; i++)
			{
				var neighbor = world.GetNeighbor(x, y, i);
				int nIndex = world.GetIndex(neighbor.x, neighbor.y);
				float nTemperature = state.Temperature[nIndex];
				float nHumidity = state.Humidity[nIndex];
				newTemperature += (nTemperature - temperature) * temperatureDispersalSpeed;
				newHumidity += (nHumidity - humidity) * humidityDispersalSpeed;
				switch (i)
				{
					case 0:
						if (windAtSurface.x > 0)
						{
							newTemperature += (nTemperature - temperature) * Math.Min(1.0f, windAtSurface.x * world.Data.temperatureEqualizationFromWind);
							newHumidity += nHumidity * Math.Min(1.0f, windAtSurface.x * world.Data.humidityLossFromWind);
						}
						break;
					case 1:
						if (windAtSurface.x < 0)
						{
							newTemperature += (nTemperature - temperature) * Math.Max(-1.0f, windAtSurface.x * world.Data.temperatureEqualizationFromWind);
							newHumidity += nHumidity * Math.Min(1.0f, -windAtSurface.y * world.Data.humidityLossFromWind);
						}
						break;
					case 2:
						if (windAtSurface.y < 0)
						{
							newTemperature += (nTemperature - temperature) * Math.Max(-1.0f, windAtSurface.y * world.Data.temperatureEqualizationFromWind);
							newHumidity += nHumidity * Math.Min(1.0f, -windAtSurface.y * world.Data.humidityLossFromWind);
						}
						break;
					case 3:
						if (windAtSurface.y > 0)
						{
							newTemperature += (nTemperature - temperature) * Math.Min(1.0f, windAtSurface.y * world.Data.temperatureEqualizationFromWind);
							newHumidity += nHumidity * Math.Min(1.0f, windAtSurface.y * world.Data.humidityLossFromWind);
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
			float heatFromSun,
			float temperature,
			float ice,
			float oceanTemperatureShallow,
			float oceanTemperatureDeep,
			float oceanSalinityShallow,
			float oceanSalinityDeep,
			float oceanDensity,
			Vector3 currentShallow,
			Vector3 currentDeep,
			ref float newOceanTemperatureShallow,
			ref float newOceanTemperatureDeep,
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

			newOceanTemperatureShallow += heatFromSun * world.Data.heatAbsorptionWater;
			float heatExchangeAir = ice > 0 ? 0 : (oceanTemperatureShallow - temperature) * world.Data.heatExchangeAirSpeed;
			newOceanTemperatureShallow -= heatExchangeAir;
			newTemperature += heatExchangeAir;


			//		if (depth > surfaceTemperatureDepth)
			{
				//			float surfacePercent = surfaceTemperatureDepth / depth;
				//			float surfacePercent = 0.1f;
				float surfaceDensity = GetOceanDensity(world, oceanTemperatureShallow, oceanSalinityShallow, world.Data.DeepOceanDepth);


				if (oceanTemperatureShallow <= world.Data.FreezingTemperature + 5)
				{
					float salinityExchange = oceanSalinityShallow * world.Data.oceanSalinityIncrease;
					newOceanSalinityDeep += salinityExchange;
					newOceanSalinityShallow -= salinityExchange;
					newOceanTemperatureDeep = world.Data.FreezingTemperature;
				}
				else
				{
					float salinityExchange = (oceanSalinityDeep / depth - oceanSalinityShallow / world.Data.DeepOceanDepth) * world.Data.salinityMixingSpeed * (oceanSalinityShallow + oceanSalinityDeep);
					newOceanSalinityDeep -= salinityExchange * depth / (depth + world.Data.DeepOceanDepth);
					newOceanSalinityShallow += salinityExchange * world.Data.DeepOceanDepth / (depth + world.Data.DeepOceanDepth);

					float heatExchange = (oceanTemperatureDeep - oceanTemperatureShallow) * world.Data.temperatureMixingSpeed / (depth + world.Data.DeepOceanDepth);
					newOceanTemperatureShallow += heatExchange * depth;
					newOceanTemperatureDeep -= heatExchange * world.Data.DeepOceanDepth;
				}

				if (currentShallow.z < 0)
				{
					float downwelling = -currentShallow.z * world.Data.downwellingSpeed;
					newOceanTemperatureDeep += (oceanTemperatureShallow - oceanTemperatureDeep) * Math.Min(0.5f, downwelling / depth);
					float salinityExchange = Math.Min(0.5f, downwelling * oceanSalinityShallow / world.Data.DeepOceanDepth);
					newOceanSalinityDeep += salinityExchange;
					newOceanSalinityShallow -= salinityExchange;
				}
				else if (currentShallow.z > 0)
				{
					float upwelling = currentShallow.z * world.Data.upwellingSpeed;
					newOceanTemperatureShallow += (oceanTemperatureDeep - oceanTemperatureShallow) * Math.Min(0.5f, upwelling / world.Data.DeepOceanDepth);
					float salinityExchange = Math.Min(0.5f, upwelling * oceanSalinityDeep / depth);
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

					float nTemperatureShallow = state.OceanTemperatureShallow[nIndex];
					float nTemperatureDeep = state.OceanTemperatureDeep[nIndex];
					float nSalinityShallow = state.OceanSalinityShallow[nIndex];
					float nSalinityDeep = state.OceanSalinityDeep[nIndex];

					newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * world.Data.horizontalMixing;
					newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * world.Data.horizontalMixing;
					float nSalinityDeepPercentage = nSalinityDeep / neighborDepth;
					newOceanSalinityDeep += (nSalinityDeepPercentage - salinityDeepPercentage) * world.Data.horizontalMixing * Math.Min(neighborDepth, depth);

					newOceanSalinityShallow += (nSalinityShallow - oceanSalinityShallow) * world.Data.horizontalMixing;

					switch (i)
					{
						case 0:
							if (neighborCurrentShallow.x > 0)
							{
								float absX = Math.Abs(neighborCurrentShallow.x);
								newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Math.Min(0.25f, absX * world.Data.oceanTemperatureMovement);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (currentShallow.x < 0)
							{
								float absX = Math.Abs(currentShallow.x);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (neighborCurrentDeep.x > 0)
							{
								float absX = Math.Abs(neighborCurrentDeep.x);
								newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Math.Min(0.25f, absX * world.Data.oceanTemperatureMovement);
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
								newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Math.Min(0.25f, absX * world.Data.oceanTemperatureMovement);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (currentShallow.x > 0)
							{
								float absX = Math.Abs(currentShallow.x);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (neighborCurrentDeep.x < 0)
							{
								float absX = Math.Abs(neighborCurrentDeep.x);
								newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Math.Min(0.25f, absX * world.Data.oceanTemperatureMovement);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							if (currentDeep.x > 0)
							{
								float absX = Math.Abs(currentDeep.x);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absX * world.Data.oceanSalinityMovement);
							}
							break;
						case 2:
							if (neighborCurrentShallow.y < 0)
							{
								float absY = Math.Abs(neighborCurrentShallow.y);
								newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Math.Min(0.25f, absY * world.Data.oceanTemperatureMovement);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (currentShallow.y > 0)
							{
								float absY = Math.Abs(currentShallow.y);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (neighborCurrentDeep.y < 0)
							{
								float absY = Math.Abs(neighborCurrentDeep.y);
								newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Math.Min(0.25f, absY * world.Data.oceanTemperatureMovement);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (currentDeep.y > 0)
							{
								float absY = Math.Abs(currentDeep.y);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							break;
						case 3:
							if (neighborCurrentShallow.y > 0)
							{
								float absY = Math.Abs(neighborCurrentShallow.y);
								newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Math.Min(0.25f, absY * world.Data.oceanTemperatureMovement);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (currentShallow.y < 0)
							{
								float absY = Math.Abs(currentShallow.y);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (neighborCurrentDeep.y > 0)
							{
								float absY = Math.Abs(neighborCurrentDeep.y);
								newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Math.Min(0.25f, absY * world.Data.oceanTemperatureMovement);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							if (currentDeep.y < 0)
							{
								float absY = Math.Abs(currentDeep.y);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absY * world.Data.oceanSalinityMovement);
							}
							break;
					}
				}
			}
			newOceanSalinityDeep = Math.Max(0, newOceanSalinityDeep);
			newOceanSalinityShallow = Math.Max(0, newOceanSalinityShallow);
		}

		static private float GetHeatFromSun(World world, float seaLevel, float elevation, float ice, float cloudOpacity, Vector3 terrainNormal, float humidity, float atmosphereMass, float sunAngle, Vector3 sunVector)
		{
			// TEMPERATURE
			float cloudAbsorptionFactor = world.Data.cloudAbsorptionRate * cloudOpacity;
			float cloudReflectionFactor = world.Data.cloudReflectionRate * cloudOpacity;
			float humidityPercentage = humidity / atmosphereMass;


			float gain = 0;
			float cloudGain = 0;
			float cloudReflection = 0;
			float reflection = 0;
			if (sunAngle > 0)
			{
				cloudGain = sunAngle * world.Data.heatGainFromSun * cloudAbsorptionFactor;
				cloudReflection = sunAngle * world.Data.heatGainFromSun * cloudReflectionFactor;

				// gain any heat not absorbed on first pass through the clouds
				float slope = 1;
				if (ice > 0)
				{
					reflection = world.Data.heatReflectionIce;
				}
				else if (elevation <= seaLevel) // ocean
				{
					reflection = world.Data.heatReflectionWater + world.Data.heatAbsorptionWater;
				}
				else // land
				{
					slope = Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
					// reflection = mineralTypes[cells[i, j].mineral].heatReflection;
					reflection = world.Data.HeatReflectionLand;
				}
				float sunGain = slope * world.Data.heatGainFromSun - cloudGain - cloudReflection;
				gain += sunGain * (1.0f - reflection) * (1.0f - humidityPercentage);
			}
			return gain;
		}
		static private void UpdateTemperature(World world, float heatFromSun, float cloudOpacity, float temperature, float airPressureInverse, float humidity, float atmosphereMass, ref float newTemperature)
		{
			// TEMPERATURE
			float cloudReflectionFactor = world.Data.cloudReflectionRate * cloudOpacity;
			float humidityPercentage = humidity / atmosphereMass;

			float heatLossFactor = (1.0f - world.Data.carbonDioxide * world.Data.heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
			float loss = temperature * (1.0f - cloudReflectionFactor) * (world.Data.heatLoss * heatLossFactor * airPressureInverse);

			newTemperature += heatFromSun - loss;

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


	}
}