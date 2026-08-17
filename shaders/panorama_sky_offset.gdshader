shader_type sky;

uniform sampler2D panorama : source_color, filter_linear_mipmap, repeat_enable;
uniform float vertical_offset : hint_range(-0.25, 0.25, 0.005) = -0.12;
uniform float brightness : hint_range(0.5, 1.5, 0.01) = 1.0;

void sky() {
	vec2 panorama_uv = SKY_COORDS;
	panorama_uv.x = fract(panorama_uv.x);
	panorama_uv.y = clamp(panorama_uv.y + vertical_offset, 0.001, 0.999);
	COLOR = texture(panorama, panorama_uv).rgb * brightness;
}
