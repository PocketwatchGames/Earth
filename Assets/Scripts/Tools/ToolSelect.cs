using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolSelect : GameTool
{
	public TileInfoPanel TileInfoPanel;
	public WorldComponent World;

	// Start is called before the first frame update
	void Start()
    {
		TileInfoPanel.gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
    {
		TileInfoPanel.TileInfoPoint = World.ScreenToWorld(Input.mousePosition);
	}

	public override void OnSelect()
	{
		TileInfoPanel.gameObject.SetActive(true);
	}
	public override void OnDeselect()
	{
		TileInfoPanel.gameObject.SetActive(false);
	}
}
