shader_type spatial;
render_mode specular_disabled;

uniform sampler2D grass_texture : source_color, filter_linear_mipmap_anisotropic;
uniform sampler2D wind_noise : repeat_enable, filter_linear_mipmap;

uniform vec4 grass_shadow_color : source_color = vec4(0.025, 0.105, 0.025, 1.0);
uniform vec4 grass_base_color : source_color = vec4(0.075, 0.235, 0.050, 1.0);
uniform vec4 grass_light_color : source_color = vec4(0.300, 0.410, 0.105, 1.0);
uniform vec4 grass_cool_color : source_color = vec4(0.045, 0.175, 0.125, 1.0);
uniform vec4 grass_warm_color : source_color = vec4(0.220, 0.285, 0.070, 1.0);
uniform vec4 grass_wind_light_color : source_color = vec4(0.440, 0.535, 0.155, 1.0);
uniform vec4 grass_dry_shadow_color : source_color = vec4(0.160, 0.110, 0.045, 1.0);
uniform vec4 grass_dry_color : source_color = vec4(0.285, 0.200, 0.070, 1.0);
uniform vec4 grass_dry_light_color : source_color = vec4(0.360, 0.290, 0.115, 1.0);
uniform vec4 grass_dry_green_color : source_color = vec4(0.100, 0.170, 0.065, 1.0);

uniform float patch_scale : hint_range(0.01, 1.0) = 0.085;
uniform float height_patch_scale : hint_range(0.01, 1.0) = 0.18;
uniform float height_patch_min : hint_range(0.1, 1.0) = 0.52;
uniform float toon_threshold : hint_range(-1.0, 1.0) = 0.28;
uniform float toon_softness : hint_range(0.001, 0.5) = 0.08;
uniform float toon_shadow_strength : hint_range(0.0, 1.0) = 0.58;

uniform vec2 wind_direction = vec2(0.894, 0.447);
uniform float wind_wave_scale : hint_range(0.01, 1.0) = 0.07;
uniform float wind_detail_scale : hint_range(0.01, 2.0) = 0.24;
uniform float wind_gust_strength : hint_range(0.0, 1.0) = 0.12;
uniform float wind_gust_threshold : hint_range(0.1, 0.9) = 0.72;
uniform float wind_gust_softness : hint_range(0.01, 0.5) = 0.10;
uniform float wind_gust_speed_multiplier : hint_range(0.5, 4.0, 0.5) = 2.0;
uniform float tile_color_blend_width : hint_range(0.05, 1.0) = 0.72;

instance uniform vec4 grass_state = vec4(1.0, 0.92, 0.0, 0.0);
instance uniform vec4 grass_wind = vec4(0.035, 0.075, 0.07, 0.012);
instance uniform vec4 grass_border = vec4(0.0);
instance uniform vec4 grass_border_directions_01 = vec4(0.0);
instance uniform vec4 grass_border_directions_23 = vec4(0.0);
instance uniform vec4 grass_border_directions_45 = vec4(0.0);
instance uniform vec4 grass_outer_edges_0123 = vec4(0.0);
instance uniform vec2 grass_outer_edges_45 = vec2(0.0);
instance uniform vec4 grass_neighbor_dry_01 = vec4(0.0);
instance uniform vec4 grass_neighbor_dry_23 = vec4(0.0);
instance uniform vec4 grass_neighbor_dry_45 = vec4(0.0);
instance uniform vec4 grass_neighbor_state_01 = vec4(0.0);
instance uniform vec4 grass_neighbor_state_23 = vec4(0.0);
instance uniform vec4 grass_neighbor_state_45 = vec4(0.0);

varying flat float blade_visible;
varying float blade_tip;
varying float wind_shadow;
varying vec3 grass_world_position;
varying flat float blade_dry_amount;
varying flat vec2 grass_root_world_position;
varying flat vec2 grass_gust_shape;

vec2 safe_normalize(vec2 value, vec2 fallback) {
	float value_length = length(value);
	return value_length > 0.0001 ? value / value_length : fallback;
}

float hash_position(vec2 position) {
	return fract(sin(dot(position, vec2(12.9898, 78.233))) * 43758.5453);
}

float crosses_border(
	vec2 position,
	vec2 direction,
	float edge_distance,
	float active
) {
	return active * step(edge_distance, dot(position, direction));
}

float neighbor_blend_weight(
	vec2 position,
	vec2 direction,
	float edge_distance,
	float active
) {
	return active * smoothstep(
		edge_distance - tile_color_blend_width,
		edge_distance,
		dot(position, direction)
	);
}

