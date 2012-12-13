using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using OpenTK;

namespace Exodus {
	
	// Small is lovely.  Especially when you're making a 5000x500 array of enums.
	public enum BlockType : byte {
		Passable,
		Impassable,
		Jumpthroughable  // We can jump up through these blocks and land on top, and jump down through them.
	}
	
	public interface ILevel {
		int Width { get; set; }
		int Height { get; set; }
		HashSet<IGameObj> Objs { get; set; }
		BlockType[,] CollideMap {get;set;}
		void Add(IGameObj o);
		void Calc(GameState g);
		void Draw(Vector2d viewLocation);
	}
	
	// We've disabled depth testing, so we have to do all the draw ordering on our own...
	public class Level : ILevel {
		public int Width { get; set; }
		public int Height {get;set;}
		public HashSet<IGameObj> Objs { get; set; }
		public BlockType[,] CollideMap {get;set;}
		
		TileMap[] Layers;
		double[] Parallax;
		const int DrawWidth = Graphics.TilesWide + 1;
		const int DrawHeight = Graphics.TilesHigh + 1;
		public Level(int numLayers) {
			Objs = new HashSet<IGameObj>();
			
			Layers = new TileMap[numLayers];
			Parallax = new double[numLayers];
			for(int i = 0; i < Layers.Length; i++) {
				Parallax[i] = 1 + (0.2 * i);
				Layers[i] = new TileMap((int)500, (int)500, DrawWidth, DrawHeight, "tilesheet.png", 4, 4);
			}
		}
		
		public Level(TileMap[] layers, double[] parallaxes, int width, int height, BlockType[,] collideMap) {
			Objs = new HashSet<IGameObj>();
			Layers = layers;
			Parallax = parallaxes;
			if(layers.Length != parallaxes.Length) {
				throw new ArgumentException("Number of layers and parallax offsets must be the same!");
			}
			Width = width;
			Height = height;
			CollideMap = collideMap;
		}


		public void Draw(Vector2d viewLocation) {
			for(int i = 0; i < Layers.Length; i++) {
				Layers[i].DrawP(viewLocation, Parallax[i]);
			}
		}
		
		public void Add(IGameObj o) {
			Objs.Add(o);
		}
		public void Calc(GameState g) {
				
		}
	}
}
