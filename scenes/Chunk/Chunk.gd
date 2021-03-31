extends Node2D

var world_image : Image
var chunk_position : Vector2
var block_count : Vector2
var block_pixel_size : Vector2
var terrain = null


var blocks = {}
var blocks_loaded = 0
var drawn = false

func _ready():
	terrain = get_tree().get_root().find_node("Terrain", true, false)

#func _process(_delta):
	# Turn this on to see the chunk be streamed in
#	update()
#	pass

func init_stream(_world_image : Image, _chunk_position : Vector2, _block_count : Vector2, _block_pixel_size : Vector2):
	"""
	Treat this method like a constructor. It initialises the chunk with all the
	data it requires to begin streaming in the blocks.
	"""
	self.world_image = _world_image
	self.chunk_position = _chunk_position
	self.block_count = _block_count
	self.block_pixel_size = _block_pixel_size
	self.position = _block_pixel_size * _chunk_position * _block_count

func is_loaded():
	return self.blocks_loaded == floor(block_count.x * block_count.y)

func is_drawn():
	"""
	This function is true only after a chunk has been fully loaded AND then drawn.
	This is so the chunks draw call is cached and the Terrain knows not to try
	and draw this chunk to the screen again.
	"""
	return self.drawn
#
func stream_all():
	"""
	This method streams in the entire chunk at once. This is usually because the
	game needs to draw this chunk urgently.
	"""
	stream(floor(block_count.x * block_count.y))
#
func stream(maximum_blocks_to_load : int):
	"""
	This method takes in an integer which is the desired number of blocks to stream
	in. Eventually this data will be retrieved from the chunk. If it cannot stream
	the amount specified, it will only stream up the max. The return value of
	this method is the number of blocks successfully streamed in.
	
	0 <= Return value <= maximum_blocks_to_load
	"""
	var blocks_available_to_load = block_count.x * block_count.y - self.blocks_loaded
	var blocks_to_load = min(blocks_available_to_load, maximum_blocks_to_load)
	
	# The image needs to be unlocked before its contents can be read (lock after).
	self.world_image.lock()
	for element in range(self.blocks_loaded, self.blocks_loaded + blocks_to_load):
		var i = element / int(block_count.x)
		var j = element % int(block_count.y) # Integer division
		var block_position = Vector2(i, j)
		if !blocks.has(block_position):
			blocks[block_position] = {}

		var block_pixel_position = self.chunk_position * block_count + block_position
		
		# Grab the colour for the pixel from the world image. If the pixel
		# goes out of bounds then just draw Red. This happens when the image is
		# not a multiple of the chunk size.
		var pixel : Color
		if (block_pixel_position.x < 0 ||
			block_pixel_position.y < 0 ||
			block_pixel_position.x >= self.world_image.get_size().x ||
			block_pixel_position.y >= self.world_image.get_size().y):
			pixel = Color.red
		else:
			pixel = Color(self.world_image.get_pixelv(block_pixel_position))

		blocks[block_position]["id"] = pixel.a
		blocks[block_position]["colour"] = pixel
		# The old colour formula
#		blocks[block_position]["colour"] = Color(
#				abs(sin(((i + j) * 4) / 255.0)),
#				abs(sin((200 - (i * 2)) / 255.0)),
#				abs(sin((200 - (j * 3)) / 255.0))
##				0
##				abs(sin(randi()) - 0.5)
#			)

	self.world_image.unlock()
	self.blocks_loaded += blocks_to_load
	return blocks_to_load

func save_chunk():
	"""
	This method will save all the data in a chunk to disk. Currently it is being
	done using compression, however this can be changed below. TODO: Change this
	to take in a parameter as a save destination. Currently it's hardcoded.
	"""
	# Create the directory if it does not exist
	var directory = Directory.new()
	directory.make_dir("user://chunk_data")
		
	# Create a file for each chunk. Store chunk specfic data below. File
	# size heavily depends on how varied the data is stored between blocks.
	var chunk = File.new()
#	chunk.open("user://chunk_data/%s.dat" % world_position, File.WRITE)
	chunk.open_compressed("user://chunk_data/%s.dat" % chunk_position, File.WRITE, File.COMPRESSION_ZSTD)
	
	# Save all chunk data in here
	for _i in range(block_count.x):
		for _j in range(block_count.y):
			chunk.store_16(floor(rand_range(0, 5)))
	chunk.close()

func draw_chunk():
	"""
	Draw the chunk to the screen using my special colour formula. This function
	Is run when a chunk is created however, we only want it to count as being
	run after all the blocks have been loaded.
	"""
	if self.is_loaded():
		self.drawn = true
		
	for i in range(block_count.x):
		for j in range(block_count.y):
			var block_position = Vector2(i, j)
			if blocks.has(block_position) && blocks[block_position]["id"]:
				var rectangle_position = block_position * block_pixel_size
				var rectangle_to_draw = Rect2(rectangle_position, block_pixel_size)
				var rectangle_colour = blocks[block_position]["colour"]
				draw_rect(rectangle_to_draw, rectangle_colour)

func stream_chunk():
	var finish_streaming_index = self.streaming_index + self.streaming_draw_calls
	
	if finish_streaming_index >= (block_count.x * block_count.y):
		finish_streaming_index = block_count.x * block_count.y - 1
		self.streaming = false
	
	for element in range(self.streaming_index, finish_streaming_index):
		var i = element / int(block_count.x)
		var j = element % int(block_count.y) # Integer division
		var block_position = Vector2(i, j)
		if blocks[block_position]["id"]:
			var rectangle_position = Vector2(i * block_pixel_size.x, j * block_pixel_size.y)
			var rectangle_to_draw = Rect2(rectangle_position, block_pixel_size)
			var rectangle_colour = blocks[block_position]["colour"]
			draw_rect(rectangle_to_draw, rectangle_colour)
		
	self.streaming_index = finish_streaming_index

func get_block_from_block_position(block_position : Vector2):
	if blocks.has(block_position):
		return blocks[block_position]
	return null

func set_block_from_block_position(block_position : Vector2, new_block : Dictionary):
	blocks[block_position] = new_block
	update()

func set_block_colour_from_int(i : int, j : int, colour : Color):
	set_block_colour(Vector2(i, j), colour)

func set_block_colour(block_position : Vector2, colour : Color):
	blocks[block_position]["colour"] = colour
	update()

func _draw():
	draw_chunk()

#	draw_circle(Vector2.ZERO, 2, Color.aquamarine)
