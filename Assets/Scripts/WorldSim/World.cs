using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public struct SpeciesType {
	public enum FoodType {
		Herbivore,
		Omnivore,
		Carnivore
	}
	public FoodType Food;
	public float RestingTemperature;
	public float TemperatureRange;
	public float Lifespan;
	public float speciesGrowthRate;
	public float speciesEatRate;
	public float starvationSpeed;
	public float dehydrationSpeed;
	public float speciesMaxPopulation;
	public float MovementSpeed;
}


public struct SpeciesStat {
	public float Population;
}

public struct SpeciesDisplayData {
	public string Name;
	public Color Color;
	public Sprite Sprite;
	public int SpeciesIndex;
}
public enum Direction {
	West,
	East,
	South,
	North
}

public partial class World {

	public enum MutationType {
		Size,
		Reproduction,
		Health
	}

	//		float MaxCloudElevation = ;
	public int MaxHerds;
	public int Size;
	public float TimeTillTick = 0.00001f;
	public float TimeScale = 1.0f;
	public float TicksPerSecond = 1.0f;
	public const int StateCount = 4;
	public const int MaxGroupsPerTile = 16;
	public int[] Neighbors;
	public State[] States = new State[StateCount];
	const int ProbeCount = 3;
	public Probe[] Probes = new Probe[ProbeCount];
	public int CurStateIndex;
	public int CurRenderStateIndex;
	public int LastRenderStateIndex;
	public object DrawLock = new object();
	public object InputLock = new object();
	public SpeciesDisplayData[] SpeciesDisplay;
	private Task _simTask;
	private bool _threaded;

	public class State {
		public int Ticks;
		public float AtmosphereCO2;
		public float AtmosphereO2;
		public float AtmosphereN;
		public float StratosphereMass;
		public float CarbonDioxide;
		public float PlanetTiltAngle;
		public float SolarRadiation;
		public float PlanetRadius;
		public float PlanetRotationSpeed;

		public SpeciesType[] Species;
		public SpeciesStat[] SpeciesStats;
		public Herd[] Herds;

		public int[] Plate;
		public float[] Elevation;
		public float[] LandEnergy;
		public float[] LowerAirEnergy;
		public float[] LowerAirTemperature;
		public float[] LowerAirPressure;
		public float[] LowerAirMass;
		public float[] UpperAirEnergy;
		public float[] UpperAirTemperature;
		public float[] UpperAirPressure;
		public float[] UpperAirMass;
		public float[] Humidity;
		public float[] CloudMass;
		public float[] RainDropMass;
		public float[] WaterTableDepth;
		public float[] GroundWater;
		public float[] WaterDepth;
		public float[] WaterAndIceDepth;
		public float[] ShallowWaterMass;
		public float[] ShallowWaterEnergy;
		public float[] ShallowSaltMass;
		public float[] ShallowWaterTemperature;
		public float[] DeepWaterMass;
		public float[] DeepWaterEnergy;
		public float[] DeepSaltMass;
		public float[] DeepWaterDensity;
		public float[] IceMass;
		public float[] SoilFertility;
		public float[] Canopy;
		public float[] Radiation;
		public int[] AnimalsPerTile;
		public Vector3[] ShallowWaterCurrent;
		public Vector3[] DeepWaterCurrent;
		public Vector3[] UpperWind;
		public Vector3[] LowerWind;
		public Vector4[] ShallowWaterFlow;
		public Vector2[] FlowDirectionGroundWater;
		public Vector3[] Normal;

		// for display
		public float[] Rainfall;
		public float[] Evaporation;
		public float[] EnergyAbsorbed;
		public float GlobalEnergyIncoming;
		public float GlobalEnergyReflectedCloud;
		public float GlobalEnergyReflectedAtmosphere;
		public float GlobalEnergyReflectedSurface;
		public float GlobalEnergyGained;
		public float GlobalEnergyAbsorbedCloud;
		public float GlobalEnergyAbsorbedAtmosphere;
		public float GlobalEnergyAbsorbedSurface;
		public float GlobalEnergyAbsorbedOcean;
		public float GlobalEnergyOceanRadiation;
		public float GlobalEnergyOceanConduction;
		public float GlobalEnergyEvapotranspiration;
		public float GlobalEnergyOutAtmosphericWindow;
		public float GlobalEnergyOutEmittedAtmosphere;
		public float GlobalEnergy;
		public float GlobalTemperature;
		public float GlobalOceanCoverage;
		public float GlobalOceanVolume;
		public float GlobalSeaLevel;
		public float GlobalCloudCoverage;
		public float GlobalEvaporation;
		public float GlobalRainfall;
		public float AtmosphericMass;

