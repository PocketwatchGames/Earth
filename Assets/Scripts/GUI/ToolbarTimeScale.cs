using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolbarTimeScale : Toolbar
{
	public WorldComponent World;

	private float[] _timeScales = new float[] { 0, 5, 20, 200 };


	override public void Start()
	{
		base.Start();
		World.World.TimeScale = _timeScales[ActiveToolIndex];
	}
	public override void OnClick(int index)
	{
		base.OnClick(index);
		World.World.TimeScale = _timeScales[index];
	}
}