vec2 dry_neighbor_contribution(float neighbor_dry, float edge_weight) {
	return vec2(neighbor_dry * edge_weight, edge_weight);
}

vec3 state_neighbor_contribution(vec2 neighbor_state, float edge_weight) {
	return vec3(neighbor_state * edge_weight, edge_weight);
}

void vertex() {
	vec3 instance_world_position = MODEL_MATRIX[3].xyz;
	float instance_random = hash_position(
		instance_world_position.xz + vec2(grass_state.w * 7.31, grass_state.w * 3.17)
	);
	float shape_random = hash_position(
		instance_world_position.zx + vec2(19.17, 47.73)
	);
	float painted_normal_x = texture(
		wind_noise,
		instance_world_position.xz * patch_scale * 1.8 + vec2(0.23, 0.61)
	).r * 2.0 - 1.0;
	float painted_normal_z = texture(
		wind_noise,
		instance_world_position.xz * patch_scale * 1.8 + vec2(0.79, 0.17)
	).r * 2.0 - 1.0;
	vec3 painted_world_normal = normalize(vec3(
		painted_normal_x * 0.34,
		1.0,
		painted_normal_z * 0.34
	));
	vec3 painted_local_normal = normalize(
		transpose(mat3(MODEL_MATRIX)) * painted_world_normal
	);
	float height_patch_noise = texture(
		wind_noise,
		instance_world_position.xz * height_patch_scale + vec2(0.17, 0.83)
	).r;
	float height_patch = mix(
		height_patch_min,
		1.0,
		smoothstep(0.30, 0.72, height_patch_noise)
	);
	grass_root_world_position = instance_world_position.xz;
	grass_gust_shape = vec2(
		painted_normal_x * 0.5 + 0.5,
		height_patch_noise
	);

	vec2 border_position = instance_world_position.xz - grass_border.xy;
	float border_edge = max(grass_border.z - grass_border.w, 0.0);
	float outside_border = 0.0;
	outside_border = max(outside_border, crosses_border(
		border_position,
		grass_border_directions_01.xy,
		border_edge,
		grass_outer_edges_0123.x));
	outside_border = max(outside_border, crosses_border(
		border_position,
		grass_border_directions_01.zw,
		border_edge,
		grass_outer_edges_0123.y));
	outside_border = max(outside_border, crosses_border(
		border_position,
		grass_border_directions_23.xy,
		border_edge,
		grass_outer_edges_0123.z));
	outside_border = max(outside_border, crosses_border(
		border_position,
		grass_border_directions_23.zw,
		border_edge,
		grass_outer_edges_0123.w));
	outside_border = max(outside_border, crosses_border(
		border_position,
		grass_border_directions_45.xy,
		border_edge,
		grass_outer_edges_45.x));
	outside_border = max(outside_border, crosses_border(
		border_position,
		grass_border_directions_45.zw,
		border_edge,
		grass_outer_edges_45.y));

	float neighbor_weight_0 = neighbor_blend_weight(
		border_position,
		grass_border_directions_01.xy,
		grass_border.z,
		grass_neighbor_dry_01.y);
	float neighbor_weight_1 = neighbor_blend_weight(
		border_position,
		grass_border_directions_01.zw,
		grass_border.z,
		grass_neighbor_dry_01.w);
	float neighbor_weight_2 = neighbor_blend_weight(
		border_position,
		grass_border_directions_23.xy,
		grass_border.z,
		grass_neighbor_dry_23.y);
	float neighbor_weight_3 = neighbor_blend_weight(
		border_position,
		grass_border_directions_23.zw,
		grass_border.z,
		grass_neighbor_dry_23.w);
	float neighbor_weight_4 = neighbor_blend_weight(
		border_position,
		grass_border_directions_45.xy,
		grass_border.z,
		grass_neighbor_dry_45.y);
	float neighbor_weight_5 = neighbor_blend_weight(
		border_position,
		grass_border_directions_45.zw,
		grass_border.z,
		grass_neighbor_dry_45.w);

	vec2 dry_blend = vec2(grass_state.z, 1.0);
	dry_blend += dry_neighbor_contribution(
		grass_neighbor_dry_01.x,
		neighbor_weight_0);
	dry_blend += dry_neighbor_contribution(
		grass_neighbor_dry_01.z,
		neighbor_weight_1);
	dry_blend += dry_neighbor_contribution(
		grass_neighbor_dry_23.x,
		neighbor_weight_2);
	dry_blend += dry_neighbor_contribution(
		grass_neighbor_dry_23.z,
		neighbor_weight_3);
	dry_blend += dry_neighbor_contribution(
		grass_neighbor_dry_45.x,
		neighbor_weight_4);
	dry_blend += dry_neighbor_contribution(
		grass_neighbor_dry_45.z,
		neighbor_weight_5);
	blade_dry_amount = clamp(dry_blend.x / dry_blend.y, 0.0, 1.0);

	vec3 state_blend = vec3(grass_state.xy, 1.0);
	state_blend += state_neighbor_contribution(
		grass_neighbor_state_01.xy,
		neighbor_weight_0);
	state_blend += state_neighbor_contribution(
		grass_neighbor_state_01.zw,
		neighbor_weight_1);
	state_blend += state_neighbor_contribution(
		grass_neighbor_state_23.xy,
		neighbor_weight_2);
	state_blend += state_neighbor_contribution(
		grass_neighbor_state_23.zw,
		neighbor_weight_3);
	state_blend += state_neighbor_contribution(
		grass_neighbor_state_45.xy,
		neighbor_weight_4);
	state_blend += state_neighbor_contribution(
		grass_neighbor_state_45.zw,
		neighbor_weight_5);
	vec2 blended_grass_state = state_blend.xy / max(state_blend.z, 0.0001);

	blade_visible =
		step(instance_random, clamp(blended_grass_state.x, 0.0, 1.0)) *
		(1.0 - outside_border);
	blade_tip = smoothstep(0.0, 0.16, VERTEX.y);

	vec2 cross_direction = vec2(-wind_direction.y, wind_direction.x);
	float wave_phase = TAU * (
		dot(instance_world_position.xz, wind_direction) * wind_wave_scale +
		TIME * grass_wind.x
	);
	float detail_phase = TAU * (
		dot(instance_world_position.xz, cross_direction) * wind_detail_scale -
		TIME * grass_wind.z
	);
	float wave = sin(wave_phase);
	float detail = sin(detail_phase + wave * 0.35);
	float bend =
		wave * grass_wind.y +
		detail * grass_wind.w;
	float shape_curve = mix(0.004, 0.014, shape_random);
	float curve_profile = blade_tip * blade_tip;
	vec2 local_wind = safe_normalize(
		(inverse(mat3(MODEL_MATRIX)) *
			vec3(wind_direction.x, 0.0, wind_direction.y)).xz,
		vec2(1.0, 0.0)
	);
	float curve_angle = shape_random * TAU;
	vec2 curve_direction = vec2(cos(curve_angle), sin(curve_angle));

	VERTEX.y *= max(blended_grass_state.y * height_patch, 0.0);
	VERTEX.xz += curve_direction * shape_curve * curve_profile;
	VERTEX.xz += local_wind * bend * blade_tip;
	VERTEX.y -= abs(bend) * blade_tip * 0.10;
	NORMAL = normalize(mix(NORMAL, painted_local_normal, 0.68));

	if (blade_visible < 0.5) {
		VERTEX *= 0.0;
	}

	wind_shadow = smoothstep(0.2, 0.8, abs(wave * 0.75 + detail * 0.25));
	grass_world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	if (blade_visible < 0.5) {
		discard;
	}

	float painted_value = mix(0.56, 0.92, blade_tip);
	float large_patch = texture(
		wind_noise,
		grass_world_position.xz * patch_scale + vec2(0.31, 0.73)
	).r;
	float secondary_patch = texture(
		wind_noise,
		grass_world_position.xz * patch_scale * 0.47 + vec2(0.67, 0.19)
	).r;
	float cool_patch = 1.0 - smoothstep(0.34, 0.62, large_patch);
	float warm_patch = smoothstep(0.46, 0.68, secondary_patch);
	vec2 gust_direction = safe_normalize(
		wind_direction,
		vec2(1.0, 0.0)
	);
	vec2 gust_cross_direction = vec2(
		-gust_direction.y,
		gust_direction.x
	);
	vec2 gust_secondary_direction = safe_normalize(
		gust_direction + gust_cross_direction * 0.78,
		gust_direction
	);
	float gust_time =
		TIME * grass_wind.x * wind_gust_speed_multiplier;
	float gust_secondary_time = gust_time * (11.0 / 9.0);
	float gust_sequence_time = gust_time * 0.73;
	float gust_sequence_index = floor(gust_sequence_time);
	float gust_sequence_blend = smoothstep(
		0.0,
		1.0,
		fract(gust_sequence_time)
	);
	float gust_activity = mix(
		hash_position(vec2(gust_sequence_index, 31.73)),
		hash_position(vec2(gust_sequence_index + 1.0, 31.73)),
		gust_sequence_blend
	);
	gust_activity = smoothstep(0.12, 0.88, gust_activity);
	float gust_primary_phase = TAU * (
		dot(grass_root_world_position, gust_direction) * wind_wave_scale +
		gust_time
	) + (grass_gust_shape.x - 0.5) * 4.2;
	float gust_secondary_phase = TAU * (
		dot(grass_root_world_position, gust_secondary_direction) *
			wind_wave_scale * 0.68 +
		gust_secondary_time
	) + (grass_gust_shape.y - 0.5) * 3.2;
	float gust_primary = sin(gust_primary_phase) * 0.5 + 0.5;
	float gust_secondary = sin(gust_secondary_phase) * 0.5 + 0.5;
	float gust_field = sqrt(max(gust_primary * gust_secondary, 0.0));
	float gust_threshold = mix(
		wind_gust_threshold + 0.08,
		wind_gust_threshold - 0.02,
		gust_activity
	);
	float wind_gust = smoothstep(
		gust_threshold - wind_gust_softness,
		gust_threshold + wind_gust_softness,
		gust_field
	);
	wind_gust *= mix(
		0.18,
		1.0,
		gust_activity
	);
	wind_gust *= mix(
		0.72,
		1.0,
		smoothstep(0.24, 0.78, grass_gust_shape.y)
	);

	vec3 grass_color = mix(
		grass_shadow_color.rgb,
		grass_base_color.rgb,
		painted_value * 0.82);
	grass_color = mix(grass_color, grass_light_color.rgb, blade_tip * 0.27);
	grass_color = mix(grass_color, grass_cool_color.rgb, cool_patch * 0.32);
	grass_color = mix(grass_color, grass_warm_color.rgb, warm_patch * 0.25);
	grass_color = mix(grass_color, grass_shadow_color.rgb, wind_shadow * 0.08);
	float dry_patch = texture(
		wind_noise,
		grass_world_position.xz * patch_scale * 0.63 + vec2(0.19, 0.71)
	).r;
	float dry_light_patch = texture(
		wind_noise,
		grass_world_position.xz * patch_scale * 1.18 + vec2(0.67, 0.23)
	).r;
	float green_patch = texture(
		wind_noise,
		grass_world_position.xz * patch_scale * 0.72 + vec2(0.43, 0.89)
	).r;
	vec3 varied_dry_color = mix(
		grass_dry_shadow_color.rgb,
		grass_dry_color.rgb,
		smoothstep(0.05, 0.90, dry_patch));
	varied_dry_color = mix(
		varied_dry_color,
		grass_dry_light_color.rgb,
		smoothstep(0.30, 0.92, dry_light_patch) * 0.34);
	varied_dry_color = mix(
		varied_dry_color,
		grass_dry_green_color.rgb,
		0.05);
	varied_dry_color = mix(
		varied_dry_color,
		grass_dry_green_color.rgb,
		smoothstep(0.48, 0.90, green_patch) * 0.14);
	float dry_mix_amount =
		smoothstep(0.05, 0.72, blade_dry_amount) * 0.88;
	float unplanted_green_patch =
		(1.0 - step(0.5, grass_state.w)) *
		smoothstep(0.55, 0.86, green_patch);
	dry_mix_amount *= 1.0 - unplanted_green_patch * 0.30;
	grass_color = mix(grass_color, varied_dry_color, dry_mix_amount);
	float wind_light_amount =
		wind_gust * wind_gust_strength * mix(0.72, 1.0, blade_tip);
	grass_color = mix(
		grass_color,
		grass_wind_light_color.rgb,
		wind_light_amount
	);

	ALBEDO = grass_color;
	ROUGHNESS = 1.0;
	SPECULAR = 0.0;
}

void light() {
	float light_facing = dot(NORMAL, LIGHT);
	float light_band = smoothstep(
		toon_threshold - toon_softness,
		toon_threshold + toon_softness,
		light_facing
	);
	vec3 toon_value = mix(vec3(toon_shadow_strength), vec3(1.0), light_band);
	DIFFUSE_LIGHT += toon_value * ATTENUATION * LIGHT_COLOR;
}
