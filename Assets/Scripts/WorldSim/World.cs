﻿using System.Collections;
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
	bool threaded = false;

	public class State : ICloneable {
		public int Ticks;
		public float AtmosphereCO2;
		public float AtmosphereO2;
		public float AtmosphereN;
		public float SeaLevel;
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
		public float[] SurfaceWater;
		public float[] OceanSalinityDeep;
		public float[] OceanSalinityShallow;
		public float[] OceanDensityDeep;
		public float[] OceanEnergyDeep;
		public float[] OceanEnergyShallow;
		public float[] OceanTemperatureShallow;
		public Vector3[] OceanCurrentShallow;
		public Vector3[] OceanCurrentDeep;
		public float[] Ice;
		public float[] SoilFertility;
		public float[] Canopy;
		public float[] Radiation;
		public int[] AnimalsPerTile;
		public Vector3[] UpperWind;
		public Vector3[] LowerWind;
		public Vector2[] FlowDirection;
		public Vector3[] Normal;

		// for display
		public float[] Rainfall;
		public float[] Evaporation;
		public float[] EnergyAbsorbed;
		public float GlobalEnergyIncoming;
		public float GlobalEnergyReflectedCloud;
		public float GlobalEnergyReflectedAtmosphere;
		public float GlobalEnergyReflectedSurface;
		public float GlobalEnergyLost;
		public float GlobalEnergyGained;
		public float GlobalEnergyAbsorbedCloud;
		public float GlobalEnergyAbsorbedUpperAtmosphere;
		public float GlobalEnergyAbsorbedLowerAtmosphere;
		public float GlobalEnergyAbsorbedSurface;
		public float GlobalEnergy;
		public float GlobalTemperature;
		public float GlobalOceanCoverage;
		public float AtmosphericMass;

		public object Clone()
		{
			State o = new State();
			o.Ticks = Ticks;
			o.AtmosphereCO2 = AtmosphereCO2;
			o.AtmosphereO2 = AtmosphereO2;
			o.AtmosphereN = AtmosphereN;
			o.GlobalTemperature = GlobalTemperature;
			o.SeaLevel = SeaLevel;
			o.StratosphereMass = StratosphereMass;
			o.CarbonDioxide = CarbonDioxide;
			o.PlanetRadius = PlanetRadius;
			o.PlanetRotationSpeed = PlanetRotationSpeed;
			o.PlanetTiltAngle = PlanetTiltAngle;
			o.SolarRadiation = SolarRadiation;
			o.GlobalEnergyIncoming = GlobalEnergyIncoming;
			o.GlobalEnergyReflectedAtmosphere = GlobalEnergyReflectedAtmosphere;
			o.GlobalEnergyReflectedSurface = GlobalEnergyReflectedSurface;
			o.GlobalEnergyLost = GlobalEnergyLost;
			o.GlobalEnergyGained = GlobalEnergyGained;
			o.GlobalEnergyAbsorbedCloud = GlobalEnergyAbsorbedCloud;
			o.GlobalEnergyAbsorbedUpperAtmosphere = GlobalEnergyAbsorbedUpperAtmosphere;
			o.GlobalEnergyAbsorbedLowerAtmosphere = GlobalEnergyAbsorbedLowerAtmosphere;
			o.GlobalEnergyAbsorbedSurface = GlobalEnergyAbsorbedSurface;
			o.GlobalEnergy = GlobalEnergy;
			o.GlobalTemperature = GlobalTemperature;
			o.GlobalOceanCoverage = GlobalOceanCoverage;
			o.AtmosphericMass = AtmosphericMass;

		o.Species = (SpeciesType[])Species.Clone();
			o.SpeciesStats = (SpeciesStat[])SpeciesStats.Clone();
			o.Herds = (Herd[])Herds.Clone();
			o.Plate = (int[])Plate.Clone();
			o.Elevation = (float[])Elevation.Clone();
			o.UpperAirTemperature = (float[])UpperAirTemperature.Clone();
			o.UpperAirEnergy = (float[])UpperAirEnergy.Clone();
			o.UpperAirPressure = (float[])UpperAirPressure.Clone();
			o.UpperAirMass = (float[])UpperAirMass.Clone();
			o.LowerAirEnergy = (float[])LowerAirEnergy.Clone();
			o.LowerAirTemperature = (float[])LowerAirTemperature.Clone();
			o.LowerAirPressure = (float[])LowerAirPressure.Clone();
			o.LowerAirMass = (float[])LowerAirMass.Clone();
			o.Humidity = (float[])Humidity.Clone();
			o.CloudMass = new float[CloudMass.Length];
			o.RainDropMass = new float[RainDropMass.Length];
			o.WaterTableDepth = (float[])WaterTableDepth.Clone();
			o.GroundWater = (float[])GroundWater.Clone();
			o.SurfaceWater = (float[])SurfaceWater.Clone();
			o.OceanSalinityDeep = (float[])OceanSalinityDeep.Clone();
			o.OceanSalinityShallow = (float[])OceanSalinityShallow.Clone();
			o.OceanDensityDeep = (float[])OceanDensityDeep.Clone();
			o.OceanEnergyDeep = (float[])OceanEnergyDeep.Clone();
			o.OceanEnergyShallow = (float[])OceanEnergyShallow.Clone();
			o.OceanTemperatureShallow = (float[])OceanTemperatureShallow.Clone();
			o.Ice = (float[])Ice.Clone();
			o.SoilFertility = (float[])SoilFertility.Clone();
			o.Canopy = (float[])Canopy.Clone();
			o.Radiation = (float[])Radiation.Clone();
			o.AnimalsPerTile = (int[])AnimalsPerTile.Clone();
			o.LowerWind = (Vector3[])LowerWind.Clone();
			o.UpperWind = (Vector3[])UpperWind.Clone();
			o.OceanCurrentShallow = (Vector3[])OceanCurrentShallow.Clone();
			o.OceanCurrentDeep = (Vector3[])OceanCurrentDeep.Clone();
			o.FlowDirection = (Vector2[])FlowDirection.Clone();
			o.Normal = (Vector3[])Normal.Clone();

			o.EnergyAbsorbed = (float[])EnergyAbsorbed.Clone();
			o.Rainfall = (float[])Rainfall.Clone();
			o.Evaporation = (float[])Evaporation.Clone();

			return o;
		}
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
			States[i].SurfaceWater = new float[s];
			States[i].EnergyAbsorbed = new float[s];
			States[i].Rainfall = new float[s];
			States[i].Evaporation = new float[s];
			States[i].OceanSalinityDeep = new float[s];
			States[i].OceanSalinityShallow = new float[s];
			States[i].OceanDensityDeep = new float[s];
			States[i].OceanEnergyDeep = new float[s];
			States[i].OceanEnergyShallow = new float[s];
			States[i].OceanTemperatureShallow = new float[s];
			States[i].Ice = new float[s];
			States[i].SoilFertility = new float[s];
			States[i].Canopy = new float[s];
			States[i].LowerWind = new Vector3[s];
			States[i].UpperWind = new Vector3[s];
			States[i].OceanCurrentShallow = new Vector3[s];
			States[i].OceanCurrentDeep = new Vector3[s];
			States[i].Radiation = new float[s];
			States[i].FlowDirection = new Vector2[s];
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
	}

	public void Start() {

		if (threaded)
		{
			_simTask = Task.Run(() =>
			{
				while (true)
				{
					try
					{
						DoSimTick(threaded);
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

	private void DoSimTick(bool threaded)
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

				States[nextStateIndex] = (State)States[CurStateIndex].Clone();
				Tick(States[CurStateIndex], States[nextStateIndex], threaded);

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

		if (!threaded)
		{
			DoSimTick(threaded);
		}
	}

	public int GetIndex(int x, int y)
	{
		return y * Size + x;
	}

	public float GetTimeOfYear(int ticks)
	{
		float t = (float)ticks / Data.TicksPerYear;
		return t - (int)t;
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
		States[nextStateIndex] = (State)States[CurStateIndex].Clone();
		return nextStateIndex;
	}

	public bool IsOcean(float elevation, float seaLevel)
	{
		return elevation < seaLevel;
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
		var n = GetNeighbor(x, y, neighborIndex);
		return GetIndex(n.x,n.y);
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

	private void Tick(State state, State nextState, bool threaded)
	{
		nextState.SpeciesStats = new SpeciesStat[MaxSpecies];

		if (threaded)
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
