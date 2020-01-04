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
			float globalEnergyEvapotranspiration = 0;
			float globalEnergyOutAtmosphericWindow = 0;
			float globalEnergyOutEmittedAtmosphere = 0;
			float globalOceanCoverage = 0;
			float globalCloudCoverage = 0;
			float globalOceanVolume = 0;
			float globalTemperature = 0;
			float globalEvaporation = 0;
			float globalRainfall = 0;
			float atmosphericMass = 0;
			float seaLevel = 0;
			int seaLevelTiles = 0;

			float inverseFullCanopyCoverage = 1.0f / world.Data.FullCanopyCoverage;
			float inverseFullWaterCoverage = 1.0f / world.Data.FullWaterCoverage;
			float inverseFullIceCoverage = 1.0f / (world.Data.MassIce * world.Data.FullIceCoverage);
			float iceMeltRate = 1.0f / (world.Data.SpecificHeatIce * world.Data.MassIce);
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
					var surfaceGradient = state.SurfaceGradient[index];
					var terrainGradient = state.TerrainGradient[index];
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
					//float groundWaterSaturation = Animals.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], soilFertility * world.Data.MaxSoilPorousness);
					float energyAbsorbed = 0;
					globalCloudCoverage += cloudCoverage;

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

					// These constants obtained here, dunno if I've interpreted them correctly
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

							contentMove = shallowWaterMass * world.Data.OceanCurrentSpeed;
							nextState.ShallowWaterMass[index] += shallowWaterMass * (1.0f - world.Data.OceanCurrentSpeed);
							nextState.ShallowWaterMass[i0] += contentMove * move0;
							nextState.ShallowWaterMass[i1] += contentMove * move1;
							nextState.ShallowWaterMass[i2] += contentMove * move2;
							nextState.ShallowWaterMass[i3] += contentMove * move3;

							contentMove = shallowWaterEnergy * world.Data.OceanCurrentSpeed;
							nextState.ShallowWaterEnergy[index] += shallowWaterEnergy * (1.0f - world.Data.OceanCurrentSpeed);
							nextState.ShallowWaterEnergy[i0] += contentMove * move0;
							nextState.ShallowWaterEnergy[i1] += contentMove * move1;
							nextState.ShallowWaterEnergy[i2] += contentMove * move2;
							nextState.ShallowWaterEnergy[i3] += contentMove * move3;

							contentMove = shallowSaltMass * world.Data.OceanCurrentSpeed;
							nextState.ShallowSaltMass[index] += shallowSaltMass * (1.0f - world.Data.OceanCurrentSpeed);
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
							movePos = new Vector2(x, y) + terrainGradient * metersPerSecondToTilesPerTick * world.Data.GroundWaterFlowSpeed * soilFertility * world.Data.GravitationalAcceleration;
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

							nextState.GroundWater[i0] += groundWater * move0;
							nextState.GroundWater[i1] += groundWater * move1;
							nextState.GroundWater[i2] += groundWater * move2;
							nextState.GroundWater[i3] += groundWater * move3;
						}
					}
					_ProfileAtmosphereMoveH.End();




					_ProfileAtmosphereEnergyBudget.Begin();

					if (waterCoverage > 0)
					{
						globalOceanCoverage += waterCoverage;
						// TODO: this currently includes ice.... should it?
						globalOceanVolume += waterDepth*world.Data.MetersPerTile*world.Data.MetersPerTile/1000000000;
					}

					if (incomingRadiation > 0)
					{
						globalEnergyIncoming += incomingRadiation;

						// TODO: reflect/absorb more in the atmosphere with a lower sun angle

						// reflect some rads off atmosphere and clouds
						// TODO: this process feels a little broken -- are we giving too much priority to reflecting/absorbing in certain layers?
						float energyReflectedAtmosphere = incomingRadiation * Mathf.Min(1, world.Data.AtmosphericHeatReflection * (upperAirMass + lowerAirMass));
						incomingRadiation -= energyReflectedAtmosphere;

						float cloudReflectionRate = world.Data.CloudIncomingReflectionRate * cloudCoverage;
						float energyReflectedClouds = incomingRadiation * cloudReflectionRate;
						incomingRadiation -= energyReflectedClouds;

						float cloudAbsorptionRate = world.Data.CloudIncomingAbsorptionRate * cloudCoverage;
						float absorbedByCloudsIncoming = incomingRadiation * cloudAbsorptionRate;
						incomingRadiation -= absorbedByCloudsIncoming;
						newUpperAirEnergy += absorbedByCloudsIncoming;
						globalEnergyAbsorbedClouds += absorbedByCloudsIncoming;

						float upperAtmosphereAbsorptionRate = Mathf.Min(1, world.Data.AtmosphericHeatAbsorption * upperAirMass);
						float absorbedByUpperAtmosphereIncoming = incomingRadiation * upperAtmosphereAbsorptionRate * atmosphericDepth;
						incomingRadiation -= absorbedByUpperAtmosphereIncoming;
						newUpperAirEnergy += absorbedByUpperAtmosphereIncoming;

						globalEnergyReflectedAtmosphere += energyReflectedAtmosphere;
						globalEnergyReflectedClouds += energyReflectedClouds;

						// Absorbed by atmosphere
						// stratosphere accounts for about a quarter of atmospheric mass
						//	float absorbedByStratosphere = incomingRadiation * world.Data.AtmosphericHeatAbsorption * (state.StratosphereMass / massOfAtmosphericColumn);

						float lowerAtmosphereAbsorptionRate = Mathf.Min(1, world.Data.AtmosphericHeatAbsorption * (lowerAirMass + humidity));
						float absorbedByLowerAtmosphereIncoming = incomingRadiation * lowerAtmosphereAbsorptionRate * atmosphericDepth;

						newLowerAirEnergy += absorbedByLowerAtmosphereIncoming;
						incomingRadiation -= absorbedByLowerAtmosphereIncoming;
						globalEnergyAbsorbedAtmosphere += absorbedByLowerAtmosphereIncoming + absorbedByUpperAtmosphereIncoming;

						// reflection off surface
						float energyReflected = 0;
						{
							if (iceCoverage > 0)
							{
								energyReflected += incomingRadiation * iceCoverage * GetAlbedo(world.Data.AlbedoIce, 0);
							}
							if (waterCoverage > 0)
							{
								float slopeAlbedo = Mathf.Pow(1.0f - Math.Max(0, sunVector.z), 9);
								energyReflected += waterCoverage * incomingRadiation * GetAlbedo(world.Data.AlbedoWater, slopeAlbedo) * (1.0f - iceCoverage);
							}
							if (waterCoverage < 1 && iceCoverage < 1)
							{
								// reflect some incoming radiation
								float slopeAlbedo = 0;
								float soilReflectivity = GetAlbedo(world.Data.AlbedoLand - world.Data.AlbedoReductionSoilQuality * soilFertility, slopeAlbedo);
								float heatReflectedLand = canopyCoverage * world.Data.AlbedoFoliage + Math.Max(0, 1.0f - canopyCoverage) * soilReflectivity;
								energyReflected += incomingRadiation * Mathf.Clamp01(heatReflectedLand) * (1.0f - iceCoverage) * (1.0f - waterCoverage);
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
						if (iceMass > 0)
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
									float energyTransfer = iceMassFrozen * world.Data.SpecificHeatWater * surfaceTemp;
									newShallowWaterEnergy -= energyTransfer;
								}

								// TODO this should be using absolute pressure not barometric
								float inverseLowerAirPressure = 1.0f / lowerAirPressure;
								// evaporation
								float evapRate = GetEvaporationRate(world, iceMass, lowerAirTemperature, relativeHumidity, inverseLowerAirPressure, inverseEvapTemperatureRange);
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
						newLandEnergy += (1.0f - waterCoverage) * incomingRadiation;
						if (waterCoverage > 0)
						{
							// absorb remaining incoming radiation (we've already absorbed radiation in surface ice above)
							newShallowWaterEnergy += waterCoverage * incomingRadiation;
							globalEnergyAbsorbedOcean += waterCoverage * incomingRadiation;

							// heat transfer (both ways) based on temperature differential
							// conduction to ice from below
							if (iceMass > 0 && shallowWaterTemperature > world.Data.FreezingTemperature)
							{
								float oceanConduction = (shallowWaterTemperature - world.Data.FreezingTemperature) * world.Data.OceanIceConduction * world.Data.SecondsPerTick * iceCoverage;
								newShallowWaterEnergy -= oceanConduction;

								float energyToIce = Math.Max(0, oceanConduction * iceCoverage);
								iceMelted += energyToIce * iceMeltRate;
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

								// radiate heat, will be absorbed by air
								// Net Back Radiation: The ocean transmits electromagnetic radiation into the atmosphere in proportion to the fourth power of the sea surface temperature(black-body radiation)
								// https://eesc.columbia.edu/courses/ees/climate/lectures/o_atm.html
								float oceanRadiation = shallowWaterEnergy * world.Data.OceanHeatRadiation * world.Data.SecondsPerTick * (1.0f - iceCoverage);
								newShallowWaterEnergy -= oceanRadiation;
								newLowerAirEnergy += oceanRadiation;
								globalEnergyOceanRadiation += oceanRadiation;
							}

							if (shallowWaterTemperature < world.Data.FreezingTemperature)
							{
								float massFrozen = shallowWaterMass - Mathf.Min(shallowWaterMass,
									(shallowWaterEnergy - shallowSaltMass * (world.Data.FreezingTemperature + world.Data.SpecificHeatSalt) - shallowWaterMass * (world.Data.FreezingTemperature * world.Data.SpecificHeatIce)) 
									/ ((world.Data.FreezingTemperature * world.Data.SpecificHeatWater + world.Data.LatentHeatWaterLiquid) - (world.Data.FreezingTemperature * world.Data.SpecificHeatIce)));

								newIceMass += massFrozen;
								newShallowWaterMass -= massFrozen;
								float energyTransfer = massFrozen * (world.Data.SpecificHeatWater * world.Data.FreezingTemperature); // no latent heat for ice -- water latent heat stays in the system, warming the water
								newShallowWaterEnergy -= energyTransfer;
							}
						}
					}

					// radiate heat from land
					{
						// TODO: deal with the fact that this also incorporates ground water
						float groundWaterEnergy = Mathf.Min(landEnergy, groundWater * (world.Data.maxGroundWaterTemperature * world.Data.SpecificHeatWater + world.Data.LatentHeatWaterLiquid));
						float radiationRate = (1.0f - soilFertility * 0.5f) * (1.0f - canopyCoverage * 0.5f);
						float heatRadiated = (landEnergy - groundWaterEnergy) * Mathf.Min(1.0f, radiationRate * world.Data.LandHeatRadiation * world.Data.SecondsPerTick);
						newLandEnergy -= heatRadiated;
						if (deepWaterMass > 0)
						{
							newDeepWaterEnergy += heatRadiated;
						} else
						{
							newShallowWaterEnergy += heatRadiated * waterCoverage;
							newLowerAirEnergy += heatRadiated * (1.0f - waterCoverage);
						}
					}

					// reduce ice
					iceMelted = Math.Min(iceMelted, iceMass);
					if (iceMelted > 0)
					{
						newIceMass -= iceMelted;
						newShallowWaterMass += iceMelted;
						float energyTransfer = iceMelted * (world.Data.SpecificHeatIce * world.Data.FreezingTemperature);
						newShallowWaterEnergy += energyTransfer;
					}


					// lose some energy to space
					//float cloudReflectionFactor = world.Data.cloudReflectionRate * cloudOpacity;
					//float humidityPercentage = humidity / atmosphereMass;
					//float heatLossFactor = (1.0f - world.Data.carbonDioxide * world.Data.heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
					//float loss = airEnergy * (1.0f - cloudReflectionFactor) * (world.Data.heatLoss * heatLossFactor * airPressureInverse);

					// absorb some outgoing energy in clouds
					//					float energyRadiatedToSpace = energyEmittedByUpperAtmosphere * Mathf.Max(1.0f, state.CarbonDioxide * (1.0f - world.Data.EnergyTrappedByGreenhouseGasses));
					float upperAtmosphereInfraredAbsorption = Mathf.Min(1, cloudMass * world.Data.CloudOutgoingAbsorptionRate);
					upperAtmosphereInfraredAbsorption += (1.0f - upperAtmosphereInfraredAbsorption) * Mathf.Min(1.0f, upperAirMass * state.CarbonDioxide * world.Data.EnergyTrappedByGreenhouseGasses);
					float lowerAtmosphereInfraredAbsorption = Mathf.Min(1.0f, (lowerAirMass * state.CarbonDioxide) * world.Data.EnergyTrappedByGreenhouseGasses + humidity * world.Data.EnergyTrappedByWaterVapor);

					{
						float energyEmittedByUpperAtmosphere = world.Data.EnergyEmittedByAtmosphere * world.Data.SecondsPerTick * upperAirEnergy;
						float energyLostThroughAtmosphericWindow = energyEmittedByUpperAtmosphere * world.Data.EnergyLostThroughAtmosphereWindow;
						newUpperAirEnergy -= energyLostThroughAtmosphericWindow;
						energyEmittedByUpperAtmosphere -= energyLostThroughAtmosphericWindow;
						energyEmittedByUpperAtmosphere *= 1.0f - upperAtmosphereInfraredAbsorption;
						newUpperAirEnergy -= energyEmittedByUpperAtmosphere;
						float energyRadiatedToSpace = energyEmittedByUpperAtmosphere / 2;
						energyEmittedByUpperAtmosphere -= energyRadiatedToSpace;
						float energyAborbedByLowerAtmosphere = lowerAtmosphereInfraredAbsorption * energyEmittedByUpperAtmosphere;
						newLowerAirEnergy += energyAborbedByLowerAtmosphere;
						energyEmittedByUpperAtmosphere -= energyAborbedByLowerAtmosphere;
						newShallowWaterEnergy += energyEmittedByUpperAtmosphere * waterCoverage;
						newLandEnergy += energyEmittedByUpperAtmosphere * (1.0f - waterCoverage);
						globalEnergyOutEmittedAtmosphere += energyRadiatedToSpace;
						globalEnergyOutAtmosphericWindow += energyLostThroughAtmosphericWindow;
					}
					{
						float energyEmitted = world.Data.EnergyEmittedByAtmosphere * world.Data.SecondsPerTick * lowerAirEnergy;
						float energyLostThroughAtmosphericWindow = energyEmitted * world.Data.EnergyLostThroughAtmosphereWindow;
						globalEnergyOutAtmosphericWindow += energyLostThroughAtmosphericWindow;
						newLowerAirEnergy -= energyLostThroughAtmosphericWindow;
						energyEmitted -= energyLostThroughAtmosphericWindow;
						energyEmitted *= 1.0f - lowerAtmosphereInfraredAbsorption;
						newLowerAirEnergy -= energyEmitted;
						float energyRadiatedUpwards = energyEmitted / 2;
						newUpperAirEnergy += energyRadiatedUpwards * upperAtmosphereInfraredAbsorption;
						globalEnergyOutEmittedAtmosphere += energyRadiatedUpwards * (1.0f - upperAtmosphereInfraredAbsorption);
						energyEmitted -= energyRadiatedUpwards;
						newShallowWaterEnergy += energyEmitted * waterCoverage;
						newLandEnergy += energyEmitted * (1.0f - waterCoverage);
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



					if (float.IsNaN(nextState.LowerAirEnergy[index]) ||
						float.IsNaN(nextState.UpperAirEnergy[index]) ||
						float.IsNaN(nextState.LowerAirMass[index]) ||
						float.IsNaN(nextState.UpperAirMass[index]) ||
						float.IsNaN(nextState.Humidity[index]))
					{
						return;
					}

					_ProfileAtmosphereFinal.Begin();

					nextState.LandEnergy[index] = Mathf.Max(0, nextState.LandEnergy[index] + newLandEnergy);
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

					nextState.IceMass[index] = Mathf.Max(0, newIceMass);
					nextState.Radiation[index] = Mathf.Max(0, newRadiation);

					nextState.Evaporation[index] = evaporation;
					nextState.Rainfall[index] = newRainfall;
					nextState.EnergyAbsorbed[index] = energyAbsorbed;

					globalEnergyGained += energyAbsorbed;
					globalRainfall += newRainfall;
					globalEvaporation += evaporation;

					_ProfileAtmosphereFinal.End();

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
				nextState.LowerAirTemperature[index] = GetAirTemperature(world, nextState.LowerAirEnergy[index], nextState.LowerAirMass[index], nextState.Humidity[index], world.Data.LatentHeatWaterVapor, world.Data.SpecificHeatWaterVapor);
				nextState.LowerAirPressure[index] = Mathf.Max(0, GetAirPressure(world, nextState.LowerAirMass[index] + nextState.UpperAirMass[index] + state.StratosphereMass, elevationOrSeaLevel, nextState.LowerAirTemperature[index]));
				nextState.UpperAirTemperature[index] = GetAirTemperature(world, nextState.UpperAirEnergy[index], nextState.UpperAirMass[index], nextState.CloudMass[index], world.Data.LatentHeatWaterLiquid, world.Data.SpecificHeatWater);
				nextState.UpperAirPressure[index] = Mathf.Max(0, GetAirPressure(world, nextState.UpperAirMass[index] + state.StratosphereMass, elevationOrSeaLevel + world.Data.BoundaryZoneElevation, nextState.UpperAirTemperature[index]));

				globalEnergy += nextState.LowerAirEnergy[index] + nextState.UpperAirEnergy[index] + nextState.DeepWaterEnergy[index] + nextState.ShallowWaterEnergy[index];
				atmosphericMass += nextState.LowerAirMass[index] + nextState.UpperAirMass[index];


				if (float.IsInfinity(nextState.LowerAirPressure[index]))
				{
					Debug.DebugBreak();
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
			nextState.GlobalEnergyEvapotranspiration = globalEnergyEvapotranspiration;
			nextState.GlobalEnergyOutAtmosphericWindow = globalEnergyOutAtmosphericWindow;
			nextState.GlobalEnergyOutEmittedAtmosphere = globalEnergyOutEmittedAtmosphere;
			nextState.GlobalTemperature = globalTemperature;
			nextState.GlobalOceanCoverage = globalOceanCoverage * inverseWorldSize * inverseWorldSize;
			nextState.GlobalCloudCoverage = globalCloudCoverage * inverseWorldSize * inverseWorldSize;
			nextState.GlobalEvaporation = globalEvaporation;
			nextState.GlobalRainfall = globalRainfall;
			nextState.GlobalOceanVolume = globalOceanVolume;
			nextState.GlobalSeaLevel = seaLevel / seaLevelTiles;
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
				float energyTransfer = condensationMass * (world.Data.LatentHeatWaterLiquid + world.Data.SpecificHeatWaterVapor * lowerAirTemperature);
				newLowerAirEnergy -= energyTransfer;
				if (lowerAirTemperature <= world.Data.FreezingTemperature)
				{
					newIceMass += condensationMass;
				}
				else
				{
					newShallowWaterMass += condensationMass;
					newShallowWaterEnergy += energyTransfer;
				}
			}

			if (lowerWind.z > 0)
			{
				float humidityToCloud = Mathf.Min(1.0f, lowerWind.z * world.Data.SecondsPerTick / (cloudElevation - elevationOrSeaLevel)) * humidity * world.Data.HumidityToCloudPercent;
				newCloudMass += humidityToCloud;
				newHumidity -= humidityToCloud;
				// We're moving the latent heat of water vapor here since we want it to heat up the upper air around the cloud
				float energyTransfer = humidityToCloud * (world.Data.LatentHeatWaterVapor + world.Data.SpecificHeatWaterVapor * lowerAirTemperature);
				newLowerAirEnergy -= energyTransfer;
				newUpperAirEnergy += energyTransfer;
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
						newRainfall = cloudMass * Mathf.Clamp01(-rainDropVelocity * world.Data.RainfallRate);
						newCloudMass -= newRainfall;
						newRainDropMass *= (1.0f - newRainfall / cloudMass);

						{
							float rainDropFallTime = -cloudElevation / rainDropVelocity;
							float rainDropMassToHumidity = Mathf.Min(newRainfall, rainDropFallTime * world.Data.rainDropEvapRate);
							newRainfall -= rainDropMassToHumidity;
							newHumidity += rainDropMassToHumidity;
							// This sucks heat out of the lower atmosphere in the form of latent heat of water vapor
							float energyTransfer = rainDropMassToHumidity * (upperAirTemperature * world.Data.SpecificHeatWater + world.Data.LatentHeatWaterLiquid);
							newUpperAirEnergy -= energyTransfer;
							newLowerAirEnergy += energyTransfer;
						}
						if (newRainfall > 0)
						{
							newShallowWaterMass += newRainfall;
							// No real state change here
							float energyTransfer = newRainfall * (upperAirTemperature * world.Data.SpecificHeatWater + world.Data.LatentHeatWaterLiquid);
							newShallowWaterEnergy += energyTransfer;
							newUpperAirEnergy -= energyTransfer;
						}
					}
				}
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
				float massTransfer = Mathf.Min(shallowWaterMass, Math.Min(soilFertility * world.Data.GroundWaterReplenishmentSpeed, maxGroundWater - groundWater));
				newGroundWater += massTransfer;
				newShallowWater -= massTransfer;
				float energyTransfer = massTransfer * (shallowWaterTemperature * world.Data.SpecificHeatWater + world.Data.LatentHeatWaterLiquid);
				newShallowEnergy -= energyTransfer;
				newGroundEnergy += energyTransfer;
			}
		}

		static private float GetEvaporationRate(World world, float iceCoverage, float temperature, float relativeHumidity, float airPressureInverse, float inverseEvapTemperatureRange)
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
				float energyTransfer = evapMass * (world.Data.SpecificHeatWater * shallowWaterTemperature + world.Data.LatentHeatWaterLiquid);
				newShallowWaterEnergy -= energyTransfer;
				newLowerAirEnergy += energyTransfer;
				evapotranspiration = evapMass * world.Data.LatentHeatWaterVapor;
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

					float deepWaterMixingDepth = Math.Min(shallowWaterMass, deepWaterMass);
					float heatExchange = (oceanTemperatureDeep - oceanTemperatureShallow) * deepWaterMixingDepth * world.Data.OceanTemperatureVerticalMixingSpeed;
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

		static public float GetAirTemperature(World world, float energy, float mass, float waterMass, float waterLatentHeat, float waterSpecificHeat)
		{
			return (energy - waterMass * waterLatentHeat) / (mass * world.Data.SpecificHeatAtmosphere + waterMass * waterSpecificHeat);
		}
		static public float GetAirEnergy(World world, float temperature, float mass, float waterMass, float waterLatentHeat, float waterSpecificHeat)
		{
			return temperature * (mass * world.Data.SpecificHeatAtmosphere + waterMass * waterSpecificHeat) + waterMass * waterLatentHeat;
		}

		static public float GetWaterTemperature(World world, float energy, float waterMass, float saltMass)
		{
			if (waterMass ==0)
			{
				return 0;
			}
			return Mathf.Max(0, (energy - waterMass * world.Data.LatentHeatWaterLiquid) / (waterMass * world.Data.SpecificHeatWater + saltMass * world.Data.SpecificHeatSalt));
		}
		static public float GetWaterEnergy(World world, float temperature, float waterMass, float saltMass)
		{
			return temperature * (world.Data.SpecificHeatWater * waterMass + world.Data.SpecificHeatSalt * saltMass) + waterMass * world.Data.LatentHeatWaterLiquid;
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