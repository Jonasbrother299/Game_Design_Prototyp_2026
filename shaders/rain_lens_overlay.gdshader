shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;
uniform sampler2D droplet_mask : repeat_disable, filter_linear;

uniform float intensity = 0.0;
uniform vec2 mask_resolution = vec2(512.0, 512.0);

uniform vec2 lens_center = vec2(0.50, 0.47);
uniform vec2 lens_scale = vec2(0.82, 1.18);
uniform float lens_radius = 0.46;
uniform float edge_softness = 0.055;
uniform float water_film_strength = 0.36;

uniform float refraction_strength = 0.035;
uniform float film_refraction_strength = 0.026;
uniform float blur_lod_inside = 1.55;
uniform float lens_darkness = 0.12;
uniform float rim_strength = 0.28;
uniform float droplet_highlight = 0.22;

float hash12(vec2 p)
{
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float noise2(vec2 p)
{
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);

	float a = hash12(i + vec2(0.0, 0.0));
	float b = hash12(i + vec2(1.0, 0.0));
	float c = hash12(i + vec2(0.0, 1.0));
	float d = hash12(i + vec2(1.0, 1.0));

	return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

float lens_mask(vec2 uv, out float lens_dist)
{
	vec2 p = (uv - lens_center) * lens_scale;
	float edge_noise = (noise2(uv * 4.0 + TIME * 0.012) - 0.5) * 0.016;
	lens_dist = length(p) + edge_noise;
	return smoothstep(lens_radius + edge_softness, lens_radius - edge_softness, lens_dist);
}

void fragment()
{
	float active = clamp(intensity, 0.0, 1.0);
	vec2 uv = SCREEN_UV;
	vec2 mask_uv = UV;

	vec2 mask_px = 1.0 / mask_resolution;

	float h = texture(droplet_mask, mask_uv).a;
	float h_l = texture(droplet_mask, mask_uv - vec2(mask_px.x, 0.0)).a;
	float h_r = texture(droplet_mask, mask_uv + vec2(mask_px.x, 0.0)).a;
	float h_u = texture(droplet_mask, mask_uv - vec2(0.0, mask_px.y)).a;
	float h_d = texture(droplet_mask, mask_uv + vec2(0.0, mask_px.y)).a;

	vec2 drop_gradient = vec2(h_r - h_l, h_d - h_u);

	float lens_dist = 0.0;
	float film = lens_mask(mask_uv, lens_dist) * water_film_strength;

	vec2 centered = (mask_uv - lens_center) * lens_scale;
	vec2 film_normal = normalize(centered + vec2(0.0001, 0.0001));
	float radial = clamp(lens_dist / max(lens_radius, 0.0001), 0.0, 1.0);

	vec2 distortion = drop_gradient * refraction_strength;
	distortion += film_normal * radial * radial * film * film_refraction_strength;
	distortion *= active;

	float combined_height = clamp(h + film, 0.0, 1.0);

	vec4 clean_scene = textureLod(screen_texture, uv, 0.0);
	vec4 sharp_scene = textureLod(screen_texture, uv + distortion, 0.0);
	vec4 blurred_scene = textureLod(screen_texture, uv + distortion, blur_lod_inside);

	float blur_amount = smoothstep(0.12, 0.85, combined_height) * active;
	vec3 refracted = mix(sharp_scene.rgb, blurred_scene.rgb, blur_amount);

	float luminance = dot(sharp_scene.rgb, vec3(0.299, 0.587, 0.114));
	float bright = smoothstep(0.48, 1.0, luminance);

	float edge = clamp(length(drop_gradient) * 8.0, 0.0, 1.0);
	float rim = smoothstep(lens_radius - 0.075, lens_radius, lens_dist);
	rim *= smoothstep(lens_radius + 0.025, lens_radius - 0.015, lens_dist);
	rim *= active;

	vec3 final_color = mix(clean_scene.rgb, refracted, combined_height * active);
	final_color = mix(final_color, final_color * (1.0 - lens_darkness), film * active);

	vec3 water_tint = vec3(0.72, 0.86, 0.95);
	final_color += water_tint * rim * rim_strength;
	final_color += water_tint * edge * (0.035 + bright * droplet_highlight) * active;

	COLOR = vec4(final_color, 1.0);
}
