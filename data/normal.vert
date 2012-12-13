#version 120
uniform vec4 Lposition;

varying vec3 lightDir;
varying vec4 pos;

void main() {
	lightDir = normalize(vec3(Lposition));
	pos = gl_ModelViewMatrix * gl_Vertex;
	gl_Position = ftransform();
	gl_TexCoord[0] = gl_MultiTexCoord0;
	//gl_TexCoord[0] = vec2(0,0);
	//gl_TexCoord[0] = ftransform();
}
