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
			float globalEnergyAbsorbedClouds = 0;
			float globalEnergyAbsorbedUpperAtmosphere = 0;
			float globalEnergyAbsorbedLowerAtmosphere = 0;
			float globalEnergyAbsorbedSurface = 0;
			float globalOceanCoverage = 0;
			float globalTemperature = 0;
			float atmosphericMass = 0;

			for (int y = 0; y < world.Size; y++)
			{
				float latitude = world.GetLatitude(y);
				float sunAngle;
				Vector3 sunVector;
				GetSunVector(world, state.PlanetTiltAngle, state.Ticks, latitude, out sunAngle, out sunVector);
				sunAngle = Math.Max(0, sunAngle);

				float lengthOfDay = GetLengthOfDay(latitude, timeOfYear, declinationOfSun);

				// get the actual atmospheric depth here based on radius of earth plus atmosphere
				float inverseSunAngle = Mathf.PI / 2 + sunAngle;
				float angleFromSunToLatitudeAndAtmophereEdge = Mathf.Asin(state.PlanetRadius * Mathf.Sin(inverseSunAngle) / (state.PlanetRadius + world.Data.troposphereElevation));
				float angleFromPlanetCenterToLatitudeAndAtmosphereEdge = Mathf.PI - inverseSunAngle - angleFromSunToLatitudeAndAtmophereEdge;
				float atmosphericDepthInMeters = Mathf.Sin(angleFromPlanetCenterToLatitudeAndAtmosphereEdge) * state.PlanetRadius / Mathf.Sin(angleFromSunToLatitudeAndAtmophereEdge);
				float atmosphericDepth = Mathf.Max(1.0f, atmosphericDepthInMeters / world.Data.troposphereElevation);

				float inverseAtmosphericDepth = 1.0f - Mathf.Pow(Mathf.Clamp01(Mathf.PI / 2 / sunAngle), 2);

				// TODO: These constants obtained here, dunno if I've interpreted them correctly
				// https://www.pveducation.org/pvcdrom/properties-of-sunlight/air-mass

				////// MAJOR TODO:
				///// USE THIS LINK: https://www.ftexploring.com/solar-energy/sun-angle-and-insolation2.htm
				/// With the sun 90 degrees above the horizon (SEA° = 90°), the air mass lowers the intensity of the sunlight from the 1,367 W / m2 that it is in outerspace down to about 1040 W / m2.
//				float consumedByAtmosphere = 1.0f - Mathf.Pow(0.7f, Mathf.Pow(atmosphericDepth, 0.678f));

				for (int x = 0; x < world.Size; x++)
				{
					int index = world.GetIndex(x, y);

					float elevation = state.Elevation[index];
					float elevationOrSeaLevel = Math.Max(state.SeaLevel, elevation);
					float cloudMass = state.CloudMass[index];
					float cloudEnergy = state.CloudEnergy[index];
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
					float surfaceIce = state.Ice[index];
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
					float newCloudMass = cloudMass;
					float newCloudEnergy = cloudEnergy;
					float newSurfaceWater = surfaceWater;
					float newSurfaceIce = surfaceIce;
					float newCloudElevation = cloudElevation;
					float rainfall = 0;
					float newRadiation = radiation;
					float newOceanEnergyDeep = oceanEnergyDeep;
					float newOceanEnergyShallow = oceanEnergyShallow;
					float newOceanSalinityDeep = oceanSalinityDeep;
					float newOceanSalinityShallow = oceanSalinityShallow;
					float massOfAtmosphericColumn = upperAirMass + lowerAirMass;
					float iceCoverage = Mathf.Min(1.0f, surfaceIce / world.Data.FullIceCoverage);
					float inverseLowerAirPressure = 1.0f / lowerAirPressure;
					bool isOcean = world.IsOcean(elevation, state.SeaLevel);

					float relativeHumidity = GetRelativeHumidity(world, lowerAirTemperature, humidity, lowerAirMass);
					float cloudOpacity = Math.Min(1.0f, cloudMass / world.Data.CloudMassFullAbsorption); // TODO: prob sqrt this and use a higher cap
					float groundWaterSaturation = Animals.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], soilFertility * world.Data.MaxSoilPorousness);
					float incomingRadiation = state.SolarRadiation * lengthOfDay / 2;
					float energyAbsorbed = 0;

					if (isOcean)
					{
						globalOceanCoverage++;
					}
					globalEnergyIncoming += incomingRadiation;

					// TODO: reflect/absorb more in the atmosphere with a lower sun angle

					// reflect some rads off atmosphere and clouds
					// TODO: this process feels a little broken -- are we giving too much priority to reflecting/absorbing in certain layers?
					float energyReflectedAtmosphere = incomingRadiation * (world.Data.AtmosphericHeatReflection + (1.0f - world.Data.AtmosphericHeatReflection) * inverseAtmosphericDepth);
					incomingRadiation -= energyReflectedAtmosphere;

					float cloudReflectionRate = world.Data.CloudIncomingReflectionRate * cloudOpacity;
					float energyReflectedClouds = incomingRadiation * (cloudReflectionRate + (1.0f - cloudReflectionRate) * inverseAtmosphericDepth);
					incomingRadiation -= energyReflectedClouds;

					float cloudAbsorptionRate = world.Data.CloudIncomingAbsorptionRate * cloudOpacity;
					float absorbedByCloudsIncoming = incomingRadiation * (cloudAbsorptionRate + (1.0f - cloudAbsorptionRate) * inverseAtmosphericDepth);
					incomingRadiation -= absorbedByCloudsIncoming;
					newCloudEnergy += absorbedByCloudsIncoming;

					float upperAtmosphereAbsorptionRate = world.Data.AtmosphericHeatAbsorption * (upperAirMass / massOfAtmosphericColumn);
					float absorbedByUpperAtmosphereIncoming = incomingRadiation * (upperAtmosphereAbsorptionRate + (1.0f - upperAtmosphereAbsorptionRate) * inverseAtmosphericDepth);
					incomingRadiation -= absorbedByUpperAtmosphereIncoming;
					newUpperAirEnergy += absorbedByUpperAtmosphereIncoming;

					globalEnergyReflectedAtmosphere += energyReflectedAtmosphere;

					// Absorbed by atmosphere
					{
						// stratosphere accounts for about a quarter of atmospheric mass
						//	float absorbedByStratosphere = incomingRadiation * world.Data.AtmosphericHeatAbsorption * (state.StratosphereMass / massOfAtmosphericColumn);

						float lowerAtmosphereAbsorptionRate = world.Data.AtmosphericHeatAbsorption * (lowerAirMass / massOfAtmosphericColumn);
						float absorbedByLowerAtmosphereIncoming = incomingRadiation * (lowerAtmosphereAbsorptionRate + (1.0f - lowerAtmosphereAbsorptionRate) * inverseAtmosphericDepth);

						float humidityAbsorptionRate = Mathf.Min(0.1f, humidity * world.Data.HumidityHeatAbsorption);
						absorbedByLowerAtmosphereIncoming += incomingRadiation * (humidityAbsorptionRate + (1.0f - humidityAbsorptionRate) * inverseAtmosphericDepth);

						newLowerAirEnergy += absorbedByLowerAtmosphereIncoming;
						incomingRadiation -= absorbedByLowerAtmosphereIncoming;

						//	incomingRadiation -= absorbedByStratosphere;

						globalEnergyAbsorbedLowerAtmosphere += absorbedByLowerAtmosphereIncoming;
						globalEnergyAbsorbedUpperAtmosphere += absorbedByUpperAtmosphereIncoming;
						globalEnergyAbsorbedClouds += absorbedByCloudsIncoming;
					}

					// reflection off surface
					float energyReflected = 0;
					{
						float slopeAlbedo = 0;
						if (!isOcean)
						{
							slopeAlbedo = 1.0f - Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
						} else
						{
							slopeAlbedo = 1.0f - Math.Max(0, sunVector.z);
						}
						if (surfaceIce > 0)
						{
							energyReflected += incomingRadiation * Math.Min(1.0f, surfaceIce) * GetAlbedo(world.Data.AlbedoIce, slopeAlbedo);
						}
						if (isOcean)
						{
							energyReflected = incomingRadiation * GetAlbedo(world.Data.AlbedoWater, slopeAlbedo) * Math.Max(0, (1.0f - surfaceIce));
						}
						else
						{
							// reflect some incoming radiation
							float waterReflectivity = surfaceWater * world.Data.AlbedoWater;
							float soilReflectivity = GetAlbedo(world.Data.AlbedoLand - world.Data.AlbedoReductionSoilQuality * soilFertility, slopeAlbedo);
							float heatReflectedLand = canopy * world.Data.AlbedoFoliage + Math.Max(0, 1.0f - canopy) * (surfaceWater * GetAlbedo(world.Data.AlbedoWater, slopeAlbedo) + Math.Max(0, 1.0f - surfaceWater) * soilReflectivity);
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
							if (isOcean || surfaceWater > 0)
							{
								// world.Data.SpecificHeatIce * world.Data.MassIce == KJ required to raise one cubic meter by 1 degree
								float surfaceTemp = lowerAirTemperature + incomingRadiation / (world.Data.SpecificHeatSeaWater * world.Data.MassSeaWater);
								if (surfaceTemp < world.Data.FreezingTemperature)
								{
									float iceFrozen = Math.Min(world.Data.FullIceCoverage - iceCoverage, (world.Data.FreezingTemperature - surfaceTemp) / (world.Data.SpecificHeatSeaWater * world.Data.MassSeaWater));
									newSurfaceIce += iceFrozen;
									if (!isOcean)
									{
										// add to surface water
										newSurfaceWater -= iceFrozen;
									}
								}

								// evaporation
								float evapRate = GetEvaporationRate(world, surfaceIce, lowerAirTemperature, relativeHumidity, inverseLowerAirPressure);
								EvaporateWater(world, isOcean, evapRate, groundWater, waterTableDepth, ref newHumidity, ref newLowerAirEnergy, ref newOceanEnergyShallow, ref newGroundWater, ref newSurfaceWater, out evaporation);

							}
						}
					}


					// absorbed by surface
					{
						globalEnergyAbsorbedSurface += incomingRadiation;
						energyAbsorbed += incomingRadiation;
						// absorb the remainder and radiate heat
						if (isOcean)
						{
							// absorb remaining incoming radiation (we've already absorbed radiation in surface ice above)
							newOceanEnergyShallow += incomingRadiation;

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
						}
					}

					// reduce ice
					iceMelted = Math.Min(iceMelted, surfaceIce);
					if (iceMelted > 0)
					{
						newSurfaceIce -= iceMelted;
						if (!isOcean)
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

					MoveWaterVaporVertically(
						world, 
						isOcean, 
						surfaceIce, 
						humidity, 
						relativeHumidity, 
						lowerWind, 
						lowerAirMass, 
						lowerAirEnergy, 
						lowerAirTemperature, 
						cloudMass, 
						cloudEnergy, 
						ref newSurfaceWater, 
						ref newSurfaceIce, 
						ref newHumidity, 
						ref newLowerAirEnergy,
						ref newCloudMass, 
						ref newCloudEnergy);
					MoveAtmosphereOnWind(world, state, x, y, elevationOrSeaLevel, lowerAirEnergy, upperAirEnergy, lowerAirMass, upperAirMass, lowerWind, upperWind, humidity, cloudMass, ref newLowerAirEnergy, ref newUpperAirEnergy, ref newLowerAirMass, ref newUpperAirMass, ref newHumidity, ref newCloudMass);

					// lose some energy to space
					//float cloudReflectionFactor = world.Data.cloudReflectionRate * cloudOpacity;
					//float humidityPercentage = humidity / atmosphereMass;
					//float heatLossFactor = (1.0f - world.Data.carbonDioxide * world.Data.heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
					//float loss = airEnergy * (1.0f - cloudReflectionFactor) * (world.Data.heatLoss * heatLossFactor * airPressureInverse);

					// absorb some outgoing energy in clouds
					float energyEmittedByUpperAtmosphere = world.Data.EnergyEmittedByAtmosphere * upperAirEnergy;
					float outgoingEnergyAbsorbedByClouds = energyEmittedByUpperAtmosphere * world.Data.CloudOutgoingAbsorptionRate;
					newCloudEnergy += outgoingEnergyAbsorbedByClouds;
					energyEmittedByUpperAtmosphere -= outgoingEnergyAbsorbedByClouds;

					// emit some remaining atmospheric energy to space
					float energyLostToSpace = energyEmittedByUpperAtmosphere * Mathf.Max(1.0f, state.CarbonDioxide * (1.0f - world.Data.EnergyTrappedByGreenhouseGasses));
					newUpperAirEnergy -= energyLostToSpace;

					// emit some energy from clouds to space
					if (cloudMass > 0)
					{
						float cloudTemperature = cloudEnergy / cloudMass * world.Data.SpecificHeatFreshWater;
						float energyEmittedByClouds = cloudEnergy * Mathf.Clamp(world.Data.CloudEnergyDispersalSpeed * (upperAirTemperature - cloudTemperature), 0, 1) / world.Data.SpecificHeatFreshWater;
						newCloudEnergy -= energyEmittedByClouds;
						newUpperAirEnergy += energyEmittedByClouds;
					}

					MoveOceanOnCurrent(world, state, x, y, elevation, surfaceIce, oceanEnergyShallow, oceanEnergyDeep, oceanSalinityShallow, oceanSalinityDeep, oceanTemperatureShallow, oceanTemperatureDeep, oceanDensity, currentShallow, currentDeep, ref newOceanEnergyShallow, ref newOceanEnergyDeep, ref newOceanSalinityShallow, ref newOceanSalinityDeep);
					FlowWater(world, state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
					SeepWaterIntoGround(world, isOcean, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);
					if (cloudMass > 0)
					{
						UpdateCloudElevation(world, elevationOrSeaLevel, lowerAirTemperature, humidity, cloudMass, cloudEnergy, ref newCloudElevation);
						rainfall = UpdateRainfall(world, state, isOcean, elevationOrSeaLevel, cloudMass, cloudEnergy, lowerAirTemperature, upperAirTemperature, upperAirMass, cloudElevation, ref newSurfaceWater, ref newCloudMass, ref newCloudEnergy);
					}

					//if (float.IsNaN(newLowerAirEnergy) || float.IsNaN(newEvaporation) || float.IsNaN(newSurfaceWater) || float.IsNaN(newSurfaceIce) || float.IsNaN(newGroundWater) || float.IsNaN(newHumidity) || float.IsNaN(newCloudCover) || float.IsNaN(newCloudElevation) || float.IsNaN(newLowerAirMass))
					//{
					//	break;
					//}

					if (float.IsNaN(newLowerAirEnergy) || float.IsNaN(newUpperAirEnergy) || float.IsNaN(newLowerAirMass) || float.IsNaN(newUpperAirEnergy) || float.IsNaN(newHumidity))
					{
						return;
					}

					nextState.LowerAirEnergy[index] = newLowerAirEnergy;
					nextState.UpperAirEnergy[index] = newUpperAirEnergy;
					nextState.LowerAirMass[index] = newLowerAirMass;
					nextState.UpperAirMass[index] = newUpperAirMass;
					nextState.SurfaceWater[index] = newSurfaceWater;
					nextState.Ice[index] = newSurfaceIce;
					nextState.GroundWater[index] = newGroundWater;
					nextState.Humidity[index] = newHumidity;
					nextState.CloudMass[index] = newCloudMass;
					nextState.CloudEnergy[index] = newCloudEnergy;
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
					nextState.LowerAirPressure[index] = GetAirPressure(world, newLowerAirMass + newUpperAirMass + state.StratosphereMass, elevationOrSeaLevel, lowerAirTemperature);
					nextState.UpperAirPressure[index] = GetAirPressure(world, newUpperAirMass + state.StratosphereMass, elevationOrSeaLevel + world.Data.BoundaryZoneElevation, upperAirTemperature);

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
			nextState.GlobalEnergyAbsorbedCloud = globalEnergyAbsorbedClouds;
			nextState.GlobalEnergyAbsorbedUpperAtmosphere = globalEnergyAbsorbedUpperAtmosphere;
			nextState.GlobalEnergyAbsorbedLowerAtmosphere = globalEnergyAbsorbedLowerAtmosphere;
			nextState.GlobalEnergyAbsorbedSurface = globalEnergyAbsorbedSurface;
			nextState.GlobalTemperature = globalTemperature;
			nextState.GlobalOceanCoverage = globalOceanCoverage / (world.Size * world.Size);
			nextState.AtmosphericMass = atmosphericMass;
		}

		// TODO: change this to use true atmospheric pressure
		static public float GetAirPressure(World world, float mass, float elevation, float temperature)
		{
			float temperatureLapse = -world.Data.temperatureLapseRate * elevation;
			float pressure = mass * world.Data.GravitationalAcceleration * Mathf.Pow(1.0f -(temperatureLapse) / (temperature + temperatureLapse), (world.Data.GravitationalAcceleration * world.Data.MolarMassEarthAir / (world.Data.UniversalGasConstant * world.Data.temperatureLapseRate)));
			return pressure;
		}

		static public float GetAirMass(World world, float pressure, float elevation, float temperature)
		{
			float temperatureLapse = -world.Data.temperatureLapseRate * elevation;
			float mass = pressure / (world.Data.GravitationalAcceleration * Mathf.Pow(1.0f - (temperatureLapse) / (temperature + temperatureLapse), (world.Data.GravitationalAcceleration * world.Data.MolarMassEarthAir / (world.Data.UniversalGasConstant * world.Data.temperatureLapseRate))));
			return mass;
		}

		static public float GetAirDensity(World world, float lowerAirPressure, float upperAirPressure, float elevationOrSeaLevel, float sampleElevation, float lowerAirTemperature, float upperAirTemperature)
		{
			float temperature;
			float boundaryElevation = elevationOrSeaLevel + world.Data.BoundaryZoneElevation;
			float pressure;
			if (sampleElevation < boundaryElevation)
			{
				temperature = (upperAirTemperature - lowerAirTemperature) * (sampleElevation / boundaryElevation) + lowerAirTemperature;
				pressure = (upperAirPressure - lowerAirPressure) * (sampleElevation / boundaryElevation) + lowerAirPressure;
			} else
			{
				temperature = upperAirTemperature + world.Data.temperatureLapseRate * (sampleElevation - boundaryElevation);
				pressure = upperAirPressure;
			}
			return pressure * world.Data.MolarMassEarthAir / (world.Data.UniversalGasConstant * temperature);
		}

		static public float GetOceanDensity(World world, float oceanTemperature, float oceanSalinity, float volume)
		{
			if (volume <= 0)
			{
				return 0;
			}
			return world.Data.OceanDensityPerSalinity * (oceanSalinity / volume) + world.Data.OceanDensityPerTemperature * (world.Data.FreezingTemperature / oceanTemperature);
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




		static private void MoveWaterVaporVertically(World world, bool isOcean, float ice, float humidity, float relativeHumidity, Vector3 windAtSurface, float lowerAtmosphereMass, float lowerAtmosphereEnergy, float lowerAirTemperature, float cloudMass, float cloudEnergy, ref float newSurfaceWater, ref float newIce, ref float newHumidity, ref float newLowerAtmosphereEnergy, ref float newCloudMass, ref float newCloudEnergy)
		{
			////////////////
			// ACCURATE SIM
			////////////////

			// condensation
			if (relativeHumidity > 1)
			{
				float humidityToGround = humidity * (relativeHumidity - 1.0f) / relativeHumidity;
				humidity -= humidityToGround;
				newHumidity -= humidityToGround;
				if (lowerAirTemperature <= world.Data.FreezingTemperature)
				{
					newIce += humidityToGround / world.Data.MassFreshWater;
				}
				else if (!isOcean)
				{
					newSurfaceWater += humidityToGround / world.Data.MassFreshWater;
				}
			}

			//float humidityTransfer = humidity * Mathf.Clamp01(windAtSurface.z * relativeHumidity * world.Data.HumidityToCloudSpeed);
			//float cloudTransfer = cloudMass * world.Data.CloudToHumiditySpeed;
			//float netTransfer = humidityTransfer - cloudTransfer;
			//newHumidity -= netTransfer;
			//newCloudMass += netTransfer;

			//float humidityEnergyTransfer = lowerAtmosphereEnergy * humidityTransfer / (lowerAtmosphereMass + humidity);
			//float netEnergyTransfer = humidityEnergyTransfer;
			//if (cloudMass > 0)
			//{
			//	netEnergyTransfer -= cloudTransfer / cloudMass * cloudEnergy;
			//}
			//newCloudEnergy += netEnergyTransfer;
			//newLowerAtmosphereEnergy -= netEnergyTransfer;

			////////////////////////////////
			/// INTERMITTENT CLOUD INJECTION
			////////////////////////////////

			//float humidityTransferTime = windAtSurface.z * relativeHumidity * world.Data.HumidityToCloudSpeed;
			//float humidityTransfer = 0;
			//if (humidityTransferTime >= 1)
			//{
			//	humidityTransfer = humidity * Mathf.Clamp01(windAtSurface.z * relativeHumidity * world.Data.HumidityToCloudSpeed);
			//}
			//float cloudTransfer = cloudCover * world.Data.CloudToHumiditySpeed;
			//float netTransfer = humidityTransfer - cloudTransfer;
			//newHumidity -= netTransfer;
			//newCloudCover += netTransfer;



		}

		static private void UpdateCloudElevation(World world, float elevationOrSeaLevel, float lowerTemperature, float upperTemperature, float cloudMass, float cloudEnergy, ref float newCloudElevation)
		{

			// cloudContent = world.Data.WaterVaporMassToAirMassAtDewPoint * Mathf.Pow(Mathf.Max(0, (temperature - world.Data.DewPointZero) / world.Data.DewPointTemperatureRange), 2) * upperAirMass;
			// Mathf.Pow(Mathf.Max(0, (temperature - world.Data.DewPointZero) / world.Data.DewPointTemperatureRange), 2) = cloudContent / (upperAirMass * world.Data.WaterVaporMassToAirMassAtDewPoint);
			// temperature = world.Data.DewPointZero + world.Data.DewPointTemperatureRange * Math.Pow(cloudContent / (upperAirMass * world.Data.WaterVaporMassToAirMassAtDewPoint), 0.5f);


			float cloudTemperature = cloudEnergy * cloudMass * world.Data.MassFreshWater / world.Data.SpecificHeatSeaWater;

			float temperatureLapseRate = (upperTemperature - lowerTemperature) / (world.Data.troposphereElevation - elevationOrSeaLevel);
			float dewPointElevation = Math.Max(0, (cloudTemperature - lowerTemperature) / temperatureLapseRate) + elevationOrSeaLevel;

			newCloudElevation = dewPointElevation;
		}


		static private float UpdateRainfall(World world, World.State state, bool isOcean, float elevationOrSeaLevel, float cloudMass, float cloudEnergy, float lowerTemperature, float upperTemperature, float upperAirMass, float cloudElevation, ref float newSurfaceWater, ref float newCloudMass, ref float newCloudEnergy)
		{
			float nightlyLowerTemperatureDelta = 20;
			float nightlyUpperTemperatureDelta = 7;
			float temperatureAtCloudElevation = GetTemperatureAtElevation(world, cloudElevation, lowerTemperature - nightlyLowerTemperatureDelta, upperTemperature - nightlyUpperTemperatureDelta, elevationOrSeaLevel);
			float relativeHumidity = GetRelativeHumidity(world, temperatureAtCloudElevation, cloudMass, upperAirMass);
			if (relativeHumidity > 1.0f)
			{
				float rainfall = cloudMass * (relativeHumidity - 1) / relativeHumidity * world.Data.RainfallRate;
				newCloudMass -= rainfall;
				newCloudEnergy -= cloudEnergy * (rainfall / newCloudMass);
				float rainVolume = rainfall / world.Data.MassFreshWater;
				if (!isOcean)
				{
					newSurfaceWater += rainVolume;
				}
				return rainVolume;
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

		static private void SeepWaterIntoGround(World world, bool isOcean, float soilFertility, float waterTableDepth, ref float groundWater, ref float surfaceWater)
		{
			float maxGroundWater = soilFertility * waterTableDepth * world.Data.MaxSoilPorousness;
			if (!isOcean)
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

		static private float GetEvaporationRate(World world, float iceCoverage, float temperature, float relativeHumidity, float airPressureInverse)
		{
			float evapTemperature = Mathf.Clamp01((temperature - world.Data.EvapMinTemperature) / world.Data.evapTemperatureRange);

			return Mathf.Clamp01((1.0f - iceCoverage) * (1.0f - relativeHumidity) * Mathf.Pow(evapTemperature, 2)) * world.Data.EvaporationRate;
		}

		static public float GetTemperatureAtElevation(World world, float elevation, float lowerTemperature, float upperTemperature, float elevationOrSeaLevel)
		{
			float temperatureLapseRate = (upperTemperature - lowerTemperature) / (world.Data.troposphereElevation - elevationOrSeaLevel);
			return (elevation - elevationOrSeaLevel) * temperatureLapseRate + lowerTemperature;
		}

		static public float GetRelativeHumidity(World world, float temperature, float humidity, float airMass)
		{
			float maxWaterVaporPerKilogramAir = world.Data.WaterVaporMassToAirMassAtDewPoint * Mathf.Pow(Mathf.Max(0, (temperature - world.Data.DewPointZero) / world.Data.DewPointTemperatureRange), 2);
			float maxHumidity = maxWaterVaporPerKilogramAir * airMass;
			if (maxHumidity <= 0)
			{
				return humidity > 0 ? 10000 : 0;
			}
			float relativeHumidity = humidity / maxHumidity;
			return relativeHumidity;
		}

		static private void EvaporateWater(World world, bool isOcean, float evapRate, float groundWater, float waterTableDepth, ref float humidity, ref float newAirEnergy, ref float newOceanEnergy, ref float newGroundWater, ref float surfaceWater, out float evaporation)
		{
			evaporation = 0;
			if (evapRate <= 0)
			{
				return;
			}
			if (isOcean)
			{
				humidity += evapRate * world.Data.MassFreshWater;
				evaporation += evapRate;
			}
			else
			{
				if (surfaceWater > 0)
				{
					float waterSurfaceArea = Math.Min(1.0f, (float)Math.Sqrt(surfaceWater));
					float evap = Math.Max(0, Math.Min(surfaceWater, waterSurfaceArea * evapRate));
					surfaceWater -= evap;
					humidity += evap * world.Data.MassFreshWater;
					evaporation += evap;
				}
				if (waterTableDepth > 0)
				{
					var groundWaterEvap = Math.Max(0, Math.Min(newGroundWater, groundWater / waterTableDepth * evapRate));
					newGroundWater -= groundWaterEvap;
					humidity += groundWaterEvap * world.Data.MassFreshWater;
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
				float nCloudContent = state.CloudMass[nIndex];
				float nElevationOrSeaLevel = Math.Max(state.SeaLevel, state.Elevation[nIndex]);
				float nUpperAtmosphereVolume = world.Data.troposphereElevation - (nElevationOrSeaLevel + world.Data.BoundaryZoneElevation);
				float nSurfaceWindSpeed = Mathf.Abs(nLowerWind.x) + Mathf.Abs(nLowerWind.y);
				float nUpperWindSpeed = Mathf.Abs(nUpperWind.x) + Mathf.Abs(nUpperWind.y);

				float lowerMassTransfer = 0;
				float upperMassTransfer = 0;
				float humidityTransfer = 0;
				float humidityMovement = nHumidity * Mathf.Min(world.Data.WindHumidityMovement, 1.0f / nSurfaceWindSpeed);
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
		static public void GetSunVector(World world, float planetTiltAngle, int ticks, float latitude, out float elevationAngle, out Vector3 sunVec)
		{
			float latitudeRadians = latitude * Mathf.PI / 2;
			float angleOfInclination = planetTiltAngle * (float)Math.Sin(Math.PI * 2 * (world.GetTimeOfYear(ticks) - 0.25f));

			//float timeOfDay = (-sunPhase + 0.5f) * Math.PI * 2;
			float timeOfDay = (float)0.0f;
			float azimuth = (float)Math.Atan2(Math.Sin(timeOfDay), Math.Cos(timeOfDay) * Math.Sin(latitudeRadians) - Math.Tan(angleOfInclination) * Math.Cos(latitudeRadians));
			elevationAngle = (float)Math.Asin((Math.Sin(latitudeRadians) * Math.Sin(angleOfInclination) + Math.Cos(latitudeRadians) * Math.Cos(angleOfInclination) * Math.Cos(timeOfDay)));

			float cosOfElevation = (float)Math.Cos(elevationAngle);
			sunVec = new Vector3((float)Math.Sin(azimuth) * cosOfElevation, (float)Math.Cos(azimuth) * cosOfElevation, (float)Math.Sin(elevationAngle));
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
		static public float GetAlbedo(float surfaceAlbedo, float slope)
		{
			return surfaceAlbedo + (1.0f - surfaceAlbedo) * slope;
		}
	}
}