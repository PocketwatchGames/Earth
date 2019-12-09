using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Unity.Profiling;
namespace Sim {
	static public class Atmosphere {

		static ProfilerMarker _ProfileAtmosphereTick = new ProfilerMarker("Atmosphere Tick");
		static ProfilerMarker _ProfileAtmosphereMoveH = new ProfilerMarker("Atmosphere Move H");
		static ProfilerMarker _ProfileAtmosphereMoveV = new ProfilerMarker("Atmosphere Move V");
		static ProfilerMarker _ProfileAtmosphereCloudMove = new ProfilerMarker("Cloud Move");
		static ProfilerMarker _ProfileAtmosphereMoveOcean = new ProfilerMarker("Atmosphere Move Ocean");
		static ProfilerMarker _ProfileAtmosphereEnergyBudget = new ProfilerMarker("Atmosphere Energy Budget");

		static public void Tick(World world, World.State state, World.State nextState)
		{
			_ProfileAtmosphereTick.Begin();

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
				//float angleFromSunToLatitudeAndAtmophereEdge = Mathf.Asin(state.PlanetRadius * Mathf.Sin(inverseSunAngle) / (state.PlanetRadius + world.Data.TropopauseElevation));
				//float angleFromPlanetCenterToLatitudeAndAtmosphereEdge = Mathf.PI - inverseSunAngle - angleFromSunToLatitudeAndAtmophereEdge;
				//float atmosphericDepthInMeters = Mathf.Sin(angleFromPlanetCenterToLatitudeAndAtmosphereEdge) * state.PlanetRadius / Mathf.Sin(angleFromSunToLatitudeAndAtmophereEdge);
				//float atmosphericDepth = Mathf.Max(1.0f, atmosphericDepthInMeters / world.Data.TropopauseElevation);

				float atmosphericDepth = 1.0f + sunVector.y;

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
					float rainDropMass = state.RainDropMass[index];
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
					float newRainDropMass = rainDropMass;
					float newSurfaceWater = surfaceWater;
					float newSurfaceIce = surfaceIce;
					float newRainfall = 0;
					float newRadiation = radiation;
					float newOceanEnergyDeep = oceanEnergyDeep;
					float newOceanEnergyShallow = oceanEnergyShallow;
					float newOceanSalinityDeep = oceanSalinityDeep;
					float newOceanSalinityShallow = oceanSalinityShallow;
					float massOfAtmosphericColumn = upperAirMass + lowerAirMass;
					float iceCoverage = Mathf.Min(1.0f, surfaceIce / world.Data.FullIceCoverage);

					_ProfileAtmosphereEnergyBudget.Begin();

					// TODO this should be using absolute pressure not barometric
					float inverseLowerAirPressure = 1.0f / lowerAirPressure; 
					bool isOcean = world.IsOcean(elevation, state.SeaLevel);

					float relativeHumidity = GetRelativeHumidity(world, lowerAirTemperature, humidity, lowerAirMass);
					float cloudOpacity = Math.Min(1.0f, cloudMass / world.Data.CloudMassFullAbsorption); // TODO: prob sqrt this and use a higher cap
					float groundWaterSaturation = Animals.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], soilFertility * world.Data.MaxSoilPorousness);
					float incomingRadiation = state.SolarRadiation * lengthOfDay*sunVector.z*sunVector.z;
					float energyAbsorbed = 0;

					if (isOcean)
					{
						globalOceanCoverage++;
					}
					globalEnergyIncoming += incomingRadiation;

					// TODO: reflect/absorb more in the atmosphere with a lower sun angle

					// reflect some rads off atmosphere and clouds
					// TODO: this process feels a little broken -- are we giving too much priority to reflecting/absorbing in certain layers?
					float energyReflectedAtmosphere = incomingRadiation * world.Data.AtmosphericHeatReflection;
					incomingRadiation -= energyReflectedAtmosphere;

					float cloudReflectionRate = world.Data.CloudIncomingReflectionRate * cloudOpacity;
					float energyReflectedClouds = incomingRadiation * cloudReflectionRate;
					incomingRadiation -= energyReflectedClouds;

					float cloudAbsorptionRate = world.Data.CloudIncomingAbsorptionRate * cloudOpacity;
					float absorbedByCloudsIncoming = incomingRadiation * cloudAbsorptionRate;
					incomingRadiation -= absorbedByCloudsIncoming;
					newUpperAirEnergy += absorbedByCloudsIncoming;

					float upperAtmosphereAbsorptionRate = world.Data.AtmosphericHeatAbsorption * (upperAirMass / massOfAtmosphericColumn);
					float absorbedByUpperAtmosphereIncoming = incomingRadiation * upperAtmosphereAbsorptionRate * atmosphericDepth;
					incomingRadiation -= absorbedByUpperAtmosphereIncoming;
					newUpperAirEnergy += absorbedByUpperAtmosphereIncoming;

