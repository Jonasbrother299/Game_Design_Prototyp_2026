shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;

uniform float intensity : hint_range(0.0, 1.0) = 0.80;
uniform float drop_scale : hint_range(5.0, 100.0) = 18.0;
uniform float drop_density : hint_range(0.0, 1.0) = 0.18;
uniform float speed : hint_range(0.0, 2.0) = 0.13;

uniform float drop_radius : hint_range(0.01, 0.30) = 0.15;
uniform float drop_stretch : hint_range(1.0, 4.0) = 2.35;
uniform float drift_strength : hint_range(0.0, 0.15) = 0.035;
uniform float moving_drop_chance : hint_range(0.0, 1.0) = 1.00;
uniform float slide_duration : hint_range(0.05, 0.60) = 0.24;
uniform float fast_drop_chance : hint_range(0.0, 0.5) = 0.18;
uniform float fast_speed_multiplier : hint_range(1.0, 5.0) = 1.7;
uniform float fast_speed_variation : hint_range(0.0, 0.5) = 0.22;
uniform float fast_column_count : hint_range(4.0, 24.0) = 11.0;
uniform float fast_width_scale : hint_range(0.20, 1.0) = 0.52;
uniform float fast_width_variation : hint_range(0.0, 0.5) = 0.18;
uniform float fast_curve_strength : hint_range(0.0, 0.30) = 0.11;
uniform float life_fade_width : hint_range(0.05, 0.35) = 0.20;
uniform float trail_chance : hint_range(0.0, 1.0) = 0.30;
uniform float trail_length : hint_range(0.05, 0.60) = 0.28;
uniform float trail_strength : hint_range(0.0, 1.0) = 0.28;

uniform float distortion_strength : hint_range(0.0, 0.06) = 0.04;
uniform float refraction_mix : hint_range(0.0, 1.0) = 0.92;
uniform float blur_lod : hint_range(0.0, 2.0) = 0.55;

