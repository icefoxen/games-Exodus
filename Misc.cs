using System;
using System.Collections.Generic;
using OpenTK;


namespace Exodus {
	public class Misc {
		public static Random Rand = new Random();
		private static long NextID = 0;
		public const double PHYSICSMAXSPEED = 10;
		// Can't be const.  >:-[
		public static Vector2d GRAVITY = new Vector2d(0, -0.1);
		
		public const double PIOVER2 = Math.PI / 2;
		public const double TWOPI = Math.PI * 2;
		
		// Returns a number that is forced to be between two values, inclusive.
		public static double Clamp(double num, double min, double max) {
			if(num < min) {
				return min;
			} else if(num > max) {
				return max;
			} else {
				return num;
			}
		}
		
		// Number of tiles wide and high the screen is.
		// Hardwiring these here is technically Evil, probably.
		// But I'm not 100% sure how to load them from a config file and
		// get them to every 
		
		public static long GetID() {
			NextID += 1;
			return NextID;
		}
		public static Vector2d Vector2dFromDirection(double d) {
			double x = Math.Cos(d);
			double y = Math.Sin(d);
			return new Vector2d(x,y);
		}
		
		public static void Vector2dFromDirection(double d, out Vector2d v) {
			v.X = Math.Cos(d);
			v.Y = Math.Sin(d);
		}
		
		public static double DirectionFromVector2d(Vector2d v) {
			return Math.Atan2(v.Y, v.X);
		}
		
		public static Vector2d Vector2dRotate(Vector2d v, double theta) {
			double x = v.X * Math.Cos(theta) - v.Y * Math.Sin(theta);
			double y = v.X * Math.Sin(theta) + v.Y * Math.Cos(theta);
			return new Vector2d(x, y);
		}
		
		public static bool IsPowerOf2(int i) {
			return (i != 0) && ((i & (i - 1)) == 0);
		}
		
		public static Vector2d PointWithin(double r) {
			double angle = Misc.Rand.NextDouble() * 2 * Math.PI;
			double distance = Misc.Rand.NextDouble() * r;
			Vector2d d = new Vector2d(Math.Cos(angle) * distance, Math.Sin(angle) * distance);
			return d;
		}
		
		public static Vector2d PointBetween(double min, double max) {
			double angle = Misc.Rand.NextDouble() * 2 * Math.PI;
			double distance = min + (Misc.Rand.NextDouble() * (max - min));
			Vector2d d = new Vector2d(Math.Cos(angle) * distance, Math.Sin(angle) * distance);
			return d;
		}
		
		// Wow, that was easy...
		public static Vector2d LeadObject(IGameObj g) {
			Vector2d diff = g.Loc - g.OldLoc;
			return Vector2d.Add(g.Loc, diff);
		}
		
		// From http://stackoverflow.com/questions/1073336/circle-line-collision-detection
		public static bool CollideRayWithCircle(Vector2d rayStart, Vector2d ray, Vector2d circle, double r) {
			Vector2d f = Vector2d.Subtract(rayStart, circle);
			double a = Vector2d.Dot(ray, ray);
			double b = 2 * Vector2d.Dot(f, ray);
			double c = Vector2d.Dot(f, f) - r * r;
			
			double discriminant = b * b - 4 * a * c;
			if(discriminant < 0) {
				return false;
				// no intersection
			} else {
				// ray didn't totally miss sphere,
				// so there is a solution to
				// the equation.
				
				
				discriminant = Math.Sqrt(discriminant);
				double t1 = (-b + discriminant) / (2 * a);
				//double t2 = (-b - discriminant) / (2 * a);
				
				if(t1 >= 0 && t1 <= 1) {
					return true;
					// solution on is ON THE RAY.
				} else {
					return false;
					// solution "out of range" of ray
				}
				
				// use t2 for second point
			}

		}
		
