extends Node2D

export(Vector2) var block_pixel_size: Vector2
#export(Vector2) var world_size_in_chunks: Vector2
export(Vector2) var chunk_block_count: Vector2

onready var player = get_tree().get_root().find_node("Player", true, false)
onready var thread_pool = get_tree().get_root().find_node("ThreadPool", true, false)

var world_image: Image
var world_image_luminance: Image
var chunk_pixel_dimensions: Vector2

var loaded_chunks := {}
var lightly_loading_blocks_chunks := {}
var lightly_loading_drawing_chunks := {}
var urgently_loading_blocks_chunks := {}

var load_margin := 1
var draw_margin := 0

var deactivated_and_reusable_chunks: Array = []
#var maximum_deactivated_and_reusable_chunks : int = 100


func _ready():
#	var world_texture = load("res://blocks.png")
#	var world_texture = load("res://LargeWorld.png")
	var world_texture = load("res://LargeWorldAlpha.png")
#	var world_texture = load("res://solid.png")
#	var world_texture = load("res://skinny.png")
#	var world_texture = load("res://small.png")
#	var world_texture = load("res://medium.png")
#	var world_texture = load("res://hd.png")
	self.world_image = world_texture.get_data()
	self.world_image.lock() # TODO: Uses no .unlock?
	
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
	pass

