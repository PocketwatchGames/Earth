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
	public float coriolisParam;
	public float inverseCoriolisParam;
}

[Serializable]
public class WorldData
{
	[Header("Pressure and Wind")]
	//public float tradeWindSpeed = 12.0f; // average wind speeds around trade winds around 12 m/s
										 //	public float pressureDifferentialWindSpeed = 70.0f; // hurricane wind speeds 70 m/s
	public float PressureToVerticalWindSpeed = 0.0001f;
	public float TemperatureDifferentialToVerticalWindSpeed = 0.01f;
	public float DestinationTemperatureDifferentialVerticalWindSpeed = 0.1f;
	public float MountainUpdraftWindSpeed = 0.1f;
	public float MaxTerrainNormalForFriction = 0.25f;
	public float AirMassDiffusionSpeedHorizontal = 0.1f;
	public float AirMassDiffusionSpeedVertical = 0.1f;
	public float AirEnergyDiffusionSpeedHorizontal = 0.1f;
	public float AirEnergyDiffusionSpeedVertical = 0.1f;
	public float WindOceanFriction = 0.2f;
	public float WindIceFriction = 0.1f;
	public float WindLandFriction = 0.5f;
	public float WindAirMovementHorizontal = 0.001f;
	public float WindAirMovementVertical = 0.01f;
	public float WindHumidityMovement = 0.00002f;
	public float PressureGradientWindMultiplier = 4000;
	public float AdiabaticLapseRate = 0.0098f;
	// http://tornado.sfsu.edu/geosciences/classes/e260/Coriolis_rdg/GeostrophicApproximation.html
	// states that geostrophic wind is only realistic at middle altitudes (500M), less so at 10K and at the surface, so we reduce overall coriolis effect by 25% to account
	public float GlobalCoriolisInfluenceWind = 0.75f;
	public float GlobalCoriolisInfluenceOcean = 0.5f;

	[Header("Atmospheric Energy Cycle")]
	// atmospheric heat balance https://energyeducation.ca/encyclopedia/Earth%27s_heat_balance
	// https://en.wikipedia.org/wiki/Earth%27s_energy_budget
	public float AtmosphericHeatAbsorption = 0.297f; // total absorbed by atmosphere AFTER reflection about 30%
	public float AtmosphericHeatReflection = 0.07f; // 7% is refelcted due to atmospheric scattering 
	public float CloudIncomingAbsorptionRate = 0.06f; // 6% absorbed by clouds
	public float CloudIncomingReflectionRate = 0.50f; // 24% incoming  reflected back to space by clouds (avg, globally)
	public float EvaporativeHeatLoss = 600f; // global average = 78 watts
	// Net Back Radiation: The ocean transmits electromagnetic radiation into the atmosphere in proportion to the fourth power of the sea surface temperature(black-body radiation)
	// https://eesc.columbia.edu/courses/ees/climate/lectures/o_atm.html
	public float OceanHeatRadiation = 0.000000066f; // global average = 66 watts per m^2 of ocean
	public float OceanAirConductionWarming = 0.016f; // global avg = 16 watts per degree delta between air and ocean (global avg = 24 watts per m^2 of ocean)
	public float OceanAirConductionCooling = 0.008f; // 
	public float OceanIceConduction = 0.00001f; // small
	public float LengthOfDaySolarRadiationExponent = 0.5f;
	public float SunVectorSolarRadiationExponent = 2;
	public float AtmosphericDepthExponent = 0.5f;

	// TODO: tune these to match the science
	public float CloudMassFullAbsorption = 50.0f; // how much heat gain/loss is caused by cloud cover
	public float EnergyEmittedByUpperAtmosphere = 0.000000199f; // how fast a cell loses heat an min elevation, no cloud cover, global average = 199 watts
	public float EnergyLostThroughAtmosphereWindow = 0.00000004f; // AKA Atmospheric window global average = 40 watts
	public float CloudOutgoingAbsorptionRate = 0.1f;
	public float EnergyTrappedByGreenhouseGasses = 0.1f;
	public float HumidityHeatAbsorption = 1.0f;

	[Header("Evap, Humidity and Clouds")]
	public float DewPointTemperatureRange = 100.0f;
	public float DewPointZero = 213.0f;
	public float WaterVaporMassToAirMassAtDewPoint = 0.2f;
	public float RainfallRate = 0.01f;
	public float EvapMinTemperature = 243; // -30 celsius
	public float EvapMaxTemperature = 343; // 70 celsius
	public float EvaporationRate = 0.002f; // TODO: evaporation on earth maxes out around 2.5M per year 
	public float rainPointTemperatureMultiplier = 0.00075f; // adjustment for temperature
	public float RainDropFormationSpeedTemperature = 10f;
	public float RainDropDissapationSpeedWind = 0.00001f;
	public float rainDropDragCoefficient = 0.5f;
	public float airDensity = 1.21f;
	public float waterDensity = 997;
	public float rainDropEvapRate = 0.000001f;

	[Header("Ocean")]
	public float DeepOceanDepth = 500;
	public float WindToOceanCurrentFactor = 0.1f;
	public float OceanCurrentSpeed = 0.00001f;
	public float OceanHorizontalMixingSpeed = 0.01f;
	public float OceanUpwellingSpeed = 0.0001f;
	public float OceanTemperatureVerticalMixingSpeed = 0.0001f;
	public float SalinityVerticalMixingSpeed = 0.001f;
	public float OceanDensityPerSalinity = 0.0008f;
	public float OceanDensityPerDegree = 0.0002f;
	public float OceanDensityCurrentSpeed = 100f;
	public float FullIceCoverage = 1.0f;

