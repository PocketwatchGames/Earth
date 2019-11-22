﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Sim;

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
		landVerts = new Vector3[_size * _size];
		landCols = new Color[_size * _size];
		landTris = new int[(_size - 1) * (_size - 1) * 6];

		cloudVerts = new Vector3[_size * _size];
		cloudCols = new Color[_size * _size];
		cloudTris = new int[(_size - 1) * (_size - 1) * 6];

		oceanVerts = new Vector3[_size * _size];
		oceanCols = new Color[_size * _size];
		oceanTris = new int[(_size - 1) * (_size - 1) * 6];


		for (int i = 0; i < _size; i++)
		{
			for (int j = 0; j < _size; j++)
			{
				landVerts[i * _size + j] = new Vector3(j, i, 0);
				landCols[i * _size + j] = Color.black;
				cloudVerts[i * _size + j] = new Vector3(j, i, 0);
				cloudCols[i * _size + j] = new Color(0, 0, 0, 0);
				oceanVerts[i * _size + j] = new Vector3(j, i, 0);
				oceanCols[i * _size + j] = new Color(0, 0, 0, 0);
			}
		}
		int index = 0;
		for (int i = 0; i < _size - 1; i++)
		{
			for (int j = 0; j < _size - 1; j++)
			{
				landTris[index + 0] = i * _size + j;
				landTris[index + 1] = (i + 1) * _size + j;
				landTris[index + 2] = (i + 1) * _size + (j + 1);
				landTris[index + 3] = i * _size + j;
				landTris[index + 4] = (i + 1) * _size + (j + 1);
				landTris[index + 5] = i * _size + (j + 1);

				cloudTris[index + 0] = i * _size + j;
				cloudTris[index + 1] = (i + 1) * _size + j;
				cloudTris[index + 2] = (i + 1) * _size + (j + 1);
				cloudTris[index + 3] = i * _size + j;
				cloudTris[index + 4] = (i + 1) * _size + (j + 1);
				cloudTris[index + 5] = i * _size + (j + 1);

				oceanTris[index + 0] = i * _size + j;
				oceanTris[index + 1] = (i + 1) * _size + j;
				oceanTris[index + 2] = (i + 1) * _size + (j + 1);
				oceanTris[index + 3] = i * _size + j;
				oceanTris[index + 4] = (i + 1) * _size + (j + 1);
				oceanTris[index + 5] = i * _size + (j + 1);

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

		for (int y = 0; y < _size; y++)
		{
			for (int x = 0; x < _size; x++)
			{
				int index = World.GetIndex(x, y);

				Color oceanColor;
				Color color = Color.white;
				float elevation = state.Elevation[index];
				float ice = state.SurfaceIce[index];
				float normalizedElevation = (elevation - World.Data.MinElevation) / (World.Data.MaxElevation - World.Data.MinElevation);
				bool drawOcean = elevation < state.SeaLevel && showLayers.HasFlag(Layers.Water);

				landVerts[index] = new Vector3(x, y, -elevation * ElevationScale);
				oceanVerts[index] = new Vector3(x, y, -state.SeaLevel * ElevationScale);

				// Base color

				if (showLayers.HasFlag(Layers.SoilFertility))
				{
					color = Color.Lerp(Color.gray, new Color(0.8f,0.5f,0.2f), Mathf.Lerp(lastState.SoilFertility[index], state.SoilFertility[index], stateLerpT));
				}

				if (showLayers.HasFlag(Layers.ElevationSubtle))
				{
					color = Lerp(new List<CVP> { new CVP(Color.black, -2000), new CVP(color, 1000), new CVP(Color.white, 4000) }, elevation);
				}

				if (showLayers.HasFlag(Layers.Vegetation))
				{
					color = Color.Lerp(color, Color.green, Mathf.Clamp(Mathf.Lerp(lastState.Canopy[index], state.Canopy[index], stateLerpT), 0.01f, 1.0f));
				}


				if (showLayers.HasFlag(Layers.TemperatureSubtle))
				{
					color = Lerp(new List<CVP>() { new CVP(new Color(0.4f, 0.4f, 1.0f), -500 + World.Data.FreezingTemperature), new CVP(color, World.Data.FreezingTemperature), new CVP(Color.red, 500 + World.Data.FreezingTemperature) }, state.LowerAirTemperature[index]);
				}

				//		if (showLayers.HasFlag(Layers.Water))
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

				if (showLayers.HasFlag(Layers.ElevationSubtle))
				{
					oceanColor = Lerp(new List<CVP> {
									new CVP(Color.black, World.Data.MinElevation),
									new CVP(Color.blue, state.SeaLevel - 500),
									new CVP(new Color(0.1f,0.2f,1.0f), state.SeaLevel), },
						elevation);
				}
				else
				{
					oceanColor = Color.blue;
				}
				if (ice > 0)
				{
					oceanColor = Color.Lerp(color, new Color(0.6f, 0.5f, 1.0f), Math.Min(1.0f, ice / World.Data.maxIce));
				}



				if (showLayers.HasFlag(Layers.LowerAirTemperature))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, -50+World.Data.FreezingTemperature),
											new CVP(Color.blue, -25+World.Data.FreezingTemperature),
											new CVP(Color.green, 0+World.Data.FreezingTemperature),
											new CVP(Color.yellow, 25+World.Data.FreezingTemperature),
											new CVP(Color.red, 50+World.Data.FreezingTemperature),
											new CVP(Color.white, 75 + World.Data.FreezingTemperature) },
						state.LowerAirTemperature[index]);
				}
				else if (showLayers.HasFlag(Layers.UpperAirTemperature))
				{
					oceanColor = color = Lerp(new List<CVP> {
											new CVP(Color.black, -50+World.Data.FreezingTemperature),
											new CVP(Color.blue, -25+World.Data.FreezingTemperature),
											new CVP(Color.green, 0+World.Data.FreezingTemperature),
											new CVP(Color.yellow, 25+World.Data.FreezingTemperature),
											new CVP(Color.red, 50+World.Data.FreezingTemperature),
											new CVP(Color.white, 75 + World.Data.FreezingTemperature) },
						state.UpperAirTemperature[index]);
				}
				else if (showLayers.HasFlag(Layers.LowerAirPressure))
				{
					float minPressure = 120;
					float maxPressure = 220;
					oceanColor = color = Lerp(new List<CVP> { new CVP(Color.blue, minPressure), new CVP(Color.white, (maxPressure + minPressure) / 2), new CVP(Color.red, maxPressure) }, state.LowerAirPressure[index]);
				}
				else if (showLayers.HasFlag(Layers.UpperAirPressure))
				{
					float minPressure = 120;
					float maxPressure = 220;
					oceanColor = color = Lerp(new List<CVP> { new CVP(Color.blue, minPressure), new CVP(Color.white, (maxPressure + minPressure) / 2), new CVP(Color.red, maxPressure) }, state.UpperAirPressure[index]);
				}
				else if (showLayers.HasFlag(Layers.WaterVapor))
				{
					float maxHumidity = 15;
					oceanColor = color = Color.Lerp(Color.black, Color.blue, Math.Min(1.0f, state.Humidity[index] / maxHumidity));
				}
				else if (showLayers.HasFlag(Layers.RelativeHumidity))
				{
					float timeOfYear = World.GetTimeOfYear(state.Ticks);
					float declinationOfSun = Atmosphere.GetDeclinationOfSun(Data.planetTiltAngle, timeOfYear);
					float lengthOfDay = Atmosphere.GetLengthOfDay(World.GetLatitude(y), timeOfYear, declinationOfSun);
					var sunVector = Atmosphere.GetSunVector(World, state.Ticks, World.GetLatitude(y)).z;
					float localTemperature = Atmosphere.GetLocalTemperature(World, Math.Max(0, sunVector), state.CloudCover[index], state.LowerAirTemperature[index], lengthOfDay);
					float relativeHumidity = Atmosphere.GetRelativeHumidity(World, localTemperature, state.Humidity[index], state.CloudElevation[index], Math.Max(elevation, state.SeaLevel));
					oceanColor = color = Color.Lerp(Color.black, Color.blue, Math.Min(1.0f, relativeHumidity / World.Data.dewPointRange));
				}
				else if (showLayers.HasFlag(Layers.Rainfall))
				{
					float maxRainfall = 5.0f / World.Data.TicksPerYear;
					oceanColor = color = Color.Lerp(Color.black, Color.blue, Math.Min(1.0f, state.Rainfall[index] / maxRainfall));
				}
				else if (showLayers.HasFlag(Layers.GroundWater))
				{
					oceanColor = color = Color.Lerp(new Color(0, 0, 0.5f), Color.gray, Math.Min(1.0f, state.GroundWater[index] / (World.Data.MaxWaterTableDepth * World.Data.MaxSoilPorousness)));
				}
				else if (showLayers.HasFlag(Layers.WaterTableDepth))
				{
					oceanColor = color = Color.Lerp(Color.black, Color.white, (state.WaterTableDepth[index] - World.Data.MinWaterTableDepth) / (World.Data.MaxWaterTableDepth - World.Data.MinWaterTableDepth));
				}
				else if (showLayers.HasFlag(Layers.Elevation))
				{
					oceanColor = color = Lerp(
						new List<CVP> {
											new CVP(Color.black, World.Data.MinElevation),
											new CVP(new Color(0, 0, 0.5f), (state.SeaLevel - World.Data.MinElevation)/2+World.Data.MinElevation),
											new CVP(new Color(0.4f, 0.4f, 1.0f), state.SeaLevel),
											new CVP(Color.yellow, state.SeaLevel+1),
											new CVP(new Color(0.5f, 0.5f, 0.5f), (World.Data.MaxElevation-state.SeaLevel)/4+state.SeaLevel),
											new CVP(Color.white, World.Data.MaxElevation) },
						elevation);
				}
				else if (showLayers.HasFlag(Layers.Plates))
				{
					float elevationT = elevation / World.Data.MaxElevation;
					color = Color.Lerp(Color.black, Color.white, Math.Sign(elevation) * (float)Math.Sqrt(Math.Abs(elevationT)) / 2 + 0.5f);
					oceanColor = color = Color.Lerp(color, PlateColors[state.Plate[index] % MaxPlateColors], 0.25f);
				}

				if (showLayers.HasFlag(Layers.OceanTemperatureShallow))
				{
					oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, -50+World.Data.FreezingTemperature),
											new CVP(Color.blue, -25+World.Data.FreezingTemperature),
											new CVP(Color.green, 0+World.Data.FreezingTemperature),
											new CVP(Color.yellow, 25+World.Data.FreezingTemperature),
											new CVP(Color.red, 50+World.Data.FreezingTemperature),
											new CVP(Color.white, 75 + World.Data.FreezingTemperature) },
						Atmosphere.GetWaterTemperature(World, state.OceanEnergyShallow[index], World.Data.DeepOceanDepth));
				}
				else if (showLayers.HasFlag(Layers.OceanTemperatureDeep))
				{
					oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, -50+World.Data.FreezingTemperature),
											new CVP(Color.blue, -25+World.Data.FreezingTemperature),
											new CVP(Color.green, 0+World.Data.FreezingTemperature),
											new CVP(Color.yellow, 25+World.Data.FreezingTemperature),
											new CVP(Color.red, 50+World.Data.FreezingTemperature),
											new CVP(Color.white, 75 + World.Data.FreezingTemperature) },
						Atmosphere.GetWaterTemperature(World, state.OceanEnergyDeep[index], Math.Max(0, state.SeaLevel - elevation)));
				}
				else if (showLayers.HasFlag(Layers.OceanSalinityShallow))
				{
					color = oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.red, 0.5f),
											new CVP(Color.blue, 1),
											new CVP(Color.green, 1.5f),
											new CVP(Color.white, 2) },
						elevation < state.SeaLevel ? state.OceanSalinityShallow[index] / Data.DeepOceanDepth : 0);
				} 
				else if (showLayers.HasFlag(Layers.OceanSalinityDeep))
				{
					color = oceanColor = Lerp(new List<CVP> {
											new CVP(Color.black, 0),
											new CVP(Color.red, 0.5f),
											new CVP(Color.blue, 1),
											new CVP(Color.green, 1.5f),
											new CVP(Color.white, 2) },
						elevation < state.SeaLevel ? state.OceanSalinityDeep[index] / (state.SeaLevel - elevation) : 0);
				}


					landCols[index] = color;
				oceanCols[index] = oceanColor;

				float minCloudsToDraw = 0.01f;
				float maxCloudsWidth = 0.5f;
				float maxCloudsToDraw = 1.0f;
				float cloudCover = Mathf.Clamp(state.CloudCover[index] - minCloudsToDraw, 0.0f, maxCloudsToDraw);
				if (cloudCover > 0)
				{
					float normalizedCloudCover = cloudCover / maxCloudsToDraw;
					var cloudColor = Color.Lerp(Color.white, Color.black, normalizedCloudCover) * (float)Math.Sqrt(normalizedCloudCover) * 0.9f;
					cloudCols[index] = cloudColor;
				} else
				{
					cloudCols[index].a = 0;
				}
				cloudVerts[index].z = -Mathf.Max(elevation+1, state.CloudElevation[index]) * ElevationScale;
				if (showLayers.HasFlag(Layers.LowerAirWind))
				{
					var wind = state.LowerWind[index];
					UpdateWindArrow(state, x, y, index, wind, 40);
				}
				else if (showLayers.HasFlag(Layers.UpperAirWind))
				{
					var wind = state.UpperWind[index];
					UpdateWindArrow(state, x, y, index, wind, 200);
				}
				else if (showLayers.HasFlag(Layers.OceanCurrentShallow))
				{
					var current = state.OceanCurrentShallow[index];
					UpdateWindArrow(state, x, y, index, current, 1);
				}
				else if (showLayers.HasFlag(Layers.OceanCurrentDeep))
				{
					var current = state.OceanCurrentDeep[index];
					UpdateWindArrow(state, x, y, index, current, 0.1f);
				}

			}
		}

		//if (showLayers.HasFlag(Layers.Animals))
		//{
		//	for (int a = 0; a < MaxAnimals; a++)
		//	{
		//		if (state.Animals[a].Population > 0)
		//		{
		//			Rectangle rect = new Rectangle((int)(state.Animals[a].Position.X * tileRenderSize), (int)(state.Animals[a].Position.Y * tileRenderSize), tileRenderSize, tileRenderSize);
		//			int p = MathHelper.Clamp((int)Math.Ceiling(tileRenderSize * (float)state.Animals[a].Population / state.Species[state.Animals[a].Species].speciesMaxPopulation), 0, tileRenderSize);
		//			for (int i = 0; i < p; i++)
		//			{
		//				int screenX = rect.X + i * 2 - p;
		//				int size = i == p - 1 ? 1 : 3;
		//				spriteBatch.Draw(whiteTex, new Rectangle(screenX, rect.Y + (int)(tileRenderSize * (Math.Cos(screenX + rect.Y + (float)gameTime.TotalGameTime.Ticks / TimeSpan.TicksPerSecond * Math.Sqrt(TimeScale)) / 2)), size, size), state.Species[state.Animals[a].Species].Color);
		//			}
		//		}
		//	}
		//}


		//if (showLayers.HasFlag(Layers.Probes))
		//{
		//	for (int i = 0; i < ProbeCount; i++)
		//	{
		//		var probe = Probes[i];
		//		spriteBatch.Draw(
		//			whiteTex,
		//			new Vector2(probe.Position.X * tileRenderSize + tileRenderSize / 2, probe.Position.Y * tileRenderSize + tileRenderSize / 2),
		//			null,
		//			Color.Purple,
		//			(float)gameTime.TotalGameTime.Ticks / TimeSpan.TicksPerSecond,
		//			new Vector2(0.5f, 0.5f),
		//			5,
		//			SpriteEffects.None,
		//			0);
		//	}
		//}
		LandMesh.mesh.vertices = landVerts;
		LandMesh.mesh.colors = landCols;

		CloudMesh.mesh.vertices = cloudVerts;
		CloudMesh.mesh.colors = cloudCols;

		OceanMesh.mesh.vertices = oceanVerts;
		OceanMesh.mesh.colors = oceanCols;

		CloudMesh.gameObject.SetActive(showLayers.HasFlag(Layers.CloudCoverage));
		OceanMesh.gameObject.SetActive(showLayers.HasFlag(Layers.Water));

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