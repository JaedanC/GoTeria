extends Node2D

export(Vector2) var block_pixel_size: Vector2
#export(Vector2) var world_size_in_chunks: Vector2
export(Vector2) var chunk_block_count: Vector2

"""
The premise behind these dictionaries are too improve performance when streaming
in chunks behind the scenes.

`lightly_loading_blocks_chunks`
- These are slowly loading in their blocks over a series of frames. These happen
from a large distance away. Mediumly off the screen.

`lightly_loading_drawing_chunks`
- These chunks are drawing their blocks. These happen from a short distance away,
just off the screen

`urgently_loading_chunks`
- These chunks are so close to the player that they need to have their blocks
loaded in right away. The drawing can happen later though

`loaded_chunks`
- These chunks are fully drawn and visible to the player.
"""
var loaded_chunks := {}
var lightly_loading_blocks_chunks := {}
var lightly_loading_drawing_chunks := {}
var urgently_loading_blocks_chunks := {}

var load_margin := 2
var draw_margin := 2

var player = null
var world_image: Image
var chunk_pixel_dimensions: Vector2

var world_image_luminance: Image

func _ready():
	player = get_tree().get_root().find_node("Player", true, false)
	
	var world_texture = load("res://blocks.png")
#	var world_texture = load("res://solid.png")
#	var world_texture = load("res://skinny.png")
#	var world_texture = load("res://small.png")
#	var world_texture = load("res://medium.png")
#	var world_texture = load("res://hd.png")
	self.world_image = world_texture.get_data()
	
	# TODO: Uses no .unlock?
	self.world_image.lock()
	
	self.chunk_pixel_dimensions = self.block_pixel_size * self.chunk_block_count
	
	self.world_image_luminance = Image.new()
	self.world_image_luminance.create(self.world_image.get_width(), self.world_image.get_height(), false, Image.FORMAT_RGBA8)
	self.world_image_luminance.fill(Color.red)
	self.world_image_luminance.lock()
	for i in range(self.world_image_luminance.get_width()):
		for j in range(self.world_image_luminance.get_height()):
			if self.world_image.get_pixel(i, j).a == 0:
				self.world_image_luminance.set_pixel(i, j, Color(0, 0, 0, 0))
			else:
				self.world_image_luminance.set_pixel(i, j, Color(1, 1, 1, 0))
#	world_image_luminance.unlock()
	
#	generate_world()


func _process(_delta):
	# Save the all chunks loaded in memory to a file.
	if InputLayering.pop_action("save_world"):
		for chunk in get_children():
			chunk.save_chunk()
		print("Finished Saving to file")
	
	
	delete_invisible_chunks()
	create_chunk_streaming_regions()
	create_chunks()
	continue_streaming_regions()
	
	# Draw the chunk borders
	update()

"""
This currently colours the chunks with a border donoting the kind of chunk it
is and how it should be streamed in. Reducing the viewport_rectangle in the
Player.get_visibility_points method will allow you to see this process in action.
"""
func _draw():
	var thickness := 10
	for point in lightly_loading_blocks_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.green, false, thickness, false)

	for point in lightly_loading_drawing_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.orange, false, thickness, false)

	for point in urgently_loading_blocks_chunks.keys():
		point *= chunk_pixel_dimensions
		draw_rect(Rect2(point, chunk_pixel_dimensions), Color.red, false, thickness, false)

"""
This method creates and initialises all the chunks the player can see such
that they are ready to have their blocks streamed in.
"""
func create_chunks():
	for point in player.get_visibility_points(load_margin + draw_margin):
		var world_image_in_chunks := world_image.get_size() / chunk_block_count
		if (point.x < 0 || point.y < 0 || point.x >= world_image_in_chunks.x || point.y >= world_image_in_chunks.y):
			continue
		
		# Only create chunks that have not already been loaded in
		if !loaded_chunks.has(point):
			var chunk: Chunk = Chunk.new()
			
			# Don't forget to add this chunk as a child so it has access to
			# the root of the project.
			add_child(chunk)
			# Initialise the chunk for streaming
			chunk.init_stream(
				world_image,
				point,
				chunk_block_count,
				block_pixel_size
			)
			loaded_chunks[point] = chunk

