tool
extends Node

var presets := {}

func simplex_presets():
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

func simplex_noise_preset(world_size: Vector2, world_seed: int, preset: Dictionary) -> Image:
	return simplex_noise(
		world_size,
		world_seed,
		preset["octaves"],
		preset["period"],
		preset["persistence"]
	)

func simplex_noise(world_size: Vector2, world_seed: int, octaves: int, period: float, persistence: float) -> Image:
	"""
	This algorithm uses Simplex Noise to create a new Image of the specified size.
	The parameters of the Simplex Noise generator are supplied. Google the details
	for more information on each of the parameters.
	"""
	var image: Image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
	image.lock()
	
	var noise = OpenSimplexNoise.new()
	# Configure
	noise.seed = world_seed
	noise.octaves = octaves
	noise.period = period
	noise.persistence = persistence
	
	for i in range(self.world_size.x):
		for j in range(self.world_size.y):
			var value = (noise.get_noise_2d(i, j) + 1) / 2
			var colour = Color(value, value, value)
			
			image.set_pixel(i, j, colour)
	image.unlock()
	return image
