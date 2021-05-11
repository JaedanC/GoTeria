extends Sprite

onready var player = get_tree().get_root().find_node("Player", true, false)
onready var terrain = get_parent()
onready var thread_pool = get_tree().get_root().find_node("ThreadPool", true, false)

var light_image = null
var light_filled = null
var light_texture: ImageTexture

var run_once = false

func _ready():
	self.light_image = Image.new()
	self.light_texture = ImageTexture.new()
#	create_light_image(Color.white)
	
	set_physics_process(false);
	
func _physics_process(_delta):
	refresh_shader_canvas_size()
	
#	create_light_image(Color.aqua)
	create_light_image(Color.white)
#	refresh_light_image_from_terrain_luminance()
	
	# Absolute machine killer this function
#	create_light_image(Color.aqua)
#	refresh_light_image_from_terrain_luminance()
	
#	var tasks = thread_pool.fetch_finished_tasks_by_tag("light")
#	if not run_once:
#		thread_pool.submit_task_unparameterized(self, "light_emission", "light")
#		run_once = true
#	elif tasks.size() > 0:
	
#	light_emission()
	var tasks = thread_pool.fetch_finished_tasks_by_tag("light")
	if not run_once:
		thread_pool.submit_task_unparameterized(self, "light_emission", "light")
		run_once = true
	elif tasks.size() > 0:
		self.light_filled = tasks[0].get_result()
		thread_pool.submit_task_unparameterized(self, "light_emission", "light")
		
	self.light_texture.create_from_image(self.light_filled, 0) # Flag 0. No texture filtering
#	self.light_texture.create_from_image(light_image) # Use Texture filtering
	material.set_shader_param("light_values_size", self.light_texture.get_size())
	material.set_shader_param("light_values", self.light_texture)
	assert(light_texture != null)
	

func create_light_image(fill_colour: Color = Color.red):
	var light_size = self.scale / terrain.GetBlockPixelSize()
	
	var lazy_load = self.light_image == null or light_size.x != self.light_image.get_width() or light_size.y != self.light_image.get_height()
	
	# TODO: Could change this back to lazy load.
	if true or lazy_load:
		self.light_image.create(light_size.x, light_size.y, false, Image.FORMAT_RGBA8)
		self.light_image.fill(fill_colour)
#		print("Recreating luminance image of size: " + str(light_size) + " for screen scale: " + str(self.scale))

func refresh_shader_canvas_size():
	var chunk_point_corners = player.get_visibility_chunk_position_corners()
	var chunk_point_top_left_in_pixels = chunk_point_corners[0] * terrain.GetChunkPixelDimensions()
	var chunk_point_bottom_right_in_pixels = (chunk_point_corners[1] + Vector2.ONE) * terrain.GetChunkPixelDimensions()
	
	self.position = chunk_point_top_left_in_pixels
	self.scale = chunk_point_bottom_right_in_pixels - chunk_point_top_left_in_pixels

"""
This helper function returns a new Vector2 containing the minimum x and y values
for each component of the two supplied Vector2's.
"""
func min_vector2(first: Vector2, second: Vector2) -> Vector2:
	return Vector2(
		min(first.x, second.x),
		min(first.y, second.y)
	)

func refresh_light_image_from_terrain_luminance():
	var chunk_point_corners = player.get_visibility_chunk_position_corners()
	
	var blocks_on_screen = self.scale / terrain.GetBlockPixelSize()
	var top_left_block = chunk_point_corners[0] * terrain.GetChunkBlockCount()
	
	# This makes sure we only copy from pixels inside the range of the world at 
	# the negative x and y values
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
		terrain.GetWorldSize() - (top_left_block + top_left_out_of_bounds_offset),
		blocks_on_screen
	)
	
	# This rectangle represents the area inside the world_luminance image that we want to copy to the shader.
	var source_luminance_rectangle: Rect2 = Rect2(top_left_block + top_left_out_of_bounds_offset, blocks_on_screen)
#	print("Copying from luminance: " + str(source_luminance_rectangle) + " to " + str(top_left_out_of_bounds_offset))
	
	self.light_image.blit_rect(terrain.world_image_luminance, source_luminance_rectangle, top_left_out_of_bounds_offset)

func emit(image: Image, location: Vector2, level: Color):
	if level.r <= 0:
		return
	if (location.x < 0 or location.y < 0 or
			location.x >= image.get_width() or location.y >= image.get_height()):
		return
	
	var existing_colour = image.get_pixelv(location)
	if level.r < existing_colour.r:
		return
	
	image.set_pixelv(location, level)
	
	var new_colour = Color(level.r - 0.1, level.g - 0.1, level.b - 0.1, 1)
	emit(image, Vector2(location.x - 1, location.y), new_colour)
	emit(image, Vector2(location.x, location.y - 1), new_colour)
	emit(image, Vector2(location.x + 1, location.y), new_colour)
	emit(image, Vector2(location.x, location.y + 1), new_colour)

func light_emission():
	var image = Image.new()
	image.copy_from(self.light_image)
	image.lock()
	for i in range(self.light_image.get_width()):
		for j in range(self.light_image.get_height()):
			var image_position = Vector2(i, j)
			var colour = image.get_pixelv(image_position)
			
			emit(image, image_position, colour)
	image.unlock()
	return image
