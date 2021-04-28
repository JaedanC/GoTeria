extends Sprite

onready var player = get_tree().get_root().find_node("Player", true, false)
onready var terrain = get_parent()

var light_image = null 

func _ready():
	self.light_image = Image.new()

func _physics_process(_delta):
	refresh_shader_canvas_size()
	
	# Absolute machine killer this function
	create_light_image(Color.aqua)
	refresh_light_image_from_terrain_luminance()
	
	var light_texture = ImageTexture.new()
	light_texture.create_from_image(light_image, 0) # Flag 0. No texture filtering
	material.set_shader_param("block_pixel_size", terrain.block_pixel_size)
	material.set_shader_param("light_values", light_texture)

func create_light_image(fill_colour: Color = Color.red):
	var light_size = self.scale / terrain.get_block_pixel_size()
	
	var lazy_load = self.light_image == null or light_size.x != self.light_image.get_width() or light_size.y != self.light_image.get_height()
	
	# TODO: Could change this back to lazy load.
	if true or lazy_load:
		self.light_image.create(light_size.x, light_size.y, false, Image.FORMAT_RGBA8)
		self.light_image.fill(fill_colour)
#		print("Recreating luminance image of size: " + str(light_size) + " for screen scale: " + str(self.scale))

func refresh_shader_canvas_size():
	var points = player.get_visibility_points()
	
	# TODO: Change this. Too Hacky
	var min_point = points[0] * terrain.get_chunk_pixel_dimensions()
	var max_point = (points[-1] + Vector2(1, 1)) * terrain.get_chunk_pixel_dimensions()
	
	self.position = min_point
	self.scale = max_point - min_point

func min_vector2(first: Vector2, second: Vector2) -> Vector2:
	"""
	This functions returns the minimum x and y values between the two vectors.
	"""
	return Vector2(
		min(first.x, second.x),
		min(first.y, second.y)
	)

func refresh_light_image_from_terrain_luminance():
	var points = player.get_visibility_points()
	
	var blocks_on_screen = self.scale / terrain.get_block_pixel_size()
	
	# This makes sure we only copy from pixels inside the range of the world at 
	# the negative x and y values
	var top_left_block = points[0] * terrain.get_chunk_block_count()
	var top_left_out_of_bounds_offset = Vector2.ZERO
	if top_left_block.x < 0:
		top_left_out_of_bounds_offset = Vector2(abs(top_left_block.x), top_left_out_of_bounds_offset.y)
		blocks_on_screen.x -= min(top_left_out_of_bounds_offset.x, blocks_on_screen.x)
	if top_left_block.y < 0:
		top_left_out_of_bounds_offset = Vector2(top_left_out_of_bounds_offset.x, abs(top_left_block.y))
		blocks_on_screen.y -= min(top_left_out_of_bounds_offset.y, blocks_on_screen.y)
	
	# This makes sure we only copy from pixels inside the range of the world at
	# the x and y values that are greater than the size of the world
	
	blocks_on_screen = min_vector2(
		terrain.get_world_size() - (top_left_block + top_left_out_of_bounds_offset),
		blocks_on_screen
	)
	
	# This rectangle represents the area inside the world_luminance image that we want to copy to the shader.
	var source_luminance_rectangle: Rect2 = Rect2(top_left_block + top_left_out_of_bounds_offset, blocks_on_screen)
#	print("Copying from luminance: " + str(source_luminance_rectangle) + " to " + str(top_left_out_of_bounds_offset))
	
	self.light_image.blit_rect(terrain.world_image_luminance, source_luminance_rectangle, top_left_out_of_bounds_offset)
	
	
	var light_texture = ImageTexture.new()
	light_texture.create_from_image(light_image, 0) # Flag 0. No texture filtering
	material.set_shader_param("light_values", light_texture)


