using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileInfoPanel : MonoBehaviour
{
	public UnityEngine.UI.Text Text;
	public WorldComponent World;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		int index = World.World.GetIndex(World.TileInfoPoint.x, World.TileInfoPoint.y);
		var state = World.World.States[World.World.CurRenderStateIndex];
		string text = "";
		text += "Index: " + index;
		text += "\nPlate: " + state.Plate[index];
		text += "\nElevation: " + (int)state.Elevation[index];
		text += "\nTemperature: " + (int)(state.Temperature[index] - World.World.Data.FreezingTemperature);
		text += "\nPressure: " + (state.Pressure[index] / World.World.Data.StaticPressure).ToString("0.00");
		text += "\nHumidity: " + state.Humidity[index].ToString("0.00");
		text += "\nCloudCover: " + state.CloudCover[index].ToString("0.00");
		text += "\nRainfall: " + (state.Rainfall[index] * World.World.Data.TicksPerYear).ToString("0.00");
		text += "\nEvaporation: " + (state.Evaporation[index] * World.World.Data.TicksPerYear).ToString("0.00");
		text += "\nWaterTableDepth: " + (int)state.WaterTableDepth[index];
		text += "\nGroundWater: " + state.GroundWater[index].ToString("0.00");
		text += "\nSurfaceWater: " + state.SurfaceWater[index].ToString("0.00");
		text += "\nSurfaceIce: " + state.SurfaceIce[index].ToString("0.00");
		text += "\nSoilFertility: " + (int)(state.SoilFertility[index] * 100);
		text += "\nCanopy: " + (int)(state.Canopy[index] * 100);
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

		Text.text = text;

	}
}
