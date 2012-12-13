using System;
using OpenTK;

namespace Exodus {
	// Camera modes... pan, follow object, hold on position... anything else?
	// Zoom might be nice.  Then pan-zoom...  Then we could write pan and zoom as special
	// cases of pan-zoom.
	// I'm pretty sure that zoom isn't affected by this class at all yet.
	public enum CameraMode {
		PAN,
		FOLLOW,
		HOLD
	}
	
	// XXX: we can easily solve the scroll-off-the-edges-of-the-map problem by adding a bound here.
	// But we need to know how big the map is.
	public class Camera {
		public Vector3d Up {get;set;}
		public Vector2d Target {get;set;}
		public double Dist { get; set; }
		
		public CameraMode Mode {get;set;}
		
		Vector2d PanTarget = Vector2d.Zero;
		double PanTime = 0;
		
		IGameObj FollowTarget = null;
		
		const double DefaultDist = 250.0;
		static Vector2d DefaultTarget = Vector2d.Zero;
		static Vector3d DefaultUp = Graphics.Up;
		public Camera(double dist, Vector2d target) {
			Mode = CameraMode.HOLD;
			Dist = dist;
			Target = target;
		}
		public Camera() : this(DefaultDist, DefaultTarget) {
			Up = DefaultUp;
		}
		
		public void Calc(int dt) {
			switch(Mode) {
			case CameraMode.PAN:
				Pan(dt);
				break;
			
			case CameraMode.FOLLOW:
				Follow(FollowTarget, dt);
				break;
			
			case CameraMode.HOLD:
				//Console.WriteLine("Holding at: {0}, {1}", Target.X, Target.Y);
				// Do nothing!  :D
				break;
			}
		}
		
		// So the camera needs to have shortcut functions to make it Do Things,
		// otherwise there's no point to having a camera class at all; we just keep doing
		// what we're doing having it independant.
		// But unlike most other things in the game, it updates on graphics time, not game time.
		// Unlike the GameState, which updates on game time.
		// But the GameState is the obvious place for it, especially since it needs to be accessible
		// from other areas, so we can't just put it in the Lost class...
		// Well, calling GameState functions from the Lost class is REAL EASY, so that might be the place
		// Yeah, we'll put it in Gamestate, that way things can reach it, and make it update in the OnRender
		// method in the Lost class.
		public void SetPan(Vector2d target, double time) {
			Mode = CameraMode.PAN;
			PanTarget = target;
			PanTime = time;
		}
		void Pan(int dt) {
			if(PanTime > 0) {
				double timePassed = (double)dt / 1000;
				double proportionOfPanDone = timePassed / PanTime;
				Vector2d panPath = Vector2d.Subtract(PanTarget, Target);
				Vector2d panIncrement = Vector2d.Multiply(panPath, proportionOfPanDone);
				Target = Vector2d.Add(Target, panIncrement);
				PanTime -= timePassed;
			} else {
				SetHold(Target);
			}
		}
		
		public void SetFollow(IGameObj o) {
			Mode = CameraMode.FOLLOW;
			FollowTarget = o;
		}
		
		// We do the interpolation here~
		// dt is the time since the last frame, in milliseconds.
		void Follow(IGameObj o, int dt) {
			// If the target suddenly vanishes, we stay where we are...
			// Though it should never vanish as long as we have a reference to it...  Hmm.
			if(FollowTarget == null) {
				SetHold(Target);
			} else {
				double frameFraction = Math.Min(1.0, (double)dt / 50.0);
				Target = Misc.LerpVector2d(o.OldLoc, o.Loc, frameFraction);
			}
		}
		
		public void SetHold(Vector2d target) {
			Mode = CameraMode.HOLD;
			Target = target;
		}
	}
}

