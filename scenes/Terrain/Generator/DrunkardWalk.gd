tool
extends Node

func drunkard_walk(image: Image, world_seed: int, drunkards: int, steps: int) -> Image:
	"""
	This algorithm spawns a number of drunkards (starting pixels) and then walks
	them the desired number of steps in any random (8 point) direction. Whereever
	drunkards walk they turn the pixels to white. The original image is changed
	and then returned.
	"""
	seed(world_seed)
	
	image.lock()
	for _drunk_index in range(drunkards):
		var starting_postiion: Vector2 = Vector2(
			randi() % int(image.get_width()),
			randi() % int(image.get_height())
		)
		
		# Choose a random direction
		for _step in range(steps + 1):
			starting_postiion += Vector2(
				randi() % 3 - 1,
				randi() % 3 - 1
			)
			
			# Dig gone out of bounds
			if not (starting_postiion.x >= 0 and starting_postiion.x < image.get_width() and
					starting_postiion.y >= 0 and starting_postiion.y < image.get_height()):
				continue
			
			var colour: Color = Color()
			
			# Colourful dig
#			colour.v = 0.4
#			colour.s = 1
#			colour.h = float(_step) / steps
			
			# White draw
			colour = Color(1, 1, 1)
			
			image.set_pixelv(starting_postiion, colour)
	
	image.unlock()
	return image
