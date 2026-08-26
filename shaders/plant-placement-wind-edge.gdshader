shader_type spatial;
render_mode unshaded, cull_disabled, blend_add, depth_draw_never, shadows_disabled;

uniform vec4 effect_color : source_color = vec4(0.94, 0.90, 0.72, 1.0);
uniform float opacity : hint_range(0.0, 1.5) = 1.0;
uniform float emission_strength : hint_range(0.0, 3.0) = 1.0;

varying float arc_phase;
varying float arc_variation;

void vertex() {
	arc_phase = UV2.x;
	arc_variation = UV2.y;
	float rotation = TIME * mix(0.10, 0.22, arc_variation);
	float rotation_sine = sin(rotation);
	float rotation_cosine = cos(rotation);
	VERTEX.xz = vec2(
		VERTEX.x * rotation_cosine - VERTEX.z * rotation_sine,
		VERTEX.x * rotation_sine + VERTEX.z * rotation_cosine);
	VERTEX.y += sin(TIME * 0.55 + arc_phase * 6.28318530718) * 0.012;
}

void fragment() {
	float across = 1.0 - abs(UV.y * 2.0 - 1.0);
	float soft_edge = smoothstep(0.0, 0.84, across);
	float core = smoothstep(0.58, 1.0, across);
	float end_fade = smoothstep(0.0, 0.10, UV.x) *
		(1.0 - smoothstep(0.88, 1.0, UV.x));
	float broad_flow = 0.5 + 0.5 * sin(
		UV.x * mix(8.0, 14.0, arc_variation) -
		TIME * mix(0.70, 1.10, arc_variation) +
		arc_phase * 6.28318530718);
	float fine_flow = 0.5 + 0.5 * sin(
		UV.x * mix(21.0, 35.0, arc_phase) -
		TIME * mix(1.20, 1.75, arc_variation) +
		arc_phase * 11.0);
	float energy = smoothstep(
		0.18,
		0.92,
		broad_flow * 0.68 + fine_flow * 0.32);
	float alpha = end_fade * soft_edge *
		(0.10 + energy * 0.52 + core * 0.28) *
		mix(0.72, 1.0, arc_variation) * opacity * effect_color.a;
	vec3 glow_color = mix(
		effect_color.rgb,
		vec3(1.0, 0.985, 0.90),
		core * 0.72 + energy * 0.18);

	ALBEDO = glow_color;
	EMISSION = glow_color * emission_strength *
		(0.55 + energy * 1.05 + core * 0.45);
	ALPHA = alpha;
}