func _process(_delta):
	# Save the all chunks loaded in memory to a file.
	if InputLayering.pop_action("save_world"):
		for chunk in get_children():
			chunk.save_chunk()
		print("Finished Saving to file")
	
	delete_invisible_chunks()
	load_visible_chunks()
	create_chunk_streaming_regions()
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
This method frees a chunk and removes it from this node as a child. The Chunk
is not freed unless the amount of chunks that are queued to be reset exceeds the
value maximum_deactivated_and_reusable_chunks. Instead, it is reset and stored
such that when a new chunk is created one of these chunks can be used instead
to decrease the number of memory allocations. This technique provides a large
speed up in performance.
"""
func free_chunk(chunk: Chunk):
	remove_child(chunk)
	# Don't free the chunk yet. Try to reset it and store it to save on 
	# memory allocations
#	if self.deactivated_and_reusable_chunks.size() + 1 > self.maximum_deactivated_and_reusable_chunks:
#		print("Freeing chunk:")
#		chunk.queue_free()
#	else:
#		thread_pool.submit_task_unparameterized(chunk, "reset")
#		chunk.reset()
	self.deactivated_and_reusable_chunks.append(chunk)

func get_chunk_data(data):
	var chunk_position: Vector2 = data[0]
	var chunk: Chunk = data[1]
	var blocks = []
	
	# This is to check if the next thread that could have been waiting is
	# accidentally about to do work a second time.
	var chunk_image
	
	# Create a local image so multiple threads don't race to write to the
	# instance variable chunk_image.
	chunk_image = Image.new()
	chunk_image.create(chunk_block_count.x, chunk_block_count.y, false, Image.FORMAT_RGBA8)
	chunk_image.fill(Color.firebrick)
#	chunk_image.resize(
#		block_count.x * block_pixel_size.x,
#		block_count.y * block_pixel_size.y,
#	)
	chunk_image.lock()
	for j in range(chunk_block_count.y):
		for i in range(chunk_block_count.x):
			var block_position := Vector2(i, j)
			var block_pixel_position: Vector2 = chunk_position * chunk_block_count + block_position
			
			# Grab the colour for the pixel from the world image. If the pixel
			# goes out of bounds then just draw Red. This happens when the image is
			# not a multiple of the chunk size.
			var pixel: Color
			if (block_pixel_position.x < 0 ||
				block_pixel_position.y < 0 ||
				block_pixel_position.x >= self.world_image.get_size().x ||
				block_pixel_position.y >= self.world_image.get_size().y):
				pixel = Color.red
			else:
				pixel = Color(self.world_image.get_pixelv(block_pixel_position))
			chunk_image.set_pixelv(block_position, pixel)
			
			var block = {}
			block["id"] = pixel.a
			block["colour"] = pixel
			blocks.append(block)
			
	chunk_image.unlock()
	
	chunk.obtain_chunk_data([blocks, chunk_image])
	
	return [blocks, chunk_image]

"""
This method returns a new chunk at the given chunk position. The new chunk is
automatically added as a child to this node. It has also already been
initialised to stream in its blocks. This function will return a previously
created chunk that has been reset if one is available. This reduces the amount
of memory allocations for a large speed up in performance.
"""
func create_chunk(chunk_position: Vector2) -> Chunk:
	# Attempt to reuse a chunk already created.
	var chunk: Chunk
	if self.deactivated_and_reusable_chunks.size() != 0:
#		print("Reusing: ", chunk_position)
		chunk = self.deactivated_and_reusable_chunks.pop_back()
		# Reset the chunk
		chunk.reset(chunk_position)
	else:
		chunk = Chunk.new(world_image, chunk_position, chunk_block_count, block_pixel_size)
	# Don't forget to add this chunk as a child so it has access to
	# the root of the project.
	add_child(chunk)
	return chunk

"""
This method creates and initialises all the chunks the player can see such
that they are ready to have their blocks streamed in.
"""
func load_visible_chunks():
	for chunk_position in player.get_visibility_chunk_positions(load_margin + draw_margin):
		var world_image_in_chunks := world_image.get_size() / chunk_block_count
		if (chunk_position.x < 0 || chunk_position.y < 0 ||
				chunk_position.x >= world_image_in_chunks.x ||
				chunk_position.y >= world_image_in_chunks.y):
			continue
		
		# Only create chunks that have not already been loaded in
		if not loaded_chunks.has(chunk_position):
			var chunk = self.create_chunk(chunk_position)
			loaded_chunks[chunk_position] = chunk

"""
This method uses the visibility points to determine which chunks should be
unloaded from memory.
"""
func delete_invisible_chunks():
	# First we grab a set the world_positions that should be loaded in the game 
	var visibility_points: Array = player.get_visibility_chunk_positions(load_margin + draw_margin)
	
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
	for invisible_chunk_position in loaded_chunks.keys():
		var chunk = loaded_chunks[invisible_chunk_position]
		self.free_chunk(chunk)
		
		# warning-ignore:return_value_discarded
		loaded_chunks.erase(invisible_chunk_position)
	
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
	var urgent_visibility_points: Array = player.get_visibility_chunk_positions()
	var draw_visibility_points: Array = player.get_visibility_chunk_positions(draw_margin, true)
	var load_visibility_points: Array = player.get_visibility_chunk_positions(load_margin + draw_margin, true, draw_margin)
	
#	print("Urgent:", urgent_visibility_points)
#	print("Load:", load_visibility_points)
	
	for point in urgent_visibility_points:
		var chunk = self.get_chunk_from_chunk_position(point)
		if chunk != null:
			self.urgently_loading_blocks_chunks[point] = chunk
	
	for point in draw_visibility_points:
		var chunk = self.get_chunk_from_chunk_position(point)
		if chunk != null:
			self.lightly_loading_drawing_chunks[point] = self.get_chunk_from_chunk_position(point)
	
	for point in load_visibility_points:
		var chunk = self.get_chunk_from_chunk_position(point)
		if chunk != null:
			self.lightly_loading_blocks_chunks[point] = self.get_chunk_from_chunk_position(point)

"""
This method continues to load in the chunks based on the three regions
outlined at the top of this script. To tweak performance consider changing
these variables:
	- blocks_to_load. Blocks to stream in every frame. Should be ~around the
	number of blocks in a chunk.
	- chunks_to_draw. The number of chunks to draw every frame. Set to 0 and no
	chunks will be streamed. This should be set 1 but experiment with more
	values.
	- draw_margin. This is the number of extra chunk layers outside the view of
	screen that will drawn (by streaming) such that a player moving into new
	chunks won't experience lag spikes
	- load_margin. This is the number of extra chunk layers past the draw_margin
	that will only stream in blocks.
	- 'Chunk Block Count'. This is the number of blocks in each chunk. This
	should be set to a reasonable value like (16, 16) or (32, 32). Experiment
	with others.

