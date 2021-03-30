extends Node2D


export(Vector2) var block_pixel_size
export(Vector2) var world_size_in_chunks
export(Vector2) var chunk_block_count
var chunk_scene = preload("res://scenes/Chunk/Chunk.tscn")

"""
The premise behind these dictionaries are too improve performance when streaming
in chunks behind the scenes.

`lightly_loading_blocks_chunks`
- These are slowly loading in their blocks over a series of frames. These happen
from a large distance away. Mediumly off the screen.

`lightly_loaded_blocks_chunks`
- The chunks from above are sent to here when they are done loading their blocks.

`lightly_loading_drawing_chunks`
- These chunks are drawing their blocks. These happen from a short distance away,
just off the screen

`urgently_loading_chunks`
- These chunks are so close to the player that they need to have their blocks
loaded in right away. The drawing can happen later though

`loaded_chunks`
- These chunks are fully drawn and visible to the player.
"""
var loaded_chunks = {}
var loading_chunks = {}
var lightly_loading_blocks_chunks = {}
var lightly_loaded_blocks_chunks = {}
var lightly_loading_drawing_chunks = {}
var urgently_loading_blocks_chunks = {}

var player = null

var world_image : Image
var chunk_pixel_dimensions = null

func _ready():
	player = get_tree().get_root().find_node("Player", true, false)
	
	var world_texture = load("res://blocks.png")
#	var world_texture = load("res://solid.png")
#	var world_texture = load("res://small.png")
#	var world_texture = load("res://medium.png")
#	var world_texture = load("res://hd.png")
	world_image = world_texture.get_data()
	chunk_pixel_dimensions = block_pixel_size * chunk_block_count
#	generate_world()


func _process(_delta):
	# Save the all chunks loaded in memory to a file.
	if InputLayering.pop_action("save_world"):
		for chunk in get_children():
			chunk.save_chunk()
		print("Finished Saving to file")
#	free_all_invisible_chunks()
	new_free_invisible_chunks()
#	create_visible_chunks()
	
	
#	loaded_chunks.clear()
	loading_chunks.clear()
	lightly_loading_blocks_chunks.clear()
	lightly_loaded_blocks_chunks.clear()
	lightly_loading_drawing_chunks.clear()
	urgently_loading_blocks_chunks.clear()
	new_create_loading_regions()
	old_create_chunks()
#	new_create_null_chunks()
#	new_continue_loading_regions()
	update()

func old_create_chunks():
	for point in lightly_loading_blocks_chunks.keys() + lightly_loading_drawing_chunks.keys() + urgently_loading_blocks_chunks.keys():
		var world_image_in_chunks = world_image.get_size() / chunk_block_count
		if (point.x < 0 || point.y < 0 || point.x >= world_image_in_chunks.x || point.y >= world_image_in_chunks.y):
			continue
		
#		print(world_image, point, chunk_block_count, chunk_pixel_dimensions)

		if !loaded_chunks.has(point):
			var chunk = chunk_scene.instance()
			add_child(chunk)
			chunk.init(
				world_image,
				point,
				chunk_block_count,
				block_pixel_size
			)
#			chunk.stream_all()
#			chunk.update()
			loaded_chunks[point] = chunk

func new_free_invisible_chunks():
	"""
	This method uses the visibility points to determine which chunks should be
	unloaded from memory.
	"""
	
	# First we grab a set the world_positions that should be loaded in the game 
	var visibility_points = player.get_visibility_points(2)
	
	# Create a temporary dictionary to store chunks that are already loaded
	# and should stay loaded
	var visible_chunks = {}
	
	# Loop through the loaded chunks and store the ones that we should keep in
	# visible_chunks while erasing them from the old loaded_chunks dictionary. 
	for visible_point in visibility_points:
		if loaded_chunks.has(visible_point):
			visible_chunks[visible_point] = loaded_chunks[visible_point]
			loaded_chunks.erase(visible_point)
	
	# Now the remaining chunks in loaded_chunks are invisible to the player
	# and can be deleted from memory. The delete_chunk method will handle this
	# for us.
	for invisible_point in loaded_chunks.keys():
		loaded_chunks[invisible_point].queue_free()
		loaded_chunks.erase(invisible_point)
	
	# Finally, our new visible chunks dictionary becomes the loaded chunks
	# dictionary
	loaded_chunks = visible_chunks
	

func _draw():
	for point in lightly_loading_blocks_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.green, false, 10, false)
#		draw_circle(point, 10, Color.green)
	
	for point in lightly_loading_drawing_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.orange, false, 10, false)
