shader_type spatial;
render_mode blend_mix, depth_draw_never, cull_disabled, unshaded, fog_disabled;

uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

uniform vec4 surface_color : source_color = vec4(0.14, 0.46, 0.54, 1.0);
uniform vec4 line_color : source_color = vec4(0.32, 0.72, 0.76, 1.0);
uniform vec4 foam_color : source_color = vec4(1.0);
uniform vec4 rain_ripple_color : source_color = vec4(0.72, 0.92, 0.96, 1.0);

uniform float cell_scale : hint_range(0.2, 8.0, 0.01) = 2.30;
uniform float animation_speed : hint_range(0.0, 1.0, 0.01) = 0.07;
uniform float refraction_strength : hint_range(0.0, 0.05, 0.0005) = 0.008;
uniform float refraction_mix : hint_range(0.0, 1.0, 0.01) = 0.32;
uniform float refraction_edge_reach : hint_range(0.02, 1.5, 0.01) = 0.55;
uniform float refraction_edge_falloff : hint_range(0.1, 6.0, 0.05) = 1.35;
uniform float refraction_center_strength : hint_range(0.0, 1.0, 0.01) = 0.16;
uniform float refraction_scale : hint_range(0.1, 8.0, 0.05) = 1.30;
uniform float refraction_speed : hint_range(0.0, 3.0, 0.01) = 0.16;
uniform float line_width : hint_range(0.005, 0.2, 0.005) = 0.018;
uniform float line_softness : hint_range(0.005, 0.2, 0.005) = 0.026;
uniform float line_strength : hint_range(0.0, 1.0, 0.01) = 0.58;
uniform float line_wobble_strength : hint_range(0.0, 0.6, 0.01) = 0.26;
uniform float line_bulge_strength : hint_range(0.0, 1.0, 0.01) = 0.82;
uniform float line_bulge_size : hint_range(0.0, 3.0, 0.01) = 1.45;
uniform float line_bulge_scale : hint_range(0.1, 3.0, 0.01) = 1.8;
uniform float line_distortion_scale : hint_range(0.2, 4.0, 0.01) = 1.4;
uniform float wave_height : hint_range(0.0, 0.05, 0.001) = 0.002;
uniform float contact_width : hint_range(0.005, 1.0, 0.005) = 0.045;
uniform float contact_softness : hint_range(0.005, 1.0, 0.005) = 0.075;
uniform float contact_strength : hint_range(0.0, 1.0, 0.01) = 0.92;
uniform float shore_ripple_strength : hint_range(0.0, 0.6, 0.01) = 0.46;
uniform float shore_ripple_reach : hint_range(4.0, 96.0, 1.0) = 36.0;
uniform float shore_ripple_speed : hint_range(0.0, 1.0, 0.01) = 0.18;
uniform float surface_alpha : hint_range(0.1, 1.0, 0.01) = 0.76;
uniform float rain_intensity : hint_range(0.0, 2.0, 0.01) = 0.0;
uniform float rain_ripple_density : hint_range(0.1, 6.0, 0.01) = 1.35;
uniform float rain_ripple_size : hint_range(0.04, 1.6, 0.01) = 0.19;
uniform float rain_ripple_speed : hint_range(0.05, 6.0, 0.01) = 1.64;
uniform float rain_ripple_brightness : hint_range(0.0, 3.0, 0.01) = 0.31;

uniform vec2 water_center = vec2(0.0);
uniform float water_radius : hint_range(4.0, 40.0, 0.1) = 16.0;
uniform float edge_fade : hint_range(0.1, 4.0, 0.1) = 1.2;

varying vec3 world_position;
varying float water_view_depth;

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

