using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;

public class WorldGUI
{
	//KeyboardState _lastKeyboardState;
	//MouseState _lastMouseState;
	List<float> _timeScales = new List<float> { 0, 5, 20, 200 };
	int _timeScaleIndex = 0;
	Vector2Int viewport;

	//RenderTarget2D _worldRenderTarget;

	//public List<Tuple<World.Layers, Keys>> LayerDisplayKeys = new List<Tuple<World.Layers, Keys>>()
	//{
	//	new Tuple<World.Layers, Keys>(World.Layers.Water, Keys.F1),
	//	new Tuple<World.Layers, Keys>(World.Layers.Elevation, Keys.F2),
	//	new Tuple<World.Layers, Keys>(World.Layers.GroundWater, Keys.F3),
	//	new Tuple<World.Layers, Keys>(World.Layers.CloudCoverage, Keys.F4),
	//	new Tuple<World.Layers, Keys>(World.Layers.Temperature, Keys.F5),
	//	new Tuple<World.Layers, Keys>(World.Layers.Pressure, Keys.F6),
	//	new Tuple<World.Layers, Keys>(World.Layers.WaterVapor, Keys.F7),
	//	new Tuple<World.Layers, Keys>(World.Layers.RelativeHumidity, Keys.F8),
	//	new Tuple<World.Layers, Keys>(World.Layers.Rainfall, Keys.F9),
	//	new Tuple<World.Layers, Keys>(World.Layers.Wind, Keys.F10),
	//	new Tuple<World.Layers, Keys>(World.Layers.Plates, Keys.F11),
	//};


	public int ActiveTool = -1;

	public List<Tool> Tools = new List<Tool>();
	public Vector2Int TileInfoPoint;
	public List<int> AnimalsSelected = new List<int>();

	public World World;


	public WorldGUI(World world)
	{
		World = world;
		world.TimeScale = _timeScales[_timeScaleIndex];

		//Tools.Add(new ToolSelect() { Gui = this, Name = "Info", HotKey = Keys.Tab });
		//Tools.Add(new ToolProbe() { Gui = this, Name = "Probe 1", HotKey = Keys.D1, ProbeIndex = 0 });
		//Tools.Add(new ToolProbe() { Gui = this, Name = "Probe 2", HotKey = Keys.D2, ProbeIndex = 1 });
		//Tools.Add(new ToolProbe() { Gui = this, Name = "Probe 3", HotKey = Keys.D3, ProbeIndex = 2 });
		//Tools.Add(new ToolTectonic() { Gui = this, Name = "Plate Tectonics", HotKey = Keys.LeftShift });
		//Tools.Add(new ToolSpawn() { Gui = this, Name = "Spawn", HotKey = Keys.Enter });
		//Tools.Add(new ToolMove() { Gui = this, Name = "Move", HotKey = Keys.M });
		//Tools.Add(new ToolElevation() { Gui = this, Name = "Elevation Up", HotKey = Keys.OemPeriod, DeltaPerSecond = 1000.0f });
		//Tools.Add(new ToolElevation() { Gui = this, Name = "Elevation Down", HotKey = Keys.OemComma, DeltaPerSecond = -1000.0f });
		//SelectTool(0);

	}

	//public void LoadContent(GraphicsDevice graphics, ContentManager content)
	//{
	//	viewport = new Vector2Int(graphics.Viewport.Width, graphics.Viewport.Height);

	//	Font = content.Load<SpriteFont>("fonts/infofont");
	//	whiteTex = new Texture2D(graphics, 1, 1);
	//	Color[] c = new Color[] { Color.white };
	//	whiteTex.SetData(c);

	//	_worldRenderTarget = new RenderTarget2D(graphics, 1000, 1000);
	//}

	//bool WasKeyJustPressed(Keys k, ref KeyboardState state, ref KeyboardState lastState)
	//{
	//	return state.IsKeyDown(k) && !lastState.IsKeyDown(k);
	//}
	//bool IsKeyPressed(Keys k, ref KeyboardState state)
	//{
	//	return state.IsKeyDown(k);
	//}

