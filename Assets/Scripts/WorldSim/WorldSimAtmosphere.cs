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
			float declinationOfSun = GetDeclinationOfSun(state.PlanetTiltAngle, timeOfYear);
			float globalEnergyLost = 0;
			float globalEnergyGained = 0;
			float globalEnergy = 0;
			float globalEnergyIncoming = 0;
			float globalEnergyReflectedAtmosphere = 0;
			float globalEnergyReflectedSurface = 0;
			float globalEnergyAbsorbedUpperAtmosphere = 0;
			float globalEnergyAbsorbedLowerAtmosphere = 0;
			float globalEnergyAbsorbedSurface = 0;
			float globalOceanCoverage = 0;
			float globalTemperature = 0;
			float atmosphericMass = 0;

			for (int y = 0; y < world.Size; y++)
			{
				float latitude = world.GetLatitude(y);
				var sunVector = GetSunVector(world, state.PlanetTiltAngle, state.Ticks, latitude);
				float sunAngle = Math.Max(0, sunVector.z);

				float lengthOfDay = GetLengthOfDay(latitude, timeOfYear, declinationOfSun);

				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);

					float elevation = state.Elevation[index];
					float elevationOrSeaLevel = Math.Max(state.SeaLevel, elevation);
					float cloudCover = state.CloudCover[index];
					float lowerAirTemperature = state.LowerAirTemperature[index];
					float lowerAirEnergy = state.LowerAirEnergy[index];
					float lowerAirPressure = state.LowerAirPressure[index];
					float lowerAirMass = state.LowerAirMass[index];
					float upperAirTemperature = state.UpperAirTemperature[index];
					float upperAirEnergy = state.UpperAirEnergy[index];
					float upperAirPressure = state.UpperAirPressure[index];
					float upperAirMass = state.UpperAirMass[index];
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
					var lowerWind = state.LowerWind[index];
					var upperWind = state.UpperWind[index];
					var currentDeep = state.OceanCurrentDeep[index];
					var currentShallow = state.OceanCurrentShallow[index];
					float oceanTemperatureShallow = state.OceanTemperatureShallow[index];
					float oceanTemperatureDeep = GetWaterTemperature(world, oceanEnergyDeep, Math.Max(0, state.SeaLevel - elevation));


					float evaporation = 0;
					float newGroundWater = groundWater;
					float newHumidity = humidity;
					float newLowerAirEnergy = lowerAirEnergy;
					float newLowerAirMass = lowerAirMass;
					float newUpperAirEnergy = upperAirEnergy;
					float newUpperAirMass = upperAirMass;
					float newCloudCover = cloudCover;
					float newSurfaceWater = surfaceWater;
					float newSurfaceIce = surfaceIce;
					float newCloudElevation = cloudElevation;
					float rainfall = 0;
					float newRadiation = radiation;
					float newOceanEnergyDeep = oceanEnergyDeep;
					float newOceanEnergyShallow = oceanEnergyShallow;
					float newOceanSalinityDeep = oceanSalinityDeep;
					float newOceanSalinityShallow = oceanSalinityShallow;
					float windSpeed = lowerWind.magnitude;
					float massOfAtmosphericColumn = upperAirMass + lowerAirMass;
					float iceCoverage = Mathf.Min(1.0f, surfaceIce / world.Data.FullIceCoverage);

					float relativeHumidity = GetRelativeHumidity(world, lowerAirTemperature, humidity, lowerAirPressure);
					float cloudOpacity = Math.Min(1.0f, cloudCover / world.Data.CloudContentFullAbsorption);
					float groundWaterSaturation = Animals.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], soilFertility * world.Data.MaxSoilPorousness);
					float incomingRadiation = state.SolarRadiation * sunAngle * lengthOfDay / 2;
					float energyAbsorbed = 0;

					if (world.IsOcean(elevation, state.SeaLevel))
					{
						globalOceanCoverage++;
					}
					globalEnergyIncoming += incomingRadiation;

					// reflect some rads off atmosphere and clouds
					float energyReflectedAtmosphere = incomingRadiation * (world.Data.AtmosphericHeatReflection + world.Data.CloudReflectionRate * cloudOpacity);
					incomingRadiation -= energyReflectedAtmosphere;
					globalEnergyReflectedAtmosphere += energyReflectedAtmosphere;

					// Absorbed by atmosphere
					{
						// stratosphere accounts for about a quarter of atmospheric mass
					//	float absorbedByStratosphere = incomingRadiation * world.Data.AtmosphericHeatAbsorption * (state.StratosphereMass / massOfAtmosphericColumn);

						// absorb some rads directly in the atmosphere
						float absorbedByUpperAtmosphereIncoming = incomingRadiation * world.Data.AtmosphericHeatAbsorption * (upperAirMass / massOfAtmosphericColumn);
						newLowerAirEnergy += absorbedByUpperAtmosphereIncoming;
						energyAbsorbed += absorbedByUpperAtmosphereIncoming;

						float absorbedByLowerAtmosphereIncoming = incomingRadiation * world.Data.AtmosphericHeatAbsorption * (lowerAirMass / massOfAtmosphericColumn);
						newLowerAirEnergy += absorbedByLowerAtmosphereIncoming;
						energyAbsorbed += absorbedByLowerAtmosphereIncoming;

					//	incomingRadiation -= absorbedByStratosphere;
						incomingRadiation -= absorbedByUpperAtmosphereIncoming;
						incomingRadiation -= absorbedByLowerAtmosphereIncoming;

						globalEnergyAbsorbedLowerAtmosphere += absorbedByLowerAtmosphereIncoming;
						globalEnergyAbsorbedUpperAtmosphere += absorbedByUpperAtmosphereIncoming;
					}

					// reflection off surface
					float energyReflected = 0;
					{
						if (surfaceIce > 0)
						{
							energyReflected += incomingRadiation * Math.Min(1.0f, surfaceIce) * world.Data.AlbedoIce;
						}
						if (world.IsOcean(elevation, state.SeaLevel))
						{
							energyReflected = incomingRadiation * world.Data.AlbedoWater * Math.Max(0, (1.0f - surfaceIce));
						}
						else
						{
							// reflect some incoming radiation
							var slope = Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
							float waterReflectivity = surfaceWater * world.Data.AlbedoWater;
							float soilReflectivity = world.Data.AlbedoLand - world.Data.AlbedoReductionSoilQuality * soilFertility;
							float heatReflectedLand = canopy * world.Data.AlbedoFoliage + Math.Max(0, 1.0f - canopy) * (surfaceWater * world.Data.AlbedoWater + Math.Max(0, 1.0f - surfaceWater) * soilReflectivity);
							energyReflected += incomingRadiation * heatReflectedLand * Math.Max(0, (1.0f - surfaceIce));
						}
						incomingRadiation -= energyReflected;

						// TODO: do we absorb some of this energy on the way back out of the atmosphere?
	//					newLowerAirEnergy += energyReflected;
						globalEnergyReflectedSurface += energyReflected;
					}

					// ice
					float iceMelted = 0;
					{
						// melt ice at surface from air temp and incoming radiation
						if (surfaceIce > 0)
						{
							float radiationAbsorbedByIce = incomingRadiation * iceCoverage;
							incomingRadiation -= radiationAbsorbedByIce;
							globalEnergyAbsorbedSurface += radiationAbsorbedByIce;

							// world.Data.SpecificHeatIce * world.Data.MassIce == KJ required to raise one cubic meter by 1 degree
							float iceTemp = lowerAirTemperature + radiationAbsorbedByIce / (world.Data.SpecificHeatIce * world.Data.MassIce);
							if (iceTemp > world.Data.FreezingTemperature)
							{
								iceMelted += (iceTemp - world.Data.FreezingTemperature) / (world.Data.SpecificHeatIce * world.Data.MassIce);
							}
						}

						// freeze the top meter based on surface temperature (plus incoming radiation)
						if (surfaceIce < world.Data.FullIceCoverage)
						{
							if (world.IsOcean(elevation, state.SeaLevel) || surfaceWater > 0)
							{
								// world.Data.SpecificHeatIce * world.Data.MassIce == KJ required to raise one cubic meter by 1 degree
								float surfaceTemp = lowerAirTemperature + incomingRadiation / (world.Data.SpecificHeatSeaWater * world.Data.MassSeaWater);
								if (surfaceTemp < world.Data.FreezingTemperature)
								{
									float iceFrozen = Math.Min(world.Data.FullIceCoverage - iceCoverage, (world.Data.FreezingTemperature - surfaceTemp) / (world.Data.SpecificHeatSeaWater * world.Data.MassSeaWater));
									newSurfaceIce += iceFrozen;
									if (!world.IsOcean(elevation, state.SeaLevel))
									{
										// add to surface water
										newSurfaceWater -= iceFrozen;
									}
								}

								// evaporation
								float evapRate = GetEvaporationRate(world, surfaceIce, lowerAirTemperature, relativeHumidity, lowerAirPressure);
								EvaporateWater(world, evapRate, elevation, state.SeaLevel, groundWater, waterTableDepth, ref newHumidity, ref newLowerAirEnergy, ref newOceanEnergyShallow, ref newGroundWater, ref newSurfaceWater, out evaporation);
								MoveHumidityToClouds(world, humidity, relativeHumidity, lowerWind, ref newHumidity, ref newCloudCover);

							}
						}
					}


					// absorbed by surface
					{
						globalEnergyAbsorbedSurface += incomingRadiation;
						// absorb the remainder and radiate heat
						if (world.IsOcean(elevation, state.SeaLevel))
						{
							// absorb remaining incoming radiation (we've already absorbed radiation in surface ice above)
							newOceanEnergyShallow += incomingRadiation;
							energyAbsorbed += incomingRadiation;

							// heat transfer (both ways) based on temperature differential
							// conduction to ice from below
							if (surfaceIce > 0 && oceanTemperatureShallow > world.Data.FreezingTemperature)
							{
								float oceanConduction = (oceanTemperatureShallow - world.Data.FreezingTemperature) * world.Data.OceanIceConduction * iceCoverage;
								newOceanEnergyShallow -= oceanConduction;

								float energyToIce = Math.Max(0, oceanConduction * iceCoverage);
								iceMelted += energyToIce / (world.Data.SpecificHeatIce * world.Data.MassIce);
							}
							// lose heat to air via conduction AND radiation
							if (surfaceIce < world.Data.FullIceCoverage)
							{
								float oceanConduction = (oceanTemperatureShallow - lowerAirTemperature) * world.Data.OceanAirConduction * (1.0f - iceCoverage);
								newLowerAirEnergy += oceanConduction;
								newOceanEnergyShallow -= oceanConduction;

								// radiate heat, will be absorbed by air
								float oceanRadiation = oceanEnergyShallow * world.Data.OceanHeatRadiation * (1.0f - iceCoverage);
								newOceanEnergyShallow -= oceanRadiation;
								newLowerAirEnergy += oceanRadiation;
							}

							if (oceanTemperatureShallow < world.Data.FreezingTemperature)
							{
								float massOfShallowWaterColumn = Math.Max(0, world.Data.DeepOceanDepth - surfaceIce) * world.Data.MassSeaWater;
								float massToAchieveEquilibrium = oceanEnergyShallow / (world.Data.FreezingTemperature * world.Data.SpecificHeatSeaWater);
								float massFrozen = massOfShallowWaterColumn - massToAchieveEquilibrium;
								//newOceanEnergyShallow -= energyToAchieveEquilibrium;
								newSurfaceIce += massToAchieveEquilibrium / world.Data.MassIce;
							}
						}
						else
						{
							newLowerAirEnergy += incomingRadiation;
							energyAbsorbed += incomingRadiation;
						}
					}

					// reduce ice
					iceMelted = Math.Min(iceMelted, surfaceIce);
					if (iceMelted > 0)
					{
						newSurfaceIce -= iceMelted;
						if (!world.IsOcean(elevation, state.SeaLevel))
						{
							// add to surface water
							newSurfaceWater += iceMelted;
						}
						else
						{
							//// cool ocean down
							//newOceanEnergyShallow += (iceMelted / world.Data.MassIce) * world.Data.FreezingTemperature * world.Data.SpecificHeatIce;
						}
					}

					MoveAtmosphereOnWind(world, state, x, y, elevationOrSeaLevel, lowerAirEnergy, upperAirEnergy, lowerAirMass, upperAirMass, lowerWind, upperWind, humidity, cloudCover, ref newLowerAirEnergy, ref newUpperAirEnergy, ref newLowerAirMass, ref newUpperAirMass, ref newHumidity, ref newCloudCover);

					// lose some energy to space
					//float cloudReflectionFactor = world.Data.cloudReflectionRate * cloudOpacity;
					//float humidityPercentage = humidity / atmosphereMass;
					//float heatLossFactor = (1.0f - world.Data.carbonDioxide * world.Data.heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
					//float loss = airEnergy * (1.0f - cloudReflectionFactor) * (world.Data.heatLoss * heatLossFactor * airPressureInverse);
					float energyLostToSpace = (float)(world.Data.AtmosphericHeatLossToSpace * upperAirEnergy);
					newUpperAirEnergy -= energyLostToSpace;

					MoveOceanOnCurrent(world, state, x, y, elevation, surfaceIce, oceanEnergyShallow, oceanEnergyDeep, oceanSalinityShallow, oceanSalinityDeep, oceanTemperatureShallow, oceanTemperatureDeep, oceanDensity, currentShallow, currentDeep, ref newOceanEnergyShallow, ref newOceanEnergyDeep, ref newOceanSalinityShallow, ref newOceanSalinityDeep);
					FlowWater(world, state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
					SeepWaterIntoGround(world, elevation, state.SeaLevel, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);
					if (cloudCover > 0)
					{
						//			UpdateCloudElevation(world, elevationOrSeaLevel, lowerAirTemperature, humidity, atmosphereMass, wind, ref newCloudElevation);
						rainfall = UpdateRainfall(world, state, elevation, cloudCover, lowerAirTemperature, cloudElevation, ref newSurfaceWater, ref newCloudCover);
					}

					//if (float.IsNaN(newLowerAirEnergy) || float.IsNaN(newEvaporation) || float.IsNaN(newSurfaceWater) || float.IsNaN(newSurfaceIce) || float.IsNaN(newGroundWater) || float.IsNaN(newHumidity) || float.IsNaN(newCloudCover) || float.IsNaN(newCloudElevation) || float.IsNaN(newLowerAirMass))
					//{
					//	break;
					//}
					nextState.LowerAirEnergy[index] = newLowerAirEnergy;
					nextState.UpperAirEnergy[index] = newUpperAirEnergy;
					nextState.LowerAirMass[index] = newLowerAirMass;
					nextState.UpperAirMass[index] = newUpperAirMass;
					nextState.SurfaceWater[index] = newSurfaceWater;
					nextState.SurfaceIce[index] = newSurfaceIce;
					nextState.GroundWater[index] = newGroundWater;
					nextState.Humidity[index] = newHumidity;
					nextState.CloudCover[index] = newCloudCover;
					nextState.CloudElevation[index] = newCloudElevation;
					nextState.Radiation[index] = newRadiation;
					nextState.OceanTemperatureShallow[index] = GetWaterTemperature(world, newOceanEnergyShallow, Math.Max(0, world.Data.DeepOceanDepth - newSurfaceIce));
					nextState.OceanEnergyShallow[index] = newOceanEnergyShallow;
					nextState.OceanEnergyDeep[index] = newOceanEnergyDeep;
					nextState.OceanSalinityDeep[index] = newOceanSalinityDeep;
					nextState.OceanSalinityShallow[index] = newOceanSalinityShallow;
					nextState.OceanDensityDeep[index] = GetOceanDensity(world, newOceanEnergyDeep, newOceanSalinityDeep, state.SeaLevel - elevation);
					float newLowerAirTemperature = GetAirTemperature(world, newLowerAirEnergy, newLowerAirMass);
					float newUpperAirTemperature = GetAirTemperature(world, newUpperAirEnergy, newUpperAirMass);
					nextState.LowerAirTemperature[index] = newLowerAirTemperature;
					nextState.UpperAirTemperature[index] = newUpperAirTemperature;
					nextState.LowerAirPressure[index] = GetAirPressure(world, newLowerAirMass, newLowerAirTemperature, elevationOrSeaLevel, world.Data.BoundaryZoneElevation);
					nextState.UpperAirPressure[index] = GetAirPressure(world, newUpperAirMass, newUpperAirTemperature, world.Data.troposphereElevation, world.Data.troposphereElevation - (elevationOrSeaLevel + world.Data.BoundaryZoneElevation));

					nextState.Evaporation[index] = evaporation;
					nextState.Rainfall[index] = rainfall;
					nextState.EnergyAbsorbed[index] = energyAbsorbed;

					globalEnergyGained += energyAbsorbed;
					globalEnergy += newLowerAirEnergy + newUpperAirEnergy + newOceanEnergyDeep + newOceanEnergyShallow;
					globalEnergyLost += energyLostToSpace;

					atmosphericMass += newLowerAirMass + newUpperAirMass;

				}
			}

			nextState.GlobalEnergyLost = globalEnergyLost;
			nextState.GlobalEnergyGained = globalEnergyGained;
			nextState.GlobalEnergy = globalEnergy;
			nextState.GlobalEnergyIncoming = globalEnergyIncoming;
			nextState.GlobalEnergyReflectedAtmosphere = globalEnergyReflectedAtmosphere;
			nextState.GlobalEnergyReflectedSurface = globalEnergyReflectedSurface;
			nextState.GlobalEnergyAbsorbedUpperAtmosphere = globalEnergyAbsorbedUpperAtmosphere;
			nextState.GlobalEnergyAbsorbedLowerAtmosphere = globalEnergyAbsorbedLowerAtmosphere;
			nextState.GlobalEnergyAbsorbedSurface = globalEnergyAbsorbedSurface;
			nextState.GlobalTemperature = globalTemperature;
			nextState.GlobalOceanCoverage = globalOceanCoverage / (world.Size * world.Size);
			nextState.AtmosphericMass = atmosphericMass;
		}

		static public float GetAirPressure(World world, float mass, float temperature, float elevation, float volume)
		{
			float density = world.Data.LowerAirDensity - (world.Data.LowerAirDensity - world.Data.UpperAirDensity) * (elevation / world.Data.troposphereElevation);
			float pressure = (float)(mass * temperature * world.Data.MassEarthAir / (volume * density));
			return pressure;
		}

		static public float GetOceanDensity(World world, float oceanTemperature, float oceanSalinity, float volume)
		{
			if (volume <= 0)
			{
				return 0;
			}
			return world.Data.OceanDensityPerSalinity * (oceanSalinity / volume) + world.Data.OceanDensityPerTemperature * (world.Data.FreezingTemperature / oceanTemperature);
		}


		static public float GetLocalTemperature(World world, float sunAngle, float cloudCover, float temperature, float lengthOfDay)
		{
			return temperature + (1.0f - Math.Min(cloudCover / world.Data.CloudContentFullAbsorption, 1.0f)) * sunAngle * world.Data.localSunHeat * lengthOfDay;
		}

		static public float GetPressureAtElevation(World world, World.State state, int index, float elevation)
		{
			// Units: Pascals
			// Barometric Formula
			// Pressure = StaticPressure * (StdTemp / (StdTemp + StdTempLapseRate * (Elevation - ElevationAtBottomOfAtmLayer)) ^ (GravitationalAcceleration * MolarMassOfEarthAir / (UniversalGasConstant * StdTempLapseRate))
			// https://en.wikipedia.org/wiki/Barometric_formula
			// For the bottom layer of atmosphere ( < 11000 meters), ElevationAtBottomOfAtmLayer == 0)

			//	float standardPressure = Data.StaticPressure * (float)Math.Pow(Data.StdTemp / (Data.StdTemp + Data.StdTempLapseRate * elevation), Data.PressureExponent);
			float pressure = world.Data.StaticPressure * (float)Math.Pow(world.Data.StdTemp / (world.Data.StdTemp + world.Data.temperatureLapseRate * elevation), world.Data.PressureExponent);
			return pressure;
		}


		static public Vector3 GetSunVector(World world, float planetTiltAngle, int ticks, float latitude)
		{

			float angleOfInclination = planetTiltAngle * (float)Math.Sin(Math.PI * 2 * (world.GetTimeOfYear(ticks) - 0.25f));
			//float timeOfDay = (-sunPhase + 0.5f) * Math.PI * 2;
			float timeOfDay = (float)0;
			float azimuth = (float)Math.Atan2(Math.Sin(timeOfDay), Math.Cos(timeOfDay) * Math.Sin(latitude * Math.PI) - Math.Tan(angleOfInclination) * Math.Cos(latitude * Math.PI));
			float elevation = (float)Math.Asin((Math.Sin(latitude) * Math.Sin(angleOfInclination) + Math.Cos(latitude) * Math.Cos(angleOfInclination) * Math.Cos(timeOfDay)));

			float cosOfElevation = (float)Math.Cos(elevation);
			Vector3 sunVec = new Vector3((float)Math.Sin(azimuth) * cosOfElevation, (float)Math.Cos(azimuth) * cosOfElevation, (float)Math.Sin(elevation));
			return sunVec;
		}



		static private void MoveHumidityToClouds(World world, float humidity, float relativeHumidity, Vector3 windAtSurface, ref float newHumidity, ref float newCloudCover)
		{
			float humidityToCloud = humidity * Mathf.Clamp01(windAtSurface.z * relativeHumidity * world.Data.HumidityToCloudSpeed);
			newHumidity -= humidityToCloud;
			newCloudCover += humidityToCloud;
		}

		static private void UpdateCloudElevation(World world, float elevationOrSeaLevel, float temperature, float humidity, float atmosphereMass, Vector3 windAtCloudElevation, ref float newCloudElevation)
		{
			float dewPointTemp = (float)Math.Pow(humidity / (world.Data.DewPointRange * atmosphereMass), 0.25f) * world.Data.DewPointTemperatureRange + world.Data.DewPointZero;
			float dewPointElevation = Math.Max(0, (dewPointTemp - temperature) / world.Data.temperatureLapseRate) + elevationOrSeaLevel;

			float desiredDeltaZ = dewPointElevation - newCloudElevation;
			newCloudElevation = elevationOrSeaLevel + 1000;
			//		newCloudElevation = newCloudElevation + desiredDeltaZ* Data.cloudElevationDeltaSpeed + windAtCloudElevation.z * Data.windVerticalCloudSpeedMultiplier;
			//	newCloudElevation = Mathf.Clamp(newCloudElevation, elevationOrSeaLevel+1, Data.stratosphereElevation);
		}


		static private float UpdateRainfall(World world, World.State state, float elevation, float cloudCover, float temperature, float cloudElevation, ref float newSurfaceWater, ref float newCloudCover)
		{
			float temperatureAtCloudElevation = cloudElevation * world.Data.temperatureLapseRate + temperature;
			float rainPoint = Math.Max(0, (temperatureAtCloudElevation - world.Data.DewPointZero) * world.Data.rainPointTemperatureMultiplier);
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

		static private float GetEvaporationRate(World world, float iceCoverage, float temperature, float relativeHumidity, float airPressure)
		{
			float evapTemperature = Mathf.Clamp01((temperature - world.Data.EvapMinTemperature) / world.Data.evapTemperatureRange);
			float evapRate = Mathf.Clamp01(evapTemperature / airPressure);
			evapRate *= evapRate;

			return Mathf.Clamp01(iceCoverage * (1.0f - relativeHumidity) * evapRate) * world.Data.EvaporationRate;
		}

		static public float GetRelativeHumidity(World world, float temperature, float humidity, float airPressure)
		{
			float maxHumidity = airPressure * world.Data.DewPointRange * Mathf.Clamp01(temperature - world.Data.DewPointZero);
			if (maxHumidity <= 0)
			{
				return humidity > 0 ? 10000 : 0;
			}
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
				if (waterTableDepth > 0)
				{
					var groundWaterEvap = Math.Max(0, Math.Min(newGroundWater, groundWater / waterTableDepth * evapRate));
					newGroundWater -= groundWaterEvap;
					humidity += groundWaterEvap;
					evaporation += groundWaterEvap;
				}
			}

			float evapotranspiration = evaporation * world.Data.EvaporativeHeatLoss;
			newAirEnergy += evapotranspiration;
			newOceanEnergy -= evapotranspiration;

		}

		static private void MoveAtmosphereOnWind(World world, World.State state, int x, int y, float elevationOrSeaLevel, float lowerEnergy, float upperEnergy, float lowerMass, float upperMass, Vector3 lowerWind, Vector3 upperWind, float humidity, float cloudContent, ref float newLowerEnergy, ref float newUpperEnergy, ref float newLowerMass, ref float newUpperMass, ref float newHumidity, ref float newCloudContent)
		{
			float upperAtmosphereVolume = world.Data.troposphereElevation - (elevationOrSeaLevel + world.Data.BoundaryZoneElevation);

			float totalUpperWind = Mathf.Abs(upperWind.x) + Mathf.Abs(upperWind.y);
			float totalLowerWind = Mathf.Abs(lowerWind.x) + Mathf.Abs(lowerWind.y);
			float upperMassLeaving = totalUpperWind * upperMass * world.Data.WindAirMovement;
			float lowerMassLeaving = totalLowerWind * lowerMass * world.Data.WindAirMovement;
			newUpperMass -= upperMassLeaving;
			newLowerMass -= lowerMassLeaving;
			newUpperEnergy -= upperMassLeaving / upperMass * upperEnergy;
			newLowerEnergy -= lowerMassLeaving / lowerMass * lowerEnergy;
			newHumidity -= humidity * Mathf.Clamp01(totalLowerWind * world.Data.WindHumidityMovement);
			newCloudContent -= cloudContent * Mathf.Clamp01(totalUpperWind * world.Data.WindCloudMovement);

			float verticalMassTransfer;
			float verticalEnergyTransfer;
			if (lowerWind.z > 0)
			{
				verticalMassTransfer = lowerMass * lowerWind.z * world.Data.WindAirMovement;
				verticalEnergyTransfer = verticalMassTransfer / lowerMass * lowerEnergy;
			} else
			{
				verticalMassTransfer = upperMass * lowerWind.z * world.Data.WindAirMovement;
				verticalEnergyTransfer = verticalMassTransfer / upperMass * upperEnergy;
			}
			newUpperMass += verticalMassTransfer;
			newLowerMass -= verticalMassTransfer;
			newUpperEnergy += verticalEnergyTransfer;
			newLowerEnergy -= verticalEnergyTransfer;

			for (int i = 0; i < 4; i++)
			{
				var neighbor = world.GetNeighbor(x, y, i);
				int nIndex = world.GetIndex(neighbor.x, neighbor.y);
				var nUpperWind = state.UpperWind[nIndex];
				var nLowerWind = state.LowerWind[nIndex];
				float nUpperEnergy = state.UpperAirEnergy[nIndex];
				float nUpperMass = state.UpperAirMass[nIndex];
				float nLowerEnergy = state.LowerAirEnergy[nIndex];
				float nLowerMass = state.LowerAirMass[nIndex];
				float nHumidity = state.Humidity[nIndex];
				float nCloudContent = state.CloudCover[nIndex];
				float nElevationOrSeaLevel = Math.Max(0, state.Elevation[nIndex]);
				float nUpperAtmosphereVolume = world.Data.troposphereElevation - (nElevationOrSeaLevel + world.Data.BoundaryZoneElevation);
				float nLowerWindSpeed = Mathf.Abs(nLowerWind.x) + Mathf.Abs(nLowerWind.y);
				float nUpperWindSpeed = Mathf.Abs(nUpperWind.x) + Mathf.Abs(nUpperWind.y);

				float lowerMassTransfer = 0;
				float upperMassTransfer = 0;
				float humidityTransfer = 0;
				float humidityMovement = nHumidity * Mathf.Min(world.Data.WindHumidityMovement, 1.0f / nLowerWindSpeed);
				float cloudMovement = nCloudContent * Mathf.Min(world.Data.WindCloudMovement, 1.0f / nUpperWindSpeed);

				// Mixing Upper atmosphere
				{
					float mixingVolume = Math.Min(upperAtmosphereVolume, nUpperAtmosphereVolume);
					float densityDiff = nUpperMass / nUpperAtmosphereVolume - upperMass / upperAtmosphereVolume;
					upperMassTransfer = densityDiff * mixingVolume * world.Data.AirDispersalSpeed;
				}

				// mixing lower atmosphere
				{
					float densityDiff = nLowerMass - lowerMass;
					lowerMassTransfer = densityDiff * world.Data.AirDispersalSpeed;
					humidityTransfer = (nHumidity - humidity) * world.Data.HumidityDispersalSpeed;
				}

				// Blowing on wind
				{
					switch (i)
					{
						case (int)Direction.West:
							if (nUpperWind.x > 0)
							{
								upperMassTransfer += nUpperMass * nUpperWind.x * world.Data.WindAirMovement;
								newCloudContent += nUpperWind.x * cloudMovement;
							}
							if (nLowerWind.x > 0)
							{
								lowerMassTransfer += nLowerMass * nLowerWind.x * world.Data.WindAirMovement;
								newHumidity += nLowerWind.x * humidityMovement;
							}
							break;
						case (int)Direction.East:
							if (nUpperWind.x < 0)
							{
								upperMassTransfer += nUpperMass * -nUpperWind.x * world.Data.WindAirMovement;
								newCloudContent += -nUpperWind.x * cloudMovement;
							}
							if (nLowerWind.x < 0)
							{
								lowerMassTransfer += nLowerMass * -nLowerWind.x * world.Data.WindAirMovement;
								newHumidity += -nLowerWind.x * humidityMovement;
							}
							break;
						case (int)Direction.South:
							if (nUpperWind.y < 0)
							{
								upperMassTransfer += nUpperMass * -nUpperWind.y * world.Data.WindAirMovement;
								newCloudContent += -nUpperWind.y * cloudMovement;
							}
							if (nLowerWind.y < 0)
							{
								lowerMassTransfer += nLowerMass * -nLowerWind.y * world.Data.WindAirMovement;
								newHumidity += -nLowerWind.y * humidityMovement;
							}
							break;
						case (int)Direction.North:
							if (nUpperWind.y > 0)
							{
								upperMassTransfer += nUpperMass * nUpperWind.y * world.Data.WindAirMovement;
								newCloudContent += nUpperWind.y * cloudMovement;
							}
							if (nLowerWind.y > 0)
							{
								lowerMassTransfer += nLowerMass * nLowerWind.y * world.Data.WindAirMovement;
								newHumidity += nLowerWind.y * humidityMovement;
							}
							break;
					}
				}
				newUpperMass += upperMassTransfer;
				if (upperMassTransfer > 0)
				{
					newUpperEnergy += nUpperEnergy * upperMassTransfer / nUpperMass;
				}
				else
				{
					newUpperEnergy += upperEnergy * upperMassTransfer / upperMass;
				}

				newHumidity += humidityTransfer;
				newLowerMass += lowerMassTransfer;
				if (lowerMassTransfer > 0)
				{
					newLowerEnergy += nLowerEnergy * lowerMassTransfer / nLowerMass;
				}
				else
				{
					newLowerEnergy += lowerEnergy * lowerMassTransfer / lowerMass;
				}

			}
			newHumidity = Math.Max(0, newHumidity);
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
		ref float newOceanSalinityDeep)
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
				float shallowColumnDepth = Mathf.Max(0, world.Data.DeepOceanDepth - ice);

				if (oceanEnergyShallow <= world.Data.FreezingTemperature + 5)
				{
					float salinityExchange = oceanSalinityShallow;
					newOceanSalinityDeep += salinityExchange;
					newOceanSalinityShallow -= salinityExchange;
					newOceanEnergyDeep = world.Data.FreezingTemperature;
				}
				else
				{
					float salinityExchange = (oceanSalinityDeep / depth - oceanSalinityShallow / shallowColumnDepth) * world.Data.SalinityVerticalMixingSpeed * (oceanSalinityShallow + oceanSalinityDeep);
					newOceanSalinityDeep -= salinityExchange * depth / (depth + shallowColumnDepth);
					newOceanSalinityShallow += salinityExchange * world.Data.DeepOceanDepth / (depth + shallowColumnDepth);

					float deepWaterMixingDepth = Math.Min(world.Data.DeepOceanDepth, depth);
					float heatExchange = (oceanTemperatureDeep - oceanTemperatureShallow) * deepWaterMixingDepth * world.Data.OceanTemperatureVerticalMixingSpeed;
					newOceanEnergyShallow += heatExchange;
					newOceanEnergyDeep -= heatExchange;
				}

				if (currentShallow.z < 0)
				{
					float downwelling = Math.Min(0.5f, -currentShallow.z * world.Data.OceanUpwellingSpeed);
					float energyExchange = oceanEnergyShallow * downwelling;
					newOceanEnergyShallow -= energyExchange;
					newOceanEnergyDeep += energyExchange;
					float salinityExchange = oceanSalinityShallow * downwelling;
					newOceanSalinityDeep += salinityExchange;
					newOceanSalinityShallow -= salinityExchange;
				}
				else if (currentShallow.z > 0)
				{
					float upwelling = Math.Min(0.5f, currentShallow.z * world.Data.OceanUpwellingSpeed);
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
					float nTemperatureShallow = state.OceanTemperatureShallow[nIndex];
					float nTemperatureDeep = GetWaterTemperature(world, nEnergyDeep, neighborDepth);
					float nSalinityShallow = state.OceanSalinityShallow[nIndex];
					float nSalinityDeep = state.OceanSalinityDeep[nIndex];

					// Horizontal mixing
					float mixingDepth = Math.Min(neighborDepth, depth);
					newOceanEnergyShallow += world.Data.SpecificHeatSeaWater * world.Data.DeepOceanDepth * (nTemperatureShallow - oceanTemperatureShallow) * world.Data.OceanHorizontalMixingSpeed;
					newOceanEnergyDeep += world.Data.SpecificHeatSeaWater * mixingDepth * (nTemperatureDeep - oceanTemperatureDeep) * world.Data.OceanHorizontalMixingSpeed;

					float nSalinityDeepPercentage = nSalinityDeep / neighborDepth;
					newOceanSalinityDeep += (nSalinityDeepPercentage - salinityDeepPercentage) * world.Data.OceanHorizontalMixingSpeed * Math.Min(neighborDepth, depth);
					newOceanSalinityShallow += (nSalinityShallow - oceanSalinityShallow) * world.Data.OceanHorizontalMixingSpeed;

					switch (i)
					{
						case 0:
							if (neighborCurrentShallow.x > 0)
							{
								float absX = Math.Abs(neighborCurrentShallow.x);
								newOceanEnergyShallow += nEnergyShallow * Math.Min(0.25f, absX * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absX * world.Data.OceanSalinityCurrentSpeed);
							}
							if (currentShallow.x < 0)
							{
								float absX = Math.Abs(currentShallow.x);
								newOceanEnergyShallow -= oceanEnergyShallow * Math.Min(0.25f, absX * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absX * world.Data.OceanSalinityCurrentSpeed);
							}
							if (neighborCurrentDeep.x > 0)
							{
								float absX = Math.Abs(neighborCurrentDeep.x);
								newOceanEnergyDeep += nEnergyDeep * Math.Min(0.25f, absX * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absX * world.Data.OceanSalinityCurrentSpeed);
							}
							if (currentDeep.x < 0)
							{
								float absX = Math.Abs(currentDeep.x);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absX * world.Data.OceanSalinityCurrentSpeed);
							}
							break;
						case 1:
							if (neighborCurrentShallow.x < 0)
							{
								float absX = Math.Abs(neighborCurrentShallow.x);
								newOceanEnergyShallow += nEnergyShallow * Math.Min(0.25f, absX * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absX * world.Data.OceanSalinityCurrentSpeed);
							}
							if (currentShallow.x > 0)
							{
								float absX = Math.Abs(currentShallow.x);
								newOceanEnergyShallow -= oceanEnergyShallow * Math.Min(0.25f, absX * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absX * world.Data.OceanSalinityCurrentSpeed);
							}
							if (neighborCurrentDeep.x < 0)
							{
								float absX = Math.Abs(neighborCurrentDeep.x);
								newOceanEnergyDeep += nEnergyDeep * Math.Min(0.25f, absX * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absX * world.Data.OceanSalinityCurrentSpeed);
							}
							if (currentDeep.x > 0)
							{
								float absX = Math.Abs(currentDeep.x);
								newOceanEnergyDeep -= oceanEnergyDeep * Math.Min(0.25f, absX * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absX * world.Data.OceanSalinityCurrentSpeed);
							}
							break;
						case 2:
							if (neighborCurrentShallow.y < 0)
							{
								float absY = Math.Abs(neighborCurrentShallow.y);
								newOceanEnergyShallow += nEnergyShallow * Math.Min(0.25f, absY * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absY * world.Data.OceanSalinityCurrentSpeed);
							}
							if (currentShallow.y > 0)
							{
								float absY = Math.Abs(currentShallow.y);
								newOceanEnergyShallow -= oceanEnergyShallow * Math.Min(0.25f, absY * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absY * world.Data.OceanSalinityCurrentSpeed);
							}
							if (neighborCurrentDeep.y < 0)
							{
								float absY = Math.Abs(neighborCurrentDeep.y);
								newOceanEnergyDeep += nEnergyDeep * Math.Min(0.25f, absY * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absY * world.Data.OceanSalinityCurrentSpeed);
							}
							if (currentDeep.y > 0)
							{
								float absY = Math.Abs(currentDeep.y);
								newOceanEnergyDeep -= oceanEnergyDeep * Math.Min(0.25f, absY * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absY * world.Data.OceanSalinityCurrentSpeed);
							}
							break;
						case 3:
							if (neighborCurrentShallow.y > 0)
							{
								float absY = Math.Abs(neighborCurrentShallow.y);
								newOceanEnergyShallow += nEnergyShallow * Math.Min(0.25f, absY * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityShallow += nSalinityShallow * Math.Min(0.25f, absY * world.Data.OceanSalinityCurrentSpeed);
							}
							if (currentShallow.y < 0)
							{
								float absY = Math.Abs(currentShallow.y);
								newOceanEnergyShallow -= oceanEnergyShallow * Math.Min(0.25f, absY * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityShallow -= oceanSalinityShallow * Math.Min(0.25f, absY * world.Data.OceanSalinityCurrentSpeed);
							}
							if (neighborCurrentDeep.y > 0)
							{
								float absY = Math.Abs(neighborCurrentDeep.y);
								newOceanEnergyDeep += nEnergyDeep * Math.Min(0.25f, absY * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityDeep += nSalinityDeep * Math.Min(0.25f, absY * world.Data.OceanSalinityCurrentSpeed);
							}
							if (currentDeep.y < 0)
							{
								float absY = Math.Abs(currentDeep.y);
								newOceanEnergyDeep -= oceanEnergyDeep * Math.Min(0.25f, absY * world.Data.OceanEnergyCurrentSpeed);
								newOceanSalinityDeep -= oceanSalinityDeep * Math.Min(0.25f, absY * world.Data.OceanSalinityCurrentSpeed);
							}
							break;
					}
				}
			}
			newOceanSalinityDeep = Math.Max(0, newOceanSalinityDeep);
			newOceanSalinityShallow = Math.Max(0, newOceanSalinityShallow);
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

		static public float GetAirTemperature(World world, float energy, float mass)
		{
			return (float)(energy / (mass * world.Data.SpecificHeatAtmosphere));
		}
		static public float GetAirEnergy(World world, float temperature, float mass)
		{
			return temperature * mass * world.Data.SpecificHeatAtmosphere;
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