shader_type spatial;
render_mode unshaded, cull_disabled;

uniform vec4 highlight_color : source_color = vec4(0.2353, 0.3294, 0.1373, 1.0);
uniform vec4 light_color : source_color = vec4(0.1529, 0.2471, 0.1059, 1.0);
uniform vec4 mid_color : source_color = vec4(0.0863, 0.1647, 0.0745, 1.0);
uniform vec4 shadow_color : source_color = vec4(0.0314, 0.0745, 0.0471, 1.0);
uniform vec3 light_direction_world = vec3(-0.55, -0.75, -0.35);
uniform float highlight_threshold : hint_range(0.0, 1.0) = 0.88;
uniform float light_threshold : hint_range(0.0, 1.0) = 0.68;
uniform float mid_threshold : hint_range(0.0, 1.0) = 0.43;
uniform float variation_strength : hint_range(0.0, 0.5) = 0.42;
uniform float variation_scale : hint_range(0.1, 8.0) = 2.8;
uniform float instance_variation : hint_range(-1.0, 1.0) = 0.0;

varying vec3 world_position;

void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec3 view_light_direction = normalize(
		(VIEW_MATRIX * vec4(-light_direction_world, 0.0)).xyz
	);
	float brightness = dot(normalize(NORMAL), view_light_direction);
	brightness = brightness * 0.5 + 0.5;

	float broad_pattern = sin(
		world_position.x * variation_scale +
		world_position.y * variation_scale * 0.73 +
		world_position.z * variation_scale * 0.41 +
		instance_variation * 4.73
	);
	float fine_pattern = sin(
		world_position.x * variation_scale * 2.17 -
		world_position.y * variation_scale * 1.31 +
		world_position.z * variation_scale * 1.83 -
		instance_variation * 3.19
	);
	float painterly_pattern =
		broad_pattern * 0.68 +
		fine_pattern * 0.32;

	brightness = clamp(
		brightness + painterly_pattern * variation_strength,
		0.0,
		1.0
	);

	vec3 band_color = shadow_color.rgb;

	if (brightness > highlight_threshold) {
		band_color = highlight_color.rgb;
	} else if (brightness > light_threshold) {
		band_color = light_color.rgb;
	} else if (brightness > mid_threshold) {
		band_color = mid_color.rgb;
	}

	float color_variation = 0.86 + (fine_pattern * 0.5 + 0.5) * 0.18;
	band_color *= color_variation;

	float instance_mix = instance_variation * 0.5 + 0.5;
	vec3 cool_tint = vec3(0.72, 0.88, 0.77);
	vec3 warm_tint = vec3(1.02, 0.91, 0.68);
	band_color *= mix(cool_tint, warm_tint, instance_mix);
	band_color *= mix(0.78, 1.0, instance_mix);

	ALBEDO = band_color;
	ROUGHNESS = 1.0;
}
