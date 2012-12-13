using System;

using OpenTK;

namespace Exodus {
	public class Collider {
		public Vector2d Location { get; set; }
		
		public virtual bool Colliding(Collider c) {
			return false;
		}
		public virtual bool CollideWithCircle(CircleCollider c) {
			return false;
		}
		public virtual bool CollideWithRay(RayCollider c) {
			return false;
		}
		public virtual bool CollideWithBB(BBCollider c) {
			return false;
		}
	}
	
	
	public class CircleCollider : Collider {
		public double Radius;
		public CircleCollider(Vector2d loc, double rad) {
			Location = loc;
			Radius = rad;
		}

		public override bool Colliding(Collider c) {
			// I hereby dub this retarded new technique "method overload reflection"
			// I didn't invent it, but apparently I did re-invent it.
			// See, if we have 
			// Collider c = new CircleCollider(loc,rad); Collider d = new CircleCollider(loc,rad); d.Colliding(c);
			// then all C and D know about each other is that they're both Colliders, d.Colliding(Collider) will
			// get called!
			// But in this method D knows it's a CircleCollider, so it calls c.Colliding(CircleCollider), and 
			// all necessary type information is magically there again!
			// Thus we basically have D do dynamic dispatch off of itself.
			// This is apparently 'the double-dispatch pattern'
			// Or we could just use Lisp and have multiple dispatch anyway... hmmmm...
			return c.CollideWithCircle(this);
		}
		public override bool CollideWithCircle(CircleCollider c) {
			Vector2d separation = Vector2d.Subtract(Location, c.Location);
			double rad = Radius + c.Radius;
			return separation.LengthSquared <= (rad * rad);
		}
		public override bool CollideWithRay(RayCollider c) {
			return Misc.CollideRayWithCircle(c.Location, c.Ray, Location, Radius);
		}
	}
	
	public class RayCollider : Collider {
		public Vector2d Ray;
		public RayCollider(Vector2d loc, Vector2d ray) {
			Ray = ray;
			Location = loc;
		}

		public override bool Colliding(Collider c) {
			return c.CollideWithRay(this);
		}
		public override bool CollideWithCircle(CircleCollider c) {
			return Misc.CollideRayWithCircle(Location, Ray, c.Location, c.Radius);
		}
		
		/*
		 * Something like this may be apropos:
# // calculates intersection and checks for parallel lines.  
# // also checks that the intersection point is actually on  
# // the line segment p1-p2  
# Point findIntersection(Point p1,Point p2,  
#   Point p3,Point p4) {  
#   float xD1,yD1,xD2,yD2,xD3,yD3;  
#   float dot,deg,len1,len2;  
#   float segmentLen1,segmentLen2;  
#   float ua,ub,div;  
#   
#   // calculate differences  
#   xD1=p2.x-p1.x;  
#   xD2=p4.x-p3.x;  
#   yD1=p2.y-p1.y;  
#   yD2=p4.y-p3.y;  
#   xD3=p1.x-p3.x;  
#   yD3=p1.y-p3.y;    
#   
#   // calculate the lengths of the two lines  
#   len1=sqrt(xD1*xD1+yD1*yD1);  
#   len2=sqrt(xD2*xD2+yD2*yD2);  
#   
#   // calculate angle between the two lines.  
#   dot=(xD1*xD2+yD1*yD2); // dot product  
#   deg=dot/(len1*len2);  
#   
#   // if abs(angle)==1 then the lines are parallell,  
#   // so no intersection is possible  
#   if(abs(deg)==1) return null;  
#   
#   // find intersection Pt between two lines  
#   Point pt=new Point(0,0);  
#   div=yD2*xD1-xD2*yD1;  
#   ua=(xD2*yD3-yD2*xD3)/div;  
#   ub=(xD1*yD3-yD1*xD3)/div;  
#   pt.x=p1.x+ua*xD1;  
#   pt.y=p1.y+ua*yD1;  
#   
#   // calculate the combined length of the two segments  
#   // between Pt-p1 and Pt-p2  
#   xD1=pt.x-p1.x;  
#   xD2=pt.x-p2.x;  
#   yD1=pt.y-p1.y;  
#   yD2=pt.y-p2.y;  
#   segmentLen1=sqrt(xD1*xD1+yD1*yD1)+sqrt(xD2*xD2+yD2*yD2);  
#   
#   // calculate the combined length of the two segments  
#   // between Pt-p3 and Pt-p4  
#   xD1=pt.x-p3.x;  
#   xD2=pt.x-p4.x;  
#   yD1=pt.y-p3.y;  
#   yD2=pt.y-p4.y;  
#   segmentLen2=sqrt(xD1*xD1+yD1*yD1)+sqrt(xD2*xD2+yD2*yD2);  
#   
#   // if the lengths of both sets of segments are the same as  
#   // the lenghts of the two lines the point is actually  
#   // on the line segment.  
#   
#   // if the point isn't on the line, return null  
#   if(abs(len1-segmentLen1)>0.01 || abs(len2-segmentLen2)>0.01)  
#     return null;  
#   
#   // return the valid intersection  
#   return pt;  
# }  

         If we ever need it.
		 */
		public override bool CollideWithRay(RayCollider c) {
			return false;
		}
	}
	
	// Simple axis-aligned bounding box.
	public class BBCollider : Collider {
		// The position is the bottom-left corner of the box.
		public Vector2d Dimensions { get; set; }
		public BBCollider(Vector2d loc, double width, double height) {
			Location = loc;
			Dimensions = new Vector2d(width, height);
		}
		public BBCollider(double width, double height) : this(Vector2d.Zero, width, height) {
		}
		public override bool Colliding(Collider c) {
			return c.CollideWithBB(this);
		}
		public override bool CollideWithRay(RayCollider c) {
			return false;
		}
		public override bool CollideWithBB(BBCollider c) {
			var other = c.Location;
			var otherD = c.Dimensions;
			if((Location.X + Dimensions.X) < other.X
				|| Location.X > (other.X + otherD.X)
				|| (Location.Y + Dimensions.Y) < other.Y
				|| Location.Y > (other.Y + otherD.Y)) {
				return false;
			}
			return true;
		}
	}
}

