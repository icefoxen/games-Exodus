using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;

using OpenTK;
using OpenTK.Graphics.OpenGL;

// XXX: Do we even need meshomatic anymore?  Well it still loads billboards...
using Meshomatic;
using Newtonsoft.Json.Linq;

namespace Exodus {
	
	// XXX: Maybe this should use weak references?  It wasn't important before, but now level files can be BIG...
	// However, we are only going to be loading new levels at pretty well-defined boundaries, so...
	// ...pshaw.  Just don't use memoizers for the level map files.
	class Memoizer<T> {
		Func<string,T> loader;
		Dictionary<string,T> cache;
		
		public Memoizer(Func<string, T> f) {
			cache = new Dictionary<string, T>();
			loader = f;
		}
		public T Get(string name) {
			try {
				return cache[name];
			} catch(KeyNotFoundException) {
				Console.WriteLine("Loading {0}", name);
				try {
					T t = loader(name);
					cache.Add(name, t);
					return t;
				} catch {
					Console.WriteLine("Error loading {0}!", name);
					throw;
				}
			}
		}
	}
	public class Loader {
		static Memoizer<uint> texloader;
		static Memoizer<Vbo> geomloader;
		static Memoizer<int> soundloader;
		static Memoizer<JObject> configloader;
		static Memoizer<Sprite> spriteloader;
		const string datadir = "../../data/";
		
		public static void Init() {
			texloader = new Memoizer<uint>(file => LoadTex(file));
			geomloader = new Memoizer<Vbo>(file => LoadGeom(file));
			soundloader = new Memoizer<int>(file => LoadSound(file));
			configloader = new Memoizer<JObject>(file => LoadConfig(file));
			spriteloader = new Memoizer<Sprite>(file => LoadSprite(file));
		}
		public static uint GetTex(string file) {
			return texloader.Get(datadir + file);
		}
		public static Vbo GetGeom(string file) {
			return geomloader.Get(datadir + file);
		}
		public static int GetSound(string file) {
			return soundloader.Get(datadir + file);
		}
		public static JObject GetConfig(string file) {
			return configloader.Get(datadir + file);
		}
		
		// This uses GetConfig, which caches everything anyway.
		public static TileMap GetTileMap(string file) {
			return LoadTileMap(file);
		}
		
		public static Sprite GetSprite(string file) {
			return spriteloader.Get(file);
		}
		
		public static string ReadFile(string file) {
			using(StreamReader s = new StreamReader(datadir + file)) {
				return s.ReadToEnd();
			}
		}
		
		public static uint LoadTex(string file) {
			Bitmap bitmap = new Bitmap(file);
			if(!Misc.IsPowerOf2(bitmap.Width) || !Misc.IsPowerOf2(bitmap.Height)) {
				// FormatException isn't really the best here, buuuut...
				throw new FormatException("Texture sizes must be powers of 2!");
			}
			uint texture;
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            
            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);
		

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			
			return texture;
		}

		static Vbo LoadGeom(string file) {
			MeshData m = new ObjLoader().LoadFile(file);
			float[] verts; float[] norms; float[] texcoords; uint[] indices;
			m.OpenGLArrays(out verts, out norms, out texcoords, out indices);
			
			bool v = false;
			for(int i = 0; i < texcoords.Length; i++) {
				if(v) {
					texcoords[i] = 1 - texcoords[i];
					v = false;
				} else {
					v = true;
				}
			}
			return new Vbo(verts, norms, texcoords, indices);
		}
		
		static int LoadSound(string file) {
			return 0;
		}
		
		static JObject LoadConfig(string file) {
			using(var f = new StreamReader(file)) {
				var j = JObject.Parse(f.ReadToEnd());
				return j;
			}
		}
		
		static TileMap LoadTileMap(string file) {
			var config = Loader.GetConfig(file);
			return LoadTileMap(config);
		}
		
