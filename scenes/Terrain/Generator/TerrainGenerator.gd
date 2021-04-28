 tool
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
export(int) var height: int setget set_height
export(int) var offset: int setget set_offset
export(Vector2) var gradient_point: Vector2 setget set_gradient_point
export(int) var gradient_radius: int setget set_gradient_radius
export(int) var gradient_falloff: int setget set_gradient_falloff
export(int) var top_threshold: int setget set_top_threshold
export(int) var bottom_threshold: int setget set_bottom_threshold

var refresh = false

var num_textures = 100
var textures: Array
var images: Array

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

func set_height(_height: int):
	if _height != height:
		height = _height
		refresh = true

func set_offset(_offset: int):
	if _offset != offset:
		offset = _offset
		refresh = true

func set_gradient_point(_gradient_point: Vector2):
	if _gradient_point != gradient_point:
		gradient_point = _gradient_point
		refresh = true

func set_gradient_radius(_gradient_radius: int):
	if _gradient_radius != gradient_radius:
		gradient_radius = _gradient_radius
		refresh = true

func set_gradient_falloff(_gradient_falloff: int):
	if _gradient_falloff != gradient_falloff:
		gradient_falloff = _gradient_falloff
		refresh = true

func set_bottom_threshold(_bottom_threshold: int):
	if _bottom_threshold != bottom_threshold:
		bottom_threshold = _bottom_threshold
		refresh = true

func set_top_threshold(_top_threshold: int):
	if _top_threshold != top_threshold:
		top_threshold = _top_threshold
		refresh = true

func _ready():
	self.images = []
	self.textures = []
	for _i in range(num_textures):
		self.textures.append(ImageTexture.new())
	
	self.refresh = true

func _process(_delta):
	OS.set_window_title("Teria | FPS: " + str(Engine.get_frames_per_second()))
	
	if not refresh:
		return
	
	refresh = false
	update()
	self.images.clear()
	
	var simplex_noise = OpenSimplexNoise.new()
	# Configure Simplex Noise
	simplex_noise.set_seed(self.world_seed)
	simplex_noise.octaves = self.octaves
	simplex_noise.period = self.period
	simplex_noise.persistence = self.persistence
	
#	print("Updating World")
#	var texture_image = load("res://dollar_sign.png").get_data()
	var image: Image
#	var gradient_image: Image
	var simplex_image: Image
	
	# Create a blank world
#	image = $ImageTools.blank_image(self.world_size, Color.black)
#	image = $ImageTools.gradient_point(self.world_size, self.gradient_point, self.gradient_radius, self.gradient_falloff)
#	gradient_image = $ImageTools.gradient_down(self.world_size, self.bottom_threshold, self.top_threshold)
#	var reduce: float = 3
#	image = $ImageTools.generate_voronoi_diagram(self.world_size / reduce, self.steps)
#	image.resize(
#		image.get_width() * reduce,
#		image.get_height() * reduce,
#		Image.INTERPOLATE_NEAREST 
#	)

#	var carve_points = $DrunkardWalk.simplex_drunkard_carver(image)
#	for carve_point in carve_points:
#		image = $ImageTools.dig_circle(image, carve_point.get_location(), carve_point.get_radius(), Color.white)
	
#	image = $DrunkardWalk.drunkard_walk(image, self.world_seed, self.drunkards, self.steps)
#	image = $DrunkardWalk.drunkard_walk(image, self.world_seed, self.drunkards, self.steps, Vector2(image.get_width()/2, image.get_height()/2))
	
	# Create a noisey world
#	image = $ImageTools.random_image(self.world_size, self.world_seed)
	
	# Create a simplex world
	simplex_image = $SimplexNoise.simplex_noise(self.world_size, simplex_noise)
#	simplex_image = $SimplexNoise.simplex_line(self.world_size, simplex_noise, self.height, self.offset)
	
	# Add Drunkards