		public void CopyFrom(State from)
		{
			Ticks = from.Ticks;
			AtmosphereCO2 = from.AtmosphereCO2;
			AtmosphereO2 = from.AtmosphereO2;
			AtmosphereN = from.AtmosphereN;
			StratosphereMass = from.StratosphereMass;
			CarbonDioxide = from.CarbonDioxide;
			PlanetRadius = from.PlanetRadius;
			PlanetRotationSpeed = from.PlanetRotationSpeed;
			PlanetTiltAngle = from.PlanetTiltAngle;
			SolarRadiation = from.SolarRadiation;

			int numTiles = from.UpperAirTemperature.Length;
			Array.Copy(from.Species, Species, Species.Length);
			Array.Copy(from.SpeciesStats, SpeciesStats, SpeciesStats.Length);
			Array.Copy(from.Herds, Herds, Herds.Length);
			Array.Copy(from.Plate, Plate, Plate.Length);
			Array.Copy(from.Elevation, Elevation, numTiles);
			Array.Copy(from.WaterTableDepth, WaterTableDepth, numTiles);
			Array.Copy(from.IceMass, IceMass, numTiles);
			Array.Copy(from.SoilFertility, SoilFertility, numTiles);
			Array.Copy(from.Canopy, Canopy, numTiles);
			Array.Copy(from.Radiation, Radiation, numTiles);
			Array.Copy(from.AnimalsPerTile, AnimalsPerTile, numTiles);
			Array.Copy(from.FlowDirectionGroundWater, FlowDirectionGroundWater, numTiles);
			Array.Copy(from.ShallowWaterFlow, ShallowWaterFlow, numTiles);
			Array.Copy(from.Normal, Normal, numTiles);
			Array.Clear(GroundWater, 0, numTiles);
			Array.Clear(UpperAirMass, 0, numTiles);
			Array.Clear(UpperAirEnergy, 0, numTiles);
			Array.Clear(LowerAirMass, 0, numTiles);
			Array.Clear(LowerAirEnergy, 0, numTiles);
			Array.Clear(LandEnergy, 0, numTiles);
			Array.Clear(Humidity, 0, numTiles);
			Array.Clear(CloudMass, 0, numTiles);
			Array.Clear(RainDropMass, 0, numTiles);
			Array.Clear(ShallowWaterMass, 0, numTiles);
			Array.Clear(ShallowSaltMass, 0, numTiles);
			Array.Clear(ShallowWaterEnergy, 0, numTiles);
			Array.Clear(DeepWaterMass, 0, numTiles);
			Array.Clear(DeepWaterEnergy, 0, numTiles);
			Array.Clear(DeepSaltMass, 0, numTiles);

		}
	}

	public World(bool threaded)
	{
		_threaded = threaded;
	}

	public void ApplyInput(Action<State> action)
	{
		lock (InputLock)
		{
			var nextStateIndex = AdvanceState();
			var state = States[CurStateIndex];
			var nextState = States[nextStateIndex];

			action.Invoke(nextState);

			CurStateIndex = nextStateIndex;
		}
	}