float rain_ripple_layer(
	vec2 position,
	float layer_time,
	float cell_size,
	float ripple_size,
	float seed,
	float activation)
{
	float safe_cell_size = max(cell_size, 0.01);
	vec2 cell = floor(position / safe_cell_size);
	vec2 base_random = hash_position(
		cell + vec2(seed, seed * 1.73));
	float speed_variation = mix(
		0.82,
		1.18,
		fract(base_random.x * 0.73 + base_random.y * 1.37));
	float local_time = layer_time * speed_variation + base_random.y;
	float cycle = floor(local_time);
	float age = fract(local_time);
	vec2 cycle_seed = vec2(
		seed + cycle * 7.13,
		seed * 1.91 - cycle * 5.37);
	vec2 position_random = hash_position(
		cell + cycle_seed + vec2(13.7, 29.1));
	float gate_random = fract(
		position_random.x + position_random.y * 0.754877666);
	float activation_limit = clamp(activation, 0.0, 0.92);
	float drop_gate = 1.0 - smoothstep(
		activation_limit,
		activation_limit + 0.04,
		gate_random);
	vec2 impact_random = vec2(
		position_random.y,
		fract(
			position_random.x * 0.618033989 +
			position_random.y * 1.324717957));
	vec2 impact_position = (
		cell + mix(vec2(0.34), vec2(0.66), impact_random)
	) * safe_cell_size;
	vec2 impact_delta = position - impact_position;
	float impact_distance = length(impact_delta);
	float size_random = fract(
		position_random.x * 1.37 + position_random.y * 2.11);
	float varied_size = ripple_size * mix(0.74, 1.22, size_random);
	vec2 normalized_delta = impact_delta / max(varied_size, 0.001);
	float shape_phase = position_random.x * 6.283185;
	float contour_wave = sin(
		normalized_delta.x * 6.7 +
		normalized_delta.y * 2.3 +
		shape_phase + age * 2.4);
	contour_wave += sin(
		normalized_delta.y * 7.9 -
		normalized_delta.x * 1.8 -
		shape_phase * 1.37 - age * 1.7) * 0.45;
	float shaped_distance = impact_distance + contour_wave * (
		varied_size * 0.028 * smoothstep(0.04, 0.24, age));
	float expansion = 1.0 - pow(1.0 - age, 2.0);
	float radius = expansion * varied_size;
	float thickness = mix(
		varied_size * 0.095,
		varied_size * 0.032,
		age) * mix(0.82, 1.12, size_random);
	float primary_ring = 1.0 - smoothstep(
		thickness,
		thickness + varied_size * 0.038,
		abs(shaped_distance - radius));
	float primary_life = smoothstep(0.015, 0.09, age) * (
		1.0 - smoothstep(0.62, 0.98, age));
	float arc_variation = mix(
		0.76,
		1.0,
		smoothstep(-1.1, 1.1, contour_wave));
	primary_ring *= primary_life * arc_variation;

	float secondary_age = clamp((age - 0.14) / 0.86, 0.0, 1.0);
	float secondary_expansion = 1.0 - pow(
		1.0 - secondary_age,
		2.0);
	float secondary_radius = secondary_expansion * varied_size * 0.64;
	float secondary_thickness = thickness * 0.68;
	float secondary_ring = 1.0 - smoothstep(
		secondary_thickness,
		secondary_thickness + varied_size * 0.030,
		abs(shaped_distance - secondary_radius));
	float secondary_life = smoothstep(
		0.02,
		0.11,
		secondary_age) * (
		1.0 - smoothstep(0.52, 0.94, secondary_age));
	secondary_ring *= secondary_life * arc_variation * 0.38;

	float impact_core = 1.0 - smoothstep(
		varied_size * 0.015,
		varied_size * 0.085,
		impact_distance);
	float impact_halo = 1.0 - smoothstep(
		varied_size * 0.08,
		varied_size * 0.21,
		impact_distance);
	float impact = max(impact_core, impact_halo * 0.28);
	impact *= 1.0 - smoothstep(0.015, 0.14, age);
	float drop_strength = mix(
		0.72,
		1.0,
		fract(position_random.x * 2.47 + position_random.y));
	return max(
		max(primary_ring, secondary_ring),
		impact * 0.68) * drop_gate * drop_strength;
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

vec3 scene_world_position_at_uv(
	vec2 screen_uv,
	mat4 inverse_projection,
	mat4 inverse_view)
{
	vec2 clamped_uv = clamp(screen_uv, vec2(0.001), vec2(0.999));
	float scene_depth = textureLod(depth_texture, clamped_uv, 0.0).r;
	#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(clamped_uv, scene_depth) * 2.0 - 1.0;
	#else
	vec3 ndc = vec3(clamped_uv * 2.0 - 1.0, scene_depth);
	#endif
	vec4 scene_view_position = inverse_projection * vec4(ndc, 1.0);
	scene_view_position.xyz /= scene_view_position.w;
	return (inverse_view * vec4(scene_view_position.xyz, 1.0)).xyz;
}

float shoreline_contact_at_uv(
	vec2 screen_uv,
	float water_height,
	mat4 inverse_projection,
	mat4 inverse_view)
{
	vec3 sampled_world_position = scene_world_position_at_uv(
		screen_uv,
		inverse_projection,
		inverse_view);
	float sampled_gap = water_height - sampled_world_position.y;
	float sampled_contact_width = contact_width * 0.18;
	float sampled_contact_softness = contact_softness * 0.12;
	float sampled_contact = 1.0 - smoothstep(
		sampled_contact_width,
		sampled_contact_width + sampled_contact_softness,
		sampled_gap);
	return sampled_contact * step(0.0, sampled_gap);
}

float shoreline_ring(
	vec2 screen_uv,
	float radius_pixels,
	float water_height,
	vec2 viewport_size,
	mat4 inverse_projection,
	mat4 inverse_view)
{
	vec2 radius_uv = vec2(radius_pixels) / viewport_size;
	vec2 diagonal_uv = radius_uv * 0.707107;
	float ring = 0.0;
	ring = max(ring, shoreline_contact_at_uv(
		screen_uv + vec2(radius_uv.x, 0.0),
		water_height,
		inverse_projection,
		inverse_view));
	ring = max(ring, shoreline_contact_at_uv(
		screen_uv - vec2(radius_uv.x, 0.0),
		water_height,
		inverse_projection,
		inverse_view));
	ring = max(ring, shoreline_contact_at_uv(
		screen_uv + vec2(0.0, radius_uv.y),
		water_height,
		inverse_projection,
		inverse_view));
	ring = max(ring, shoreline_contact_at_uv(
		screen_uv - vec2(0.0, radius_uv.y),
		water_height,
		inverse_projection,
		inverse_view));
	ring = max(ring, shoreline_contact_at_uv(
		screen_uv + diagonal_uv,
		water_height,
		inverse_projection,
		inverse_view));
	ring = max(ring, shoreline_contact_at_uv(
		screen_uv - diagonal_uv,
		water_height,
		inverse_projection,
		inverse_view));
	ring = max(ring, shoreline_contact_at_uv(
		screen_uv + vec2(diagonal_uv.x, -diagonal_uv.y),
		water_height,
		inverse_projection,
		inverse_view));
	ring = max(ring, shoreline_contact_at_uv(
		screen_uv + vec2(-diagonal_uv.x, diagonal_uv.y),
		water_height,
		inverse_projection,
		inverse_view));
	return ring;
}

void vertex()
{
	vec3 base_world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float wave = sin(
		(base_world_position.x + base_world_position.z) * 0.62 +
		TIME * animation_speed * 5.0);
	wave += sin(
		base_world_position.x * 0.37 -
		base_world_position.z * 0.51 -
		TIME * animation_speed * 3.4) * 0.55;
	VERTEX.y += wave * wave_height;
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	water_view_depth = -(MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).z;
}

void fragment()
{
	vec2 relative_position = world_position.xz - water_center;
	float radial_distance = length(relative_position);
	float outer_alpha = 1.0 - smoothstep(
		water_radius - edge_fade,
		water_radius,
		radial_distance);

	if (outer_alpha <= 0.01)
		discard;

	float time_value = TIME * animation_speed;
	vec2 cell_position = relative_position * cell_scale;
	cell_position += vec2(
		sin(relative_position.y * 0.16 + time_value),
		cos(relative_position.x * 0.14 - time_value)) * 0.12;
	cell_position = wobble_cell_position(
		cell_position,
		time_value,
		line_wobble_strength,
		line_distortion_scale);
	vec4 cell_data = cellular_metrics(cell_position, time_value);
	float line = 1.0 - smoothstep(
		line_width,
		line_width + line_softness,
		cell_data.x);
	float rounded_lobe = rounded_line_lobe(
		cell_data.x,
		cell_data.w,
		cell_data.z);
	line = max(line, rounded_lobe);
	float cell_tone = smoothstep(0.18, 0.85, cell_data.y);
	float color_motion = sin(
		world_position.x * 0.18 +
		world_position.z * 0.13 +
		time_value) * 0.5 + 0.5;
	vec3 deep_water_color = surface_color.rgb * vec3(
		0.535714,
		0.652174,
		0.703704);
	vec3 shallow_water_color = min(
		surface_color.rgb * vec3(1.571429, 1.304348, 1.222222),
		vec3(1.0));
	vec3 water_color = mix(
		deep_water_color,
		surface_color.rgb,
		cell_tone * 0.48);
	water_color = mix(
		water_color,
		shallow_water_color,
		color_motion * 0.12);

	float refraction_edge = 1.0 - smoothstep(
		0.0,
		max(refraction_edge_reach, 0.001),
		cell_data.x);
	refraction_edge = max(refraction_edge, rounded_lobe);
	refraction_edge = pow(
		max(refraction_edge, 0.0),
		max(refraction_edge_falloff, 0.01));
	float refraction_mask = mix(
		clamp(refraction_center_strength, 0.0, 1.0),
		1.0,
		refraction_edge);
	vec2 refraction_position = relative_position * refraction_scale;
	float refraction_time = TIME * refraction_speed;
	float refraction_seed = cell_data.z * 6.283185;
	vec2 refraction_wave = vec2(
		sin(
			refraction_position.y * 1.41 +
			refraction_position.x * 0.37 +
			refraction_time + refraction_seed),
		cos(
			refraction_position.x * 1.33 -
			refraction_position.y * 0.42 -
			refraction_time * 0.87 - refraction_seed * 0.73));
	float refraction_detail = sin(
		dot(refraction_position, vec2(2.17, -1.83)) +
		refraction_time * 1.31 + refraction_seed * 1.37);
	vec2 refraction_direction = refraction_wave * 0.82 + vec2(
		refraction_detail,
		-refraction_detail) * 0.18;
	vec2 refracted_uv = clamp(
		SCREEN_UV +
		refraction_direction * refraction_strength * refraction_mask,
		vec2(0.002),
		vec2(0.998));
	vec3 base_scene_world_position = scene_world_position_at_uv(
		SCREEN_UV,
		INV_PROJECTION_MATRIX,
		INV_VIEW_MATRIX);
	vec3 refracted_scene_world_position = scene_world_position_at_uv(
		refracted_uv,
		INV_PROJECTION_MATRIX,
		INV_VIEW_MATRIX);
	float refraction_height_jump = max(
		refracted_scene_world_position.y - base_scene_world_position.y,
		0.0);
	float refraction_occlusion_guard = 1.0 - smoothstep(
		0.015,
		0.060,
		refraction_height_jump);
	refracted_uv = mix(
		SCREEN_UV,
		refracted_uv,
		refraction_occlusion_guard);
	vec3 refracted_scene_color = textureLod(
		screen_texture,
		refracted_uv,
		0.0).rgb;
	water_color = mix(
		water_color,
		refracted_scene_color,
		clamp(
			refraction_mix * refraction_mask,
			0.0,
			1.0));

	float scene_depth_sample = textureLod(
		depth_texture,
		SCREEN_UV,
		0.0).r;
	#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(SCREEN_UV, scene_depth_sample) * 2.0 - 1.0;
	#else
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, scene_depth_sample);
	#endif
	vec4 scene_view_position = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	scene_view_position.xyz /= scene_view_position.w;
	float scene_view_depth = -scene_view_position.z;
	vec4 scene_world_position = INV_VIEW_MATRIX * vec4(
		scene_view_position.xyz,
		1.0);
	float vertical_gap = world_position.y - scene_world_position.y;
	float geometry_is_behind_water = step(
		water_view_depth,
		scene_view_depth);
	float geometry_is_below_surface = step(0.0, vertical_gap);
	float contact_wobble = (
		sin(world_position.x * 3.70 + time_value * 2.30) +
		cos(world_position.z * 4.10 - time_value * 1.90)
	) * contact_width * 0.03;
	float contact_gap = vertical_gap + contact_wobble;
	float thin_contact_width = contact_width * 0.18;
	float thin_contact_softness = contact_softness * 0.12;
	float contact = 1.0 - smoothstep(
		thin_contact_width,
		thin_contact_width + thin_contact_softness,
		contact_gap);
	float contact_geometry = (
		geometry_is_behind_water * geometry_is_below_surface);
	contact *= contact_geometry;
	float foam_edge = 1.0 - smoothstep(
		contact_width * 0.01,
		contact_width * 0.07,
		max(contact_gap, 0.0));
	foam_edge *= contact_geometry;

	float ripple_time = TIME * shore_ripple_speed;
	float ripple_phase_a = fract(ripple_time);
	float ripple_phase_b = fract(ripple_time + 0.333333);
	float ripple_phase_c = fract(ripple_time + 0.666667);
	float ripple_travel_a = min(ripple_phase_a / 0.68, 1.0);
	float ripple_travel_b = min(ripple_phase_b / 0.68, 1.0);
	float ripple_travel_c = min(ripple_phase_c / 0.68, 1.0);
	float ripple_radius_wobble = (
		sin(
			world_position.x * 2.35 +
			world_position.z * 1.90 +
			ripple_time * 1.20) +
		sin(
			world_position.x * 4.10 -
			world_position.z * 2.70 -
			ripple_time * 0.70) * 0.45
	) * 0.75;
	float ripple_radius_a = mix(
		2.0,
		shore_ripple_reach,
		ripple_travel_a) + ripple_radius_wobble;
	float ripple_radius_b = mix(
		2.0,
		shore_ripple_reach,
		ripple_travel_b) - ripple_radius_wobble * 0.65;
	float ripple_radius_c = mix(
		2.0,
		shore_ripple_reach,
		ripple_travel_c) + ripple_radius_wobble * 0.40;
	float ripple_a = shoreline_ring(
		SCREEN_UV,
		ripple_radius_a,
		world_position.y,
		VIEWPORT_SIZE,
		INV_PROJECTION_MATRIX,
		INV_VIEW_MATRIX);
	float ripple_b = shoreline_ring(
		SCREEN_UV,
		ripple_radius_b,
		world_position.y,
		VIEWPORT_SIZE,
		INV_PROJECTION_MATRIX,
		INV_VIEW_MATRIX);
	float ripple_c = shoreline_ring(
		SCREEN_UV,
		ripple_radius_c,
		world_position.y,
		VIEWPORT_SIZE,
		INV_PROJECTION_MATRIX,
		INV_VIEW_MATRIX);
	ripple_a *= smoothstep(0.02, 0.12, ripple_phase_a) * (
		1.0 - smoothstep(0.54, 0.68, ripple_phase_a));
	ripple_b *= smoothstep(0.02, 0.12, ripple_phase_b) * (
		1.0 - smoothstep(0.54, 0.68, ripple_phase_b));
	ripple_c *= smoothstep(0.02, 0.12, ripple_phase_c) * (
		1.0 - smoothstep(0.54, 0.68, ripple_phase_c));
	float broad_breakup = sin(
		world_position.x * 1.70 +
		world_position.z * 1.30 +
		ripple_time * 0.80) * 0.5 + 0.5;
	float fine_breakup = sin(
		world_position.x * 5.10 -
		world_position.z * 4.30 -
		ripple_time * 1.10) * 0.5 + 0.5;
	float ripple_breakup = smoothstep(
		0.20,
		0.68,
		broad_breakup * 0.68 + fine_breakup * 0.32);
	ripple_breakup = mix(0.45, 1.0, ripple_breakup);
	float shore_ripple = max(max(ripple_a, ripple_b), ripple_c);
	shore_ripple *= shore_ripple_strength * ripple_breakup;

	float combined_line = max(
		max(line * line_strength, contact * contact_strength),
		shore_ripple);
	water_color = mix(water_color, line_color.rgb, combined_line);
	float ripple_foam = smoothstep(0.34, 0.45, shore_ripple) * 0.20;
	float foam_highlight = max(foam_edge * 0.96, ripple_foam);
	water_color = mix(
		water_color,
		foam_color.rgb,
		clamp(foam_highlight, 0.0, 1.0));

	float rain_amount = clamp(rain_intensity, 0.0, 2.0);
	float rain_level = min(rain_amount, 1.0);
	float rain_highlight = 0.0;
	if (rain_level > 0.001)
	{
		float rain_density = max(
			rain_ripple_density,
			0.1);
		float rain_time = TIME * rain_ripple_speed;
		float rain_layer_a = rain_ripple_layer(
			relative_position,
			rain_time,
			1.90 / sqrt(rain_density),
			rain_ripple_size,
			3.17,
			(0.025 + rain_level * 0.085) * rain_density);
		float heavy_rain_mix = smoothstep(0.52, 0.88, rain_level);
		float rain_layer_b = rain_ripple_layer(
			relative_position,
			rain_time * 1.31 + 17.0,
			1.45 / sqrt(rain_density),
			rain_ripple_size * 0.82,
			8.43,
			(0.02 + rain_level * 0.08) * rain_density);
		float rain_ripple = max(
			rain_layer_a,
			rain_layer_b * heavy_rain_mix);
		rain_ripple *= rain_level;
		rain_highlight = clamp(
			rain_ripple * rain_ripple_brightness,
			0.0,
			1.0);
		water_color = mix(
			water_color,
			rain_ripple_color.rgb,
			rain_highlight);
	}

	ALBEDO = water_color;
	ROUGHNESS = 0.78;
	SPECULAR = 0.12;
	float line_emission = max(line * 0.035, contact * 0.08);
	EMISSION = line_color.rgb * line_emission;
	EMISSION += foam_color.rgb * (
		foam_edge * 0.055 +
		ripple_foam * 0.025);
	EMISSION += rain_ripple_color.rgb * rain_highlight * 0.08;
	ALPHA = outer_alpha * surface_alpha;
}
