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

			float metersPerSecondToTilesPerTick = world.Data.SecondsPerTick / world.Data.tileSize;
			float wattsToKJPerTick = world.Data.SecondsPerTick * 1000;
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
				float lengthOfDay = GetLengthOfDay(latitude, timeOfYear, declinationOfSun);

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
					float ice = state.Ice[index];
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
					float newSurfaceWater = surfaceWater;
					float newIce = ice;
					float newRainfall = 0;
					float newRadiation = radiation;
					float massOfAtmosphericColumn = upperAirMass + lowerAirMass;
					float iceCoverage = Mathf.Min(1.0f, ice / world.Data.FullIceCoverage);
					float relativeHumidity = GetRelativeHumidity(world, lowerAirTemperature, humidity, lowerAirMass);
					float dewPoint = lowerAirTemperature - (1.0f - relativeHumidity) * world.Data.DewPointTemperaturePerRelativeHumidity;
					float cloudElevation = world.Data.BoundaryZoneElevation + (upperAirTemperature - dewPoint) * world.Data.DewPointElevationPerDegree;

					float longitude = (float)x / world.Size;
					float sunAngle;
					Vector3 sunVector;
					GetSunVector(world, state.PlanetTiltAngle, state.Ticks, latitude, longitude, out sunAngle, out sunVector);
					sunAngle = Math.Max(0, sunAngle);

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



					float newLowerAirEnergy = 0;
					float newUpperAirEnergy = 0;
					float newHumidity = 0;
					float newCloudMass = 0;
					float newRainDropMass = 0;
					float newOceanEnergyShallow = 0;
					float newOceanEnergyDeep = 0;
					float newOceanSalinityShallow = 0;
					float newOceanSalinityDeep = 0;

					_ProfileAtmosphereMoveH.Begin();
					{
						// Upper atmosphere

						Vector2 movePos = new Vector3(x, y, 0) + upperWind * metersPerSecondToTilesPerTick;
						movePos.x = Mathf.Repeat(movePos.x, world.Size);
						movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
						int x0 = (int)movePos.x;
						int y0 = (int)movePos.y;
						int x1 = (x0 + 1) % world.Size;
						int y1 = Mathf.Min(y0 + 1, world.Size - 1);
						float xT = movePos.x - x0;
						float yT = movePos.y - y0;
						float zT = Mathf.Clamp01(-lowerWind.z * world.Data.SecondsPerTick / world.Data.BoundaryZoneElevation);
						float zTComplement = 1.0f - zT;

						int i0 = world.GetIndex(x0, y0);
						int i1 = world.GetIndex(x1, y0);
						int i2 = world.GetIndex(x0, y1);
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

						float contentMove = upperAirMass * world.Data.WindAirMovement;
						nextState.UpperAirMass[index] += upperAirMass * (1.0f - world.Data.WindAirMovement);
						nextState.UpperAirMass[i0] += contentMove * move0 * zTComplement;
						nextState.UpperAirMass[i1] += contentMove * move1 * zTComplement;
						nextState.UpperAirMass[i2] += contentMove * move2 * zTComplement;
						nextState.UpperAirMass[i3] += contentMove * move3 * zTComplement;
						if (zT > 0)
						{
							nextState.LowerAirMass[index] += contentMove * zT;
						}

						contentMove = upperAirEnergy * world.Data.WindAirMovement;
						nextState.UpperAirEnergy[index] += upperAirEnergy * (1.0f - world.Data.WindAirMovement);
						nextState.UpperAirEnergy[i0] += contentMove * move0 * zTComplement;
						nextState.UpperAirEnergy[i1] += contentMove * move1 * zTComplement;
						nextState.UpperAirEnergy[i2] += contentMove * move2 * zTComplement;
						nextState.UpperAirEnergy[i3] += contentMove * move3 * zTComplement;
						if (zT > 0)
						{
							nextState.LowerAirEnergy[index] += contentMove * zT;
						}

						// lower atmosphere
						movePos = new Vector3(x, y, 0) + lowerWind * metersPerSecondToTilesPerTick;
						movePos.x = Mathf.Repeat(movePos.x, world.Size);
						movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
						x0 = (int)movePos.x;
						y0 = (int)movePos.y;
						x1 = (x0 + 1) % world.Size;
						y1 = Mathf.Min(y0 + 1, world.Size - 1);
						xT = movePos.x - x0;
						yT = movePos.y - y0;
						zT = Mathf.Clamp01(lowerWind.z * world.Data.SecondsPerTick / world.Data.BoundaryZoneElevation);
						zTComplement = 1.0f - zT;

						i0 = world.GetIndex(x0, y0);
						i1 = world.GetIndex(x1, y0);
						i2 = world.GetIndex(x0, y1);
						i3 = world.GetIndex(x1, y1);

						move0 = (1.0f - xT) * (1.0f - yT);
						move1 = (1.0f - xT) * yT;
						move2 = xT * (1.0f - yT);
						move3 = xT * yT;

						contentMove = lowerAirMass * world.Data.WindAirMovement;
						nextState.LowerAirMass[index] += lowerAirMass * (1.0f - world.Data.WindAirMovement);
						nextState.LowerAirMass[i0] += contentMove * move0 * zTComplement;
						nextState.LowerAirMass[i1] += contentMove * move1 * zTComplement;
						nextState.LowerAirMass[i2] += contentMove * move2 * zTComplement;
						nextState.LowerAirMass[i3] += contentMove * move3 * zTComplement;
						if (zT > 0)
						{
							nextState.UpperAirMass[index] += contentMove * zT;
						}

						contentMove = lowerAirEnergy * world.Data.WindAirMovement;
						nextState.LowerAirEnergy[index] += lowerAirEnergy * (1.0f - world.Data.WindAirMovement);
						nextState.LowerAirEnergy[i0] += contentMove * move0 * zTComplement;
						nextState.LowerAirEnergy[i1] += contentMove * move1 * zTComplement;
						nextState.LowerAirEnergy[i2] += contentMove * move2 * zTComplement;
						nextState.LowerAirEnergy[i3] += contentMove * move3 * zTComplement;
						if (zT > 0)
						{
							nextState.UpperAirEnergy[index] += contentMove * zT;
						}

						float humidityZT = Mathf.Clamp01(lowerWind.z / cloudElevation);
						float humidityZTComplement = 1.0f - humidityZT;

						contentMove = humidity * world.Data.WindAirMovement;
						nextState.Humidity[index] += humidity * (1.0f - world.Data.WindAirMovement);
						nextState.Humidity[i0] += contentMove * move0 * humidityZTComplement;
						nextState.Humidity[i1] += contentMove * move1 * humidityZTComplement;
						nextState.Humidity[i2] += contentMove * move2 * humidityZTComplement;
						nextState.Humidity[i3] += contentMove * move3 * humidityZTComplement;
						if (humidityZT > 0)
						{
							nextState.CloudMass[index] += contentMove * humidityZT;
						}



						//surface ocean
						movePos = new Vector3(x, y, 0) + currentShallow * metersPerSecondToTilesPerTick;
						movePos.x = Mathf.Repeat(movePos.x, world.Size);
						movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
						x0 = (int)movePos.x;
						y0 = (int)movePos.y;
						x1 = (x0 + 1) % world.Size;
						y1 = Mathf.Min(y0 + 1, world.Size - 1);
						xT = movePos.x - x0;
						yT = movePos.y - y0;

						i0 = world.GetIndex(x0, y0);
						i1 = world.GetIndex(x1, y0);
						i2 = world.GetIndex(x0, y1);
						i3 = world.GetIndex(x1, y1);
						if (!world.IsOcean(state.Elevation[i0], state.SeaLevel)) i0 = index;
						if (!world.IsOcean(state.Elevation[i1], state.SeaLevel)) i1 = index;
						if (!world.IsOcean(state.Elevation[i2], state.SeaLevel)) i2 = index;
						if (!world.IsOcean(state.Elevation[i3], state.SeaLevel)) i3 = index;

						move0 = (1.0f - xT) * (1.0f - yT);
						move1 = (1.0f - xT) * yT;
						move2 = xT * (1.0f - yT);
						move3 = xT * yT;

						contentMove = oceanEnergyShallow * world.Data.OceanCurrentSpeed;
						nextState.OceanEnergyShallow[index] += oceanEnergyShallow * (1.0f - world.Data.OceanCurrentSpeed);
						nextState.OceanEnergyShallow[i0] += contentMove * move0;
						nextState.OceanEnergyShallow[i1] += contentMove * move1;
						nextState.OceanEnergyShallow[i2] += contentMove * move2;
						nextState.OceanEnergyShallow[i3] += contentMove * move3;

						contentMove = oceanSalinityShallow * world.Data.OceanCurrentSpeed;
						nextState.OceanSalinityShallow[index] += oceanSalinityShallow * (1.0f - world.Data.OceanCurrentSpeed);
						nextState.OceanSalinityShallow[i0] += contentMove * move0;
						nextState.OceanSalinityShallow[i1] += contentMove * move1;
						nextState.OceanSalinityShallow[i2] += contentMove * move2;
						nextState.OceanSalinityShallow[i3] += contentMove * move3;


						//deep ocean
						movePos = new Vector3(x, y, 0) + currentDeep * metersPerSecondToTilesPerTick;
						movePos.x = Mathf.Repeat(movePos.x, world.Size);
						movePos.y = Mathf.Clamp(movePos.y, 0, world.Size - 1);
						x0 = (int)movePos.x;
						y0 = (int)movePos.y;
						x1 = (x0 + 1) % world.Size;
						y1 = Mathf.Min(y0 + 1, world.Size - 1);
						xT = movePos.x - x0;
						yT = movePos.y - y0;

						i0 = world.GetIndex(x0, y0);
						i1 = world.GetIndex(x1, y0);
						i2 = world.GetIndex(x0, y1);
						i3 = world.GetIndex(x1, y1);
						if (!world.IsOcean(state.Elevation[i0], state.SeaLevel)) i0 = index;
						if (!world.IsOcean(state.Elevation[i1], state.SeaLevel)) i1 = index;
						if (!world.IsOcean(state.Elevation[i2], state.SeaLevel)) i2 = index;
						if (!world.IsOcean(state.Elevation[i3], state.SeaLevel)) i3 = index;

						move0 = (1.0f - xT) * (1.0f - yT);
						move1 = (1.0f - xT) * yT;
						move2 = xT * (1.0f - yT);
						move3 = xT * yT;

						contentMove = oceanEnergyDeep * world.Data.OceanCurrentSpeed;
						nextState.OceanEnergyDeep[index] += oceanEnergyDeep * (1.0f - world.Data.OceanCurrentSpeed);
						nextState.OceanEnergyDeep[i0] += contentMove * move0;
						nextState.OceanEnergyDeep[i1] += contentMove * move1;
						nextState.OceanEnergyDeep[i2] += contentMove * move2;
						nextState.OceanEnergyDeep[i3] += contentMove * move3;

						contentMove = oceanSalinityDeep * world.Data.OceanCurrentSpeed;
						nextState.OceanSalinityDeep[index] += oceanSalinityDeep * (1.0f - world.Data.OceanCurrentSpeed);
						nextState.OceanSalinityDeep[i0] += contentMove * move0;
						nextState.OceanSalinityDeep[i1] += contentMove * move1;
						nextState.OceanSalinityDeep[i2] += contentMove * move2;
						nextState.OceanSalinityDeep[i3] += contentMove * move3;

					}
					_ProfileAtmosphereMoveH.End();




					_ProfileAtmosphereEnergyBudget.Begin();

					// TODO this should be using absolute pressure not barometric
					float inverseLowerAirPressure = 1.0f / lowerAirPressure; 
					bool isOcean = world.IsOcean(elevation, state.SeaLevel);

					float cloudOpacity = Math.Min(1.0f, cloudMass / world.Data.CloudMassFullAbsorption); // TODO: prob sqrt this and use a higher cap
					float groundWaterSaturation = Animals.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], soilFertility * world.Data.MaxSoilPorousness);
					float incomingRadiation = state.SolarRadiation* world.Data.SecondsPerTick * Mathf.Pow(sunAngle*2/Mathf.PI,2);
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
						if (ice > 0)
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
						if (ice > 0)
						{
							float radiationAbsorbedByIce = incomingRadiation * iceCoverage;
							incomingRadiation -= radiationAbsorbedByIce;
							globalEnergyAbsorbedSurface += radiationAbsorbedByIce;

							// world.Data.SpecificHeatIce * world.Data.MassIce == KJ required to raise one cubic meter by 1 degree
							float meltRate = 1.0f / (world.Data.SpecificHeatIce * world.Data.MassIce);
							float iceTemp = lowerAirTemperature + radiationAbsorbedByIce * meltRate;
							if (iceTemp > world.Data.FreezingTemperature)
							{
								iceMelted += (iceTemp - world.Data.FreezingTemperature) * meltRate;
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
									newIce += iceFrozen;
									if (!isOcean)
									{
										// add to surface water
										newSurfaceWater -= iceFrozen;
									}
								}

								// evaporation
								float evapRate = GetEvaporationRate(world, ice, lowerAirTemperature, relativeHumidity, inverseLowerAirPressure);
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
							if (ice > 0 && oceanTemperatureShallow > world.Data.FreezingTemperature)
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
								float massOfShallowWaterColumn = Math.Max(0, world.Data.DeepOceanDepth - ice) * world.Data.MassSeaWater;
								float massToAchieveEquilibrium = oceanEnergyShallow / (world.Data.FreezingTemperature * world.Data.SpecificHeatSeaWater);
								float massFrozen = massOfShallowWaterColumn - massToAchieveEquilibrium;
								//newOceanEnergyShallow -= energyToAchieveEquilibrium;
								newIce += massToAchieveEquilibrium / world.Data.MassIce;
							}
						}
						else
						{
							newLowerAirEnergy += incomingRadiation;
						}
					}

					// reduce ice
					iceMelted = Math.Min(iceMelted, ice);
					if (iceMelted > 0)
					{
						newIce -= iceMelted;
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
						humidity,
						relativeHumidity,
						lowerWind,
						upperWind,
						lowerAirTemperature,
						cloudMass,
						rainDropMass,
						dewPoint,
						cloudElevation,
						ref newSurfaceWater,
						ref newIce,
						ref newHumidity,
						ref newCloudMass,
						ref newRainfall,
						ref newRainDropMass);
					_ProfileAtmosphereMoveV.End();


					_ProfileAtmosphereMoveOcean.Begin();
					MoveOceanOnCurrent(world, state, x, y, elevation, ice, oceanEnergyShallow, oceanEnergyDeep, oceanSalinityShallow, oceanSalinityDeep, oceanTemperatureShallow, oceanTemperatureDeep, oceanDensity, currentShallow, currentDeep, ref newOceanEnergyShallow, ref newOceanEnergyDeep, ref newOceanSalinityShallow, ref newOceanSalinityDeep);
					_ProfileAtmosphereMoveOcean.End();

					// Diffusion step
					float newUpperAirMass = 0;
					float newLowerAirMass = 0;
					float airDiffusionSpeedPerPressureDifferential = world.Data.AirDiffusionSpeed / world.Data.StaticPressure;
					for (int i = 0; i < 4; i++)
					{
						int nIndex = world.GetNeighborIndex(index, i);


						//TODO: make air diffuse faster at low density
						//Mixing Upper atmosphere
						{
							float massDiffusionSpeed = airDiffusionSpeedPerPressureDifferential * (state.UpperAirPressure[nIndex] - upperAirPressure);
							float massTransfer = massDiffusionSpeed * Mathf.Min(upperAirMass, state.UpperAirMass[nIndex]);
							newUpperAirMass += massTransfer;
							if (massTransfer < 0)
							{
								newUpperAirEnergy += upperAirEnergy * massTransfer / upperAirMass;
							} else
							{
								newUpperAirEnergy += state.UpperAirEnergy[nIndex] * massTransfer / upperAirMass;
							}
						}

						//mixing lower atmosphere
						{
							float massDiffusionSpeed = airDiffusionSpeedPerPressureDifferential * (state.LowerAirPressure[nIndex] - lowerAirPressure);
							float massTransfer = massDiffusionSpeed * Mathf.Min(lowerAirMass, state.LowerAirMass[nIndex]);
							newLowerAirMass += massTransfer;
							if (massTransfer < 0)
							{
								newLowerAirEnergy += lowerAirEnergy * massTransfer / lowerAirMass;
							}
							else
							{
								newLowerAirEnergy += state.LowerAirEnergy[nIndex] * massTransfer / lowerAirMass;
							}
						}

					}

					FlowWater(world, state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
					SeepWaterIntoGround(world, isOcean, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);


					if (float.IsNaN(nextState.LowerAirEnergy[index]) ||
						float.IsNaN(nextState.UpperAirEnergy[index]) ||
						float.IsNaN(nextState.LowerAirMass[index]) ||
						float.IsNaN(nextState.UpperAirMass[index]) ||
						float.IsNaN(nextState.Humidity[index]))
					{
						return;
					}

					nextState.LowerAirMass[index] += Mathf.Max(0, newLowerAirMass);
					nextState.UpperAirMass[index] += Mathf.Max(0, newUpperAirMass);
					nextState.LowerAirEnergy[index] += Mathf.Max(0, newLowerAirEnergy);
					nextState.UpperAirEnergy[index] += Mathf.Max(0, newUpperAirEnergy);
					nextState.Humidity[index] += Mathf.Max(0, newHumidity);
					nextState.CloudMass[index] += Mathf.Max(0, newCloudMass);
					nextState.RainDropMass[index] += Mathf.Max(0, newRainDropMass);
					nextState.OceanEnergyShallow[index] += Mathf.Max(0, newOceanEnergyShallow);
					nextState.OceanEnergyDeep[index] += Mathf.Max(0, newOceanEnergyDeep);
					nextState.OceanSalinityShallow[index] += Mathf.Max(0, newOceanSalinityShallow);
					nextState.OceanSalinityDeep[index] += Mathf.Max(0, newOceanSalinityDeep);


					nextState.SurfaceWater[index] = Mathf.Max(0, newSurfaceWater);
					nextState.Ice[index] = Mathf.Max(0, newIce);
					nextState.GroundWater[index] = Mathf.Max(0, newGroundWater);
					nextState.Radiation[index] = Mathf.Max(0, newRadiation);
					nextState.LowerAirTemperature[index] = GetAirTemperature(world, nextState.LowerAirEnergy[index], nextState.LowerAirMass[index]);
					nextState.LowerAirPressure[index] = Mathf.Max(0, GetAirPressure(world, nextState.LowerAirMass[index] + nextState.UpperAirMass[index] + state.StratosphereMass, elevationOrSeaLevel, nextState.LowerAirTemperature[index]));
					nextState.UpperAirTemperature[index] = GetAirTemperature(world, nextState.UpperAirEnergy[index], nextState.UpperAirMass[index]);
					nextState.UpperAirPressure[index] = Mathf.Max(0, GetAirPressure(world, nextState.UpperAirMass[index] + state.StratosphereMass, elevationOrSeaLevel + world.Data.BoundaryZoneElevation, nextState.UpperAirTemperature[index]));
					nextState.OceanTemperatureShallow[index] = Mathf.Max(0, GetWaterTemperature(world, nextState.OceanEnergyShallow[index], Math.Max(0, world.Data.DeepOceanDepth - newIce)));
					nextState.OceanDensityDeep[index] = Mathf.Max(0, GetOceanDensity(world, nextState.OceanEnergyDeep[index], nextState.OceanSalinityDeep[index], state.SeaLevel - elevation));

					if (float.IsInfinity(nextState.LowerAirPressure[index]))
					{
						Debug.Break();
					}

					nextState.Evaporation[index] = evaporation;
					nextState.Rainfall[index] = newRainfall;
					nextState.EnergyAbsorbed[index] = energyAbsorbed;

					globalEnergyGained += energyAbsorbed;
					globalEnergyLost += energyLostToSpace;
					globalEnergy += nextState.LowerAirEnergy[index] + nextState.UpperAirEnergy[index] + nextState.OceanEnergyDeep[index] + nextState.OceanEnergyShallow[index];
					atmosphericMass += nextState.LowerAirMass[index] + nextState.UpperAirMass[index];

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




		static private void MoveWaterVaporVertically(
			World world, 
			bool isOcean, 
			float humidity, 
			float relativeHumidity, 
			Vector3 lowerWind, 
			Vector3 upperWind, 
			float lowerAirTemperature, 
			float cloudMass, 
			float rainDropMass, 
			float dewPoint,
			float cloudElevation,
			ref float newSurfaceWater, 
			ref float newIce, 
			ref float newHumidity, 
			ref float newCloudMass, 
			ref float newRainfall, 
			ref float newRainDropMass)
		{


			// condensation
			if (relativeHumidity > 1)
			{
				float humidityToGround = humidity * (relativeHumidity - 1.0f) / relativeHumidity;
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


			if (cloudMass > 0)
			{
				newRainDropMass = Mathf.Max(0, newRainDropMass + cloudMass * (world.Data.RainDropFormationSpeedTemperature / dewPoint - upperWind.magnitude * world.Data.RainDropDissapationSpeedWind));

				if (newRainDropMass > 0)
				{
					// TODO: airDesntiy and rainDensity should probably be cleaned up (derived from other data?)
					float rainDropSize = Mathf.Sqrt(newRainDropMass) / 100;
					float newRainDropSurfaceArea = Mathf.Pow(rainDropSize / world.Data.waterDensity, 0.667f);
					float rainDropDrag = 1.0f - newRainDropSurfaceArea * world.Data.rainDropDragCoefficient;
					float rainDropVelocity = lowerWind.z - Mathf.Sqrt(2 * rainDropSize * world.Data.GravitationalAcceleration / (world.Data.airDensity * newRainDropSurfaceArea * world.Data.rainDropDragCoefficient));
					if (rainDropVelocity < 0)
					{
						// TODO: rainfall mass isnt a true mass, it doesnt cap at cloud mass, so transfer of cloudmass and humidity should be based on a percentage
						float rainfallMass = newRainDropMass * Mathf.Max(0, -rainDropVelocity * world.Data.RainfallRate);
						float rainDropFallTime = -cloudElevation / rainDropVelocity;
						float rainDropMassToHumidity = Mathf.Min(rainfallMass, rainDropFallTime * world.Data.rainDropEvapRate);
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
								surfaceWater += nGradient.x * nWater * world.Data.FlowSpeed / world.Data.tileSize * world.Data.SecondsPerTick;
								groundWater += nGroundWater * nGroundFlow;
							}
							break;
						case 1:
							if (nGradient.x < 0)
							{
								surfaceWater += nGradient.x * nWater * world.Data.FlowSpeed / world.Data.tileSize * world.Data.SecondsPerTick;
							}
							break;
						case 2:
							if (nGradient.y < 0)
							{
								surfaceWater += nGradient.x * nWater * world.Data.FlowSpeed / world.Data.tileSize * world.Data.SecondsPerTick;
							}
							break;
						case 3:
							if (nGradient.y > 0)
							{
								surfaceWater += nGradient.x * nWater * world.Data.FlowSpeed / world.Data.tileSize * world.Data.SecondsPerTick;
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
		static public void GetSunVector(World world, float planetTiltAngle, int ticks, float latitude, float longitude, out float elevationAngle, out Vector3 sunVec)
		{
			float latitudeRadians = latitude * Mathf.PI / 2;
			float angleOfInclination = planetTiltAngle * (float)Math.Sin(Math.PI * 2 * (world.GetTimeOfYear(ticks) - 0.25f));

			float timeOfDay = (world.GetTimeOfDay(ticks, longitude) - 0.5f) * Mathf.PI * 2;
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