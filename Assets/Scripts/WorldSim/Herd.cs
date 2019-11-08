using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct Herd {

	public struct DisplayStatus {
		public Vector2 Position;
		public int Population;
		public float Food;
		public float Water;
		public float Health;
		public float Social;
		public float Comfort;
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

}