	//Vector2Int ScreenToWorld(Vector2Int screenPoint)
	//{
	//	return new Vector2Int((int)Mathf.Clamp((screenPoint.X - viewport.X / 2) / (Zoom * World.tileRenderSize) + CameraPos.X, 0, World.Size - 1), (int)MathHelper.Clamp((screenPoint.Y - viewport.Y / 2) / (Zoom * World.tileRenderSize) + CameraPos.Y, 0, World.Size - 1));
	//}
	//public void Update(float dt)
	//{
	//	var keyboardState = Keyboard.GetState();
	//	var mouseState = Mouse.GetState();
	//	TileInfoPoint = ScreenToWorld(mouseState.Position);

	//	foreach (var k in LayerDisplayKeys)
	//	{
	//		if (WasKeyJustPressed(k.Item2, ref keyboardState, ref _lastKeyboardState))
	//		{
	//			ShowLayers ^= k.Item1;
	//		}
	//	}
	//	for (int i=0;i<Tools.Count;i++)
	//	{
	//		if (WasKeyJustPressed(Tools[i].HotKey, ref keyboardState, ref _lastKeyboardState))
	//		{
	//			SelectTool(i);
	//			break;
	//		}
	//	}
	//	if (WasKeyJustPressed(Keys.OemOpenBrackets, ref keyboardState, ref _lastKeyboardState))
	//	{

	//		_timeScaleIndex = _timeScaleIndex - 1;
	//		if (_timeScaleIndex < 0)
	//		{
	//			_timeScaleIndex = 0;
	//			World.TimeTillTick = 0;
	//		}
	//		World.TimeScale = _timeScales[_timeScaleIndex];
	//	}
	//	if (WasKeyJustPressed(Keys.OemCloseBrackets, ref keyboardState, ref _lastKeyboardState))
	//	{
	//		_timeScaleIndex = Mathf.Min(_timeScales.Count - 1, _timeScaleIndex + 1);
	//		World.TimeScale = _timeScales[_timeScaleIndex];
	//	}

	//	if (WasKeyJustPressed(Keys.Q, ref keyboardState, ref _lastKeyboardState))
	//	{
	//		ZoomLevel = Math.Max(0, ZoomLevel - 0.25f);
	//	}
	//	if (WasKeyJustPressed(Keys.E, ref keyboardState, ref _lastKeyboardState))
	//	{
	//		ZoomLevel = Math.Min(1, ZoomLevel + 0.25f);
	//	}
	//	Vector2 cameraMove = Vector2.Zero;
	//	if (IsKeyPressed(Keys.W, ref keyboardState))
	//	{
	//		cameraMove.y--;
	//	}
	//	if (IsKeyPressed(Keys.A, ref keyboardState))
	//	{
	//		cameraMove.x--;
	//	}
	//	if (IsKeyPressed(Keys.S, ref keyboardState))
	//	{
	//		cameraMove.y++;
	//	}
	//	if (IsKeyPressed(Keys.D, ref keyboardState))
	//	{
	//		cameraMove.x++;
	//	}
	//	float cameraSpeed = 100 / Zoom;
	//	CameraPos += cameraSpeed * cameraMove * dt;
	//	CameraPos.x = Mathf.Clamp(CameraPos.x, 0, World.Size);
	//	CameraPos.y = Mathf.Clamp(CameraPos.y, 0, World.Size);

	//	var curTool = GetTool(ActiveTool);
	//	if (curTool != null)
	//	{
	//		if (mouseState.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton != ButtonState.Pressed)
	//		{
	//			curTool.OnMouseDown(TileInfoPoint);
	//		} else if (mouseState.LeftButton != ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Pressed)
	//		{
	//			curTool.OnMouseUp(TileInfoPoint);
	//		}
	//		float wheelDelta = mouseState.ScrollWheelValue - _lastMouseState.ScrollWheelValue;
	//		if (wheelDelta != 0)
	//		{
	//			curTool.OnMouseWheel(wheelDelta);
	//		}
	//		curTool.Update(dt, TileInfoPoint);
	//	}

	//	_lastKeyboardState = keyboardState;
	//	_lastMouseState = mouseState;

	//	World.Update(dt);


	//	for (int i=0; i<AnimalsSelected.Count;i++)
	//	{
	//		if (World.States[World.CurStateIndex].Animals[AnimalsSelected[i]].Population == 0)
	//		{
	//			AnimalsSelected.RemoveAt(i--);
	//		}
	//	}
	//}

