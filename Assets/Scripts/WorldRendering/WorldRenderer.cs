﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Sim;
using Unity.Profiling;

public static class WorldExtensions {
	public static bool IsSet(this WorldComponent.Layers self, WorldComponent.Layers flag)
	{
		return (self & flag) == flag;
	}
}

public partial class WorldComponent {
	Vector3[] landVerts;
	Color[] landCols;
	int[] landTris;

	Vector3[] cloudVerts;
	Color[] cloudCols;
	int[] cloudTris;

	Vector3[] oceanVerts;
	Color[] oceanCols;
	int[] oceanTris;

	static ProfilerMarker _ProfileUpdateMesh1 = new ProfilerMarker("_ProfileUpdateMesh1");
	static ProfilerMarker _ProfileUpdateMesh2 = new ProfilerMarker("_ProfileUpdateMesh2");
	static ProfilerMarker _ProfileUpdateMesh3 = new ProfilerMarker("_ProfileUpdateMesh3");

	static ProfilerMarker _ProfileRenderLock = new ProfilerMarker("Render Lock");
	static ProfilerMarker _ProfileRenderConstruct = new ProfilerMarker("Render Construct");
	static ProfilerMarker _ProfileRenderUpdateMesh = new ProfilerMarker("Render Update Mesh");


	struct CVP {
		public Color Color;
		public float Value;
		public CVP(Color c, float v) { Color = c; Value = v; }
	};
	Color Lerp(List<CVP> colors, float value)
	{
		for (int i = 0; i < colors.Count - 1; i++)
		{
			if (value < colors[i + 1].Value)
			{
				return Color.Lerp(colors[i].Color, colors[i + 1].Color, (value - colors[i].Value) / (colors[i + 1].Value - colors[i].Value));
			}
		}
		return colors[colors.Count - 1].Color;
	}
	Color Lerp(float value, List<float> values, List<Color> colors)
	{
		for (int i = 0; i < values.Count - 1; i++)
		{
			if (value < values[i + 1])
			{
				return Color.Lerp(colors[i], colors[i + 1], (value - values[i]) / (values[i + 1] - values[i]));
			}
		}
		return colors[colors.Count - 1];
	}

	float stateLerpT = 0;
	public const int MaxPlateColors = 12;
	Color[] PlateColors = new Color[MaxPlateColors]
	{
		new Color(1.0f,0,0),
		new Color(0,1.0f,0),
		new Color(0,0,1.0f),
		new Color(1.0f,1,0),
		new Color(1.0f,0,1),
		new Color(0,1.0f,1),
		new Color(1,0.5f,0),
		new Color(0.5f,1,0),
		new Color(1,0,0.5f),
		new Color(0.5f,0,1),
		new Color(0,1,0.5f),
		new Color(0,0.5f,1),
	};

