using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace Exodus {
	
	// TODO:
	// Separate physical map size from display map size more betterer.
	// Do bounds checking, and handle it if you try to scroll off the edge of the map
	// Load a map from a file/array?
	// Make a class that holds multiple TileMaps and does layering and parallax.
	// WTF is the difference between DrawWidth and MapWidth???
	// Ooooh, MapWidth is the width of the actual map, DrawWidth is the width of the segment
	// actually being drawn on screen... a subset of the full map.
	public class TileMap {
		ushort[] Map;
		int MapWidth, MapHeight;
		int DrawWidth, DrawHeight;
		Vector2 TileSize;
		// Number of tiles on the tile sheet.
		float TileSheetWidth;
		float TileSheetHeight;
		
		uint TileSheet;
		uint VboHandle;
		uint TexCoordHandle;
		
		public TileMap(int width, int height, int drawWidth, int drawHeight, 
			string tilesheet, int tilesheetWidth, int tilesheetHeight) {
			TileSize = new Vector2(1, 1);
			if(width < 0 || height < 0) {
				throw new IndexOutOfRangeException("Dimensions of a tilemap must be >0!");
			}
			Map = new ushort[width * height];
			MapWidth = width;
			MapHeight = height;
			DrawWidth = drawWidth;
			DrawHeight = drawHeight;
			
			TileSheet = Loader.GetTex(tilesheet);
			TileSheetWidth = tilesheetWidth;
			TileSheetHeight = tilesheetHeight;
			
			MakeRandomMap();
			MakeTileVBO();
		}
		
		public TileMap(int width, int height, int drawWidth, int drawHeight, 
			string tilesheet, int tilesheetWidth, int tilesheetHeight,
			ushort[] map) {
			TileSize = new Vector2(1, 1);
			if(width < 0 || height < 0) {
				throw new IndexOutOfRangeException("Dimensions of a tilemap must be >0!");
			}
			Map = map;
			MapWidth = width;
			MapHeight = height;
			DrawWidth = drawWidth;
			DrawHeight = drawHeight;
			
			TileSheet = Loader.GetTex(tilesheet);
			TileSheetWidth = tilesheetWidth;
			TileSheetHeight = tilesheetHeight;
			
			MakeTileVBO();
		}
		
		// This builds a mesh for a background, just a series of quads
		// This seems inefficient, at 80 bytes per quad, until you realize with 50*50 quads (the screen is 40*30)
		// it uses up a whole 200 kb.
		float[] makeTileMap(int width, int height) {
			int numVerts = width * height * 4;
			int numElements = numVerts * 3;
			float[] elements = new float[numElements];
			// i and j refer to tiles, so we have to translate that to actual
			// offsets in the array...
			for(int i = 0; i < height; i++) {
				for(int j = 0; j < width; j++) {
					int coordsPerTile = 12;
					int coordsPerRow = coordsPerTile * width;
					int xoffset = (j * coordsPerTile);
					int yoffset = (i * coordsPerRow);
					int offset = xoffset + yoffset;
					
					float x1 = j * TileSize.X;
					float x2 = (j + 1) * TileSize.X;
					float y1 = i * TileSize.Y;
					float y2 = (i + 1) * TileSize.Y;
					
					elements[offset + 0] = y1;
					elements[offset + 1] = 0;
					elements[offset + 2] = x1;
					
					elements[offset + 3] = y1;
					elements[offset + 4] = 0;
					elements[offset + 5] = x2;
					
					elements[offset + 6] = y2;
					elements[offset + 7] = 0;
					elements[offset + 8] = x2;
					
					elements[offset + 9] = y2;
					elements[offset + 10] = 0;
					elements[offset + 11] = x1;
				}
			}
			return elements;
		}
		
		// This copies a block of texcoords into an array and slaps
		// it into a VBO.
		// XXX: We might want to use vertex arrays instead of VBO's, since the data is changing a lot.
		// Can we do that?
		void SetupTexcoords(int offsetx, int offsety, int width, int height) {
			int numVerts = width * height * 4;
			int numCoords = numVerts * 2;
			float[] verts = new float[numCoords];
			float tileWidth = 1 / TileSheetWidth;
			float tileHeight = 1 / TileSheetHeight;
			
			for(int i = 0; i < height; i++) {
				for(int j = 0; j < width; j++) {
					int coordsPerTile = 8;
					int coordsPerRow = coordsPerTile * width;
					int xoffset = (j * coordsPerTile);
					int yoffset = (i * coordsPerRow);
					int offset = xoffset + yoffset;
					
					int tileIndex = Math.Min(Map.Length - 1, (j + offsetx) + ((i + offsety)*MapWidth));
					int tileNumber = Map[tileIndex];
					// Sorta ugly but it works.
					float tileX = (tileNumber % TileSheetWidth) * tileWidth;
					float tileY = (float) (Math.Floor(tileNumber / TileSheetHeight) * tileHeight);
					
					float x1 = tileX + tileWidth;
					float x2 = tileX;
					float y1 = tileY;
					float y2 = tileY + tileHeight;
					//Console.WriteLine("TileX: {0} TileY: {1}", tileX, tileY);
					//Console.WriteLine("Number of coords: {0}, {1},{2}, now working on set {3}", numCoords, i, j, offset);
					
					verts[offset + 0] = y1;
					verts[offset + 1] = x1;
					
					verts[offset + 2] = y2;
					verts[offset + 3] = x1;
					
					verts[offset + 4] = y2;
					verts[offset + 5] = x2;
					
					verts[offset + 6] = y1;
					verts[offset + 7] = x2;
					
				}
			}
			
			GL.BindBuffer(BufferTarget.ArrayBuffer, TexCoordHandle);
			GL.BufferData<float>(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * verts.Length), 
				verts, BufferUsageHint.StaticDraw);
			
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			/*
			GL.EnableClientState(ArrayCap.TextureCoordArray);
			GL.TexCoordPointer(3, TexCoordPointerType.Float, 0, verts);
			*/
		}
		
		void MakeTileVBO() {
			float[] verts = makeTileMap(DrawWidth, DrawHeight);
			
			GL.GenBuffers(1, out VboHandle);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);
			GL.BufferData<float>(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * verts.Length), 
				verts, BufferUsageHint.StaticDraw);
			
			GL.GenBuffers(1, out TexCoordHandle);
			// Don't need to generate tex coordinates since it's done on the fly.
			
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}
		
		void MakeRandomMap() {
			double numPossibleTiles = (TileSheetWidth * TileSheetHeight) - 1;
			for(int i = 0; i < MapWidth * MapHeight; i++) {
				ushort val = (ushort)Math.Round(Misc.Rand.NextDouble() * numPossibleTiles);
				//Console.WriteLine("Tile #{0}", val);
				Map[i] = val;
			}
		}
		
		public void Draw(Vector2d location, int xOffset, int yOffset) {
			//Console.WriteLine("Location: {0},{1}  offsets {2},{3}", location.X, location.Y, xOffset, yOffset);
			if(location.X + DrawWidth > MapWidth || 
				location.Y + DrawHeight > MapHeight ||
				location.X < 0 ||
				location.Y < 0) {
				string message = String.Format("Tried to draw out of bounds at {0},{1}", location.X, location.Y);
				throw new IndexOutOfRangeException(message);
			}
			Vector3d location2 = new Vector3d(location.X, location.Y, 0);
			Matrix4d trans = Matrix4d.CreateTranslation(location2);
			Matrix4d temp = Matrix4d.Mult(Graphics.Modelview, trans);
			
			GL.PushMatrix();
			GL.LoadMatrix(ref temp);
			
			// Push current Array Buffer state so we can restore it later
			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
			GL.UseProgram(Graphics.Shaders[Graphics.TILESHADER]);
			
			// Texture sheet
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, TileSheet);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
			
			SetupTexcoords(xOffset, yOffset, DrawWidth, DrawHeight);
			
			// Set up buffers.
			GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);
			GL.VertexPointer(3, VertexPointerType.Float, 0, (IntPtr)0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, TexCoordHandle);
			GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, (IntPtr)0);
			GL.DrawArrays(BeginMode.Quads, 0, DrawWidth * DrawHeight * 4);
			
			/*
			for(int i = 0; i < MapHeight; i++) {
				int vertsPerStrip = MapWidth * 4;
				int offset = i * vertsPerStrip;
				GL.DrawArrays(BeginMode.Quads, offset, vertsPerStrip);
			}
			*/

			// Restore the state
		GL.UseProgram(Graphics.Shaders[Graphics.NORMALSHADER]);
			GL.PopClientAttrib();
			
			GL.PopMatrix();
		}
		
		// Draw with parallax
		public void DrawP(Vector2d viewLocation, double parallax) {
			// XXX: This assumes tile size = 1
			// Is this ever not going to be true?
			Vector2d parallaxLocation = Vector2d.Multiply(viewLocation, parallax);
			Vector2d tileStartingCoordinate = new Vector2d(Math.Truncate(parallaxLocation.X), Math.Truncate(parallaxLocation.Y));
			
			Vector2d parallaxDrawOffset = Vector2d.Subtract(parallaxLocation, tileStartingCoordinate);
			Vector2d drawLocation = Vector2d.Subtract(viewLocation, parallaxDrawOffset);
			/*
				Console.WriteLine("Layer: {0}, View location: {1}", i, viewLocation);
				Console.WriteLine("Parallax location: {0}, tile starting coordinate: {1}", 
					parallaxLocation, tileStartingCoordinate);
				Console.WriteLine("Parallax draw offset: {0}, draw location: {1}",
					parallaxDrawOffset, drawLocation);
				Console.WriteLine();
			 */				
			Draw(drawLocation, (int)tileStartingCoordinate.X, (int)tileStartingCoordinate.Y);
		}
		
		void Validate() {
			double numPossibleTiles = (TileSheetWidth * TileSheetHeight) - 1;
			foreach(int i in Map) {
				if(i > numPossibleTiles) {
					throw new IndexOutOfRangeException("Index in map that's out of bounds of the tile map!");
				}
			}
		}
	}
}

