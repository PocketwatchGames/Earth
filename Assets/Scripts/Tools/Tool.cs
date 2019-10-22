using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public abstract class Tool
{
	public String Name;
	//public Keys HotKey;
	public WorldGUI Gui;

	public abstract void OnSelect();
	public abstract void OnDeselect();
	//public abstract void DrawWorld(SpriteBatch spriteBatch, World.State state);
	//public abstract void DrawTooltip(SpriteBatch spriteBatch, World.State state);
	public abstract void Update(float dt, Vector2Int p);
	public abstract void OnMouseDown(Vector2Int p);
	public abstract void OnMouseUp(Vector2Int p);
	public abstract void OnMouseWheel(float delta);


	//public static void DrawSelection(SpriteBatch spriteBatch, WorldGUI gui, World.State state)
	//{
	//	spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
	//	for (int i = 0; i < gui.AnimalsSelected.Count; i++)
	//	{
	//		int animalIndex = gui.AnimalsSelected[i];
	//		spriteBatch.Draw(
	//			gui.whiteTex,
	//			new Vector2(state.Animals[animalIndex].Position.x * gui.World.tileRenderSize, state.Animals[animalIndex].Position.y * gui.World.tileRenderSize),
	//			null,
	//			Color.magenta * 0.5f,
	//			0,
	//			new Vector2(0.5f, 0.5f),
	//			8,
	//			SpriteEffects.None,
	//			0);

	//	}
	//	spriteBatch.End();
	//}
	//public static void DrawInfoTooltip(SpriteBatch spriteBatch, WorldGUI gui, World.State state)
	//{
	//	int index = gui.World.GetIndex(gui.TileInfoPoint.X, gui.TileInfoPoint.Y);
	//	int textY = 300;

	//	spriteBatch.DrawString(gui.Font, "Index: " + index, new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "Plate: " + state.Plate[index], new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "Elevation: " + (int)(state.Elevation[index]), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "Temperature: " + (int)(state.Temperature[index] - gui.World.Data.FreezingTemperature), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "Pressure: " + (state.Pressure[index] / gui.World.Data.StaticPressure).ToString("0.00"), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "Humidity: " + state.Humidity[index].ToString("0.00"), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "CloudCover: " + state.CloudCover[index].ToString("0.00"), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "Rainfall: " + (state.Rainfall[index] * gui.World.Data.TicksPerYear).ToString("0.00"), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "Evaporation: " + (state.Evaporation[index] * gui.World.Data.TicksPerYear).ToString("0.00"), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "WaterTableDepth: " + (int)state.WaterTableDepth[index], new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "GroundWater: " + state.GroundWater[index].ToString("0.00"), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "SurfaceWater: " + state.SurfaceWater[index].ToString("0.00"), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "SurfaceIce: " + state.SurfaceIce[index].ToString("0.00"), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "SoilFertility: " + (int)(state.SoilFertility[index] * 100), new Vector2(5, textY += 15), Color.white);
	//	spriteBatch.DrawString(gui.Font, "Canopy: " + (int)(state.Canopy[index] * 100), new Vector2(5, textY += 15), Color.white);
	//	//	spriteBatch.DrawString(font, "Wind: " + Wind[index], new Vector2(5, textY += 15), Color.White);
	//	for (int s = 0; s < World.MaxGroupsPerTile; s++)
	//	{
	//		int groupIndex = state.AnimalsPerTile[index * World.MaxGroupsPerTile + s];
	//		if (groupIndex >= 0 && state.Animals[groupIndex].Population > 0)
	//		{
	//			int speciesIndex = state.Animals[groupIndex].Species;
	//			spriteBatch.Draw(gui.whiteTex, new Rect(5, textY += 15, 10, 10), null, state.Species[speciesIndex].Color);
	//			float population = state.Animals[groupIndex].Population;
	//			spriteBatch.DrawString(gui.Font,
	//				state.Species[speciesIndex].Name + ": " + ((int)population),
	//				new Vector2(25, textY), Color.white);
	//		}
	//	}

	//}

}
