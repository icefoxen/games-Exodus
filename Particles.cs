using System;
using OpenTK;

// Hmmm, this will probably load properties from a config file....
// Particle properties: Life, billboard, transparency, movement pattern, 
// Generator properties: Pattern (direction, angle), frequency, number, particle list...
// Shapes: Cone, circle...

namespace Exodus {

	// Moves in some way, lasts a certain time, draws something, doesn't interact.
	// We might also want to make these not inherit from BaseObj, to save memory.
	// There's no real reason for them to; particles don't interact with things, they just
	// move, draw, animate and eventually die.
	// Could also make a freelist, but eh.
	// But for now, oh well.
	public interface IParticle {
		// Animation...
		Sprite Sprite {get;set;}
		// How long until the particle dies, in update ticks!
		int Lifetime {get;set;}
		Vector2d Loc {get;set;}
		Vector2d Vel {get;set;}
		double Rotation {get;set;}
		void Calc(GameState g);
		void Draw(double dt);
		void Die(GameState g);
	}
	
	public class Particle : IParticle {
		public Sprite Sprite {get;set;}
		public int Lifetime {get;set;}
		public Vector2d Loc {get;set;}
		public Vector2d Vel {get;set;}
		public double Rotation {get;set;}
		
		Vector2d OldLoc;
		
		public Particle(Vector2d vec, Direction facing) : this(vec, facing, "blue-spark.bb") {
		}
		
		public Particle(Vector2d vec, Direction facing, string bb) : this(vec, facing, Loader.GetSprite(bb)) {
			
		}
		
		public Particle(Vector2d vec, Direction facing, Sprite bb) {
			Sprite = bb;
			Lifetime = 100;
		}
		
		public virtual void Calc(GameState g) {
			OldLoc = Loc;
			Loc = Vector2d.Add(Loc, Vel);
			
			Lifetime -= 1;
			if(Lifetime > 0) {
				//g.AddParticle(this);
			} else {
				g.KillParticle(this);
			}
			
			Sprite.Animate();
		}
		
		public virtual void Die(GameState g) {}
		
		public virtual void Draw(double dt) {
			Vector2d lerploc = Misc.LerpVector2d(OldLoc, Loc, dt);
			Vector3d v = new Vector3d(lerploc);
			Sprite.Draw(v);
		}
	}
	
	public delegate Particle ParticleMaker(Vector2d loc, double facing);
	public class ParticleFactory {
		public static Particle MakeParticle(Vector2d loc, Direction facing) {
			return new Particle(loc, facing);
		}
		
		// C-c-c-COMBINATORS!
		// ...okay, figure out what combinators we want.
		// OneOf, ManyOf, combinators that emit particles in a particular (hah) direction
		// and velocity and rotation...
		// Hmm.
		// XXX: You know, I think I'll just keep particle emission hard-wired for now...
		// XXX: You know, particles look a lot better when they're animated!
		// We want to flip between textures, interpolate between textures, change
		// object size, rotate and move textures/objects...
		public static Particle OneOf(ParticleMaker[] m, Vector2d loc, double f) {
			int choice = Misc.Rand.Next(m.Length);
			return m[choice](loc, f);
		}
	}

	
	// Creates more particles.
	// Types: Circular, directional, conical, constant/burst...
	public class ParticleGenerator : Particle {
		protected Func<Particle> PartMaker;
		protected int Last; // Time until next emit
		protected int Freq; // How often between emits
		protected int Count; // How many particles to emit
		public ParticleGenerator(Vector2d loc, Direction facing) : base(loc, facing) {}
		public override void Calc(GameState g) {
			base.Calc(g);
			if(Last == 0) {
				Last = Freq;
				Emit(g);
			} else {
				Last -= 1;
			}
		}
		public virtual void Emit(GameState g) {
			
		}
	}
	
	public class CircleGenerator : ParticleGenerator {
		double Speed;
		public CircleGenerator(Vector2d loc, double speed, int count, int frequency, Func<Particle> f) 
		: base(loc, 0) {
			Speed = speed;
			PartMaker = f;
			Count = count;
			Freq = frequency;
			Sprite = Loader.GetSprite("shot.bb");
		}
		
		public override void Emit(GameState g) {
			for(int i = 0; i < Count; i++) {
				Particle p = PartMaker();
				p.Loc = Loc;
				double angle = Misc.Rand.NextDouble() * (Math.PI*2);
				Vector2d vel;
				vel.X = Speed*Math.Cos(angle);
				vel.Y = Speed*Math.Sin(angle);
				p.Vel = vel;
				g.AddParticle(p);
			}
			Console.WriteLine("Emitted");
		}
	}
	
	public class ConeGenerator : ParticleGenerator {
		double Angle;
		double Speed;
		public ConeGenerator(Vector2d loc, Direction facing, double angle, double speed, 
			int count, int frequency, Func<Particle> f) : base(loc, facing) {
			PartMaker = f;
			Angle = angle;
			Count = count;
			Speed = speed;
			Freq = frequency;
		}
		
		public override void Emit(GameState g) {
			for(int i = 0; i < Count; i++) {
				Particle p = PartMaker();
				p.Loc = Loc;
				// Oh gods is this math right?
				double angle = ((Misc.Rand.NextDouble() - 0.5) * Angle); // + Facing;
				Vector2d vel;
				vel.X = Speed*Math.Cos(angle);
				vel.Y = Speed*Math.Sin(angle);
				p.Vel = vel;
				g.AddParticle(p);
			}
		}
	}
}
