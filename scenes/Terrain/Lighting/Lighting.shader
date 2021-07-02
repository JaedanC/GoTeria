shader_type canvas_item;
render_mode blend_mul;

uniform sampler2D light_values;
uniform vec2 light_values_size;

void fragment() {
//	float block_dist_x = 1.0 / light_values_size.x;
//	float block_dist_y = 1.0 / light_values_size.y;
	vec4 colour = vec4(0.0);
	colour += texture(light_values,  UV);
	
	// UV is a value between 0 and 1 already
//	vec2 neighbours[9] = {
//		vec2(0.0),
//		vec2(-block_dist_x, 0.0),
//		vec2(-block_dist_x, -block_dist_y),
//		vec2(0.0, -block_dist_y),
//		vec2(block_dist_x, -block_dist_y),
//		vec2(block_dist_x, 0.0),
//		vec2(block_dist_x, block_dist_y),
//		vec2(0.0, block_dist_y),
//		vec2(-block_dist_x, block_dist_y)
//	};
//
//	vec4 colours[9];
//	for (int i = 0; i < 9; i += 1) {
//		colours[i] = texture(light_values, UV + neighbours[i]);
//	}
//
//	int index = 0;
//	vec4 old_maximum_colour = vec4(0.0);
//	vec4 maximum_colour = vec4(0.0);
//	for (int i = 0; i < 9; i++) {
//		maximum_colour = max(maximum_colour, colours[i]);
//
//		if (maximum_colour != old_maximum_colour) {
//			index = i;
//		}
//
//		old_maximum_colour = maximum_colour;
//	}
//
//	colour = maximum_colour;
//	colour = mix(vec4(1.0), colour, distance(UV, UV + neighbours[index]));
//	colour = mix(colour, vec4(1.0), distance(UV, UV + neighbours[index]));
//	colour = mix(colour, vec4(1.0), fract((UV.x + UV.y) * light_values_size.x));
//	colour = mix(vec4(1.0), colour, fract((UV.x + UV.y) * light_values_size.x));
	
//	colour += texture(light_values, UV);
//	colour += texture(light_values, UV + vec2(-block_dist_x, 0.0));
//	colour += texture(light_values, UV + vec2(-block_dist_x, -block_dist_y));
//	colour += texture(light_values, UV + vec2(0.0, -block_dist_y));
//	colour += texture(light_values, UV + vec2(block_dist_x, -block_dist_y));
//	colour += texture(light_values, UV + vec2(block_dist_x, 0.0));
//	colour += texture(light_values, UV + vec2(block_dist_x, block_dist_y));
//	colour += texture(light_values, UV + vec2(0.0, block_dist_y));
//	colour += texture(light_values, UV + vec2(-block_dist_x, block_dist_y));
	
	
	float alpha = 0.25;
//	float alpha = colour.a;
	
//	colour.a = distance(vec2(0.5), UV);
	
//	COLOR = vec4(colour.xyz, 1);
	COLOR = vec4(colour.xyz, alpha);
//	COLOR = vec4(vec3(0.0), alpha);
}