The premise behind these dictionaries are to improve performance when streaming
in chunks behind the scenes.
	- lightly_loading_blocks_chunks. These are slowly loading in their blocks
	over a series of frames. These happen from a large distance away. Mediumly
	off the screen.
	- lightly_loading_drawing_chunks. These chunks are drawing their blocks.
	These happen from a short distance away, just off the screen
	- urgently_loading_chunks. These chunks are so close to the player that
	they need to have their blocks loaded in right away. The drawing can happen
	later though
	- loaded_chunks. These chunks are fully drawn and visible to the player.

Tweaking these values on different computers may result in better performance.
TODO: Make them editable in a configuration file in the future.
"""
func continue_streaming_regions():
	var force_load = []
	
	# First we check if the chunks need to be forced to load their blocks
	var chunk_points = urgently_loading_blocks_chunks.keys() + lightly_loading_drawing_chunks.keys()
	for chunk_point in chunk_points:
		var chunk: Chunk = loaded_chunks[chunk_point]
		if not chunk.is_loaded():
			force_load.append(chunk_point)
	
	# Next we check the draw conditions
	var chunks_to_draw = 1
	for chunk in urgently_loading_blocks_chunks.values():
		if not chunk.is_drawn():
			chunk.update()
			chunks_to_draw -= 1
	for chunk in lightly_loading_drawing_chunks.values():
		if chunks_to_draw <= 0:
			break
		if not chunk.is_drawn():
			chunk.update()
			chunks_to_draw -= 1
	
	# Finally, start the thread pool with the next batch as long as they haven't
	# been locked already. Don't start the chunks again that were found to have
	# finished in the thread pool.
	var chunks_to_load = lightly_loading_blocks_chunks.keys() + force_load
	for chunk_point in chunks_to_load:
		var chunk: Chunk = loaded_chunks[chunk_point]
		if chunk.is_locked() or chunk.is_loaded():
			continue
		
		chunk.lock()
#		var chunk_data = get_chunk_data(point)
#		chunk.obtain_chunk_data(chunk_data)
		thread_pool.submit_task(self, "get_chunk_data", [chunk_point, chunk], "chunk", chunk_point)
	
	
	# Next, we obtain the completed blocks that have loaded and start the
	# next batch of block loads. During this process, wait until the force_load
	# blocks have been done as they are required to be completed by now.
	for chunk_position in force_load:
		thread_pool.wait_for_task_specific(chunk_position)
		# Now all tasks that need to be done should be ready
	
	# Obtain the tasks completed that has the chunks that are being
	# loaded inside the thread pool. If they have completed, then we can send
	# this data to the chunk to automatically mark it as completed.
	var completed_chunk_tasks = thread_pool.fetch_finished_tasks_by_tag("chunk")
	for completed_chunk_task in completed_chunk_tasks:
		var chunk_point: Vector2 = completed_chunk_task.get_argument()[0]
		# In the case that the chunk has already been freed, don't assign the
		# chunk any data.
		if not loaded_chunks.has(chunk_point):
			continue
		
		# Give the chunk it's data
#		var chunk_data: Array = completed_chunk_task.get_result()
		var chunk: Chunk = loaded_chunks[chunk_point]
#		chunk.obtain_chunk_data(chunk_data)
		
		chunk.update()
	

"""
Returns the size of a chunk in pixels as a Vector2
"""
func get_chunk_pixel_dimensions() -> Vector2:
	return chunk_pixel_dimensions

"""
Returns the size of the world in blocks as a Vector2. TODO: Change to use the
world_size_in_chunks variable when we create our own world.s
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
	if chunk == null:
		return null
	if not chunk.is_loaded():
		return null
	var block = chunk.get_block_from_block_position(block_position)
	return block

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
	var block_position: Vector2 = get_block_position_from_world_position_and_chunk_position(world_position, chunk_position)
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
