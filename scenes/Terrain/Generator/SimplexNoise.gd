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

func simplex_noise(world_size: Vector2, noise: OpenSimplexNoise) -> Image:
	"""
	This algorithm uses Simplex Noise to create a new Image of the specified size.
	The parameters of the Simplex Noise generator are supplied. Google the details
	for more information on each of the parameters.
	"""
	var image: Image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
	image.lock()
	
	for i in range(world_size.x):
		for j in range(world_size.y):
			var value = (noise.get_noise_2d(i, j * 2) + 1) / 2
			var colour = Color(value, value, value)
			
			image.set_pixel(i, j, colour)
	image.unlock()
	return image

func simplex_line(world_size: Vector2, noise: OpenSimplexNoise, height: float, offset: int) -> Image:
	var image: Image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
	
	var height_line = []
	for i in range(world_size.x):
		height_line.append(noise.get_noise_1d(i) * height)
	
	image.lock()
	for i in range(world_size.x):
		for j in range(world_size.y):
			if j + offset > height_line[i]:
				image.set_pixel(i, j, Color.white)
			else:
				image.set_pixel(i, j, Color.black)
	image.unlock()
	return image
