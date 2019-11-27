using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

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
		LowerAirTemperature = 1 << 12,
		TemperatureSubtle = 1 << 13,
		LowerAirWind = 1 << 14,
		LowerAirPressure = 1 << 15,
		RelativeHumidity = 1 << 16,
		Rainfall = 1 << 17,
		Probes = 1 << 18,
		WaterVapor = 1 << 19,
		Plates = 1 << 20,
		OceanCurrentShallow = 1 << 21,
		OceanCurrentDeep = 1 << 22,
		OceanTemperatureShallow = 1 << 23,
		OceanTemperatureDeep = 1 << 24,
		OceanSalinityShallow = 1 << 25,
		OceanSalinityDeep = 1 << 26,
		UpperAirTemperature = 1 << 27,
		UpperAirPressure = 1 << 28,
		UpperAirWind = 1 << 29,
		HeatAbsorbed = 1 << 30,
		Evaporation = 1 << 31,
	}
	public enum TemperatureDisplayType {
		Kelvin,
		Celsius,
		Farenheit
	}

	[Header("Data")]
	public WorldGenData WorldGenData;
	public WorldData Data;

	[Header("Display")]
	public Layers ShowLayers;
	public float ElevationScale = 0.002f;
	public float MinZoom;
	public float MaxZoom;
	public float Zoom { get { return MinZoom + MaxZoom * (float)Mathf.Pow(ZoomLevel, 3); } }
	public float ZoomLevel = 0.5f;
	public float CameraMoveSpeed = 2;
	public float CameraZoomSpeed = 2;
	public TemperatureDisplayType TemperatureDisplay;
	public float minPressure = 300;
	public float maxPressure = 600;
	public float MinElevation = -11000;
	public float MaxElevation = 10000;
	public float maxHumidity = 300;
	public float maxRainfall = 5.0f;
	public float minCloudsToDraw = 10f;
	public float maxCloudsWidth = 0.5f;
	public float maxCloudAlpha = 50.0f;
	public float maxCloudColor = 300.0f;


	[Header("Internal")]
	public Camera MainCamera;
	public World World;
	public GameObject WorldIcons;
	public MeshFilter LandMesh;
	public MeshFilter CloudMesh;
	public MeshFilter OceanMesh;
	public GameObject ArrowPrefab;
	public HerdIcon HerdIconPrefab;
	public GameObject TerritoryMarker;
	public List<Sprite> SpeciesSprites;

	// events
	public event Action WorldStartedEvent;
	public event Action HerdSelectedEvent;

	public int HerdSelected { get; private set; }

	#region private vars

	private GameObject[] _windArrows;
	private HerdIcon[] _herdIcons;
	private GameObject[] _territoryMarkers;

	#endregion
	

	// Start is called before the first frame update
	void Start()
    {
		World = new World();

		//ActiveFeatures = SimFeature.All;
		//ActiveFeatures &= ~(SimFeature.Evaporation);
		//		ActiveFeatures &= ~(SimFeature.TradeWinds);

		Data.Init(WorldGenData.Size);
		WorldGen.Generate(World, SpeciesSprites, Data, WorldGenData);
		CreateWorldMesh();
		MainCamera.transform.position = new Vector3(World.Size / 2, World.Size / 2, MainCamera.transform.position.z);

		_windArrows = new GameObject[World.Size* World.Size];
		for (int i = 0; i < World.Size; i++)
		{
			for (int j = 0; j < World.Size; j++)
			{
				var a = GameObject.Instantiate<GameObject>(ArrowPrefab, this.transform);
				a.transform.position = new Vector3(i, j, 0);
				a.SetActive(false);
				a.hideFlags = HideFlags.HideInHierarchy;
				_windArrows[i+j* World.Size] = a;
			}
		}

		_herdIcons = new HerdIcon[World.MaxHerds];
		for (int i = 0; i < World.MaxHerds; i++)
		{
			var icon = HerdIcon.Instantiate<HerdIcon>(HerdIconPrefab, WorldIcons.transform);
			icon.gameObject.hideFlags = HideFlags.HideInHierarchy;
			icon.World = this;
			icon.gameObject.SetActive(false);
			_herdIcons[i] = icon;
		}

		_territoryMarkers = new GameObject[Herd.MaxActiveTiles];
		for (int i = 0; i < Herd.MaxActiveTiles; i++)
		{
			var marker = GameObject.Instantiate<GameObject>(TerritoryMarker, WorldIcons.transform);
			marker.gameObject.hideFlags = HideFlags.HideInHierarchy;
			marker.gameObject.SetActive(false);
			_territoryMarkers[i] = marker;
		}

		WorldStartedEvent?.Invoke();
		HerdSelected = -1;

		World.Start();
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
			bool isActive = World.States[World.CurRenderStateIndex].Herds[i].Population > 0 && speciesIndex >= 0;
			_herdIcons[i].gameObject.SetActive(isActive);
			if (isActive)
			{
				_herdIcons[i].SpeciesImage.sprite = World.SpeciesDisplay[speciesIndex].Sprite;
				var herdPos = World.States[World.CurRenderStateIndex].Herds[i].Status.Position;
				_herdIcons[i].transform.position = new Vector3(herdPos.x, herdPos.y, -10);
				_herdIcons[i].HerdIndex = i;
			}
		}

		for (int i=0;i<Herd.MaxActiveTiles;i++)
		{
			bool visible = false;
			if (HerdSelected >= 0)
			{
				var herd = World.States[World.CurRenderStateIndex].Herds[HerdSelected];
				if (i < herd.DesiredTileCount)
				{
					visible = true;
					_territoryMarkers[i].transform.position = new Vector3(herd.DesiredTiles[i].x, herd.DesiredTiles[i].y, -11);
				}
			}
			_territoryMarkers[i].SetActive(visible);
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
	public void OnWindFilterChanged(UnityEngine.UI.Toggle toggle)
	{
		for (int i = 0; i < World.Size * World.Size; i++)
		{
			_windArrows[i].SetActive(toggle.isOn);
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
