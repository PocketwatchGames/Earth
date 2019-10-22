using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public partial class World
{

	public void TickWind(State state, State nextState)
	{
		for (int y = 0; y < Size; y++)
		{
			float latitude = Data.windInfo[y].latitude;

			for (int x = 0; x < Size; x++)
			{
				int index = GetIndex(x, y);
				float pressure = state.Pressure[index];

				// within 1 km of the ground, frictional forces slow wind down
				var newWind = UpdateWind(state, x, y, latitude, pressure);

				nextState.Wind[index] = newWind;
			}
		}
		Task[] windTasks = new Task[]
		{
			Task.Run(() => { UpdateWindAtElevation(state, nextState.WindSurface, nextState.Wind, state.Elevation); }),
			Task.Run(() => { UpdateWindAtElevation(state, nextState.WindCloud, nextState.Wind, state.CloudElevation); }),
		};
		Task.WaitAll(windTasks);
	}

	private void UpdateWindAtElevation(State state, Vector3[] windToSet, Vector3[] wind, float[] elevation)
	{
		for (int y = 0; y < Size; y++)
		{
			var windInfo = Data.windInfo[y];
			float tropopauseElevation = windInfo.tropopauseElevationMax * (GetTimeOfYear(state.Ticks) * 2 - 1);

			for (int x = 0; x < Size; x++)
			{
				int index = GetIndex(x, y);
				var normal = state.Normal[index];
				float elevationOrSeaLevel = Math.Max(state.SeaLevel, state.Elevation[index]);

				// within 1 km of the ground, frictional forces slow wind down
				float friction = (1.0f - normal.z * 0.75f);

				windToSet[index] = GetWindAtElevation(windInfo.tradeWind, wind[index], Math.Max(state.SeaLevel, elevation[index]), Math.Max(state.SeaLevel, state.Elevation[index]), tropopauseElevation, windInfo.latitude, windInfo.yaw, windInfo.coriolisPower, friction);
			}
		}
	}

	private Vector3 UpdateWind(State state, int x, int y, float latitude, float pressure)
	{
		Vector2 pressureDifferential = Vector2.zero;
		Vector3 nWind = Vector3.zero;
		for (int i = 0; i < 4; i++)
		{
			var neighbor = GetNeighbor(x, y, i);
			int nIndex = GetIndex(neighbor.x, neighbor.y);
			//var neighborWind = state.Wind[nIndex];
			//nWind += neighborWind;

			switch (i)
			{
				case 0:
					pressureDifferential.x += state.Pressure[nIndex] - pressure;
					break;
				case 1:
					pressureDifferential.x -= state.Pressure[nIndex] - pressure;
					break;
				case 2:
					pressureDifferential.y -= state.Pressure[nIndex] - pressure;
					break;
				case 3:
					pressureDifferential.y += state.Pressure[nIndex] - pressure;
					break;
			}
		}
		Vector3 newWind = new Vector3(pressureDifferential.x, pressureDifferential.y, (pressureDifferential.x + pressureDifferential.y) / 4) * Data.pressureDifferentialWindSpeed;
		return newWind * (1.0f - Data.windInertia) + nWind / 4 * Data.windInertia;

	}

	Vector3 GetWindAtElevation(Vector3 tradeWind, Vector3 pressureWind, float windElevation, float landElevation, float tropopauseElevation, float latitude, float yaw, float coriolisPower, float friction)
	{
		float altitude = windElevation - landElevation;
		float hadleyCellHeight = Math.Min(1.0f, altitude / (tropopauseElevation - landElevation));

		if (latitude < 0.3333f && latitude > -0.3333f)
		{
			tradeWind.y *= (float)-Math.Sin(yaw + Math.PI * hadleyCellHeight);
		}
		else if (latitude < 0.667f && latitude > -0.667f)
		{
			tradeWind.y *= (float)Math.Sin(yaw + Math.PI * hadleyCellHeight);
		}
		else
		{
			tradeWind.y *= (float)Math.Sin(yaw + Math.PI * hadleyCellHeight);
		}
		if (tradeWind.z > 0)
		{
			tradeWind.z *= 1.0f - hadleyCellHeight;
		}
		else
		{
			tradeWind.z *= hadleyCellHeight;
		}
		Vector3 wind = tradeWind;

		// within 1 km of the ground, frictional forces slow wind down			
		float frictionElevation = Math.Max(0.0f, (1.0f - altitude / Data.maxWindFrictionElevation));
		friction *= frictionElevation * frictionElevation;

		if (coriolisPower > 0 && friction < 1)
		{
//				Matrix4x4.Rotate(Quaternion.EulerRotation())
			wind += Quaternion.Euler(0,0,coriolisPower * (1.0f - friction)) * pressureWind;
		}

		// Wind speeds are much higher at high altitudes
		float windElevationNormalized = Math.Min(1.0f, windElevation * Data.windElevationFactor);
		wind *= 1.0f + windElevationNormalized * windElevationNormalized * (1.0f - friction);

		// TODO: Should I be simulating pressure differentials at the tropopause to distribute heat at the upper atmosphere?
		return wind;
	}

}
