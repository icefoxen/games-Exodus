using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;


namespace Exodus {	
	public enum Direction {
		Left,
		Right
	}
	
	public interface IGameObj {
		// Attributes
		long Id { get; } // An ID is guaranteed to be unique and not change over the object's lifetime.
		
		Vector2d OldLoc {get; set;}
		Vector2d Loc { get; set; }
		Direction Facing {get; set;}
		Vector2d Vel {get; set;}
		double DragAmount {get;set;}
		double Mass { get; set; }
		
		// This is how close something needs to be before the object cares about it
		// Used both for collision and AI.
		double EffectRange { get; set; }
		int Hits { get; set; }
		int MaxHits { get; set; }
		
		// True if bullets interact with this object.
		// False for various objects, like, many bullets.
		bool Shootable { get; set; }
		// True if colliding with something bounces both parties apart instead of
		// having them pass through each other.
		bool Impactable { get; set; }
		// True if unaffected by gravity.
		bool Floating { get; set; }
		bool TouchingGround {get;set;}
		
		// Operations
		void Push(Vector2d force);
		void Drag(double d);
		void ClampVel(double d);
		
		// Collision
		BBCollider Collider { get; set; }
		// Hmm, just how do we do this...
		bool CollidingWithLevel(ILevel l);
		
		bool Colliding(IGameObj o);
		void OnCollide(IGameObj o, GameState g);
		void Calc(GameState g);
		void Draw(double dt);
		void Die(GameState g);
		void Damage(int i);
		void Heal(int i);
		
	}
	
	// Objects that implement IControllable can be run by Controllers... Input module, AI, etc. 
	public interface IControllable : IGameObj {
		double MaxVel { get; set; }
		IGameObj Target { get; set; }
		// Each weapon has an Attack, LowAttack and HighAttack...
		// KISS for now.
		IWeapon[] Weapons { get; set; }
		int CurrentWeapon {get;set;}
		void WalkLeft();
		void WalkRight();
		void Jump();
		void Duck();
		void Defend();
	}
	
	public class BaseObj : IGameObj {
		public double EffectRange { get; set; }
		public Vector2d OldLoc {get;set;}
		public Vector2d Loc {get;set;}
		
		public Vector2d Vel {get;set;}
		public double DragAmount { get; set; }
		
		public Direction Facing {get; set;}
		public double Mass {get; set;}
		
		public BBCollider Collider{get; set;}
		
		public bool Alive{get; set;}
		public int Hits { get; set; }
		public int MaxHits { get; set; }
		// Vulnerable means it takes damage.  Not vulnerable means it does not.
		public bool Vulnerable {get;set;}
		
		public bool Shootable { get; set; }
		public bool Impactable { get; set; }
		public bool Floating { get; set; }
		// True if it is on the ground and, ie, can jump.
		public bool TouchingGround {get;set;}
		
		protected Sprite Sprite;
		
		// Technically we should not need (nor probably have) a setter, but
		// C# is pesky.
		public long Id { get; set; }
		
		public BaseObj(Vector2d loc, Direction facing) {
			Loc = loc;
			OldLoc = loc;
			Facing = facing;
			Id = Misc.GetID();
			Collider = new BBCollider(loc, 5, 5);
			
			Sprite = Loader.GetSprite("player.sprite");
			Mass = 100.0;
			Hits = 100;
			DragAmount = 0;
			MaxHits = Hits;
			Vulnerable = true;
			
			Shootable = true;
			Impactable = true;
			Floating = false;
			TouchingGround = false;
			EffectRange = 10;
		}
		
		// Operations
		public virtual void Push(Vector2d force) {
			Vector2d force2;
			force2 = Vector2d.Divide(force, Mass);
			Vel = Vector2d.Add(Vel, force2);
		}
		public virtual void Drag() {
			Drag(DragAmount);
		}
		public virtual void Drag(double d) {
			Vel = Vector2d.Multiply(Vel, d);
		}
		
		public virtual void ClampVel(double d) {
			if(Vel.LengthSquared > (d * d)) {
				Vel = Vector2d.NormalizeFast(Vel);
				Vel = Vector2d.Multiply(Vel, d);
			}
		}
		
