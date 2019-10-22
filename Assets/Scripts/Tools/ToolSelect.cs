using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

//public class ToolSelect : Tool
//{
//	bool _selecting;
//	Vector2Int _start;
//	Vector2Int _end;

//	override public void OnSelect() { }
//	override public void OnDeselect() { }
//	//override public void DrawWorld(SpriteBatch spriteBatch, World.State state) {
//	//	DrawSelection(spriteBatch, Gui, state);
//	//}
//	//override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
//	//{
//	//	Tool.DrawInfoTooltip(spriteBatch, Gui, state);
//	//}
//	override public void Update(float dt, Vector2Int p) {
//		if (_selecting)
//		{
//			_end = p;
//			UpdateSelection();
//		}
//	}
//	override public void OnMouseDown(Vector2Int p) {
//		_start = p;
//		_end = p;
//		_selecting = true;
//		Gui.AnimalsSelected.Clear();
//	}
//	override public void OnMouseUp(Vector2Int p) {
//		UpdateSelection();
//		_selecting = false;
//	}
//	override public void OnMouseWheel(float delta) { }

//	void UpdateSelection()
//	{
//		Gui.AnimalsSelected.Clear();
//		Rect marquee = new Rect(Math.Min(_end.x, _start.x), Math.Min(_end.y, _start.y), 0, 0);
//		marquee.width = Math.Max(_end.x, _start.x) - marquee.x;
//		marquee.height = Math.Max(_end.y, _start.y) - marquee.y;
//		var state = Gui.World.States[Gui.World.CurStateIndex];
//		for (int i = 0; i < Gui.World.MaxAnimals; i++)
//		{
//			if (state.Animals[i].Population > 0 && marquee.Contains(state.Animals[i].Position))
//			{
//				Gui.AnimalsSelected.Add(i);
//			}
//		}
//	}
//}

