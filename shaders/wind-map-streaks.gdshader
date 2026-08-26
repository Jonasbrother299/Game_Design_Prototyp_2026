shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, shadows_disabled;

uniform float animation_time = 0.0;
uniform float event_blend : hint_range(0.0, 1.0) = 0.0;
uniform float travel_distance : hint_range(4.0, 60.0) = 30.0;
uniform float min_length : hint_range(0.2, 8.0) = 1.15;
uniform float max_length : hint_range(0.2, 10.0) = 3.4;
uniform float min_width : hint_range(0.005, 0.3) = 0.025;
uniform float max_width : hint_range(0.005, 0.4) = 0.075;
uniform float min_speed : hint_range(0.2, 30.0) = 7.0;
uniform float max_speed : hint_range(0.2, 40.0) = 12.5;
uniform float wave_amplitude : hint_range(0.0, 0.8) = 0.075;
uniform float wave_frequency : hint_range(0.1, 8.0) = 1.45;
uniform float loop_chance : hint_range(0.0, 1.0) = 0.12;
uniform float loop_radius : hint_range(0.05, 1.5) = 0.38;
uniform float loop_turns : hint_range(1.0, 2.0, 1.0) = 1.0;
uniform float loop_position : hint_range(0.15, 0.75) = 0.48;
uniform float loop_position_variation : hint_range(0.0, 0.25) = 0.16;
uniform float vertical_lift : hint_range(0.0, 0.35) = 0.045;
uniform float broken_streak_chance : hint_range(0.0, 1.0) = 0.16;
uniform float broken_streak_strength : hint_range(0.0, 1.0) = 0.48;
uniform float normal_appearance_chance : hint_range(0.0, 1.0) = 0.08;
uniform float event_appearance_chance : hint_range(0.0, 1.0) = 0.46;
uniform float appearance_fade_softness : hint_range(0.001, 0.2) = 0.035;
uniform float visible_travel_fraction : hint_range(0.08, 1.0) = 0.32;
uniform float cycle_fade : hint_range(0.01, 0.35) = 0.12;
uniform vec4 streak_color : source_color = vec4(0.89, 0.96, 1.0, 1.0);
uniform float base_alpha : hint_range(0.0, 1.0) = 0.58;
uniform float emission_strength : hint_range(0.0, 4.0) = 0.68;
uniform float event_brightness_multiplier : hint_range(0.2, 2.5) = 1.22;
uniform float end_fade : hint_range(0.01, 0.48) = 0.18;
uniform float center_highlight : hint_range(0.0, 0.5) = 0.16;

varying float streak_visibility;
varying float streak_detail;

float hash11(float value) {
	return fract(sin(value * 127.1 + 311.7) * 43758.5453123);
}