		// Collision
		// This one checks whether we are touching level terrain
		// XXX: It also sucks, only testing a single point, and knowing nothing about direction
		// or the possibilities of a partial list of blocks, or anything.  Hmmmmm.
		// To improve this we... basically have to check ALL the blocks that the gameobj
		// MIGHT be over, making a real bounding box.  That's easy, actually.
		// Then for handling, we need to find out how much they intersect, and backtrack
		// along the player's velocity until they no longer intersect.
		// Handling walk-through issues I think will be handled simply and elegantly by not
		// making anything move that fast.
		public virtual bool CollidingWithLevel(ILevel l) {
			int tileX = (int) Math.Floor(Loc.X);
			int tileY = (int) Math.Floor(Loc.Y);
			BlockType tileTouching = l.CollideMap[tileX,tileY];
			//foreach(var i in CollidingWithTiles(l)) {
			//	Console.WriteLine("Colliding with tile {0}, {1}", i.Key, i.Value);
			//}
			
			if(tileTouching != BlockType.Passable) {
				return true;
			}
			//Console.WriteLine("Touching tile at {0},{1}: {2}", tileX, tileY,  tileTouching);
			return false;
		}
		
		// We assume tiles use the same coordinate system as gameobjs.
		// It'd be pretty silly if they didn't.  Though note that this is the
		// collision map, not the (parallax-affected) background.
		public virtual List<KeyValuePair<int, int>> CollidingWithTiles(ILevel l) {
			var tileLeftBound = (int)Math.Floor(Collider.Location.X);
			var tileRightBound = (int)Math.Ceiling(Collider.Location.X + Collider.Dimensions.X);
			var tileTopBound = (int)Math.Ceiling(Collider.Location.Y + Collider.Dimensions.Y);
			var tileBottomBound = (int)Math.Floor(Collider.Location.Y);
			
			
			// We're using Mono 2.6, which apparently doesn't implement .NET 4.0 yet, so tuples are right out.
			// Sigh.
			var list = new List<KeyValuePair<int, int>>();
			for(int i = tileLeftBound; i < tileRightBound; i++) {
				for(int j = tileBottomBound; j < tileTopBound; j++) {
					BlockType tileTouching = l.CollideMap[i,j];
					if(tileTouching != BlockType.Passable) {
						list.Add(new KeyValuePair<int, int>(i, j));
					}
				}
			}
			
			return list;
		}
		
		public virtual void CollideWithTile(ILevel l, int x, int y) {
			
		}
			
		// This one checks whether things are actually intersecting.
		public virtual bool Colliding(IGameObj p) {
			return Collider.Colliding(p.Collider);
		}
		
		// And this gets overridden to handle anything that needs
		// to happen on collision.  The actual collision physics
		// is handled in Misc.DoCollision.
		public virtual void OnCollide(IGameObj p, GameState g) {
			
		}
		
		protected virtual void CalcPhysics(GameState g) {
			if(!TouchingGround && !Floating) {
				Vel = Vector2d.Add(Vel, Misc.GRAVITY);
				//Console.WriteLine("Foo: {0}, {1}", TouchingGround, Floating);
			}
			Loc = Vector2d.Add(Loc, Vel);
			ClampVel(Misc.PHYSICSMAXSPEED);
			//g.GetLevel().Boundary(this);
			//Console.WriteLine("Loc: {0}, Vel: {1}, Facing: {2}, RVel: {3}",
			//                  Loc, Vel, Facing, RVel);
		}

		protected virtual void CalcCollision(GameState g, ICollection<IGameObj> nearbyObjects) {
			foreach(IGameObj go in nearbyObjects) {
				// The ID check conveniently prevents things from colliding twice or
				// colliding with themselves.
				if(Id > go.Id && Colliding(go)) {
					if(Impactable && go.Impactable) {
						Misc.DoCollision(this, go);
					}
					this.OnCollide(go, g);
					go.OnCollide(this, g);
				}
			}
		}
		
