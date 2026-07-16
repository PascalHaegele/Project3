shader_type sky;

uniform sampler2D sky_tex: source_color, filter_linear, hint_default_black;

uniform vec3 background_color: source_color;
uniform vec3 accent_color: source_color;
uniform float accent_intensitiy: hint_range(0.0, 10.0, 0.1);

void sky() {
	vec3 col = texture(sky_tex, SKY_COORDS).rgb;
	COLOR = col;
	vec3 col2 = COLOR * 2.0 + accent_color * accent_intensitiy;
	COLOR *= col2;
	if(length(COLOR) < 0.1) {
		COLOR = background_color;
	}
}
