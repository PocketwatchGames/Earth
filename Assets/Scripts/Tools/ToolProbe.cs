using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

//public class ToolProbe : Tool
//{
//	public int ProbeIndex;
//	override public void OnSelect() { }
//	override public void OnDeselect() { }
//	//override public void DrawWorld(SpriteBatch spriteBatch, World.State state) {
//	//	var probe = Gui.World.Probes[ProbeIndex];
//	//	spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
//	//	spriteBatch.Draw(
//	//		Gui.whiteTex,
//	//		new Vector2(probe.Position.X * Gui.World.tileRenderSize + Gui.World.tileRenderSize / 2, probe.Position.Y * Gui.World.tileRenderSize + Gui.World.tileRenderSize / 2),
//	//		null,
//	//		Color.magenta * 0.5f,
//	//		0,
//	//		new Vector2(0.5f, 0.5f),
//	//		8,
//	//		SpriteEffects.None,
//	//		0);
//	//	spriteBatch.End();
//	//}

//	//private void DrawDataPoint(SpriteBatch spriteBatch, int sampleIndex, float value, float min, float max, Color color)
//	//{
//	//	spriteBatch.Draw(Gui.whiteTex, new Rect(sampleIndex, (int)(1000 - Mathf.Clamp01((value - min) / (max - min)) * 400), 1, 1), null, color);
//	//}
//	//override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
//	//{
//	//	Tool.DrawInfoTooltip(spriteBatch, Gui, state);
//	//	var probe = Gui.World.Probes[ProbeIndex];

//	//	int y = 1000;
//	//	for (int i=0;i< probe.TotalSamples;i++)
//	//	{
//	//		int index = (i + probe.CurSampleIndex - probe.TotalSamples + Probe.SampleCount) % Probe.SampleCount;
//	//		DrawDataPoint(spriteBatch, i, probe.Temperature[index], Gui.World.Data.FreezingTemperature - 60, Gui.World.Data.FreezingTemperature + 60, Color.red);
//	//		DrawDataPoint(spriteBatch, i, probe.Pressure[index], Gui.World.Data.StaticPressure - 5000, Gui.World.Data.StaticPressure + 1000, Color.yellow);
//	//		DrawDataPoint(spriteBatch, i, probe.Humidity[index], 0, 6, Color.blue);
//	//		DrawDataPoint(spriteBatch, i, probe.CloudCover[index], 0, 6, Color.gray);
//	//		DrawDataPoint(spriteBatch, i, probe.Rainfall[index], 0, 10.0f / Gui.World.Data.TicksPerYear, Color.cyan);
//	//		//DrawDataPoint(spriteBatch, i, probe.GroundWater[index], 0, 5, Color.Brown);
//	//		//DrawDataPoint(spriteBatch, i, probe.SurfaceWater[index], 0, 5, Color.Teal);
//	//		DrawDataPoint(spriteBatch, i, probe.Canopy[index], 0, 5, Color.green);
//	//	}
//	//}
//	override public void Update(float dt, Vector2Int p) { }
//	override public void OnMouseDown(Vector2Int p) {
//		var probe = Gui.World.Probes[ProbeIndex];
//		probe.Position = p;
//		probe.TotalSamples = 0;
//	}
//	override public void OnMouseUp(Vector2Int p) { }
//	override public void OnMouseWheel(float delta) { }
//}

public class Probe {
	public const int SampleCount = 1000;
	public Vector2Int Position;
	public int CurSampleIndex;
	public int TotalSamples;
	public float[] AirTemperature = new float[SampleCount];
	public float[] Pressure = new float[SampleCount];
	public float[] Humidity = new float[SampleCount];
	public float[] CloudCover = new float[SampleCount];
	public float[] Rainfall = new float[SampleCount];
	public float[] Evaporation = new float[SampleCount];
	public float[] GroundWater = new float[SampleCount];
	public float[] Canopy = new float[SampleCount];

	public void Update(World world, World.State state)
	{
		int index = world.GetIndex(Position.x, Position.y);
		AirTemperature[CurSampleIndex] = state.LowerAirTemperature[index];
		Pressure[CurSampleIndex] = state.LowerAirPressure[index];
		Humidity[CurSampleIndex] = state.Humidity[index];
		CloudCover[CurSampleIndex] = state.CloudMass[index];
		Rainfall[CurSampleIndex] = state.Rainfall[index];
		GroundWater[CurSampleIndex] = state.GroundWater[index];
		Canopy[CurSampleIndex] = state.Canopy[index];
		CurSampleIndex = (CurSampleIndex + 1) % SampleCount;
		TotalSamples = Math.Min(SampleCount, TotalSamples + 1);
	}

}
