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
	public UnityEngine.UI.Text TextGlobal;
	public UnityEngine.GameObject GeoPanel;
	public UnityEngine.GameObject AtmospherePanel;
	public UnityEngine.GameObject OceanPanel;
	public UnityEngine.GameObject TerrainPanel;
	public WorldComponent WorldComponent;
	public Vector2Int TileInfoPoint;

	// Start is called before the first frame update
	void Start()
    {
        
    }

	float ConvertTileEnergyToWatts(float energy)
	{
		return energy * 1000 / WorldComponent.World.Data.SecondsPerTick;
	}

    // Update is called once per frame
    void Update()
    {
		float inverseDewPointTemperatureRange = 1.0f / WorldComponent.World.Data.DewPointTemperatureRange;
		int index = WorldComponent.World.GetIndex(TileInfoPoint.x, TileInfoPoint.y);
		var state = WorldComponent.World.States[WorldComponent.World.CurRenderStateIndex];
		string textGeo = "";
		string textUpperAtmosphere = "";
		string textLowerAtmosphere = "";
		string textSurfaceOcean = "";
		string textDeepOcean = "";
		string textTerrain = "";
		string textGlobal = "";

		int totalTiles = WorldComponent.World.Size * WorldComponent.World.Size;
		var totalReflected = state.GlobalEnergyReflectedCloud + state.GlobalEnergyReflectedAtmosphere + state.GlobalEnergyReflectedSurface;
		var totalOutgoing = state.GlobalEnergyOutAtmosphericWindow + state.GlobalEnergyOutEmittedAtmosphere;
		textGlobal += "GLOBAL";
		textGlobal += "\nCloud Coverage: " + (state.GlobalCloudCoverage * 100).ToString("0.0") + "%";
		textGlobal += "\nOcean Coverage: " + (state.GlobalOceanCoverage * 100).ToString("0.0") + "%";
		textGlobal += "\nOcean Volume: " + (state.GlobalOceanVolume/1000000000).ToString("0.00") + " B";
		textGlobal += "\nAtmospheric Mass: " + (state.AtmosphericMass/1000).ToString("0") + " K";
		textGlobal += "\nENERGY";
		textGlobal += "\nTotal: " + (state.GlobalEnergy / 1000000).ToString("0") + " MJ";
		textGlobal += "\nDelta: " + ConvertTileEnergyToWatts((state.GlobalEnergyIncoming - totalReflected - totalOutgoing) / totalTiles).ToString("0.0");
		textGlobal += "\nIncoming: " + ConvertTileEnergyToWatts(state.GlobalEnergyIncoming / totalTiles).ToString("0.0");
		textGlobal += "\nReflected: " + ConvertTileEnergyToWatts((totalReflected) / totalTiles).ToString("0.0");
		textGlobal += "\nOutgoing: " + ConvertTileEnergyToWatts(totalOutgoing / totalTiles).ToString("0.0");
		textGlobal += "\nOut Atm Window: " + ConvertTileEnergyToWatts(state.GlobalEnergyOutAtmosphericWindow / totalTiles).ToString("0.0");
		textGlobal += "\nOut Atm Radiation: " + ConvertTileEnergyToWatts(state.GlobalEnergyOutEmittedAtmosphere / totalTiles).ToString("0.0");
		textGlobal += "\nReflected Cloud: " + ConvertTileEnergyToWatts(state.GlobalEnergyReflectedCloud / totalTiles).ToString("0.0");
		textGlobal += "\nReflected Atmos: " + ConvertTileEnergyToWatts(state.GlobalEnergyReflectedAtmosphere / totalTiles).ToString("0.0");
		textGlobal += "\nReflected Surf: " + ConvertTileEnergyToWatts(state.GlobalEnergyReflectedSurface / totalTiles).ToString("0.0");
		textGlobal += "\nAbs Clouds: " + ConvertTileEnergyToWatts(state.GlobalEnergyAbsorbedCloud / totalTiles).ToString("0.0");
		textGlobal += "\nAbs Atm: " + ConvertTileEnergyToWatts(state.GlobalEnergyAbsorbedAtmosphere / totalTiles).ToString("0.0");
		textGlobal += "\nAbs Surface: " + ConvertTileEnergyToWatts(state.GlobalEnergyAbsorbedSurface / totalTiles).ToString("0.0");
		textGlobal += "\nAbs Ocean: " + ConvertTileEnergyToWatts(state.GlobalEnergyAbsorbedOcean / totalTiles).ToString("0.0");
		textGlobal += "\nOcean Radiation: " + ConvertTileEnergyToWatts(state.GlobalEnergyOceanRadiation / (totalTiles * state.GlobalOceanCoverage)).ToString("0.0");
		textGlobal += "\nOcean Conduction: " + ConvertTileEnergyToWatts(state.GlobalEnergyOceanConduction / (totalTiles * state.GlobalOceanCoverage)).ToString("0.0");
		textGlobal += "\nOcean Evap Heat: " + ConvertTileEnergyToWatts(state.GlobalEnergyOceanEvapHeat / (totalTiles * state.GlobalOceanCoverage)).ToString("0.0");

		if (TileInfoPoint.x >= 0 && TileInfoPoint.x < WorldComponent.World.Size && TileInfoPoint.y >= 0 && TileInfoPoint.y < WorldComponent.World.Size)
		{
			GeoPanel.SetActive(true);
			AtmospherePanel.SetActive(true);
			float elevation = state.Elevation[index];
			textGeo += "GEOLOGY";
			textGeo += "\nIndex: " + index;
			textGeo += "\nPlate: " + state.Plate[index];
			textGeo += "\nElevation: " + (int)elevation;
			textGeo += "\nIce: " + state.Ice[index].ToString("0.00");
			textGeo += "\nSurface Abs: " + ConvertTileEnergyToWatts(state.EnergyAbsorbed[index]).ToString("0.0");

			float relativeHumidity = Sim.Atmosphere.GetRelativeHumidity(WorldComponent.World, state.LowerAirTemperature[index], state.Humidity[index], state.LowerAirMass[index], inverseDewPointTemperatureRange);
			textUpperAtmosphere += "UPPER ATMOSPHERE";
			textUpperAtmosphere += "\nTemperature: " + (int)WorldComponent.ConvertTemperature(state.UpperAirTemperature[index], WorldComponent.TemperatureDisplay) + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
			textUpperAtmosphere += "\nPressure: " + (state.UpperAirPressure[index]).ToString("0");
			textUpperAtmosphere += "\nMass: " + (state.UpperAirMass[index]).ToString("0");
			textUpperAtmosphere += "\nWind: " + (state.UpperWind[index].x).ToString("0") + ", " + state.UpperWind[index].y.ToString("0");
			textUpperAtmosphere += "\nDewpoint: " + Sim.Atmosphere.GetDewPoint(WorldComponent.World, state.LowerAirTemperature[index], relativeHumidity).ToString("0.00");
			textUpperAtmosphere += "\nCloud Mass: " + state.CloudMass[index].ToString("0.00");
			textUpperAtmosphere += "\nRaindrop Mass: " + state.RainDropMass[index].ToString("0.00");
			textUpperAtmosphere += "\nRainfall: " + (state.Rainfall[index] * WorldComponent.World.Data.TicksPerYear).ToString("0.00");

			textLowerAtmosphere += "LOWER ATMOSPHERE";
			textLowerAtmosphere += "\nTemperature: " + (int)WorldComponent.ConvertTemperature(state.LowerAirTemperature[index], WorldComponent.TemperatureDisplay) + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
			textLowerAtmosphere += "\nPressure: " + (state.LowerAirPressure[index]).ToString("0");
			textLowerAtmosphere += "\nMass: " + (state.LowerAirMass[index]).ToString("0");
			textLowerAtmosphere += "\nWind: " + (state.LowerWind[index]).ToString("0.0");
			textLowerAtmosphere += "\nHumidity (Abs): " + state.Humidity[index].ToString("0");
			textLowerAtmosphere += "\nHumidity (Rel): " + relativeHumidity.ToString("0.00");
			textLowerAtmosphere += "\nEvaporation: " + (state.Evaporation[index] * WorldComponent.World.Data.TicksPerYear).ToString("0.00");


			if (WorldComponent.World.IsOcean(state.WaterDepth[index]))
			{
				textSurfaceOcean += "SURFACE OCEAN";
				textSurfaceOcean += "\nTemperature: " + (int)WorldComponent.ConvertTemperature(state.OceanTemperatureShallow[index], WorldComponent.TemperatureDisplay) + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
				textSurfaceOcean += "\nSalinity: " + (state.OceanSalinityShallow[index] / WorldComponent.World.Data.DeepOceanDepth).ToString("0.00");
				textSurfaceOcean += "\nEnergy: " + (uint)(state.OceanEnergyShallow[index]/1000) + "K";
				textSurfaceOcean += "\nCurrent: " + state.OceanCurrentShallow[index].ToString("0.0");

				textDeepOcean += "DEEP OCEAN";
				textDeepOcean += "\nTemperature: " + (int)WorldComponent.ConvertTemperature(Sim.Atmosphere.GetWaterTemperature(WorldComponent.World, state.OceanEnergyDeep[index], state.WaterDepth[index]), WorldComponent.TemperatureDisplay) + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
				textDeepOcean += "\nSalinity: " + (state.OceanSalinityDeep[index] / state.WaterDepth[index]).ToString("0.00");
				textDeepOcean += "\nDensity: " + state.OceanDensityDeep[index].ToString("0.00");
				textDeepOcean += "\nEnergy: " + (uint)(state.OceanEnergyDeep[index]/1000) + "K";
				textDeepOcean += "\nCurrent: " + state.OceanCurrentDeep[index].x.ToString("0.00") + ", " + state.OceanCurrentDeep[index].y.ToString("0.00");
				OceanPanel.SetActive(true);
				TerrainPanel.SetActive(false);
			}
			else
			{
				textTerrain += "TERRAIN";
				textTerrain += "\nCanopy: " + (int)(state.Canopy[index] * 100);
				textTerrain += "\nSoil Fertility: " + (int)(state.SoilFertility[index] * 100);
				textTerrain += "\nWater Table Depth: " + (int)state.WaterTableDepth[index];
				textTerrain += "\nGround Water: " + state.GroundWater[index].ToString("0.00");
				textTerrain += "\nSurface Water: " + state.SurfaceWater[index].ToString("0.00");
				textTerrain += "\nLand Energy: " + state.LandEnergy[index].ToString("0.0");
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
		} else
		{
			GeoPanel.SetActive(false);
			AtmospherePanel.SetActive(false);
			OceanPanel.SetActive(false);
			TerrainPanel.SetActive(false);
		}

		TextGlobal.text = textGlobal;
		TextGeo.text = textGeo;
		TextUpperAtmosphere.text = textUpperAtmosphere;
		TextLowerAtmosphere.text = textLowerAtmosphere;
		TextTerrain.text = textTerrain;
		TextSurfaceOcean.text = textSurfaceOcean;
		TextDeepOcean.text = textDeepOcean;

	}
}
