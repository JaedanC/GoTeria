extends Node2D
class_name Chunk

onready var terrain = get_tree().get_root().find_node("Terrain", true, false)
onready var thread_pool = get_tree().get_root().find_node("ThreadPool", true, false)

var world_image: Image
var chunk_image: Image
var chunk_texture: ImageTexture
var volatile_streaming: int
var multiple_thread_create_mutex: Mutex

var chunk_position: Vector2
var block_count: Vector2
var block_pixel_size: Vector2

var blocks: Dictionary
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
func _init(_world_image: Image, _block_count: Vector2, _block_pixel_size: Vector2):
	self.world_image = _world_image
	self.block_count = _block_count
	self.block_pixel_size = _block_pixel_size
	self.blocks = {}
	self.multiple_thread_create_mutex = Mutex.new()
	reset()

"""
This is the method that is called when a chunk is reset before it is reused.
"""
func reset():
	self.volatile_streaming = 0
	self.loaded = false
	self.drawn = false
	self.calls = 0

"""
This method sets up the chunk with values that it requires to be streamed.
"""
func init_stream(_chunk_position: Vector2):
	self.chunk_position = _chunk_position
	self.position = self.block_pixel_size * self.chunk_position * self.block_count

"""
This method marks the chunk as 'locked'. 
"""
func lock():
	assert(self.volatile_streaming == 0)
	self.volatile_streaming += 1

func is_locked():
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

"""
This method begins the streaming in of blocks if chunk is not locked or if it
has been already loaded. The streaming in of blocks is sent to a ThreadPool to
increase performance.
"""
func stream():
	if is_locked() or is_loaded():
		return
	lock()
	thread_pool.submit_task_unparameterized(self, "create")

"""
This method loads the corresponing blocks from the world image into the blocks
Dictionary. This section of code is a critical section, so it is therefore
guarded by a Mutex. This method may be run twice if a chunk has not finished
streaming in before it becomes too close to the player. This does not affect
the loading but something the texture may throw an error.
"""
func create():
	calls += 1
	if calls != 1:
		print("Calls: ", calls)
		return
		
	multiple_thread_create_mutex.lock()
	
	
	# This is to check if the next thread that could have been waiting is
	# accidentally about to do work a second time.
	if self.loaded:
		multiple_thread_create_mutex.unlock()
		return
	
	
	# Create a local image so multiple threads don't race to write to the
	# instance variable chunk_image.
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
	
	# The texture must always keep scope. If it doesn't, errors are thrown all
	# over the shop. In the special case where a thread and main are running
	# this method, we only want to keep one of them when the instance variables
	# are being written to, otherwise they may get out of sync. It's better to
	# be safe than sorry.
	self.chunk_texture = ImageTexture.new()
	self.chunk_texture.create_from_image(self.chunk_image, Texture.FLAG_MIPMAPS)
	
	# So since this method is run on a thread, it is very important that we
	# the loaded variable to true after everything has actually been loaded.
	self.loaded = true
	multiple_thread_create_mutex.unlock()

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
	
	self.chunk_texture.create_from_image(chunk_image, Texture.FLAG_MIPMAPS | Texture.FLAG_ANISOTROPIC_FILTER)
	draw_set_transform(Vector2.ZERO, 0, block_pixel_size)
	draw_texture(self.chunk_texture, Vector2.ZERO)
	draw_set_transform(Vector2.ZERO, 0, Vector2(1, 1))

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
#	draw_circle(Vector2.ZERO, 2, Color.aquamarine)
	draw_chunk()
