 tool
extends Node2D

export(int) var world_seed: int
export(Vector2) var world_size: Vector2
export(int) var octaves: int
export(float) var period: float
export(float) var persistence: float
export(float) var threshold: float

export(int) var drunkards: int
export(int) var steps: int

export(int) var iterations: int

var prev_world_size: Vector2
var prev_world_seed: int
var prev_octaves: int
var prev_period: float
var prev_persistence: float
var prev_threshold: float

var prev_drunkards: int
var prev_steps: int

var prev_iterations: int

var presets = {}
var texture: ImageTexture

func save_presets():
	self.presets["gold"] = { 
		"octaves": 2,
		"period": 13.8,
		"persistence": -18.4,
		"threshold": 0.779
	}
	self.presets["cave1"] = {
		"octaves": 4,
		"period": 85,
		"persistence": 0.35,
		"threshold": 0.366
	}

func _ready():
	self.world_seed = 0
	self.world_size = Vector2(512, 512)
	self.octaves = 4
	self.period = 20.0
	self.persistence = 0.8
	self.threshold = 0
	
	self.texture = ImageTexture.new()
	
	save_presets()
	
	self.prev_world_size = self.world_size
	self.prev_world_seed = self.world_seed
	self.prev_octaves = self.octaves
	self.prev_period = self.period
	self.prev_persistence = self.persistence
	self.prev_threshold = self.threshold
	
	self.prev_drunkards = self.drunkards
	self.prev_steps = self.steps
	
	self.prev_iterations = self.iterations
	
	update()

func random_image(world_size: Vector2, _world_seed: int):
	var image: Image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
	image.lock()
	seed(_world_seed)
	# Fill the image with random data
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			var value = round(randf())
			image.set_pixel(i, j, Color(value, value, value))
	image.unlock()
	return image

func simplex_noise_preset(world_size: Vector2, world_seed: int, preset: Dictionary) -> Image:
	return simplex_noise(
		world_size,
		world_seed,
		preset["octaves"],
		preset["period"],
		preset["persistence"]
	)

func simplex_noise(world_size: Vector2, _world_seed: int, _octaves: int, _period: float, _persistence: float) -> Image:
	var image: Image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
	image.lock()
	
	var noise = OpenSimplexNoise.new()
	# Configure
	noise.seed = _world_seed
	noise.octaves = _octaves
	noise.period = _period
	noise.persistence = _persistence
	
	for i in range(self.world_size.x):
		for j in range(self.world_size.y):
			var value = (noise.get_noise_2d(i, j) + 1) / 2
			var colour = Color(value, value, value)
			
			image.set_pixel(i, j, colour)
	image.unlock()
	return image

func drunkard_walk(world_size: Vector2, _world_seed: int, drunkards: int, steps: int):
	var image: Image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
	image.fill(Color.white)
	seed(_world_seed)
	
	image.lock()
	for drunk_index in range(drunkards):
		var starting_postiion: Vector2 = Vector2(
			randi() % int(world_size.x),
			randi() % int(world_size.y)
		)
		
		for step in range(steps):
			if not (starting_postiion.x >= 0 && starting_postiion.x < image.get_width() &&
				starting_postiion.y >= 0 && starting_postiion.y < image.get_height()):
				continue
			
			# Dig position
			var colour: Color = Color()
			
			# Colourful dig
			colour.v = 0.4
			colour.s = 1
			colour.h = float(step) / steps
			
			# Black and White dig
			colour = Color(0, 0, 0)
			
			image.set_pixelv(starting_postiion, colour)
			
			# Random direction
			starting_postiion += Vector2(
				randi() % 3 - 1,
				randi() % 3 - 1
			)
	
	image.unlock()
	return image

func cellular_auto(world_size: Vector2, _world_seed: int, iterations: int):
	seed(_world_seed)
	
	var image: Image = random_image(world_size, _world_seed)
	image.lock()
	
	var next_image: Image = Image.new()
	# Perform the cellular passes
	for iteration in range(iterations):
		next_image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
		next_image.copy_from(image)
		
		next_image.lock()
		for i in range(image.get_width()):
			for j in range(image.get_height()):
				if (i == 0 || i == image.get_width() - 1 ||
					j == 0 || j == image.get_height() - 1):
					next_image.set_pixel(i, j, image.get_pixel(i, j))
					continue
				
				# Count the neighbouring walls
				var results := 0
				results += int(image.get_pixel(i - 1, j - 1) == Color(1, 1, 1))
				results += int(image.get_pixel(i - 1, j) 	 == Color(1, 1, 1))
				results += int(image.get_pixel(i - 1, j + 1) == Color(1, 1, 1))
				results += int(image.get_pixel(i, j - 1) 	 == Color(1, 1, 1))
				results += int(image.get_pixel(i, j + 1) 	 == Color(1, 1, 1))
				results += int(image.get_pixel(i + 1, j - 1) == Color(1, 1, 1))
				results += int(image.get_pixel(i + 1, j) 	 == Color(1, 1, 1))
				results += int(image.get_pixel(i + 1, j + 1) == Color(1, 1, 1))
				
				var is_solid = image.get_pixel(i, j) == Color.white
				
				var colour: Color
				if is_solid && results < 3:
					colour = Color(0, 0, 0)
				elif !is_solid && results > 4:
					colour = Color(1, 1, 1)
				else:
					colour = image.get_pixel(i, j)
#
#				if results == 0:
#					colour = Color(1, 1, 1)
#				elif results >= 1 && results <= 4:
#					colour = Color(0, 0, 0)
#				elif results >= 5:
#					colour = Color(1, 1, 1)
				
				next_image.set_pixel(i, j, colour)
				
		for i in range(image.get_width()):
			for j in range(image.get_height()):
				image.set_pixel(i, j, next_image.get_pixel(i, j))
		next_image.unlock()
	image.unlock()
	return image

func _process(_delta):
	OS.set_window_title("Teria | FPS: " + str(Engine.get_frames_per_second()))
	
	if (prev_world_seed != world_seed ||
		prev_world_size != world_size ||
		prev_octaves != octaves ||
		prev_period != period ||
		prev_persistence != persistence ||
		prev_threshold != threshold ||
		prev_drunkards != drunkards ||
		prev_steps != steps ||
		prev_iterations != iterations):
		update()
	
	self.prev_world_size = self.world_size
	self.prev_world_seed = self.world_seed
	self.prev_octaves = self.octaves
	self.prev_period = self.period
	self.prev_persistence = self.persistence
	self.prev_threshold = self.threshold
	
	self.prev_drunkards = self.drunkards
	self.prev_steps = self.steps
	
	self.prev_iterations = self.iterations

func _draw():
#	var image: Image = simplex_noise(self.world_size, self.world_seed, self.octaves, self.period, self.persistence)
#	var image: Image = drunkard_walk(self.world_size, self.world_seed, self.drunkards, self.steps)
	var image: Image = cellular_auto(self.world_size, self.world_seed, self.iterations)
	
	texture.create_from_image(image, Texture.FLAG_MIPMAPS | Texture.FLAG_ANISOTROPIC_FILTER)
	draw_texture(texture, Vector2.ZERO)