#		draw_circle(point, 10, Color.orange)
	
	for point in urgently_loading_blocks_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.red, false, 10, false)
#		draw_circle(point, 10, Color.red)

func new_create_loading_regions():
	var player_visibility_points = player.get_visibility_points()
	
	# TODO: This requires the implementation to be very specific. Find a better
	# way to find the top left and bottom right points
	var top_left_point = player_visibility_points[0]
	var bottom_right_point = player_visibility_points[-1]
	
	var light_loading_block_top_left = top_left_point - Vector2(2, 2)
	var light_loading_block_bottom_right = bottom_right_point + Vector2(2, 2)
	var light_loading_drawing_top_left = top_left_point - Vector2(1, 1)
	var light_loading_drawing_bottom_right = bottom_right_point + Vector2(1, 1)
	
	for i in range(light_loading_block_top_left.x, light_loading_block_bottom_right.x):
		for j in range(light_loading_block_top_left.y, light_loading_block_bottom_right.y):
			var point = Vector2(i, j)
			if point.x < 0 or point.y < 0:
				continue
				
			if (i == light_loading_block_top_left.x ||
				i == light_loading_block_bottom_right.x - 1 ||
				j == light_loading_block_top_left.y ||
				j == light_loading_block_bottom_right.y - 1):
				lightly_loading_blocks_chunks[point] = null
				continue
				
			if (i == light_loading_block_top_left.x + 1 ||
				i == light_loading_block_bottom_right.x - 2 ||
				j == light_loading_block_top_left.y + 1 ||
				j == light_loading_block_bottom_right.y - 2):
				lightly_loading_drawing_chunks[point] = null
				continue
			
			urgently_loading_blocks_chunks[point] = null

func new_create_null_chunks():
	for point in lightly_loading_blocks_chunks.keys():
		var chunk = lightly_loading_blocks_chunks[point]
		
		if (chunk == null):
			chunk = chunk_scene.instance()
			lightly_loading_blocks_chunks[point] = chunk
			add_child(chunk)
			chunk.init_stream(
				world_image,
				point,
				chunk_block_count,
				chunk_pixel_dimensions
			)
		loaded_chunks[point] = chunk
	
	for point in lightly_loading_drawing_chunks.keys():
		var chunk = lightly_loading_drawing_chunks[point]
		
		if (chunk == null):
			chunk = chunk_scene.instance()
			lightly_loading_drawing_chunks[point] = chunk
			add_child(chunk)
			chunk.init_stream(
				world_image,
				point,
				chunk_block_count,
				chunk_pixel_dimensions
			)
			chunk.stream_all()
		loaded_chunks[point] = chunk
	
	for point in urgently_loading_blocks_chunks.keys():
		var chunk = urgently_loading_blocks_chunks[point]
		
		if (chunk == null):
			chunk = chunk_scene.instance()
			urgently_loading_blocks_chunks[point] = chunk
			add_child(chunk)
			chunk.init_stream(
				world_image,
				point,
				chunk_block_count,
				chunk_pixel_dimensions
			)
			chunk.stream_all()
			chunk.update()
		loaded_chunks[point] = chunk

func new_continue_loading_regions():
	# These are the lightly loaded block chunks
	var blocks_to_load = 1000
	for point in lightly_loading_blocks_chunks.keys():
		var chunk = lightly_loading_blocks_chunks[point]
		
		var is_loaded = chunk.stream(blocks_to_load)
		blocks_to_load = blocks_to_load - chunk.get_loaded_blocks()
		if is_loaded:
			lightly_loading_drawing_chunks[point] = chunk
			lightly_loading_blocks_chunks.erase(point)
		if blocks_to_load <= 0:
			break
	
	# These are the blocks ready to be drawn
	var blocks_to_draw = 1
	for point in lightly_loading_drawing_chunks.keys():
		if (blocks_to_draw == 0):
			break
		
		var chunk = lightly_loading_drawing_chunks[point]
		
		chunk.update() # Let the chunk draw
		lightly_loading_drawing_chunks.erase(point)
		loaded_chunks[point] = chunk
		blocks_to_draw -= 1

