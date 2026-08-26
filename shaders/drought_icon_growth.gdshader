shader_type canvas_item;

uniform bool growth_enabled = false;
uniform float growth_progress : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	vec4 texture_color = texture(TEXTURE, UV);
	if (growth_enabled) {
		const vec3 background_color = vec3(0.94902, 0.75686, 0.52941);
		float reveal_line = mix(1.02, -0.02, growth_progress);
		float reveal_mask = smoothstep(
			reveal_line - 0.018,
			reveal_line + 0.018,
			UV.y);

		vec2 ellipse_position = (UV - vec2(0.5)) / vec2(0.5);
		float frame_mask = smoothstep(0.965, 0.99, length(ellipse_position));
		float background_mask = 1.0 - smoothstep(
			0.018,
			0.065,
			distance(texture_color.rgb, background_color));
		float original_color_mask = max(
			reveal_mask,
			max(frame_mask, background_mask));

		texture_color.rgb = mix(
			background_color,
			texture_color.rgb,
			original_color_mask);
	}
	COLOR = texture_color * COLOR;
}
