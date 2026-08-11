shader_type spatial;
render_mode depth_draw_opaque, cull_disabled, unshaded, fog_disabled;

uniform vec4 underlay_color : source_color = vec4(0.16, 0.50, 0.57, 1.0);
uniform vec4 caustic_color : source_color = vec4(0.32, 0.72, 0.76, 1.0);
uniform float cell_scale : hint_range(0.2, 8.0, 0.01) = 1.55;
uniform float animation_speed : hint_range(0.0, 1.0, 0.01) = 0.07;
uniform float line_width : hint_range(0.005, 0.2, 0.005) = 0.018;
uniform float line_softness : hint_range(0.005, 0.2, 0.005) = 0.026;
uniform float caustic_strength : hint_range(0.0, 1.0, 0.01) = 0.58;
uniform float line_wobble_strength : hint_range(0.0, 0.6, 0.01) = 0.26;
uniform float line_bulge_strength : hint_range(0.0, 1.0, 0.01) = 0.82;
uniform float line_bulge_size : hint_range(0.0, 3.0, 0.01) = 1.45;
uniform float line_bulge_scale : hint_range(0.1, 3.0, 0.01) = 1.8;
uniform float line_distortion_scale : hint_range(0.2, 4.0, 0.01) = 1.4;
uniform vec2 pattern_offset = vec2(0.48, -0.34);
uniform vec2 water_center = vec2(0.0);
uniform float underlay_radius : hint_range(4.0, 40.0, 0.1) = 19.0;

varying vec3 world_position;

vec2 hash_position(vec2 point)
{
	vec3 hash = fract(vec3(point.xyx) * vec3(0.1031, 0.1030, 0.0973));
	hash += dot(hash, hash.yzx + 33.33);
	return fract((hash.xx + hash.yz) * hash.zy);
}

float value_noise(vec2 point)
{
	vec2 cell = floor(point);
	vec2 local_position = fract(point);
	vec2 blend = local_position * local_position * (
		3.0 - 2.0 * local_position);
	float bottom_left = hash_position(cell).x;
	float bottom_right = hash_position(cell + vec2(1.0, 0.0)).x;
	float top_left = hash_position(cell + vec2(0.0, 1.0)).x;
	float top_right = hash_position(cell + vec2(1.0, 1.0)).x;
	float bottom = mix(bottom_left, bottom_right, blend.x);
	float top = mix(top_left, top_right, blend.x);
	return mix(bottom, top, blend.y);
}

float rounded_line_lobe(
	float edge_distance,
	float tangent_coordinate,
	float edge_seed)
{
	float frequency = max(line_bulge_scale * 1.85, 0.1);
	float seeded_tangent = (
		tangent_coordinate * frequency + edge_seed * 17.3);
	float segment = floor(seeded_tangent);
	vec2 random_shape = hash_position(vec2(
		segment + edge_seed * 41.7,
		edge_seed * 73.1));
	float occurrence = hash_position(vec2(
		segment - edge_seed * 29.4,
		edge_seed * 103.7)).x;
	float center_phase = mix(0.24, 0.76, random_shape.x);
	float center_tangent = (
		segment + center_phase - edge_seed * 17.3) / frequency;
	float lobe_extent = (
		line_width + line_softness * 0.65
	) * line_bulge_size * line_bulge_strength * 1.20;
	float normal_radius = max(
		lobe_extent * mix(0.82, 1.18, random_shape.y),
		0.0001);
	float tangent_radius = max(normal_radius * 0.62, 0.0001);
	float normal_center = line_width + normal_radius * 0.18;
	vec2 lobe_position = vec2(
		(tangent_coordinate - center_tangent) / tangent_radius,
		(edge_distance - normal_center) / normal_radius);
	float round_lobe = 1.0 - smoothstep(
		0.72,
		1.0,
		length(lobe_position));
	return round_lobe * step(0.18, occurrence) * step(
		0.001,
		line_bulge_strength);
}

