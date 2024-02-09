#version 330

layout(location=0) in vec3 a_position;
layout(location=1) in vec2 a_uv;
layout(location=2) in vec3 a_normal;
layout(location=3) in vec4 a_color;
layout(location=4) in vec4 a_extra;

out vec3 v_position;
out vec2 v_tex;
out vec3 v_normal;
out vec4 v_color;
out vec4 v_shadowPos;
flat out int v_texID;

uniform mat4 u_viewMatrix;
uniform mat4 u_shadowMatrix;
uniform mat4 u_modelMatrix;

void main(void) 
{
	gl_Position = u_viewMatrix * u_modelMatrix * vec4(a_position.xyz, 1.0);
	v_position = (u_modelMatrix * vec4(a_position.xyz, 1.0)).xyz;
	v_tex    = a_uv;
	v_normal = a_normal;
	v_color  = a_color;
	v_shadowPos = u_shadowMatrix * u_modelMatrix * vec4(a_position.xyz, 1.0);
	v_texID = int(a_extra.x);
}