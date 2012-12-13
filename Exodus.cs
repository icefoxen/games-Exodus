using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


using Newtonsoft.Json.Linq;

namespace Exodus {
	public class Exodus : GameWindow {
		#region Members
		const int updateRate = 20;
		const double updatesPerSecond = 1.0 / (double) updateRate;
		const string title = "Exodus";
		
		GameState g;
		//Gui gui;
		Stopwatch gameTime = new Stopwatch();
		long lastUpdate = 0;
		IController playerControl;
		//TileMap t;
		
		#endregion
		
		public Exodus(int x, int y) : base(x, y, new GraphicsMode(), title, GameWindowFlags.Default) {
		}
		
		protected override void OnLoad(System.EventArgs e) {
			Graphics.InitGL();
			//gui = new Gui();
			gameTime.Start();
			playerControl = new InputController(Keyboard);
			Player player = new Player(new Vector2d(450, 100), 0, playerControl);
			g = new GameState(player);
			
			Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(OnKeydown);

			//Level l = new Level(3);
			Level l = Loader.LoadLevel("test.level");
			g.AddLevel(l);
			g.SetLevel(0);
		}
		
		protected override void OnUnload(EventArgs e) {
		}
		
		protected override void OnResize(EventArgs e) {
			base.OnResize(e);
			Graphics.Resize();
		}
		
		protected void OnKeydown(object sender, OpenTK.Input.KeyboardKeyEventArgs e) {
			switch(e.Key) {
			case OpenTK.Input.Key.F1:
				//gui.ToggleHelp();
				break;
			case OpenTK.Input.Key.Escape:
				this.Exit();
				break;
			default:
				break;
			}
		}
		
		protected override void OnUpdateFrame(FrameEventArgs e) {
			if(g.IsGameOver()) {
				Console.WriteLine("Game over!");
				this.Exit();
				return;
			}
			
			foreach(IGameObj o in g.Objs) {
				o.Calc(g);
			}
			
			foreach(IGameObj o in g.Objs) {
				o.CollidingWithLevel(g.Levels[g.CurrentLevel]);
			}
			
			foreach(Particle p in g.Particles) {
				p.Calc(g);
			}
			
			g.NextFrame();
			
			lastUpdate = gameTime.ElapsedMilliseconds;
		}
		
		protected override void OnRenderFrame(FrameEventArgs e) {
			long now = gameTime.ElapsedMilliseconds;
			int dt = (int) (now - lastUpdate);
			double frameFraction = Math.Min(1.0, (double)dt / 50.0);
			g.NextGraphicFrame(dt);
			
			// Setup camera, centered on the target (instead of the target being at the bottom-
			// left).  There are some constraints to prevent the camera from scrolling off the
			// background; they make it stop just short of doing so.
			// XXX: These hardcoded values should be taken from the level map,
			// somehow...
			const int xCenterOffset = 16;
			const int yCenterOffset = 12;
			Vector2d CameraTarget = g.Camera.Target;
			CameraTarget.X = Misc.Clamp(CameraTarget.X, 
			                            xCenterOffset, g.GetLevel().Width - xCenterOffset - 1);
			CameraTarget.Y = Misc.Clamp(CameraTarget.Y, 
			                            yCenterOffset, g.GetLevel().Height - yCenterOffset - 1);
			Graphics.StartDraw(CameraTarget);
			
			// Draw background
			Vector2d MapOffset = new Vector2d(-xCenterOffset, -yCenterOffset);
			Vector2d MapPoint = Vector2d.Add(CameraTarget, MapOffset);
			//Console.WriteLine("MapPoint: {0}, {1}", MapPoint.X, MapPoint.Y);
			g.Levels[g.CurrentLevel].Draw(MapPoint);
			
			// Draw actual objects
			foreach(IGameObj o in g.Objs) {
				o.Draw(frameFraction);
			}
			
			
			foreach(Particle p in g.Particles) {
				p.Draw(frameFraction);
			}
			
			//gui.Draw(g);
			
			SwapBuffers();
		}
		
		[STAThread]
		public static void Main() {
			Loader.Init();
			var config = Loader.GetConfig("game.cfg");
			var w = config["resolution"]["width"];
			var h = config["resolution"]["height"];
			var width = int.Parse(w.ToString());
			var height = int.Parse(h.ToString());
			
			using (Exodus g = new Exodus(width, height)) {
				g.VSync = VSyncMode.Adaptive;
				Console.WriteLine("Game inited");
				// Updates per second = 20, frames per second = as fast as possible
                g.Run(updateRate);
            }
		}
	}
}

