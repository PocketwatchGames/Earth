using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public class ToolSpawn : Tool
{
	int _speciesIndex = 0;
	override public void OnSelect() { }
	override public void OnDeselect() { }
	//override public void DrawWorld(SpriteBatch spriteBatch, World.State state) {
	//	DrawSelection(spriteBatch, Gui, state);
	//}
	//override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
	//{
	//	Tool.DrawInfoTooltip(spriteBatch, Gui, state);
	//}
	override public void Update(float dt, Vector2Int p) {
	}
	override public void OnMouseDown(Vector2Int p) {
		lock (Gui.World.InputLock)
		{
			int nextStateIndex = Gui.World.AdvanceState();

			var tileIndex = Gui.World.GetIndex(p.x, p.y);
			int animalTileIndex = tileIndex * World.MaxGroupsPerTile;
			var state = Gui.World.States[Gui.World.CurStateIndex];
			var nextState = Gui.World.States[nextStateIndex];
			for (int i = 0; i < Gui.World.MaxAnimals; i++)
			{
				if (state.Animals[i].Population == 0)
				{
					for (int j = 0; j < World.MaxGroupsPerTile; j++)
					{
						if (state.AnimalsPerTile[animalTileIndex + j] == -1)
						{
							nextState.AnimalsPerTile[animalTileIndex + j] = i;
							nextState.Animals[i] = new AnimalGroup() { Species = _speciesIndex, Population = 100, Position = new Vector2(p.x + 0.5f, p.y + 0.5f), Destination = new Vector2(p.x + 0.5f, p.y + 0.5f) };
							nextState.Animals[i].Destination = state.Animals[i].Position;
							goto FoundIt;
						}
					}
				}
			}
FoundIt:
			Gui.World.CurStateIndex = nextStateIndex;
		}

	}
	override public void OnMouseUp(Vector2Int p) {
	}
	override public void OnMouseWheel(float delta) { }

}