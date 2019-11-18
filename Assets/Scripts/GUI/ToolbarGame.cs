using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolbarGame : Toolbar
{
	override public void Start()
	{
		base.Start();
		int startIndex = 0;
		base.OnClick(startIndex);
		Buttons[startIndex].GetComponent<GameTool>()?.OnSelect();
	}
	override public void OnClick(int index)
	{
		Buttons[ActiveToolIndex].GetComponent<GameTool>()?.OnDeselect();
		base.OnClick(index);
		Buttons[index].GetComponent<GameTool>()?.OnSelect();
	}
}
