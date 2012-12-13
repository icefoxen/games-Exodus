#version 120

uniform sampler2D tex;
void main() {
   gl_FragColor = texture2D(tex, gl_TexCoord[0].st);
   //gl_FragColor = vec4(1.0, 0.5, 0.75, 1);
   //gl_FragColor = gl_Color;
}