	public void Init(int size, WorldData data)
	{
		Size = size;
		int s = Size * Size;
		MaxHerds = 256;
		Data = data;
		SpeciesDisplay = new SpeciesDisplayData[MaxSpecies];

		for (int i = 0; i < StateCount; i++)
		{
			States[i] = new State();
			States[i].Plate = new int[s];
			States[i].Elevation = new float[s];
			States[i].LandEnergy = new float[s];
			States[i].UpperAirEnergy = new float[s];
			States[i].UpperAirTemperature = new float[s];
			States[i].UpperAirPressure = new float[s];
			States[i].UpperAirMass = new float[s];
			States[i].LowerAirEnergy = new float[s];
			States[i].LowerAirTemperature = new float[s];
			States[i].LowerAirPressure = new float[s];
			States[i].LowerAirMass = new float[s];
			States[i].Humidity = new float[s];
			States[i].CloudMass = new float[s];
			States[i].RainDropMass = new float[s];
			States[i].WaterTableDepth = new float[s];
			States[i].GroundWater = new float[s];
			States[i].WaterDepth = new float[s];
			States[i].WaterAndIceDepth = new float[s];
			States[i].EnergyAbsorbed = new float[s];
			States[i].Rainfall = new float[s];
			States[i].Evaporation = new float[s];
			States[i].ShallowSaltMass = new float[s];
			States[i].ShallowWaterMass = new float[s];
			States[i].ShallowWaterEnergy = new float[s];
			States[i].ShallowWaterTemperature = new float[s];
			States[i].DeepWaterMass = new float[s];
			States[i].DeepWaterEnergy = new float[s];
			States[i].DeepSaltMass = new float[s];
			States[i].DeepWaterDensity = new float[s];
			States[i].IceMass = new float[s];
			States[i].SoilFertility = new float[s];
			States[i].Canopy = new float[s];
			States[i].LowerWind = new Vector3[s];
			States[i].UpperWind = new Vector3[s];
			States[i].ShallowWaterCurrent = new Vector3[s];
			States[i].DeepWaterCurrent = new Vector3[s];
			States[i].Radiation = new float[s];
			States[i].ShallowWaterFlow = new Vector4[s];
			States[i].FlowDirectionGroundWater = new Vector2[s];
			States[i].Normal = new Vector3[s];
			States[i].AnimalsPerTile = new int[s * MaxGroupsPerTile];
			for (int j = 0; j < s * MaxGroupsPerTile; j++)
			{
				States[i].AnimalsPerTile[j] = -1;
			}

			States[i].Herds = new Herd[MaxHerds];
			States[i].Species = new SpeciesType[MaxSpecies];
			States[i].SpeciesStats = new SpeciesStat[MaxSpecies];
		}

		for (int i = 0; i < ProbeCount; i++)
		{
			Probes[i] = new Probe();
		}

		Neighbors = new int[Size * Size * 4];
		for (int x=0;x<Size;x++)
		{
			for (int y = 0; y < Size; y++) {
				int index = (x + y * Size)*4;
				Neighbors[index + 0] = GetIndex(GetNeighbor(x, y, 0));
				Neighbors[index + 1] = GetIndex(GetNeighbor(x, y, 1));
				Neighbors[index + 2] = GetIndex(GetNeighbor(x, y, 2));
				Neighbors[index + 3] = GetIndex(GetNeighbor(x, y, 3));
			}
		}
	}

	public void Start() {

		if (_threaded)
		{
			_simTask = Task.Run(() =>
			{
				while (true)
				{
					try
					{
						DoSimTick();
					}
					catch (AggregateException e)
					{
						foreach (var i in e.InnerExceptions)
						{
							Debug.Log(i);
						}
					}
				}

			});
		}
	}

	private void DoSimTick()
	{
		if (TimeTillTick <= 0)
		{
			TimeTillTick += TicksPerSecond;

			lock (InputLock)
			{
				int nextStateIndex = (CurStateIndex + 1) % StateCount;
				lock (DrawLock)
				{
					while (nextStateIndex == LastRenderStateIndex || nextStateIndex == CurRenderStateIndex)
					{
						nextStateIndex = (nextStateIndex + 1) % StateCount;
					}
				}

				States[nextStateIndex].CopyFrom(States[CurStateIndex]);

				Tick(States[CurStateIndex], States[nextStateIndex]);

				// TODO: why can't i edit this in the tick call?  it's a class, so it should be pass by reference?
				States[nextStateIndex].Ticks = States[CurStateIndex].Ticks + 1;
				CurStateIndex = nextStateIndex;
			}
		}
	}

