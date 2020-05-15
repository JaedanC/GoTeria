extends Node2D

var world_position : Vector2
var block_count : Vector2
var block_pixel_size : Vector2

func init(_world_position : Vector2, _block_count  : Vector2, _block_pixel_size : Vector2):
	"""
	Chunks require a constructor to pass in all the data. Unfortunately this means
	that you cannot mark this script as a tool and visually see the chunk. This
	can bypassed by temporarily hardcoding this data and calling the init
	function in ready if Engine.editor_hint is true.
	"""
	self.world_position = _world_position
	self.block_count = _block_count
	self.block_pixel_size = _block_pixel_size
	self.position = block_pixel_size * world_position * block_count
	
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
	chunk.open_compressed("user://chunk_data/%s.dat" % world_position, File.WRITE, File.COMPRESSION_ZSTD)
	
	# Save all chunk data in here
	for i in range(block_count.x):
		for j in range(block_count.y):
			chunk.store_16(floor(rand_range(0, 5)))
	chunk.close()

func draw_chunk():
	"""
	Draw the chunk to the screen using my special colour formula.
	"""
	for i in range(block_count.x):
		for j in range(block_count.y):
			var rectangle_position = Vector2(i * block_pixel_size.x, j * block_pixel_size.y)
			var rectangle_to_draw = Rect2(rectangle_position, block_pixel_size)
			var rectangle_colour = Color(
				abs(sin(((i + j) * 4) / 255.0)),
				abs(sin((200 - (i * 2)) / 255.0)),
				abs(sin((200 - (j * 3)) / 255.0))
#				0.2
#				abs(sin(randi()) - 0.5)
			)
			draw_rect(rectangle_to_draw, rectangle_colour)

func _draw():
	draw_chunk()
