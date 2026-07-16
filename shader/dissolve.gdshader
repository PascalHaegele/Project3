shader_type spatial;

uniform float t = 0.0;
uniform vec3 emmission_color: source_color = vec3(1.0);

uniform sampler2D noise_tex;
uniform float noise_scale = 1.0;

void fragment() {
	float noise = texture(noise_tex, UV * noise_scale).r;
	float mask = step(t, noise);
	if(mask < 1.0) { discard; }
	EMISSION = emmission_color;
}
