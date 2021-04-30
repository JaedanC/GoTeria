shader_type canvas_item;

uniform sampler2D light_values;

void fragment() {
	// UV is a value between 0 and 1 already
	
	vec4 colour = texture(light_values, UV);
	float alpha = 0.25;
	
//	COLOR = vec4(colour.xyza);
//	COLOR = vec4(colour.xyz, 0.5);
	COLOR = vec4(vec3(0.0), alpha);
}