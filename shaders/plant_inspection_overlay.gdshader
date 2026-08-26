shader_type canvas_item;
render_mode unshaded;

uniform vec2 viewport_size = vec2(1920.0, 1080.0);
uniform vec2 hex_point_0 = vec2(0.35, 0.45);
uniform vec2 hex_point_1 = vec2(0.40, 0.40);
uniform vec2 hex_point_2 = vec2(0.45, 0.45);
uniform vec2 hex_point_3 = vec2(0.45, 0.55);
uniform vec2 hex_point_4 = vec2(0.40, 0.60);
uniform vec2 hex_point_5 = vec2(0.35, 0.55);
uniform sampler2D plant_foreground_mask;
uniform float edge_feather : hint_range(0.0, 12.0, 0.5) = 1.0;
uniform float darkness : hint_range(0.0, 0.85, 0.01) = 0.5;

float cross_2d(vec2 left, vec2 right) {
	return left.x * right.y - left.y * right.x;
}

float signed_edge_distance(
		vec2 pixel_position,
		vec2 edge_start,
		vec2 edge_end,
		float orientation) {
	vec2 edge = edge_end - edge_start;
	return cross_2d(edge, pixel_position - edge_start) * orientation /
		max(length(edge), 0.001);
}

void fragment() {
	vec2 pixel_position = UV * viewport_size;
	vec2 point_0 = hex_point_0 * viewport_size;
	vec2 point_1 = hex_point_1 * viewport_size;
	vec2 point_2 = hex_point_2 * viewport_size;
	vec2 point_3 = hex_point_3 * viewport_size;
	vec2 point_4 = hex_point_4 * viewport_size;
	vec2 point_5 = hex_point_5 * viewport_size;
	float turn = cross_2d(point_1 - point_0, point_2 - point_1);
	float orientation = mix(-1.0, 1.0, step(0.0, turn));
	float signed_distance = signed_edge_distance(
		pixel_position, point_0, point_1, orientation);
	signed_distance = min(signed_distance, signed_edge_distance(
		pixel_position, point_1, point_2, orientation));
	signed_distance = min(signed_distance, signed_edge_distance(
		pixel_position, point_2, point_3, orientation));
	signed_distance = min(signed_distance, signed_edge_distance(
		pixel_position, point_3, point_4, orientation));
	signed_distance = min(signed_distance, signed_edge_distance(
		pixel_position, point_4, point_5, orientation));
	signed_distance = min(signed_distance, signed_edge_distance(
		pixel_position, point_5, point_0, orientation));
	float feather = max(edge_feather, 0.001);
	float bright_hex = smoothstep(-feather, feather, signed_distance);
	float plant_mask = smoothstep(
		0.02,
		0.25,
		texture(plant_foreground_mask, UV).a);
	float dark_mask = (1.0 - bright_hex) * (1.0 - plant_mask);

	COLOR = vec4(vec3(0.0), darkness * dark_mask);
}