		/* v1 and v2 are vectors.
		 * v1' and v2' are velocity after collision.
		 * m1v1 + m2v2 = m1v1' + m2v2'
		 * 0.5m1v1^2 + 0.5m2v2^2 = 0.5m1v1'^2 0.5m2v2'^2
		 * 
		 * http://www.hoomanr.com/Demos/Elastic2/index.shtml
		 * This isn't the most efficient way to actually program it,
		 * but let's make it work first, hm?
		 * XXX: Rewrite!
		 */
		public static void DoCollision(IGameObj o1, IGameObj o2) {
			
			// First we have to figure out the new direction vectors for each thing...
			// u is initial velocity, v is final velocity
			Vector2d u1 = o1.Vel;
			Vector2d u2 = o2.Vel;
			Vector2d diff = Vector2d.Subtract(o1.Loc, o2.Loc);
			double a = Math.Atan2(diff.Y, diff.X);
			double d1 = Math.Atan2(u1.Y, u1.X);
			double d2 = Math.Atan2(u2.Y, u2.X);
			double v1x = u1.Length * Math.Cos(d1 - a);
			double v1y = u1.Length * Math.Sin(d1 - a);
			double v2x = u2.Length * Math.Cos(d2 - a);
			double v2y = u2.Length * Math.Sin(d2 - a);
			
			double smass = o1.Mass + o2.Mass;
			double f1x = (v1x * (o1.Mass - o2.Mass) + 2 * o2.Mass * v2x) / smass;
			double f2x = (v2x * (o2.Mass - o1.Mass) + 2 * o1.Mass * v1x) / smass;
			
			double m1 = Math.Sqrt(f1x * f1x + v1y * v1y);
			double m2 = Math.Sqrt(f2x * f2x + v2y * v2y);
			double fd1 = Math.Atan2(v1y, f1x) + a;
			double fd2 = Math.Atan2(v2y, f2x) + a;
			
			o1.Vel = new Vector2d(Math.Cos(fd1) * m1, Math.Sin(fd1) * m1);
			o2.Vel = new Vector2d(Math.Cos(fd2) * m2, Math.Sin(fd2) * m2);
			
			// Okay...  diff is a vector pointing from o2 to o1.
			// diff.Length < o1.Radius+o2.Radius
			// The amount of this difference is how much o1 and o2 overlap.
			// (at least in terms of bounding circles...)
			// So overlap = (o1.Radius+o2.Radius) - diff.Length
			// So o2 needs to be moved overlap/2 along -diff
			// and o1 needs to be moved overlap/2 along diff.
			// Also, when in doubt, use brute force.
			
			// XXX: Gameobjs always use bounding boxes now!
			/*
			if(o1.Collider is CircleCollider && o2.Collider is CircleCollider) {
				double overlap = ((CircleCollider)o1.Collider).Radius + ((CircleCollider)o2.Collider).Radius - diff.Length;
				
				Vector2d diffUnit = Vector2d.Normalize(diff);
				Vector2d adjust = Vector2d.Multiply(diffUnit, overlap / 1.5);
				
				o1.Loc += adjust;
				o2.Loc -= adjust;
			}
			*/
		}
		
		public static void LerpVector2d(ref Vector2d fromm, ref Vector2d to, double amount, out Vector2d between) {
			Vector2d diff, diff2;
			Vector2d.Subtract(ref to, ref fromm, out diff);
			Vector2d.Multiply(ref diff, amount, out diff2);
			Vector2d.Add(ref fromm, ref diff2, out between);
			//Console.WriteLine("From: {0} to {1} by {2}, result: {3}", fromm, to, amount, between);
		}
		
		public static Vector2d LerpVector2d(Vector2d fromm, Vector2d to, double amount) {
			Vector2d diff = Vector2d.Multiply(Vector2d.Subtract(to, fromm), amount);
			return Vector2d.Add(fromm, diff);
		}
		
		public static double LerpDouble(double fromm, double to, double amount) {
			double diff = (to-fromm);
			return fromm + (diff*amount);
		}
		
	}
}
