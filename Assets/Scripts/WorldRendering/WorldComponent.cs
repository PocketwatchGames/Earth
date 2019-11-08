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
	public GameObject WorldIcons;
	public MeshFilter LandMesh;
	public MeshFilter CloudMesh;
	public MeshFilter OceanMesh;
	public GameObject ArrowPrefab;
	public HerdIcon HerdIconPrefab;
	public List<Sprite> SpeciesSprites;

	// events
	public event Action WorldStartedEvent;
	public event Action HerdSelectedEvent;

	public int HerdSelected { get; private set; }

	#region private vars

	private int _size = 100;
	private GameObject[] _windArrows;
	private HerdIcon[] _herdIcons;

	#endregion


	// Start is called before the first frame update
	void Start()
    {
		World = new World();
		World.Init(_size);
		World.Generate(SpeciesSprites);
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
				a.SetActive(false);
				a.hideFlags = HideFlags.HideInHierarchy;
				_windArrows[i+j*_size] = a;
			}
		}

		_herdIcons = new HerdIcon[World.MaxHerds];
		for (int i = 0; i < World.MaxHerds; i++)
		{
			var icon = HerdIcon.Instantiate<HerdIcon>(HerdIconPrefab);
			icon.transform.parent = WorldIcons.transform;
			icon.gameObject.hideFlags = HideFlags.HideInHierarchy;
			icon.World = this;
			icon.gameObject.SetActive(false);
			_herdIcons[i] = icon;
		}

		WorldStartedEvent?.Invoke();
		HerdSelected = -1;
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

		for (int i=0;i<World.MaxHerds;i++)
		{
			int speciesIndex = World.States[World.CurRenderStateIndex].Herds[i].SpeciesIndex;
			bool isActive = World.States[World.CurRenderStateIndex].Herds[i].Status.Population > 0 && speciesIndex >= 0;
			_herdIcons[i].gameObject.SetActive(isActive);
			if (isActive)
			{
				_herdIcons[i].SpeciesImage.sprite = World.SpeciesDisplay[speciesIndex].Sprite;
				var herdPos = World.States[World.CurRenderStateIndex].Herds[i].Status.Position;
				_herdIcons[i].transform.position = new Vector3(herdPos.x, herdPos.y, -10);
				_herdIcons[i].HerdIndex = i;
			}
		}

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
			_windArrows[i].SetActive(value);
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

	public void SelectHerd(int index)
	{
		HerdSelected = index;
		HerdSelectedEvent?.Invoke();
	}

	public void SetDesiredMutation(int herdIndex, World.MutationType mutation, float value)
	{
		World.ApplyInput((nextState) =>
		{

			switch (mutation)
			{
				case World.MutationType.Health:
					nextState.Herds[herdIndex].DesiredMutationHealth = value;
					break;
				case World.MutationType.Reproduction:
					nextState.Herds[herdIndex].DesiredMutationReproduction = value;
					break;
				case World.MutationType.Size:
					nextState.Herds[herdIndex].DesiredMutationSize = value;
					break;
			}

		});
	}
}
