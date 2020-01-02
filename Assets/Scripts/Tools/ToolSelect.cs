using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolSelect : GameTool
{
	public TileInfoPanel TileInfoPanel;
	public WorldComponent World;
	public bool TileSelected;

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

		if (!TileSelected)
		{
			var p = World.ScreenToWorld(Input.mousePosition);
			TileInfoPanel.TileInfoPoint = p;
		}

		if (Input.GetMouseButton(0))
		{
			TileSelected = true;
		} else if (Input.GetMouseButton(1))
		{
			TileSelected = false;
		}



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
