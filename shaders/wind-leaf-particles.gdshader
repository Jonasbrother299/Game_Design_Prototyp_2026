shader_type particles;

uniform vec3 emission_center = vec3(0.0, 3.2, 0.0);
uniform vec3 emission_box = vec3(34.0, 7.0, 28.0);
uniform vec3 wind_direction = vec3(1.0, 0.0, 0.0);
uniform float wind_intensity : hint_range(0.0, 1.0) = 0.0;
uniform float gust_strength : hint_range(0.0, 1.0) = 0.5;
uniform float speed_min = 4.8;
uniform float speed_max = 8.8;
uniform float fall_speed_min = 0.25;
uniform float fall_speed_max = 0.75;
uniform float vertical_wobble = 0.7;
uniform float side_wobble = 0.55;
uniform float wobble_frequency_min = 1.1;
uniform float wobble_frequency_max = 2.7;
uniform float lateral_spread = 0.72;
uniform float vertical_spread = 0.22;
uniform float size_min = 0.16;
uniform float size_max = 0.46;
uniform float spin_min = 1.2;
uniform float spin_max = 4.3;

uniform vec4 color_one : source_color = vec4(0.69, 0.45, 0.12, 0.94);
uniform vec4 color_two : source_color = vec4(0.48, 0.58, 0.16, 0.92);
uniform vec4 color_three : source_color = vec4(0.72, 0.25, 0.12, 0.92);
uniform vec4 color_four : source_color = vec4(0.35, 0.18, 0.10, 0.90);

uint hash(uint value) {
	value = ((value >> uint(16)) ^ value) * uint(73244475);
	value = ((value >> uint(16)) ^ value) * uint(73244475);
	value = (value >> uint(16)) ^ value;
	return value;
}

float rand_from_seed(inout uint seed) {
	int state = int(seed);
	if (state == 0) {
		state = 305420679;
	}

	int quotient = state / 127773;
	state = 16807 * (state - quotient * 127773) - 2836 * quotient;
	if (state < 0) {
		state += 2147483647;
	}

	seed = uint(state);
	return float(seed % uint(65536)) / 65535.0;
}

mat3 rotation_x(float angle) {
	float cosine = cos(angle);
	float sine = sin(angle);
	return mat3(
		vec3(1.0, 0.0, 0.0),
		vec3(0.0, cosine, -sine),
		vec3(0.0, sine, cosine)
	);
}

mat3 rotation_y(float angle) {
	float cosine = cos(angle);
	float sine = sin(angle);
	return mat3(
		vec3(cosine, 0.0, sine),
		vec3(0.0, 1.0, 0.0),
		vec3(-sine, 0.0, cosine)
	);
}

mat3 rotation_z(float angle) {
	float cosine = cos(angle);
	float sine = sin(angle);
	return mat3(
		vec3(cosine, -sine, 0.0),
		vec3(sine, cosine, 0.0),
		vec3(0.0, 0.0, 1.0)
	);
}

