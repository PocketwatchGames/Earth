using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolElevation : GameTool
{
	public ElevationInfoPanel ElevationInfoPanel;
	public WorldComponent World;
	public float DeltaPerSecond;

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
		if (!Active)
		{
			return;
		}
		var p = World.ScreenToWorld(Input.mousePosition);

		if (Input.GetMouseButton(0))
		{
			World.World.ApplyInput((nextState) =>
			{
				for (int i = -Mathf.CeilToInt(BrushSize); i <= Mathf.CeilToInt(BrushSize); i++)
				{
					for (int j = -Mathf.CeilToInt(BrushSize); j <= Mathf.CeilToInt(BrushSize); j++)
					{
						float dist = Mathf.Sqrt(i * i + j * j);
						if (dist <= BrushSize)
						{
							float distT = (BrushSize == 0) ? 1.0f : (1.0f - Mathf.Pow(dist / BrushSize, 2));
							int x = World.World.WrapX(p.x + i);
							int y = p.y + j;
							if (y < 0 || y >= World.World.Size)
							{
								continue;
							}
							int index = World.World.GetIndex(x, y);
							nextState.Elevation[index] += Direction * distT * DeltaPerSecond * Time.deltaTime;
						}
					}
				}
			});
		}
	}

	public override void OnSelect()
	{
		base.OnSelect();
		ElevationInfoPanel.gameObject.SetActive(true);
	}
	public override void OnDeselect()
	{
		base.OnDeselect();
		ElevationInfoPanel.gameObject.SetActive(false);
	}
}
