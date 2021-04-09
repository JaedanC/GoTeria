# tool
extends Node2D

export(int) var world_seed: int setget set_world_seed
export(Vector2) var world_size: Vector2 setget set_world_size
export(int) var octaves: int setget set_octaves
export(float) var period: float setget set_period
export(float) var persistence: float setget set_persistence
export(float) var threshold: float setget set_threshold
export(int) var drunkards: int setget set_drunkards
export(int) var steps: int setget set_steps
export(int) var iterations: int setget set_iterations
export(float) var scale_factor: float setget set_scale_factor
export(int) var largest_island: int setget set_largest_island
export(int) var smallest_island: int setget set_smallest_island
export(bool) var include_diagonal: bool setget set_include_diagonal

var refresh = false

var textures: Array
var images: Dictionary 


"""
This script is responsible for creating the underlying .png that will eventually
become the basis for the world generation algorithm.
"""

func set_world_size(_world_size: Vector2):
	if _world_size != world_size:
		world_size = _world_size
		refresh = true

func set_world_seed(_world_seed: int):
	if _world_seed != world_seed:
		world_seed = _world_seed
		refresh = true

func set_octaves(_octaves: int):
	if _octaves != octaves:
		octaves = _octaves
		refresh = true

func set_period(_period: float):
	if _period != period:
		period = _period
		refresh = true

func set_persistence(_persistence: float):
	if _persistence != persistence:
		persistence = _persistence
		refresh = true

func set_threshold(_threshold: float):
	if _threshold != threshold:
		threshold = _threshold
		refresh = true

func set_drunkards(_drunkards: int):
	if _drunkards != drunkards:
		drunkards = _drunkards
		refresh = true

func set_steps(_steps: int):
	if _steps != steps:
		steps = _steps
		refresh = true

func set_iterations(_iterations: int):
	if _iterations != iterations:
		iterations = _iterations
		refresh = true

func set_scale_factor(_scale_factor: float):
	if _scale_factor != scale_factor:
		scale_factor = _scale_factor
		refresh = true
		
func set_largest_island(_largest_island: int):
	if _largest_island != largest_island:
		largest_island = _largest_island
		refresh = true

func set_smallest_island(_smallest_island: int):
	if _smallest_island != smallest_island:
		smallest_island = _smallest_island
		refresh = true

func set_include_diagonal(_include_diagonal: bool):
	if _include_diagonal != include_diagonal:
		include_diagonal = _include_diagonal
		refresh = true

func _ready():
	self.textures.append(ImageTexture.new())
	self.textures.append(ImageTexture.new())
	self.textures.append(ImageTexture.new())
	self.textures.append(ImageTexture.new())
	self.textures.append(ImageTexture.new())
	self.textures.append(ImageTexture.new())
	
	self.refresh = true

func _process(_delta):
	OS.set_window_title("Teria | FPS: " + str(Engine.get_frames_per_second()))
	
	if not refresh:
		return
	
	refresh = false
	update()
	
	
	print("Updating World")
	var image: Image
	
	# Create a blank world
	image = $ImageTools.blank_image(self.world_size)
	
	# Create a noisey world
#	var image: Image = $RandomImage.random_image(self.world_size, self.world_seed)
	
	# Create a simplex world
#	var image: Image = $SimplexNoise.simplex_noise(self.world_size, self.world_seed, self.octaves, self.period, self.persistence)
	
	# Add Drunkards
	image = $DrunkardWalk.drunkard_walk(image, self.world_seed, self.drunkards, self.steps)
	
	# Remove the islands based on these parameters
#	var new_image = $IslandFinder.get_islands_faster(image, self.smallest_island, self.largest_island, self.include_diagonal)
	var new_image = $IslandFinder.get_islands(image, self.smallest_island, self.largest_island, self.include_diagonal)
#	var new_image = $IslandFinder.get_islands_naive_fastest(image, self.smallest_island, self.largest_island, self.include_diagonal)
	new_image = $ImageTools.invert_image(new_image)
	image = $ImageTools.merge_images(image, new_image, $ImageTools.MERGE_TYPE.MERGE)
	
#	var image_islands_two = $IslandFinder.get_islands(image, 0, 10, true)
#	image_islands_two = $ImageTools.invert_image(image_islands_two)
#	image = $ImageTools.merge_images(image, image_islands_two, $ImageTools.MERGE_TYPE.MERGE)
	
	
	# Resize the image
#	image.resize(
#		image.get_width() * scale_factor,
#		image.get_height() * scale_factor,
#		Image.INTERPOLATE_TRILINEAR
#	)
#
	image = $CellularAutomator.cellular_auto(image, self.iterations)
#
#	draw_image(image_islands_two, Vector2(image_islands.get_size().x * 2, 0))
	draw_image(image)
	draw_image(new_image, Vector2(image.get_size().x, 0))

func draw_image(image: Image, location=Vector2.ZERO):
	self.images[image] = location

func _draw():
	var i = 0
	for image in self.images.keys():
		var location = images[image]
		var texture = self.textures[i % self.textures.size()]
		texture.create_from_image(image, Texture.FLAG_MIPMAPS | Texture.FLAG_ANISOTROPIC_FILTER)
		draw_texture(texture, location)
		i += 1

