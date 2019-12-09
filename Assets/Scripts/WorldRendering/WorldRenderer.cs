using System;
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
		lock (World.DrawLock)
		{
			World.LastRenderStateIndex = World.CurRenderStateIndex;
			World.CurRenderStateIndex = World.CurStateIndex;
		}

		ref var state = ref World.States[World.CurRenderStateIndex];
		ref var lastState = ref World.States[World.LastRenderStateIndex];
		stateLerpT = Math.Max(1.0f, dt * 10);

		var elevationColors = new List<CVP> { new CVP(Color.black, -2000), new CVP(new Color(0.5f,0.5f,0.5f), 1000), new CVP(Color.white, 4000) };
		var oceanElevationColors = new List<CVP> {
								new CVP(Color.black, MinElevation),
								new CVP(Color.blue, state.SeaLevel - 1000),
								new CVP(new Color(0.4f,0.3f,1.0f), state.SeaLevel), };
		var landTemperatureColors = new List<CVP>() { new CVP(new Color(0.4f, 0.4f, 1.0f), -500 + World.Data.FreezingTemperature), new CVP(Color.white, World.Data.FreezingTemperature), new CVP(Color.red, 500 + World.Data.FreezingTemperature) };
		var oceanTemperatureColors = new List<CVP>() { new CVP(new Color(0.0f, 0.0f, 0.6f), World.Data.FreezingTemperature - 50), new CVP(new Color(0.0f, 1.0f, 1.0f), 60 + World.Data.FreezingTemperature) };

		Color oceanColor;
		Color color;

		for (int y = 0; y < World.Size; y++)
		{
			for (int x = 0; x < World.Size; x++)
			{
				int index = World.GetIndex(x, y);

				float elevation = state.Elevation[index];
				float ice = state.Ice[index];
				float normalizedElevation = (elevation - MinElevation) / (MaxElevation - MinElevation);
				bool drawOcean = elevation < state.SeaLevel && showLayers.IsSet(Layers.Water);

				landVerts[index].z = -elevation * ElevationScale;
				oceanVerts[index].z = -state.SeaLevel * ElevationScale;

				// Base color

				color = Lerp(elevationColors, elevation);
				oceanColor = Lerp(oceanElevationColors, elevation);
				if (showLayers.IsSet(Layers.SoilFertility))
				{
					color = Color.Lerp(color, new Color(0.8f, 0.5f, 0.2f), Mathf.Lerp(lastState.SoilFertility[index], state.SoilFertility[index], stateLerpT));
				}

				if (showLayers.IsSet(Layers.Vegetation))
				{
					color = Color.Lerp(color, Color.green, Mathf.Clamp(Mathf.Lerp(lastState.Canopy[index], state.Canopy[index], stateLerpT), 0.01f, 1.0f));
				}


				if (showLayers.IsSet(Layers.TemperatureSubtle))
				{
		//			color = Lerp(landTemperatureColors, state.LowerAirTemperature[index]);
		//			oceanColor = Color.Lerp(oceanColor, Lerp(oceanTemperatureColors, state.OceanTemperatureShallow[index]), 0.25f);
				}

				//		if (showLayers.IsSet(Layers.Water))
				//		{
				//			float sw = MathUtils.Lerp(lastState.SurfaceWater[index], state.SurfaceWater[index], stateLerpT);
				//			if (sw > 0 || ice > 0)
				//			{
				//				int width = (int)(Math.Min(1.0f, sw + ice) * (tileRenderSize - 2));
				//				Rectangle surfaceWaterRect = new Rectangle(x * tileRenderSize + 1, y * tileRenderSize + 1, width, width);
				//				Color waterColor = Color.Lerp(Color.Blue, Color.Teal, (elevation - state.SeaLevel) / (Data.MaxElevation - state.SeaLevel));
				//				if (ice > 0)
				//				{
				//					waterColor = Color.Lerp(waterColor, Color.LightSteelBlue, Math.Min(1.0f, ice / Data.maxIce));
				//				}

				//				spriteBatch.Draw(whiteTex, surfaceWaterRect, waterColor * 0.75f);
				//			}
				//		}


				if (ice > 0)
				{
					oceanColor = Color.Lerp(oceanColor, new Color(0.4f, 0.6f, 1.0f), 0.25f + 0.75f * Mathf.Clamp01(ice / World.Data.FullIceCoverage));
					color = Color.Lerp(color, new Color(0.4f, 0.5f, 1.0f), 0.75f * Mathf.Clamp01(ice / World.Data.FullIceCoverage));
				}



				if (showLayers.IsSet(Layers.LowerAirTemperature))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, -50+World.Data.FreezingTemperature),
											new CVP(Color.white, -25+World.Data.FreezingTemperature),
											new CVP(Color.blue, 0+World.Data.FreezingTemperature),
											new CVP(Color.yellow, 25+World.Data.FreezingTemperature),
											new CVP(Color.red, 50+World.Data.FreezingTemperature),
											new CVP(Color.magenta, 75 + World.Data.FreezingTemperature) },
						state.LowerAirTemperature[index]);
				}
				else if (showLayers.IsSet(Layers.UpperAirTemperature))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, -50+World.Data.FreezingTemperature),
											new CVP(Color.white, -25+World.Data.FreezingTemperature),
											new CVP(Color.blue, 0+World.Data.FreezingTemperature),
											new CVP(Color.yellow, 25+World.Data.FreezingTemperature),
											new CVP(Color.red, 50+World.Data.FreezingTemperature),
											new CVP(Color.magenta, 75 + World.Data.FreezingTemperature) },
						state.UpperAirTemperature[index]);
				}
				else if (showLayers.IsSet(Layers.Evaporation))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.blue, 0.01f),
											new CVP(Color.yellow, 0.02f),
											new CVP(Color.red, 0.03f),
											new CVP(Color.white, 0.05f) },
						state.Evaporation[index] * World.Data.TicksPerYear);
				}
				else if (showLayers.IsSet(Layers.Rainfall))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.blue, 0.01f),
											new CVP(Color.yellow, 0.02f),
											new CVP(Color.red, 0.03f),
											new CVP(Color.white, 0.05f) },
						state.Rainfall[index]);
				}
				else if (showLayers.IsSet(Layers.HeatAbsorbed))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.blue, 20),
											new CVP(Color.yellow, 40),
											new CVP(Color.red, 60),
											new CVP(Color.white, 80) },
						state.EnergyAbsorbed[index]);
				}
				else if (showLayers.IsSet(Layers.LowerAirPressure))
				{
					float p = (state.LowerAirPressure[index] - minPressure) / (maxPressure - minPressure);
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.blue, 0.25f),
											new CVP(Color.yellow, 0.5f),
											new CVP(Color.red, 0.75f),
											new CVP(Color.white, 1.0f) },
						p);
				}
				else if (showLayers.IsSet(Layers.UpperAirPressure))
				{
					float p = (state.UpperAirPressure[index] - minPressure) / (maxPressure - minPressure);
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.blue, 0.25f),
											new CVP(Color.yellow, 0.5f),
											new CVP(Color.red, 0.75f),
											new CVP(Color.white, 1.0f) },
						p);
				}
				else if (showLayers.IsSet(Layers.WaterVapor))
				{
					float humidityDisplay = state.Humidity[index] / maxHumidity;
					color = oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.blue, 0.2f),
											new CVP(Color.green, 0.4f),
											new CVP(Color.yellow, 0.6f),
											new CVP(Color.red, 0.8f),
											new CVP(Color.white, 1) },
											humidityDisplay);
				}
				else if (showLayers.IsSet(Layers.RelativeHumidity))
				{
					float relativeHumidity = Mathf.Clamp01(Atmosphere.GetRelativeHumidity(World, state.LowerAirTemperature[index], state.Humidity[index], state.LowerAirMass[index]));
					color = oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.blue, 0.2f),
											new CVP(Color.green, 0.4f),
											new CVP(Color.yellow, 0.6f),
											new CVP(Color.red, 0.8f),
											new CVP(Color.white, 1) },
											relativeHumidity);

				}
				else if (showLayers.IsSet(Layers.Rainfall))
				{
					oceanColor = color = Color.Lerp(Color.black, Color.blue, Math.Min(1.0f, state.Rainfall[index] / maxRainfall));
				}
				else if (showLayers.IsSet(Layers.GroundWater))
				{
					oceanColor = color = Color.Lerp(new Color(0, 0, 0.5f), Color.gray, Math.Min(1.0f, state.GroundWater[index] / (World.Data.MaxWaterTableDepth * World.Data.MaxSoilPorousness)));
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
											new CVP(new Color(0, 0, 0.5f), (state.SeaLevel - MinElevation)/2+MinElevation),
											new CVP(new Color(0.4f, 0.4f, 1.0f), state.SeaLevel),
											new CVP(Color.yellow, state.SeaLevel+1),
											new CVP(new Color(0.5f, 0.5f, 0.5f), (MaxElevation-state.SeaLevel)/4+state.SeaLevel),
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
					oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, -50+World.Data.FreezingTemperature),
											new CVP(Color.white, -25+World.Data.FreezingTemperature),
											new CVP(Color.blue, 0+World.Data.FreezingTemperature),
											new CVP(Color.yellow, 25+World.Data.FreezingTemperature),
											new CVP(Color.red, 50+World.Data.FreezingTemperature),
											new CVP(Color.magenta, 75 + World.Data.FreezingTemperature) },
						state.OceanTemperatureShallow[index]);
				}
				else if (showLayers.IsSet(Layers.OceanTemperatureDeep))
				{
					oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, -50+World.Data.FreezingTemperature),
											new CVP(Color.white, -25+World.Data.FreezingTemperature),
											new CVP(Color.blue, 0+World.Data.FreezingTemperature),
											new CVP(Color.yellow, 25+World.Data.FreezingTemperature),
											new CVP(Color.red, 50+World.Data.FreezingTemperature),
											new CVP(Color.magenta, 75 + World.Data.FreezingTemperature) },
						Atmosphere.GetWaterTemperature(World, state.OceanEnergyDeep[index], Math.Max(0, state.SeaLevel - elevation)));
				}
				else if (showLayers.IsSet(Layers.OceanSalinityShallow))
				{
					color = oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, 30),
											new CVP(Color.blue, 32),
											new CVP(Color.green, 34),
											new CVP(Color.yellow, 36),
											new CVP(Color.red, 38),
											new CVP(Color.white, 40) },
						elevation < state.SeaLevel ? (state.OceanSalinityShallow[index] / World.Data.DeepOceanDepth) : 0);
				}
				else if (showLayers.IsSet(Layers.OceanSalinityDeep))
				{
					color = oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, 30),
											new CVP(Color.blue, 32),
											new CVP(Color.green, 34),
											new CVP(Color.yellow, 36),
											new CVP(Color.red, 38),
											new CVP(Color.white, 40) },
						elevation < state.SeaLevel ? state.OceanSalinityDeep[index] / (state.SeaLevel - elevation) : 0);
				}


				landCols[index] = color;
				oceanCols[index] = oceanColor;

				if (showLayers.IsSet(Layers.CloudCoverage))
				{
					if (state.CloudMass[index] > minCloudsToDraw)
					{
						var cloudColor = Color.Lerp(Color.white, Color.black, Mathf.Clamp01(state.RainDropMass[index] / maxCloudColor)) * (float)Math.Sqrt(Mathf.Clamp01((state.CloudMass[index] - minCloudsToDraw) / maxCloudAlpha)) * 0.9f;
						cloudCols[index] = cloudColor;
					}
					else
					{
						cloudCols[index].a = 0;
					}
					cloudVerts[index].z = -Mathf.Max(Math.Max(elevation, state.SeaLevel) + 1) * ElevationScale;
				}

				if (showLayers.IsSet(Layers.SurfaceAirWind))
				{
					var wind = state.LowerWind[index];
					UpdateWindArrow(state, x, y, index, wind, 50);
				}
				else if (showLayers.IsSet(Layers.UpperAirWind))
				{
					var wind = state.UpperWind[index];
					UpdateWindArrow(state, x, y, index, wind, 250);
				}
				else if (showLayers.IsSet(Layers.OceanCurrentShallow))
				{
					var current = state.OceanCurrentShallow[index];
					UpdateWindArrow(state, x, y, index, current, 2.5f);
				}
				else if (showLayers.IsSet(Layers.OceanCurrentDeep))
				{
					var current = state.OceanCurrentDeep[index];
					UpdateWindArrow(state, x, y, index, current, 0.1f);
				}

			}
		}


		LandMesh.mesh.vertices = landVerts;
		LandMesh.mesh.colors = landCols;

		CloudMesh.mesh.vertices = cloudVerts;
		CloudMesh.mesh.colors = cloudCols;

		OceanMesh.mesh.vertices = oceanVerts;
		OceanMesh.mesh.colors = oceanCols;

		CloudMesh.gameObject.SetActive(showLayers.IsSet(Layers.CloudCoverage));
		OceanMesh.gameObject.SetActive(showLayers.IsSet(Layers.Water));

	}

	private void UpdateWindArrow(World.State state, int x, int y, int index, Vector3 wind, float maxSpeed)
	{
		float elevationOrSeaLevel = Math.Max(state.SeaLevel, state.Elevation[index]);
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

		_windArrows[index].transform.position = new Vector3(x, y, -(elevationOrSeaLevel + 1000) * ElevationScale);
		_windArrows[index].transform.localScale = Vector3.one * Mathf.Clamp01(windXYSpeed / maxSpeed);
		_windArrows[index].transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * windAngle);
		_windArrows[index].GetComponentInChildren<MeshRenderer>().material.color = windColor;
	}
}