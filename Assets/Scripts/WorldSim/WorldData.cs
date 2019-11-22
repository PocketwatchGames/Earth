using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public struct WindInfo
{
	public float latitude;
	public float yaw;
	public float tropopauseElevationMax;
	public float coriolisPower;
}

public class WorldData : MonoBehaviour
{
	[Header("Planetary")]
	public int TicksPerYear = 360;
	public float tileSize = 400000;
	public float carbonDioxide = 0.001f;
	public float FreezingTemperature = 273.15f;
	public float MinTemperature = 223.15f;
	public float MaxTemperature = 323.15f;
	public float planetTiltAngle = -23.5f;
	public float troposphereElevation = 10000;
	public float BoundaryZoneElevation = 1000;
	public float stratosphereElevation = 50000;
	public float MaxTropopauseElevation = 17000f;
	public float MinTropopauseElevation = 9000f;
	public float TropopauseElevationSeason = 1000f;
	public float troposphereAtmosphereContent = 0.8f;

	[Header("Ecology")]
	public float canopyGrowthRate = 100.0f;
	public float canopyDeathRate = 0.2f;
	public float freshWaterMaxAvailability = 0.1f;

	[Header("Animals")]
	public float populationExpansionPercent = 0.2f;
	public float minPopulationDensityForExpansion = 0.1f;

	[Header("Geology")]
	public float MaxElevation = 10000.0f;
	public float MinElevation = -10000.0f;

	[Header("Water")]
	public float FlowSpeed = 10.0f; // mississippi travels at around 3 km/h
	public float FlowSpeedExponent = 0.25f; // arbitrary exponent to make flow speeds work at lower gradients
	public float MaxWaterTableDepth = 1000.0f; // There is still a lot of water below a kilometer, but it's generally not worth simulating
	public float MinWaterTableDepth = 0.0f;
	public float MaxSoilPorousness = 0.1f;
	public float GroundWaterReplenishmentSpeed = 10.0f;
	public float GroundWaterFlowSpeed = 0.5f;

	[Header("Atmosphere")]
	public float tradeWindSpeed = 12.0f; // average wind speeds around trade winds around 12 m/s
										 //	public float pressureDifferentialWindSpeed = 70.0f; // hurricane wind speeds 70 m/s
	public float pressureToHorizontalWindSpeed = 0.01f;
	public float pressureToVerticalWindSpeed = 0.01f;
	public float heatLossPreventionCarbonDioxide = 200;
	public float temperatureLapseRate = -0.0065f;
	public float MassEarthAir = 1.29f;
	public float MassSeaWater = 1024f;
	public float LowerAirDensity = 1.2f;
	public float UpperAirDensity = 0.4f;
	public float massWindMovement = 0.0001f;
	public float windInertia = 0.0f;
	public float StaticPressure = 101325;
	public float StdTemp = 288.15f;
	public float GravitationalAcceleration = 9.80665f;
	public float MolarMassEarthAir = 0.0289644f;
	public float UniversalGasConstant = 8.3144598f;
	public float airDispersalSpeed = 0.01f;
	public float humidityDispersalSpeed = 0.01f;

	// atmospheric heat balance https://energyeducation.ca/encyclopedia/Earth%27s_heat_balance
	// https://en.wikipedia.org/wiki/Earth%27s_energy_budget
	public float localSunHeat = 5; // sun can add about 5 degrees celsius
	public float SolarRadiation = 118; // extraterrestrial solar radiation // https://en.wikipedia.org/wiki/Sunlight (1367 w/m^2) *seconds per day (86400)
	public float AtmosphericHeatAbsorption = 0.23f; // total absorbed by atmosphere about 23%
	public float AtmosphericHeatReflection = 0.23f;
	public float EvaporativeHeatLoss = 0.065f; // global average = 78 watts -- TODO: get this in line with average evaportaion (2.5M per year)
	public float OceanHeatRadiation = 0.00001021f; // global average = 66 watts
	public float OceanAirConduction = 1.3824f; // global avg = 16 watts per degree delta between air and ocean
	public float AlbedoWater = 0.06f; // How much heat is reflected back by the water
	public float AlbedoIce = 0.5f; // How much heat is reflected back by the water
	public float AlbedoLand = 0.4f;
	public float AlbedoReductionSoilQuality = 0.15f;
	public float AlbedoFoliage = 0.1f;
	public float AtmosphericHeatLossToSpace = 0.000001024f; // how fast a cell loses heat an min elevation, no cloud cover, global average = 199 watts
	public float LandRadiation = 0.5f;
	public float SpecificHeatSeaWater = 3.85f; // specific heat is joules to raise one degree
	public float SpecificHeatAtmosphere = 1.158f; // specific heat is joules to raise one degree
	public float StratosphereMass = 2583;
	public float TroposphereMass = 7749;