#	image = $DrunkardWalk.drunkard_walk(simplex_noise, image, self.drunkards, self.steps, image.get_size() / 2, Color.white)
#	image = $DrunkardWalk.simplex_drunkard_carver(image)
	
	# Cave Bombing Technique
#	image = $CaveBombing.cave_bomb(image)
	
	# Remove the islands based on these parameters
#	var new_image = $IslandFinder.get_islands_faster(image, self.smallest_island, self.largest_island, self.include_diagonal)
#	var new_image = $IslandFinder.get_islands(image, self.smallest_island, self.largest_island, self.include_diagonal)
#	var new_image = $IslandFinder.get_islands_naive_fastest(image, self.smallest_island, self.largest_island, self.include_diagonal)
	
#	new_image = $ImageTools.invert_image(new_image)
#	var blended_image: Image = $ImageTools.blend_images(gradient_image, simplex_image, $ImageTools.BLEND_TYPE.OVERLAY)
#	var blended_image: Image = $ImageTools.blend_images(simplex_image, gradient_image, $ImageTools.BLEND_TYPE.OVERLAY)
	
	simplex_image = $ImageTools.black_and_white(simplex_image, self.threshold)

#	image = $ImageTools.flood_fill(image, Vector2.ZERO, Color.aqua)
	
#
#	image = $ImageTools.add_border(image, 1, Color(0, 0, 0, 0))
#	image = $ImageTools.flood_fill(image, Vector2.ZERO, Color.pink)
	
#	var flipped_image = rotated_image_1.flip_x()
	
	# Resize the image
#	image.resize(
#		image.get_width() * scale_factor,
#		image.get_height() * scale_factor,
#		Image.INTERPOLATE_TRILINEAR
#	)
#
#	image = $CellularAutomator.cellular_auto(image, self.iterations)
	
	image = cave_stickers_algorithm()
	
	simplex_image = $ImageTools.change_colour(simplex_image, Color.black, Color(0, 0, 0, 0))
	
	var new_image: Image = $ImageTools.blend_images(image, simplex_image, $ImageTools.BLEND_TYPE.MERGE)
	
	draw_image(image)
	draw_image(simplex_image)
#	draw_image(new_image)
#	draw_images(cave_stickers)
#	draw_image(rotated_image_3)
#	draw_image(blended_image)
#	draw_image(gradient_image)

func cave_stickers_algorithm() -> Image:
	var simplex_noise = OpenSimplexNoise.new()
	simplex_noise.set_seed(self.world_seed)
	simplex_noise.octaves = 0
	simplex_noise.period = 1
	simplex_noise.persistence = 0.4
	
	var image: Image = $ImageTools.blank_image(self.world_size, Color.black)
	var cave_stickers = []
	for i in range(max(1, self.iterations)):
		cave_stickers.append($CompleteAlgorithms.generate_cave_sticker(self.world_seed + i, simplex_noise, self.world_size, self.drunkards, self.steps))
	
	return $CompleteAlgorithms.place_cave_stickers(self.world_seed, image, cave_stickers, self.drunkards)

func draw_image(image: Image):
	self.images.append(image)

func draw_images(multiple_images: Array):
	for image in multiple_images:
		self.images.append(image)

func _draw():
	var largest_image = Vector2.ZERO
	for image in self.images:
		largest_image.x = max(image.get_width(), largest_image.x)
		largest_image.y = max(image.get_height(), largest_image.y)
	
	var per_column: int = 5
	for i in range(self.images.size()):
		if i >= self.textures.size():
			break
		var texture = self.textures[i]
		var image = self.images[i]
		
		# Draw image
		texture.create_from_image(image, Texture.FLAG_MIPMAPS | Texture.FLAG_ANISOTROPIC_FILTER)
		var x = (i / per_column) * largest_image.x
		var y = (i % per_column) * largest_image.y
		draw_texture(texture, Vector2(x, y))
