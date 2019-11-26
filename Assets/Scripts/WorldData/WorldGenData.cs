﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class WorldGenData {

	public int Size = 100;
	public float MaxElevation = 10000.0f;
	public float MinElevation = -11500.0f;
	public float StratosphereMass = 2583;
	public float TroposphereMass = 7749;
	public float MinTemperature = 253.15f;
	public float MaxTemperature = 323.15f;
	public float CarbonDioxide = 0.001f;
	public float PlanetTiltAngle = -23.5f;
	public float SolarRadiation = 118; // extraterrestrial solar radiation // https://en.wikipedia.org/wiki/Sunlight (1367 w/m^2) *seconds per day (86400)

}