	void CreateWorldMesh()
	{
		landVerts = new Vector3[World.Size * World.Size];
		landCols = new Color[World.Size * World.Size];
		landTris = new int[(World.Size - 1) * (World.Size - 1) * 6];

		cloudVerts = new Vector3[World.Size * World.Size];
		cloudCols = new Color[World.Size * World.Size];
		cloudTris = new int[(World.Size - 1) * (World.Size - 1) * 6];

		oceanVerts = new Vector3[World.Size * World.Size];
		oceanCols = new Color[World.Size * World.Size];
		oceanTris = new int[(World.Size - 1) * (World.Size - 1) * 6];


		for (int i = 0; i < World.Size; i++)
		{
			for (int j = 0; j < World.Size; j++)
			{
				landVerts[i * World.Size + j] = new Vector3(j, i, 0);
				landCols[i * World.Size + j] = Color.black;
				cloudVerts[i * World.Size + j] = new Vector3(j, i, 0);
				cloudCols[i * World.Size + j] = new Color(0, 0, 0, 0);
				oceanVerts[i * World.Size + j] = new Vector3(j, i, 0);
				oceanCols[i * World.Size + j] = new Color(0, 0, 0, 0);
			}
		}
		int index = 0;
		for (int i = 0; i < World.Size - 1; i++)
		{
			for (int j = 0; j < World.Size - 1; j++)
			{
				landTris[index + 0] = i * World.Size + j;
				landTris[index + 1] = (i + 1) * World.Size + j;
				landTris[index + 2] = (i + 1) * World.Size + (j + 1);
				landTris[index + 3] = i * World.Size + j;
				landTris[index + 4] = (i + 1) * World.Size + (j + 1);
				landTris[index + 5] = i * World.Size + (j + 1);

				cloudTris[index + 0] = i * World.Size + j;
				cloudTris[index + 1] = (i + 1) * World.Size + j;
				cloudTris[index + 2] = (i + 1) * World.Size + (j + 1);
				cloudTris[index + 3] = i * World.Size + j;
				cloudTris[index + 4] = (i + 1) * World.Size + (j + 1);
				cloudTris[index + 5] = i * World.Size + (j + 1);

				oceanTris[index + 0] = i * World.Size + j;
				oceanTris[index + 1] = (i + 1) * World.Size + j;
				oceanTris[index + 2] = (i + 1) * World.Size + (j + 1);
				oceanTris[index + 3] = i * World.Size + j;
				oceanTris[index + 4] = (i + 1) * World.Size + (j + 1);
				oceanTris[index + 5] = i * World.Size + (j + 1);

				index += 6;
			}
		}

		LandMesh.mesh.vertices = landVerts;
		LandMesh.mesh.triangles = landTris;
		LandMesh.mesh.colors = landCols;

		CloudMesh.mesh.vertices = cloudVerts;
		CloudMesh.mesh.triangles = cloudTris;
		CloudMesh.mesh.colors = cloudCols;

		OceanMesh.mesh.vertices = oceanVerts;
		OceanMesh.mesh.triangles = oceanTris;
		OceanMesh.mesh.colors = oceanCols;


	}


