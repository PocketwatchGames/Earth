using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolSelect : GameTool
{
	public TileInfoPanel TileInfoPanel;
	public WorldComponent World;

	// Start is called before the first frame update
	void Awake()
    {
		TileInfoPanel.gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
    {
		if (!Active)
		{
			return;
		}
		TileInfoPanel.TileInfoPoint = World.ScreenToWorld(Input.mousePosition);
	}

	public override void OnSelect()
	{
		base.OnSelect();
		TileInfoPanel.gameObject.SetActive(true);
	}
	public override void OnDeselect()
	{
		base.OnDeselect();
		TileInfoPanel.gameObject.SetActive(false);
	}
}
