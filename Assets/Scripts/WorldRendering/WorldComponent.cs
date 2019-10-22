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

	public Camera camera;
	public World World;
	public Layers ShowLayers;
	public Vector2Int TileInfoPoint;

	public Vector2 CameraPos = new Vector2(50, 50);
	public float Zoom { get { return 0.5f + 2.5f * (float)Mathf.Pow(ZoomLevel + 0.5f, 4); } }
	public float ZoomLevel = 0.5f;


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

		Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		float zoom = Input.GetAxis("Zoom");
		camera.transform.position += new Vector3(move.x, move.y, 0) * Time.deltaTime;
		ZoomLevel = Mathf.Clamp01(ZoomLevel + zoom * Time.deltaTime);

		camera.orthographicSize = Zoom;

		World.Update(Time.deltaTime);
		UpdateMesh(ShowLayers, Time.deltaTime);
	}



}