		static TileMap LoadTileMap(JToken config) {
			var tilesheet = config["tilesheet"].ToString();
			var tilesheetW = int.Parse(config["tilesheetWidth"].ToString());
			var tilesheetH = int.Parse(config["tilesheetHeight"].ToString());
			// XXX: Depends on drawing size!  Technically probably shouldn't be here, but...
			var drawWidth = Graphics.TilesWide + 1;
			var drawHeight = Graphics.TilesHigh + 1;
			
			var mapdataFile = datadir + config["data"].ToString();
			using(var bitmap = new Bitmap(mapdataFile)) {
				var w = bitmap.Size.Width;
				var h = bitmap.Size.Height;
				var mapData = new ushort[w * h];
				
				// Might not be the most efficient way, but... who cares?
				for(int i = 0; i < h; i++) {
					for(int j = 0; j < w; j++) {
						var pix = bitmap.GetPixel(j, i);
						// Note the blue channel goes unused.
						// We could put shit there if we want.  But we ain't gonna.
						mapData[(i * w) + j] = (ushort) ((pix.R << 8) | pix.G);
					}
				}
				var map = new TileMap(w, h, drawWidth, drawHeight, tilesheet, tilesheetW, tilesheetH, mapData);
				return map;
			}
		}
		
		public static Level LoadLevel(string file) {
			var config = Loader.GetConfig(file);
			var collMapFile = datadir + config["collisionMap"].ToString();
			
			// I dislike doubling the code here, but it's not EXACTLY identical to
			// loading a tilemap...
			using(var bitmap = new Bitmap(collMapFile)) {
				var w = bitmap.Size.Width;
				var h = bitmap.Size.Height;
				var collMap = new BlockType[w,h];
				for(int i = 0; i < h; i++) {
					for(int j = 0; j < w; j++) {
						var pix = bitmap.GetPixel(j, h - i - 1);
						// Pretty crude, but it works I suppose.
						if(pix.R > 0) {
							collMap[j,i] = BlockType.Impassable;
						} else if(pix.G > 0) {
							collMap[j,i] = BlockType.Jumpthroughable;
						} else {
							collMap[j,i] = BlockType.Passable;
						}
					}
				}
				
				/*
				for(int i = 0; i < h; i++) {
					for(int j = 0; j < w; j++) {
						Console.Write("{0}, ", collMap[(i * w) + j]);
					}
					Console.WriteLine();
				}
				*/
				var tilemapConfigs = config["tiles"];
				var tilemaps = new List<TileMap>();
				var parallaxes = new List<double>();
				foreach(var t in tilemapConfigs) {
					
					tilemaps.Add(LoadTileMap(t));
					parallaxes.Add(double.Parse(t["parallax"].ToString()));
				}
				return new Level(tilemaps.ToArray(), parallaxes.ToArray(), w, h, collMap);
			}
		}
		
		// Hmm!  I could support loading from atlases just by making
		// x and y offsets here, and handling them properly in the Sprite class.
		// Something to think about.
		// It would require either loading subsections of images from LoadTex,
		// or supporting them in the Vbo class though.
		static Sprite LoadSprite(string file) {
			var config = Loader.GetConfig(file);
			var w = double.Parse(config["width"].ToString());
			var h = double.Parse(config["height"].ToString());
						
			var imageStrings = config["images"];
			uint[] texs;
			if(imageStrings.Type == JTokenType.Array) {
				List<uint> l = new List<uint>();
				foreach(var token in imageStrings.Values()) {
					l.Add(Loader.GetTex(token.ToString()));
				}
				texs = l.ToArray();
			} else {
				throw new Exception("'delay' property is not a list!");
			}
			
			var delayStrings = config["delays"];
			uint[] delays;
			if(delayStrings.Type == JTokenType.Array) {
				List<uint> l = new List<uint>();
				foreach(var token in delayStrings.Values()) {
					l.Add(uint.Parse(token.ToString()));
				}
				delays = l.ToArray();
			} else {
				throw new Exception("'delay' property is not a list!");
			}
			
			if(texs.Length != delays.Length) {
				throw new Exception("Number of images and delays in animation is different!");
			}
					
			return new Sprite(texs, delays, new Vector2d(w, h));
			
		}
	}
}