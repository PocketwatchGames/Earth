using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public class ToolTectonic : GameTool
{
	public WorldComponent World;
	private Vector2Int Start;
	private int StartPlate;
	private WorldComponent.Layers oldLayers;

	override public void OnSelect() {
		oldLayers = World.ShowLayers;
		World.ShowLayers = WorldComponent.Layers.Plates;
	}
	override public void OnDeselect() {
		World.ShowLayers = oldLayers;
	}
	public void Update()
	{
		var p = World.ScreenToWorld(Input.mousePosition);
		if (Input.GetMouseButtonDown(0))
		{
			if (p.x >= 0 && p.y >= 0 && p.x < World.World.Size && p.y < World.World.Size)
			{
				Start = p;
				StartPlate = World.World.States[World.World.CurStateIndex].Plate[World.World.GetIndex(p.x, p.y)];
			} else
			{
				StartPlate = -1;
			}
		}
		if (Input.GetMouseButton(0) && StartPlate >= 0)
		{
			lock (World.World.InputLock)
			{
				if (p != Start)
				{
					int nextStateIndex = World.World.AdvanceState();

					Vector2 diff = new Vector2(p.x - Start.x, p.y - Start.y);
					Vector2Int move;
					if (Math.Abs(diff.x) > Math.Abs(diff.y))
					{
						move = new Vector2Int(Math.Sign(diff.x), 0);
					} else
					{
						move = new Vector2Int(0, Math.Sign(diff.y));
					}
					World.World.MovePlate(World.World.States[World.World.CurStateIndex], World.World.States[nextStateIndex], StartPlate, move);

					Start = p;

					World.World.CurStateIndex = nextStateIndex;
				}



			}
		}
	}
}

