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
var lightly_loading_blocks_chunks = {}
var lightly_loaded_blocks_chunks = {}
var lightly_loading_drawing_chunks = {}
var urgently_loading_blocks_chunks = {}

var load_margin = 2
var draw_margin = 2

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
	new_free_invisible_chunks()
	
	
#	loaded_chunks.clear()
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
	"""
	This method crudely loads in all the chunks that the player can see with a
	margin of 2. TODO: Stream in the loading of the Chunks and the drawing over
	multiple frames to increase performance.
	"""
#	for point in lightly_loading_blocks_chunks.keys() + lightly_loading_drawing_chunks.keys() + urgently_loading_blocks_chunks.keys():
	for point in player.get_visibility_points(draw_margin):
		var world_image_in_chunks = world_image.get_size() / chunk_block_count
		if (point.x < 0 || point.y < 0 || point.x >= world_image_in_chunks.x || point.y >= world_image_in_chunks.y):
			continue
		
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
	var visibility_points = player.get_visibility_points(draw_margin)
	
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
	"""
	This currently colours the chunks with a border donoting the kind of chunk it
	is and how it should be streamed in. Changing the visibility_points method
	will allow you to see this process in action.
	"""
	for point in lightly_loading_blocks_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.green, false, 10, false)
	
	for point in lightly_loading_drawing_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.orange, false, 10, false)
	
	for point in urgently_loading_blocks_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.red, false, 10, false)

func new_create_loading_regions():
	var load_visibility_points = player.get_visibility_points(load_margin + draw_margin)
	var draw_visibility_points = player.get_visibility_points(draw_margin)
	var urgent_visibility_points = player.get_visibility_points()
	
	for point in urgent_visibility_points:
		load_visibility_points.erase(point)
		draw_visibility_points.erase(point)
	
	for point in draw_visibility_points:
		load_visibility_points.erase(point)
	
	for point in urgent_visibility_points:
		urgently_loading_blocks_chunks[point] = null
	
	for point in draw_visibility_points:
		lightly_loading_drawing_chunks[point] = null
	
	for point in load_visibility_points:
		lightly_loading_blocks_chunks[point] = null


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

