using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public partial class WorldComponent {
	Vector3[] verts;
	Color[] cols;
	int[] tris;

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
	public int tileRenderSize = 10;
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
		verts = new Vector3[size * size];
		cols = new Color[size * size];
		tris = new int[(size - 1) * (size - 1) * 6];


		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				verts[i * size + j] = new Vector3(j, i, 0) * 0.1f - new Vector3(5,5,0);
				cols[i * size + j] = Color.black;
			}
		}
		int index = 0;
		for (int i = 0; i < size - 1; i++)
		{
			for (int j = 0; j < size - 1; j++)
			{
				tris[index + 0] = i * size + j;
				tris[index + 1] = (i + 1) * size + j;
				tris[index + 2] = (i + 1) * size + (j + 1);
				tris[index + 3] = i * size + j;
				tris[index + 4] = (i + 1) * size + (j + 1);
				tris[index + 5] = i * size + (j + 1);
				index += 6;
			}
		}

		var meshFilter = GetComponent<MeshFilter>();
		meshFilter.mesh.vertices = verts;
		meshFilter.mesh.triangles = tris;
		meshFilter.mesh.colors = cols;
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

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int index = World.GetIndex(x, y);

				Color color = Color.white;
				float elevation = state.Elevation[index];
				float ice = state.SurfaceIce[index];
				float normalizedElevation = (elevation - World.Data.MinElevation) / (World.Data.MaxElevation - World.Data.MinElevation);
				bool drawOcean = elevation <= state.SeaLevel && showLayers.HasFlag(Layers.Water);

				// Base color

				if (drawOcean)
				{
					if (showLayers.HasFlag(Layers.ElevationSubtle))
					{
						color = Lerp(new List<CVP> {
									new CVP(Color.black, World.Data.MinElevation),
									new CVP(Color.blue, state.SeaLevel - 500),
									new CVP(new Color(0.1f,0.2f,1.0f), state.SeaLevel), },
							elevation);
					}
					else
					{
						color = Color.blue;
					}
					if (ice > 0)
					{
						color = Color.Lerp(color, new Color(0.6f, 0.5f, 1.0f), Math.Min(1.0f, ice / World.Data.maxIce));
					}
				}
				else
				{
					if (showLayers.HasFlag(Layers.SoilFertility))
					{
						color = Color.Lerp(Color.gray, Color.yellow, Mathf.Lerp(lastState.SoilFertility[index], state.SoilFertility[index], stateLerpT));
					}

					if (showLayers.HasFlag(Layers.ElevationSubtle))
					{
						color = Lerp(new List<CVP> { new CVP(Color.black, -1000), new CVP(color, 1000), new CVP(Color.white, 3000) }, elevation);
					}

					if (showLayers.HasFlag(Layers.Vegetation))
					{
						color = Color.Lerp(color, Color.green, (float)Math.Sqrt(Mathf.Clamp(Mathf.Lerp(lastState.Canopy[index], state.Canopy[index], stateLerpT), 0.01f, 1.0f)));
					}
				}


				if (showLayers.HasFlag(Layers.TemperatureSubtle))
				{
					color = Lerp(new List<CVP>() { new CVP(new Color(0.4f, 0.4f, 1.0f), -500 + World.Data.FreezingTemperature), new CVP(color, World.Data.FreezingTemperature), new CVP(Color.red, 500 + World.Data.FreezingTemperature) }, state.Temperature[index]);
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

				if (showLayers.HasFlag(Layers.Temperature))
				{
					color = Lerp(new List<CVP> {
											new CVP(Color.black, -45+World.Data.FreezingTemperature),
											new CVP(Color.blue, -15 + World.Data.FreezingTemperature),
											new CVP(Color.green, 15+World.Data.FreezingTemperature),
											new CVP(Color.red, 45+World.Data.FreezingTemperature),
											new CVP(Color.white, 75+World.Data.FreezingTemperature) },
						state.Temperature[index]);
				}
				else if (showLayers.HasFlag(Layers.Pressure))
				{
					float minPressure = World.Data.StaticPressure - 40000;
					float maxPressure = World.Data.StaticPressure + 10000;
					color = Lerp(new List<CVP> { new CVP(Color.magenta, minPressure), new CVP(Color.white, (maxPressure + minPressure) / 2), new CVP(new Color(0.4f, 0.4f, 1.0f), maxPressure) }, state.Pressure[index]);
				}
				else if (showLayers.HasFlag(Layers.WaterVapor))
				{
					float maxHumidity = 15;
					color = Color.Lerp(Color.black, Color.blue, Math.Min(1.0f, state.Humidity[index] / maxHumidity));
				}
				else if (showLayers.HasFlag(Layers.RelativeHumidity))
				{
					color = Color.Lerp(Color.black, Color.blue, Math.Min(1.0f, World.GetRelativeHumidity(World.GetLocalTemperature(Math.Max(0, World.GetSunVector(state.Ticks, World.GetLatitude(y)).z), state.CloudCover[index], state.Temperature[index]), state.Humidity[index], state.CloudElevation[index], Math.Max(elevation, state.SeaLevel)) / World.Data.dewPointRange));
				}
				else if (showLayers.HasFlag(Layers.Rainfall))
				{
					float maxRainfall = 5.0f / World.Data.TicksPerYear;
					color = Color.Lerp(Color.black, Color.blue, Math.Min(1.0f, state.Rainfall[index] / maxRainfall));
				}
				else if (showLayers.HasFlag(Layers.GroundWater))
				{
					color = Color.Lerp(new Color(0, 0, 0.5f), Color.gray, Math.Min(1.0f, state.GroundWater[index] / (World.Data.MaxWaterTableDepth * World.Data.MaxSoilPorousness)));
				}
				else if (showLayers.HasFlag(Layers.WaterTableDepth))
				{
					color = Color.Lerp(Color.black, Color.white, (state.WaterTableDepth[index] - World.Data.MinWaterTableDepth) / (World.Data.MaxWaterTableDepth - World.Data.MinWaterTableDepth));
				}
				else if (showLayers.HasFlag(Layers.Elevation))
				{
					color = Lerp(
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
					color = Color.Lerp(color, PlateColors[state.Plate[index] % MaxPlateColors], 0.25f);
				}

				cols[index] = color;
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

		//if (showLayers.HasFlag(Layers.CloudHeight) || showLayers.HasFlag(Layers.CloudCoverage) || showLayers.HasFlag(Layers.Wind))
		//{
		//	for (int x = 0; x < Size; x++)
		//	{
		//		for (int y = 0; y < Size; y++)
		//		{
		//			int index = GetIndex(x, y);
		//			if (showLayers.HasFlag(Layers.CloudHeight))
		//			{
		//				//					spriteBatch.Draw(whiteTex, rect, Color.Lerp(Color.Black, Color.White, CloudElevation[index] / MaxCloudElevation) * CloudCover[index]);
		//			}
		//			else if (showLayers.HasFlag(Layers.CloudCoverage))
		//			{
		//				float minCloudsToDraw = 0.01f;
		//				float maxCloudsWidth = 0.5f;
		//				float maxCloudsToDraw = 1.0f;
		//				float cloudCover = (float)MathHelper.Clamp(state.CloudCover[index] - minCloudsToDraw, 0.0f, maxCloudsToDraw);
		//				if (cloudCover > 0)
		//				{
		//					float normalizedCloudCover = cloudCover / maxCloudsToDraw;
		//					int width = (int)(Math.Min(1.0f, cloudCover / maxCloudsWidth) * (tileRenderSize - 2));
		//					Rectangle rect = new Rectangle(x * tileRenderSize + 1, y * tileRenderSize + 1, width, width);
		//					spriteBatch.Draw(whiteTex, rect, Color.Lerp(Color.White, Color.Black, normalizedCloudCover) * (float)Math.Sqrt(normalizedCloudCover) * 0.9f);
		//				}
		//			}
		//			if (showLayers.HasFlag(Layers.Wind))
		//			{
		//				//							var wind = state.Wind[index];
		//				float elevationOrSeaLevel = Math.Max(state.SeaLevel, state.Elevation[index]);
		//				var wind = state.WindCloud[index];
		//				//var wind = GetWindAtElevation(state, elevationOrSeaLevel, elevationOrSeaLevel, index, GetLatitude(y), state.Normal[index]);
		//				float maxWindSpeed = 40;
		//				float maxWindSpeedVertical = 2;
		//				Color windColor;
		//				if (wind.Z < 0)
		//				{
		//					windColor = Color.Lerp(Color.White, Color.Blue, -wind.Z / maxWindSpeedVertical);
		//				}
		//				else
		//				{
		//					windColor = Color.Lerp(Color.White, Color.Red, wind.Z / maxWindSpeedVertical);
		//				}
		//				float windXYSpeed = (float)Math.Sqrt(wind.X * wind.X + wind.Y * wind.Y);
		//				Rectangle rect = new Rectangle(x * tileRenderSize, y * tileRenderSize, tileRenderSize, tileRenderSize);
		//				float windAngle = (float)Math.Atan2(wind.Y, wind.X);
		//				if (index == 2100)
		//				{
		//					Console.WriteLine(wind);
		//				}
		//				spriteBatch.Draw(whiteTex, new Rectangle(rect.X + tileRenderSize / 2 - 1, rect.Y + tileRenderSize / 2 - 1, 3, 3), null, Color.White * 0.5f);
		//				spriteBatch.Draw(whiteTex, new Rectangle(rect.X + tileRenderSize / 2, rect.Y + tileRenderSize / 2, (int)(tileRenderSize * windXYSpeed / maxWindSpeed), 1), null, windColor, windAngle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
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

		GetComponent<MeshFilter>().mesh.colors = cols;
	}
}