	[Header("Fresh Water")]
	public float FlowSpeed = 10.0f; // mississippi travels at around 3 km/h
	public float FlowSpeedExponent = 0.25f; // arbitrary exponent to make flow speeds work at lower gradients
	public float MaxWaterTableDepth = 1000.0f; // There is still a lot of water below a kilometer, but it's generally not worth simulating
	public float MinWaterTableDepth = 0.0f;
	public float MaxSoilPorousness = 0.1f;
	public float GroundWaterReplenishmentSpeed = 10.0f;
	public float GroundWaterFlowSpeed = 0.5f;

	[Header("Ecology")]
	public float canopyGrowthRate = 100.0f;
	public float canopyDeathRate = 0.2f;
	public float freshWaterMaxAvailability = 0.1f;
	public float MinTemperatureCanopy = 223.15f;
	public float MaxTemperatureCanopy = 323.15f;

	[Header("Animals")]
	public float populationExpansionPercent = 0.2f;
	public float minPopulationDensityForExpansion = 0.1f;

	[Header("Planetary")]
	public int TicksPerYear = 8640;
	public float MetersPerTile = 400000;
	public float TropopauseElevation = 10000;
	public float BoundaryZoneElevation = 1000;
	public float stratosphereElevation = 50000;
	public float MaxTropopauseElevation = 17000f;
	public float MinTropopauseElevation = 9000f;
	public float TropopauseElevationSeason = 1000f;
	public float TemperatureLapseRate = -0.0065f;
	public float GravitationalAcceleration = 9.80665f;
	public float StaticPressure = 101325;
	public float StdTemp = 288.15f;
	public float MolarMassEarthAir = 0.0289644f;
	public float UniversalGasConstant = 8.3144598f;
	public float FreezingTemperature = 273.15f;
	public float DewPointElevationPerDegree = 67.73f;
	public float DewPointTemperaturePerRelativeHumidity = 20;

	[Header("Albedo")]
	public float AlbedoWater = 0.06f; // How much heat is reflected back by the water
	public float AlbedoIce = 0.5f; // How much heat is reflected back by the water
	public float AlbedoLand = 0.4f;
	public float AlbedoReductionSoilQuality = 0.15f;
	public float AlbedoFoliage = 0.1f;

	[Header("Specific Heats")] // in kJ/kgK
	public float SpecificHeatIce = 2.108f; // specific heat is joules to raise one degree
	public float SpecificHeatFreshWater = 4.187f; // specific heat is joules to raise one degree
	public float SpecificHeatSeaWater = 3.85f; // specific heat is joules to raise one degree
	public float SpecificHeatAtmosphere = 1.158f; // specific heat is joules to raise one degree

	[Header("Masses")]
	public float MassEarthAir = 1.29f;
	public float MassSeaWater = 1024f;
	public float MassFreshWater = 1000f;
	public float MassIce = 919f;


	[NonSerialized]
	public WindInfo[] windInfo;
	[NonSerialized]
	public float TicksPerHour;
	[NonSerialized]
	public float TicksPerSecond;
	[NonSerialized]
	public float SecondsPerTick;
	[NonSerialized]
	public float PressureExponent;
	[NonSerialized]
	public float SpecificGasConstantDryAir;
	[NonSerialized]
	public float DryAirAdiabaticLapseRate;
	[NonSerialized]
	public float InverseMetersPerTile;
	[NonSerialized]
	public float evapTemperatureRange;


	public void Init(int size)
	{
		TicksPerHour = (float)TicksPerYear / (365 * 24);
		int secondsPerYear = 365 * 24 * 60 * 60;
		TicksPerSecond = (float)TicksPerYear / secondsPerYear;
		SecondsPerTick = 1.0f / TicksPerSecond;

		windInfo = new WindInfo[size];

		canopyGrowthRate /= TicksPerYear;
		canopyDeathRate /= TicksPerYear;

		GroundWaterReplenishmentSpeed /= TicksPerYear;
		GroundWaterFlowSpeed /= TicksPerYear;

		InverseMetersPerTile = 1.0f / MetersPerTile;

		evapTemperatureRange = EvapMaxTemperature - EvapMinTemperature;
		SpecificGasConstantDryAir = UniversalGasConstant / MolarMassEarthAir;
		PressureExponent = GravitationalAcceleration * MolarMassEarthAir / (UniversalGasConstant * TemperatureLapseRate);

		DryAirAdiabaticLapseRate = AdiabaticLapseRate / SpecificHeatAtmosphere;

		for (int y = 0; y < size; y++)
		{
			float latitude = ((float)y / size) * 2 - 1.0f;
			float yaw = (float)(latitude * Math.PI * 1.5f);
			float pitch = (float)(latitude * Math.PI * 3f);
			float absSinPitch = (float)(Math.Abs(Math.Sin(pitch)));
			float cosYaw = (float)Math.Cos(yaw);
			float cosPitch = (float)Math.Cos(pitch);

			float tropopauseElevation = (1.0f - Math.Abs(latitude)) * (MaxTropopauseElevation - MinTropopauseElevation) + MinTropopauseElevation + TropopauseElevationSeason * latitude;
			windInfo[y] = new WindInfo()
			{
				latitude = latitude,
				yaw = yaw,
				tropopauseElevationMax = tropopauseElevation,
				coriolisParam = Mathf.Sin(latitude * Mathf.PI / 2),
				inverseCoriolisParam = 1.0f / Mathf.Sin(latitude * Mathf.PI / 2)
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
