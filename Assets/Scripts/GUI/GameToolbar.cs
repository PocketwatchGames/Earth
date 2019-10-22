using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameToolbar : Toolbar
{
	override public void Start()
	{
		base.Start();
		OnClick(0);
	}
	override public void OnClick(int index)
	{
		Buttons[ActiveToolIndex].GetComponent<GameTool>()?.OnDeselect();
		base.OnClick(index);
		Buttons[index].GetComponent<GameTool>()?.OnSelect();
	}
}
