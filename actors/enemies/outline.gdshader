shader_type spatial;
render_mode unshaded;

uniform float weight: hint_range(0.0, 10.0, 0.1) = 1.0;
uniform vec4 color: source_color = vec4(0.75, 0.55, 0.0, 1.0);
uniform float intensity = 4.0;

uniform float scale = 1.0;
uniform float depth_normal_threshold = 0.1;
uniform float depth_normal_threshold_scale = 3.0;
uniform float depth_threshold = 1.5;
uniform float normal_threshold = 2.0;

uniform sampler2D screen_texture: hint_screen_texture, filter_linear_mipmap, repeat_disable;
uniform sampler2D depth_texture: hint_depth_texture, filter_linear_mipmap, repeat_disable;
uniform sampler2D normal_roughness_texture: hint_normal_roughness_texture, repeat_disable;

//void vertex() {
  //VERTEX += NORMAL * weight * COLOR.r;
//}

void fragment() {
  float half_scale_floor = floor(scale * 0.5);
  float half_scale_ceil = floor(scale * 0.5);
  
  vec2 texel_size = vec2(1.0) / vec2(textureSize(screen_texture, 0));
  
  vec2 uv_bottom_left = SCREEN_UV - texel_size * half_scale_floor;
  vec2 uv_top_right = SCREEN_UV + texel_size * half_scale_ceil;
  vec2 uv_bottom_right = SCREEN_UV + vec2(texel_size.x * half_scale_ceil, -texel_size.y * half_scale_floor);
  vec2 uv_top_left = SCREEN_UV + vec2(-texel_size.x * half_scale_floor, texel_size.y * half_scale_ceil);
  
  vec3 normal0 = texture(normal_roughness_texture, uv_bottom_left).rgb;
  vec3 normal1 = texture(normal_roughness_texture, uv_top_right).rgb;
  vec3 normal2 = texture(normal_roughness_texture, uv_bottom_right).rgb;
  vec3 normal3 = texture(normal_roughness_texture, uv_top_left).rgb;
  
  float depth0 = texture(depth_texture, uv_bottom_left).r;
  float depth1 = texture(depth_texture, uv_top_right).r;
  float depth2 = texture(depth_texture, uv_bottom_right).r;
  float depth3 = texture(depth_texture, uv_top_left).r;
  
  vec3 view_normal = normal0 * 2.0 - 1.0;
  float view_dot = 1.0 - dot(view_normal, VIEW);
  
  float n_threshold = clamp(((view_dot - depth_normal_threshold) / (1.0 - depth_normal_threshold)), 0.0, 1.0);
  n_threshold = n_threshold * depth_normal_threshold_scale + 1.0;
  float d_threshold = depth_threshold * depth0 * n_threshold;
  
  float depth_finite_difference0 = depth1 - depth0;
  float depth_finite_difference1 = depth3 - depth2;
  
  float edge_depth = sqrt(pow(depth_finite_difference0, 2.0) + pow(depth_finite_difference1, 2.0)) * 100.0;
  edge_depth = edge_depth > d_threshold ? 1.0 : 0.0;
  
  vec3 normal_finite_difference0 = normal1 - normal0;
  vec3 normal_finite_difference1 = normal3 - normal2;
  
  float edge_normal = sqrt(dot(normal_finite_difference0, normal_finite_difference0) + dot(normal_finite_difference1, normal_finite_difference1));
  edge_normal = edge_normal > n_threshold ? 1.0 : 0.0;
  
  float edge = max(edge_depth, edge_normal);
  
  vec2 offsets[4] = {
    vec2(texel_size.x, 0.0),
    vec2(-texel_size.x, 0.0),
    vec2(0.0, texel_size.y),
    vec2(0.0, -texel_size.y)
  };
  
  for(int i = 0; i < 4; i++) {
    vec2 offset_uv = SCREEN_UV + offsets[i] * weight;
    
    float depth_n = texture(depth_texture, offset_uv).r;
    vec3 normal_n = texture(normal_roughness_texture, offset_uv).rgb;
    
    float depth_diff = depth_n - depth0;
    vec3 normal_diff = normal_n - normal0;
    
    float edge_depth_n = (sqrt(depth_diff * depth_diff) * 100.0 > d_threshold) ? 1.0 : 0.0;
    float edge_normal_n = (length(normal_diff) > normal_threshold) ? 1.0 : 0.0;
    
    edge = max(edge, max(edge_depth_n, edge_normal_n));
  }
  
  vec4 edge_color = vec4(color.rgb, color.a * edge);
  
  ALBEDO = mix(texture(screen_texture, SCREEN_UV).rgb, edge_color.rgb * pow(2.0, intensity), edge_color.a);
  
  //ALPHA = color.a;
  //ALBEDO = color.rgb * pow(2, intensity);
}
