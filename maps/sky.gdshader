shader_type sky;

uniform sampler2D sky_tex: source_color, filter_linear, hint_default_black;
uniform sampler2DArray sun_tex: source_color, filter_linear, hint_default_black;

uniform float sun_tex_layer: hint_range(0.0, 60.0, 1.0) = 1.0;
uniform float sun_size: hint_range(0.0, 1.0, 0.001) = 0.99;
uniform float sun_softness: hint_range(0.0, 0.05, 0.001) = 0.002;

uniform vec3 background_color: source_color = vec3(0.0);
uniform vec3 accent_color: source_color = vec3(0.591, 0.342, 0.067);
uniform float accent_intensity: hint_range(0.0, 10.0, 0.1);

void sky() {
	vec3 col = texture(sky_tex, SKY_COORDS).rgb;
	COLOR = col;
	vec3 col2 = COLOR * 2.0 + accent_color * accent_intensity;
	COLOR *= col2;
	if(length(COLOR) < 0.1) {
		COLOR = background_color;
	}
	float sun_dot = max(0.0, dot(EYEDIR, LIGHT0_DIRECTION));
	float sun = smoothstep(sun_size, sun_size + sun_softness, sun_dot);
	COLOR += sun * texture(sun_tex, vec3(EYEDIR.xz * 4.4 + vec2(0.4), sun_tex_layer)).rgb;
}
