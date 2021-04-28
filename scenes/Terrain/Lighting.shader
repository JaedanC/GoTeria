shader_type canvas_item;

uniform sampler2D light_values;
uniform vec2 block_pixel_size;

void fragment() {
	
	vec4 colour = texture(light_values, UV);
//	float alpha = 0.5;
	
//	COLOR = vec4(colour.xyza);
	COLOR = vec4(colour.xyz, 0.5);
//	COLOR = vec4(vec3(0.0), alpha);
//	COLOR = vec4(vec3(0.0), 0);
	
}