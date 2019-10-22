using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class WorldComponent : MonoBehaviour
{
	public enum Layers {
		None = 0,
		Elevation = 1 << 0,
		ElevationSubtle = 1 << 1,
		Gradient = 1 << 2,
		Water = 1 << 3,
		SoilFertility = 1 << 4,
		WaterTableDepth = 1 << 5,
		GroundWater = 1 << 6,
		Vegetation = 1 << 7,
		Animals = 1 << 8,
		CloudCoverage = 1 << 9,
		CloudHeight = 1 << 11,
		Temperature = 1 << 12,
		TemperatureSubtle = 1 << 13,
		Wind = 1 << 14,
		Pressure = 1 << 15,
		RelativeHumidity = 1 << 16,
		Rainfall = 1 << 17,
		Probes = 1 << 18,
		WaterVapor = 1 << 19,
		Plates = 1 << 20,
	}

	World World;
	public Layers ShowLayers;

	int size = 100;

	// Start is called before the first frame update
	void Start()
    {
		World = new World();
		World.Init(size);
		World.Generate();
		ShowLayers = Layers.Probes | Layers.ElevationSubtle | Layers.TemperatureSubtle | Layers.Water | Layers.SoilFertility | Layers.Vegetation | Layers.Animals;
		CreateWorldMesh();
	}


	// Update is called once per frame
	void Update()
    {
		World.Update(Time.deltaTime);
		UpdateMesh((Layers)ShowLayers, Time.deltaTime);
	}



}