		protected void StopOnBoundingBox(double x, double y, double w, double h) {
			if(Vel.X < 0) {
				if(Collider.Location.X < x+w) {
					// Then we hit the box travelling left
					Loc = new Vector2d(x+w, Loc.Y);
					Vel = new Vector2d(0, Vel.Y);
				}
			} else if(Vel.X > 0) {
				if(Collider.Location.X + Collider.Dimensions.X > x) {
					// Then we hit the box travelling right
					Loc = new Vector2d(x - Collider.Dimensions.X, Loc.Y);
					Vel = new Vector2d(0, Vel.Y);
				}
			}
			
			if(Vel.Y < 0) {
				if(Collider.Location.Y < y+h) {
					// Then we hit the box travelling down
					Loc = new Vector2d(Loc.X, y+h);
					Vel = new Vector2d(Vel.X, 0);
					TouchingGround = true;
				} else {
					Console.WriteLine("Not touching ground!");
					TouchingGround = false;
				}
			} else if(Vel.Y > 0) {
				if(Collider.Location.Y + Collider.Dimensions.Y > y) {
					// Then we hit the box travelling up
					Loc = new Vector2d(Loc.X, y - Collider.Dimensions.Y);
					Vel = new Vector2d(Vel.X, 0);
				}
			}
		}
		public void CalcLevelCollision(ILevel l) {
			var tileLeftBound = (int)Math.Floor(Collider.Location.X);
			var tileRightBound = (int)Math.Ceiling(Collider.Location.X + Collider.Dimensions.X);
			var tileTopBound = (int)Math.Ceiling(Collider.Location.Y + Collider.Dimensions.Y);
			var tileBottomBound = (int)Math.Floor(Collider.Location.Y);
			
			
			for(int i = tileLeftBound; i < tileRightBound; i++) {
				for(int j = tileBottomBound; j < tileTopBound; j++) {
					var tileTouching = l.CollideMap[i,j];
					if(tileTouching != BlockType.Passable) {
						var tile = l.CollideMap[i, j];
						switch(tile) {
						case BlockType.Passable:
							break;
						case BlockType.Impassable:
							StopOnBoundingBox(tileLeftBound, tileBottomBound, 1, 1);
							break;
						case BlockType.Jumpthroughable:
							break;
						}
					}
				}
			}
		}

		// The ordering here MIGHT be a problem someday, since CalcCollision() updates the
		// object's locations and such in more or less interleaved order.
		// A better way MIGHT be to do all the physics and calc, THEN do all the collisions...
		// but in practice I cannot see any difference at all.
		// Apparently the Real Physics Engine way to do it is to collect collision events, then
		// resolve them all at once.
		public virtual void Calc(GameState g) {
			OldLoc = new Vector2d(Loc.X, Loc.Y);
			CalcLevelCollision(g.Levels[g.CurrentLevel]);
			var nearbyObjects = g.GetObjectsWithin(Loc, EffectRange);
			CalcCollision(g, nearbyObjects);
			//bool touchingLevel = CollidingWithLevel(g.Levels[g.CurrentLevel]);
			
			CalcPhysics(g);
			Collider.Location = Loc;
			// XXX: How the FUCK do we see whether you're standing on top of a block versus, say,
			// pushing into it from the side?
			// It has to be for 
			// MAYBE the best option is to just do conventional
			// collision detection and response for EVERYTHING.  Ponder this.
			// This is mostly impossible because it involves storing one rect per tile in the level.
			// However, we can create the half-dozen or so rects that the gameobj is intersecting with
			// each loop, and collide against those.  Hmmm.
			// It MIGHT actually become the case that it is simplest to make a Real Physics Engine.
			// XXX: Remember any changes here might have to happen in subclasses too.
			/*
			if(touchingLevel) {
				TouchingGround = true;
				Vel = new Vector2d(Vel.X, 0);
			}
			*/
			
			if(Hits > 0) {
			} else {
				//Console.WriteLine("Something dead!");
				g.KillObj(this);
			}
		}
		
		// Just for reference, dt is in units where 1 = 1 physics tick
		public virtual void Draw(double dt) {
			Vector2d lerploc = Misc.LerpVector2d(OldLoc, Loc, dt);
			Vector3d v = new Vector3d(lerploc);
			Sprite.Draw(v);
		}
		public virtual void Die(GameState g) {
			//Console.WriteLine("Dying");
		}
		
