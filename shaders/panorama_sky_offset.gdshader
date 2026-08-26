shader_type sky;

uniform sampler2D panorama : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D cloud_panorama : source_color, filter_linear_mipmap, repeat_enable;
uniform float vertical_offset : hint_range(-0.25, 0.25, 0.005) = -0.12;
uniform float brightness : hint_range(0.5, 1.5, 0.01) = 1.0;
uniform float cloud_opacity : hint_range(0.0, 1.0, 0.01) = 0.38;
uniform float cloud_horizontal_offset : hint_range(0.0, 1.0, 0.005) = 0.18;
uniform float cloud_vertical_offset : hint_range(-0.2, 0.2, 0.005) = 0.0;
uniform float cloud_cutoff : hint_range(0.0, 1.0, 0.01) = 0.34;
uniform float cloud_softness : hint_range(0.01, 0.5, 0.01) = 0.20;
uniform vec3 cloud_tint : source_color = vec3(1.0, 0.94, 0.86);
uniform float cloud_night_visibility : hint_range(0.0, 1.0, 0.01) = 0.52;
uniform float night_amount : hint_range(0.0, 1.0, 0.01) = 0.0;
uniform vec3 night_tint : source_color = vec3(0.42, 0.56, 0.88);
uniform float night_brightness : hint_range(0.1, 1.0, 0.01) = 0.58;
uniform float night_saturation : hint_range(0.0, 1.0, 0.01) = 0.62;

void sky() {
	vec2 panorama_uv = SKY_COORDS;
	panorama_uv.x = fract(panorama_uv.x);
	panorama_uv.y = clamp(panorama_uv.y + vertical_offset, 0.001, 0.999);
	vec3 day_color = texture(panorama, panorama_uv).rgb * brightness;

	vec2 cloud_uv = panorama_uv;
	cloud_uv.x = fract(cloud_uv.x + cloud_horizontal_offset);
	cloud_uv.y = clamp(
		cloud_uv.y + cloud_vertical_offset,
		0.001,
		0.999);
	vec3 cloud_sample = texture(cloud_panorama, cloud_uv).rgb;
	float cloud_whiteness = min(
		cloud_sample.r,
		min(cloud_sample.g, cloud_sample.b));
	float cloud_mask = smoothstep(
		cloud_cutoff,
		cloud_cutoff + cloud_softness,
		cloud_whiteness);
	float upper_sky_mask = 1.0 - smoothstep(0.49, 0.57, panorama_uv.y);
	float visible_clouds = cloud_opacity * cloud_mask * upper_sky_mask;
	visible_clouds *= mix(1.0, cloud_night_visibility, night_amount);
	vec3 cloud_color = cloud_sample * cloud_tint * brightness;
	vec3 cloudy_day_color = mix(day_color, cloud_color, visible_clouds);

	float luminance = dot(cloudy_day_color, vec3(0.2126, 0.7152, 0.0722));
	vec3 muted_color = mix(
		vec3(luminance),
		cloudy_day_color,
		night_saturation);
	vec3 night_color = muted_color * night_tint * night_brightness;
	COLOR = mix(cloudy_day_color, night_color, night_amount);
}
