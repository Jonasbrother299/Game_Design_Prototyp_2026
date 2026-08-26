shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

uniform float intensity : hint_range(0.0, 1.0) = 0.72;
uniform float distortion_strength : hint_range(0.0, 0.03) = 0.0075;
uniform float effect_height : hint_range(0.1, 0.95) = 0.62;
uniform float rise_speed : hint_range(0.0, 2.0) = 0.55;
uniform float plume_count : hint_range(4.0, 5.0, 1.0) = 5.0;
uniform float plume_width : hint_range(0.02, 0.12, 0.005) = 0.055;
uniform float lateral_drift : hint_range(0.0, 0.2, 0.005) = 0.085;
uniform float shimmer_strength : hint_range(0.0, 1.0) = 0.36;
uniform float chromatic_split : hint_range(0.0, 0.008, 0.00025) = 0.00125;

const float HEAT_TAU = 6.28318530718;

float hash_11(float value)
{
	return fract(sin(value * 127.1) * 43758.5453123);
}

float smooth_noise_11(float value, float seed)
{
	float cell = floor(value);
	float weight = fract(value);
	weight = weight * weight * (3.0 - 2.0 * weight);
	return mix(
		hash_11(cell + seed),
		hash_11(cell + 1.0 + seed),
		weight);
}