		public virtual void Damage(int i) {
			if(Vulnerable) {
				Hits -= i;
			}
		}
		public virtual void Heal(int i) {
			Hits = Math.Min(Hits + i, MaxHits);
		}
	}
	
	// A character who wanders around, is controllable, etc.
	// As opposed to, say, a bullet, which is also a GameObj.
	public class BaseActor : BaseObj, IControllable {
		public IController Control;
		public double MaxVel { get; set; }
		public IGameObj Target { get; set; }

		public IWeapon[] Weapons {get;set;}
		public int CurrentWeapon { get; set; }
		
		public BaseActor(Vector2d loc, Direction facing) : this(loc, facing, new DummyController()) {
		}
		
		public BaseActor(Vector2d loc, Direction facing, IController ai) : base(loc, facing) {
			Collider = new BBCollider(2, 3);
			Control = ai;
			Control.Controlled = this;
			Target = null;
			
			Mass = 50.0;
			Hits = MaxHits = 100;
			Vulnerable = true;
			MaxVel = 10;
			
			Hits = MaxHits = 100;
			
			//Weapons[0] = new BaseWeapon(0, 2, weaponpoints, ShotMode.CYCLE);
		}
		// XXX: Thrust, Turn and Brake should just set flags that are checked in Calc
		// That way we can never Thrust or Turn more than once!
		// We might also want to make it possible for them to range from 0 to 1, for the
		// sake of steering being easier...
		// These ponderments are still valid.
		// We want to add some drag to the normal calc when on the ground.
		public virtual void Jump() {
			//force = Vector2d.Multiply(Misc.Vector2dFromDirection(Facing), ThrustForce);
			if(TouchingGround) {
				TouchingGround = false;
				Push(new Vector2d(0, 10));
				//Console.WriteLine("Jumping!  {0}", Vel);
			} else {
				//Console.WriteLine("Can't jump in the air!");
			}
		}
		public virtual void WalkLeft() {
			Facing = Direction.Left;
			Push(new Vector2d(-1, 0));
			ClampVel(MaxVel);
		}
		public virtual void WalkRight() {
			Facing = Direction.Right;
			Push(new Vector2d(1, 0));
			ClampVel(MaxVel);
		}
		public virtual void Duck() {
			Console.WriteLine("Ducking!");
		}
		
		public virtual void Defend() {
			Console.WriteLine("Defending!");
		}
		
		public override void Calc(GameState g) {
			OldLoc = new Vector2d(Loc.X, Loc.Y);
			ICollection<IGameObj> nearbyObjects = g.GetObjectsWithin(Loc, EffectRange);
			CalcCollision(g, nearbyObjects);
			
			CalcPhysics(g);
			
			bool touchingLevel = CollidingWithLevel(g.Levels[g.CurrentLevel]);
			// XXX: How the FUCK do we see whether you're standing on top of a block versus, say,
			// pushing into it from the side?  MAYBE the best option is to just do conventional
			// collision detection and response for EVERYTHING.  Ponder this.
			// It MIGHT actually become the case that it is simplest to make a Real Physics Engine.
			if(touchingLevel) {
				TouchingGround = true;
				Vel = new Vector2d(Vel.X, 0);
			}
			
			Collider.Location = Loc;
			
			// All weapons refresh fire rate even when they're not selected, which I THINK is the right thing to do.
			// ...if Weapons is empty this calls the method with a null argument.  What the FLYING FUCK???
			foreach(IWeapon w in Weapons) {
				if(w != null) {
					w.Calc(g);
				}
			}
			Control.Calc(g, nearbyObjects);
			Sprite.Animate();
			//Console.WriteLine("Loc: {0}", Loc);
		}
	}
	
	#region Actors
	public class Player : BaseActor {
		public Player(Vector2d vec, Direction facing, IController c) : base(vec, facing, c) {
			Sprite = Loader.GetSprite("player.sprite");
			
			Mass = 20.0;
			Vulnerable = true;
			
			Hits = MaxHits = 100;
			
			Weapons = new BaseWeapon[3];
		}
		public override void Calc(GameState g) {
			base.Calc(g);
		}
		
		public override void OnCollide(IGameObj o, GameState g) {
			//Console.WriteLine("Player hit something...");
		}
	}
	
	#endregion
	
}
	