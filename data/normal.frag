#version 120

uniform vec4 Lambient, Ldiffuse, Lspecular;
uniform vec4 Lposition;

varying vec3 lightDir;
varying vec4 pos;

// Material.r = ambient, material.g = diffuse, material.b = specular, material.a = shininess
// Glow is currently unused.
uniform sampler2D tex, material, glow;
// Possibly over-complicated.
// But otherwise works really well!  :D
// Sort of, anyway.
void main() {
   gl_FragColor = texture2D(tex, gl_TexCoord[0].st);
/*
   vec4 mat = texture2D(material, gl_TexCoord[0].st);
   float mambient = mat.r;
   float mdiffuse = mat.g;
   float mspecular = mat.b;
   float mshininess = mat.a;

   vec4 s = -normalize(pos-Lposition);
   vec3 light = s.xyz;

   // We don't bother with normal maps, we just use a fixed normal.
   // 2D, yaknow.
   vec3 n = vec3(0,0,1); //normalize(normal);
   vec3 r = -reflect(light,n);
   r = normalize(r);
   vec3 v = -pos.xyz;
   v = normalize(v);

   vec4 diffuse = (max(0.0, dot(n, s.xyz)) * Ldiffuse) * mdiffuse;
   vec4 specular;
   if(mshininess != 0.0) {
      specular = Lspecular * mspecular * pow(max(0.0, dot(r,v)), mshininess);
   } else {
      specular = vec4(0,0,0,0);
   }

   // Lighting shall not contribute to alpha blending.
   vec4 ll = (Lambient * mambient) + diffuse + specular;
   ll.a = 1.0;

   vec4 nonglow = texture2D(tex, gl_TexCoord[0].st) * ll;
   vec4 glowpoint = texture2D(glow, gl_TexCoord[0].st) * texture2D(tex, gl_TexCoord[0].st);

   // This version of glow makes the glow determine percentage of environmental lighting.
   gl_FragColor = max(nonglow, glowpoint);
*/
}
