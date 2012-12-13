using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;

namespace Exodus {
	public class GameState {
		#region Members
		public HashSet<IGameObj> Objs {get;set;}
		public HashSet<IGameObj> DeadObjs {get;set;}
		public HashSet<IGameObj> NewObjs {get;set;}
		public List<ILevel> Levels {get;set;}
		public int CurrentLevel {get;set;}
		
		// Particles are kept in their own list, since they do not collide.
		public HashSet<Particle> Particles {get;set;}
		public HashSet<Particle> DeadParticles {get;set;}
		public HashSet<Particle> NewParticles { get; set; }
		
		private QuadTree<IGameObj> CollisionTree;
		
		public Player Player { get; set; }
		
		public ulong Frames {get;set;}
		public ulong GraphicsFrames {get;set;}
		public Stopwatch Realtime { get; set; }
		
		public Camera Camera {get;set;}
		#endregion
		
		public GameState(Player p) {
			Objs = new HashSet<IGameObj>();
			NewObjs = new HashSet<IGameObj>();
			DeadObjs = new HashSet<IGameObj>();
			Particles = new HashSet<Particle>();
			NewParticles = new HashSet<Particle>();
			DeadParticles = new HashSet<Particle>();
			Levels = new List<ILevel>();
			CollisionTree = null;
			
			Camera = new Camera();
			Camera.SetFollow(p);
			//Camera.SetHold(new Vector2d(0, 0));
			//Camera.SetPan(new Vector2d(100, 0), 5);
			
			CurrentLevel = 0;
			Frames = 0;
			Realtime = new Stopwatch();
			Realtime.Start();
			
			Player = p;
		}
		
		public bool IsGameOver() {
			return Player.Hits < 1;
		}
		
		public void NextGraphicFrame(int dt) {
			GraphicsFrames += 1;
			Camera.Calc(dt);
		}
		
		private void UpdateObjects() {
			foreach(IGameObj o in DeadObjs) {
				o.Die(this);
				Objs.Remove(o);
			}
			DeadObjs.Clear();
		
			foreach(IGameObj o in NewObjs) {
				Objs.Add(o);
			}
			NewObjs.Clear();
		}
		
		private void UpdateParticles() {
			foreach(Particle o in DeadParticles) {
				o.Die(this);
				Particles.Remove(o);
			}
			DeadParticles.Clear();
		
			foreach(Particle o in NewParticles) {
				Particles.Add(o);
			}
			NewParticles.Clear();
		}
		
		// If we wanted to be groovy, we could only update this every other frame or so,
		// since objects won't have moved much in the mean time and most objects will probably have
	    // some slop in their EffectRange.  :-P
		// Possible optimization, anyway.
		private void UpdateCollisionTree() {
			// XXX: This might want to be a const so it's not re-created every frame...
			Func<IGameObj, Vector2d> f = delegate(IGameObj o) { return o.Loc; };
			
			CollisionTree = QuadTree<IGameObj>.Build(f, Objs);
		}
		
		public ICollection<IGameObj> GetObjectsWithin(Vector2d point, double distance) {
			return CollisionTree.GetWithin(point, distance);
		}
		
		public void NextFrame() {
			UpdateObjects();
			UpdateParticles();
			UpdateCollisionTree();
			
			
			Frames += 1;
			if(Frames % 99 == 0) {
				Console.WriteLine("Frame: {0} Gameobjs: {1} Particles: {2} ", 
				                  Frames, Objs.Count, Particles.Count);
				Console.WriteLine("Real time elapsed: {0} Average update FPS: {1} Average graphical FPS: {2}", 
				                  Realtime.Elapsed, 
				                  1000 * ((float)Frames / (float)Realtime.ElapsedMilliseconds),
				                  1000 * ((float)GraphicsFrames / (float)(Realtime.ElapsedMilliseconds)));
				
				Console.WriteLine("Memory used: {0} bytes", GC.GetTotalMemory(false));
				
				Levels[CurrentLevel].Calc(this);

			}
		}
		
		public void AddParticle(Particle p) {
			NewParticles.Add(p);
		}
		
		public void KillParticle(Particle p) {
			DeadParticles.Add(p);
		}
		
		public void AddObj(IGameObj o) {
			NewObjs.Add(o);
		}
		
		public void AddObjs(IEnumerable<IGameObj> o) {
			foreach(IGameObj go in o) {
				NewObjs.Add(go);
			}
		}
		public void KillObj(IGameObj o) {
			DeadObjs.Add(o);
		}
		public void AddLevel(ILevel l) {
			Levels.Add(l);
		}
		
		public ILevel GetLevel() {
			return Levels[CurrentLevel];
		}
		
		// Crap, when must this happen?
		// It can happen whenever Objs is the current state... which is thoretically always.
		// This will nuke the particles though.
		// This is actually pretty close to correct.
		public void SetLevel(int level) {
			UpdateObjects();
			Objs.Remove(Player);
			Levels[CurrentLevel].Objs = Objs;
			// This is PROBABLY right, but I'm not sure.
			CurrentLevel = level;
			Objs = Levels[CurrentLevel].Objs;
			Objs.Add(Player);
			NewObjs.Clear();
			DeadObjs.Clear();
			Particles.Clear();
			NewParticles.Clear();
			DeadParticles.Clear();
			UpdateCollisionTree();
		}
		
	}
}
