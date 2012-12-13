using System;
using System.Collections.Generic;
using OpenTK;
namespace Exodus {
	public class QuadTree<T> {
		public Vector2d Loc;
		public T Item;
		public QuadTree<T> UpperLeft;
		public QuadTree<T> UpperRight;
		public QuadTree<T> LowerLeft;
		public QuadTree<T> LowerRight;
		
		public QuadTree(Vector2d loc, T i) {
			Item = i;
			Loc = loc;
			UpperLeft = null;
			UpperRight = null;
			LowerLeft = null;
			LowerRight = null;
		}
		
		public void Insert(Vector2d loc, T item) {
			if(loc.X > Loc.X) {
				if(loc.Y > Loc.Y) {
					if(UpperRight == null) {
						UpperRight = new QuadTree<T>(loc, item);
					} else {
						UpperRight.Insert(loc, item);
					}
				} else {
					// loc.Y <= Loc.Y
					if(LowerRight == null) {
						LowerRight = new QuadTree<T>(loc, item);
					} else {
						LowerRight.Insert(loc, item);
					}
				}
			} else {
				// loc.X <= Loc.X
				if(loc.Y > Loc.Y) {
					if(UpperLeft == null) {
						UpperLeft = new QuadTree<T>(loc, item);
					} else {
						UpperLeft.Insert(loc, item);
					}
				} else {
					// loc.Y <= Loc.Y
					if(LowerLeft == null) {
						LowerLeft = new QuadTree<T>(loc, item);
					} else {
						LowerLeft.Insert(loc, item);
					}
				}
			}
		}
		
		public List<T> GetWithin(Vector2d target, double d) {
			return GetWithin(target, d, new List<T>());
		}
		
		// Returns a list of items within d of the target
		// Upon consideration, it feels weird not making this tail-recursive.
		public List<T> GetWithin(Vector2d target, double d, List<T> ret) {
			double dsquared = d * d;
			// First, we check and see if the current item is in range
			Vector2d dist = Vector2d.Subtract(Loc, target);
			if(dist.LengthSquared < dsquared) {
				ret.Add(Item);
			}
			
			if(target.X + d > Loc.X || target.X > Loc.X) {
				if(target.Y + d > Loc.Y || target.Y > Loc.Y) {
					if(UpperRight != null) {
						UpperRight.GetWithin(target, d, ret);
					}
				}
				if(target.Y - d <= Loc.Y || target.Y <= Loc.Y) {
					if(LowerRight != null) {
						LowerRight.GetWithin(target, d, ret);
					}
				}
			}
			if(target.X - d <= Loc.X || target.X <= Loc.X) {
				if(target.Y + d > Loc.Y || target.Y > Loc.Y) {
					if(UpperLeft != null) {
						UpperLeft.GetWithin(target, d, ret);
					}
				}
				if(target.Y - d <= Loc.Y || target.Y <= Loc.Y) {
					if(LowerLeft != null) {
						LowerLeft.GetWithin(target, d, ret);
					}
				}
			}
			
			return ret;
		}
		
		#region Static methods
		public const QuadTree<T> Leaf = null;
		
		public static QuadTree<T> Build(Func<T, Vector2d> locfunc, ICollection<T> items) {
			// Gotta sorta hand-start the iterator because the first node needs an item in it.
			IEnumerator<T> e = items.GetEnumerator();
			e.MoveNext();
			QuadTree<T> q = new QuadTree<T>(locfunc(e.Current), e.Current);
			while(e.MoveNext()) {
				q.Insert(locfunc(e.Current), e.Current);
			}
			return q;
		}
		
		// I was going to try different methods of building the tree, like taking the items collection
		// and sorting or splitting it into bits and then inserting each separately...
		// ...but the current method works REALLY WELL anyway.
		/*
		public static QuadTree<T> Build(Func<T, Vector2d> locfunc, ICollection<T> items) {
			return null;
		}
		*/
		#endregion
	}
}

