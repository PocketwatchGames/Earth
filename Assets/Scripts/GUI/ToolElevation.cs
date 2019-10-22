using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolElevation : GameTool
{
	public ElevationInfoPanel ElevationInfoPanel;

	private float Direction { get { return ElevationInfoPanel.ActiveToolIndex == 0 ? 1 : -1; } }
	private float BrushSize { get { return ElevationInfoPanel.BrushSize.value; } }


	// Start is called before the first frame update
	void Start()
	{
		ElevationInfoPanel.gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{

	}

	public override void OnSelect()
	{
		ElevationInfoPanel.gameObject.SetActive(true);
	}
	public override void OnDeselect()
	{
		ElevationInfoPanel.gameObject.SetActive(false);
	}
}
