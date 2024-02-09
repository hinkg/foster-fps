#version 330

uniform sampler2D u_texture0;
uniform sampler2D u_texture1;
uniform sampler2D u_texture2;
uniform sampler2D u_texture3;
uniform sampler2D u_texture4;
uniform sampler2D u_texture5;
uniform sampler2D u_texture6;
uniform sampler2D u_texture7;

uniform sampler2D u_textureShadow;
uniform vec3 u_sunDirection;
uniform float u_unlit;
uniform vec3 u_cameraPosition;

in vec3 v_position;
in vec2 v_tex;
in vec3 v_normal;
in vec4 v_color;
in vec4 v_shadowPos;
flat in int v_texID;

out vec4 frag_color;

#define PI 3.1415926535

float GetShadow(vec4 position) 
{
    vec3 proj = position.xyz / position.w * 0.5 + 0.5;

    float shadow = 0.0;

    vec2 texelSize = 1.0 / textureSize(u_textureShadow, 0);

    for(int y = -1; y <= 1; y++)
    {
        for(int x = -1; x <= 1; x++)
        {
            float map_depth = texture(u_textureShadow, proj.xy + vec2(x, y) * texelSize).r;
            shadow += map_depth > proj.z ? 1.0 : 0.0;
        }
    }

    shadow /= 9;
    return shadow;
}

void main(void) 
{
    vec4 tex_color = vec4(1.0);

    switch (v_texID) 
    {
        case 0:
            tex_color = texture(u_texture0, v_tex);
            break;
        case 1:
            tex_color = texture(u_texture1, v_tex);
            break;
        case 2:
            tex_color = texture(u_texture2, v_tex);
            break;
        case 3:
            tex_color = texture(u_texture3, v_tex);
            break;
        case 4:
            tex_color = texture(u_texture4, v_tex);
            break;
        case 5:
            tex_color = texture(u_texture5, v_tex);
            break;
        case 6:
            tex_color = texture(u_texture6, v_tex);
            break;
        case 7:
            tex_color = texture(u_texture7, v_tex);
            break;
    }

    float light = max(dot(v_normal, u_sunDirection), 0.0);
    light *= GetShadow(v_shadowPos);
    light *= pow(1.0 - max(dot(u_sunDirection, vec3(0.0, 0.0, -1.0)), 0.0), 20.0);

    light = mix(1.0, light, 0.75 * (1.0 - u_unlit));

    frag_color = vec4(tex_color.rgb * light, 1.0) * v_color;
}