void fragment()
{
	vec2 screen_uv = SCREEN_UV;
	float safe_height = max(effect_height, 0.001);
	float height_from_bottom = (1.0 - UV.y) / safe_height;
	float height_mask = 1.0 - smoothstep(
		1.28,
		1.48,
		height_from_bottom);
	float source_fade = smoothstep(
		0.0,
		0.055,
		height_from_bottom);
	float active_plume_count = clamp(
		floor(plume_count + 0.5),
		4.0,
		5.0);
	float horizontal_distortion = 0.0;
	float vertical_distortion = 0.0;
	float heat_presence = 0.0;
	float heat_shimmer = 0.0;

	for (int plume_index = 0; plume_index < 5; plume_index++)
	{
		float index = float(plume_index);
		float active = 1.0 - step(active_plume_count - 0.5, index);
		float seed = hash_11(index + 11.7);
		float secondary_seed = hash_11(index + 37.1);
		float tertiary_seed = hash_11(index + 73.9);
		float flow_time = TIME * max(rise_speed, 0.0) *
			mix(0.82, 1.18, seed);
		float upward_phase =
			height_from_bottom * mix(14.0, 19.0, tertiary_seed) -
			flow_time * 5.0 +
			seed * HEAT_TAU;
		float first_plume = 1.0 - step(0.5, index);
		float last_plume = 1.0 - step(
			0.5,
			abs(index - (active_plume_count - 1.0)));
		float tall_outlier = 1.0 - step(
			0.5,
			abs(index - min(3.0, active_plume_count - 1.0)));
		float slot_width = 1.0 / active_plume_count;
		float base_center =
			(index + 0.5) / active_plume_count +
			(seed - 0.5) * slot_width * 0.42 +
			(last_plume - first_plume) * lateral_drift * 0.35;
		float tip_height =
			0.76 +
			seed * 0.22 +
			secondary_seed * 0.14 +
			tall_outlier * 0.22 +
			sin(flow_time * 1.25 + tertiary_seed * HEAT_TAU) * 0.055;
		float height_progress = clamp(
			height_from_bottom / max(tip_height, 0.001),
			0.0,
			1.0);
		float sway_amount = smoothstep(
			0.0,
			0.20,
			height_from_bottom);
		float flowing_noise = smooth_noise_11(
			height_from_bottom * 4.1 - flow_time * 0.85,
			index * 17.3 + 5.7) * 2.0 - 1.0;
		float fine_noise = smooth_noise_11(
			height_from_bottom * 9.7 - flow_time * 1.45,
			index * 29.1 + 13.4) * 2.0 - 1.0;
		float sway = (
			sin(
				height_from_bottom * 7.3 -
				flow_time * 1.8 +
				seed * HEAT_TAU) * 0.019 +
			sin(
				height_from_bottom * 16.0 -
				flow_time * 3.1 +
				secondary_seed * HEAT_TAU) * 0.009 +
			flowing_noise * 0.018 +
			fine_noise * 0.006
		) * sway_amount;
		float random_bend =
			(secondary_seed - 0.5) * 2.0 *
			lateral_drift * 0.58 *
			pow(height_progress, 1.45);
		float edge_bend =
			(last_plume - first_plume) *
			lateral_drift *
			pow(height_progress, 1.75);
		float plume_center = base_center + sway + random_bend + edge_bend;
		float tip_mask = 1.0 - smoothstep(
			tip_height - 0.17,
			tip_height + 0.015,
			height_from_bottom);
		float taper = mix(
			1.0,
			0.12,
			smoothstep(
				0.04,
				1.0,
				height_progress));
		float width_breath = mix(
			0.78,
			1.12,
			0.5 + 0.5 * sin(
				height_from_bottom * 11.0 -
				flow_time * 3.8 +
				tertiary_seed * HEAT_TAU));
		float half_width = max(plume_width, 0.005) *
			mix(0.72, 1.38, secondary_seed) *
			taper *
			width_breath;
		float horizontal_distance = abs(
			UV.x - plume_center);
		float horizontal_mask = 1.0 - smoothstep(
			half_width * 0.38,
			half_width,
			horizontal_distance);
		float filament_direction = mix(
			-1.0,
			1.0,
			step(0.5, tertiary_seed));
		float fork_amount = smoothstep(0.34, 0.78, height_progress);
		float filament_center = plume_center +
			filament_direction *
			half_width *
			mix(0.85, 1.55, seed) *
			fork_amount;
		float filament_width = max(
			half_width * mix(0.24, 0.42, tertiary_seed),
			0.0025);
		float filament_mask = (
			1.0 - smoothstep(
				filament_width * 0.25,
				filament_width,
				abs(UV.x - filament_center))
		) * fork_amount;
		horizontal_mask = max(
			horizontal_mask,
			filament_mask * 0.68);
		float rising_detail =
			0.64 +
			(0.5 + 0.5 * sin(upward_phase)) * 0.36;
		float secondary_detail =
			0.70 +
			(0.5 + 0.5 * sin(
				height_from_bottom * 31.0 -
				flow_time * 8.0 +
				secondary_seed * HEAT_TAU)) * 0.30;
		float plume_mask =
			active *
			height_mask *
			source_fade *
			tip_mask *
			horizontal_mask *
			rising_detail *
			secondary_detail;
		float horizontal_flow =
			sin(upward_phase) * 0.70 +
			sin(
				upward_phase * 1.7 +
				secondary_seed * HEAT_TAU) * 0.30 +
			flowing_noise * 0.22;
		float vertical_flow = cos(
			upward_phase * 0.83 +
			seed * HEAT_TAU) * 0.22 +
			fine_noise * 0.12;
		float shimmer_band = pow(
			0.5 + 0.5 * sin(
				upward_phase * 0.72 +
				tertiary_seed * HEAT_TAU),
			6.0);
		float edge_shimmer =
			4.0 * horizontal_mask * (1.0 - horizontal_mask);

		horizontal_distortion += horizontal_flow * plume_mask;
		vertical_distortion += vertical_flow * plume_mask;
		heat_presence += plume_mask;
		heat_shimmer += plume_mask *
			(0.34 + shimmer_band * 0.66 + edge_shimmer * 0.16);
	}

	float wave_strength = clamp(intensity, 0.0, 1.0);
	vec2 refraction_offset = vec2(
		horizontal_distortion,
		vertical_distortion) *
		max(distortion_strength, 0.0) *
		wave_strength;

	vec2 screen_margin = SCREEN_PIXEL_SIZE * 2.0;
	vec2 refracted_uv = clamp(
		screen_uv + refraction_offset,
		screen_margin,
		vec2(1.0) - screen_margin);
	float visible_presence = clamp(heat_presence, 0.0, 1.0);
	float visible_shimmer =
		clamp(heat_shimmer, 0.0, 1.0) *
		clamp(shimmer_strength, 0.0, 1.0) *
		wave_strength;
	float split_amount =
		max(chromatic_split, 0.0) *
		wave_strength *
		visible_presence;
	vec2 red_uv = clamp(
		refracted_uv + vec2(split_amount, -split_amount * 0.22),
		screen_margin,
		vec2(1.0) - screen_margin);
	vec2 blue_uv = clamp(
		refracted_uv - vec2(split_amount, -split_amount * 0.22),
		screen_margin,
		vec2(1.0) - screen_margin);
	vec3 heat_color = textureLod(screen_texture, refracted_uv, 0.0).rgb;
	vec3 split_color = heat_color;
	split_color.r = textureLod(screen_texture, red_uv, 0.0).r;
	split_color.b = textureLod(screen_texture, blue_uv, 0.0).b;
	heat_color = mix(
		heat_color,
		split_color,
		visible_presence * 0.62);
	heat_color += vec3(1.0, 0.58, 0.22) * visible_shimmer * 0.026;
	heat_color *= 1.0 + visible_shimmer * 0.016;
	COLOR = vec4(heat_color, 1.0);
}