	[Header("Water Vapor")]
	public float EvapRateWind = 1.0f;
	public float EvapRateTemperature = 1.0f;
	public float dewPointZero = 213.0f;
	public float dewPointTemperatureRange = 100.0f;
	public float dewPointRange = 0.06f;
	public float RainfallRate = 10.0f;
	public float cloudContentFullAbsorption = 5.0f; // how much heat gain/loss is caused by cloud cover
	public float cloudAbsorptionRate = 0.06f; // 6% absorbed by clouds
	public float cloudReflectionRate = 0.20f; // 20% reflected back to space
	public float evapMinTemperature = 253; // -20 celsius
	public float evapMaxTemperature = 413; // 140 celsius
	public float evapTemperatureRange;
	public float rainPointTemperatureMultiplier = 0.00075f; // adjustment for temperature
	public float humidityLossFromWind = 0.1f;
	public float cloudMovementFromWind = 20.0f;
	public float cloudElevationDeltaSpeed = 10.0f;
	public float windVerticalCloudSpeedMultiplier = 100000;

	[Header("Ocean")]
	public float maxIce = 2.0f;
	public float iceFreezeRate = 10.0f;
	public float iceMeltRate = 10.0f;
	public float iceMeltRadiationRate = 0.0001f;
	public float DeepOceanDepth = 500;
	public float WindToOceanCurrentFactor = 0.1f;
	public float OceanEnergyCurrentSpeed = 0.001f;
	public float OceanSalinityCurrentSpeed = 0.01f;
	public float OceanHorizontalMixingSpeed = 0.01f;
	public float OceanUpwellingSpeed = 0.0001f;
	public float OceanTemperatureVerticalMixingSpeed = 0.0001f;
	public float SalinityVerticalMixingSpeed = 0.001f;
	public float OceanDensityPerSalinity = 1.0f;
	public float OceanDensityPerTemperature = 10.0f;

	[NonSerialized]
	public WindInfo[] windInfo;
	[NonSerialized]
	public float TicksPerHour;
	[NonSerialized]
	public float SecondsPerTick;
	[NonSerialized]
	public float PressureExponent;


