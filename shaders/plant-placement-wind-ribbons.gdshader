shader_type spatial;
render_mode unshaded, cull_disabled, blend_add, depth_draw_never, shadows_disabled;

uniform vec4 effect_color : source_color = vec4(0.72, 1.0, 0.24, 1.0);
uniform float opacity : hint_range(0.0, 1.5) = 1.0;
uniform float emission_strength : hint_range(0.0, 3.0) = 1.0;
uniform float base_radius : hint_range(0.2, 1.5) = 1.074;
uniform float base_height : hint_range(-0.3, 0.5) = 0.07;
uniform float rise_height : hint_range(0.1, 1.8) = 0.62;
uniform float rise_speed : hint_range(0.05, 1.0) = 0.11;
uniform float spin_speed : hint_range(0.0, 2.0) = 0.24;

varying float ribbon_visibility;
varying float ribbon_detail;
varying float ribbon_shape;
varying float ribbon_width_profile;

float hash11(float value) {
	return fract(sin(value * 127.1 + 311.7) * 43758.5453123);
}

void vertex() {
	float phase_seed = INSTANCE_CUSTOM.r;
	float speed_seed = INSTANCE_CUSTOM.g;
	float shape_seed = INSTANCE_CUSTOM.b;
	float detail_seed = INSTANCE_CUSTOM.a;
	float speed_variation = mix(0.78, 1.24, speed_seed);
	float cycle = TIME * rise_speed * speed_variation + phase_seed;
	float life = fract(cycle);
	float cycle_index = floor(cycle);
	float cycle_random = hash11(cycle_index + shape_seed * 79.31);
	float arc_span = mix(1.05, 2.35, shape_seed);
	float sector_jitter = (cycle_random - 0.5) *
		mix(0.10, 0.28, shape_seed);
	float start_angle = phase_seed * 6.28318530718 +
		sector_jitter;
	float forward_turn = life * spin_speed * mix(2.2, 4.0, speed_seed);
	float tail_angle = start_angle + forward_turn;
	float angle = tail_angle + UV.x * arc_span;
	angle += sin(
		UV.x * mix(3.0, 5.5, detail_seed) +
		shape_seed * 8.0) *
		mix(0.035, 0.12, shape_seed) *
		smoothstep(0.03, 0.30, UV.x);
	vec2 radial_direction = vec2(cos(angle), sin(angle));
	vec2 tail_direction = abs(vec2(cos(tail_angle), sin(tail_angle)));
	float hex_direction = max(
		tail_direction.y * 1.1547005,
		tail_direction.x + tail_direction.y * 0.5773503);
	float local_radius = base_radius / max(hex_direction, 0.0001);
	float width_flow = 0.5 + 0.5 * sin(
		UV.x * mix(4.6, 7.8, detail_seed) +
		shape_seed * 13.0);
	float width_detail = 0.5 + 0.5 * sin(
		UV.x * mix(10.0, 16.0, shape_seed) +
		detail_seed * 19.0);
	float width_profile = mix(0.58, 1.18, width_flow) *
		mix(0.84, 1.12, width_detail);
	float width = mix(0.045, 0.085, detail_seed) * width_profile;
	float across = UV.y - 0.5;
	float wave = sin(
		UV.x * mix(8.0, 15.0, detail_seed) +
		shape_seed * 12.0) *
		mix(0.014, 0.052, shape_seed);
	wave *= smoothstep(0.03, 0.25, UV.x);
	float free_flow = smoothstep(0.02, 0.38, UV.x);
	float radial_drift = life * mix(0.18, 0.44, detail_seed);
	radial_drift += UV.x * mix(0.045, 0.17, shape_seed);
	radial_drift *= free_flow;
	float radial_position = local_radius +
		radial_drift + across * width + wave;

	VERTEX.xz = radial_direction * radial_position;
	float local_rise = rise_height * mix(0.48, 0.92, shape_seed);
	float rise_from_line = smoothstep(0.0, 0.20, UV.x);
	VERTEX.y = base_height + pow(UV.x, 1.12) * local_rise +
		life * mix(0.16, 0.42, speed_seed) * rise_from_line;
	VERTEX.y += sin(UV.x * 3.14159265359) *
		mix(0.025, 0.09, detail_seed);
	VERTEX.y += across * width * 0.75;

	float rise_fade = smoothstep(0.0, 0.09, life) *
		(1.0 - smoothstep(0.62, 0.88, life));
	float flicker = mix(
		0.80,
		1.0,
		0.5 + 0.5 * sin(
			TIME * mix(1.2, 2.1, detail_seed) +
			shape_seed * 17.0));
	ribbon_visibility = rise_fade * flicker;
	ribbon_detail = detail_seed;
	ribbon_shape = shape_seed;
	ribbon_width_profile = width_profile;
}

void fragment() {
	float tail_length = mix(0.07, 0.15, ribbon_detail);
	float head_length = mix(0.82, 0.94, ribbon_shape);
	float end_fade = smoothstep(0.0, tail_length, UV.x) *
		(1.0 - smoothstep(head_length, 1.0, UV.x));
	float width_distance = abs(UV.y * 2.0 - 1.0);
	float soft_glow = 1.0 - smoothstep(0.08, 1.0, width_distance);
	soft_glow *= soft_glow;
	float soft_body = 1.0 - smoothstep(0.12, 0.68, width_distance);
	float signed_width = UV.y * 2.0 - 1.0;
	float strand_offset = mix(0.22, 0.31, ribbon_detail);
	float core_width = mix(0.11, 0.19,
		clamp(ribbon_width_profile, 0.0, 1.0));
	float first_core_distance = abs(signed_width - strand_offset);
	float second_core_distance = abs(signed_width + strand_offset);
	float first_core = 1.0 - smoothstep(
		0.0, core_width, first_core_distance);
	float second_core = 1.0 - smoothstep(
		0.0, core_width, second_core_distance);
	float bright_core = max(first_core, second_core);
	float first_white_core = 1.0 - smoothstep(
		0.0, core_width * 0.32, first_core_distance);
	float second_white_core = 1.0 - smoothstep(
		0.0, core_width * 0.32, second_core_distance);
	float white_core = max(first_white_core, second_white_core);
	float wispy_breakup = 0.76 + 0.24 * sin(
		UV.x * mix(18.0, 31.0, ribbon_detail) +
		TIME * mix(0.55, 0.95, ribbon_detail) +
		ribbon_detail * 15.0);
	float drifting_gap = smoothstep(
		0.08,
		0.55,
		0.5 + 0.5 * sin(
			UV.x * mix(7.0, 13.0, ribbon_shape) -
			TIME * mix(0.45, 0.80, ribbon_detail) +
			ribbon_shape * 21.0));
	float alpha = ribbon_visibility * end_fade *
		(soft_glow * 0.22 + soft_body * 0.38 +
			bright_core * 0.68 + white_core * 0.28) *
		wispy_breakup * mix(0.58, 1.0, drifting_gap) *
		opacity * effect_color.a;
	vec3 core_color = mix(
		effect_color.rgb,
		vec3(0.92, 1.0, 0.70),
		0.16 + bright_core * 0.28 + white_core * 0.24);

	ALBEDO = core_color;
	EMISSION = core_color * emission_strength *
		(0.50 + soft_glow * 0.50 + bright_core * 1.45 +
			white_core * 1.25);
	ALPHA = alpha;
}
