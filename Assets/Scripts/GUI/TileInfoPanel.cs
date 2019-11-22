﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileInfoPanel : MonoBehaviour
{
	public UnityEngine.UI.Text Text;
	public WorldComponent World;
	public Vector2Int TileInfoPoint;

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		int index = World.World.GetIndex(TileInfoPoint.x, TileInfoPoint.y);
		var state = World.World.States[World.World.CurRenderStateIndex];
		string text = "";
		if (TileInfoPoint.x >= 0 && TileInfoPoint.x < World.World.Size && TileInfoPoint.y >= 0 && TileInfoPoint.y < World.World.Size)
		{
			float elevation = state.Elevation[index];
			text += "Index: " + index;
			text += "\nPlate: " + state.Plate[index];
			text += "\nElevation: " + (int)elevation;
			text += "\nCloudCover: " + state.CloudCover[index].ToString("0.00");
			text += "\nRainfall: " + (state.Rainfall[index] * World.World.Data.TicksPerYear).ToString("0.00");
			text += "\nUpper Temp: " + (int)World.ConvertTemperature(state.UpperAirTemperature[index], World.TemperatureDisplay) + ((World.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
			text += "\nUpper Pressure: " + (state.UpperAirPressure[index]).ToString("0.00");
			text += "\nLower Temp: " + (int)World.ConvertTemperature(state.LowerAirTemperature[index], World.TemperatureDisplay) + ((World.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
			text += "\nLower Pressure: " + (state.LowerAirPressure[index]).ToString("0.00");
			text += "\nHumidity: " + state.Humidity[index].ToString("0.00");
			text += "\nEvaporation: " + (state.Evaporation[index] * World.World.Data.TicksPerYear).ToString("0.00");
			text += "\nWaterTableDepth: " + (int)state.WaterTableDepth[index];
			text += "\nSurfaceIce: " + state.SurfaceIce[index].ToString("0.00");
			text += "\nSoilFertility: " + (int)(state.SoilFertility[index] * 100);

			if (World.World.IsOcean(elevation, state.SeaLevel))
			{
				text += "\nShallow Temp: " + (int)World.ConvertTemperature(Sim.Atmosphere.GetWaterTemperature(World.World, state.OceanEnergyShallow[index], World.Data.DeepOceanDepth), World.TemperatureDisplay) + ((World.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
				text += "\nDeep Temp: " + (int)World.ConvertTemperature(Sim.Atmosphere.GetWaterTemperature(World.World, state.OceanEnergyDeep[index], state.SeaLevel - elevation), World.TemperatureDisplay) + ((World.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
				text += "\nShallow Salinity: " + state.OceanSalinityShallow[index].ToString("0.00");
				text += "\nDeep Salinity: " + state.OceanSalinityDeep[index].ToString("0.00");
				text += "\nDensity: " + state.OceanDensityDeep[index].ToString("0.00");
			}
			else
			{
				text += "\nGroundWater: " + state.GroundWater[index].ToString("0.00");
				text += "\nSurfaceWater: " + state.SurfaceWater[index].ToString("0.00");
				text += "\nCanopy: " + (int)(state.Canopy[index] * 100);
				text += "\nTerrain Energy: " + (int)(state.LandEnergy[index]);
			}
			//	spriteBatch.DrawString(font, "Wind: " + Wind[index], new Vector2(5, textY += 15), Color.White);
			//for (int s = 0; s < World.MaxGroupsPerTile; s++)
			//{
			//	int groupIndex = state.AnimalsPerTile[index * World.MaxGroupsPerTile + s];
			//	if (groupIndex >= 0 && state.Animals[groupIndex].Population > 0)
			//	{
			//		int speciesIndex = state.Animals[groupIndex].Species;
			//		spriteBatch.Draw(gui.whiteTex, new Rect(5, textY += 15, 10, 10), null, state.Species[speciesIndex].Color);
			//		float population = state.Animals[groupIndex].Population;
			//		spriteBatch.DrawString(gui.Font,
			//			state.Species[speciesIndex].Name + ": " + ((int)population),
			//			new Vector2(25, textY), Color.white);
			//	}
			//}
		}

		Text.text = text;

	}
}