vec4 cellular_metrics(vec2 point, float time_value)
{
	vec2 cell = floor(point);
	vec2 local_position = fract(point);
	float nearest = 8.0;
	float second_nearest = 8.0;
	vec2 nearest_cell = vec2(0.0);
	vec2 nearest_position = vec2(0.0);
	vec2 second_nearest_position = vec2(0.0);

	for (int y = -1; y <= 1; y++)
	{
		for (int x = -1; x <= 1; x++)
		{
			vec2 neighbor = vec2(float(x), float(y));
			vec2 random_position = hash_position(cell + neighbor);
			random_position = 0.5 + 0.32 * sin(
				time_value + 6.283185 * random_position);
			vec2 candidate_position = cell + neighbor + random_position;
			float distance_to_point = length(
				neighbor + random_position - local_position);

			if (distance_to_point < nearest)
			{
				second_nearest = nearest;
				second_nearest_position = nearest_position;
				nearest = distance_to_point;
				nearest_cell = cell + neighbor;
				nearest_position = candidate_position;
			}
			else if (distance_to_point < second_nearest)
			{
				second_nearest = distance_to_point;
				second_nearest_position = candidate_position;
			}
		}
	}

	float nearest_seed = hash_position(
		nearest_cell + vec2(19.37, 41.73)).x;
	vec2 edge_direction = normalize(
		second_nearest_position - nearest_position);
	vec2 edge_tangent = vec2(-edge_direction.y, edge_direction.x);
	float tangent_coordinate = dot(point, edge_tangent);
	return vec4(
		second_nearest - nearest,
		nearest,
		nearest_seed,
		tangent_coordinate);
}

vec2 wobble_cell_position(
	vec2 point,
	float time_value,
	float wobble_strength,
	float distortion_scale)
{
	vec2 noise_position = point * distortion_scale * 0.55;
	vec2 noise_motion = vec2(time_value * 0.24, -time_value * 0.18);
	vec2 random_warp = vec2(
		value_noise(noise_position + noise_motion + vec2(13.7, 4.2)),
		value_noise(noise_position - noise_motion + vec2(3.1, 17.6))
	) * 2.0 - 1.0;
	vec2 detail_wave = vec2(
		sin(point.y * 4.20 - point.x * 1.15 + time_value * 0.70),
		cos(point.x * 3.90 + point.y * 1.25 - time_value * 0.62));
	return point + (
		random_warp * 1.35 +
		detail_wave * 0.22
	) * wobble_strength;
}

void vertex()
{
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment()
{
	vec2 relative_position = world_position.xz - water_center;

	if (length(relative_position) > underlay_radius)
		discard;

	float time_value = TIME * animation_speed;
	vec2 cell_position = relative_position * cell_scale + pattern_offset;
	cell_position += vec2(
		sin(relative_position.y * 0.16 + time_value),
		cos(relative_position.x * 0.14 - time_value)) * 0.12;
	cell_position = wobble_cell_position(
		cell_position,
		time_value,
		line_wobble_strength,
		line_distortion_scale);
	vec4 cell_data = cellular_metrics(cell_position, time_value);
	float caustic = 1.0 - smoothstep(
		line_width,
		line_width + line_softness,
		cell_data.x);
	float rounded_lobe = rounded_line_lobe(
		cell_data.x,
		cell_data.w,
		cell_data.z);
	caustic = max(caustic, rounded_lobe);
	float cell_tone = smoothstep(0.18, 0.85, cell_data.y);
	vec3 base_water_color = underlay_color.rgb * vec3(
		0.625,
		0.68,
		0.736842);
	vec3 water_color = mix(
		base_water_color,
		underlay_color.rgb,
		cell_tone * 0.38);
	water_color = mix(
		water_color,
		caustic_color.rgb,
		caustic * caustic_strength);

	ALBEDO = water_color;
	EMISSION = caustic_color.rgb * caustic * 0.035;
}
