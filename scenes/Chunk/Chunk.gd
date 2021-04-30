extends Node2D
class_name Chunk

onready var terrain = get_tree().get_root().find_node("Terrain", true, false)

var world_image: Image
var chunk_image: Image
var chunk_texture: ImageTexture
var volatile_streaming: int

var chunk_position: Vector2
var block_count: Vector2
var block_pixel_size: Vector2

var blocks: Array
var loaded: bool
var drawn: bool
var calls: int


#func _process(_delta):
	# Turn this on to see the chunk be streamed in
#	update()
#	pass



"""
This function is the real constructor of the chunk. Remember, since chunks are
going to be reused, the variables that need to be set once should be placed in
here. For example, the Mutex should not be changed in the event that the
previous Mutex had not opened up yet. self.blocks is in here because these will
be changed in the stream method anyway. Assigning the variable a new Dictionary
is an expensive operation because all its blocks are dellocated on the main
thread.
"""
func _init(_world_image: Image, _chunk_position: Vector2, _block_count: Vector2, _block_pixel_size: Vector2):
	self.chunk_texture = ImageTexture.new()
	self.blocks = []
	self.world_image = _world_image
	self.block_count = _block_count
	self.block_pixel_size = _block_pixel_size
	reset(_chunk_position)

"""
This is the method that is called when a chunk is reset before it is reused.
"""
func reset(_chunk_position: Vector2):
	self.chunk_position = _chunk_position
	self.position = self.block_pixel_size * self.chunk_position * self.block_count
	self.volatile_streaming = 0
	self.loaded = false
	self.drawn = false
	self.calls = 0

func lock():
	assert(self.volatile_streaming == 0)
	self.volatile_streaming += 1

func is_locked() -> bool:
	return self.volatile_streaming > 0

func is_loaded():
	return self.loaded

"""
This function is true only after a chunk has been fully loaded AND then drawn.
This is so the chunks draw call is cached and the terrain knows not to try
and draw this chunk to the screen again.
"""
func is_drawn():
	return self.drawn

#func obtain_chunk_data(_blocks: Array, _chunk_image: Image):
func obtain_chunk_data(chunk_data: Array):
	self.blocks = chunk_data[0]
	self.chunk_image = chunk_data[1]
	self.loaded = true

"""
This method will save all the data in a chunk to disk. Currently it is being
done using compression, however this can be changed below. TODO: Change this
to take in a parameter as a save destination. Currently it's hardcoded.
"""
func save_chunk():
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

"""
Draw the chunk to the screen using my special colour formula. This function
Is run when a chunk is created however, we only want it to count as being
run after all the blocks have been loaded.
"""
func draw_chunk():
	if not self.is_loaded():
		return
	self.drawn = true
	
	self.chunk_texture.create_from_image(self.chunk_image, Texture.FLAG_MIPMAPS | Texture.FLAG_ANISOTROPIC_FILTER)
	draw_set_transform(Vector2.ZERO, 0, block_pixel_size)
	draw_texture(self.chunk_texture, Vector2.ZERO)
	draw_set_transform(Vector2.ZERO, 0, Vector2(1, 1))

func is_valid_block_position(block_position: Vector2):
	return not (block_position.x < 0 or block_position.x >= self.block_count.x or 
				block_position.y < 0 or block_position.y >= self.block_count.y)

func block_position_to_block_index(block_position: Vector2):
	return self.block_count.x * block_position.y + block_position.x
	
func get_block_from_block_position(block_position: Vector2):
	if not self.is_valid_block_position(block_position):
		return null
	
	return self.blocks[self.block_position_to_block_index(block_position)]

func set_block_from_block_position(block_position: Vector2, new_block: Dictionary):
	self.chunk_image.lock()
	if new_block["id"] == 0:
		self.chunk_image.set_pixelv(block_position, Color(0, 0, 0, 0))
	else:
		self.chunk_image.set_pixelv(block_position, new_block["colour"])
	self.chunk_image.unlock()
	
	if not self.is_valid_block_position(block_position):
		return
	
	self.blocks[self.block_position_to_block_index(block_position)] = new_block
	update()

func set_block_colour_from_int(i: int, j: int, colour: Color):
	set_block_colour(Vector2(i, j), colour)

func set_block_colour(block_position: Vector2, colour: Color):
	var block = self.get_block_from_block_position(block_position)
	if block == null:
		return
	
	block["colour"] = colour
	
	self.chunk_image.lock()
	self.chunk_image.set_pixelv(block_position, colour)
	self.chunk_image.unlock()
	update()

func get_chunk_position() -> Vector2:
	return self.chunk_position

func _draw():
#	draw_circle(Vector2.ZERO, 2, Color.aquamarine)
	draw_chunk()
