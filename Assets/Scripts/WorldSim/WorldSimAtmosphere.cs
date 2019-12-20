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
			float globalEnergy = 0;
			float globalEnergyIncoming = 0;
			float globalEnergyReflectedAtmosphere = 0;
			float globalEnergyReflectedClouds = 0;
			float globalEnergyReflectedSurface = 0;
			float globalEnergyAbsorbedClouds = 0;
			float globalEnergyAbsorbedAtmosphere = 0;
			float globalEnergyAbsorbedSurface = 0;
			float globalEnergyAbsorbedOcean = 0;
			float globalEnergyOceanRadiation = 0;
			float globalEnergyOceanConduction = 0;
			float globalEnergyOceanEvapHeat = 0;
			float globalEnergyOutAtmosphericWindow = 0;
			float globalEnergyOutEmittedAtmosphere = 0;
			float globalOceanCoverage = 0;
			float globalCloudCoverage = 0;
			float globalOceanVolume = 0;
			float globalTemperature = 0;
			float atmosphericMass = 0;

			float inverseFullIceCoverage = 1.0f / world.Data.FullIceCoverage;
			float iceMeltRate = 1.0f / (world.Data.SpecificHeatIce * world.Data.MassIce);
			float inverseCloudMassFullAbsorption = 1.0f / world.Data.CloudMassFullAbsorption;
			float inverseWorldSize = 1.0f / world.Size;
			float inverseBoundaryZoneElevation = 1.0f / world.Data.BoundaryZoneElevation;
			float seaWaterHeatingRate = 1.0f / (world.Data.SpecificHeatSeaWater * world.Data.MassSeaWater);
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
					float elevationOrSeaLevel = elevation+waterDepth;
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
					float ice = state.Ice[index];
					float radiation = 0.001f;
					var lowerWind = state.LowerWind[index];
					var upperWind = state.UpperWind[index];
					var currentDeep = state.OceanCurrentDeep[index];
					var currentShallow = state.OceanCurrentShallow[index];

					float oceanEnergyDeep=0;
					float oceanEnergyShallow=0;
					float oceanDensity=0;
					float oceanSalinityDeep=0;
					float oceanSalinityShallow=0;
					float oceanTemperatureShallow=0;
					float oceanTemperatureDeep=0;
					float surfaceWater=0;
					float groundWater=0;
					float canopy=0;
					Vector3 gradient=Vector3.zero;
					Vector3 terrainNormal=Vector3.zero;

					bool isOcean = world.IsOcean(waterDepth);
					if (isOcean)
					{
						oceanEnergyDeep = state.OceanEnergyDeep[index];
						oceanEnergyShallow = state.OceanEnergyShallow[index];
						oceanDensity = state.OceanDensityDeep[index];
						oceanSalinityDeep = state.OceanSalinityDeep[index];
						oceanSalinityShallow = state.OceanSalinityShallow[index];
						oceanTemperatureShallow = state.OceanTemperatureShallow[index];
						oceanTemperatureDeep = GetWaterTemperature(world, oceanEnergyDeep, waterDepth);
					} else
					{
						surfaceWater = state.SurfaceWater[index];
						groundWater = state.GroundWater[index];
						canopy = state.Canopy[index];
						gradient = state.FlowDirection[index];
						terrainNormal = state.Normal[index];
					}

					float evaporation = 0;
					float newGroundWater = groundWater;
					float newSurfaceWater = surfaceWater;
					float newWaterDepth = waterDepth;
					float newIce = ice;
					float newRainfall = 0;
					float newRadiation = radiation;
					float inverseMassOfAtmosphericColumn = 1.0f / (upperAirMass + lowerAirMass);
					float iceCoverage = Mathf.Min(1.0f, ice * inverseFullIceCoverage);
					float relativeHumidity = GetRelativeHumidity(world, lowerAirTemperature, humidity, lowerAirMass, inverseDewPointTemperatureRange);
					float dewPoint = GetDewPoint(world, lowerAirTemperature, relativeHumidity);
					float cloudElevation = GetCloudElevation(world, upperAirTemperature, dewPoint);
					float cloudOpacity = Math.Min(1.0f, Mathf.Pow(cloudMass * inverseCloudMassFullAbsorption, 0.6667f)); // bottom surface of volume
					//float groundWaterSaturation = Animals.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], soilFertility * world.Data.MaxSoilPorousness);
					float energyAbsorbed = 0;
					globalCloudCoverage += cloudOpacity;

					float longitude = (float)x * inverseWorldSize;
					float sunAngle;
					Vector3 sunVector;
					GetSunVector(world, state.PlanetTiltAngle, state.Ticks, latitude, longitude, out sunAngle, out sunVector);
					sunAngle = Math.Max(0, sunAngle);
					float incomingRadiation = state.SolarRadiation * world.Data.SecondsPerTick * Mathf.Max(0, (sunVector.z + sunHitsAtmosphereBelowHorizonAmount) * inverseSunAtmosphereAmount);

					// get the actual atmospheric depth here based on radius of earth plus atmosphere
					//float inverseSunAngle = PIOver2 + sunAngle;
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



					float newLandEnergy = landEnergy;
					float newLowerAirEnergy = 0;
					float newUpperAirEnergy = 0;
					float newLowerAirMass = 0;
					float newUpperAirMass = 0;
					float newHumidity = 0;
					float newCloudMass = 0;
					float newRainDropMass = 0;
					float newOceanEnergyShallow = 0;
					float newOceanEnergyDeep = 0;
					float newOceanSalinityShallow = 0;
					float newOceanSalinityDeep = 0;

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
						if (isOcean)
						{
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
							if (!world.IsOcean(state.WaterDepth[i0])) i0 = index;
							if (!world.IsOcean(state.WaterDepth[i1])) i1 = index;
							if (!world.IsOcean(state.WaterDepth[i2])) i2 = index;
							if (!world.IsOcean(state.WaterDepth[i3])) i3 = index;

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
							if (!world.IsOcean(state.WaterDepth[i0])) i0 = index;
							if (!world.IsOcean(state.WaterDepth[i1])) i1 = index;
							if (!world.IsOcean(state.WaterDepth[i2])) i2 = index;
							if (!world.IsOcean(state.WaterDepth[i3])) i3 = index;

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
					}
					_ProfileAtmosphereMoveH.End();




					_ProfileAtmosphereEnergyBudget.Begin();

					if (isOcean)
					{
						globalOceanCoverage++;
						globalOceanVolume += waterDepth*world.Data.MetersPerTile*world.Data.MetersPerTile/1000000000;
					}

					if (incomingRadiation > 0)
					{
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

						float upperAtmosphereAbsorptionRate = world.Data.AtmosphericHeatAbsorption * upperAirMass * inverseMassOfAtmosphericColumn;
						float absorbedByUpperAtmosphereIncoming = incomingRadiation * upperAtmosphereAbsorptionRate * atmosphericDepth;
						incomingRadiation -= absorbedByUpperAtmosphereIncoming;
						newUpperAirEnergy += absorbedByUpperAtmosphereIncoming;

						globalEnergyReflectedAtmosphere += energyReflectedAtmosphere;
						globalEnergyReflectedClouds += energyReflectedClouds;

						// Absorbed by atmosphere
						{
							// stratosphere accounts for about a quarter of atmospheric mass
							//	float absorbedByStratosphere = incomingRadiation * world.Data.AtmosphericHeatAbsorption * (state.StratosphereMass / massOfAtmosphericColumn);

							float lowerAtmosphereAbsorptionRate = world.Data.AtmosphericHeatAbsorption * lowerAirMass * inverseMassOfAtmosphericColumn;
							float absorbedByLowerAtmosphereIncoming = incomingRadiation * lowerAtmosphereAbsorptionRate * atmosphericDepth;

							float humidityAbsorptionRate = Mathf.Min(0.1f, humidity * world.Data.HumidityHeatAbsorption);
							absorbedByLowerAtmosphereIncoming += incomingRadiation * humidityAbsorptionRate * atmosphericDepth;

							newLowerAirEnergy += absorbedByLowerAtmosphereIncoming;
							incomingRadiation -= absorbedByLowerAtmosphereIncoming;

							//	incomingRadiation -= absorbedByStratosphere;

							globalEnergyAbsorbedAtmosphere += absorbedByLowerAtmosphereIncoming + absorbedByUpperAtmosphereIncoming;
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
								energyReflected += incomingRadiation * GetAlbedo(world.Data.AlbedoWater, slopeAlbedo) * (1.0f - iceCoverage);
							}
							else
							{
								// reflect some incoming radiation
								float waterReflectivity = surfaceWater * world.Data.AlbedoWater;
								float soilReflectivity = GetAlbedo(world.Data.AlbedoLand - world.Data.AlbedoReductionSoilQuality * soilFertility, slopeAlbedo);
								float heatReflectedLand = canopy * world.Data.AlbedoFoliage + Math.Max(0, 1.0f - canopy) * (surfaceWater * GetAlbedo(world.Data.AlbedoWater, slopeAlbedo) + Math.Max(0, 1.0f - surfaceWater) * soilReflectivity);
								energyReflected += incomingRadiation * Mathf.Clamp01(heatReflectedLand) * (1.0f - iceCoverage);
							}
							incomingRadiation -= energyReflected;

							// TODO: do we absorb some of this energy on the way back out of the atmosphere?
							//					newLowerAirEnergy += energyReflected;
							globalEnergyReflectedSurface += energyReflected;
						}

						globalEnergyAbsorbedSurface += incomingRadiation;
						energyAbsorbed += incomingRadiation;

					}


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
							float iceTemp = lowerAirTemperature + radiationAbsorbedByIce * iceMeltRate;
							if (iceTemp > world.Data.FreezingTemperature)
							{
								iceMelted += (iceTemp - world.Data.FreezingTemperature) * iceMeltRate;
							}
						}

						// freeze the top meter based on surface temperature (plus incoming radiation)
						if (iceCoverage < 1)
						{
							if (isOcean || surfaceWater > 0)
							{
								// world.Data.SpecificHeatIce * world.Data.MassIce == KJ required to raise one cubic meter by 1 degree
								float surfaceTemp = lowerAirTemperature + incomingRadiation * seaWaterHeatingRate;
								if (surfaceTemp < world.Data.FreezingTemperature)
								{
									float iceFrozen = Math.Min(world.Data.FullIceCoverage - iceCoverage, (world.Data.FreezingTemperature - surfaceTemp) * seaWaterHeatingRate);
									newIce += iceFrozen;
									if (!isOcean)
									{
										// add to surface water
										newSurfaceWater -= iceFrozen;
									}
								}

								// TODO this should be using absolute pressure not barometric
								float inverseLowerAirPressure = 1.0f / lowerAirPressure;
								// evaporation
								float evapRate = GetEvaporationRate(world, ice, lowerAirTemperature, relativeHumidity, inverseLowerAirPressure, inverseEvapTemperatureRange);
								if (evapRate > 0)
								{
									float evapotranspiration;
									EvaporateWater(world, isOcean, evapRate, groundWater, waterTableDepth, ref newHumidity, ref newLowerAirEnergy, ref newOceanEnergyShallow, ref newGroundWater, ref newSurfaceWater, out evaporation, out evapotranspiration);
									globalEnergyOceanEvapHeat += evapotranspiration;
								}
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
							globalEnergyAbsorbedOcean += incomingRadiation;

							// heat transfer (both ways) based on temperature differential
							// conduction to ice from below
							if (ice > 0 && oceanTemperatureShallow > world.Data.FreezingTemperature)
							{
								float oceanConduction = (oceanTemperatureShallow - world.Data.FreezingTemperature) * world.Data.OceanIceConduction * world.Data.SecondsPerTick * iceCoverage;
								newOceanEnergyShallow -= oceanConduction;

								float energyToIce = Math.Max(0, oceanConduction * iceCoverage);
								iceMelted += energyToIce * iceMeltRate;
							}
							// lose heat to air via conduction AND radiation
							if (iceCoverage < 1)
							{
								// when ocean is warmer than air, it creates a convection current, which makes conduction more efficient)
								float oceanConduction = (oceanTemperatureShallow - lowerAirTemperature) * world.Data.SecondsPerTick * (1.0f - iceCoverage);
								if (oceanConduction > 0) {
									oceanConduction *= world.Data.OceanAirConductionWarming;
								} else
								{
									oceanConduction *= world.Data.OceanAirConductionCooling;
								}
								newLowerAirEnergy += oceanConduction;
								newOceanEnergyShallow -= oceanConduction;
								globalEnergyOceanConduction += oceanConduction;

								// radiate heat, will be absorbed by air
								// Net Back Radiation: The ocean transmits electromagnetic radiation into the atmosphere in proportion to the fourth power of the sea surface temperature(black-body radiation)
								// https://eesc.columbia.edu/courses/ees/climate/lectures/o_atm.html
								float oceanRadiation = oceanEnergyShallow * world.Data.OceanHeatRadiation * world.Data.SecondsPerTick * (1.0f - iceCoverage);
								newOceanEnergyShallow -= oceanRadiation;
								newLowerAirEnergy += oceanRadiation;
								globalEnergyOceanRadiation += oceanRadiation;
							}

							if (oceanTemperatureShallow < world.Data.FreezingTemperature)
							{
								float massOfShallowWaterColumn = Math.Max(0, world.Data.DeepOceanDepth - ice) * world.Data.MassSeaWater;
								float massToAchieveEquilibrium = oceanEnergyShallow / (world.Data.FreezingTemperature * world.Data.SpecificHeatSeaWater);
								float massFrozen = massOfShallowWaterColumn - massToAchieveEquilibrium;
								//newOceanEnergyShallow -= energyToAchieveEquilibrium;
								newIce += massFrozen / world.Data.MassIce;
							}
						}
						else
						{
							newLandEnergy += incomingRadiation;
						}
					}

					// radiate heat from land
					{
						float radiationRate = (1.0f - soilFertility) * (1.0f - groundWater / (world.Data.MaxWaterTableDepth * world.Data.MaxSoilPorousness)) * (1.0f / (1.0f + canopy));
						float heatRadiated = landEnergy * Mathf.Clamp01(world.Data.SecondsPerTick * radiationRate * world.Data.LandHeatRadiation);
						newLandEnergy -= heatRadiated;
						newLowerAirEnergy += heatRadiated;
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
					float energyEmittedByUpperAtmosphere = world.Data.EnergyEmittedByUpperAtmosphere * world.Data.SecondsPerTick * upperAirEnergy;
					float outgoingEnergyAbsorbedByClouds = energyEmittedByUpperAtmosphere * world.Data.CloudOutgoingAbsorptionRate * cloudOpacity;
					energyEmittedByUpperAtmosphere -= outgoingEnergyAbsorbedByClouds;

					// emit some remaining atmospheric energy to space
					float energyRadiatedToSpace = energyEmittedByUpperAtmosphere * Mathf.Max(1.0f, state.CarbonDioxide * (1.0f - world.Data.EnergyTrappedByGreenhouseGasses));
					newUpperAirEnergy -= energyRadiatedToSpace;
					globalEnergyOutEmittedAtmosphere += energyRadiatedToSpace;

					float energyLostThroughAtmosphericWindow = upperAirEnergy * world.Data.EnergyLostThroughAtmosphereWindow * world.Data.SecondsPerTick;
					newUpperAirEnergy -= energyLostThroughAtmosphericWindow;
					globalEnergyOutAtmosphericWindow += energyLostThroughAtmosphericWindow;

					float lowerEnergyLostToAtmosphericWindow = lowerAirEnergy * world.Data.EnergyLostThroughAtmosphereWindow * world.Data.SecondsPerTick;
					newLowerAirEnergy -= lowerEnergyLostToAtmosphericWindow;
					globalEnergyOutAtmosphericWindow += lowerEnergyLostToAtmosphericWindow;
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
						ref newRainDropMass,
						ref newLowerAirEnergy,
						ref newUpperAirEnergy,
						ref newOceanEnergyShallow);

					if (lowerWind.z > 0)
					{
						float verticalTransfer = lowerWind.z * world.Data.WindAirMovementVertical * inverseBoundaryZoneElevation * world.Data.SecondsPerTick;
						newUpperAirMass += verticalTransfer * lowerAirMass;
						newLowerAirMass -= verticalTransfer * lowerAirMass;
						newUpperAirEnergy += verticalTransfer * lowerAirEnergy;
						newLowerAirEnergy -= verticalTransfer * lowerAirEnergy;
					}
					else
					{
						float verticalTransfer = -lowerWind.z * world.Data.WindAirMovementVertical * inverseBoundaryZoneElevation * world.Data.SecondsPerTick;
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

					if (isOcean)
					{
						MoveOceanVertically(
							world,
							state,
							waterDepth,
							ice,
							oceanEnergyShallow,
							oceanEnergyDeep,
							oceanSalinityShallow,
							oceanSalinityDeep,
							oceanTemperatureShallow,
							oceanTemperatureDeep,
							currentShallow.z,
							ref newOceanEnergyShallow,
							ref newOceanEnergyDeep,
							ref newOceanSalinityShallow,
							ref newOceanSalinityDeep);
					}
					SeepWaterIntoGround(world, isOcean, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);
					_ProfileAtmosphereMoveV.End();



					_ProfileAtmosphereDiffusion.Begin();

					// Diffusion step
					for (int i = 0; i < 4; i++)
					{
						int nIndex = world.GetNeighborIndex(index, i);


						//TODO: make air diffuse faster at low density
						//Mixing Upper atmosphere
						{
							float nEnergy = state.UpperAirEnergy[nIndex];
							float nMass = state.UpperAirMass[nIndex];
							float upperAirDensityAtSeaLevel = upperAirMass / (world.Data.TropopauseElevation - (elevationOrSeaLevel + world.Data.BoundaryZoneElevation));
							float upperAirDensityAtSeaLevelNeighbor = nMass / (world.Data.TropopauseElevation - (state.Elevation[nIndex]+ state.WaterDepth[nIndex] + world.Data.BoundaryZoneElevation));
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

							float nTempAtSeaLevel = state.UpperAirTemperature[nIndex] - world.Data.TemperatureLapseRate * (world.Data.BoundaryZoneElevation + state.Elevation[nIndex] + state.WaterDepth[nIndex]);
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
							float nTempAtSeaLevel = state.LowerAirTemperature[nIndex] - world.Data.TemperatureLapseRate * (state.Elevation[nIndex] + state.WaterDepth[nIndex]);
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

						if (isOcean)
						{
							float neighborDepth = state.WaterDepth[nIndex];
							if (neighborDepth > 0)
							{
								float nEnergyDeep = state.OceanEnergyDeep[nIndex];
								float nTemperatureShallow = state.OceanTemperatureShallow[nIndex];
								float nTemperatureDeep = GetWaterTemperature(world, nEnergyDeep, neighborDepth);
								float nSalinityShallow = state.OceanSalinityShallow[nIndex];
								float nSalinityDeep = state.OceanSalinityDeep[nIndex];
								float salinityDeepPercentage = oceanSalinityDeep / waterDepth;

								// Horizontal mixing
								float mixingDepth = Math.Min(neighborDepth, waterDepth);
								newOceanEnergyShallow += world.Data.SpecificHeatSeaWater * world.Data.MassSeaWater * world.Data.DeepOceanDepth * (nTemperatureShallow - oceanTemperatureShallow) * world.Data.OceanHorizontalMixingSpeed;
								newOceanEnergyDeep += world.Data.SpecificHeatSeaWater * world.Data.MassSeaWater * mixingDepth * (nTemperatureDeep - oceanTemperatureDeep) * world.Data.OceanHorizontalMixingSpeed;

								float nSalinityDeepPercentage = nSalinityDeep / neighborDepth;
								newOceanSalinityDeep += (nSalinityDeepPercentage - salinityDeepPercentage) * world.Data.OceanHorizontalMixingSpeed * Math.Min(neighborDepth, waterDepth);
								newOceanSalinityShallow += (nSalinityShallow - oceanSalinityShallow) * world.Data.OceanHorizontalMixingSpeed;
							}
						}

						// ground water flow
						float nWater = state.SurfaceWater[nIndex];
						if (nWater > 0)
						{
							float nGroundWater = state.GroundWater[nIndex];
							var nGradient = state.FlowDirection[nIndex];
							var nGroundFlow = world.Data.GroundWaterFlowSpeed * state.SoilFertility[nIndex];
							switch (i)
							{
								case 0:
									if (nGradient.x > 0)
									{
										newSurfaceWater += nGradient.x * nWater * world.Data.FlowSpeed * world.Data.InverseMetersPerTile * world.Data.SecondsPerTick;
										newGroundWater += nGroundWater * nGroundFlow;
									}
									break;
								case 1:
									if (nGradient.x < 0)
									{
										newSurfaceWater += nGradient.x * nWater * world.Data.FlowSpeed * world.Data.InverseMetersPerTile * world.Data.SecondsPerTick;
										newGroundWater += nGroundWater * nGroundFlow;
									}
									break;
								case 2:
									if (nGradient.y < 0)
									{
										newSurfaceWater += nGradient.x * nWater * world.Data.FlowSpeed * world.Data.InverseMetersPerTile * world.Data.SecondsPerTick;
										newGroundWater += nGroundWater * nGroundFlow;
									}
									break;
								case 3:
									if (nGradient.y > 0)
									{
										newSurfaceWater += nGradient.x * nWater * world.Data.FlowSpeed * world.Data.InverseMetersPerTile * world.Data.SecondsPerTick;
										newGroundWater += nGroundWater * nGroundFlow;
									}
									break;
							}
						}


					}
					_ProfileAtmosphereDiffusion.End();



					if (float.IsNaN(nextState.LowerAirEnergy[index]) ||
						float.IsNaN(nextState.UpperAirEnergy[index]) ||
						float.IsNaN(nextState.LowerAirMass[index]) ||
						float.IsNaN(nextState.UpperAirMass[index]) ||
						float.IsNaN(nextState.Humidity[index]))
					{
						return;
					}

					_ProfileAtmosphereFinal.Begin();

					nextState.LandEnergy[index] += newLandEnergy;
					nextState.LowerAirMass[index] += newLowerAirMass;
					nextState.UpperAirMass[index] += newUpperAirMass;
					nextState.LowerAirEnergy[index] += newLowerAirEnergy;
					nextState.UpperAirEnergy[index] += newUpperAirEnergy;
					nextState.Humidity[index] += newHumidity;
					nextState.CloudMass[index] = Mathf.Max(0, nextState.CloudMass[index] + newCloudMass);
					nextState.RainDropMass[index] += newRainDropMass;
					nextState.OceanEnergyShallow[index] += newOceanEnergyShallow;
					nextState.OceanEnergyDeep[index] += newOceanEnergyDeep;
					nextState.OceanSalinityShallow[index] += newOceanSalinityShallow;
					nextState.OceanSalinityDeep[index] += newOceanSalinityDeep;

					nextState.WaterDepth[index] = Mathf.Max(0, newWaterDepth);
					nextState.SurfaceWater[index] = Mathf.Max(0, newSurfaceWater);
					nextState.Ice[index] = Mathf.Max(0, newIce);
					nextState.GroundWater[index] = Mathf.Max(0, newGroundWater);
					nextState.Radiation[index] = Mathf.Max(0, newRadiation);

					nextState.Evaporation[index] = evaporation;
					nextState.Rainfall[index] = newRainfall;
					nextState.EnergyAbsorbed[index] = energyAbsorbed;

					globalEnergyGained += energyAbsorbed;

					_ProfileAtmosphereFinal.End();

				}
			}

			for (int index = 0; index < world.Size*world.Size; index++)
			{
				float elevation = nextState.Elevation[index];
				float elevationOrSeaLevel = elevation + nextState.WaterDepth[index];
				nextState.LowerAirTemperature[index] = GetAirTemperature(world, nextState.LowerAirEnergy[index], nextState.LowerAirMass[index]);
				nextState.LowerAirPressure[index] = Mathf.Max(0, GetAirPressure(world, nextState.LowerAirMass[index] + nextState.UpperAirMass[index] + state.StratosphereMass, elevationOrSeaLevel, nextState.LowerAirTemperature[index]));
				nextState.UpperAirTemperature[index] = GetAirTemperature(world, nextState.UpperAirEnergy[index], nextState.UpperAirMass[index]);
				nextState.UpperAirPressure[index] = Mathf.Max(0, GetAirPressure(world, nextState.UpperAirMass[index] + state.StratosphereMass, elevationOrSeaLevel + world.Data.BoundaryZoneElevation, nextState.UpperAirTemperature[index]));
				nextState.OceanTemperatureShallow[index] = Mathf.Max(0, GetWaterTemperature(world, nextState.OceanEnergyShallow[index], Math.Max(0, world.Data.DeepOceanDepth - nextState.Ice[index])));
				nextState.OceanDensityDeep[index] = Mathf.Max(0, GetOceanDensity(world, nextState.OceanEnergyDeep[index], nextState.OceanSalinityDeep[index], state.WaterDepth[index]));
				globalEnergy += nextState.LowerAirEnergy[index] + nextState.UpperAirEnergy[index] + nextState.OceanEnergyDeep[index] + nextState.OceanEnergyShallow[index];
				atmosphericMass += nextState.LowerAirMass[index] + nextState.UpperAirMass[index];

				if (float.IsInfinity(nextState.LowerAirPressure[index]))
				{
					Debug.Break();
				}

			}


			nextState.GlobalEnergyGained = globalEnergyGained;
			nextState.GlobalEnergy = globalEnergy;
			nextState.GlobalEnergyIncoming = globalEnergyIncoming;
			nextState.GlobalEnergyReflectedAtmosphere = globalEnergyReflectedAtmosphere;
			nextState.GlobalEnergyReflectedCloud = globalEnergyReflectedClouds;
			nextState.GlobalEnergyReflectedSurface = globalEnergyReflectedSurface;
			nextState.GlobalEnergyAbsorbedCloud = globalEnergyAbsorbedClouds;
			nextState.GlobalEnergyAbsorbedAtmosphere = globalEnergyAbsorbedAtmosphere;
			nextState.GlobalEnergyAbsorbedSurface = globalEnergyAbsorbedSurface;
			nextState.GlobalEnergyAbsorbedOcean = globalEnergyAbsorbedOcean;
			nextState.GlobalEnergyOceanRadiation = globalEnergyOceanRadiation;
			nextState.GlobalEnergyOceanConduction = globalEnergyOceanConduction;
			nextState.GlobalEnergyOceanEvapHeat = globalEnergyOceanEvapHeat;
			nextState.GlobalEnergyOutAtmosphericWindow = globalEnergyOutAtmosphericWindow;
			nextState.GlobalEnergyOutEmittedAtmosphere = globalEnergyOutEmittedAtmosphere;
			nextState.GlobalTemperature = globalTemperature;
			nextState.GlobalOceanCoverage = globalOceanCoverage * inverseWorldSize * inverseWorldSize;
			nextState.GlobalCloudCoverage = globalCloudCoverage * inverseWorldSize * inverseWorldSize;
			nextState.GlobalOceanVolume = globalOceanVolume;
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
			ref float newRainDropMass,
			ref float newLowerAirEnergy,
			ref float newUpperAirEnergy,
			ref float newOceanEnergyShallow)
		{


			// condensation
			if (relativeHumidity > 1)
			{
				float condensationMass = humidity * (relativeHumidity - 1.0f) / relativeHumidity;
				newHumidity -= condensationMass;
				float condensationVolume = condensationMass / world.Data.MassFreshWater;

				if (isOcean)
				{
					newOceanEnergyShallow += condensationVolume * world.Data.EvaporativeHeatLoss;
				} else
				{
					newLowerAirEnergy += condensationVolume * world.Data.EvaporativeHeatLoss;
				}

				if (lowerAirTemperature <= world.Data.FreezingTemperature)
				{
					newIce += condensationVolume;
				}
				else if (!isOcean)
				{
					newSurfaceWater += condensationVolume;
				}
			}

			if (lowerWind.z > 0)
			{
				float humidityToCloud = Mathf.Min(1.0f, lowerWind.z * world.Data.SecondsPerTick / cloudElevation) * humidity;
				newCloudMass += humidityToCloud;
				newHumidity -= humidityToCloud;
			}

			if (cloudMass > 0)
			{
				newRainDropMass = Mathf.Max(0, newRainDropMass + Mathf.Sqrt(cloudMass) * (world.Data.RainDropFormationSpeedTemperature / dewPoint * Mathf.Max(0, lowerWind.z) * world.Data.RainDropCoalescenceWind));

				if (newRainDropMass > 0)
				{
					// TODO: airDesntiy and rainDensity should probably be cleaned up (derived from other data?)
					float rainDropSize = newRainDropMass / cloudMass;
					float newRainDropSurfaceArea = Mathf.Pow(rainDropSize / world.Data.waterDensity, 0.667f);
					float rainDropDrag = 1.0f - newRainDropSurfaceArea * world.Data.rainDropDragCoefficient;
					float rainDropVelocity = lowerWind.z - Mathf.Sqrt(2 * rainDropSize * world.Data.GravitationalAcceleration / (world.Data.airDensity * newRainDropSurfaceArea * world.Data.rainDropDragCoefficient));
					if (rainDropVelocity < 0)
					{
						// TODO: rainfall mass isnt a true mass, it doesnt cap at cloud mass, so transfer of cloudmass and humidity should be based on a percentage
						float rainfallMass = cloudMass * Mathf.Clamp01(-rainDropVelocity * world.Data.RainfallRate);
						float rainDropFallTime = -cloudElevation / rainDropVelocity;
						float rainDropMassToHumidity = Mathf.Min(rainfallMass, rainDropFallTime * world.Data.rainDropEvapRate);
						newCloudMass -= rainfallMass;
						newRainDropMass *= (1.0f - rainfallMass / cloudMass);
						newHumidity += rainDropMassToHumidity;
						if (rainfallMass > rainDropMassToHumidity)
						{
							newRainfall = (rainfallMass - rainDropMassToHumidity) / world.Data.MassFreshWater;
							newUpperAirEnergy += newRainfall * world.Data.EvaporativeHeatLoss; // return the latent heat to the air
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

		static private float GetEvaporationRate(World world, float iceCoverage, float temperature, float relativeHumidity, float airPressureInverse, float inverseEvapTemperatureRange)
		{
			float evapTemperature = Mathf.Clamp01((temperature - world.Data.EvapMinTemperature) * inverseEvapTemperatureRange);

			return Mathf.Clamp01((1.0f - iceCoverage) * (1.0f - relativeHumidity) * Sqr(evapTemperature)) * world.Data.EvaporationRate;
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

		static public float GetAbsoluteHumidity(World world, float temperature, float relativeHumidity, float airMass, float inverseDewPointTemperatureRange)
		{
			float maxWaterVaporPerKilogramAir = world.Data.WaterVaporMassToAirMassAtDewPoint * Sqr(Mathf.Max(0, (temperature - world.Data.DewPointZero) * inverseDewPointTemperatureRange));
			float maxHumidity = maxWaterVaporPerKilogramAir * airMass;
			if (maxHumidity <= 0)
			{
				return 0;
			}
			float humidity = relativeHumidity * maxHumidity;
			return humidity;
		}

		static private void EvaporateWater(World world, bool isOcean, float evapRate, float groundWater, float waterTableDepth, ref float newHumidity, ref float newAirEnergy, ref float newOceanEnergy, ref float newGroundWater, ref float surfaceWater, out float evaporation, out float evapotranspiration)
		{
			evaporation = 0;
			if (isOcean)
			{
				newHumidity += evapRate * world.Data.MassFreshWater;
				evaporation += evapRate;
			}
			else
			{
				if (surfaceWater > 0)
				{
					float waterSurfaceArea = Math.Min(1.0f, (float)Math.Sqrt(surfaceWater));
					float evap = Math.Max(0, Math.Min(surfaceWater, waterSurfaceArea * evapRate));
					surfaceWater -= evap;
					newHumidity += evap * world.Data.MassFreshWater;
					evaporation += evap;
				}
				if (waterTableDepth > 0)
				{
					var groundWaterEvap = Math.Max(0, Math.Min(newGroundWater, groundWater / waterTableDepth * evapRate));
					newGroundWater -= groundWaterEvap;
					newHumidity += groundWaterEvap * world.Data.MassFreshWater;
					evaporation += groundWaterEvap;
				}
			}

			evapotranspiration = evaporation * world.Data.EvaporativeHeatLoss;
			newOceanEnergy -= evapotranspiration;
		}

		static private void MoveOceanVertically(
		World world,
		World.State state,
		float depth,
		float ice,
		float oceanEnergyShallow,
		float oceanEnergyDeep,
		float oceanSalinityShallow,
		float oceanSalinityDeep,
		float oceanTemperatureShallow,
		float oceanTemperatureDeep,
		float currentShallowZ,
		ref float newOceanEnergyShallow,
		ref float newOceanEnergyDeep,
		ref float newOceanSalinityShallow,
		ref float newOceanSalinityDeep)
		{
			//		if (depth > surfaceTemperatureDepth)
			{
				//			float surfacePercent = surfaceTemperatureDepth / depth;
				//			float surfacePercent = 0.1f;
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
					float salinityExchange = (oceanSalinityDeep / depth - oceanSalinityShallow / shallowColumnDepth) * world.Data.SalinityVerticalMixingSpeed * (oceanSalinityShallow + oceanSalinityDeep) * world.Data.SecondsPerTick;
					newOceanSalinityDeep -= salinityExchange * depth / (depth + shallowColumnDepth);
					newOceanSalinityShallow += salinityExchange * world.Data.DeepOceanDepth / (depth + shallowColumnDepth);

					float deepWaterMixingDepth = Math.Min(world.Data.DeepOceanDepth, depth);
					float heatExchange = (oceanTemperatureDeep - oceanTemperatureShallow) * deepWaterMixingDepth * world.Data.OceanTemperatureVerticalMixingSpeed;
					newOceanEnergyShallow += heatExchange;
					newOceanEnergyDeep -= heatExchange;
				}

				if (currentShallowZ < 0)
				{
					float downwelling = Math.Min(0.5f, -currentShallowZ * world.Data.OceanUpwellingSpeed);
					float energyExchange = oceanEnergyShallow * downwelling;
					newOceanEnergyShallow -= energyExchange;
					newOceanEnergyDeep += energyExchange;
					float salinityExchange = oceanSalinityShallow * downwelling;
					newOceanSalinityDeep += salinityExchange;
					newOceanSalinityShallow -= salinityExchange;
				}
				else if (currentShallowZ > 0)
				{
					float upwelling = Math.Min(0.5f, currentShallowZ * world.Data.OceanUpwellingSpeed);
					float mixingDepth = Math.Min(depth, world.Data.DeepOceanDepth) / depth;
					float energyExchange = oceanEnergyDeep * mixingDepth * upwelling;
					newOceanEnergyShallow += energyExchange;
					newOceanEnergyDeep -= energyExchange;
					float salinityExchange = oceanSalinityDeep * mixingDepth * upwelling;
					newOceanSalinityDeep -= salinityExchange;
					newOceanSalinityShallow += salinityExchange;
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
			return energy / (world.Data.SpecificHeatSeaWater * depth * world.Data.MassSeaWater);
		}
		static public float GetWaterEnergy(World world, float temperature, float depth)
		{
			return temperature * (world.Data.SpecificHeatSeaWater * depth * world.Data.MassSeaWater);
		}
		static public float GetAlbedo(float surfaceAlbedo, float slope)
		{
			return surfaceAlbedo + (1.0f - surfaceAlbedo) * slope;
		}
		static public float GetDewPoint(World world, float lowerAirTemperature, float relativeHumidity)
		{
			return lowerAirTemperature - (1.0f - relativeHumidity) * world.Data.DewPointTemperaturePerRelativeHumidity;
		}
		static public float GetCloudElevation(World world, float upperAirTemperature, float dewPoint)
		{
			return world.Data.BoundaryZoneElevation + (upperAirTemperature - dewPoint) * world.Data.DewPointElevationPerDegree;
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