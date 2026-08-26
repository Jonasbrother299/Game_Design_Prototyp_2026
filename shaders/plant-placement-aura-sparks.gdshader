shader_type spatial;
render_mode unshaded, cull_disabled, blend_add, depth_draw_never, shadows_disabled;

uniform vec4 effect_color : source_color = vec4(0.72, 1.0, 0.24, 1.0);
uniform float opacity : hint_range(0.0, 1.5) = 0.8;
uniform float emission_strength : hint_range(0.0, 3.0) = 1.2;
uniform float rise_speed : hint_range(0.05, 1.0) = 0.24;
uniform float maximum_height : hint_range(0.2, 2.5) = 1.35;

varying float spark_visibility;
varying float spark_brightness;

void vertex() {
	float angle_seed = INSTANCE_CUSTOM.r;
	float radius_seed = INSTANCE_CUSTOM.g;
	float phase_seed = INSTANCE_CUSTOM.b;
	float speed_seed = INSTANCE_CUSTOM.a;
	float cycle = TIME * rise_speed * mix(0.72, 1.28, speed_seed) + phase_seed;
	float life = fract(cycle);
	float cycle_index = floor(cycle);
	float start_angle = angle_seed * 6.28318530718 +
		cycle_index * mix(0.17, 0.41, speed_seed);
	float angle = start_angle +
		life * mix(0.72, 1.42, radius_seed) +
		life * life * mix(0.08, 0.32, speed_seed);
	vec2 radial_direction = vec2(cos(angle), sin(angle));
	float radius = mix(0.66, 1.05, radius_seed);
	radius += life * mix(0.18, 0.42, speed_seed);
	float height = 0.065 + life * maximum_height * mix(0.34, 0.68, speed_seed);
	height += sin(life * 3.14159265359) * 0.045;
	float pulse = 0.72 + 0.28 * sin(TIME * 2.1 + phase_seed * 17.0);
	float size = mix(0.76, 1.34, radius_seed) * mix(0.88, 1.08, pulse);
	float flutter = TIME * mix(1.8, 3.4, speed_seed) + phase_seed * 12.0;
	float turn_sine = sin(angle + flutter * 0.18);
	float turn_cosine = cos(angle + flutter * 0.18);
	VERTEX.xz = mat2(
		vec2(turn_cosine, -turn_sine),
		vec2(turn_sine, turn_cosine)) * VERTEX.xz;
	float roll_sine = sin(flutter) * 0.72;
	float roll_cosine = cos(flutter);
	VERTEX.xy = mat2(
		vec2(roll_cosine, -roll_sine),
		vec2(roll_sine, roll_cosine)) * VERTEX.xy;

	VERTEX *= size;
	VERTEX += vec3(radial_direction.x * radius, height, radial_direction.y * radius);

	float appear = smoothstep(0.0, 0.10, life);
	float disappear = 1.0 - smoothstep(0.60, 0.86, life);
	spark_visibility = appear * disappear;
	spark_brightness = pulse;
}

void fragment() {
	vec3 spark_color = mix(
		effect_color.rgb,
		vec3(0.88, 1.0, 0.54),
		0.32);
	float alpha = spark_visibility * spark_brightness *
		opacity * effect_color.a;

	ALBEDO = spark_color;
	EMISSION = spark_color * emission_strength *
		(1.15 + spark_brightness * 1.35);
	ALPHA = alpha;
}
