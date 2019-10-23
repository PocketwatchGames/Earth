using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilterPanel : MonoBehaviour
{
	public WorldComponent World;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void OnFilterChangedElevation(bool value)
	{
		World.ShowLayers ^= WorldComponent.Layers.Elevation;
	}
	public void OnFilterChangedTemperature(bool value)
	{
		World.ShowLayers ^= WorldComponent.Layers.Temperature;
	}
	public void OnFilterChangedPressure(bool value)
	{
		World.ShowLayers ^= WorldComponent.Layers.Pressure;
	}
	public void OnFilterChangedHumidity(bool value)
	{
		World.ShowLayers ^= WorldComponent.Layers.WaterVapor;
	}
	public void OnFilterChangedWind(bool value)
	{
		World.ShowLayers ^= WorldComponent.Layers.Wind;
	}
	public void OnFilterChangedClouds(bool value)
	{
		World.ShowLayers ^= WorldComponent.Layers.CloudCoverage;
	}
}
