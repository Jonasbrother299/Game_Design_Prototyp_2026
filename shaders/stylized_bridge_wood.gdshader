shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;

uniform vec4 base_tint : source_color = vec4(1.0);
uniform float roughness : hint_range(0.0, 1.0, 0.01) = 0.88;
uniform float grain_scale : hint_range(1.0, 20.0, 0.1) = 7.2;
uniform float grain_strength : hint_range(0.0, 0.5, 0.01) = 0.24;
uniform float brush_strength : hint_range(0.0, 0.5, 0.01) = 0.18;
uniform float edge_darkening : hint_range(0.0, 0.7, 0.01) = 0.32;
uniform float instance_variation : hint_range(0.0, 0.4, 0.01) = 0.20;

varying vec2 painter_uv;
varying vec4 instance_tint;
varying float instance_seed;

float hash_value(vec2 value)
{
	vec3 hash = fract(vec3(value.xyx) * vec3(0.1031, 0.1030, 0.0973));
	hash += dot(hash, hash.yzx + 33.33);
	return fract((hash.x + hash.y) * hash.z);
}

float value_noise(vec2 position)
{
	vec2 cell = floor(position);
	vec2 local_position = fract(position);
	vec2 blend = local_position * local_position * (
		3.0 - 2.0 * local_position);
	float bottom_left = hash_value(cell);
	float bottom_right = hash_value(cell + vec2(1.0, 0.0));
	float top_left = hash_value(cell + vec2(0.0, 1.0));
	float top_right = hash_value(cell + vec2(1.0, 1.0));
	float bottom = mix(bottom_left, bottom_right, blend.x);
	float top = mix(top_left, top_right, blend.x);
	return mix(bottom, top, blend.y);
}

void vertex()
{
	painter_uv = UV;
	instance_tint = COLOR;
	instance_seed = fract(
		sin(float(INSTANCE_ID) * 12.9898 + 4.1414) * 43758.5453);
}

void fragment()
{
	vec2 seed_offset = vec2(instance_seed * 17.31, instance_seed * 8.73);
	float broad_tone = value_noise(
		painter_uv * vec2(3.1, 1.45) + seed_offset);
	float grain_warp = value_noise(
		painter_uv * vec2(2.2, 7.4) + seed_offset * 1.67) - 0.5;
	float grain_wave = sin((
		painter_uv.x * grain_scale +
		grain_warp * 1.35 +
		instance_seed * 3.7) * 6.283185) * 0.5 + 0.5;
	float grain_noise = value_noise(
		painter_uv * vec2(grain_scale * 1.8, 3.2) + seed_offset * 2.4);
	float grain = mix(grain_wave, grain_noise, 0.34);

	float brush_noise = value_noise(
		painter_uv * vec2(10.5, 3.4) + seed_offset * 3.1);
	float brush_band = sin((
		painter_uv.y * 3.3 +
		broad_tone * 0.72 +
		instance_seed) * 6.283185) * 0.5 + 0.5;
	float brush_pattern = mix(brush_noise, brush_band, 0.38);
	brush_pattern = floor(brush_pattern * 5.0) / 5.0;

	float instance_tone = mix(
		1.0 - instance_variation,
		1.0 + instance_variation,
		instance_seed);
	float surface_tone = 1.0 +
		(broad_tone - 0.5) * 0.34 +
		(grain - 0.5) * 2.0 * grain_strength;
	vec3 color = base_tint.rgb * instance_tint.rgb *
		instance_tone * surface_tone;

	float light_brush = smoothstep(0.58, 0.92, brush_pattern);
	float dark_brush = smoothstep(0.62, 0.96, 1.0 - brush_pattern);
	vec3 dry_brush_color = color * vec3(1.28, 1.12, 0.82);
	color = mix(
		color,
		dry_brush_color,
		light_brush * brush_strength);
	color *= 1.0 - dark_brush * brush_strength * 0.46;

	vec2 edge_distance = min(painter_uv, vec2(1.0) - painter_uv);
	float edge = 1.0 - smoothstep(
		0.012,
		0.15,
		min(edge_distance.x, edge_distance.y));
	vec3 edge_color = color * vec3(0.54, 0.42, 0.30);
	color = mix(color, edge_color, edge * edge_darkening);

	ALBEDO = max(color, vec3(0.0));
	ROUGHNESS = clamp(
		roughness + (grain - 0.5) * 0.12,
		0.0,
		1.0);
	SPECULAR = 0.16;
}
