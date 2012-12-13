using System;
using System.Collections.Generic;
using OpenTK;

namespace Exodus {
	public enum Dimension {
		XDim,
		YDim
	}
	
	public class KdTree<T> {
		Vector2d Loc {get;set;}
		public Dimension Dim {get;set;}
		KdTree<T> Left {get;set;}
		KdTree<T> Right {get;set;}
		T Item {get;set;}
		
		public KdTree(Vector2d v, Dimension d, KdTree<T> l, KdTree<T> r, T i) {
			Loc = v;
			Dim = d;
			Left = l;
			Right = r;
			Item = i;
		}
		
		public KdTree(Vector2d v, Dimension d, T i) {
			Loc = v;
			Dim = d;
			Left = null;
			Right = null;
			Item = i;
		}

		public T Get(Vector2d target) {
			if(target == Loc) {
				return Item;
			} else {
				if(Dim == Dimension.XDim) {
					if(target.X < Loc.X) {
						if(Left == null) {
							throw new KeyNotFoundException();
						} else {
							return Left.Get(target);
						}
					} else {
						if(Right == null) {
							throw new KeyNotFoundException();
						} else {
							return Right.Get(target);
						}
					}
				} else {
					if(target.Y < Loc.Y) {
						if(Left == null) {
							throw new KeyNotFoundException();
						} else {
							return Left.Get(target);
						}
					} else {
						if(Right == null) {
							throw new KeyNotFoundException();
						} else {
							return Right.Get(target);
						}
					}
				}
			}
		}
		
		public List<T> GetWithin(Vector2d target, double d) {
			return GetWithin(target, d, new List<T>());
		}
		
		// Returns a list of items within d of the target
		// Upon consideration, it feels weird not making this tail-recursive.
		// It is totally a good idea to pass a list down the thing anyway, so we don't
		// merge lots of independent lists.  Makes it work much faster.
		public List<T> GetWithin(Vector2d target, double d, List<T> ret) {
			double dsquared = d * d;
			// First, we check and see if the current item is in range
			Vector2d dist = Vector2d.Subtract(Loc, target);
			if(dist.LengthSquared < dsquared) {
				ret.Add(Item);
			}
			
			// Then we check if the subtrees are in range and thus potential candidates.
			// For each dimension we do the same thing but compare different
			// parts of the target.
			if(Dim == Dimension.XDim) {
				// First see if we have to search the left subchild
				// We do this if the target.X is < current X or if target.X-d is < current X
				if(target.X - d < Loc.X ||
					target.X < Loc.X) {
					if(Left != null) {
						Left.GetWithin(target, d, ret);
					}
				
				}
				// Check the same thing for the right subchild
				if(target.X + d > Loc.X ||
					target.X > Loc.X) {
					if(Right != null) {
						Right.GetWithin(target, d, ret);
					}
				}
				// Process is exactly the same for YDim
			} else {
				if(target.Y - d < Loc.Y || target.Y < Loc.Y) {
					if(Left != null) {
						Left.GetWithin(target, d, ret);
					}
				}
				if(target.Y + d > Loc.Y || target.Y > Loc.Y) {
					if(Right != null) {
						Right.GetWithin(target, d, ret);
					}
				}
			}
			return ret;
		}
		
		public void Insert(Vector2d loc, T item) {
			if(Dim == Dimension.XDim) {
				if(loc.X < Loc.X) {
					if(Left == null) {
						Left = new KdTree<T>(loc, Dimension.YDim, item);
					} else {
						Left.Insert(loc, item);
					}
				} else {
					// loc.X >= Loc.X
					if(Right == null) {
						Right = new KdTree<T>(loc, Dimension.YDim, item);
					} else {
						Right.Insert(loc, item);
					}
				}
			} else {
				// Dim == Dimension.YDim
				if(loc.Y < Loc.Y) {
					if(Left == null) {
						Left = new KdTree<T>(loc, Dimension.XDim, item);
					} else {
						Left.Insert(loc, item);
					}
				} else {
					// loc.X >= Loc.X
					if(Right == null) {
						Right = new KdTree<T>(loc, Dimension.XDim, item);
					} else {
						Right.Insert(loc, item);
					}
				}
			}
		
		}
		
		public override String ToString() {
			return String.Format("Node at {0} with dimension {1}\nLeft: {2}\nRight: {3}",
				Loc, Dim, Left, Right);
		}
		
		
		#region Static members
		public const KdTree<T> Leaf = null;
		
		public static KdTree<T> BuildByInsertion(Func<T, Vector2d> locfunc, ICollection<T> items) {
			IEnumerator<T> e = items.GetEnumerator();
			e.MoveNext();
			KdTree<T> q = new KdTree<T>(locfunc(e.Current), Dimension.XDim, e.Current);
			while(e.MoveNext()) {
				q.Insert(locfunc(e.Current), e.Current);
			}
			return q;
		}

		// Is this building the tree in a particularly pessimal way, I wonder?
		public static KdTree<T> Build(Dimension d, Func<T, Vector2d> locfunc, ICollection<T> items) {

			if(items.Count == 0) {
				return Leaf;
			}
			// Choose pivot location
			Dimension splt;
			T item;
			Pivot(locfunc, items, d, out item, out splt);
			// Get the segments of the list on either side of the chosen pivot point
			ICollection<T> left, right;
			SplitAndRemove(locfunc, item, items, splt, out left, out right);
			// Recurse on either side
			KdTree<T> kdleft, kdright;
			kdleft = Build(splt, locfunc, left);
			kdright = Build(splt, locfunc, right);
			return new KdTree<T>(locfunc(item), splt, kdleft, kdright, item);
		}
		
		public static KdTree<T> Build(Func<T, Vector2d> locfunc, ICollection<T> items) {
			return Build(Dimension.XDim, locfunc, items);
		}
		
		
		// Returns items split along the location of item on dimension d, with item removed.
		private static void SplitAndRemove(Func<T,Vector2d> locfunc, T item, ICollection<T> items, Dimension d,
		                                   out ICollection<T> left, out ICollection<T> right) {
			Vector2d v = locfunc(item);
			left = new List<T>();
			right = new List<T>();
			foreach(T i in items) {
				// Iffy definition of "equals" here; remember, we're putting gameobj's in this
				if(i.Equals(item)) {
					continue;
				} else {
					Vector2d vi = locfunc(i);
					if(d == Dimension.XDim) {
						if(vi.X < v.X) {
							left.Add(i);
						} else {
							right.Add(i);
						}
					}
					else {
						if(vi.Y < v.Y) {
							left.Add(i);
						} else {
							right.Add(i);
						}
					}
				}
			}
		}
		
		// XXX: Might want a better pivot choice than just the first item in the collection.
		// Here we use a random pivot, which really doesn't seem to be much better than choosing the first.
		// So we might as well just choose the first, noob!
		// XXX: Error checking for empty collection!
		private static void Pivot(Func<T, Vector2d> locfunc, ICollection<T> items, Dimension d, 
								  out T oitem, out Dimension od) {
			// I guess this is basically the only way to get something out of an ICollection...
			int rand = Misc.Rand.Next() % items.Count;
			IEnumerator<T> e = items.GetEnumerator();
			e.MoveNext();
			for(int i = 0; i < rand; i++) {
				e.MoveNext();
			}
			oitem = e.Current;
			if(d == Dimension.XDim) {
				od = Dimension.YDim;
			}
			else {
				od = Dimension.XDim;
			}
		}
		#endregion
	}
}
