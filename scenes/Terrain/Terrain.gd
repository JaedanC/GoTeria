extends Node2D


export(Vector2) var block_pixel_size
export(Vector2) var world_size_in_chunks
export(Vector2) var chunk_block_count
var chunk_scene = preload("res://scenes/Chunk.tscn")

var loaded_chunks = {}

func _ready():
#	generate_world()
	pass

func _process(_delta):
	# Save the all chunks loaded in memory to a file.
	if InputLayering.pop_action("save_world"):
		for chunk in get_children():
			chunk.save_chunk()
		print("Finished Saving to file")
	free_all_invisible_chunks()
	create_visible_chunks()
	
	# Tells Godot to run the draw() method each frame.
	update()
	
func free_all_invisible_chunks():
	"""
	This method uses the visibility points to determine which chunks should be
	unloaded from memory.
	"""
	
	# First we grab a set the world_positions that should be loaded in the game 
	var visibility_points = get_visibility_points()
	
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

func get_visibility_points() -> Array:
	"""
	This method returns a list of integer Vector2's that represent the indexes of
	chunks that should be loaded for the player. This takes into account where
	the player is and the camera zoom by performing some neat mathematical
	tricks on the viewport, player and camera, position vectors.
	
	Example Output:
	[
		Vector2(0, 0),
		Vector2(0, 1),
		Vector2(1, 0),
		Vector2(1, 1)
	]
	"""
	# Grab important data
	var player = get_tree().get_root().find_node("Player", true, false)
	var camera = player.find_node("Camera")
	var viewport_rectangle = get_viewport_rect()
	
	# Using the viewport rectangle as a base, centre a copy of it around the player.
	viewport_rectangle.position = player.position - viewport_rectangle.size/2
	
	# Expand this Rectangle to take into account camera zooming by using two
	# Vector2's the point the the corners of the screen and using the Rect2.expand
	# method. TODO: This strategy does not take into account zooming in, only zooming
	# out is considered. Adding this feature would only slightly increase the fps
	# when zoomed in and thus it isn't a high priority
	viewport_rectangle = viewport_rectangle.expand(player.position - (get_viewport_rect().size / 2) * camera.zoom)
	viewport_rectangle = viewport_rectangle.expand(player.position + (get_viewport_rect().size / 2) * camera.zoom)
	
	# Convert the top left and bottom right points of the Rect2 into an integer
	# that we can loop through to get the points for the visible chunks. This
	# takes into account the size of each chunk.
	var chunk_dimensions = get_chunk_pixel_dimensions()
	var top_left = (viewport_rectangle.position / chunk_dimensions).floor()
	var bottom_right = ((viewport_rectangle.position + viewport_rectangle.size) / chunk_dimensions).floor()
	
	# Loop through these two values and include them in the visibility point
	# list. +1 is there so that the right and bottom edge are forced to be
	# included. Remove it if you don't know what I mean an then you'll see.
	var visibility_points = []
	for i in range(top_left.x, bottom_right.x + 1):
		for j in range(top_left.y, bottom_right.y + 1):
			var visible_point = Vector2(i, j)
			visibility_points.append(visible_point)

	return visibility_points

func create_visible_chunks():
	"""
	This method uses the visibility points to load chunks from memory if they
	not already loaded. Loaded chunks are added to the loaded_chunk dictionary
	and added as children to this scene. Points including negative values
	will be rejected by create_chunk so we need to check if the return is null
	before we add it to the tree.
	"""
	var visibility_points = get_visibility_points()
	for point in visibility_points:
		if !loaded_chunks.has(point):
			var chunk = create_chunk(point)
			# If create_chunk was successful.
			if chunk:
				add_child(chunk)

func create_chunk(world_position : Vector2):
	"""
	This method creates and loads a chunk based on the world_position Vector2.
	It will return null if the x or y value is negative. Otherwise it will add
	the chunk to the loaded_chunks dictionary and return the chunk instance
	it created. 
	"""
	# Input checks
	if world_position.x < 0 or world_position.y < 0:
		return null
	
	# Instance a chunk
	var chunk = chunk_scene.instance()
	chunk.init(world_position,
		chunk_block_count,
		block_pixel_size
	)
	
	# Cache the chunk for future reference
	loaded_chunks[world_position] = chunk
	return chunk

func delete_chunk(chunk):
	"""
	This method deletes the chunk passed in and removed the reference to this
	chunk in the loaded_chunks dictionary.
	"""
	loaded_chunks.erase(chunk.world_position)
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

func get_chunk_pixel_dimensions() -> Vector2:
	"""
	Returns the size of a chunk in pixels as a Vector2
	"""
	return block_pixel_size * chunk_block_count

func _draw():
#	var points = get_visibility_points()
#	for point in points:
#		draw_circle(point * get_chunk_pixel_dimensions(), 15, Color(0, 1, 0, 1))
	pass