"""
This method uses the visibility points to determine which chunks should be
unloaded from memory.
"""
func delete_invisible_chunks():
	
	# First we grab a set the world_positions that should be loaded in the game 
	var visibility_points: Array = player.get_visibility_points(load_margin + draw_margin)
	
	# Create a temporary dictionary to store chunks that are already loaded
	# and should stay loaded
	var visible_chunks := {}
	
	# Loop through the loaded chunks and store the ones that we should keep in
	# visible_chunks while erasing them from the old loaded_chunks dictionary. 
	for visible_point in visibility_points:
		if loaded_chunks.has(visible_point):
			visible_chunks[visible_point] = loaded_chunks[visible_point]
			# warning-ignore:return_value_discarded
			loaded_chunks.erase(visible_point)
	
	# Now the remaining chunks in loaded_chunks are invisible to the player
	# and can be deleted from memory. The delete_chunk method will handle this
	# for us.
	for invisible_point in loaded_chunks.keys():
		loaded_chunks[invisible_point].queue_free()
		# warning-ignore:return_value_discarded
		loaded_chunks.erase(invisible_point)
	
	# Finally, our new visible chunks dictionary becomes the loaded chunks
	# dictionary
	loaded_chunks = visible_chunks
	
	# Also reset the region loading dictionaries. These will be repopulated
	# later.
	lightly_loading_blocks_chunks.clear()
	lightly_loading_drawing_chunks.clear()
	urgently_loading_blocks_chunks.clear()
	
"""
Create the three regions and populate them with null values. At the moment,
they do not have chunk instances. They can be retrieved later from the
loaded_chunks dictionary instead. This method just create the appropriate
keys for each dictionary.
"""
func create_chunk_streaming_regions():
	var load_visibility_points: Array = player.get_visibility_points(load_margin + draw_margin)
	var draw_visibility_points: Array = player.get_visibility_points(draw_margin)
	var urgent_visibility_points: Array = player.get_visibility_points()
	
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

"""
This method continues to load in the chunks based on the three regions
outlined at the top of this script. To tweak performance consider changing
these variables:
- blocks_to_load
	- Blocks to stream in every frame. Should be ~around the number of blocks
	in a chunk.
- chunks_to_draw
	- The number of chunks to draw every frame. Set to 0 and no chunks will
	be streamed. This should be set 1 but experiment with more values.
- draw_margin
	- This is the number of extra chunk layers outside the view of screen
	that will drawn (by streaming) such that a player moving into new chunks
	won't experience lag spikes
- load_margin
	- This is the number of extra chunk layers past the draw_margin that will
	only stream in blocks.
- 'Chunk Block Count'
	- This is the number of blocks in each chunk. This should be set to a
	reasonable value like (16, 16) or (32, 32). Experiment with others.

Tweaking these values on different computers will result in better
performance. Maybe I'll make them editable in a configuration file in the
future...
"""
func continue_streaming_regions():
#	var blocks_to_load := 512
	var chunks_to_draw := 1
	
	for point in loaded_chunks.keys():
		if urgently_loading_blocks_chunks.has(point):
			var chunk: Chunk = loaded_chunks[point]
			urgently_loading_blocks_chunks[point] = chunk
			if !chunk.is_loaded():
				chunk.create()
			if !chunk.is_drawn():
				chunk.update()
		
		if lightly_loading_drawing_chunks.has(point):
			var chunk: Chunk = loaded_chunks[point]
			lightly_loading_drawing_chunks[point] = chunk
			
			if !chunk.is_loaded():
				chunk.create()
			if !chunk.is_drawn() && chunks_to_draw > 0:
				chunks_to_draw -= 1
				chunk.update()
				
		if lightly_loading_blocks_chunks.has(point):
			var chunk: Chunk = loaded_chunks[point]
			lightly_loading_blocks_chunks[point] = chunk
			
			if !chunk.is_locked() && !chunk.is_loaded():
				chunk.lock()
				$ThreadPool.submit_task_unparameterized(chunk, "create")

"""
Returns the size of a chunk in pixels as a Vector2
"""
func get_chunk_pixel_dimensions() -> Vector2:
	return chunk_pixel_dimensions

"""
Returns the size of the world in blocks as a Vector2. TODO: Change to use the
world_size_in_chunks variable when we create our own world.
"""
func get_world_size() -> Vector2:
# 	return world_size_in_chunks
	return self.world_image.get_size()

"""
Returns the size of a chunk in blocks as a Vector2
"""
func get_chunk_block_count() -> Vector2:
	return chunk_block_count

"""
Returns the size of a block in pixels as a Vector2
"""
func get_block_pixel_size() -> Vector2:
	return block_pixel_size

