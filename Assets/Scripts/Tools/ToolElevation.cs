//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Unity;
//using UnityEngine;

//public class ToolElevation : Tool
//{
//	public float BrushSize = 1;
//	public float DeltaPerSecond = 100;
//	public bool Active;

//	override public void OnSelect() { Active = false; }
//	override public void OnDeselect() { Active = false; }
//	override public void DrawWorld(SpriteBatch spriteBatch, World.State state)
//	{
//		var p = Gui.TileInfoPoint;
//		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
//		for (int i = (int)-Math.Ceiling(BrushSize); i <= Math.Ceiling(BrushSize); i++)
//		{
//			for (int j = (int)-Math.Ceiling(BrushSize); j <= Math.Ceiling(BrushSize); j++)
//			{
//				float dist = (float)Math.Sqrt(i * i + j * j);
//				if (dist <= BrushSize)
//				{
//					float distT = (BrushSize == 0) ? 1.0f : (1.0f - (float)Math.Pow(dist / BrushSize, 2));
//					int x = Gui.World.WrapX(p.X + i);
//					int y = p.Y + j;
//					if (y < 0 || y >= Gui.World.Size)
//					{
//						continue;
//					}
//					Rect rect = new Rect(x * Gui.World.tileRenderSize, y * Gui.World.tileRenderSize, Gui.World.tileRenderSize, Gui.World.tileRenderSize);
//					spriteBatch.Draw(Gui.whiteTex, rect, Color.white * 0.2f);
//				}
//			}
//		}
//		spriteBatch.End();
//	}
//	override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
//	{
//		int index = Gui.World.GetIndex(Gui.TileInfoPoint.X, Gui.TileInfoPoint.Y);
//		int textY = 300;
//		spriteBatch.DrawString(Gui.Font, "Elevation: " + (int)(state.Elevation[index]), new Vector2(5, textY += 15), Color.white);
//	}
//	override public void Update(float dt, Vector2Int p)
//	{
//		if (Active)
//		{
//			lock (Gui.World.InputLock)
//			{
//				var nextStateIndex = Gui.World.AdvanceState();
//				var state = Gui.World.States[Gui.World.CurStateIndex];
//				var nextState = Gui.World.States[nextStateIndex];

//				for (int i = (int)-Math.Ceiling(BrushSize); i <= Math.Ceiling(BrushSize); i++)
//				{
//					for (int j = (int)-Math.Ceiling(BrushSize); j <= Math.Ceiling(BrushSize); j++)
//					{
//						float dist = (float)Math.Sqrt(i * i + j * j);
//						if (dist <= BrushSize)
//						{
//							float distT = (BrushSize == 0) ? 1.0f : (1.0f - (float)Math.Pow(dist / BrushSize, 2));
//							int x = Gui.World.WrapX(p.x + i);
//							int y = p.y + j;
//							if (y < 0 || y >= Gui.World.Size)
//							{
//								continue;
//							}
//							int index = Gui.World.GetIndex(x, y);
//							nextState.Elevation[index] += distT * DeltaPerSecond * dt;
//						}
//					}
//				}

//				Gui.World.CurStateIndex = nextStateIndex;

//			}
//		}
//	}
//	override public void OnMouseDown(Vector2Int p)
//	{
//		Active = true;
//	}
//	override public void OnMouseUp(Vector2Int p)
//	{
//		Active = false;
//	}
//	override public void OnMouseWheel(float delta)
//	{
//		BrushSize = Mathf.Clamp(BrushSize + delta / 100, 0, 50);
//	}
//}

