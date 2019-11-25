using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimePanel : MonoBehaviour
{
	public UnityEngine.UI.Text TimeText;
	public WorldComponent WorldComponent;

	public float _tickTimer;
	public int _ticksLastSecond, _ticksToDisplay;

	// Start is called before the first frame update
	void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
		var state = WorldComponent.World.States[WorldComponent.World.CurRenderStateIndex];

		_tickTimer += Time.deltaTime;
		if (_tickTimer >= 1)
		{
			_tickTimer -= 1;
			_ticksToDisplay = state.Ticks - _ticksLastSecond;
			_ticksLastSecond = state.Ticks;
		}

		TimeText.text = ((int)(WorldComponent.World.GetTimeOfYear(state.Ticks) * 12)).ToString() + "/" + ((int)(WorldComponent.World.GetYear(state.Ticks) * 12)).ToString() + " [x" + ((int)WorldComponent.World.TimeScale) + "] Actual: " + _ticksToDisplay + " Ticks: " + state.Ticks;

	}

}