void vertex() {
	float phase_seed = INSTANCE_CUSTOM.r;
	float speed_seed = INSTANCE_CUSTOM.g;
	float shape_seed = INSTANCE_CUSTOM.b;
	float detail_seed = INSTANCE_CUSTOM.a;
	float speed = mix(min_speed, max_speed, speed_seed);
	float safe_travel = max(travel_distance, 0.001);
	float travel_cycles = animation_time * speed / safe_travel + phase_seed;
	float cycle_index = floor(travel_cycles);
	float progress = fract(travel_cycles);
	float cycle_seed = hash11(cycle_index + shape_seed * 91.73);
	float length_seed = hash11(shape_seed * 73.17 + cycle_index * 0.71);
	float width_seed = hash11(detail_seed * 53.91 + cycle_index * 1.17);
	float streak_length = mix(min_length, max_length, length_seed);
	float streak_width = mix(min_width, max_width, width_seed);
	float along = (UV.x - 0.5) * streak_length;
	float side = (UV.y - 0.5) * streak_width;
	float moving_center = (progress - 0.5) * safe_travel;

	float loop_seed = hash11(cycle_index * 2.31 + detail_seed * 117.7);
	float loop_enabled = 1.0 - step(loop_chance, loop_seed);
	float complete_turns = max(1.0, floor(loop_turns + 0.5));
	float maximum_loop_radius = safe_travel * 0.55 /
		(6.28318530718 * complete_turns);
	float effective_loop_radius = min(loop_radius, maximum_loop_radius);
	float loop_path_length = 6.28318530718 * effective_loop_radius *
		complete_turns;
	float position_seed = hash11(
		shape_seed * 33.1 + cycle_index * 1.43);
	float requested_loop_progress = loop_position +
		(position_seed * 2.0 - 1.0) * loop_position_variation;
	float loop_end_margin = streak_length * 0.5 / safe_travel + 0.06;
	float latest_loop_progress = max(
		0.12,
		1.0 - loop_path_length / safe_travel - loop_end_margin);
	float loop_entry_progress = clamp(
		requested_loop_progress,
		0.12,
		latest_loop_progress);
	float loop_entry_distance = (loop_entry_progress - 0.5) * safe_travel;
	float loop_exit_distance = loop_entry_distance + loop_path_length;

	float appearance_chance = mix(
		normal_appearance_chance,
		event_appearance_chance,
		event_blend);
	float appearance_softness = max(appearance_fade_softness, 0.001);
	float appears = step(0.0001, appearance_chance) *
		(1.0 - smoothstep(
			appearance_chance - appearance_softness,
			appearance_chance + appearance_softness,
			cycle_seed));
	float life_fraction = clamp(visible_travel_fraction, 0.08, 1.0);
	float required_loop_life = min(
		0.92,
		(loop_path_length + streak_length * 1.6) / safe_travel);
	float cycle_life_fraction = mix(
		life_fraction,
		max(life_fraction, required_loop_life),
		loop_enabled);
	float random_life_start = hash11(
		cycle_index * 4.17 + detail_seed * 163.9) *
		(1.0 - cycle_life_fraction);
	float loop_center_progress = loop_entry_progress +
		loop_path_length * 0.5 / safe_travel;
	float loop_life_start = clamp(
		loop_center_progress - cycle_life_fraction * 0.5,
		0.0,
		1.0 - cycle_life_fraction);
	float life_start = mix(
		random_life_start,
		loop_life_start,
		loop_enabled);
	float life_progress = (progress - life_start) / cycle_life_fraction;
	float inside_lifetime = step(0.0, life_progress) *
		(1.0 - step(1.0, life_progress));
	float safe_cycle_fade = min(cycle_fade, 0.49);
	float cycle_visibility = inside_lifetime *
		smoothstep(0.0, safe_cycle_fade, life_progress) *
		(1.0 - smoothstep(
			1.0 - safe_cycle_fade,
			1.0,
			life_progress));

	float wave_phase = shape_seed * 6.28318530718 + cycle_index * 0.37;
	float path_distance = moving_center + along;
	float path_forward = path_distance;
	float path_height = 0.0;

	if (loop_enabled > 0.5 && path_distance >= loop_entry_distance &&
		path_distance <= loop_exit_distance) {
		float loop_angle = (path_distance - loop_entry_distance) /
			effective_loop_radius;
		path_forward = loop_entry_distance +
			sin(loop_angle) * effective_loop_radius;
		path_height = (1.0 - cos(loop_angle)) * effective_loop_radius;
	} else if (loop_enabled > 0.5 && path_distance > loop_exit_distance) {
		path_forward = loop_entry_distance +
			(path_distance - loop_exit_distance);
	}

	float wave_offset = sin(
		path_forward * wave_frequency + wave_phase) * wave_amplitude;
	VERTEX.x = path_forward;
	VERTEX.z = wave_offset + side;
	VERTEX.y += path_height +
		sin(UV.x * 3.14159265359) * vertical_lift;

	streak_visibility = appears * cycle_visibility;
	streak_detail = hash11(detail_seed * 211.3 + cycle_index * 3.7);
}

void fragment() {
	float safe_end_fade = min(end_fade, 0.49);
	float length_fade = smoothstep(0.0, safe_end_fade, UV.x) *
		(1.0 - smoothstep(1.0 - safe_end_fade, 1.0, UV.x));
	float width_shape = 1.0 - abs(UV.y * 2.0 - 1.0);
	width_shape = smoothstep(0.0, 0.72, width_shape);
	float broken_enabled = 1.0 - step(
		broken_streak_chance,
		streak_detail);
	float broken_pattern = 0.5 + 0.5 * sin(
		UV.x * 18.0 + streak_detail * 19.0);
	float broken_mask = mix(
		1.0,
		mix(1.0 - broken_streak_strength, 1.0, broken_pattern),
		broken_enabled);
	float alpha = streak_visibility * length_fade * width_shape *
		broken_mask * base_alpha * streak_color.a;
	float center = 1.0 - abs(UV.y * 2.0 - 1.0);
	float highlight = 1.0 + center * center_highlight;
	float event_brightness = mix(
		1.0,
		event_brightness_multiplier,
		event_blend);

	ALBEDO = streak_color.rgb * highlight * event_brightness;
	EMISSION = streak_color.rgb * emission_strength *
		highlight * event_brightness;
	ALPHA = alpha;
}
