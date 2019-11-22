using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolbarTimeScale : Toolbar
{
	public WorldComponent World;

	private float[] _timeScales = new float[] { 0, 5, 20, 200, -1 };

	override public void Start()
	{
		base.Start();

		if (World.World != null)
		{
			OnWorldStart();
		}
		World.WorldStartedEvent += OnWorldStart;
	}

	public void OnWorldStart()
	{
		World.World.TimeScale = _timeScales[ActiveToolIndex];
	}
	public override void OnClick(int index)
	{
		base.OnClick(index);
		World.World.TimeScale = Mathf.Max(0,_timeScales[index]);
		if (_timeScales[index] < 0)
		{
			World.World.TimeTillTick = 0;
		}
	}
	public void OnAdvanceFrameClicked()
	{
		World.World.TimeScale = 0;
		World.World.TimeTillTick = 0;
	}
}