					globalEnergyReflectedAtmosphere += energyReflectedAtmosphere + energyReflectedClouds;

					// Absorbed by atmosphere
					{
						// stratosphere accounts for about a quarter of atmospheric mass
						//	float absorbedByStratosphere = incomingRadiation * world.Data.AtmosphericHeatAbsorption * (state.StratosphereMass / massOfAtmosphericColumn);

						float lowerAtmosphereAbsorptionRate = world.Data.AtmosphericHeatAbsorption * (lowerAirMass / massOfAtmosphericColumn);
						float absorbedByLowerAtmosphereIncoming = incomingRadiation * lowerAtmosphereAbsorptionRate * atmosphericDepth;

						float humidityAbsorptionRate = Mathf.Min(0.1f, humidity * world.Data.HumidityHeatAbsorption);
						absorbedByLowerAtmosphereIncoming += incomingRadiation * humidityAbsorptionRate * atmosphericDepth;

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
						if (isOcean)
						{
							slopeAlbedo = Mathf.Pow(1.0f - Math.Max(0, sunVector.z), 9);
						}
						if (surfaceIce > 0)
						{
							energyReflected += incomingRadiation * iceCoverage * GetAlbedo(world.Data.AlbedoIce, slopeAlbedo);
						}
						if (isOcean)
						{
							energyReflected = incomingRadiation * GetAlbedo(world.Data.AlbedoWater, slopeAlbedo) * (1.0f - iceCoverage);
						}
						else
						{
							// reflect some incoming radiation
							float waterReflectivity = surfaceWater * world.Data.AlbedoWater;
							float soilReflectivity = GetAlbedo(world.Data.AlbedoLand - world.Data.AlbedoReductionSoilQuality * soilFertility, slopeAlbedo);
							float heatReflectedLand = canopy * world.Data.AlbedoFoliage + Math.Max(0, 1.0f - canopy) * (surfaceWater * GetAlbedo(world.Data.AlbedoWater, slopeAlbedo) + Math.Max(0, 1.0f - surfaceWater) * soilReflectivity);
							energyReflected += incomingRadiation * heatReflectedLand * (1.0f - iceCoverage);
						}
						incomingRadiation -= energyReflected;

						// TODO: do we absorb some of this energy on the way back out of the atmosphere?
	//					newLowerAirEnergy += energyReflected;
						globalEnergyReflectedSurface += energyReflected;
					}

					globalEnergyAbsorbedSurface += incomingRadiation;
					energyAbsorbed += incomingRadiation;

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
						if (iceCoverage < 1)
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
							if (iceCoverage < 1)
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


					// lose some energy to space
					//float cloudReflectionFactor = world.Data.cloudReflectionRate * cloudOpacity;
					//float humidityPercentage = humidity / atmosphereMass;
					//float heatLossFactor = (1.0f - world.Data.carbonDioxide * world.Data.heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
					//float loss = airEnergy * (1.0f - cloudReflectionFactor) * (world.Data.heatLoss * heatLossFactor * airPressureInverse);

					// absorb some outgoing energy in clouds
					float energyEmittedByUpperAtmosphere = world.Data.EnergyEmittedByUpperAtmosphere * upperAirEnergy;
					float outgoingEnergyAbsorbedByClouds = energyEmittedByUpperAtmosphere * world.Data.CloudOutgoingAbsorptionRate * cloudOpacity;
					energyEmittedByUpperAtmosphere -= outgoingEnergyAbsorbedByClouds;

					// emit some remaining atmospheric energy to space
					float energyLostToSpace = energyEmittedByUpperAtmosphere * Mathf.Max(1.0f, state.CarbonDioxide * (1.0f - world.Data.EnergyTrappedByGreenhouseGasses));
					energyLostToSpace += upperAirEnergy * world.Data.EnergyLostThroughAtmosphereWindow;
					newUpperAirEnergy -= energyLostToSpace;

					float lowerEnergyLostToAtmosphericWindow = lowerAirEnergy * world.Data.EnergyLostThroughAtmosphereWindow;
					energyLostToSpace += lowerEnergyLostToAtmosphericWindow;
					newLowerAirEnergy -= lowerEnergyLostToAtmosphericWindow;
					_ProfileAtmosphereEnergyBudget.End();


					_ProfileAtmosphereMoveV.Begin();
					MoveWaterVaporVertically(
						world,
						isOcean,
						surfaceIce,
						humidity,
						relativeHumidity,
						lowerWind,
						upperWind,
						lowerAirTemperature,
						upperAirTemperature,
						cloudMass,
						rainDropMass,
						ref newSurfaceWater,
						ref newSurfaceIce,
						ref newHumidity,
						ref newCloudMass,
						ref newRainfall,
						ref newRainDropMass);
					_ProfileAtmosphereMoveV.End();