	public void Update(float dt)
	{
		if (TimeTillTick > -1)
		{
			TimeTillTick -= TimeScale * dt;
		}

		if (!_threaded)
		{
			while (TimeTillTick <= 0)
			{
				//try
				//{
					DoSimTick();
				//} catch (Exception e)
				//{
				//	Debug.Break();
				//}
			}
		}
	}

	public int GetIndex(int x, int y)
	{
		return y * Size + x;
	}

	public int GetIndex(Vector2Int p)
	{
		return p.y * Size + p.x;
	}

	public float GetTimeOfYear(int ticks)
	{
		float t = (float)ticks / Data.TicksPerYear;
		return t - (int)t;
	}

	public float GetTimeOfDay(int ticks, float longitude)
	{
		float t = (float)ticks / (Data.TicksPerHour * 24) + longitude;
		return Mathf.Repeat(t, 1);
	}


	public int GetYear(int ticks)
	{
		return ticks / Data.TicksPerYear;
	}

	public float GetLatitude(int y)
	{
		return -(((float)y / Size) * 2 - 1.0f);
	}

	public int AdvanceState()
	{
		int nextStateIndex = (CurStateIndex + 1) % World.StateCount;
		lock (DrawLock)
		{
			while (nextStateIndex == LastRenderStateIndex || nextStateIndex == CurRenderStateIndex)
			{
				nextStateIndex = (nextStateIndex + 1) % World.StateCount;
			}
		}
		States[nextStateIndex].CopyFrom(States[CurStateIndex]);
		return nextStateIndex;
	}

	public bool IsOcean(float waterDepth)
	{
		return waterDepth > 0;
	}


	public int WrapX(int x)
	{
		if (x < 0)
		{
			x += Size;
		}
		else if (x >= Size)
		{
			x -= Size;
		}
		return x;
	}
	public int WrapY(int y)
	{
		return Mathf.Clamp(y, 0, Size - 1);
	}
	public int GetNeighborIndex(int x, int y, int neighborIndex)
	{
		return Neighbors[(x + y * Size) * 4 + neighborIndex];
	}
	public int GetNeighborIndex(int index, int neighborIndex)
	{
		return Neighbors[index * 4 + neighborIndex];
	}
	public Vector2Int GetNeighbor(int x, int y, int neighborIndex)
	{
		switch (neighborIndex)
		{
			case 0:
				x--;
				if (x < 0)
				{
					x += Size;
				}
				break;
			case 1:
				x++;
				if (x >= Size)
				{
					x -= Size;
				}
				break;
			case 2:
				y++;
				if (y >= Size)
				{
					y = Size - 1;
					//						x = (x + Size / 2) % Size;
				}
				break;
			case 3:
				y--;
				if (y < 0)
				{
					y = 0;
					//						x = (x + Size / 2) % Size;
				}
				break;
		}
		return new Vector2Int(x, y);
	}

	private void Tick(State state, State nextState)
	{
		nextState.SpeciesStats = new SpeciesStat[MaxSpecies];

		if (_threaded)
		{
			List<Task> simTasks = new List<Task>();
			simTasks.Add(Task.Run(() =>
			{
				Sim.Geology.Tick(this, state, nextState);
			}));
			simTasks.Add(Task.Run(() =>
			{
				Sim.Wind.Tick(this, state, nextState);
			}));
			simTasks.Add(Task.Run(() =>
			{
				Sim.Atmosphere.Tick(this, state, nextState);
			}));
			simTasks.Add(Task.Run(() =>
			{
				Sim.Animals.Tick(this, state, nextState);
			}));
			//simTasks.Add(Task.Run(() =>
			//{
			//	for (int i = 0; i < ProbeCount; i++)
			//	{
			//		Probes[i].Update(this, state);
			//	}
			//}));
			Task.WaitAll(simTasks.ToArray());
		}
		else
		{
			Sim.Geology.Tick(this, state, nextState);
			Sim.Wind.Tick(this, state, nextState);
			Sim.Atmosphere.Tick(this, state, nextState);
			Sim.Animals.Tick(this, state, nextState);
		}
	}


}
