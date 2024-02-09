#version 330

uniform sampler2D u_skyTexture;
uniform sampler2D u_cloudTexture;

uniform vec3 u_sunDirection;
uniform float u_scroll;

in vec2 v_tex;
in vec3 v_normal;
in vec4 v_color;

out vec4 frag_color;

#define PI 3.1415926535

float luminance(vec3 col)
{
    return dot(col, vec3(0.2125, 0.7154, 0.0721));
}

void main(void) 
{
    float uv_y = acos(normalize(v_normal).z) / PI;
    float uv_x = atan(-normalize(v_normal).y, -normalize(v_normal).x) / (PI*2);

    float cloudAlpha = texture(u_cloudTexture, vec2(uv_x + u_scroll, uv_y)).a * 0.75;
    cloudAlpha *= pow(max(dot(normalize(v_normal), u_sunDirection), 0.0), 10) * 0.5 + 1.0;

    vec4 skyColor = texture(u_skyTexture, vec2(uv_x, uv_y));

    vec3 color = skyColor.rgb;

    float alpha = luminance(skyColor.rgb);
    color.rgb = mix(color.rgb, vec3(1.0), alpha * cloudAlpha);

    frag_color = vec4(color.rgb, 1.0);
}