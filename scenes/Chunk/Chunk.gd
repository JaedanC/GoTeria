extends Node2D

var world_image : Image
var chunk_position : Vector2
var block_count : Vector2
var block_pixel_size : Vector2

var blocks = {}
var terrain = null

var drawn = false
var blocks_loaded = 0

var streaming = false
var streaming_index = 0
var streaming_draw_calls = 64

func _ready():
	terrain = get_tree().get_root().find_node("Terrain", true, false)

func _process(_delta):
#	if (streaming):
#	update()
	
#	if (drawn):
#		set_process(false)
#		update()
#	else:
#		stream()
	pass
	
	
	
func init_stream(_world_image : Image, _chunk_position : Vector2, _block_count : Vector2, _block_pixel_size : Vector2):
	self.world_image = _world_image
	self.chunk_position = _chunk_position
	self.block_count = _block_count
	self.block_pixel_size = _block_pixel_size
	self.position = _block_pixel_size * _chunk_position * _block_count

func get_loaded_blocks():
	return self.blocks_loaded
#
#func stream_all():
#	stream(block_count.x * block_count.y)
#
#func stream(maximum_blocks_to_load : int):
#	var blocks_available_to_load = block_count.x * block_count.y - get_loaded_blocks()
#	var blocks_to_load = min(blocks_available_to_load, maximum_blocks_to_load)
#
#	self.world_image.lock()
#	for element in range(get_loaded_blocks(), get_loaded_blocks() + blocks_to_load):
#		var i = element / int(block_count.x)
#		var j = element % int(block_count.y) # Integer division
#		var block_position = Vector2(i, j)
#		if !blocks.has(block_position):
#			blocks[block_position] = {}
#
#		var block_pixel_position = self.chunk_position * block_count + block_position
#
#		var pixel : Color
#		if (block_pixel_position.x < 0 ||
#			block_pixel_position.y < 0 ||
#			block_pixel_position.x >= self.world_image.get_size().x ||
#			block_pixel_position.y >= self.world_image.get_size().y):
#			pixel = Color.red
#		else:
#			pixel = Color(self.world_image.get_pixelv(block_pixel_position))
#
#		blocks[block_position]["id"] = pixel.a
#		blocks[block_position]["colour"] = pixel
#
#	self.world_image.unlock()
#	self.blocks_loaded += blocks_to_load
#	return int(block_count.x * block_count.y) == get_loaded_blocks()

func init(_world_image : Image, _chunk_position : Vector2, _block_count  : Vector2, _block_pixel_size : Vector2):
	"""
	Chunks require a constructor to pass in all the data. Unfortunately this means
	that you cannot mark this script as a tool and visually see the chunk. This
	can bypassed by temporarily hardcoding this data and calling the init
	function in ready if Engine.editor_hint is true.
	
	The method initialise a Chunk to contain all the block data inside the 'blocks'
	dictionary. It uses the world image to retrieve the colour of the block.
	"""
	self.world_image = _world_image
	self.chunk_position = _chunk_position
	self.block_count = _block_count
	self.block_pixel_size = _block_pixel_size
	self.position = _block_pixel_size * _chunk_position * _block_count
	
	self.streaming = true
	
	world_image.lock()
	for i in range(block_count.x):
		for j in range(block_count.y):
			var block_position = Vector2(i, j)
			if !blocks.has(block_position):
				blocks[block_position] = {}
			
			var block_pixel_position = self.chunk_position * block_count + block_position
			var pixel = Color(world_image.get_pixelv(block_pixel_position))
			
			blocks[block_position]["id"] = pixel.a
			blocks[block_position]["colour"] = pixel
#			blocks[block_position]["colour"] = Color(
#				abs(sin(((i + j) * 4) / 255.0)),
#				abs(sin((200 - (i * 2)) / 255.0)),
#				abs(sin((200 - (j * 3)) / 255.0)),
#				0.2
#				abs(sin(randi()) - 0.5)
#			)
	
	world_image.unlock()


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
	Draw the chunk to the screen using my special colour formula.
	"""
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

	draw_circle(Vector2.ZERO, 2, Color.aquamarine)
#	print(position)
#	stream_chunk()
