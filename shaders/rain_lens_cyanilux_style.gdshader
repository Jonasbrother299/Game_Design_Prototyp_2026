shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;

uniform float intensity : hint_range(0.0, 1.0) = 0.80;
uniform float drop_scale : hint_range(5.0, 100.0) = 28.0;
uniform float drop_density : hint_range(0.0, 1.0) = 0.18;
uniform float speed : hint_range(0.0, 2.0) = 0.18;

uniform float drop_radius : hint_range(0.01, 0.30) = 0.135;
uniform float distortion_strength : hint_range(0.0, 0.05) = 0.009;
uniform float blur_lod : hint_range(0.0, 2.0) = 0.03;

uniform float highlight_strength : hint_range(0.0, 1.0) = 0.16;
uniform float ring_strength : hint_range(0.0, 1.0) = 0.14;

float hash21(vec2 p)
{
vec3 p3 = fract(vec3(p.xyx) * 0.1031);
p3 += dot(p3, p3.yzx + 33.33);
return fract((p3.x + p3.y) * p3.z);
}

vec2 hash22(vec2 p)
{
float n1 = hash21(p);
float n2 = hash21(p + 17.73);
return vec2(n1, n2);
}

void fragment()
{
vec2 screen_uv = SCREEN_UV;
vec2 effect_uv = UV;

effect_uv.x += sin(effect_uv.y * 23.0 + TIME * 0.07) * 0.012;
effect_uv.y += sin(effect_uv.x * 17.0 + TIME * 0.05) * 0.010;

vec3 base = textureLod(screen_texture, screen_uv, 0.0).rgb;

vec2 grid_uv = effect_uv * vec2(drop_scale * 0.82, drop_scale * 1.28);
vec2 cell_id = floor(grid_uv);
vec2 cell_uv = fract(grid_uv) - 0.5;

vec2 rnd = hash22(cell_id);
float rnd3 = hash21(cell_id + 43.21);
float rnd4 = hash21(cell_id + 91.73);

float local_speed = mix(0.65, 1.35, rnd3) * speed;
float local_phase = rnd4 * 10.0;

float travel = fract(TIME * local_speed + local_phase);

float x_center = (rnd.x - 0.5) * 0.42;
float y_center = mix(-0.28, 0.28, travel) + (rnd.y - 0.5) * 0.16;

vec2 p = cell_uv - vec2(x_center, y_center);

vec2 drop_shape = vec2(p.x, p.y * 1.28);
float dist = length(drop_shape);

float drop = 1.0 - smoothstep(drop_radius * 0.55, drop_radius, dist);

float ring = smoothstep(drop_radius * 0.70, drop_radius * 0.95, dist);
ring *= 1.0 - smoothstep(drop_radius * 0.95, drop_radius * 1.25, dist);

vec2 normal = normalize(drop_shape + vec2(0.0001, 0.0001));
vec2 distort = normal * distortion_strength * drop * intensity;

vec3 refracted = textureLod(screen_texture, screen_uv - distort, blur_lod).rgb;

float highlight = pow(clamp(1.0 - dist / drop_radius, 0.0, 1.0), 3.0);
highlight *= highlight_strength * drop * intensity;

float visibility_random = hash21(cell_id + 11.2);
float visible = step(1.0 - drop_density, visibility_random);

vec3 color = mix(base, refracted, drop * 0.75);
color += vec3(highlight);
color += vec3(ring * ring_strength * intensity);

float alpha = (drop * 0.8 + ring * 0.35) * intensity * visible;
alpha = clamp(alpha, 0.0, 1.0);

COLOR = vec4(color, alpha);

}
