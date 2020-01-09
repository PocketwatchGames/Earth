using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileInfoPanel : MonoBehaviour
{
	public TileInfoSubPanel GlobalPanel;
	public TileInfoSubPanel GeoPanel;
	public TileInfoSubPanel EnergyPanel;
	public TileInfoSubPanel UpperAtmospherePanel;
	public TileInfoSubPanel LowerAtmospherePanel;
	public TileInfoSubPanel ShallowWaterPanel;
	public TileInfoSubPanel DeepWaterPanel;
	public TileInfoSubPanel TerrainPanel;
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
		string textGlobal;
		string textEnergy;
		string textGeo;
		string textUpperAtmosphere;
		string textLowerAtmosphere;
		string textSurfaceOcean = "";
		string textDeepOcean = "";
		string textTerrain = "";

		int totalTiles = WorldComponent.World.Size * WorldComponent.World.Size;
		var totalReflected = state.GlobalEnergySolarReflectedCloud + state.GlobalEnergySolarReflectedAtmosphere + state.GlobalEnergySolarReflectedSurface;
		var totalOutgoing = state.GlobalEnergyThermalOutAtmosphericWindow + state.GlobalEnergyThermalOutAtmosphere;
		textGlobal = "Cloud Coverage: " + (state.GlobalCloudCoverage * 100).ToString("0.0") + "%";
		textGlobal += "\nGlobal Sea Level: " + (state.GlobalSeaLevel).ToString("0.00");
		textGlobal += "\nOcean Coverage: " + (state.GlobalOceanCoverage * 100).ToString("0.0") + "%";
		textGlobal += "\nOcean Volume: " + (state.GlobalOceanVolume / 1000000000).ToString("0.00") + " B";
		textGlobal += "\nTemperature: " + WorldComponent.ConvertTemperature(state.GlobalTemperature / totalTiles, WorldComponent.TemperatureDisplay).ToString("0.00") + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
		textGlobal += "\nAtmospheric Mass: " + (state.AtmosphericMass / 1000).ToString("0") + " K";
		textGlobal += "\nCloud Mass: " + (state.GlobalCloudMass / totalTiles).ToString("0.00");
		textGlobal += "\nWater Vapor: " + (state.GlobalWaterVapor / totalTiles).ToString("0.00");
		textGlobal += "\nRainfall: " + (state.GlobalRainfall * WorldComponent.Data.TicksPerYear / (totalTiles * WorldComponent.Data.MassWater)).ToString("0.00");
		textGlobal += "\nEvaporation: " + (state.GlobalEvaporation * WorldComponent.Data.TicksPerYear / (totalTiles * WorldComponent.Data.MassWater)).ToString("0.00");
		float iceEnergy = state.GlobalIceMass * WorldComponent.Data.FreezingTemperature * WorldComponent.Data.SpecificHeatIce;
		float globalEnergy = state.GlobalEnergyUpperAir + state.GlobalEnergyLowerAir + state.GlobalEnergyLand + state.GlobalEnergyShallowWater + state.GlobalEnergyDeepWater + iceEnergy;
		textGlobal += "\nEnergy Total: " + (globalEnergy / 1000000).ToString("0") + " MJ";
		textGlobal += "\nEnergy Upper Air: " + (state.GlobalEnergyUpperAir / 1000000).ToString("0") + " MJ";
		textGlobal += "\nEnergy Lower Air: " + (state.GlobalEnergyLowerAir / 1000000).ToString("0") + " MJ";
		textGlobal += "\nEnergy S Water: " + (state.GlobalEnergyShallowWater / 1000000).ToString("0") + " MJ";
		textGlobal += "\nEnergy D Water: " + (state.GlobalEnergyDeepWater / 1000000).ToString("0") + " MJ";
		textGlobal += "\nEnergy Ice: " + (iceEnergy / 1000000).ToString("0") + " MJ";
		textGlobal += "\nEnergy Land: " + (state.GlobalEnergyLand / 1000000).ToString("0") + " MJ";
		GlobalPanel.SetText(textGlobal);
		textEnergy = "\nDelta: " + ConvertTileEnergyToWatts((state.GlobalEnergyIncoming - totalReflected - totalOutgoing) / totalTiles).ToString("0.0");
		textEnergy += "\nS Incoming: " + ConvertTileEnergyToWatts(state.GlobalEnergyIncoming / totalTiles).ToString("0.0");
		textEnergy += "\nS Reflected: " + ConvertTileEnergyToWatts((totalReflected) / totalTiles).ToString("0.0");
		textEnergy += "\nS Reflected Cloud: " + ConvertTileEnergyToWatts(state.GlobalEnergySolarReflectedCloud / totalTiles).ToString("0.0");
		textEnergy += "\nS Reflected Atmos: " + ConvertTileEnergyToWatts(state.GlobalEnergySolarReflectedAtmosphere / totalTiles).ToString("0.0");
		textEnergy += "\nS Reflected Surf: " + ConvertTileEnergyToWatts(state.GlobalEnergySolarReflectedSurface / totalTiles).ToString("0.0");
		textEnergy += "\nS Abs Atm Total: " + ConvertTileEnergyToWatts(state.GlobalEnergySolarAbsorbedAtmosphere / totalTiles).ToString("0.0");
		textEnergy += "\nS Abs Clouds: " + ConvertTileEnergyToWatts(state.GlobalEnergySolarAbsorbedCloud / totalTiles).ToString("0.0");
		textEnergy += "\nS Abs Surface Total: " + ConvertTileEnergyToWatts(state.GlobalEnergySolarAbsorbedSurface / totalTiles).ToString("0.0");
		textEnergy += "\nS Abs Ocean: " + ConvertTileEnergyToWatts(state.GlobalEnergySolarAbsorbedOcean / totalTiles).ToString("0.0");
		textEnergy += "\nT Outgoing: " + ConvertTileEnergyToWatts(totalOutgoing / totalTiles).ToString("0.0");
		textEnergy += "\nT Out Atm Window: " + ConvertTileEnergyToWatts(state.GlobalEnergyThermalOutAtmosphericWindow / totalTiles).ToString("0.0");
		textEnergy += "\nT Out Radiation: " + ConvertTileEnergyToWatts(state.GlobalEnergyThermalOutAtmosphere / totalTiles).ToString("0.0");
		textEnergy += "\nT Surface Radiation: " + ConvertTileEnergyToWatts(state.GlobalEnergyThermalSurfaceRadiation / totalTiles).ToString("0.0");
		textEnergy += "\nT Atm Absorbed: " + ConvertTileEnergyToWatts(state.GlobalEnergyThermalAbsorbedAtmosphere / totalTiles).ToString("0.0");
		textEnergy += "\nT Back Radiation: " + ConvertTileEnergyToWatts(state.GlobalEnergyThermalBackRadiation / totalTiles).ToString("0.0");
		textEnergy += "\nEvapotranspiration: " + ConvertTileEnergyToWatts(state.GlobalEnergyEvapotranspiration / totalTiles).ToString("0.0");
		textEnergy += "\nSurface Conduction: " + ConvertTileEnergyToWatts(state.GlobalEnergySurfaceConduction / totalTiles).ToString("0.0");
		textEnergy += "\nOcean Radiation: " + ConvertTileEnergyToWatts(state.GlobalEnergyThermalOceanRadiation / (totalTiles * state.GlobalOceanCoverage)).ToString("0.0");
		textEnergy += "\nOcean Conduction: " + ConvertTileEnergyToWatts(state.GlobalEnergyOceanConduction / (totalTiles * state.GlobalOceanCoverage)).ToString("0.0");
		EnergyPanel.SetText(textEnergy);
		if (TileInfoPoint.x >= 0 && TileInfoPoint.x < WorldComponent.World.Size && TileInfoPoint.y >= 0 && TileInfoPoint.y < WorldComponent.World.Size)
		{
			float elevation = state.Elevation[index];

			textGeo = "Index: " + index;
			textGeo += "\nPlate: " + state.Plate[index];
			textGeo += "\nElevation: " + (int)elevation;
			GeoPanel.SetText(textGeo);
			float relativeHumidity = Sim.Atmosphere.GetRelativeHumidity(WorldComponent.World, state.LowerAirTemperature[index], state.Humidity[index], state.LowerAirMass[index], inverseDewPointTemperatureRange);
			textUpperAtmosphere = "Temperature: " + (int)WorldComponent.ConvertTemperature(state.UpperAirTemperature[index], WorldComponent.TemperatureDisplay) + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
			textUpperAtmosphere += "\nPressure: " + (state.UpperAirPressure[index]).ToString("0");
			textUpperAtmosphere += "\nMass: " + (state.UpperAirMass[index]).ToString("0");
			textUpperAtmosphere += "\nWind: " + (state.UpperWind[index].x).ToString("0") + ", " + state.UpperWind[index].y.ToString("0");
			textUpperAtmosphere += "\nDewpoint: " + Sim.Atmosphere.GetDewPoint(WorldComponent.World, state.LowerAirTemperature[index], relativeHumidity).ToString("0.00");
			textUpperAtmosphere += "\nCloud Mass: " + state.CloudMass[index].ToString("0.00");
			textUpperAtmosphere += "\nRaindrop Mass: " + state.RainDropMass[index].ToString("0.00");
			textUpperAtmosphere += "\nRainfall: " + (state.Rainfall[index] / WorldComponent.Data.MassWater*100).ToString("0.00") + "cm";
			UpperAtmospherePanel.SetText(textUpperAtmosphere);
			textLowerAtmosphere = "Temperature: " + (int)WorldComponent.ConvertTemperature(state.LowerAirTemperature[index], WorldComponent.TemperatureDisplay) + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
			textLowerAtmosphere += "\nPressure: " + (state.LowerAirPressure[index]).ToString("0");
			textLowerAtmosphere += "\nMass: " + (state.LowerAirMass[index]).ToString("0");
			textLowerAtmosphere += "\nWind: " + (state.LowerWind[index]).ToString("0.0");
			textLowerAtmosphere += "\nHumidity (Abs): " + state.Humidity[index].ToString("0");
			textLowerAtmosphere += "\nHumidity (Rel): " + relativeHumidity.ToString("0.00");
			textLowerAtmosphere += "\nEvaporation: " + (state.Evaporation[index] / WorldComponent.World.Data.MassWater*100).ToString("0.00") + "cm";
			LowerAtmospherePanel.SetText(textLowerAtmosphere);
			if (state.ShallowWaterMass[index] > 0)
			{
				textSurfaceOcean = "Surface Elevation: " + (elevation + state.WaterAndIceDepth[index]).ToString("0.0000");
				textSurfaceOcean += "\nWater Depth: " + state.WaterDepth[index].ToString("0.0000");
				textSurfaceOcean += "\nTemperature: " + (int)WorldComponent.ConvertTemperature(state.ShallowWaterTemperature[index], WorldComponent.TemperatureDisplay) + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
				textSurfaceOcean += "\nSalinity: " + (1000 * state.ShallowSaltMass[index] / state.ShallowWaterMass[index]).ToString("0.00");
				textSurfaceOcean += "\nMass: " + (uint)(state.ShallowWaterMass[index]);
				textSurfaceOcean += "\nEnergy: " + (uint)(state.ShallowWaterEnergy[index] / 1000) + "K";
				textSurfaceOcean += "\nCurrent: " + state.ShallowWaterCurrent[index].ToString("0.0");
			}
			ShallowWaterPanel.SetText(textSurfaceOcean);
			if (state.DeepWaterMass[index] > 0)
			{
				textDeepOcean = "Temperature: " + (int)WorldComponent.ConvertTemperature(Sim.Atmosphere.GetWaterTemperature(WorldComponent.World, state.DeepWaterEnergy[index], state.DeepWaterMass[index], state.DeepSaltMass[index]), WorldComponent.TemperatureDisplay) + ((WorldComponent.TemperatureDisplay == WorldComponent.TemperatureDisplayType.Celsius) ? "C" : "F");
				textDeepOcean += "\nSalinity: " + (1000 * state.DeepSaltMass[index] / state.DeepWaterMass[index]).ToString("0.00");
				textDeepOcean += "\nDensity: " + state.DeepWaterDensity[index].ToString("0.00");
				textDeepOcean += "\nMass: " + (uint)(state.DeepWaterMass[index]);
				textDeepOcean += "\nEnergy: " + (uint)(state.DeepWaterEnergy[index] / 1000) + "K";
				textDeepOcean += "\nCurrent: " + state.DeepWaterCurrent[index].x.ToString("0.00") + ", " + state.DeepWaterCurrent[index].y.ToString("0.00");
			}
			DeepWaterPanel.SetText(textDeepOcean);

			textTerrain = "Ice: " + (state.IceMass[index] / WorldComponent.Data.MassIce).ToString("0.00");
			textTerrain += "\nCanopy: " + (int)(state.Canopy[index]);
			textTerrain += "\nSoil Fertility: " + (int)(state.SoilFertility[index] * 100);
			textTerrain += "\nWater Table Depth: " + (int)state.WaterTableDepth[index];
			textTerrain += "\nGround Water: " + (state.GroundWater[index] / WorldComponent.Data.MassWater).ToString("0.00");
			textTerrain += "\nLand Energy: " + state.LandEnergy[index].ToString("0.0");
			textTerrain += "\nSurface Abs: " + ConvertTileEnergyToWatts(state.EnergyAbsorbed[index]).ToString("0.0");
			TerrainPanel.SetText(textTerrain);
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


	}
}
