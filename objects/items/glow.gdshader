shader_type spatial;
render_mode unshaded, shadows_disabled;

uniform vec3 main_color: source_color;

uniform sampler2D transparency_texture;

void vertex() {
	mat4 modified_model_view = VIEW_MATRIX * mat4(
		INV_VIEW_MATRIX[0],
		INV_VIEW_MATRIX[1],
		INV_VIEW_MATRIX[2],
		MODEL_MATRIX[3]
	);
	MODELVIEW_MATRIX = modified_model_view;
}

void fragment() {
	float val = mix(0.3, 1.3, (cos(TIME * 4.0) + 1.0) * 0.5);
	ALPHA = texture(transparency_texture, UV).r * val;
	ALBEDO = main_color;
}
