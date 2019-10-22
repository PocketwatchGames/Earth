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
	public Vector3 tradeWind;
}

public class SimData
{
	public int TicksPerYear = 360;
	public float TicksPerHour;
	public float SecondsPerTick;

	public float tileSize = 400000;

	public float canopyGrowthRate = 100.0f;
	public float canopyDeathRate = 0.2f;
	public float freshWaterMaxAvailability = 0.1f;
	public float populationExpansionPercent = 0.2f;
	public float minPopulationDensityForExpansion = 0.1f;
	public float carbonDioxide = 0.001f;

	public float MaxElevation = 10000.0f;
	public float MinElevation = -10000.0f;
	public float EvapRateWind = 1.0f;
	public float EvapRateTemperature = 1.0f;
	public float tradeWindSpeed = 12.0f; // average wind speeds around trade winds around 12 m/s
	public float pressureDifferentialWindSpeed = 70.0f; // hurricane wind speeds 70 m/s
	public float RainfallRate = 10.0f;
	public float FlowSpeed = 10.0f; // mississippi travels at around 3 km/h
	public float FlowSpeedExponent = 0.25f; // arbitrary exponent to make flow speeds work at lower gradients
	public float MaxWaterTableDepth = 1000.0f; // There is still a lot of water below a kilometer, but it's generally not worth simulating
	public float MinWaterTableDepth = 0.0f;
	public float MaxSoilPorousness = 0.1f;
	public float GroundWaterReplenishmentSpeed = 10.0f;
	public float GroundWaterFlowSpeed = 0.5f;
	public float FreezingTemperature = 273.15f;
	public float MinTemperature = 223.15f;
	public float MaxTemperature = 323.15f;
	public float planetTiltAngle = -23.5f;
	public float evapMinTemperature = 253; // -20 celsius
	public float evapMaxTemperature = 413; // 140 celsius
	public float evapTemperatureRange;
	public float localSunHeat = 5; // sun can add about 10 degrees farenheit


	public float heatLoss = 0.00015f; // how fast a cell loses heat an min elevation, no cloud cover
	public float heatGainFromSun = 20.0f; // how fast a cell gains heat with no cloud cover, modified by sun height
	public float heatReflectionWater = 0.0f; // How much heat is reflected back by the water
	public float heatReflectionIce = 0.5f; // How much heat is reflected back by the water
	public float HeatReflectionLand = 0.1f;
	public float heatLossPreventionCarbonDioxide = 200;
	public float cloudContentFullAbsorption = 5.0f; // how much heat gain/loss is caused by cloud cover
	public float cloudAbsorptionRate = 0.06f; // 6% absorbed by clouds
	public float cloudReflectionRate = 0.20f; // 20% reflected back to space
	public float troposphereElevation = 10000;
	public float stratosphereElevation = 50000;
	public float troposphereAtmosphereContent = 0.8f;
	public float dewPointZero = 213.0f;
	public float dewPointTemperatureRange = 100.0f;
	public float dewPointRange = 0.06f;
	public float rainPointTemperatureMultiplier = 0.00075f; // adjustment for temperature
	public float temperatureLapseRate = -0.0065f;
	public float EvaporativeCoolingRate = 1.0f;
	public float temperatureLossFromWind = 20.0f;
	public float humidityLossFromWind = 0.1f;
	public float cloudMovementFromWind = 20.0f;
	public float windInertia = 0.0f;
	public float cloudElevationDeltaSpeed = 10.0f;
	public float windVerticalCloudSpeedMultiplier = 100000;
	public float StaticPressure = 101325;
	public float StdTemp = 288.15f;
	public float StdTempLapseRate = -0.0065f;
	public float GravitationalAcceleration = 9.80665f;
	public float MolarMassEarthAir = 0.0289644f;
	public float UniversalGasConstant = 8.3144598f;
	public float PressureExponent;
	public float verticalWindPressureAdjustment = 1;
	public float temperatureGradientPressureAdjustment = 1;
	public float upperAtmosphereCoolingRate = 0.0f;
	public float MaxTropopauseElevation = 17000f;
	public float MinTropopauseElevation = 9000f;
	public float TropopauseElevationSeason = 1000f;
	public float maxIce = 2.0f;
	public float iceFreezeRate = 10.0f;
	public float iceMeltRate = 10.0f;
	public float windElevationFactor = 1.0f / 2000;
	public float maxWindFrictionElevation = 1000;

	public WindInfo[] windInfo;


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
		pressureDifferentialWindSpeed /= StaticPressure; // hurricane wind speeds 70 m/s
		RainfallRate /= TicksPerYear;
		FlowSpeed /= (tileSize / 1000) * TicksPerHour; // mississippi travels at around 3 km/h
		GroundWaterReplenishmentSpeed /= TicksPerYear;
		GroundWaterFlowSpeed /= TicksPerYear;
		planetTiltAngle = Mathf.Deg2Rad * planetTiltAngle;

		evapTemperatureRange = evapMaxTemperature - evapMinTemperature;
		heatGainFromSun /= TicksPerYear; // how fast a cell gains heat with no cloud cover, modified by sun height
		temperatureLossFromWind *= SecondsPerTick / tileSize / TicksPerYear;
		humidityLossFromWind *= SecondsPerTick / tileSize / TicksPerYear;
		cloudMovementFromWind *= SecondsPerTick / tileSize / TicksPerYear;
		cloudElevationDeltaSpeed /= TicksPerYear;
		windVerticalCloudSpeedMultiplier /= TicksPerYear;
		PressureExponent = GravitationalAcceleration * MolarMassEarthAir / (UniversalGasConstant * StdTempLapseRate);
		upperAtmosphereCoolingRate /= TicksPerYear;
		iceFreezeRate /= TicksPerYear;
		iceMeltRate /= TicksPerYear;

		if (!activeFeatures.HasFlag(World.SimFeature.HumidityMovesOnWind))
		{
			humidityLossFromWind = 0;
		}
		if (!activeFeatures.HasFlag(World.SimFeature.TemperatureMovesOnWind)) {
			temperatureLossFromWind = 0;
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
		if (!activeFeatures.HasFlag(World.SimFeature.PressureWinds )) {
			pressureDifferentialWindSpeed = 0;
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

			float coriolisPower = (float)Math.Sqrt(Math.Abs(latitude)) * (float)Math.PI / 2; ;
			coriolisPower *= (latitude > 0) ? 1 : -1;
			float tropopauseElevation = (1.0f - Math.Abs(latitude)) * (MaxTropopauseElevation - MinTropopauseElevation) + MinTropopauseElevation + TropopauseElevationSeason * latitude;

			windInfo[y] = new WindInfo()
			{
				latitude = latitude,
				yaw = yaw,
				tropopauseElevationMax = tropopauseElevation,
				coriolisPower = coriolisPower,
				tradeWind = wind
			};
		}

	}
}

public partial class World
{
	public SimData Data;
	public const int MaxSpecies = 4;
	public SimFeature ActiveFeatures;

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