	Tool GetTool(int index)
	{
		return (ActiveTool >= 0 && ActiveTool < Tools.Count) ? Tools[ActiveTool] : null;
	}

	public void SelectTool(int t)
	{
		if (t != ActiveTool)
		{
			var curTool = GetTool(ActiveTool);
			curTool?.OnDeselect();

			ActiveTool = t;
			curTool = GetTool(ActiveTool);
			curTool?.OnSelect();
		}
	}

	//public void Draw(float dt, SpriteBatch spriteBatch, GraphicsDevice graphics )
	//{

	//	graphics.SetRenderTarget(_worldRenderTarget);
	//	World.Draw(dt, spriteBatch, ShowLayers, whiteTex);


	//	World.State state = World.States[World.CurRenderStateIndex];

	//	_tickTimer += gameTime.ElapsedGameTime.Ticks;
	//	if (_tickTimer >= TimeSpan.TicksPerSecond)
	//	{
	//		_tickTimer -= TimeSpan.TicksPerSecond;
	//		_ticksToDisplay = state.Ticks - _ticksLastSecond;
	//		_ticksLastSecond = state.Ticks;
	//	}

	//	var curTool = GetTool(ActiveTool);
	//	if (curTool != null)
	//	{
	//		curTool.DrawWorld(spriteBatch, state);
	//	}
	//	graphics.SetRenderTarget(null);

	//	spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
	//	Vector2 cameraPos = new Vector2(CameraPos.x * World.tileRenderSize, CameraPos.y * World.tileRenderSize);

	//	spriteBatch.Draw(_worldRenderTarget, new Rectangle((int)(viewport.x / 2 - cameraPos.x * Zoom), (int)(viewport.y / 2 - cameraPos.y * Zoom), (int)(Zoom * _worldRenderTarget.Width), (int)(Zoom * _worldRenderTarget.Height)), null, Color.white);

	//	spriteBatch.Draw(whiteTex, new Rectangle(0, 0, 150, 15 * LayerDisplayKeys.Count + 20), null, Color.black * 0.5f);

	//	int textY = 5;

	//	for (int i = 0; i < Tools.Count; i++)
	//	{
	//		spriteBatch.DrawString(Font, Tools[i].HotKey + " - [" + ((i == ActiveTool) ? "X" : " ") + "] " + Tools[i].Name, new Vector2(5, textY), Color.white);
	//		textY += 15;
	//	}
	//	textY += 20;

	//	spriteBatch.DrawString(Font, "[ x" + ((int)World.TimeScale) + " ] (actual = " + _ticksToDisplay + ")", new Vector2(5, textY), Color.white);
	//	textY += 20;

	//	foreach (var k in LayerDisplayKeys)
	//	{
	//		spriteBatch.DrawString(Font, "[" + (ShowLayers.HasFlag(k.Item1) ? "X" : " ") + "] - " + k.Item2 + " - " + k.Item1.ToString(), new Vector2(5, textY), Color.white);
	//		textY += 15;
	//	}

	//	spriteBatch.DrawString(Font, (int)(World.GetTimeOfYear(state.Ticks) * 12 + 1) + "/" + World.GetYear(state.Ticks), new Vector2(5, textY), Color.white);
	//	textY += 20;


	//	textY += 15;
	//	for (int s = 0; s < World.MaxSpecies; s++)
	//	{
	//		if (state.SpeciesStats[s].Population > 0)
	//		{
	//			var species = state.Species[s];
	//			spriteBatch.Draw(whiteTex, new Rectangle(5, textY, 10, 10), null, species.Color);
	//			spriteBatch.DrawString(Font, species.Name + " [" + species.Food.ToString().Substring(0, 1) + "] " + ((float)state.SpeciesStats[s].Population / 1000000).ToString("0.00"), new Vector2(20, textY), Color.White);
	//			textY += 15;
	//		}
	//	}
	//	textY += 15;

	//	if (curTool != null)
	//	{
	//		curTool.DrawTooltip(spriteBatch, state);
	//	}

	//	spriteBatch.End();

	//}
}
