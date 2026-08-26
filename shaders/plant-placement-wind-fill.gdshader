shader_type spatial;
render_mode unshaded, cull_disabled, blend_add, depth_draw_never, depth_test_disabled, shadows_disabled;

uniform vec4 fill_color : source_color = vec4(0.52, 0.92, 0.18, 1.0);
uniform float opacity : hint_range(0.0, 0.2, 0.005) = 0.08;

void fragment() {
	vec2 centered_uv = UV * 2.0 - 1.0;
	vec2 absolute_uv = abs(centered_uv);
	float hex_distance = max(
		absolute_uv.y * 1.1547005,
		absolute_uv.x + absolute_uv.y * 0.5773503);
	float edge_distance = abs(hex_distance - 0.91);
	float edge = 1.0 - smoothstep(0.005, 0.021, edge_distance);
	float edge_alpha = edge * opacity * 4.2;

	ALBEDO = fill_color.rgb;
	EMISSION = fill_color.rgb * edge * 2.0;
	ALPHA = clamp(edge_alpha, 0.0, 1.0) * fill_color.a;
}
