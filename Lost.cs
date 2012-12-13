using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace Exodus {
	enum GameMode {
		WAVE,
		CAMPAIGN
	}
	
	public class Lost : GameWindow {
		#region Members
		const int updateRate = 20;
		const double updatesPerSecond = 1.0 / (double) updateRate;
		
		GameState g;
		Gui gui;
		Stopwatch gameTime = new Stopwatch();
		long lastUpdate = 0;
		IController playerControl;
		
		XmlDocument Config;
		GameMode mode;
		
		#endregion
		
		public Lost(int x, int y) : base(x,y) {
			Config = Loader.GetConfig("game.xml");
			XmlNode gmode = Config.SelectSingleNode("/game/gamemode");
			if(gmode.InnerText == "wave") {
				mode = GameMode.WAVE;
			}
		}
		
		protected override void OnLoad(System.EventArgs e) {
			Graphics.InitGL();
			gui = new Gui();
			gameTime.Start();
			playerControl = new InputController(Keyboard);
			Player player = new Player(Vector2d.Zero, 0, playerControl);
			g = new GameState(player);
			
			Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(OnKeydown);
			
			if(mode == GameMode.WAVE) {
				WaveLevel l = new WaveLevel();
				g.AddLevel(l);
				g.SetLevel(0);
				g.AddObj(new Gate(Vector2d.Zero, 0));
			} else {
				Misc.BuildLevelTree(g);
				g.SetLevel(0);
				ILevel lev = g.GetLevel();
				//lev.Rocks = 100;
				//lev.Danger = 100;
				//lev.Weirdness = 100;
				//g.AddObj(player);
				for(int i = 0; i < 100; i++) {
					lev.Calc(g);
				}
			}
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
				gui.ToggleHelp();
				break;
			case OpenTK.Input.Key.Escape:
				this.Exit();
				break;
			default:
				break;
			}
		}
		
		protected override void OnUpdateFrame(FrameEventArgs e) {
			// XXX: Fix this to use the actual camera...
			if(Keyboard[OpenTK.Input.Key.A]) {
				Graphics.CameraDistance = Math.Max(20, Graphics.CameraDistance - 20);
			}
			if(Keyboard[OpenTK.Input.Key.S]) {
				Graphics.CameraDistance = Math.Min(Graphics.ClipFar - 20, Graphics.CameraDistance + 20);
			}
			
			if(g.IsGameOver()) {
				Console.WriteLine("Game over!");
				this.Exit();
				return;
			}
			
			foreach(IGameObj o in g.Objs) {
				o.Calc(g);
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
			
			Graphics.StartDraw(g.Camera.Target, g.GetLevel().Background);
			
			foreach(IGameObj o in g.Objs) {
				o.Draw(frameFraction);
			}
			foreach(Particle p in g.Particles) {
				p.Draw(frameFraction);
			}
			
			gui.Draw(g);
			
			SwapBuffers();
		}
		
		[STAThread]
		public static void Main() {
			Loader.Init();
			XmlDocument config = Loader.GetConfig("game.xml");
			XmlNode res = config.SelectSingleNode("/game/resolution");
			int width = Int32.Parse(res.Attributes["w"].Value);
			int height = Int32.Parse(res.Attributes["h"].Value);
			
			using (Lost g = new Lost(width, height)) {
				Console.WriteLine("Game inited");
				// Updates per second = 20, frames per second = as fast as possible
                g.Run(updateRate);
            }
		}
	}
}
