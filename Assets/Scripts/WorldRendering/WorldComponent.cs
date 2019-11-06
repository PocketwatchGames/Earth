using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class WorldComponent : MonoBehaviour
{
	[System.Flags]
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
	public enum TemperatureDisplayType {
		Kelvin,
		Celsius,
		Farenheit
	}

	public Layers ShowLayers;
	public float ElevationScale = 0.002f;
	public float MinZoom;
	public float MaxZoom;
	public float Zoom { get { return MinZoom + MaxZoom * (float)Mathf.Pow(ZoomLevel, 3); } }
	public float ZoomLevel = 0.5f;
	public float CameraMoveSpeed = 2;
	public float CameraZoomSpeed = 2;
	public TemperatureDisplayType TemperatureDisplay;

	[Header("Internal")]
	public Camera MainCamera;
	public World World;
	public MeshFilter LandMesh;
	public MeshFilter CloudMesh;
	public MeshFilter OceanMesh;
	public GameObject ArrowPrefab;

	// events
	public event Action WorldStartedEvent;


	#region private vars

	private int _size = 100;
	private GameObject[] _windArrows;
	
	#endregion


	// Start is called before the first frame update
	void Start()
    {
		World = new World();
		World.Init(_size);
		World.Generate();
		CreateWorldMesh();
		MainCamera.transform.position = new Vector3(World.Size / 2, World.Size / 2, MainCamera.transform.position.z);

		_windArrows = new GameObject[_size*_size];
		for (int i = 0; i < _size; i++)
		{
			for (int j = 0; j < _size; j++)
			{
				var a = GameObject.Instantiate<GameObject>(ArrowPrefab);
				a.transform.position = new Vector3(i, j, 0);
				a.transform.parent = this.transform;
				a.active = false;
				a.hideFlags = HideFlags.HideInHierarchy;
				_windArrows[i+j*_size] = a;
			}
		}

		WorldStartedEvent?.Invoke();
	}


	// Update is called once per frame
	void Update()
    {

		float zoom = Input.GetAxis("Zoom");
		ZoomLevel = Mathf.Clamp01(ZoomLevel + zoom * Time.deltaTime * CameraZoomSpeed);

		Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		MainCamera.transform.position += new Vector3(move.x, move.y, 0) * Time.deltaTime * Zoom * CameraMoveSpeed;

		MainCamera.orthographicSize = Zoom;

		World.Update(Time.deltaTime);
		UpdateMesh(ShowLayers, Time.deltaTime);

	}

	public Vector2Int ScreenToWorld(Vector3 screenPoint)
	{
		var p = MainCamera.ScreenToWorldPoint(screenPoint);
		return new Vector2Int((int)p.x, (int)p.y);
	}

	public void OnCelsiusChanged(bool value)
	{
		if (value)
		{
			TemperatureDisplay = TemperatureDisplayType.Celsius;
		}
	}
	public void OnFarenheitChanged(bool value)
	{
		if (value)
		{
			TemperatureDisplay = TemperatureDisplayType.Farenheit;
		}
	}
	public void OnWindFilterChanged(bool value)
	{
		for (int i = 0; i < _size * _size; i++)
		{
			_windArrows[i].active = value;
		}

	}

	public float ConvertTemperature(float kelvin, TemperatureDisplayType displayType)
	{
		if (displayType == TemperatureDisplayType.Celsius)
		{
			return kelvin - World.Data.FreezingTemperature;
		} else if (displayType == TemperatureDisplayType.Farenheit)
		{
			return (kelvin - World.Data.FreezingTemperature) * 9.0f / 5.0f + 32;
		}
		return kelvin;
	}

}
