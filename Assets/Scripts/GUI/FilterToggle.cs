using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

class FilterToggle : MonoBehaviour {
	public WorldComponent World;
	public WorldComponent.Layers Layer;

	void Start()
	{
		GetComponent<Toggle>().isOn = World.ShowLayers.HasFlag(Layer);
	}

	public void OnFilterChanged(bool value)
	{
		if (value)
		{
			World.ShowLayers |= Layer;
		} else
		{
			World.ShowLayers &= ~Layer;
		}

	}
}