	public void Init(World.SimFeature activeFeatures, int size)
	{
		TicksPerHour = TicksPerYear * (365 * 24);
		int secondsPerYear = 365 * 24 * 60 * 60;
		SecondsPerTick = (float)secondsPerYear / TicksPerYear;

		windInfo = new WindInfo[size];

		canopyGrowthRate /= TicksPerYear;
		canopyDeathRate /= TicksPerYear;

		EvapRateWind /= TicksPerYear;
		EvapRateTemperature /= TicksPerYear;
		RainfallRate /= TicksPerYear;
		FlowSpeed /= (tileSize / 1000) * TicksPerHour; // mississippi travels at around 3 km/h
		GroundWaterReplenishmentSpeed /= TicksPerYear;
		GroundWaterFlowSpeed /= TicksPerYear;
		planetTiltAngle = Mathf.Deg2Rad * planetTiltAngle;

		evapTemperatureRange = evapMaxTemperature - evapMinTemperature;
		humidityLossFromWind *= SecondsPerTick / tileSize / TicksPerYear;
		cloudMovementFromWind *= SecondsPerTick / tileSize / TicksPerYear;
		cloudElevationDeltaSpeed /= TicksPerYear;
		windVerticalCloudSpeedMultiplier /= TicksPerYear;
		PressureExponent = GravitationalAcceleration * MolarMassEarthAir / (UniversalGasConstant * temperatureLapseRate);
		iceFreezeRate /= TicksPerYear;
		iceMeltRate /= TicksPerYear;

		if (!activeFeatures.HasFlag(World.SimFeature.HumidityMovesOnWind))
		{
			humidityLossFromWind = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.TemperatureMovesOnWind)) {
		}
		if (!activeFeatures.HasFlag(World.SimFeature.Evaporation)) {
			EvapRateTemperature = 0;
			EvapRateWind = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.EvaporationFromTemperature)) {
			EvapRateTemperature = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.EvaporationFromWind)) {
			EvapRateWind = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.HumidityToCloud)) {
		}
		if (!activeFeatures.HasFlag(World.SimFeature.HumidityToCloudFromWind)) {

		}
		if (!activeFeatures.HasFlag(World.SimFeature.HumidityToCloudFromAbsorption)) {
		}
		if (!activeFeatures.HasFlag(World.SimFeature.Rainfall)) {
			RainfallRate = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.CloudShadesLand)) { }
		if (!activeFeatures.HasFlag(World.SimFeature.Ice)) {
			iceFreezeRate = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.GroundWaterAbsorption)) {
			GroundWaterReplenishmentSpeed = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.GroundWaterFlow)) {
			GroundWaterFlowSpeed = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.TradeWinds)) {
			tradeWindSpeed = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.WindCoriolisForce )) {
		}

		for (int y = 0; y < size; y++)
		{
			float latitude = ((float)y / size) * 2 - 1.0f;
			float yaw = (float)(latitude * Math.PI * 1.5f);
			float pitch = (float)(latitude * Math.PI * 3f);
			float absSinPitch = (float)(Math.Abs(Math.Sin(pitch)));
			float cosYaw = (float)Math.Cos(yaw);
			float cosPitch = (float)Math.Cos(pitch);
			Vector3 wind = Vector3.zero;
			if (latitude < 0.3333 && latitude > -0.3333)
			{
				wind.x = absSinPitch * -cosYaw;
				wind.y = absSinPitch;
			}
			else if (latitude < 0.667 && latitude > -0.667)
			{
				wind.x = absSinPitch * -cosYaw;
				wind.y = absSinPitch;
			}
			else
			{
				wind.x = absSinPitch * cosYaw;
				wind.y = absSinPitch;
			}
			wind.z = cosPitch;
			wind *= tradeWindSpeed;

			float tropopauseElevation = (1.0f - Math.Abs(latitude)) * (MaxTropopauseElevation - MinTropopauseElevation) + MinTropopauseElevation + TropopauseElevationSeason * latitude;
			windInfo[y] = new WindInfo()
			{
				latitude = latitude,
				yaw = yaw,
				tropopauseElevationMax = tropopauseElevation,
				coriolisPower = -Mathf.Sin(latitude*Mathf.PI/2),
			};
		}

	}
}

public partial class World
{
	public WorldData Data;
	public const int MaxSpecies = 4;

	[Flags]
	public enum SimFeature : uint
	{
		HumidityMovesOnWind = 1 << 1,
		TemperatureMovesOnWind = 1 << 2,
		Evaporation = 1 << 3,
		EvaporationFromTemperature = 1 << 4,
		EvaporationFromWind = 1 << 5,
		HumidityToCloud = 1 << 6,
		HumidityToCloudFromWind = 1 << 7,
		HumidityToCloudFromAbsorption = 1 << 8,
		Rainfall = 1 << 9,
		CloudShadesLand = 1 << 10,
		Ice = 1 << 11,
		GroundWaterAbsorption = 1 <<12,
		GroundWaterFlow = 1 << 13,
		TradeWinds = 1 << 14,
		PressureWinds = 1 << 15,
		WindCoriolisForce = 1 << 16,
		All = ~0u,
	}



}