					_ProfileAtmosphereMoveH.Begin();
					MoveAtmosphereOnWind(world, state, x, y, elevationOrSeaLevel, lowerAirEnergy, upperAirEnergy, lowerAirMass, upperAirMass, lowerWind, upperWind, humidity, rainDropMass, ref newLowerAirEnergy, ref newUpperAirEnergy, ref newLowerAirMass, ref newUpperAirMass, ref newHumidity, ref newRainDropMass);
					_ProfileAtmosphereMoveH.End();

					_ProfileAtmosphereCloudMove.Begin();
					{
						Vector2 newCloudPos = new Vector3(x, y, 0) + upperWind;
						newCloudPos.x = Mathf.Repeat(newCloudPos.x, world.Size);
						newCloudPos.y = Mathf.Clamp(newCloudPos.y, 0, world.Size - 1);
						int x0 = (int)newCloudPos.x;
						int y0 = (int)newCloudPos.y;
						int x1 = (x0 + 1) % world.Size;
						int y1 = Mathf.Min(y0 + 1, world.Size - 1);
						float xT = newCloudPos.x - x0;
						float yT = newCloudPos.y - y0;

						int i0 = world.GetIndex(x0, y0);
						int i1 = world.GetIndex(x1, y0);
						int i2 = world.GetIndex(x0, y1);
						int i3 = world.GetIndex(x1, y1);

						nextState.CloudMass[i0] += cloudMass * (1.0f - xT) * (1.0f - yT);
						nextState.CloudMass[i1] += cloudMass * (1.0f - xT) * yT;
						nextState.CloudMass[i2] += cloudMass * xT * (1.0f - yT);
						nextState.CloudMass[i3] += cloudMass * xT * yT;

						nextState.RainDropMass[i0] += rainDropMass * (1.0f - xT) * (1.0f - yT);
						nextState.RainDropMass[i1] += rainDropMass * (1.0f - xT) * yT;
						nextState.RainDropMass[i2] += rainDropMass * xT * (1.0f - yT);
						nextState.RainDropMass[i3] += rainDropMass * xT * yT;
					}
					_ProfileAtmosphereCloudMove.End();

					_ProfileAtmosphereMoveOcean.Begin();
					MoveOceanOnCurrent(world, state, x, y, elevation, surfaceIce, oceanEnergyShallow, oceanEnergyDeep, oceanSalinityShallow, oceanSalinityDeep, oceanTemperatureShallow, oceanTemperatureDeep, oceanDensity, currentShallow, currentDeep, ref newOceanEnergyShallow, ref newOceanEnergyDeep, ref newOceanSalinityShallow, ref newOceanSalinityDeep);
					_ProfileAtmosphereMoveOcean.End();

