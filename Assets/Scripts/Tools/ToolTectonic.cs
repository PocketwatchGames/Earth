using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public class ToolTectonic : Tool
{
	public float BrushSize = 1;
	public float DeltaPerSecond = 100;
	public bool Active;
	public Vector2Int Start;
	public int StartPlate;

	override public void OnSelect() { Active = false; }
	override public void OnDeselect() { Active = false; }
	//override public void DrawWorld(SpriteBatch spriteBatch, World.State state)
	//{
	//	if (!Active)
	//	{
	//		var p = Gui.TileInfoPoint;
	//		StartPlate = Gui.World.States[Gui.World.CurStateIndex].Plate[Gui.World.GetIndex(p.X, p.Y)];
	//	}
	//	spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
	//	for (int i = 0; i < Gui.World.Size; i++)
	//	{
	//		for (int j = 0; j < Gui.World.Size; j++)
	//		{
	//			if (Gui.World.States[Gui.World.CurStateIndex].Plate[Gui.World.GetIndex(i, j)] == StartPlate)
	//			{
	//				Rect rect = new Rect(i * Gui.World.tileRenderSize, j * Gui.World.tileRenderSize, Gui.World.tileRenderSize, Gui.World.tileRenderSize);
	//				spriteBatch.Draw(Gui.whiteTex, rect, Color.white * 0.2f);
	//			}
	//		}
	//	}
	//	spriteBatch.End();
	//}
	//override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
	//{
	//}
	override public void Update(float dt, Vector2Int p)
	{
		if (Active)
		{
			lock (Gui.World.InputLock)
			{
				if (Gui.TileInfoPoint != Start)
				{
					int nextStateIndex = Gui.World.AdvanceState();

					Vector2 diff = new Vector2(Gui.TileInfoPoint.x - Start.x, Gui.TileInfoPoint.y - Start.y);
					Vector2Int move;
					if (Math.Abs(diff.x) > Math.Abs(diff.y))
					{
						move = new Vector2Int(Math.Sign(diff.x), 0);
					} else
					{
						move = new Vector2Int(0, Math.Sign(diff.y));
					}
					Gui.World.MovePlate(Gui.World.States[Gui.World.CurStateIndex], Gui.World.States[nextStateIndex], StartPlate, move);

					Start = Gui.TileInfoPoint;

					Gui.World.CurStateIndex = nextStateIndex;
				}



			}
		}
	}
	override public void OnMouseDown(Vector2Int p)
	{
		Active = true;
		Start =  Gui.TileInfoPoint;
		StartPlate = Gui.World.States[Gui.World.CurStateIndex].Plate[Gui.World.GetIndex(p.x, p.y)];
	}
	override public void OnMouseUp(Vector2Int p)
	{
		Active = false;
	}
	override public void OnMouseWheel(float delta)
	{
		BrushSize = Mathf.Clamp(BrushSize + delta / 100, 0, 50);
	}
}

