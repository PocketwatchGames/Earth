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
		var p = World.ScreenToWorld(Input.mousePosition);
		TileInfoPanel.TileInfoPoint = p;
		TileInfoPanel.gameObject.SetActive(p.x >= 0 && p.x < World.World.Size && p.y >= 0 && p.y < World.World.Size);
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
