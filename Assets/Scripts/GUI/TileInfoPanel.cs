using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileInfoPanel : MonoBehaviour
{
	public UnityEngine.UI.Text TextGeo;
	public UnityEngine.UI.Text TextUpperAtmosphere;
	public UnityEngine.UI.Text TextLowerAtmosphere;
	public UnityEngine.UI.Text TextSurfaceOcean;
	public UnityEngine.UI.Text TextDeepOcean;
	public UnityEngine.UI.Text TextTerrain;
	public UnityEngine.GameObject OceanPanel;
	public UnityEngine.GameObject TerrainPanel;
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
		string textGeo = "";
		string textUpperAtmosphere = "";
		string textLowerAtmosphere = "";
		string textSurfaceOcean = "";
		string textDeepOcean = "";
		string textTerrain = "";
		if (TileInfoPoint.x >= 0 && TileInfoPoint.x < World.World.Size && TileInfoPoint.y >= 0 && TileInfoPoint.y < World.World.Size)
		{
			float elevation = state.Elevation[index];
			textGeo += "GEOLOGY";
			textGeo += "\nIndex: " + index;
			textGeo += "\nPlate: " + state.Plate[index];
			textGeo += "\nElevation: " + (int)elevation;
			textGeo += "\nWaterTableDepth: " + (int)state.WaterTableDepth[index];
			textGeo += "\nSurfaceIce: " + state.SurfaceIce[index].ToString("0.00");

			textUpperAtmosphere += "UPPER ATMOS";
			textUpperAtmosphere += "\nTemperature: " + (int)World.ConvertTemperature(state.UpperAirTemperature[index], World.TemperatureDisplay) + ((World.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
			textUpperAtmosphere += "\nPressure: " + (state.UpperAirPressure[index]).ToString("0.00");
			textUpperAtmosphere += "\nWind: " + (state.UpperWind[index]).ToString();
			textUpperAtmosphere += "\nCloudCover: " + state.CloudCover[index].ToString("0.00");
			textUpperAtmosphere += "\nRainfall: " + (state.Rainfall[index] * World.World.Data.TicksPerYear).ToString("0.00");

			textLowerAtmosphere += "LOWER ATMOS";
			textLowerAtmosphere += "\nTemperature: " + (int)World.ConvertTemperature(state.LowerAirTemperature[index], World.TemperatureDisplay) + ((World.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
			textLowerAtmosphere += "\nPressure: " + (state.LowerAirPressure[index]).ToString("0.00");
			textLowerAtmosphere += "\nWind: " + (state.LowerWind[index]).ToString();
			textLowerAtmosphere += "\nHumidity: " + state.Humidity[index].ToString("0.00");
			textLowerAtmosphere += "\nEvaporation: " + (state.Evaporation[index] * World.World.Data.TicksPerYear).ToString("0.00");


			if (World.World.IsOcean(elevation, state.SeaLevel))
			{
				textSurfaceOcean += "SURFACE OCEAN";
				textSurfaceOcean += "\nTemperature: " + (int)World.ConvertTemperature(Sim.Atmosphere.GetWaterTemperature(World.World, state.OceanEnergyShallow[index], World.World.Data.DeepOceanDepth), World.TemperatureDisplay) + ((World.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
				textSurfaceOcean += "\nSalinity: " + state.OceanSalinityShallow[index].ToString("0.00");
				textSurfaceOcean += "\nCurrent: " + state.OceanCurrentShallow[index].ToString();

				textDeepOcean += "DEEP OCEAN";
				textDeepOcean += "\nTemperature: " + (int)World.ConvertTemperature(Sim.Atmosphere.GetWaterTemperature(World.World, state.OceanEnergyDeep[index], state.SeaLevel - elevation), World.TemperatureDisplay) + ((World.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
				textDeepOcean += "\nSalinity: " + state.OceanSalinityDeep[index].ToString("0.00");
				textDeepOcean += "\nDensity: " + state.OceanDensityDeep[index].ToString("0.00");
				textDeepOcean += "\nCurrent: " + state.OceanCurrentDeep[index].ToString();
				OceanPanel.SetActive(true);
				TerrainPanel.SetActive(false);
			}
			else
			{
				textTerrain += "TERRAIN";
				textTerrain += "\nCanopy: " + (int)(state.Canopy[index] * 100);
				textTerrain += "\nSoilFertility: " + (int)(state.SoilFertility[index] * 100);
				textTerrain += "\nGroundWater: " + state.GroundWater[index].ToString("0.00");
				textTerrain += "\nSurfaceWater: " + state.SurfaceWater[index].ToString("0.00");
				TerrainPanel.SetActive(true);
				OceanPanel.SetActive(false);
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

		TextGeo.text = textGeo;
		TextUpperAtmosphere.text = textUpperAtmosphere;
		TextLowerAtmosphere.text = textLowerAtmosphere;
		TextTerrain.text = textTerrain;
		TextSurfaceOcean.text = textSurfaceOcean;
		TextDeepOcean.text = textDeepOcean;

	}
}
