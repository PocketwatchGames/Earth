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
			var windInfo = Data.windInfo[y];

			for (int x = 0; x < Size; x++)
			{
				int index = GetIndex(x, y);

				if (state.Elevation[index] <= state.SeaLevel)
				{

					float pressure = state.Pressure[index];
					var normal = state.Normal[index];
					float friction = (1.0f - normal.z) * 0.5f;

					// within 1 km of the ground, frictional forces slow wind down
					var newWind = UpdateWind(state, x, y, latitude, pressure, friction, windInfo.coriolisPower, windInfo.tradeWind.z);
					nextState.Wind[index] = newWind;
					if (state.Elevation[index] <= state.SeaLevel && state.SurfaceIce[index] == 0)
					{
						nextState.OceanCurrentShallow[index] = Quaternion.Euler(0, 0, windInfo.coriolisPower * 90) * new Vector3(newWind.x, newWind.y, 0) * Data.WindToOceanCurrentFactor;
					}
					else
					{
						nextState.OceanCurrentShallow[index] = Vector3.zero;
					}





					float density = state.OceanDensityDeep[index];
					Vector2 densityDifferential = Vector2.zero;
					for (int i = 0; i < 4; i++)
					{
						var neighbor = GetNeighbor(x, y, i);
						int nIndex = GetIndex(neighbor.x, neighbor.y);
						if (state.Elevation[nIndex] <= state.SeaLevel)
						{
							//var neighborWind = state.Wind[nIndex];
							//nWind += neighborWind;

							switch (i)
							{
								case 0:
									densityDifferential.x += state.OceanDensityDeep[nIndex] - density;
									break;
								case 1:
									densityDifferential.x -= state.OceanDensityDeep[nIndex] - density;
									break;
								case 2:
									densityDifferential.y -= state.OceanDensityDeep[nIndex] - density;
									break;
								case 3:
									densityDifferential.y += state.OceanDensityDeep[nIndex] - density;
									break;
							}
						}
					}
					nextState.OceanCurrentDeep[index] = new Vector3(densityDifferential.x, densityDifferential.y, 0);


				}
			}
		}

		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				int index = GetIndex(x, y);
				if (state.Elevation[index] <= state.SeaLevel)
				{
					var vertCurrent = nextState.OceanCurrentShallow[index].magnitude;
					for (int i = 0; i < 4; i++)
					{
						var neighbor = GetNeighbor(x, y, i);
						int nIndex = GetIndex(neighbor.x, neighbor.y);
						if (state.Elevation[nIndex] <= state.SeaLevel)
						{
							switch (i)
							{
								case 0:
									if (nextState.OceanCurrentShallow[index].x > 0)
									{
										vertCurrent -= nextState.OceanCurrentShallow[index].x;
									}
									break;
								case 1:
									if (nextState.OceanCurrentShallow[index].x < 0)
									{
										vertCurrent += nextState.OceanCurrentShallow[index].x;
									}
									break;
								case 2:
									if (nextState.OceanCurrentShallow[index].y < 0)
									{
										vertCurrent += nextState.OceanCurrentShallow[index].y;
									}
									break;
								case 3:
									if (nextState.OceanCurrentShallow[index].y > 0)
									{
										vertCurrent -= nextState.OceanCurrentShallow[index].y;
									}
									break;
							}
						}
					}
					nextState.OceanCurrentShallow[index].z = vertCurrent;
				}
			}
		}

	}


	private Vector3 UpdateWind(State state, int x, int y, float latitude, float pressure, float friction, float coriolisPower, float verticalWindSpeed)
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
		var pressureGradient = new Vector3(pressureDifferential.x, pressureDifferential.y, (pressureDifferential.x + pressureDifferential.y) / 4) * Data.pressureDifferentialWindSpeed;
		var newWind = Quaternion.Euler(0, 0, coriolisPower * 90) * pressureGradient;
		newWind.z = verticalWindSpeed;
		return newWind;

	}

}
