tool
extends Node

func drunkard_walk(noise: OpenSimplexNoise, image, drunkards: int, steps: int, starting_point=null, colour: Color=Color.white) -> Image:
	"""
	This algorithm spawns a number of drunkards (starting pixels) and then walks
	them the desired number of steps in any random (8 point) direction. Where ever
	drunkards walk they turn the pixels to the input colour. The resulting image
	is cropped to only include the drunkard. The original image is modified then
	returned.
	
	This algorithm will stop when the drunkard goes out of bounds so it is advised
	to make the starting position the centre of the image. The noise parameter
	is used to fluctuate to radius of the dig when carving out random holes.
	"""
	var image_tools = get_parent().find_node("ImageTools")
	
	image.lock()
	for drunk_index in range(drunkards):
		var current_position: Vector2
		if starting_point == null:
			current_position = Vector2(
				randi() % int(image.get_width()),
				randi() % int(image.get_height())
			)
		else:
			current_position = starting_point
		
		# Choose a random direction
		var possible_directions = [
			Vector2(-1, 0),
			Vector2(-1, -1),
			Vector2(0, -1),
			Vector2(1, -1),
			Vector2(1, 0),
			Vector2(1, 1),
			Vector2(0, 1),
			Vector2(-1, 1)
		]
		var reverse_direction_offset = possible_directions.size() / 2
		var prev_direction = randi() % possible_directions.size()
		
		var min_x = current_position.x
		var max_x = current_position.x
		var min_y = current_position.y
		var max_y = current_position.y
		for step in range(steps + 1):
			"""
			Only let the next direction go in 7 directions -> not backward. This
			makes caves look more narrow as the algorithm will much more rarily
			turn back on itself.
			"""
			# A number in this range [1, possible_directions.size()]
			var random_direction = randi()
			var safe_offset = int(random_direction) % (possible_directions.size() - 1) + 1
			var next_direction = (prev_direction + safe_offset) % possible_directions.size()
			current_position += possible_directions[next_direction]
			
			# Treat the previous direction as the reversed Vector2
			prev_direction = (next_direction + reverse_direction_offset) % possible_directions.size()
			
			# Dig gone out of bounds so ignore it
			if not (current_position.x >= 0 and current_position.x < image.get_width() and
					current_position.y >= 0 and current_position.y < image.get_height()):
				continue
			
			# Fluctuate the size of the dig to be a radius between 0 and the value
			# below. This makes caves look less algorithmic.
			var max_radius = 2.2
			var random_radius = ((noise.get_noise_1d(step) + 1) / 2) * max_radius
			
			# Keep track of the size of the image
			min_x = min(current_position.x, min_x)
			max_x = max(current_position.x, max_x)
			min_y = min(current_position.y, min_y)
			max_y = max(current_position.y, max_y)
			
			# We're about the write outside the bounds of the map so stop
			if (min_x - random_radius < 0 or max_x + random_radius > image.get_width() or 
					min_y - random_radius < 0 or max_y + random_radius > image.get_height()):
				break
			
			# 1 Pixel dig
#			image.set_pixelv(current_position, colour)

			# Multi-pixel dig
			image_tools.dig_circle(image, current_position, random_radius, Color.white)
	
	# Crop out the excess pixels
	var used_rect: Rect2 = image.get_used_rect()
	image.blit_rect(image, used_rect, Vector2.ZERO)
	image.crop(used_rect.size.x, used_rect.size.y)
	
	image.unlock()
	return image