uniform float highlight_strength : hint_range(0.0, 1.0) = 0.20;
uniform float ring_strength : hint_range(0.0, 1.0) = 0.12;

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
	float screen_aspect = SCREEN_PIXEL_SIZE.y / max(SCREEN_PIXEL_SIZE.x, 0.000001);

	effect_uv.x += sin(effect_uv.y * 19.0 + TIME * 0.055) * 0.010;
	effect_uv.y += sin(effect_uv.x * 13.0 + TIME * 0.040) * 0.006;

	vec3 base = textureLod(screen_texture, screen_uv, 0.0).rgb;
	vec2 grid_uv = vec2(effect_uv.x * screen_aspect, effect_uv.y) * drop_scale;
	vec2 cell_id = floor(grid_uv);
	vec2 cell_uv = fract(grid_uv) - 0.5;

	vec2 rnd = hash22(cell_id);
	float speed_random = hash21(cell_id + 43.21);
	float phase_random = hash21(cell_id + 91.73);
	float moving_random = hash21(cell_id + 173.51);
	float trail_random = hash21(cell_id + 211.93);
	float slide_random = hash21(cell_id + 251.47);
	float moving_drop = step(
		1.0 - moving_drop_chance,
		moving_random);
	float local_speed = mix(0.72, 1.18, speed_random) * speed;
	float cycle_phase = fract(TIME * local_speed + phase_random);
	float slide_start = mix(0.18, 0.46, slide_random);
	float local_slide_duration = slide_duration * mix(
		0.92,
		1.12,
		slide_random);
	float slide_end = min(
		slide_start + local_slide_duration,
		0.86);
	float slide_progress = smoothstep(
		slide_start,
		slide_end,
		cycle_phase);
	float slide_visibility = smoothstep(
		0.0,
		life_fade_width,
		cycle_phase);
	slide_visibility *= 1.0 - smoothstep(
		slide_end,
		min(slide_end + life_fade_width, 0.98),
		cycle_phase);
	float life_fade = mix(
		1.0,
		slide_visibility,
		moving_drop);
	float is_sliding = smoothstep(
		slide_start - 0.025,
		slide_start + 0.015,
		cycle_phase);
	is_sliding *= 1.0 - smoothstep(
		slide_end - 0.015,
		slide_end + 0.045,
		cycle_phase);
	is_sliding *= moving_drop;

	float horizontal_drift = sin(
		slide_progress * 6.2831853 + rnd.y * 6.2831853);
	float x_center = (rnd.x - 0.5) * 0.42;
	x_center += horizontal_drift * drift_strength * moving_drop;
	float stationary_y = (rnd.y - 0.5) * 0.76;
	float moving_y = mix(-0.72, 0.72, slide_progress);
	float y_center = mix(stationary_y, moving_y, moving_drop);
	vec2 p = cell_uv - vec2(x_center, y_center);

	float stretch_random = mix(0.84, 1.16, rnd.y);
	float stretch = drop_stretch * stretch_random;
	float vertical_position = clamp(
		p.y / max(drop_radius * stretch, 0.0001) * 0.5 + 0.5,
		0.0,
		1.0);
	float width_scale = mix(
		0.58,
		1.08,
		smoothstep(0.0, 1.0, vertical_position));
	vec2 drop_shape = vec2(p.x / width_scale, p.y / stretch);
	float dist = length(drop_shape);

	float drop = 1.0 - smoothstep(
		drop_radius * 0.58,
		drop_radius,
		dist);
	float ring = smoothstep(
		drop_radius * 0.68,
		drop_radius * 0.93,
		dist);
	ring *= 1.0 - smoothstep(
		drop_radius * 0.93,
		drop_radius * 1.20,
		dist);

	float local_trail_length = trail_length * mix(
		0.72,
		1.18,
		hash21(cell_id + 283.19));
	float trail_progress = clamp(
		-p.y / max(local_trail_length, 0.0001),
		0.0,
		1.0);
	float trail_vertical = step(-local_trail_length, p.y);
	trail_vertical *= 1.0 - step(0.0, p.y);
	float trail_center = sin(
		trail_progress * 4.2 + rnd.y * 6.2831853);
	trail_center *= drift_strength * 0.42;
	float trail_width = drop_radius * mix(
		0.26,
		0.10,
		trail_progress);
	float trail = 1.0 - smoothstep(
		trail_width * 0.42,
		trail_width,
		abs(p.x - trail_center));
	trail *= trail_vertical;
	trail *= 1.0 - smoothstep(0.68, 1.0, trail_progress);
	trail *= step(1.0 - trail_chance, trail_random);
	trail *= is_sliding;
	float trail_surface = trail * trail_strength;
	float visibility_random = hash21(cell_id + 11.2);
	float visible = step(1.0 - drop_density, visibility_random);
	float main_visibility = life_fade * visible;

	float fast_columns = max(fast_column_count, 1.0);
	float fast_grid_x = effect_uv.x * screen_aspect * fast_columns;
	float fast_column_id = floor(fast_grid_x);
	float fast_local_x = fract(fast_grid_x) - 0.5;
	float fast_phase_offset = hash21(vec2(fast_column_id, 7.13));
	float fast_speed_factor = mix(
		1.0 - fast_speed_variation,
		1.0 + fast_speed_variation,
		hash21(vec2(fast_column_id, 13.67)));
	float fast_time = TIME * speed * fast_speed_multiplier;
	fast_time *= fast_speed_factor;
	fast_time += fast_phase_offset * 3.0;
	float fast_cycle = floor(fast_time);
	float fast_travel = fract(fast_time);
	vec2 fast_seed = vec2(fast_column_id, fast_cycle);
	float fast_visible = step(
		1.0 - fast_drop_chance,
		hash21(fast_seed + 19.71));
	float fast_life = smoothstep(0.0, 0.07, fast_travel);
	fast_life *= 1.0 - smoothstep(0.90, 1.0, fast_travel);
	float fast_presence = fast_visible * fast_life;

	float fast_x_center = (
		hash21(fast_seed + 37.29) - 0.5) * 0.56;
	float fast_curve_frequency = mix(
		0.35,
		1.85,
		hash21(fast_seed + 48.17));
	float fast_curve_amplitude = fast_curve_strength * mix(
		0.22,
		1.0,
		hash21(fast_seed + 59.31));
	float fast_curve_direction = mix(
		-1.0,
		1.0,
		step(0.5, hash21(fast_seed + 64.73)));
	float fast_curve_phase = hash21(fast_seed + 71.09) * 6.2831853;
	fast_x_center += sin(
		fast_travel * 6.2831853 * fast_curve_frequency
		+ fast_curve_phase)
		* fast_curve_amplitude
		* fast_curve_direction;
	float fast_secondary_frequency = mix(
		1.35,
		3.10,
		hash21(fast_seed + 86.47));
	fast_x_center += sin(
		fast_travel * 6.2831853 * fast_secondary_frequency
		+ hash21(fast_seed + 94.63) * 6.2831853)
		* fast_curve_amplitude
		* mix(0.12, 0.34, hash21(fast_seed + 103.21));
	float fast_side_slope = (
		hash21(fast_seed + 112.57) - 0.5)
		* fast_curve_strength
		* 1.35;
	fast_x_center += (fast_travel - 0.5) * fast_side_slope;
	float fast_y_center = mix(
		-0.08,
		1.08,
		smoothstep(0.0, 1.0, fast_travel));
	vec2 fast_p = vec2(
		fast_local_x - fast_x_center,
		(effect_uv.y - fast_y_center) * fast_columns);
	float fast_stretch = drop_stretch * mix(
		1.15,
		1.45,
		hash21(fast_seed + 68.41));
	float fast_cycle_width = fast_width_scale * mix(
		1.0 - fast_width_variation,
		1.0 + fast_width_variation,
		hash21(fast_seed + 76.93));
	vec2 fast_shape = vec2(
		fast_p.x / max(fast_cycle_width, 0.01),
		fast_p.y / fast_stretch);
	float fast_dist = length(fast_shape);
	float fast_drop = 1.0 - smoothstep(
		drop_radius * 0.54,
		drop_radius,
		fast_dist);
	float fast_ring = smoothstep(
		drop_radius * 0.68,
		drop_radius * 0.93,
		fast_dist);
	fast_ring *= 1.0 - smoothstep(
		drop_radius * 0.93,
		drop_radius * 1.18,
		fast_dist);

	float fast_trail_length = trail_length * 2.60;
	float fast_trail_progress = clamp(
		-fast_p.y / max(fast_trail_length, 0.0001),
		0.0,
		1.0);
	float fast_trail_vertical = step(
		-fast_trail_length,
		fast_p.y);
	fast_trail_vertical *= 1.0 - step(0.0, fast_p.y);
	float fast_trail_center = sin(
		fast_trail_progress * 3.8
		+ hash21(fast_seed + 83.17) * 6.2831853);
	fast_trail_center *= drift_strength * 0.40;
	float fast_trail_width = drop_radius * mix(
		0.24,
		0.08,
		fast_trail_progress) * fast_cycle_width;
	float fast_trail = 1.0 - smoothstep(
		fast_trail_width * 0.40,
		fast_trail_width,
		abs(fast_p.x - fast_trail_center));
	fast_trail *= fast_trail_vertical;
	fast_trail *= 1.0 - smoothstep(
		0.72,
		1.0,
		fast_trail_progress);
	fast_trail *= step(
		1.0 - trail_chance,
		hash21(fast_seed + 101.53));
	float fast_trail_surface = fast_trail * trail_strength;

	float main_surface = max(drop, trail_surface) * main_visibility;
	float fast_surface = max(
		fast_drop,
		fast_trail_surface) * fast_presence;
	float surface = max(main_surface, fast_surface);

	vec2 drop_normal = normalize(drop_shape + vec2(0.0001));
	vec2 trail_normal = normalize(
		vec2(p.x - trail_center, -trail_width * 0.32)
		+ vec2(0.0001));
	vec2 main_normal = normalize(mix(
		drop_normal,
		trail_normal,
		step(drop, trail_surface)));
	vec2 fast_drop_normal = normalize(fast_shape + vec2(0.0001));
	vec2 fast_trail_normal = normalize(
		vec2(
			fast_p.x - fast_trail_center,
			-fast_trail_width * 0.32)
		+ vec2(0.0001));
	vec2 fast_normal = normalize(mix(
		fast_drop_normal,
		fast_trail_normal,
		step(fast_drop, fast_trail_surface)));
	vec2 normal = normalize(mix(
		main_normal,
		fast_normal,
		step(main_surface, fast_surface)));
	vec2 distortion_scale = vec2(
		distortion_strength / screen_aspect,
		distortion_strength);
	vec2 distortion = normal * distortion_scale * surface * intensity;
	vec2 refracted_uv = clamp(
		screen_uv - distortion,
		vec2(0.0),
		vec2(1.0));
	vec3 refracted = textureLod(
		screen_texture,
		refracted_uv,
		blur_lod).rgb;

	float main_highlight = pow(
		clamp(1.0 - dist / drop_radius, 0.0, 1.0),
		3.0);
	main_highlight *= highlight_strength * drop * intensity;
	main_highlight *= main_visibility;
	float fast_highlight = pow(
		clamp(1.0 - fast_dist / drop_radius, 0.0, 1.0),
		3.0);
	fast_highlight *= highlight_strength * fast_drop * intensity;
	fast_highlight *= fast_presence;

	vec3 color = mix(base, refracted, surface * refraction_mix);
	color += vec3(main_highlight + fast_highlight);
	color += vec3(
		(ring * main_visibility + fast_ring * fast_presence)
		* ring_strength * intensity);

	float alpha = (
		drop * 0.88
		+ ring * 0.28
		+ trail_surface * 0.55) * main_visibility;
	alpha += (
		fast_drop * 0.88
		+ fast_ring * 0.28
		+ fast_trail_surface * 0.55) * fast_presence;
	alpha *= intensity;
	COLOR = vec4(color, clamp(alpha, 0.0, 1.0));
}
