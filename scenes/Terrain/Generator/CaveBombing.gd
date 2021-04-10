tool
extends Node

func cave_bomb(image: Image, use_borders: bool=true) -> Image:
	# From https://www.reddit.com/r/roguelikedev/comments/81prau/contour_bombing_cave_generation/
	# http://www.darkgnosis.com/2018/03/03/contour-bombing-cave-generation-algorithm
	# This algorithm is low and behold too slow
	image.lock()
	var min_x = 0
	var min_y = 0
	var max_x = image.get_width()
	var max_y = image.get_height()
	var candidates := []

	# create list of tile candidates
	for i in range(max_x):
		for j in range(max_y):
			var colour: Color = image.get_pixel(i, j)
			
			if colour != Color.black:
				candidates.append(Vector2(i, j))

	# shuffle them all
#	candidates.shuffle()
	
#	var iteration_multiplier = 4.8
	var iteration_multiplier = 1.5
	var iteration_number = candidates.size() * iteration_multiplier

	for k in range(iteration_number):
		var random_offset: float = 0;
		var tile_variation: Color = Color.white
	
		# 1/3 chance that we will use as a bombing point one of the last 15 positions
		if randi() % 3 == 0:
			random_offset = floor(rand_range(candidates.size() - 15, candidates.size() - 1))
			tile_variation = Color.white
		else:
			# otherwise use lower half of remaining tiles
			random_offset = randi() % candidates.size()/2
			tile_variation = Color.cyan
	
		# check boundaries
		if random_offset >= candidates.size():
			random_offset = candidates.size() - 1
		if random_offset < 0:
			random_offset = 0
	
		var tx = candidates[random_offset].x
		var ty = candidates[random_offset].y
		var map_width = image.get_width()
		var map_height = image.get_height()
#		var use_borders = true;
	   
		# we will use bombs of radius 1 mostly with smaller chance (1/20)
		# that radius will be of size 2
		var bomb_radius: int
		if randi() % 20 == 0:
			bomb_radius = 1
		else:
			bomb_radius = 2
	    
		# bomb    
		var start_x = max(0, tx - bomb_radius - 1)
		var end_x = max(map_width, tx + bomb_radius)
		for x in range(start_x, end_x):
			var start_y = max(0, ty - bomb_radius - 1)
			var end_y = max(map_height, ty + bomb_radius)
			for y in range(start_y, end_y):
				# check if tile is within the circle
				if ((x - tx)*(x - tx) + (y - ty)*(y - ty) < bomb_radius * bomb_radius + bomb_radius):
					if use_borders:
						if x < min_x or x >= max_x or y < min_y or y >= max_y:
							continue;
		            
					# if we have at least one tile bombed on screen
					# push those coordinates to candidate list
					var colour = image.get_pixel(x, y)
					if colour != tile_variation:
						image.set_pixel(x, y, tile_variation)
						candidates.append(Vector2(x, y))
		
		# erase our bombing cell, it is re-added in bombing loop above, if at least one tile is changed.
		candidates.erase(random_offset)
	image.unlock()
	return image

