shader_type spatial;
render_mode unshaded, cull_disabled, shadows_disabled, depth_draw_never, blend_mix;

uniform vec4 color_a : source_color = vec4(0.025, 0.018, 0.014, 1.0);
uniform vec4 color_b : source_color = vec4(0.19, 0.095, 0.035, 1.0);
uniform float effect_opacity : hint_range(0.0, 1.0) = 0.9;
uniform vec4 outline_color : source_color = vec4(0.86, 0.52, 0.14, 1.0);
uniform float outline_strength : hint_range(0.0, 1.0) = 0.72;
uniform float emission_strength : hint_range(0.0, 2.0) = 0.16;

varying vec4 particle_data;

void vertex() {
	particle_data = INSTANCE_CUSTOM;
}

void fragment() {
	float color_mix = clamp(particle_data.r, 0.0, 1.0);
	vec4 particle_color = mix(color_a, color_b, color_mix);
	float edge_distance = length(UV - vec2(0.5));
	float edge_mask = smoothstep(0.30, 0.50, edge_distance);
	particle_color.rgb = mix(
		particle_color.rgb,
		outline_color.rgb,
		edge_mask * outline_strength
	);
	ALBEDO = particle_color.rgb;
	EMISSION = particle_color.rgb * emission_strength;
	ALPHA = particle_color.a * effect_opacity * particle_data.a;
}