	public void UpdateMesh(Layers showLayers, float dt)
	{
		_ProfileRenderLock.Begin();
		lock (World.DrawLock)
		{
			World.LastRenderStateIndex = World.CurRenderStateIndex;
			World.CurRenderStateIndex = World.CurStateIndex;
		}
		_ProfileRenderLock.End();

		_ProfileRenderConstruct.Begin();
		ref var state = ref World.States[World.CurRenderStateIndex];
		ref var lastState = ref World.States[World.LastRenderStateIndex];
		stateLerpT = Math.Max(1.0f, dt * 10);

		var elevationColors = new List<CVP> { new CVP(Color.black, -2000), new CVP(new Color(0.5f,0.5f,0.5f), 1000), new CVP(Color.white, 4000) };
		var oceanElevationColors = new List<CVP> {
								new CVP(new Color(0.4f,0.3f,1.0f), 0),
								new CVP(Color.blue, 1000),
								new CVP(Color.black, MaxDepth)
		};
		var landTemperatureColors = new List<CVP>() { new CVP(new Color(0, 0.4f, 1.0f), -30 + World.Data.FreezingTemperature), new CVP(Color.white, World.Data.FreezingTemperature + 20), new CVP(new Color(1.0f, 0.4f, 0), 70 + World.Data.FreezingTemperature) };
		var oceanTemperatureColors = new List<CVP>() { new CVP(new Color(0.0f, 0.0f, 0.6f), World.Data.FreezingTemperature), new CVP(new Color(0.0f, 1.0f, 1.0f), 60 + World.Data.FreezingTemperature) };

		Color oceanColor;
		Color color;

		float inverseElevationRange = 1.0f / (MaxElevation - MinElevation);
		float inverseWorldSize = 1.0f / World.Size;
		float inverseFullIceCoverage = 1.0f / (World.Data.MassIce * World.Data.FullIceCoverage);
		float inverseDewPointTemperatureRange = 1.0f / World.Data.DewPointTemperatureRange;
		float inverseMaxEvapMass = World.Data.TicksPerYear / (MaxEvap * World.Data.MassWater);
		float inverseMaxRainfall = World.Data.TicksPerYear / (maxRainfall * World.Data.MassWater);

		float maxSoilPorousness = 0.1f;
		float maxWaterTableDepth = 1000;
		float inverseMaxGroundWater = 1.0f / (World.Data.MassWater * maxSoilPorousness * maxWaterTableDepth);
		for (int y = 0; y < World.Size; y++)
		{
			float latitude = World.GetLatitude(y);
			for (int x = 0; x < World.Size; x++)
			{
				int index = World.GetIndex(x, y);

				float elevation = state.Elevation[index];
				float waterDepth = state.WaterDepth[index];
				float iceMass = state.IceMass[index];
				float elevationOrSeaLevel = state.Elevation[index] + waterDepth + iceMass / World.Data.MassIce;
				bool isOcean = World.IsOcean(waterDepth);
				float normalizedElevation = (elevation - MinElevation) * inverseElevationRange;
				float deepOceanMass = state.DeepWaterMass[index];
				float shallowOceanMass = state.ShallowWaterMass[index];

				landVerts[index].z = -elevation * ElevationScale;
				oceanVerts[index].z = (elevation+state.WaterAndIceDepth[index]- waterDepthThreshold) * ElevationScale;

				// Base color

				color = Lerp(elevationColors, elevation);
				oceanColor = Lerp(oceanElevationColors, state.WaterDepth[index]);
				if (elevationOrSeaLevel > 0)
				{
					oceanColor = Color.Lerp(oceanColor, Color.white, elevationOrSeaLevel / MaxElevation);
				} else
				{
					oceanColor = Color.Lerp(oceanColor, Color.black, elevationOrSeaLevel / MinElevation);
				}
				if (showLayers.IsSet(Layers.SoilFertility))
				{
					color = Color.Lerp(color, new Color(0.8f, 0.4f, 0.1f), Mathf.Lerp(lastState.SoilFertility[index], state.SoilFertility[index], stateLerpT));
				}

				if (showLayers.IsSet(Layers.Vegetation))
				{
					float canopyAmount = Mathf.Clamp01(Mathf.Lerp(lastState.Canopy[index], state.Canopy[index], stateLerpT) / DisplayMaxCanopy);
					color = Color.Lerp(color, Color.green, canopyAmount);
				}


				if (showLayers.IsSet(Layers.TemperatureSubtle))
				{
					color = color * 0.5f + color * Lerp(landTemperatureColors, state.LowerAirTemperature[index]) * 0.5f;
					color = Color.Lerp(color, Color.blue, state.GroundWater[index] * inverseMaxGroundWater);
					oceanColor = oceanColor * 0.5f + oceanColor * Lerp(oceanTemperatureColors, state.ShallowWaterTemperature[index]) * 0.5f;
				}

				if (waterDepth > 0)
				{
					color = Color.Lerp(color, oceanColor, Mathf.Pow(Mathf.Clamp01(waterDepth / World.Data.FullWaterCoverage), 0.25f));
				}


				if (iceMass > 0)
				{
					float iceCoverage = Mathf.Clamp01(iceMass * inverseFullIceCoverage);
					oceanColor = Color.Lerp(oceanColor, new Color(0.4f, 0.6f, 1.0f), 0.95f * iceCoverage);
					color = Color.Lerp(color, new Color(0.4f, 0.5f, 1.0f), 0.95f * iceCoverage);
				}



				if (showLayers.IsSet(Layers.LowerAirTemperature))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						(state.LowerAirTemperature[index]-DisplayMinTemperature) / (DisplayMaxTemperature - DisplayMinTemperature));
				}
				else if (showLayers.IsSet(Layers.UpperAirTemperature))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						(state.UpperAirTemperature[index] - DisplayMinTemperature) / (DisplayMaxTemperature - DisplayMinTemperature));
				}
				else if (showLayers.IsSet(Layers.LandTemperature))
				{
					float landTemperature = Atmosphere.GetLandTemperature(World, state.LandEnergy[index], state.GroundWater[index], state.SoilFertility[index], Mathf.Clamp01(state.Canopy[index] / World.Data.FullCanopyCoverage));
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						(landTemperature - DisplayMinTemperature) / (DisplayMaxTemperature - DisplayMinTemperature));
				}
				else if (showLayers.IsSet(Layers.Evaporation))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						state.Evaporation[index] * inverseMaxEvapMass);
				}
				else if (showLayers.IsSet(Layers.WindVert))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.blue, -DisplayMaxVerticalWindSpeed),
											new CVP(Color.black, 0.0f),
											new CVP(Color.red, DisplayMaxVerticalWindSpeed) },
						state.LowerWind[index].z);
				}
				else if (showLayers.IsSet(Layers.Rainfall))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						state.Rainfall[index] * inverseMaxRainfall);
				}
				else if (showLayers.IsSet(Layers.HeatAbsorbed))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						state.EnergyAbsorbed[index]/MaxEnergyAbsorbed);
				}
				else if (showLayers.IsSet(Layers.LowerAirPressure))
				{
					float p = (state.LowerAirPressure[index] - minPressure) / (maxPressure - minPressure);
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						p);
				}
				else if (showLayers.IsSet(Layers.UpperAirPressure))
				{
					float p = (state.UpperAirPressure[index] - minPressure) / (maxPressure - minPressure);
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						p);
				}
				else if (showLayers.IsSet(Layers.WaterVapor))
				{
					float humidityDisplay = state.Humidity[index] / maxHumidity;
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
											humidityDisplay);
				}
				else if (showLayers.IsSet(Layers.RelativeHumidity))
				{
					float relativeHumidity = Mathf.Clamp01(Atmosphere.GetRelativeHumidity(World, state.LowerAirTemperature[index], state.Humidity[index], state.LowerAirMass[index], inverseDewPointTemperatureRange));
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
											relativeHumidity);

				}
				else if (showLayers.IsSet(Layers.Rainfall))
				{
					oceanColor = color = Color.Lerp(Color.black, Color.blue, Math.Min(1.0f, state.Rainfall[index] / maxRainfall));
				}
				else if (showLayers.IsSet(Layers.GroundWater))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
											Math.Min(1.0f, state.GroundWater[index] * inverseMaxGroundWater));
				}
				else if (showLayers.IsSet(Layers.WaterTableDepth))
				{
					oceanColor = color = Color.Lerp(Color.black, Color.white, (state.WaterTableDepth[index] - World.Data.MinWaterTableDepth) / (World.Data.MaxWaterTableDepth - World.Data.MinWaterTableDepth));
				}
				else if (showLayers.IsSet(Layers.Elevation))
				{
					oceanColor = color = Lerp(
						new List<CVP> {
											new CVP(Color.black, MinElevation),
											new CVP(new Color(0, 0, 0.5f), (-MinElevation)/2+MinElevation),
											new CVP(new Color(0.4f, 0.4f, 1.0f), 0),
											new CVP(Color.yellow, 1),
											new CVP(new Color(0.5f, 0.5f, 0.5f), (MaxElevation)/4),
											new CVP(Color.white, MaxElevation) },
						elevation);
				}
				else if (showLayers.IsSet(Layers.Plates))
				{
					float elevationT = elevation / MaxElevation;
					color = Color.Lerp(Color.black, Color.white, Math.Sign(elevation) * (float)Math.Sqrt(Math.Abs(elevationT)) / 2 + 0.5f);
					oceanColor = color = Color.Lerp(color, PlateColors[state.Plate[index] % MaxPlateColors], 0.25f);
				}

				if (showLayers.IsSet(Layers.OceanTemperatureShallow))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						(state.ShallowWaterTemperature[index] - DisplayMinTemperature) / (DisplayMaxTemperature - DisplayMinTemperature));
				}
				else if (showLayers.IsSet(Layers.OceanTemperatureDeep))
				{
					float deepWaterTemperature = Atmosphere.GetWaterTemperature(World, state.DeepWaterEnergy[index], Math.Max(0, state.DeepWaterMass[index]), state.DeepSaltMass[index]);
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
						(deepWaterTemperature - DisplayMinTemperature) / (DisplayMaxTemperature - DisplayMinTemperature));
				}
				else if (showLayers.IsSet(Layers.OceanSalinityShallow))
				{
					float s = 0;
					if (shallowOceanMass > 0)
					{
						s = (state.ShallowSaltMass[index] / shallowOceanMass - MinSalinity) / (MaxSalinity - MinSalinity);
					}
					//					s = state.WaterDepth[index] / 2000;
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
										s);
				}
				else if (showLayers.IsSet(Layers.OceanSalinityDeep))
				{
					float s = 0;
					if (deepOceanMass > 0)
					{
						s = (state.DeepSaltMass[index] / deepOceanMass - MinSalinity) / (MaxSalinity - MinSalinity);
					}
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.white, 0.1667f),
											new CVP(Color.blue, 0.3333f),
											new CVP(Color.green, 0.5f),
											new CVP(Color.yellow, 0.6667f),
											new CVP(Color.red, 0.8333f),
											new CVP(Color.magenta, 1) },
										s);
				}


				float sunAngle;
				Vector3 sunVector;
				Sim.Atmosphere.GetSunVector(World, state.PlanetTiltAngle, state.Ticks, latitude, (float)x * inverseWorldSize, out sunAngle, out sunVector);
				float sunIntensity = Mathf.Pow(Mathf.Max(0, sunAngle), 0.5f);
				color *= sunIntensity * 0.25f + 0.75f;
				oceanColor *= sunIntensity * 0.25f + 0.75f;

				landCols[index] = color;
				oceanCols[index] = oceanColor;

				oceanVerts[index].z = -Mathf.Max((waterDepth > 0) ? (elevation + state.WaterDepth[index]-waterDepthThreshold + iceMass / World.Data.MassIce) : Mathf.Min(elevation - 1, 0)) * ElevationScale;

				if (showLayers.IsSet(Layers.CloudCoverage))
				{
					var cloudColor = Color.Lerp(Color.white, Color.black, Mathf.Clamp01(state.RainDropMass[index] / (state.CloudMass[index] * maxCloudColor))) * (float)Math.Sqrt(Mathf.Clamp01(state.CloudMass[index] / World.Data.CloudMassFullAbsorption)) * 0.9f;
					cloudCols[index] = cloudColor;
					cloudVerts[index].z = -Mathf.Max(elevation+ state.WaterDepth[index] + 1) * ElevationScale;
				}

				if (showLayers.IsSet(Layers.SurfaceAirWind))
				{
					var wind = state.LowerWind[index];
					UpdateWindArrow(state, x, y, index, wind, DisplayMaxWindSpeedLowerAtm);
				}
				else if (showLayers.IsSet(Layers.UpperAirWind))
				{
					var wind = state.UpperWind[index];
					UpdateWindArrow(state, x, y, index, wind, DisplayMaxWindSpeedUpperAtm);
				}
				else if (showLayers.IsSet(Layers.OceanCurrentShallow))
				{
					var current = state.ShallowWaterCurrent[index];
					UpdateWindArrow(state, x, y, index, current, DisplayMaxWindSpeedSurfaceWater);
				}
				else if (showLayers.IsSet(Layers.OceanCurrentDeep))
				{
					var current = state.DeepWaterCurrent[index];
					UpdateWindArrow(state, x, y, index, current, DisplayMaxWindSpeedDeepWater);
				}

			}
		}
		_ProfileRenderConstruct.End();

		_ProfileRenderUpdateMesh.Begin();
		LandMesh.mesh.vertices = landVerts;
		LandMesh.mesh.colors = landCols;

		CloudMesh.mesh.vertices = cloudVerts;
		CloudMesh.mesh.colors = cloudCols;

		OceanMesh.mesh.vertices = oceanVerts;
		OceanMesh.mesh.colors = oceanCols;

		CloudMesh.gameObject.SetActive(showLayers.IsSet(Layers.CloudCoverage));
		OceanMesh.gameObject.SetActive(showLayers.IsSet(Layers.Water));
		_ProfileRenderUpdateMesh.End();
	}

	private void UpdateWindArrow(World.State state, int x, int y, int index, Vector3 wind, float maxSpeed)
	{
		float maxWindSpeedVertical = maxSpeed / 10;
		Color windColor;
		if (wind.z < 0)
		{
			windColor = Color.Lerp(Color.white, Color.blue, -wind.z / maxWindSpeedVertical);
		}
		else
		{
			windColor = Color.Lerp(Color.white, Color.red, wind.z / maxWindSpeedVertical);
		}
		float windXYSpeed = Mathf.Sqrt(wind.x * wind.x + wind.y * wind.y);
		float windAngle = Mathf.Atan2(wind.y, wind.x);

		_windArrows[index].transform.position = new Vector3(x, y, -(state.Elevation[index]+state.WaterDepth[index] + 1000) * ElevationScale);
		_windArrows[index].transform.localScale = Vector3.one * Mathf.Clamp01(windXYSpeed / maxSpeed);
		_windArrows[index].transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * windAngle);
		_windArrows[index].GetComponentInChildren<MeshRenderer>().material.color = windColor;
	}
}