"""
Returns a Chunk if it exists at the given chunk_position in the world.
	- Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
	index in the world.

Fastest function to get chunks.
"""
func get_chunk_from_chunk_position(chunk_position: Vector2) -> Chunk:
#	if create_chunk(chunk_position):
	if loaded_chunks.has(chunk_position):
		return loaded_chunks[chunk_position]
	return null

"""
Returns a Chunk if it exists at the given world_position in the world.
World positions are locations represented by pixels. Entities in the world
are stored using this value.

Slowest function to get chunks.
"""
func get_chunk_from_world_position(world_position: Vector2) -> Chunk:
	var chunk_position = get_chunk_position_from_world_position(world_position)
	return get_chunk_from_chunk_position(chunk_position)
	
"""
Returns a chunk position from the given world_position.
	- World positions are locations represented by pixels. Entities in the world
	are stored using this value.
Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
index in the world.

Fastest function to get chunk positions.
"""
func get_chunk_position_from_world_position(world_position: Vector2) -> Vector2:
	return (world_position / get_chunk_pixel_dimensions()).floor()

"""
Returns a block if it exists using the given chunk_position and block_position
values.
	- Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
	index in the world.
	- Block positions are the position of the block relative to the chunk it is
	in. It cannot be larger than the chunk's block_size.
Blocks are Dictionaries containing a set of standard variables. See the Block
documentation in the Chunk scene.

Fastest function to get blocks.
"""
func get_block_from_chunk_position_and_block_position(chunk_position: Vector2, block_position: Vector2):
	var chunk = get_chunk_from_chunk_position(chunk_position)
	if chunk == null or not chunk.blocks.has(block_position):
		return null
	return chunk.blocks[block_position]

"""
Returns a block if it exists using the given world_position.
	- World positions are locations represented by pixels. Entities in the world
	are stored using this value.
Blocks are Dictionaries containing a set of standard variables. See the Block
documentation in the Chunk scene.

Slowest function to get blocks.
"""
func get_block_from_world_position(world_position: Vector2) -> Dictionary:
	var chunk_position: Vector2 = get_chunk_position_from_world_position(world_position)
	var block_position: Vector2 = get_block_position_from_world_position(world_position)
	return get_block_from_chunk_position_and_block_position(chunk_position, block_position)
	
"""
Returns a block position using the given world_position and chunk_position
values.
	- World positions are locations represented by pixels. Entities in the world
	are stored using this value.
	- Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
	index in the world.
Block positions are the position of the block relative to the chunk it is in. It
cannot be larger than the chunk's block_size.

Fastest function to get block positions.
"""
func get_block_position_from_world_position_and_chunk_position(world_position: Vector2, chunk_position: Vector2):
	var block_position = (world_position - chunk_position * get_chunk_pixel_dimensions()).floor()
	return (block_position / block_pixel_size).floor()
	
"""
Returns a block position using the given world_position.
	- World positions are locations represented by pixels. Entities in the world
	are stored using this value.
Block positions are the position of the block relative to the chunk it is in. It
cannot be larger than the chunk's block_size.

Slowest function to get block positions.
"""
func get_block_position_from_world_position(world_position: Vector2) -> Vector2:
	var chunk_position = get_chunk_position_from_world_position(world_position)
	return get_block_position_from_world_position_and_chunk_position(world_position, chunk_position)

"""
Sets a block at the given chunk_position and block_position to be the
new_block if it exists.
	- Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
	index in the world.
	- Block positions are the position of the block relative to the chunk it is
	in. It cannot be larger than the chunk's block_size.
	- Blocks are Dictionaries containing a set of standard variables. See the
	Block documentation in the Chunk scene.

Fastest function to set blocks.
"""
func set_block_from_chunk_position_and_block_position(chunk_position: Vector2, block_position: Vector2, new_block: Dictionary):
	var chunk = get_chunk_from_chunk_position(chunk_position)
	if chunk != null:
		chunk.set_block_from_block_position(block_position, new_block)

"""
Sets a block at the given world_position to be the new_block if it exists.
	- World positions are locations represented by pixels. Entities in the world
	are stored using this value.
	- Blocks are Dictionaries containing a set of standard variables. See the
	Block documentation in the Chunk scene.

Slowest function to set blocks.
"""
func set_block_at_world_position(world_position: Vector2, new_block: Dictionary):
	var chunk_position = get_chunk_position_from_world_position(world_position)
	var chunk = get_chunk_from_chunk_position(chunk_position)
	if chunk != null:
		var block_position = get_block_position_from_world_position_and_chunk_position(world_position, chunk_position)
		chunk.set_block_from_block_position(block_position, new_block)