void start() {
	uint seed = hash(NUMBER + RANDOM_SEED + uint(1));

	if (RESTART_POSITION) {
		float edge_selector = rand_from_seed(seed);
		float edge_sign = step(0.5, rand_from_seed(seed)) * 2.0 - 1.0;
		vec3 random_position;
		if (edge_selector < 0.55) {
			float edge_distance = mix(
				0.72,
				1.0,
				rand_from_seed(seed)) * emission_box.x * 0.5;
			random_position = vec3(
				edge_sign * edge_distance,
				(rand_from_seed(seed) - 0.5) * emission_box.y,
				(rand_from_seed(seed) - 0.5) * emission_box.z
			);
		} else {
			float edge_distance = mix(
				0.72,
				1.0,
				rand_from_seed(seed)) * emission_box.y * 0.5;
			random_position = vec3(
				(rand_from_seed(seed) - 0.5) * emission_box.x,
				edge_sign * edge_distance,
				(rand_from_seed(seed) - 0.5) * emission_box.z
			);
		}
		TRANSFORM[3].xyz = emission_center + random_position;
	}

	if (RESTART_ROT_SCALE) {
		float size_random = pow(rand_from_seed(seed), 1.7);
		float particle_size = mix(size_min, size_max, size_random);
		if (rand_from_seed(seed) > 0.93) {
			particle_size *= 1.55;
		}

		mat3 initial_rotation =
			rotation_y(rand_from_seed(seed) * TAU) *
			rotation_x((rand_from_seed(seed) - 0.5) * 1.4) *
			rotation_z(rand_from_seed(seed) * TAU);
		TRANSFORM[0].xyz = initial_rotation * vec3(particle_size, 0.0, 0.0);
		TRANSFORM[1].xyz = initial_rotation * vec3(0.0, particle_size, 0.0);
		TRANSFORM[2].xyz = initial_rotation * vec3(0.0, 0.0, particle_size);

		USERDATA1 = vec4(
			0.0,
			rand_from_seed(seed) * TAU,
			mix(speed_min, speed_max, rand_from_seed(seed)),
			mix(
				wobble_frequency_min,
				wobble_frequency_max,
				rand_from_seed(seed))
		);

		float spin_direction_x = step(0.5, rand_from_seed(seed)) * 2.0 - 1.0;
		float spin_direction_y = step(0.5, rand_from_seed(seed)) * 2.0 - 1.0;
		float spin_direction_z = step(0.5, rand_from_seed(seed)) * 2.0 - 1.0;
		USERDATA2 = vec4(
			mix(spin_min, spin_max, rand_from_seed(seed)) * spin_direction_x,
			mix(spin_min, spin_max, rand_from_seed(seed)) * spin_direction_y,
			mix(spin_min, spin_max, rand_from_seed(seed)) * spin_direction_z,
			mix(fall_speed_min, fall_speed_max, rand_from_seed(seed))
		);
		float lateral_random = rand_from_seed(seed) * 2.0 - 1.0;
		float lateral_direction = sign(lateral_random) *
			pow(abs(lateral_random), 1.5) * lateral_spread;
		float vertical_direction =
			(rand_from_seed(seed) * 2.0 - 1.0) * vertical_spread;
		USERDATA3 = vec4(
			mix(0.45, 1.0, rand_from_seed(seed)),
			0.0,
			lateral_direction,
			vertical_direction
		);
	}

	if (RESTART_VELOCITY) {
		VELOCITY = vec3(0.0);
	}

	if (RESTART_COLOR) {
		float color_selector = rand_from_seed(seed);
		if (color_selector < 0.25) {
			COLOR = color_one;
		} else if (color_selector < 0.5) {
			COLOR = color_two;
		} else if (color_selector < 0.75) {
			COLOR = color_three;
		} else {
			COLOR = color_four;
		}
		USERDATA3.y = COLOR.a;
	}
}

void process() {
	USERDATA1.x += DELTA;
	float normalized_age = clamp(USERDATA1.x / LIFETIME, 0.0, 1.0);

	vec3 direction = vec3(wind_direction.x, 0.0, wind_direction.z);
	if (length(direction) < 0.001) {
		direction = vec3(1.0, 0.0, 0.0);
	} else {
		direction = normalize(direction);
	}
	vec3 particle_direction = normalize(
		direction + vec3(USERDATA3.z, USERDATA3.w, 0.0));
	vec3 side_direction = vec3(-direction.z, 0.0, direction.x);

	float gust_multiplier = mix(0.72, 1.16, gust_strength);
	float intensity_multiplier = mix(0.58, 1.0, wind_intensity);
	float vertical_motion =
		sin(TIME * USERDATA1.w + USERDATA1.y) * vertical_wobble;
	float side_motion =
		sin(TIME * USERDATA1.w * 0.73 + USERDATA1.y * 1.9) *
		side_wobble * USERDATA3.x;
	VELOCITY =
		particle_direction * USERDATA1.z *
			gust_multiplier * intensity_multiplier +
		side_direction * side_motion +
		vec3(0.0, vertical_motion - USERDATA2.w, 0.0);

	mat3 tumble =
		rotation_y(USERDATA2.y * DELTA) *
		rotation_x(USERDATA2.x * DELTA) *
		rotation_z(USERDATA2.z * DELTA);
	TRANSFORM[0].xyz = tumble * TRANSFORM[0].xyz;
	TRANSFORM[1].xyz = tumble * TRANSFORM[1].xyz;
	TRANSFORM[2].xyz = tumble * TRANSFORM[2].xyz;

	float fade_in = smoothstep(0.0, 0.08, normalized_age);
	float fade_out = 1.0 - smoothstep(0.72, 1.0, normalized_age);
	COLOR.a =
		USERDATA3.y * fade_in * fade_out *
		clamp(wind_intensity, 0.0, 1.0);
}