func free_all_invisible_chunks():
	"""
	This method uses the visibility points to determine which chunks should be
	unloaded from memory.
	"""
	
	# First we grab a set the world_positions that should be loaded in the game 
	var visibility_points = player.get_visibility_points()
	
	# Create a temporary dictionary to store chunks that are already loaded
	# and should stay loaded
	var visible_chunks = {}
	
	# Loop through the loaded chunks and store the ones that we should keep in
	# visible_chunks while erasing them from the old loaded_chunks dictionary. 
	for visible_point in visibility_points:
		if loaded_chunks.has(visible_point):
			visible_chunks[visible_point] = loaded_chunks[visible_point]
			loaded_chunks.erase(visible_point)
	
	# Now the remaining chunks in loaded_chunks are invisible to the player
	# and can be deleted from memory. The delete_chunk method will handle this
	# for us.
	for invisible_point in loaded_chunks.keys():
#		self.remove_child(loaded_chunks[world_position])
		delete_chunk(loaded_chunks[invisible_point])
	
	# Finally, our new visible chunks dictionary becomes the loaded chunks
	# dictionary
	loaded_chunks = visible_chunks

func delete_chunk(chunk):
	"""
	This method deletes the chunk passed in and removed the reference to this
	chunk in the loaded_chunks dictionary.
	"""
	loaded_chunks.erase(chunk.chunk_position)
	chunk.queue_free()

func generate_world():
	"""
	This method creates a sample world and saves it to disk. The chunks created
	are not kept around and thus this is where world generation will eventually
	go. TODO: What happens to the created chunks? Do they persist in memory or
	are they collected?
	"""
	for i in range(world_size_in_chunks.x):
		for j in range(world_size_in_chunks.y):
			var chunk = create_chunk(Vector2(i, j))
			chunk.save_chunk()

func create_visible_chunks():
	"""
	This method uses the visibility points to load chunks from memory if they
	not already loaded. Loaded chunks are added to the loaded_chunk dictionary
	and added as children to this scene. Points including negative values
	will be rejected by create_chunk so we need to check if the return is null
	before we add it to the tree.
	"""
	var visibility_points = player.get_visibility_points()
	for point in visibility_points:
		if !loaded_chunks.has(point):
			var _result = create_chunk(point)

func create_chunk(chunk_position : Vector2) -> bool:
	"""
	This method creates and loads a chunk based on the chunk_position Vector2.
	It will return null if the x or y value is negative. Otherwise it will add
	the chunk to the loaded_chunks dictionary. returns whether the chunk exists
	"""
	# Input checks. TODO: World Boundaries
	if chunk_position.x < 0 or chunk_position.y < 0:
		return false

	# Check if it's already loaded
	if loaded_chunks.has(chunk_position):
		return true

	# Instance a chunk
	var chunk = chunk_scene.instance()
	add_child(chunk)
	chunk.init_stream(
		world_image,
		chunk_position,
		chunk_block_count,
		block_pixel_size
	)
	# Cache the chunk for future reference
	loaded_chunks[chunk_position] = chunk
	return true

func get_chunk_pixel_dimensions() -> Vector2:
	"""
	Returns the size of a chunk in pixels as a Vector2
	"""
	return chunk_pixel_dimensions

func get_chunk_from_chunk_position(chunk_position : Vector2):
#	if create_chunk(chunk_position):
	if loaded_chunks.has(chunk_position):
		return loaded_chunks[chunk_position]
	return null

func get_chunk_from_world_position(world_position : Vector2):
	return get_chunk_from_chunk_position(get_chunk_position_from_world_position(world_position))

func get_block_from_world_position(world_position : Vector2):
	var chunk_position = get_chunk_position_from_world_position(world_position)
	var block_position = get_block_position_from_world_position(world_position)
	return get_block_from_chunk_position_and_block_position(chunk_position, block_position)

func get_block_from_chunk_position_and_block_position(chunk_position : Vector2, block_position : Vector2):
	var chunk = get_chunk_from_chunk_position(chunk_position)
	if !chunk:
		return null
	if chunk.blocks.has(block_position):
		return chunk.blocks[block_position]
	return null

func get_block_position_from_world_position(world_position : Vector2):
	var chunk_position = get_chunk_position_from_world_position(world_position)
	var block_position = (world_position - chunk_position * get_chunk_pixel_dimensions()).floor()
	return (block_position / block_pixel_size).floor()

func get_chunk_position_from_world_position(world_position : Vector2) -> Vector2:
	return (world_position / get_chunk_pixel_dimensions()).floor()

func set_block_at_world_position(world_position : Vector2, new_block : Dictionary):
	var chunk = get_chunk_from_world_position(world_position)
	if chunk:
		var block_position = get_block_position_from_world_position(world_position)
		chunk.set_block_from_block_position(block_position, new_block)
	return null

