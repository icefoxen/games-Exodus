using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Exodus {
	public class Graphics {
		public static Matrix4d Projection;
		public static Matrix4d Modelview;
		public static double ClipNear = 10.0;
		public static double ClipFar = 1000.0;
		
		public static Vector3d Up = new Vector3d(1.0, 0.0, 0.0);
		public static Vector3d OutOfScreen = new Vector3d(0.0, 1.0, 0.0);
		public static Vector3d CameraTarget = new Vector3d(0.0, 0.0, 0.0);
		// This produces a view port 32 units across and 24 tall.
		public const double AspectRatio = 4.0/3.0;
		public const double CameraDistance = 24.0;
		// This works out to the following...
		public const int TilesWide = 32;
		public const int TilesHigh = 24;
		
		public static Quaterniond StandardRotation = Quaterniond.FromAxisAngle(Graphics.OutOfScreen, -Misc.PIOVER2);
		
		public const int TILESHADER = 0;
		public const int NORMALSHADER = 1;
		public const int NUMSHADERS = 2;
		public static int[] Shaders;
		
		public static Light L1 = new Light();
		
		static int BuildShader(string shadername) {
			string shadervert = shadername + ".vert";
			string shaderfrag = shadername + ".frag";
			
			int programHandle, vHandle, fHandle;
			vHandle = GL.CreateShader(ShaderType.VertexShader);
			fHandle = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(vHandle, Loader.ReadFile(shadervert));
			GL.ShaderSource(fHandle, Loader.ReadFile(shaderfrag));
			GL.CompileShader(vHandle);
			GL.CompileShader(fHandle);
			Console.WriteLine("Built shader {0}", shadername);
			Console.WriteLine("Vertex shader errors: {0}", GL.GetShaderInfoLog(vHandle));
			Console.WriteLine("Fragment shader errors: {0}", GL.GetShaderInfoLog(fHandle));
			
			programHandle = GL.CreateProgram();
			GL.AttachShader(programHandle, vHandle);
			GL.AttachShader(programHandle, fHandle);
			GL.LinkProgram(programHandle);
			Console.Write(GL.GetProgramInfoLog(programHandle));
			return programHandle;
}
		
		public static void InitShaders() {
			Shaders = new int[NUMSHADERS];
			Shaders[TILESHADER] = BuildShader("tile");
			Shaders[NORMALSHADER] = BuildShader("normal");

		}
		
		public static void InitGL() {
			GL.EnableClientState(ArrayCap.VertexArray);
			//GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.TextureCoordArray);
			GL.Enable(EnableCap.Texture2D);
			GL.Disable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.ActiveTexture(TextureUnit.Texture1);
			GL.ActiveTexture(TextureUnit.Texture2);
			GL.ClearColor(Color.Blue);
			
			InitShaders();
			GL.UseProgram(Shaders[NORMALSHADER]);
			
			StartDraw(Vector2d.Zero);
			
			L1.Ambient = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);
			L1.Diffuse = new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
			L1.Specular = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
			
		}
		// XXX: Resize() and StartDraw() leave the matrix in a different mode than they got it in...
		public static void Resize() {
			Projection = Matrix4d.CreateOrthographic(CameraDistance * AspectRatio, 
				CameraDistance, ClipNear, ClipFar);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref Projection);
		}
		
		// Does not draw a background
		public static void StartDraw(Vector2d target) {
			// Coordinate transform between gameworld and screenworld coordinates.
			GL.UseProgram(Shaders[NORMALSHADER]);
			CameraTarget.X = target.Y;
			CameraTarget.Y = 0.0;
			CameraTarget.Z = target.X;
			
			// This fixes the light position to (10 units above) the gameworld origin.
			L1.Position.X = (float)-target.X;
			L1.Position.Y = (float)-target.Y;
			L1.Position.Z = (float)-0;
			L1.BindToShader(Shaders[Graphics.NORMALSHADER]);
			
			Vector3d CameraLoc = Vector3d.Multiply(OutOfScreen, CameraDistance);
			CameraLoc = Vector3d.Add(CameraTarget, CameraLoc);
			Modelview = Matrix4d.LookAt(CameraLoc, CameraTarget, Up);
			
			// We also need to transform 
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref Modelview);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		
		// This version does draw the background.
		public static void StartDraw(Vector2d target, Sprite background) {
			StartDraw(target);
			
			Vector3d bgLoc = new Vector3d(target.X * 0.9, target.Y * 0.9, -(ClipFar - CameraDistance));

			background.Draw(bgLoc);
		}
		
		public static void StartGui() {
			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Disable(EnableCap.DepthTest);
			GL.Ortho(-50, 50, -37.5, 37.5, -200, 200);
			GL.MatrixMode(MatrixMode.Modelview);
		}
		
		public static void EndGui() {
			GL.Enable(EnableCap.DepthTest);
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();
		}
	}
	
	public struct Vbo {
		uint IndexHandle;
		uint DataHandle;
		int IndexCount;
		
		int VertOffset;
		int NormOffset;
		int TexcoordOffset;
		
		public Vbo(float[] verts, float[] norms, float[] texcoords, uint[] indices) {
			if(verts == null) throw new ArgumentException("Vbo()", "verts");
			if(indices == null) throw new ArgumentException("Vbo()", "indices");
			if(norms == null) throw new ArgumentException("Vbo()", "norms");
			if(texcoords == null) throw new ArgumentException("Vbo()", "texcoords");
			
			IndexCount = indices.Length;
			
			int dataCount = verts.Length + norms.Length + texcoords.Length;
			float[] dataBuffer = new float[dataCount];
			
			VertOffset = 0;
			NormOffset = verts.Length;
			TexcoordOffset = (verts.Length + norms.Length);
			
			verts.CopyTo(dataBuffer, VertOffset);
			norms.CopyTo(dataBuffer, NormOffset);
			texcoords.CopyTo(dataBuffer, TexcoordOffset);
			
			GL.GenBuffers(1, out DataHandle);
			GL.BindBuffer(BufferTarget.ArrayBuffer, DataHandle);
			GL.BufferData<float>(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float)*dataCount),
			                     dataBuffer, BufferUsageHint.StaticDraw);
			
			GL.GenBuffers(1, out IndexHandle);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexHandle);
			GL.BufferData<uint>(BufferTarget.ElementArrayBuffer,(IntPtr)(sizeof(uint)*indices.Length), 
			                       indices, BufferUsageHint.StaticDraw);
			
			 GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			 GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}
		
		public void Draw() {
			// Push current Array Buffer state so we can restore it later
			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);

			// Normal buffer
			GL.BindBuffer(BufferTarget.ArrayBuffer, DataHandle);
			//GL.NormalPointer(NormalPointerType.Float, 0, NormOffset*sizeof(float));

			// TexCoord buffer
			GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, (IntPtr) (TexcoordOffset*sizeof(float)));
			
			// Vertex buffer
			GL.VertexPointer(3, VertexPointerType.Float, 0, (IntPtr) (VertOffset*sizeof(float)));
			
			// Index array
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexHandle);
			GL.DrawElements(BeginMode.Triangles, IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

            // Restore the state
            GL.PopClientAttrib();
		}
	}
	
	// Material properties will be expressed via a second texture.
	// RGBA channels -> ambient, diffuse, specular, and alpha.
	// Then we can slap on another RGBA layer for emission/glow...
	// WARNING!  If the lighting terms have an alpha channel other than 1,
	// light WILL make things transparent by shining on them!
	
	public struct Light {
		// Only first 3 of these terms are used, but 4 makes the math easier.
		public Vector4 Ambient;
		public Vector4 Diffuse;
		public Vector4 Specular;
		public Vector4 Position;  // If 4th param = 0, light is directional, otherwise point source.
		
		public Light(Vector4 pos, Vector4 amb, Vector4 diff, Vector4 spec) {
			Position = pos;
			Ambient = amb;
			Diffuse = diff;
			Specular = spec;
		}
		
		public void BindToShader(int shader) {
			int hAmb = GL.GetUniformLocation(shader, "Lambient");
			int hDiff = GL.GetUniformLocation(shader, "Ldiffuse");
			int hSpec = GL.GetUniformLocation(shader, "Lspecular");
			int hPos = GL.GetUniformLocation(shader, "Lposition");
			
			GL.Uniform4(hAmb, Ambient);
			GL.Uniform4(hDiff, Diffuse);
			GL.Uniform4(hSpec, Specular);
			GL.Uniform4(hPos, Position);
		}
		
	}
		/*
	public struct Material {
		public uint Texture;
		public uint Props;
		public uint Glow;
		
		public Material(uint t, uint m, uint g) {
			Texture = t;
			Props = m;
			Glow = g;
		}
		
		public void Draw() {
			DrawWithShader(Graphics.Shaders[Graphics.NORMALSHADER]);
		}
		
		public void DrawWithShader(int shader) {
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			
			GL.ActiveTexture(TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture2D, Props);
			
			GL.ActiveTexture(TextureUnit.Texture2);
			GL.BindTexture(TextureTarget.Texture2D, Glow);
			
			int t1 = GL.GetUniformLocation(shader, "tex");
			GL.Uniform1(t1, 0);
			int t2 = GL.GetUniformLocation(shader, "material");
			GL.Uniform1(t2, 1);
			int t3 = GL.GetUniformLocation(shader, "glow");
			GL.Uniform1(t3, 2);
		}
	}
	public class Mesh {
		protected Vbo geometry;
		protected Material material;
		protected Vector2d scale;
		
		public Mesh(Vbo vbo, Material m) {
			geometry = vbo;
			material = m;
			scale = Vector2d.One;
		}
		
		public Mesh(Vbo vbo, Material m, Vector2d s) {
			geometry = vbo;
			material = m;
			scale = s;
		}
		public void Draw(Vector3d location) {
			//Quaterniond q = Quaterniond.Identity;
			Quaterniond q = Quaterniond.FromAxisAngle(Graphics.OutOfScreen, -Misc.PIOVER2);
			Draw(location, q);
		}
		
		public virtual void Draw(Vector3d location, Quaterniond rotation) {
			Matrix4d trans;
			Matrix4d rot;
			Matrix4d sca;
			Matrix4d temp;
			Vector3d axis;
			double angle;
			//Console.WriteLine("DDDDrawing at {0},{1},{2}", location.X, location.Y, location.Z);
			
			// Translate and rotate stuff.
			trans = Matrix4d.CreateTranslation(location);
			rotation.ToAxisAngle(out axis, out angle);
			Matrix4d.CreateFromAxisAngle(axis, angle, out rot);
			sca = Matrix4d.Scale(scale.X, 1, scale.Y);
			
			temp = Matrix4d.Mult(Graphics.Modelview, trans);
			temp = Matrix4d.Mult(sca, Matrix4d.Mult(rot, temp));
			
			//Console.WriteLine("MMMMatrix:\n{0}", temp);
			GL.PushMatrix();
			GL.LoadMatrix(ref temp);
			//Console.WriteLine("Matrix'd");
			
			// Do drawing stuff.
			//GL.ClientActiveTexture(TextureUnit.Texture0);
			//GL.ActiveTexture(TextureUnit.Texture1);
			//GL.BindTexture(TextureTarget.Texture2D, tex);
			//int loc = GL.GetUniformLocation(Graphics.CurrentShader, "tex1");
			//GL.Uniform1(loc, 1);
			//Console.WriteLine("Texture'd {0}", tex);
			
			material.Draw();
			geometry.Draw();
			GL.PopMatrix();
		}
	}
	*/
	
	// XXX: Fuuuuuuuck animations, tilesheets...
	// Eventually this needs to handle animations, of the flip-book style.
	// Should also be able to handle rotations... hmmm.  This should probably
	// become Image or Billboard or such, and there should be a Sprite wrapper
	// class that does all that fancy stuff.  It WILL need a tilesheet, and hence
	// a config file.  So.
	// This works for now though.
	// We could adapt this system to a tilesheet by having an "image" type that is
	// just a texture+geometry, with its place on the texture sheet specified by
	// config file that generates the texcoords... but...  eh.
	public class Sprite {
		protected Vbo geometry;
		protected Vector2d scale;
		
		
		// The counter just increments each calculation tick --5 ms
		// It is reset to 0 each time we flip to a new frame.
		protected uint animCounter;
		// Which animation frame we are on
		protected uint animFrame;
		// Array of textures
		protected uint[] frames;
		// Specifies the delay for each frame --how many calculation ticks
		// until we switch to the next one.
		protected uint[] animDelays;
		
		// No animation at all!
		public Sprite(string tex, Vector2d size) : 
			this(new string[1]{tex}, new uint[1]{1}, size) {
		}
		
		// I suddenly am more sympathetic for SmugLispWeenies, for being
		// able to rewrite the language to do this for them.
		private static uint[] FuckMeHarder(string[] s) {
			var mykingdomforarraymap = new uint[s.Length];
			for(int i = 0; i < s.Length; i++) {
				mykingdomforarraymap[i] = Loader.GetTex(s[i]);
			}
			return mykingdomforarraymap;
		}
		public Sprite(string[] tex, uint[] delays, Vector2d size) :
			this(FuckMeHarder(tex), delays, size) {
		}
		
		public Sprite(uint tex, Vector2d size) :
			this(new uint[1]{tex}, new uint[1]{1}, size) {
		}
		
		public Sprite(uint[] tex, uint[] delays, Vector2d size) {
			frames = tex;
			animDelays = delays;
			animFrame = 0;
			animCounter = 0;
			
			if(frames.Length != animDelays.Length) {
				throw new Exception("Fuuuuuuuuck animation count and delay count is not the same!");
			}
			
			// XXX: Can we hard-code this perhaps, instead of loading a mesh?
			// One less resource to worry about...
			geometry = Loader.GetGeom("billboard.obj");
			scale = size;
		}
		
		// Should be called every physics frame, updates timers and
		// sets which frame to use.
		public void Animate() {
			animCounter += 1;
			uint delay = animDelays[animFrame];
			if(animCounter == delay) {
				animCounter = 0;
				animFrame = (uint) ((animFrame + 1) % frames.Length);
			}
		}
		
		// XXX: Do this!
		public void Draw(Vector3d location, double rotation) {
		}
		
		public void Draw(Vector3d location) {
			Draw(location, Graphics.StandardRotation);
		}

		public virtual void Draw(Vector3d location, Quaterniond rotation) {
			Matrix4d trans;
			Matrix4d rot;
			Matrix4d sca;
			Matrix4d temp;
			Vector3d axis;
			double angle;
			//Console.WriteLine("DDDDrawing at {0},{1},{2}", location.X, location.Y, location.Z);
			
			// Translate and rotate stuff.
			trans = Matrix4d.CreateTranslation(location);
			rotation.ToAxisAngle(out axis, out angle);
			Matrix4d.CreateFromAxisAngle(axis, angle, out rot);
			sca = Matrix4d.Scale(scale.X, 1, scale.Y);
			
			temp = Matrix4d.Mult(Graphics.Modelview, trans);
			temp = Matrix4d.Mult(sca, Matrix4d.Mult(rot, temp));
			
			//Console.WriteLine("MMMMatrix:\n{0}", temp);
			GL.PushMatrix();
			GL.LoadMatrix(ref temp);
			//Console.WriteLine("Matrix'd");
			
			// Do drawing stuff.
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, frames[animFrame]);
			geometry.Draw();
			GL.PopMatrix();
		}
	}
	
	/*
	
	// This will work, though it's probably pretty inefficient.  A better way might be to,
	// instead of having all geoms/textures/scales synchronized, having each have a different
	// counter with a delay between the current and next frames.
	// Even better would be to be able to interpolate scaling on the fly, but I don't expect to
	// really use that significantly, so.
	public class Animation : Mesh {
		protected int current = 0;
		protected int max = 0;
		protected Vbo[] geoms;
		protected Material[] materials;
		protected Vector2d[] scales;
		
		public Animation(Vbo[] g, Material[] t, Vector2d[] s) : base(g[0], t[0], s[0]) {
			if((g.Length != t.Length) || (t.Length != s.Length)) {
				throw new FormatException("Animation got non-similar parameters!");
			}
			// Might die already from the base() parameters...
			if(g.Length < 1) {
				throw new FormatException("Animation needs actual contents!");
			}
			geoms = g;
			materials = t;
			scales = s;
			max = g.Length;
		}
		
		// ...and so on for other constructors.  Gotta be some way to refactor that out.
		public Animation(Vbo[] g, Material[] t, Vector2d s) : base(g[0], t[0], s) {
			if((g.Length != t.Length)) {
				throw new FormatException("Animation got non-similar parameters!");
			}
			// Might die already from the base() parameters...
			if(g.Length < 1) {
				throw new FormatException("Animation needs actual contents!");
			}
			Vector2d[] ss = new Vector2d[g.Length];
			// We don't have an Array.Fill() method????
			for(int i = 0; i < ss.Length; i++) {ss[i] = s;}
			geoms = g;
			materials = t;
			scales = ss;
			max = g.Length;
		}
		
		public void Next() {
			current = (current+1) % max;
			geometry = geoms[current];
			material = materials[current];
			scale = scales[current];
		}
	}
*/
}