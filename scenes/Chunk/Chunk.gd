extends Node2D
class_name Chunk

onready var terrain = get_tree().get_root().find_node("Terrain", true, false)
var world_image: Image
var chunk_image: Image
var chunk_texture: ImageTexture

var chunk_position: Vector2
var block_count: Vector2
var block_pixel_size: Vector2

var blocks: Dictionary = {}
var loaded := false
var drawn := false

var volatile_streaming := false

#func _process(_delta):
	# Turn this on to see the chunk be streamed in
#	update()
#	pass

"""
Treat this method like a constructor. It initialises the chunk with all the
data it requires to begin streaming in the blocks.
"""
func init_stream(_world_image: Image, _chunk_position: Vector2, _block_count: Vector2, _block_pixel_size: Vector2):
	self.world_image = _world_image
	self.chunk_position = _chunk_position
	self.block_count = _block_count
	self.block_pixel_size = _block_pixel_size
	self.position = _block_pixel_size * _chunk_position * _block_count
	

func lock():
	assert(volatile_streaming == false)
	volatile_streaming = true

func is_locked():
	return volatile_streaming

func is_loaded():
	return loaded

"""
This function is true only after a chunk has been fully loaded AND then drawn.
This is so the chunks draw call is cached and the terrain knows not to try
and draw this chunk to the screen again.
"""
func is_drawn():
	return self.drawn

func create():
	if self.is_loaded():
		return
	
	self.chunk_texture = ImageTexture.new()
	self.chunk_image = Image.new()
	self.chunk_image.create(self.block_count.x, self.block_count.y, false, Image.FORMAT_RGBA8)
#	self.chunk_image.resize(
#		block_count.x * block_pixel_size.x,
#		block_count.y * block_pixel_size.y,
#	)
	self.chunk_image.lock()
	for i in range(self.block_count.x):
		for j in range(self.block_count.y):
			var block_position := Vector2(i, j)
			if !blocks.has(block_position):
				blocks[block_position] = {}
	
			var block_pixel_position: Vector2 = self.chunk_position * block_count + block_position
			
			# Grab the colour for the pixel from the world image. If the pixel
			# goes out of bounds then just draw Red. This happens when the image is
			# not a multiple of the chunk size.
			var pixel: Color
			
			# The old colour formula
#			pixel = Color(
	#				abs(sin(((i + j) * 4) / 255.0)),
	#				abs(sin((200 - (i * 2)) / 255.0)),
	#				abs(sin((200 - (j * 3)) / 255.0))
	##				0
	##				abs(sin(randi()) - 0.5)
	#			)
			
			if (block_pixel_position.x < 0 ||
				block_pixel_position.y < 0 ||
				block_pixel_position.x >= self.world_image.get_size().x ||
				block_pixel_position.y >= self.world_image.get_size().y):
				pixel = Color.red
			else:
				pixel = Color(self.world_image.get_pixelv(block_pixel_position))
			
			self.chunk_image.set_pixelv(block_position, pixel)
			blocks[block_position]["id"] = pixel.a
			blocks[block_position]["colour"] = pixel
			
	self.chunk_image.unlock()
	self.chunk_texture.create_from_image(chunk_image, Texture.FLAG_MIPMAPS)
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
	if self.is_loaded():
		self.drawn = true
	else:
		return
	
#	self.chunk_texture.create_from_image(chunk_image, Texture.FLAG_MIPMAPS | Texture.FLAG_ANISOTROPIC_FILTER)
	draw_set_transform(Vector2.ZERO, 0, block_pixel_size)
	draw_texture(self.chunk_texture, Vector2.ZERO)
	draw_set_transform(Vector2.ZERO, 0, Vector2(1, 1))
	return

func get_block_from_block_position(block_position: Vector2):
	if blocks.has(block_position):
		return blocks[block_position]
	return null

func set_block_from_block_position(block_position: Vector2, new_block: Dictionary):
	self.chunk_image.lock()
	
	if new_block["id"] == 0:
		self.chunk_image.set_pixelv(block_position, Color(0, 0, 0, 0))
	else:
		self.chunk_image.set_pixelv(block_position, new_block["colour"])
		
	
	
	self.chunk_image.unlock()
	blocks[block_position] = new_block
	update()

func set_block_colour_from_int(i: int, j: int, colour: Color):
	set_block_colour(Vector2(i, j), colour)

func set_block_colour(block_position: Vector2, colour: Color):
	self.chunk_image.lock()
	self.chunk_image.set_pixelv(block_position, colour)
	self.chunk_image.unlock()
	
	blocks[block_position]["colour"] = colour
	update()

func _draw():
	draw_chunk()

#	draw_circle(Vector2.ZERO, 2, Color.aquamarine)
