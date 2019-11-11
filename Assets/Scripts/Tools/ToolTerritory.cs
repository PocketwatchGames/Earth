using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public class ToolTerritory : GameTool
{
	public WorldComponent World;
	private Vector2Int _lastTileToggled;
	private bool _toggleOn;

	override public void OnSelect() {
		base.OnSelect();
//		World.ShowLayers = WorldComponent.Layers.Plates;
	}
	override public void OnDeselect() {
		base.OnDeselect();
	}
	public void Update()
	{
		if (!Active)
		{
			return;
		}

		var wp = World.ScreenToWorld(Input.mousePosition);
		var p = new Vector2Int((int)wp.x, (int)wp.y);
		if (Input.GetMouseButtonDown(0))
		{
			_lastTileToggled = new Vector2Int(-1, 0);
			_toggleOn = true;
			var herd = World.World.States[World.World.CurStateIndex].Herds[World.HerdSelected];
			for (int i = 0; i < herd.DesiredTileCount; i++)
			{
				if (herd.DesiredTiles[i] == p)
				{
					_toggleOn = false;
				}
			}
		}
		if (Input.GetMouseButton(0))
		{
			World.World.ApplyInput((nextState) =>
			{
				if (p != _lastTileToggled)
				{
					var herd = World.World.States[World.World.CurStateIndex].Herds[World.HerdSelected];
					int desiredTileCount = herd.DesiredTileCount;
					if (_toggleOn)
					{
						if (desiredTileCount < Herd.MaxDesiredTiles)
						{
							for (int i = 0; i < herd.DesiredTileCount; i++)
							{
								if (herd.DesiredTiles[i] == p)
								{
									goto FoundIt;
								}
							}
							nextState.Herds[World.HerdSelected].DesiredTiles[desiredTileCount] = p;
							nextState.Herds[World.HerdSelected].DesiredTileCount = desiredTileCount + 1;
						FoundIt:;
						}
					}
					else
					{
						for (int i = 0; i < desiredTileCount; i++)
						{
							if (herd.DesiredTiles[i] == p)
							{
								for (int j = i; j < desiredTileCount - 1; j++)
								{
									nextState.Herds[World.HerdSelected].DesiredTiles[j] = herd.DesiredTiles[j + 1];

								}
								nextState.Herds[World.HerdSelected].DesiredTileCount = desiredTileCount - 1;
								break;
							}
						}
					}
					_lastTileToggled = p;
				}
			});
		}

	}
}

