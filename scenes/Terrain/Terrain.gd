extends Node2D


export(Vector2) var block_pixel_size
export(Vector2) var world_size_in_chunks
export(Vector2) var chunk_block_count
var chunk_scene = preload("res://scenes/Chunk/Chunk.tscn")

var loaded_chunks = {}
var player = null

func _ready():
	player = get_tree().get_root().find_node("Player", true, false)
#	generate_world()

func _process(_delta):
	# Save the all chunks loaded in memory to a file.
	if InputLayering.pop_action("save_world"):
		for chunk in get_children():
			chunk.save_chunk()
		print("Finished Saving to file")
	free_all_invisible_chunks()
	create_visible_chunks()
	
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
			create_chunk(point)

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
	chunk.init(chunk_position,
		chunk_block_count,
		block_pixel_size
	)
	# Cache the chunk for future reference
	loaded_chunks[chunk_position] = chunk
	add_child(chunk)
	return true

func get_chunk_pixel_dimensions() -> Vector2:
	"""
	Returns the size of a chunk in pixels as a Vector2
	"""
	return block_pixel_size * chunk_block_count

func get_chunk_from_chunk_position(chunk_position : Vector2):
	if create_chunk(chunk_position):
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
	return chunk.blocks[block_position]

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