					FlowWater(world, state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
					SeepWaterIntoGround(world, isOcean, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);

					//if (float.IsNaN(newLowerAirEnergy) || float.IsNaN(newEvaporation) || float.IsNaN(newSurfaceWater) || float.IsNaN(newSurfaceIce) || float.IsNaN(newGroundWater) || float.IsNaN(newHumidity) || float.IsNaN(newCloudCover) || float.IsNaN(newCloudElevation) || float.IsNaN(newLowerAirMass))
					//{
					//	break;
					//}

					if (float.IsNaN(newLowerAirEnergy) || float.IsNaN(newUpperAirEnergy) || float.IsNaN(newLowerAirMass) || float.IsNaN(newUpperAirEnergy) || float.IsNaN(newHumidity))
					{
						return;
					}

					nextState.LowerAirEnergy[index] = Mathf.Max(0, newLowerAirEnergy);
					nextState.UpperAirEnergy[index] = Mathf.Max(0, newUpperAirEnergy);
					nextState.LowerAirMass[index] = Mathf.Max(0, newLowerAirMass);
					nextState.UpperAirMass[index] = Mathf.Max(0, newUpperAirMass);
					nextState.SurfaceWater[index] = Mathf.Max(0, newSurfaceWater);
					nextState.Ice[index] = Mathf.Max(0, newSurfaceIce);
					nextState.GroundWater[index] = Mathf.Max(0, newGroundWater);
					nextState.Humidity[index] = Mathf.Max(0, newHumidity);
					nextState.CloudMass[index] = Mathf.Max(0, newCloudMass);
					nextState.RainDropMass[index] = Mathf.Max(0, newRainDropMass);
					nextState.Radiation[index] = Mathf.Max(0, newRadiation);
					nextState.OceanTemperatureShallow[index] = Mathf.Max(0, GetWaterTemperature(world, newOceanEnergyShallow, Math.Max(0, world.Data.DeepOceanDepth - newSurfaceIce)));
					nextState.OceanEnergyShallow[index] = Mathf.Max(0, newOceanEnergyShallow);
					nextState.OceanEnergyDeep[index] = Mathf.Max(0, newOceanEnergyDeep);
					nextState.OceanSalinityDeep[index] = Mathf.Max(0, newOceanSalinityDeep);
					nextState.OceanSalinityShallow[index] = Mathf.Max(0, newOceanSalinityShallow);
					nextState.OceanDensityDeep[index] = Mathf.Max(0, GetOceanDensity(world, newOceanEnergyDeep, newOceanSalinityDeep, state.SeaLevel - elevation));
					float newLowerAirTemperature = Mathf.Max(0, GetAirTemperature(world, newLowerAirEnergy, newLowerAirMass));
					float newUpperAirTemperature = Mathf.Max(0, GetAirTemperature(world, newUpperAirEnergy, newUpperAirMass));
					nextState.LowerAirTemperature[index] = Mathf.Max(0, newLowerAirTemperature);
					nextState.UpperAirTemperature[index] = Mathf.Max(0, newUpperAirTemperature);
					nextState.LowerAirPressure[index] = Mathf.Max(0, GetAirPressure(world, newLowerAirMass + newUpperAirMass + state.StratosphereMass, elevationOrSeaLevel, lowerAirTemperature));
					nextState.UpperAirPressure[index] = Mathf.Max(0, GetAirPressure(world, newUpperAirMass + state.StratosphereMass, elevationOrSeaLevel + world.Data.BoundaryZoneElevation, upperAirTemperature));

					nextState.Evaporation[index] = evaporation;
					nextState.Rainfall[index] = newRainfall;
					nextState.EnergyAbsorbed[index] = energyAbsorbed;

					globalEnergyGained += energyAbsorbed;
					globalEnergy += newLowerAirEnergy + newUpperAirEnergy + newOceanEnergyDeep + newOceanEnergyShallow;
					globalEnergyLost += energyLostToSpace;

					atmosphericMass += newLowerAirMass + newUpperAirMass;

				}
			}

			//int cloudIterations = 1;
			//float[,] cloudMassTemp = new float[cloudIterations + 1, world.Size * world.Size];
			//float[,] rainDropMassTemp = new float[cloudIterations + 1, world.Size * world.Size];
			//int curI = 0;
			//int nextI = 1;
			//for (int i = 0; i < world.Size* world.Size; i++)
			//{
			//	cloudMassTemp[0, i] = nextState.CloudMass[i];
			//	rainDropMassTemp[0, i] = nextState.RainDropMass[i];
			//}
			//for (int t = 0; t < cloudIterations; t++)
			//{
			//	float dt = world.Data.SecondsPerTick / (world.Data.tileSize * cloudIterations);
			//	for (int y = 0; y < world.Size; y++)
			//	{
			//		for (int x = 0; x < world.Size; x++)
			//		{
			//			int index = world.GetIndex(x, y);
			//			var upperWind = state.UpperWind[index];
			//			var cloudMass = cloudMassTemp[curI, index];
			//			var rainDropMass = rainDropMassTemp[curI, index];

			//			Vector2 newCloudPos = new Vector3(x, y, 0) + upperWind * dt;
			//			newCloudPos.x = Mathf.Repeat(newCloudPos.x, world.Size);
			//			newCloudPos.y = Mathf.Clamp(newCloudPos.y, 0, world.Size - 1);
			//			int x0 = (int)newCloudPos.x;
			//			int y0 = (int)newCloudPos.y;
			//			int x1 = (x0 + 1) % world.Size;
			//			int y1 = Mathf.Min(y0 + 1, world.Size - 1);
			//			float xT = newCloudPos.x - x0;
			//			float yT = newCloudPos.y - y0;

			//			int i0 = world.GetIndex(x0, y0);
			//			int i1 = world.GetIndex(x1, y0);
			//			int i2 = world.GetIndex(x0, y1);
			//			int i3 = world.GetIndex(x1, y1);

			//			cloudMassTemp[nextI, i0] += cloudMass * (1.0f - xT) * (1.0f - yT);
			//			cloudMassTemp[nextI, i1] += cloudMass * (1.0f - xT) * yT;
			//			cloudMassTemp[nextI, i2] += cloudMass * xT * (1.0f - yT);
			//			cloudMassTemp[nextI, i3] += cloudMass * xT * yT;

			//			rainDropMassTemp[nextI, i0] += rainDropMass * (1.0f - xT) * (1.0f - yT);
			//			rainDropMassTemp[nextI, i1] += rainDropMass * (1.0f - xT) * yT;
			//			rainDropMassTemp[nextI, i2] += rainDropMass * xT * (1.0f - yT);
			//			rainDropMassTemp[nextI, i3] += rainDropMass * xT * yT;

			//		}
			//	}
			//	curI++;
			//	nextI++;
			//}
			//for (int i=0;i<world.Size*world.Size;i++)
			//{
			//	nextState.CloudMass[i] = cloudMassTemp[curI, i];
			//}

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

			_ProfileAtmosphereTick.End();
		}

		static public float GetAirPressure(World world, float mass, float elevation, float temperature)
		{
			float temperatureLapse = -world.Data.TemperatureLapseRate * elevation;
			float pressure = mass * world.Data.GravitationalAcceleration * Mathf.Pow(1.0f -(temperatureLapse) / (temperature + temperatureLapse), world.Data.PressureExponent);
			return pressure;
		}

		static public float GetAirMass(World world, float pressure, float elevation, float temperature)
		{
			float temperatureLapse = -world.Data.TemperatureLapseRate * elevation;
			float mass = pressure / (world.Data.GravitationalAcceleration * Mathf.Pow(1.0f - (temperatureLapse) / (temperature + temperatureLapse), world.Data.PressureExponent));
			return mass;
		}

		static public float GetAirDensity(World world, float absolutePressure, float temperature)
		{
			return absolutePressure / (world.Data.SpecificGasConstantDryAir * temperature);
		}

		static public float GetOceanDensity(World world, float oceanEnergy, float oceanSalinity, float volume)
		{
			if (volume <= 0)
			{
				return 0;
			}
			return 1 + (world.Data.OceanDensityPerSalinity * oceanSalinity / volume + world.Data.OceanDensityPerDegree * GetWaterTemperature(world, oceanEnergy, volume));
		}


		static public float GetPressureAtElevation(World world, World.State state, int index, float elevation)
		{
			// Units: Pascals
			// Barometric Formula
			// Pressure = StaticPressure * (StdTemp / (StdTemp + StdTempLapseRate * (Elevation - ElevationAtBottomOfAtmLayer)) ^ (GravitationalAcceleration * MolarMassOfEarthAir / (UniversalGasConstant * StdTempLapseRate))
			// https://en.wikipedia.org/wiki/Barometric_formula
			// For the bottom layer of atmosphere ( < 11000 meters), ElevationAtBottomOfAtmLayer == 0)

			//	float standardPressure = Data.StaticPressure * (float)Math.Pow(Data.StdTemp / (Data.StdTemp + Data.StdTempLapseRate * elevation), Data.PressureExponent);
			float pressure = world.Data.StaticPressure * (float)Math.Pow(world.Data.StdTemp / (world.Data.StdTemp + world.Data.TemperatureLapseRate * elevation), world.Data.PressureExponent);
			return pressure;
		}




		static private void MoveWaterVaporVertically(World world, bool isOcean, float ice, float humidity, float relativeHumidity, Vector3 lowerWind, Vector3 upperWind, float lowerAirTemperature, float upperAirTemperature, float cloudMass, float rainDropMass, ref float newSurfaceWater, ref float newIce, ref float newHumidity, ref float newCloudMass, ref float newRainfall, ref float newRainDropMass)
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


			float dewPointElevationPerDegree = 67.73f;
			float dewPointTemperaturePerRelativeHumidity = 20;
			float dewPoint = lowerAirTemperature - (1.0f - relativeHumidity) * dewPointTemperaturePerRelativeHumidity;
			float cloudElevation = world.Data.BoundaryZoneElevation + (upperAirTemperature - dewPoint) * dewPointElevationPerDegree;

			float humidityToCloud = newHumidity * Mathf.Clamp01(lowerWind.z / cloudElevation);
			if (humidityToCloud > 0) {
				newCloudMass += humidityToCloud;
				newHumidity -= humidityToCloud;
			}

			if (cloudMass > 0)
			{
				float rainDropSize = Mathf.Sqrt(newRainDropMass) / 100;
				newRainDropMass = Mathf.Max(0, newRainDropMass + cloudMass * (world.Data.RainDropFormationSpeedTemperature / dewPoint - upperWind.magnitude * world.Data.RainDropDissapationSpeedWind));

				if (newRainDropMass > 0)
				{
					float rainDropDragCoefficient = 0.5f;
					float airDensity = 1.21f;
					float waterDensity = 997;
					float newRainDropSurfaceArea = Mathf.Pow(rainDropSize / waterDensity, 0.667f);
					float rainDropDrag = 1.0f - newRainDropSurfaceArea * rainDropDragCoefficient;
					float rainDropVelocity = lowerWind.z - Mathf.Sqrt(2 * rainDropSize * world.Data.GravitationalAcceleration / (airDensity * newRainDropSurfaceArea * rainDropDragCoefficient));
					if (rainDropVelocity < 0)
					{
						float rainDropEvapRate = 0.000001f;
						float rainfallMass = newRainDropMass * Mathf.Max(0, -rainDropVelocity * world.Data.RainfallRate);
						float rainDropFallTime = -cloudElevation / rainDropVelocity;
						float rainDropMassToHumidity = Mathf.Min(rainfallMass, rainDropFallTime * rainDropEvapRate);
						newCloudMass -= rainfallMass;
						newRainDropMass -= rainfallMass;
						newHumidity += rainDropMassToHumidity;
						if (rainfallMass > rainDropMassToHumidity)
						{
							newRainfall = (rainfallMass - rainDropMassToHumidity) / world.Data.MassFreshWater;
							if (!isOcean)
							{
								newSurfaceWater += newRainfall;
							}
						}
					}
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
			float temperatureLapseRate = (upperTemperature - lowerTemperature) / world.Data.BoundaryZoneElevation;
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

		static private void MoveAtmosphereOnWind(World world, World.State state, int x, int y, float elevationOrSeaLevel, float lowerEnergy, float upperEnergy, float lowerMass, float upperMass, Vector3 lowerWind, Vector3 upperWind, float humidity, float rainDropMass, ref float newLowerEnergy, ref float newUpperEnergy, ref float newLowerMass, ref float newUpperMass, ref float newHumidity, ref float newRainDropMass)
		{
			float maxLostToVertical = 0.1f;
			float maxLostToDiffusion = 0.5f;
			float maxLostToWind = 0.4f;

			float totalUpperWind = Mathf.Abs(upperWind.x) + Mathf.Abs(upperWind.y);
			float totalLowerWind = Mathf.Abs(lowerWind.x) + Mathf.Abs(lowerWind.y);
			float upperMassLeaving = Mathf.Min(maxLostToWind, totalUpperWind * world.Data.WindAirMovementHorizontal) * upperMass;
			float lowerMassLeaving = Mathf.Min(maxLostToWind, totalLowerWind * world.Data.WindAirMovementHorizontal) * lowerMass;
			newUpperMass -= upperMassLeaving;
			newLowerMass -= lowerMassLeaving;
			float upperMassPercentLeaving = upperMassLeaving / upperMass;
			newUpperEnergy -= upperMassPercentLeaving * upperEnergy;
			newLowerEnergy -= upperMassPercentLeaving * lowerEnergy;
			newHumidity -= upperMassPercentLeaving * humidity;

			float verticalMassTransfer;
			float verticalEnergyTransfer;
			float massTransferSpeed = Mathf.Clamp(lowerWind.z * world.Data.WindAirMovementVertical, -maxLostToVertical, maxLostToVertical);
			if (lowerWind.z > 0)
			{
				verticalMassTransfer = massTransferSpeed * lowerMass;
				verticalEnergyTransfer = verticalMassTransfer / lowerMass * lowerEnergy;
			} else
			{
				verticalMassTransfer = massTransferSpeed * upperMass;
				verticalEnergyTransfer = verticalMassTransfer / upperMass * upperEnergy;
			}
			newUpperMass += verticalMassTransfer;
			newLowerMass -= verticalMassTransfer;
			newUpperEnergy += verticalEnergyTransfer;
			newLowerEnergy -= verticalEnergyTransfer;

			for (int i = 0; i < 4; i++)
			{
				var nIndex = world.GetNeighborIndex(x, y, i);
				var nUpperWind = state.UpperWind[nIndex];
				var nLowerWind = state.LowerWind[nIndex];
				float nUpperEnergy = state.UpperAirEnergy[nIndex];
				float nUpperMass = state.UpperAirMass[nIndex];
				float nLowerEnergy = state.LowerAirEnergy[nIndex];
				float nLowerMass = state.LowerAirMass[nIndex];
				float nHumidity = state.Humidity[nIndex];
				float nCloudContent = state.CloudMass[nIndex];
				float nRainDropMass = state.RainDropMass[nIndex];

				float lowerMassTransfer = 0;
				float upperMassTransfer = 0;

				// TODO: make air diffuse faster at low density
				// Mixing Upper atmosphere
				{
					float massDiffusionSpeed = Mathf.Clamp(world.Data.AirDiffusionSpeed * (state.UpperAirPressure[nIndex] - state.UpperAirPressure[world.GetIndex(x, y)]) / world.Data.StaticPressure, -maxLostToDiffusion, maxLostToDiffusion);
					upperMassTransfer = massDiffusionSpeed * Mathf.Min(upperMass, nUpperMass);
				}

				// mixing lower atmosphere
				{
					float massDiffusionSpeed = Mathf.Clamp(world.Data.AirDiffusionSpeed * (state.LowerAirPressure[nIndex] - state.LowerAirPressure[world.GetIndex(x, y)]) / world.Data.StaticPressure, -maxLostToDiffusion, maxLostToDiffusion);
					lowerMassTransfer = massDiffusionSpeed * Mathf.Min(lowerMass, nLowerMass);
				}

				// Blowing on wind
				{
					float upperWindSpeed = Mathf.Abs(nUpperWind.x) + Mathf.Abs(nUpperWind.y);
					float desiredUpperMassTransfer = upperWindSpeed * world.Data.WindAirMovementHorizontal;
					float clampedUpperMassTransfer = Mathf.Min(desiredUpperMassTransfer, maxLostToWind);
					float clampedUpperWindDivisor = clampedUpperMassTransfer / upperWindSpeed;

					float lowerWindSpeed = Mathf.Abs(nLowerWind.x) + Mathf.Abs(nLowerWind.y);
					float desiredLowerMassTransfer = lowerWindSpeed * world.Data.WindAirMovementHorizontal;
					float clampedLowerMassTransfer = Mathf.Min(desiredLowerMassTransfer, maxLostToWind);
					float clampedLowerWindDivisor = clampedLowerMassTransfer / lowerWindSpeed;

					switch (i)
					{
						case (int)Direction.West:
							if (nUpperWind.x > 0)
							{
								upperMassTransfer += nUpperMass * nUpperWind.x * clampedUpperWindDivisor;
							}
							if (nLowerWind.x > 0)
							{
								lowerMassTransfer += nLowerMass * nLowerWind.x * clampedLowerWindDivisor;
							}
							break;
						case (int)Direction.East:
							if (nUpperWind.x < 0)
							{
								upperMassTransfer += nUpperMass * -nUpperWind.x * clampedUpperWindDivisor;
							}
							if (nLowerWind.x < 0)
							{
								lowerMassTransfer += nLowerMass * -nLowerWind.x * clampedLowerWindDivisor;
							}
							break;
						case (int)Direction.South:
							if (nUpperWind.y < 0)
							{
								upperMassTransfer += nUpperMass * -nUpperWind.y * clampedUpperWindDivisor;
							}
							if (nLowerWind.y < 0)
							{
								lowerMassTransfer += nLowerMass * -nLowerWind.y * clampedLowerWindDivisor;
							}
							break;
						case (int)Direction.North:
							if (nUpperWind.y > 0)
							{
								upperMassTransfer += nUpperMass * nUpperWind.y * clampedUpperWindDivisor;
							}
							if (nLowerWind.y > 0)
							{
								lowerMassTransfer += nLowerMass * nLowerWind.y * clampedLowerWindDivisor;
							}
							break;
					}
				}
				// TODO: this isn't mass conserving since neighbors won't be taking this minimum into account
				newUpperMass = Mathf.Max(0, newUpperMass + upperMassTransfer);
				if (upperMassTransfer > 0)
				{
					if (nUpperMass > 0)
					{
						float upperMassTransferPercent = upperMassTransfer / nUpperMass;
						newUpperEnergy += nUpperEnergy * upperMassTransferPercent;
						newRainDropMass += nRainDropMass * upperMassTransferPercent;
					}
				}
				else
				{
					if (upperMass > 0)
					{
						float upperMassTransferPercent = upperMassTransfer / upperMass;
						newUpperEnergy += upperEnergy * upperMassTransferPercent;
						newRainDropMass += rainDropMass * upperMassTransferPercent;
					}
				}

				newLowerMass = newLowerMass + lowerMassTransfer;
				if (lowerMassTransfer > 0)
				{
					if (nLowerMass > 0)
					{
						float lowerMassTransferPercent = lowerMassTransfer / nLowerMass;
						newLowerEnergy += nLowerEnergy * lowerMassTransferPercent;
						newHumidity += nHumidity * lowerMassTransferPercent;
					}
				}
				else
				{
					if (lowerMass > 0)
					{
						float lowerMassTransferPercent = lowerMassTransfer / lowerMass;
						newLowerEnergy += lowerEnergy * lowerMassTransferPercent;
						newHumidity += humidity * lowerMassTransferPercent;
					}
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

				newOceanEnergyShallow -= oceanEnergyShallow * (Mathf.Abs(currentShallow.x) + Mathf.Abs(currentShallow.y)) * world.Data.OceanCurrentSpeed;
				newOceanSalinityShallow -= oceanSalinityShallow * (Mathf.Abs(currentShallow.x) + Mathf.Abs(currentShallow.y)) * world.Data.OceanCurrentSpeed;

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
				var nIndex = world.GetNeighborIndex(x, y, i);
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
								float absX = neighborCurrentShallow.x;
								newOceanEnergyShallow += nEnergyShallow * absX * world.Data.OceanCurrentSpeed;
								newOceanSalinityShallow += nSalinityShallow * absX * world.Data.OceanCurrentSpeed;
							}
							if (currentShallow.x < 0)
							{
								float absX = -currentShallow.x;
								newOceanEnergyShallow -= oceanEnergyShallow * absX * world.Data.OceanCurrentSpeed;
								newOceanSalinityShallow -= oceanSalinityShallow * absX * world.Data.OceanCurrentSpeed;
							}
							if (neighborCurrentDeep.x > 0)
							{
								float absX = neighborCurrentDeep.x;
								newOceanEnergyDeep += nEnergyDeep * absX * world.Data.OceanCurrentSpeed;
								newOceanSalinityDeep += nSalinityDeep * absX * world.Data.OceanCurrentSpeed;
							}
							if (currentDeep.x < 0)
							{
								float absX = -currentDeep.x;
								newOceanEnergyDeep -= oceanEnergyDeep * absX * world.Data.OceanCurrentSpeed;
								newOceanSalinityDeep -= oceanSalinityDeep * absX * world.Data.OceanCurrentSpeed;
							}
							break;
						case 1:
							if (neighborCurrentShallow.x < 0)
							{
								float absX = -neighborCurrentShallow.x;
								newOceanEnergyShallow += nEnergyShallow * absX * world.Data.OceanCurrentSpeed;
								newOceanSalinityShallow += nSalinityShallow * absX * world.Data.OceanCurrentSpeed;
							}
							if (currentShallow.x > 0)
							{
								float absX = currentShallow.x;
								newOceanEnergyShallow -= oceanEnergyShallow * absX * world.Data.OceanCurrentSpeed;
								newOceanSalinityShallow -= oceanSalinityShallow * absX * world.Data.OceanCurrentSpeed;
							}
							if (neighborCurrentDeep.x < 0)
							{
								float absX = -neighborCurrentDeep.x;
								newOceanEnergyDeep += nEnergyDeep * absX * world.Data.OceanCurrentSpeed;
								newOceanSalinityDeep += nSalinityDeep * absX * world.Data.OceanCurrentSpeed;
							}
							if (currentDeep.x > 0)
							{
								float absX = currentDeep.x;
								newOceanEnergyDeep -= oceanEnergyDeep * absX * world.Data.OceanCurrentSpeed;
								newOceanSalinityDeep -= oceanSalinityDeep * absX * world.Data.OceanCurrentSpeed;
							}
							break;
						case 2:
							if (neighborCurrentShallow.y < 0)
							{
								float absY = -neighborCurrentShallow.y;
								newOceanEnergyShallow += nEnergyShallow * absY * world.Data.OceanCurrentSpeed;
								newOceanSalinityShallow += nSalinityShallow * absY * world.Data.OceanCurrentSpeed;
							}
							if (currentShallow.y > 0)
							{
								float absY = currentShallow.y;
								newOceanEnergyShallow -= oceanEnergyShallow * absY * world.Data.OceanCurrentSpeed;
								newOceanSalinityShallow -= oceanSalinityShallow * absY * world.Data.OceanCurrentSpeed;
							}
							if (neighborCurrentDeep.y < 0)
							{
								float absY = -neighborCurrentDeep.y;
								newOceanEnergyDeep += nEnergyDeep * absY * world.Data.OceanCurrentSpeed;
								newOceanSalinityDeep += nSalinityDeep * absY * world.Data.OceanCurrentSpeed;
							}
							if (currentDeep.y > 0)
							{
								float absY = currentDeep.y;
								newOceanEnergyDeep -= oceanEnergyDeep * absY * world.Data.OceanCurrentSpeed;
								newOceanSalinityDeep -= oceanSalinityDeep * absY * world.Data.OceanCurrentSpeed;
							}
							break;
						case 3:
							if (neighborCurrentShallow.y > 0)
							{
								float absY = neighborCurrentShallow.y;
								newOceanEnergyShallow += nEnergyShallow * absY * world.Data.OceanCurrentSpeed;
								newOceanSalinityShallow += nSalinityShallow * absY * world.Data.OceanCurrentSpeed;
							}
							if (currentShallow.y < 0)
							{
								float absY = -currentShallow.y;
								newOceanEnergyShallow -= oceanEnergyShallow * absY * world.Data.OceanCurrentSpeed;
								newOceanSalinityShallow -= oceanSalinityShallow * absY * world.Data.OceanCurrentSpeed;
							}
							if (neighborCurrentDeep.y > 0)
							{
								float absY = neighborCurrentDeep.y;
								newOceanEnergyDeep += nEnergyDeep * absY * world.Data.OceanCurrentSpeed;
								newOceanSalinityDeep += nSalinityDeep * absY * world.Data.OceanCurrentSpeed;
							}
							if (currentDeep.y < 0)
							{
								float absY = -currentDeep.y;
								newOceanEnergyDeep -= oceanEnergyDeep * absY * world.Data.OceanCurrentSpeed;
								newOceanSalinityDeep -= oceanSalinityDeep * absY * world.Data.OceanCurrentSpeed;
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
		static public float GetAlbedo(float surfaceAlbedo, float slope)
		{
			return surfaceAlbedo + (1.0f - surfaceAlbedo) * slope;
		}
	}
}