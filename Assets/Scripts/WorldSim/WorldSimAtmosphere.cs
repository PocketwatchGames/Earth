using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public partial class World
{

	public void TickBarometricPressure(State state, State nextState)
	{
		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				int index = GetIndex(x, y);
				float elevation = state.Elevation[index];
				float temperature = state.Temperature[index];

				nextState.Pressure[index] = UpdateBarometricPressure(state, x, y, index, elevation, temperature);
			}
		}
	}

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

				float newEvaporation;
				float newGroundWater = groundWater;
				float newHumidity = humidity;
				float newCloudCover = cloudCover;
				float newSurfaceWater = surfaceWater;
				float newSurfaceIce = surfaceIce;
				float newTemperature = temperature;
				float newCloudElevation = cloudElevation;
				float rainfall = 0;
				float newRadiation = radiation;


				float atmosphereMass = GetAtmosphereMass(elevation, elevationOrSeaLevel);
				Vector3 windAtSurface = state.WindSurface[index];
				float windSpeedAtSurface = windAtSurface.magnitude;
				float airPressureInverse = Data.StaticPressure / pressure;
				float tempWithSunAtGround = GetLocalTemperature(sunAngle, cloudCover, temperature);
				float evapRate = GetEvaporationRate(surfaceIce, tempWithSunAtGround, humidity, windSpeedAtSurface, cloudElevation, elevationOrSeaLevel);

				UpdateTemperature(state.SeaLevel, elevation, surfaceIce, cloudCover, temperature, terrainNormal, humidity, atmosphereMass, sunAngle, sunVector, airPressureInverse, ref newTemperature);
				MoveAtmosphereOnWind(state, x, y, temperature, humidity, windAtSurface, ref newHumidity, ref newTemperature);
				SimulateIce(elevation, state.SeaLevel, tempWithSunAtGround, ref newSurfaceWater, ref newSurfaceIce);
				FlowWater(state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
				SeepWaterIntoGround(elevation, state.SeaLevel, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);
				EvaporateWater(evapRate, elevation, state.SeaLevel, groundWater, waterTableDepth, ref newHumidity, ref newTemperature, ref newGroundWater, ref newSurfaceWater, out newEvaporation);
	//			MoveHumidityToClouds(elevation, humidity, tempWithSunAtGround, cloudElevation, windAtSurface, ref newHumidity, ref newCloudCover);
				if (cloudCover > 0)
				{
					var windAtCloudElevation = state.WindCloud[index];
					UpdateCloudElevation(elevationOrSeaLevel, temperature, humidity, atmosphereMass, windAtCloudElevation, ref newCloudElevation);
					MoveClouds(state, x, y, windAtCloudElevation, cloudCover, ref newCloudCover);
					rainfall = UpdateRainfall(state, elevation, cloudCover, temperature, cloudElevation, ref newSurfaceWater, ref newCloudCover);
				}

				if (float.IsNaN(newTemperature) || float.IsNaN(newEvaporation) || float.IsNaN(newSurfaceWater) || float.IsNaN(newSurfaceIce) || float.IsNaN(newGroundWater) || float.IsNaN(newHumidity) || float.IsNaN(newCloudCover) || float.IsNaN(newCloudElevation))
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
			}
		}

	}

	public float GetLocalTemperature(float sunAngle, float cloudCover, float temperature)
	{
		return temperature + (1.0f - Math.Min(cloudCover / Data.cloudContentFullAbsorption, 1.0f)) * sunAngle * Data.localSunHeat;
	}

	public float GetPressureAtElevation(State state, int index, float elevation, float temperatureDifferential)
	{
		// Units: Pascals
		// Barometric Formula
		// Pressure = StaticPressure * (StdTemp / (StdTemp + StdTempLapseRate * (Elevation - ElevationAtBottomOfAtmLayer)) ^ (GravitationalAcceleration * MolarMassOfEarthAir / (UniversalGasConstant * StdTempLapseRate))
		// https://en.wikipedia.org/wiki/Barometric_formula
		// For the bottom layer of atmosphere ( < 11000 meters), ElevationAtBottomOfAtmLayer == 0)

		float standardPressure = Data.StaticPressure * (float)Math.Pow(Data.StdTemp / (Data.StdTemp + Data.StdTempLapseRate * elevation), Data.PressureExponent);

		float pressure = standardPressure - Data.verticalWindPressureAdjustment * state.Wind[index].z + Data.temperatureGradientPressureAdjustment * temperatureDifferential;
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
		newCloudElevation = newCloudElevation + desiredDeltaZ* Data.cloudElevationDeltaSpeed + windAtCloudElevation.z * Data.windVerticalCloudSpeedMultiplier;
		newCloudElevation = Math.Max(newCloudElevation, elevationOrSeaLevel);
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
						var nWindAtCloudElevation = state.WindCloud[neighborIndex];
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

		temperature -= evaporation * Data.EvaporativeCoolingRate;
	}

	private void MoveAtmosphereOnWind(State state, int x, int y, float temperature, float humidity, Vector3 windAtSurface, ref float newHumidity, ref float newTemperature)
	{
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
			var neighborWind = state.Wind[nIndex];
			switch (i)
			{
				case 0:
					if (neighborWind.x > 0)
					{
						float absX = Math.Abs(neighborWind.x);
						newTemperature += (state.Temperature[nIndex] - temperature) * Math.Min(1.0f, absX * Data.temperatureLossFromWind);
						newHumidity += state.Humidity[nIndex] * absX * Data.humidityLossFromWind;
					}
					break;
				case 1:
					if (neighborWind.x < 0)
					{
						float absX = Math.Abs(neighborWind.x);
						newTemperature += (state.Temperature[nIndex] - temperature) * Math.Min(1.0f, absX * Data.temperatureLossFromWind);
						newHumidity += state.Humidity[nIndex] * absX * Data.humidityLossFromWind;
					}
					break;
				case 2:
					if (neighborWind.y < 0)
					{
						float absY = Math.Abs(neighborWind.y);
						newTemperature += (state.Temperature[nIndex] - temperature) * Math.Min(1.0f, absY * Data.temperatureLossFromWind);
						newHumidity += state.Humidity[nIndex] * absY * Data.humidityLossFromWind;
					}
					break;
				case 3:
					if (neighborWind.y > 0)
					{
						float absY = Math.Abs(neighborWind.y);
						newTemperature += (state.Temperature[nIndex] - temperature) * Math.Min(1.0f, absY * Data.temperatureLossFromWind);
						newHumidity += state.Humidity[nIndex] * absY * Data.humidityLossFromWind;
					}
					break;
			}				
		}
	}

	private void UpdateTemperature(float seaLevel, float elevation, float ice, float cloudCover, float temperature, Vector3 terrainNormal, float humidity, float atmosphereMass, float sunAngle, Vector3 sunVector, float airPressureInverse, ref float newTemperature)
	{

		// TEMPERATURE
		float cloudMass = Math.Min(1.0f, cloudCover / Data.cloudContentFullAbsorption);
		float cloudAbsorptionFactor = Data.cloudAbsorptionRate * cloudMass;
		float cloudReflectionFactor = Data.cloudReflectionRate * cloudMass;
		float humidityPercentage = humidity / atmosphereMass;


		float heatLossFactor = (1.0f - Data.carbonDioxide * Data.heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
		float loss = temperature * (1.0f - cloudReflectionFactor) * (Data.heatLoss * heatLossFactor * airPressureInverse);
		//		float cloudLoss = 0;
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
				reflection = Data.heatReflectionWater;
			}
			else // land
			{
				slope = Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
				// reflection = mineralTypes[cells[i, j].mineral].heatReflection;
				reflection = Data.HeatReflectionLand;
			}
			float sunGain = slope * Data.heatGainFromSun - cloudGain - cloudReflection;
			gain += sunGain * (1.0f - reflection) * (1.0f - humidityPercentage);

			// trap some heat in
			gain += cloudMass * cloudReflectionFactor * loss;
		}

		newTemperature += gain - loss;
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

	private float UpdateBarometricPressure(State state, int x, int y, int index, float elevation, float temperature)
	{
		float temperatureDifferential = 0;
		temperatureDifferential += state.Temperature[GetIndex(WrapX(x + 1), y)] - temperature;
		temperatureDifferential += state.Temperature[GetIndex(WrapX(x - 1), y)] - temperature;
		temperatureDifferential += state.Temperature[GetIndex(x, WrapY(y + 1))] - temperature;
		temperatureDifferential += state.Temperature[GetIndex(x, WrapY(y - 1))] - temperature;
		temperatureDifferential /= 4;
		return GetPressureAtElevation(state, index, Math.Max(state.SeaLevel, elevation), temperatureDifferential);
	}

}
