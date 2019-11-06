using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevationInfoPanel : Toolbar
{
	public ToolElevation Tool;
	public UnityEngine.UI.Slider BrushSize;

    // Start is called before the first frame update
    void Start()
    {
		base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public override void OnClick(int index)
	{
		base.OnClick(index);
	}
}
