using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public partial class World
{


	public void TickAtmosphere(State state, State nextState)
	{
		float timeOfYear = GetTimeOfYear(state.Ticks);
		for (int y = 0; y < Size; y++)
		{
			float latitude = GetLatitude(y);
			var sunVector = GetSunVector(state.Ticks, latitude);
			float sunAngle = Math.Max(0, sunVector.z);

			for (int x = 0; x < Size; x++)
			{
				int index = GetIndex(x, y);

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


				float atmosphereMass = GetAtmosphereMass(elevation, elevationOrSeaLevel);
				float airPressureInverse = Data.StaticPressure / pressure;
				float tempWithSunAtGround = GetLocalTemperature(sunAngle, cloudCover, temperature);
				float evapRate = GetEvaporationRate(surfaceIce, tempWithSunAtGround, humidity, wind.magnitude, cloudElevation, elevationOrSeaLevel);
				float cloudOpacity = Math.Min(1.0f, cloudCover / Data.cloudContentFullAbsorption);
				float heatFromSun = GetHeatFromSun(state.SeaLevel, elevation, surfaceIce, cloudOpacity, terrainNormal, humidity, atmosphereMass, sunAngle, sunVector);

				UpdateTemperature(heatFromSun, cloudOpacity, temperature, airPressureInverse, humidity, atmosphereMass, ref newTemperature);
				MoveOceanOnCurrent(state, x, y, elevation, heatFromSun, temperature, surfaceIce, oceanTemperatureShallow, oceanTemperatureDeep, oceanSalinityShallow, oceanSalinityDeep, oceanDensity, currentShallow, currentDeep, ref newOceanTemperatureShallow, ref newOceanTemperatureDeep, ref newOceanSalinityShallow, ref newOceanSalinityDeep, ref newTemperature);
				MoveAtmosphereOnWind(state, x, y, temperature, humidity, wind, ref newHumidity, ref newTemperature);
				SimulateIce(elevation, state.SeaLevel, tempWithSunAtGround, ref newSurfaceWater, ref newSurfaceIce);
				FlowWater(state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
				SeepWaterIntoGround(elevation, state.SeaLevel, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);
				EvaporateWater(evapRate, elevation, state.SeaLevel, groundWater, waterTableDepth, ref newHumidity, ref newTemperature, ref newGroundWater, ref newSurfaceWater, out newEvaporation);
				//			MoveHumidityToClouds(elevation, humidity, tempWithSunAtGround, cloudElevation, windAtSurface, ref newHumidity, ref newCloudCover);
				if (cloudCover > 0)
				{
					UpdateCloudElevation(elevationOrSeaLevel, temperature, humidity, atmosphereMass, wind, ref newCloudElevation);
					MoveClouds(state, x, y, wind, cloudCover, ref newCloudCover);
					rainfall = UpdateRainfall(state, elevation, cloudCover, temperature, cloudElevation, ref newSurfaceWater, ref newCloudCover);
				}
				UpdatePressure(state, y, x, index, elevationOrSeaLevel, pressure, temperature, newTemperature, wind, out newPressure);

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
				nextState.OceanDensityDeep[index] = GetOceanDensity(newOceanTemperatureDeep, newOceanSalinityDeep, state.SeaLevel - elevation);

			}
		}

	}

	private float GetOceanDensity(float oceanTemperature, float oceanSalinity, float volume)
	{
		if (volume <= 0)
		{
			return 0;
		}
		float OceanSalinityDensity = 1.0f;
		float OceanTemperatureDensity = 10.0f;
		return OceanSalinityDensity * (oceanSalinity / volume) + OceanTemperatureDensity * (Data.FreezingTemperature / oceanTemperature);
	}

	private void UpdatePressure(State state, int y, int x, int index, float elevationOrSeaLevel, float pressure, float temperature, float newTemperature, Vector3 wind, out float newPressure)
	{
		float temperatureToPressure = 1000;
		newPressure = Data.StaticPressure;

		newPressure -= wind.z * Data.verticalWindPressureAdjustment;
		newPressure -= (temperature - (Data.StdTemp - Data.StdTempLapseRate)) * temperatureToPressure;
	}

	public float GetLocalTemperature(float sunAngle, float cloudCover, float temperature)
	{
		return temperature + (1.0f - Math.Min(cloudCover / Data.cloudContentFullAbsorption, 1.0f)) * sunAngle * Data.localSunHeat;
	}

	public float GetPressureAtElevation(State state, int index, float elevation)
	{
		// Units: Pascals
		// Barometric Formula
		// Pressure = StaticPressure * (StdTemp / (StdTemp + StdTempLapseRate * (Elevation - ElevationAtBottomOfAtmLayer)) ^ (GravitationalAcceleration * MolarMassOfEarthAir / (UniversalGasConstant * StdTempLapseRate))
		// https://en.wikipedia.org/wiki/Barometric_formula
		// For the bottom layer of atmosphere ( < 11000 meters), ElevationAtBottomOfAtmLayer == 0)

	//	float standardPressure = Data.StaticPressure * (float)Math.Pow(Data.StdTemp / (Data.StdTemp + Data.StdTempLapseRate * elevation), Data.PressureExponent);
		float pressure = Data.StaticPressure * (float)Math.Pow(Data.StdTemp / (Data.StdTemp + Data.StdTempLapseRate * elevation), Data.PressureExponent);
		return pressure;
	}


	public Vector3 GetSunVector(int ticks, float latitude)
	{

		float angleOfInclination = Data.planetTiltAngle * (float)Math.Sin(Math.PI * 2 * (GetTimeOfYear(ticks) - 0.25f));
		//float timeOfDay = (-sunPhase + 0.5f) * Math.PI * 2;
		float timeOfDay = (float)0;
		float azimuth = (float)Math.Atan2(Math.Sin(timeOfDay), Math.Cos(timeOfDay) * Math.Sin(latitude * Math.PI) - Math.Tan(angleOfInclination) * Math.Cos(latitude * Math.PI));
		float elevation = (float)Math.Asin((Math.Sin(latitude) * Math.Sin(angleOfInclination) + Math.Cos(latitude) * Math.Cos(angleOfInclination) * Math.Cos(timeOfDay)));

		float cosOfElevation = (float)Math.Cos(elevation);
		Vector3 sunVec = new Vector3((float)Math.Sin(azimuth) * cosOfElevation, (float)Math.Cos(azimuth) * cosOfElevation, (float)Math.Sin(elevation));
		return sunVec;
	}



	private void MoveHumidityToClouds(float elevationOrSeaLevel, float humidity, float localTemperature, float cloudElevation, Vector3 windAtSurface, ref float newHumidity, ref float newCloudCover)
	{
		float humidityToCloud = Mathf.Clamp(windAtSurface.z / cloudElevation + Math.Max(0, 1.0f - GetRelativeHumidity(localTemperature, humidity, cloudElevation, elevationOrSeaLevel)), 0, humidity);
		newHumidity -= humidityToCloud;
		newCloudCover += humidityToCloud;
	}

	private void UpdateCloudElevation(float elevationOrSeaLevel, float temperature, float humidity, float atmosphereMass, Vector3 windAtCloudElevation, ref float newCloudElevation)
	{
		float dewPointTemp = (float)Math.Pow(humidity / (Data.dewPointRange * atmosphereMass), 0.25f) * Data.dewPointTemperatureRange + Data.dewPointZero;
		float dewPointElevation = Math.Max(0, (dewPointTemp - temperature) / Data.temperatureLapseRate) + elevationOrSeaLevel;

		float desiredDeltaZ = dewPointElevation - newCloudElevation;
		newCloudElevation = elevationOrSeaLevel + 1000;
//		newCloudElevation = newCloudElevation + desiredDeltaZ* Data.cloudElevationDeltaSpeed + windAtCloudElevation.z * Data.windVerticalCloudSpeedMultiplier;
	//	newCloudElevation = Mathf.Clamp(newCloudElevation, elevationOrSeaLevel+1, Data.stratosphereElevation);
	}

	private void MoveClouds(State state, int x, int y, Vector3 windAtCloudElevation, float cloudCover, ref float newCloudCover)
	{
		if (cloudCover > 0)
		{
			if (windAtCloudElevation.x != 0 || windAtCloudElevation.y != 0)
			{
				float cloudMove = Math.Min(cloudCover, (Math.Abs(windAtCloudElevation.x) + Math.Abs(windAtCloudElevation.y)) * Data.cloudMovementFromWind);
				newCloudCover -= cloudMove;

				for (int i = 0; i < 4; i++)
				{
					var neighborPoint = GetNeighbor(x, y, i);
					int neighborIndex = GetIndex(neighborPoint.x, neighborPoint.y);
					float nCloudCover = state.CloudCover[neighborIndex];
					if (nCloudCover > 0)
					{
						var nWindAtCloudElevation = state.Wind[neighborIndex];
						switch (i)
						{
							case 0:
								if (nWindAtCloudElevation.x > 0)
								{
									float nCloudMove = Math.Min(nCloudCover, (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y)) * Data.cloudMovementFromWind);
									newCloudCover += nCloudMove * nWindAtCloudElevation.x / (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y));
								}
								break;
							case 1:
								if (nWindAtCloudElevation.x < 0)
								{
									float nCloudMove = Math.Min(nCloudCover, (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y)) * Data.cloudMovementFromWind);
									newCloudCover += nCloudMove * -nWindAtCloudElevation.x / (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y));
								}
								break;
							case 2:
								if (nWindAtCloudElevation.y < 0)
								{
									float nCloudMove = Math.Min(nCloudCover, (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y)) * Data.cloudMovementFromWind);
									newCloudCover += nCloudMove * -nWindAtCloudElevation.y / (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y));
								}
								break;
							case 3:
								if (nWindAtCloudElevation.y > 0)
								{
									float nCloudMove = Math.Min(nCloudCover, (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y)) * Data.cloudMovementFromWind);
									newCloudCover += nCloudMove * nWindAtCloudElevation.y / (Math.Abs(nWindAtCloudElevation.x) + Math.Abs(nWindAtCloudElevation.y));
								}
								break;
						}
					}
				}
			}


		}
	}

	private float UpdateRainfall(State state, float elevation, float cloudCover, float temperature, float cloudElevation, ref float newSurfaceWater, ref float newCloudCover)
	{
		float temperatureAtCloudElevation = cloudElevation * Data.temperatureLapseRate + temperature;
		float rainPoint = Math.Max(0, (temperatureAtCloudElevation - Data.dewPointZero) * Data.rainPointTemperatureMultiplier);
		if (cloudCover > rainPoint)
		{
			float rainfall = (cloudCover - rainPoint) * Data.RainfallRate;
			newCloudCover -= rainfall;
			if (elevation > state.SeaLevel)
			{
				newSurfaceWater += rainfall;
			}
			return rainfall;
		}
		return 0;
	}

	private void FlowWater(State state, int x, int y, Vector2 gradient, float soilFertility, ref float surfaceWater, ref float groundWater)
	{
		float flow = Math.Min(surfaceWater, (Math.Abs(gradient.x) + Math.Abs(gradient.y)));
		surfaceWater = Math.Max(surfaceWater - flow * Data.FlowSpeed, 0);
		groundWater = Math.Max(groundWater - Data.GroundWaterFlowSpeed * soilFertility, 0);


		for (int i = 0; i < 4; i++)
		{
			var neighborPoint = GetNeighbor(x, y, i);
			int neighborIndex = GetIndex(neighborPoint.x, neighborPoint.y);
			float nWater = state.SurfaceWater[neighborIndex];
			float nGroundWater = state.GroundWater[neighborIndex];
			if (nWater > 0)
			{
				var nGradient = state.FlowDirection[neighborIndex];
				var nGroundFlow = Data.GroundWaterFlowSpeed * state.SoilFertility[neighborIndex];
				switch (i)
				{
					case 0:
						if (nGradient.x > 0)
						{
							surfaceWater += nGradient.x * nWater * Data.FlowSpeed;
							groundWater += nGroundWater * nGroundFlow;
						}
						break;
					case 1:
						if (nGradient.x < 0)
						{
							surfaceWater += nGradient.x * nWater * Data.FlowSpeed;
						}
						break;
					case 2:
						if (nGradient.y < 0)
						{
							surfaceWater += nGradient.x * nWater * Data.FlowSpeed;
						}
						break;
					case 3:
						if (nGradient.y > 0)
						{
							surfaceWater += nGradient.x * nWater * Data.FlowSpeed;
						}
						break;
				}
			}
		}
			
			
	}

	private void SimulateIce(float elevation, float seaLevel, float localTemperature, ref float surfaceWater, ref float surfaceIce)
	{
		if (localTemperature <= Data.FreezingTemperature)
		{
			float frozen = Data.iceFreezeRate * (Data.FreezingTemperature - localTemperature) * (1.0f - (float)Math.Pow(Math.Min(1.0f, surfaceIce / Data.maxIce), 2));
			if (elevation > seaLevel)
			{
				frozen = Math.Min(frozen, surfaceWater);
				surfaceWater -= frozen;
			}
			surfaceIce += frozen;
		} else if (surfaceIce > 0)
		{
			float meltRate = (localTemperature - Data.FreezingTemperature) * Data.iceMeltRate;
			float melted = Math.Min(surfaceIce, meltRate);
			surfaceIce -= melted;
			if (elevation > seaLevel)
			{
				surfaceWater += melted;
			}
		}
	}
	private void SeepWaterIntoGround(float elevation, float seaLevel, float soilFertility, float waterTableDepth, ref float groundWater, ref float surfaceWater)
	{
		float maxGroundWater = soilFertility * waterTableDepth * Data.MaxSoilPorousness;
		if (elevation > seaLevel)
		{
			float seepage = Math.Min(surfaceWater * soilFertility * Data.GroundWaterReplenishmentSpeed, maxGroundWater - groundWater);
			groundWater += seepage;
			surfaceWater -= seepage;
		}
		else
		{
			groundWater = maxGroundWater;
			surfaceWater = 0;
		}
	}

	private float GetEvaporationRate(float ice, float localTemperature, float humidity, float windSpeedAtSurface, float cloudElevation, float elevationOrSeaLevel)
	{
		if (ice > 0)
		{
			return 0;
		}
		float evapTemperature = 1.0f - Mathf.Clamp((localTemperature - Data.evapMinTemperature) / Data.evapTemperatureRange, 0, 1);
		float evapRate = Data.EvapRateTemperature * (1.0f - evapTemperature * evapTemperature);
		evapRate += Data.EvapRateWind * windSpeedAtSurface;

		float relativeHumidity = GetRelativeHumidity(localTemperature, humidity, cloudElevation, elevationOrSeaLevel);

		evapRate *= Math.Max(0.0f, 1.0f - relativeHumidity);
		return evapRate;
	}

	public float GetRelativeHumidity(float localTemperature, float humidity, float cloudElevation, float elevationOrSeaLevel)
	{
		float atmosphereMass = (cloudElevation - elevationOrSeaLevel) * Data.MolarMassEarthAir;
		float maxHumidity = atmosphereMass * Data.dewPointRange * Mathf.Clamp((localTemperature - Data.dewPointZero) / Data.dewPointTemperatureRange, 0, 1);
		float relativeHumidity = humidity / maxHumidity;
		return relativeHumidity;
	}

	private void EvaporateWater(float evapRate, float elevation, float seaLevel, float groundWater, float waterTableDepth, ref float humidity, ref float temperature, ref float newGroundWater, ref float surfaceWater, out float evaporation)
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

	private void MoveAtmosphereOnWind(State state, int x, int y, float temperature, float humidity, Vector3 windAtSurface, ref float newHumidity, ref float newTemperature)
	{
		float temperatureDispersalSpeed = 0.01f;
		float humidityDispersalSpeed = 0.01f;

		// in high pressure systems, air from the upper atmosphere will cool us
		if (windAtSurface.z < 0)
		{
			newTemperature += Data.upperAtmosphereCoolingRate * windAtSurface.z;
		}

		if (windAtSurface.x != 0 || windAtSurface.y != 0)
		{
			newHumidity = Math.Max(0, newHumidity - humidity * Math.Min(1.0f, (Math.Abs(windAtSurface.x) + Math.Abs(windAtSurface.y)) * Data.humidityLossFromWind));
		}
		for (int i = 0; i < 4; i++)
		{
			var neighbor = GetNeighbor(x, y, i);
			int nIndex = GetIndex(neighbor.x, neighbor.y);
			float nTemperature = state.Temperature[nIndex];
			float nHumidity = state.Humidity[nIndex];
			newTemperature += (nTemperature - temperature) * temperatureDispersalSpeed;
			newHumidity += (nHumidity - humidity) * humidityDispersalSpeed;
			switch (i)
			{
				case 0:
					if (windAtSurface.x > 0)
					{
						newTemperature += (nTemperature - temperature) * Math.Min(1.0f, windAtSurface.x * Data.temperatureEqualizationFromWind);
						newHumidity += nHumidity * Math.Min(1.0f, windAtSurface.x * Data.humidityLossFromWind);
					}
					break;
				case 1:
					if (windAtSurface.x < 0)
					{
						newTemperature += (nTemperature - temperature) * Math.Max(-1.0f, windAtSurface.x * Data.temperatureEqualizationFromWind);
						newHumidity += nHumidity * Math.Min(1.0f, -windAtSurface.y * Data.humidityLossFromWind);
					}
					break;
				case 2:
					if (windAtSurface.y < 0)
					{
						newTemperature += (nTemperature - temperature) * Math.Max(-1.0f, windAtSurface.y * Data.temperatureEqualizationFromWind);
						newHumidity += nHumidity * Math.Min(1.0f, -windAtSurface.y * Data.humidityLossFromWind);
					}
					break;
				case 3:
					if (windAtSurface.y > 0)
					{
						newTemperature += (nTemperature - temperature) * Math.Min(1.0f, windAtSurface.y * Data.temperatureEqualizationFromWind);
						newHumidity += nHumidity * Math.Min(1.0f, windAtSurface.y * Data.humidityLossFromWind);
					}
					break;
			}				
		}
	}


	private void MoveOceanOnCurrent(
		State state, 
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

		newOceanTemperatureShallow += heatFromSun * Data.heatAbsorptionWater;
		float heatExchangeAir = ice > 0 ? 0 : (oceanTemperatureShallow - temperature) * Data.heatExchangeAirSpeed;
		newOceanTemperatureShallow -= heatExchangeAir;
		newTemperature += heatExchangeAir;


		//		if (depth > surfaceTemperatureDepth)
		{
			//			float surfacePercent = surfaceTemperatureDepth / depth;
			//			float surfacePercent = 0.1f;
			float surfaceDensity = GetOceanDensity(oceanTemperatureShallow, oceanSalinityShallow, Data.DeepOceanDepth);


			if (oceanTemperatureShallow <= Data.FreezingTemperature+5)
			{
				float salinityExchange = oceanSalinityShallow * Data.oceanSalinityIncrease;
				newOceanSalinityDeep += salinityExchange;
				newOceanSalinityShallow -= salinityExchange;
				newOceanTemperatureDeep = Data.FreezingTemperature;
			}
			else
			{
				float salinityExchange = (oceanSalinityDeep/depth - oceanSalinityShallow/Data.DeepOceanDepth) * Data.salinityMixingSpeed * (oceanSalinityShallow+oceanSalinityDeep);
				newOceanSalinityDeep -= salinityExchange * depth / (depth + Data.DeepOceanDepth);
				newOceanSalinityShallow += salinityExchange * Data.DeepOceanDepth / (depth + Data.DeepOceanDepth);

				float heatExchange = (oceanTemperatureDeep - oceanTemperatureShallow) * Data.temperatureMixingSpeed / (depth + Data.DeepOceanDepth);
				newOceanTemperatureShallow += heatExchange * depth;
				newOceanTemperatureDeep -= heatExchange * Data.DeepOceanDepth;
			}

			if (currentShallow.z < 0)
			{
				float downwelling = -currentShallow.z * Data.downwellingSpeed;
				newOceanTemperatureDeep += (oceanTemperatureShallow - oceanTemperatureDeep) * Math.Min(0.5f, downwelling / depth);
				float salinityExchange = Math.Min(0.5f, downwelling * oceanSalinityShallow / Data.DeepOceanDepth);
				newOceanSalinityDeep += salinityExchange;
				newOceanSalinityShallow -= salinityExchange;
			}
			else if (currentShallow.z > 0)
			{
				float upwelling = currentShallow.z * Data.upwellingSpeed;
				newOceanTemperatureShallow += (oceanTemperatureDeep - oceanTemperatureShallow) * Math.Min(0.5f, upwelling / Data.DeepOceanDepth);
				float salinityExchange = Math.Min(0.5f, upwelling * oceanSalinityDeep / depth);
				newOceanSalinityDeep -= salinityExchange;
				newOceanSalinityShallow += salinityExchange;
			}
		}

		for (int i = 0; i < 4; i++)
		{
			var neighbor = GetNeighbor(x, y, i);
			int nIndex = GetIndex(neighbor.x, neighbor.y);
			float neighborDepth = state.SeaLevel - state.Elevation[nIndex];
			if (neighborDepth > 0)
			{
				var neighborCurrentDeep = state.OceanCurrentDeep[nIndex];
				var neighborCurrentShallow = state.OceanCurrentShallow[nIndex];

				float nTemperatureShallow = state.OceanTemperatureShallow[nIndex];
				float nTemperatureDeep = state.OceanTemperatureDeep[nIndex];
				float nSalinityShallow = state.OceanSalinityShallow[nIndex];
				float nSalinityDeep = state.OceanSalinityDeep[nIndex];

				newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Data.horizontalMixing;
				newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Data.horizontalMixing;
				float nSalinityDeepPercentage = nSalinityDeep / neighborDepth;
				newOceanSalinityDeep += (nSalinityDeepPercentage - salinityDeepPercentage) * Data.horizontalMixing * Math.Min(neighborDepth, depth);

				newOceanSalinityShallow += (nSalinityShallow - oceanSalinityShallow) * Data.horizontalMixing;

				switch (i)
				{
					case 0:
						if (neighborCurrentShallow.x > 0)
						{
							float absX = Math.Abs(neighborCurrentShallow.x);
							newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Math.Min(0.25f, absX * Data.oceanTemperatureMovement);
							newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absX * Data.oceanSalinityMovement);
						}
						if (currentShallow.x < 0)
						{
							float absX = Math.Abs(currentShallow.x);
							newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absX * Data.oceanSalinityMovement);
						}
						if (neighborCurrentDeep.x > 0)
						{
							float absX = Math.Abs(neighborCurrentDeep.x);
							newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Math.Min(0.25f, absX * Data.oceanTemperatureMovement);
							newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absX * Data.oceanSalinityMovement);
						}
						if (currentDeep.x < 0)
						{
							float absX = Math.Abs(currentDeep.x);
							newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absX * Data.oceanSalinityMovement);
						}
						break;
					case 1:
						if (neighborCurrentShallow.x < 0)
						{
							float absX = Math.Abs(neighborCurrentShallow.x);
							newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Math.Min(0.25f, absX * Data.oceanTemperatureMovement);
							newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absX * Data.oceanSalinityMovement);
						}
						if (currentShallow.x > 0)
						{
							float absX = Math.Abs(currentShallow.x);
							newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absX * Data.oceanSalinityMovement);
						}
						if (neighborCurrentDeep.x < 0)
						{
							float absX = Math.Abs(neighborCurrentDeep.x);
							newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Math.Min(0.25f, absX * Data.oceanTemperatureMovement);
							newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absX * Data.oceanSalinityMovement);
						}
						if (currentDeep.x > 0)
						{
							float absX = Math.Abs(currentDeep.x);
							newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absX * Data.oceanSalinityMovement);
						}
						break;
					case 2:
						if (neighborCurrentShallow.y < 0)
						{
							float absY = Math.Abs(neighborCurrentShallow.y);
							newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Math.Min(0.25f, absY * Data.oceanTemperatureMovement);
							newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absY * Data.oceanSalinityMovement);
						}
						if (currentShallow.y > 0)
						{
							float absY = Math.Abs(currentShallow.y);
							newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absY * Data.oceanSalinityMovement);
						}
						if (neighborCurrentDeep.y < 0)
						{
							float absY = Math.Abs(neighborCurrentDeep.y);
							newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Math.Min(0.25f, absY * Data.oceanTemperatureMovement);
							newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absY * Data.oceanSalinityMovement);
						}
						if (currentDeep.y > 0)
						{
							float absY = Math.Abs(currentDeep.y);
							newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absY * Data.oceanSalinityMovement);
						}
						break;
					case 3:
						if (neighborCurrentShallow.y > 0)
						{
							float absY = Math.Abs(neighborCurrentShallow.y);
							newOceanTemperatureShallow += (nTemperatureShallow - oceanTemperatureShallow) * Math.Min(0.25f, absY * Data.oceanTemperatureMovement);
							newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absY * Data.oceanSalinityMovement);
						}
						if (currentShallow.y < 0)
						{
							float absY = Math.Abs(currentShallow.y);
							newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absY * Data.oceanSalinityMovement);
						}
						if (neighborCurrentDeep.y > 0)
						{
							float absY = Math.Abs(neighborCurrentDeep.y);
							newOceanTemperatureDeep += (nTemperatureDeep - oceanTemperatureDeep) * Math.Min(0.25f, absY * Data.oceanTemperatureMovement);
							newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absY * Data.oceanSalinityMovement);
						}
						if (currentDeep.y < 0)
						{
							float absY = Math.Abs(currentDeep.y);
							newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absY * Data.oceanSalinityMovement);
						}
						break;
				}
			}
		}
		newOceanSalinityDeep = Math.Max(0, newOceanSalinityDeep);
		newOceanSalinityShallow = Math.Max(0, newOceanSalinityShallow);
	}

	private float GetHeatFromSun(float seaLevel, float elevation, float ice, float cloudOpacity, Vector3 terrainNormal, float humidity, float atmosphereMass, float sunAngle, Vector3 sunVector)
	{
		// TEMPERATURE
		float cloudAbsorptionFactor = Data.cloudAbsorptionRate * cloudOpacity;
		float cloudReflectionFactor = Data.cloudReflectionRate * cloudOpacity;
		float humidityPercentage = humidity / atmosphereMass;


		float gain = 0;
		float cloudGain = 0;
		float cloudReflection = 0;
		float reflection = 0;
		if (sunAngle > 0)
		{
			cloudGain = sunAngle * Data.heatGainFromSun * cloudAbsorptionFactor;
			cloudReflection = sunAngle * Data.heatGainFromSun * cloudReflectionFactor;

			// gain any heat not absorbed on first pass through the clouds
			float slope = 1;
			if (ice > 0)
			{
				reflection = Data.heatReflectionIce;
			}
			else if (elevation <= seaLevel) // ocean
			{
				reflection = Data.heatReflectionWater + Data.heatAbsorptionWater;
			}
			else // land
			{
				slope = Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
				// reflection = mineralTypes[cells[i, j].mineral].heatReflection;
				reflection = Data.HeatReflectionLand;
			}
			float sunGain = slope * Data.heatGainFromSun - cloudGain - cloudReflection;
			gain += sunGain * (1.0f - reflection) * (1.0f - humidityPercentage);
		}
		return gain;
	}
	private void UpdateTemperature(float heatFromSun, float cloudOpacity, float temperature, float airPressureInverse, float humidity, float atmosphereMass, ref float newTemperature)
	{
		// TEMPERATURE
		float cloudReflectionFactor = Data.cloudReflectionRate * cloudOpacity;
		float humidityPercentage = humidity / atmosphereMass;

		float heatLossFactor = (1.0f - Data.carbonDioxide * Data.heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
		float loss = temperature * (1.0f - cloudReflectionFactor) * (Data.heatLoss * heatLossFactor * airPressureInverse);

		newTemperature += heatFromSun - loss;

	}

	private float GetAtmosphereMass(float elevation, float elevationOrSeaLevel)
	{
		float atmosphereMass;
		if (elevation <= Data.troposphereElevation)
		{
			atmosphereMass = (Data.troposphereElevation - elevationOrSeaLevel) / Data.troposphereAtmosphereContent;
		}
		else
		{
			atmosphereMass = Data.troposphereElevation + (Data.stratosphereElevation - elevationOrSeaLevel) * (1.0f - Data.troposphereAtmosphereContent) * Data.troposphereElevation;
		}

		return atmosphereMass;
	}

	private void UpdateFlowDirectionAndNormal(State state, State nextState, int x, int y, int index, float elevation, out Vector2 flowDirection, out Vector3 normal)
	{
		if (elevation <= state.SeaLevel)
		{
			flowDirection = Vector2.zero;
			normal = new Vector3(0, 0, 1);
		}
		else
		{
			int indexW = GetIndex(WrapX(x - 1), y);
			int indexE = GetIndex(WrapX(x + 1), y);
			int indexN = GetIndex(x, WrapY(y - 1));
			int indexS = GetIndex(x, WrapY(y + 1));
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

			flowDirection = new Vector2(Math.Sign(g.x) * (1.0f +(float)Math.Pow(Math.Abs(g.x) / Data.tileSize, Data.FlowSpeedExponent)), Math.Sign(g.y) * (1.0f + (float)Math.Pow(Math.Abs(g.x) / Data.tileSize, Data.FlowSpeedExponent)));

			// TODO: this is wong, gradient is just steepest downhill direction
			normal = Vector3.Normalize(new Vector3(g.x, g.y, Data.tileSize));

		}
	}


}
