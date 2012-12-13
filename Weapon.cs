using System;
using OpenTK;
namespace Exodus {
	public enum ShotMode {
		CYCLE,
		BURST
	}
	
	public delegate IGameObj MakeShot(Vector2d loc, double facing);
	
	public interface IWeapon {
		int RefireRate { get; set; }
		double EnergyCost { get; set; }
		ShotMode Mode { get; set; }
		WeaponPoint[] Points { get; set; }
		
		// Takes amount of energy, checks that it's okay, returns new energy amount
		double Fire(GameState g, double energy, Vector2d loc, double direction, Vector2d vel);
		void Calc(GameState g);	
	}
	
	public struct WeaponPoint {
		Vector2d FirePoint;
		Vector2d FireDirection;
		int BulletCount;
		double Deviation;
		double ShotVel;
		MakeShot ShotFunc;
		
		public WeaponPoint(MakeShot shotfunc, Vector2d point, Vector2d dir, double dev, double vel, int count) {
			FirePoint = point;
			FireDirection = dir;
			Deviation = dev;
			ShotVel = vel;
			BulletCount = count;
			ShotFunc = shotfunc;
		}
		
		public void Fire(GameState g, Vector2d loc, double facing, Vector2d vel) {
			
			Vector2d firePoint = Misc.Vector2dRotate(FirePoint, facing);
			firePoint = Vector2d.Add(firePoint, loc);
			
			for(int i = 0; i < BulletCount; i++) {
				double deviation = (Misc.Rand.NextDouble() * Deviation) - (Deviation / 2);
				double speedDeviation = (Misc.Rand.NextDouble() * Deviation * 5);
				Vector2d fireDir = Misc.Vector2dRotate(FireDirection, facing + deviation);
				IGameObj s = ShotFunc(firePoint, facing + deviation);
				s.Vel = Vector2d.Multiply(fireDir, ShotVel+speedDeviation);
				s.Vel = Vector2d.Add(s.Vel, vel);
				g.AddObj(s);
			}
		}
	}
	
	public class BaseWeapon : IWeapon {
		public ShotMode Mode { get; set; }
		public int RefireRate { get; set; }
		int ReloadState { get; set; }
		int NextShot { get; set; }
		public double EnergyCost { get; set; }
		public WeaponPoint[] Points { get; set; }
		
		public BaseWeapon(int refire, double energy, WeaponPoint[] p, ShotMode m) {
			ReloadState = 0;
			NextShot = 0;
			EnergyCost = energy;
			
			RefireRate = refire;
			Points = p;
			Mode = m;
		}
		
		public double Fire(GameState g, double energy, Vector2d loc, double facing, Vector2d vel) {
			if(ReloadState > 0 || energy < EnergyCost) {
				return energy;
			}
			ReloadState = RefireRate;
			
			switch(Mode) {
			case ShotMode.CYCLE:
				Points[NextShot].Fire(g, loc, facing, vel);
				NextShot = (NextShot + 1) % Points.Length;
				break;
			case ShotMode.BURST:
				foreach(WeaponPoint p in Points) {
					p.Fire(g, loc, facing, vel);
				}
				break;
			}
			return energy - EnergyCost;
		}
		
		public void Calc(GameState g) {
			ReloadState = Math.Max(0, ReloadState - 1);
		}
	}
	
}

