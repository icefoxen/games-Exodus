using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;


namespace Exodus {
	public interface IController {
		// Needs to have a specified target...
		IControllable Controlled { get; set; }
		void Calc(GameState g, ICollection<IGameObj> nearbyObjects);
	}
	
	public class DummyController : IController {
		public IControllable Controlled { get; set; }
		
		public void Calc(GameState g, ICollection<IGameObj> nearbyObjects) {
		}
	}
	
	// XXX: TODO: Load controls from a config file.
	public class InputController : IController {
		public IControllable Controlled { get; set; }
		KeyboardDevice keyboard;
		Key walkLeft = Key.Left;
		Key walkRight = Key.Right;
		
		Key jump = Key.C;
		Key attack = Key.X;
		Key defend = Key.Z;
		Key duck = Key.Down;
		
		// This is a dirty dirty hack to avoid having to hook the
		// keyboard event system into this.
		int keydelay = 0;
		
		public InputController(KeyboardDevice k) : base() {
			keyboard = k;
		}
		public void Calc(GameState g, ICollection<IGameObj> nearbyObjects) {
			keydelay -= 1;
			
			if(keyboard[walkLeft])
				Controlled.WalkLeft();
			if(keyboard[walkRight])
				Controlled.WalkRight();
			if(keyboard[duck])
				Controlled.Duck();
			if(keyboard[jump])
				Controlled.Jump();
			if(keyboard[defend])
				Controlled.Defend();
			if(keyboard[attack]) {
				
			}
				
			/*
			if(keyboard[switchMain] && keydelay < 1) {
				Controlled.SwitchMain();
				keydelay = 10;
			}
			if(keyboard[switchSecondary] && keydelay < 1) {
				Controlled.SwitchSecondary();
				keydelay = 10;
			}
			if(keyboard[switchSpecial] && keydelay < 1) {
				Controlled.SwitchSpecial();
				keydelay = 10;
			}
			*/
			
		}
	}
	/*
	public class AIController : IController {
		public IControllable Controlled { get; set; }
		
		public AIController() {
		}

		// Need to be able to Seek, Arrive, Avoid, 
		public virtual void Calc(GameState g, ICollection<IGameObj> nearbyObjects) {
			Face(g.Player.Loc);
			Controlled.Fire(g);
		}
		
		// Attempts to turn the current velocity into the given velocity
		public virtual void Steer(Vector2d targetVel) {
			Vector2d deltaV = Vector2d.Subtract(targetVel, Controlled.Vel);
			
			Vector2d relativeDeltaV = Misc.Vector2dRotate(deltaV, -Controlled.Facing);
			double slop = 0.50;
			//Console.WriteLine("Relative dV: {0}", relativeDeltaV);
			
			if(relativeDeltaV.Y > slop) {
				Controlled.TurnLeft();
				// These checks improve handling a little bit at least
				if(relativeDeltaV.X < -slop) {
					Controlled.Brake();
				}
				
			} else if(relativeDeltaV.Y < -(slop)) {
				Controlled.TurnRight();
				if(relativeDeltaV.X < -slop) {
					Controlled.Brake();
				}
				
			} else { // We're on target
				if(relativeDeltaV.X > slop) {
					Controlled.Thrust();
				} else if(relativeDeltaV.X < -slop) {
					Controlled.Brake();
				}
			}
		}
		
		
		// This is a little ad-hoc; dot product gives 'towards' or 'away from'
		// and we rotate it 90 degrees to get 'left' or 'right', sort of.
		public void Face(Vector2d point, double within) {
			Vector2d offset = Vector2d.Subtract(point, Controlled.Loc);
			Vector2d controlledFacing = Misc.Vector2dFromDirection(Controlled.Facing + Misc.PIOVER2);
			double blar = Vector2d.Dot(controlledFacing, offset);
			
			// A slop factor in the angle here helps reduce wiggling.
			// XXX: Might be interesting to play with it someday to have "difficulty"
			if(blar > within) {
				Controlled.TurnLeft();
			} else if(blar < -within) {
				Controlled.TurnRight();
			}
		}
		
		public void Face(Vector2d point) {
			Face(point, 0.10);
		}
		
		public void FaceAway(Vector2d point, double within) {
			Vector2d offset = Vector2d.Subtract(point, Controlled.Loc);
			Vector2d controlledFacing = Misc.Vector2dFromDirection(Controlled.Facing + Misc.PIOVER2);
			double blar = Vector2d.Dot(controlledFacing, offset);
			
			if(blar > within) {
				Controlled.TurnRight();
			} else if(blar < -within) {
				Controlled.TurnLeft();
			}
		}
		
		public void FaceAway(Vector2d point) {
			FaceAway(point, 0.10);
		}
		
		
		// See Craig Reynolds's "Steering behaviors"
		// http://www.red3d.com/cwr/
		// This should be correct...
		public Vector2d Seek(Vector2d p) {
			Vector2d target = Vector2d.Subtract(p, Controlled.Loc);
			target = Vector2d.NormalizeFast(target);
			// The *2 is just for the sake of overkill
			target = Vector2d.Multiply(target, Controlled.MaxVel*2);
			return target;
		}
		
		public Vector2d Flee(Vector2d p) {
			Vector2d target = Vector2d.Subtract(Controlled.Loc, p);
			target = Vector2d.NormalizeFast(target);
			// The *2 is just for the sake of overkill
			target = Vector2d.Multiply(target, Controlled.MaxVel * 2);
			return target;
		}
		
		public Vector2d Pursue(Vector2d p, Vector2d vel) {
			Vector2d scaledVel = Vector2d.Multiply(vel, 10);
			Vector2d target = Vector2d.Add(p, scaledVel);
			return Seek(target);
		}
		
		public Vector2d Arrive(Vector2d p, double stopRadius) {
			//Console.WriteLine("Target location: {0}", p);
			Vector2d targetPoint = Vector2d.Subtract(p, Controlled.Loc);
			double distance = targetPoint.Length;
			double targetSpeed = (distance / stopRadius);
			// * Controlled.MaxVel;
			//Console.WriteLine("Target speed: {0}", targetSpeed);
			Vector2d targetDirection = Vector2d.NormalizeFast(targetPoint);
			Vector2d desiredVel = Vector2d.Multiply(targetDirection, targetSpeed);
			return desiredVel;
		}
		
		// This rather sucks but I don't REALLY care right now...
		// Some parameter tweaking may make it better.
		double WanderTheta = 0;
		public Vector2d Wander() {
			double wanderRadius = 16;
			double wanderDistance = 600;
			WanderTheta += Misc.Rand.NextDouble() - 0.5;
			Vector2d circle = Controlled.Vel;
			circle = Vector2d.NormalizeFast(circle);
			circle = Vector2d.Multiply(circle, wanderDistance);
			//circle = Vector2d.Add(circle, Controlled.Loc);
			
			Vector2d circleOffset = new Vector2d(wanderRadius * Math.Cos(WanderTheta),
				wanderRadius * Math.Sin(WanderTheta));
			Vector2d target = Vector2d.Add(circle, circleOffset);
			return target;
		}
		
		public Vector2d Cohesion(ICollection<IGameObj> nearbyObjects) {
			double neighborDist = 500;
			Vector2d sum = Vector2d.Zero;
			int count = 0;
			foreach(IGameObj o in nearbyObjects) {
				double d = Vector2d.Subtract(Controlled.Loc, o.Loc).Length;
				if(d < neighborDist) {
					sum = Vector2d.Add(sum, o.Loc);
					count += 1;
				}
			}
			sum = Vector2d.Divide(sum, count);
			sum = Vector2d.Subtract(sum, Controlled.Loc);
			sum = Vector2d.NormalizeFast(sum);
			sum = Vector2d.Multiply(sum, Controlled.MaxVel);
			//sum = Vector2d.Multiply
			return sum;
		}
		
		public Vector2d Separation(ICollection<IGameObj> nearbyObjects) {
			double neighborDist = 500;
			Vector2d sum = Vector2d.Zero;
			int count = 0;
			foreach(IGameObj o in nearbyObjects) {
				double d = Vector2d.Subtract(Controlled.Loc, o.Loc).Length;
				if(d < neighborDist) {
					sum = Vector2d.Add(sum, o.Loc);
					count += 1;
				}
			}
			sum = Vector2d.Divide(sum, count);
			sum = Vector2d.Subtract(sum, Controlled.Loc);
			sum = Vector2d.NormalizeFast(sum);
			sum = Vector2d.Multiply(sum, -Controlled.MaxVel);
			return sum;
		}
		
		// XXX: Baaaaaaaah, broked
		public Vector2d Alignment(ICollection<IGameObj> nearbyObjects) {
			double neighborDist = 500;
			double facingSum = 0;
			int count = 0;
			foreach(IGameObj o in nearbyObjects) {
				double d = Vector2d.Subtract(Controlled.Loc, o.Loc).Length;
				if(d < neighborDist) {
					facingSum += o.Facing;
					count += 1;
				}
			}
			
			facingSum = facingSum / count;
			Vector2d sum = Misc.Vector2dFromDirection(facingSum - Controlled.Facing );
			Console.WriteLine("Average facing: {0}", sum);
			return sum;
		}

	}
	
	public class TurretController : AIController {
		public override void Calc(GameState g, ICollection<IGameObj> nearbyObjects) {
			Face(g.Player.Loc);
			Controlled.Fire(g);
		}
	}
	
	// Experimental AI testbed
	public class BirdController : AIController {
		//Vector2d offset = Misc.PointWithin(200);
		public override void Calc(GameState g, ICollection<IGameObj> nearbyObjects) {
			//Vector2d playerOffset = Vector2d.Add(g.Player.Loc, offset);
			List<IGameObj> l = new List<IGameObj>();
			l.Add(g.Player);
			Vector2d sum = Cohesion(l);
			sum = Vector2d.Add(sum, Vector2d.Multiply(Separation(nearbyObjects), 0.5));
			Steer(sum);
			//Steer(Wander());
			//Steer(Arrive(playerOffset, 30));
			//Steer(Pursue(g.Player.Loc, g.Player.Vel));
		}
	}
	
	public class MissileController : AIController {
		public IGameObj Target {get;set;}
		
		public MissileController() {
		}
		
		public MissileController(IGameObj o) {
			Target = o;
		}
		
		public override void Calc(GameState g, ICollection<IGameObj> nearbyObjects) {
			//Face(Target.Loc);
			Controlled.Thrust();
		}
	}
	
	public class DumbShipController : AIController {
		public DumbShipController() {
		}

		public override void Calc(GameState g, ICollection<IGameObj> nearbyObjects) {
			Face(g.Player.Loc);
			Controlled.Thrust();
			Controlled.Fire(g);
		}
	}
	
	public class FlockController : IController {
		public IControllable Controlled { get; set; }
		public IGameObj Object { get; set; }
		
		IGameObj[] Flock {get;set;}

		public FlockController(IGameObj[] flock) {
			Flock = flock;
		}

		// Need to be able to Seek, Arrive, Avoid, 
		public void Calc(GameState g, ICollection<IGameObj> nearbyObjects) {
		}
	}
	*/
}
