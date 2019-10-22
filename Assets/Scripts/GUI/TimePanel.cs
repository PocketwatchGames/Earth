using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimePanel : MonoBehaviour
{
	public UnityEngine.UI.Text TimeText;
	public WorldComponent World;

	public float _tickTimer;
	public int _ticksLastSecond, _ticksToDisplay;

	// Start is called before the first frame update
	void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
		var state = World.World.States[World.World.CurRenderStateIndex];

		_tickTimer += Time.deltaTime;
		if (_tickTimer >= 1)
		{
			_tickTimer -= 1;
			_ticksToDisplay = state.Ticks - _ticksLastSecond;
			_ticksLastSecond = state.Ticks;
		}

		TimeText.text = ((int)(World.World.GetTimeOfYear(state.Ticks) * 12)).ToString() + "/" + ((int)(World.World.GetYear(state.Ticks) * 12)).ToString() + " [x" + ((int)World.World.TimeScale) + "] Actual: " + _ticksToDisplay;

	}
}
