﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct Herd {
	public const int MaxActiveTiles = 32;
	public const int MaxDesiredTiles = 24;

	public enum Maturity {
		Juvenile,
		Adult,
		Elderly
	}


	public struct DisplayStatus {
		public Vector2 Position;
	}
	public DisplayStatus Status;

	// Simulation variables
	public int SpeciesIndex;
	public float MutationHealth;
	public float MutationReproduction;
	public float MutationSize;
	public float DesiredMutationHealth;
	public float DesiredMutationReproduction;
	public float DesiredMutationSize;

	public float EvolutionProgress;

	public int Population;
	public float Disease;
	public float Food;
	public float Water;
	public float Comfort;
	public float Social;

	public float Birth;


	public int ActiveTileCount;
	public Vector2Int[] ActiveTiles;
	public float[] TilePopulation;
	public int DesiredTileCount;
	public Vector2Int[] DesiredTiles;

}
