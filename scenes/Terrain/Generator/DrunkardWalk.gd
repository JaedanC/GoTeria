tool
extends Node

func drunkard_walk(noise: OpenSimplexNoise, image, world_seed: int, drunkards: int, steps: int, starting_point=null) -> Image:
	"""
	This algorithm spawns a number of drunkards (starting pixels) and then walks
	them the desired number of steps in any random (8 point) direction. Whereever
	drunkards walk they turn the pixels to white. The original image is changed
	and then returned.
	"""
	seed(world_seed)
	
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
#			Vector2(-1, -1),
			Vector2(0, -1),
#			Vector2(1, -1),
			Vector2(1, 0),
#			Vector2(1, 1),
			Vector2(0, 1),
#			Vector2(-1, 1)
		]
		var reverse_direction_offset = possible_directions.size() / 2
		var prev_direction = -1
		
		for step in range(steps + 1):
			"""
			Vector list implementation.
			"""
			# A number in this range [1, possible_directions.size()]
			var random_direction = randi()
			var safe_offset = int(random_direction) % (possible_directions.size() - 1) + 1
			var next_direction = (prev_direction + safe_offset) % possible_directions.size()
			current_position += possible_directions[next_direction]
			
			# Treat the previous direction as the reversed Vector2
			prev_direction = (next_direction + reverse_direction_offset) % possible_directions.size()
			
			# Dig gone out of bounds
			if not (current_position.x >= 0 and current_position.x < image.get_width() and
					current_position.y >= 0 and current_position.y < image.get_height()):
				continue
			
			var colour: Color = Color()
			
			# Colourful dig
#			colour.v = 0.4
#			colour.s = 1
#			colour.h = float(_step) / steps
			
			# White draw
			colour = Color(1, 1, 1)
			
#			image.set_pixelv(current_position, colour)
			
			# [-1, 1] -> [1, 2]
			var random_radius = ((noise.get_noise_1d(step) + 1) / 2) * 2.5
			image_tools.dig_circle(image, current_position, random_radius, Color.white)
	
	image.unlock()
	return image

class CarvePoint:
	var location: Vector2
	var radius: float
	func _init(_location: Vector2, _radius: float):
		self.location = _location
		self.radius = _radius
	
	func get_location() -> Vector2:
		return self.location
	
	func get_radius() -> float:
		return self.radius

func simplex_drunkard_carver(image: Image) -> CarvePoint:
	var radius_values := []
	var reduction = 0
	var maximum_steps = 50
	var beginning_value = randi() % 10 + 5 # [5, 10]
	var current_step = 0
	
	var new_noise = OpenSimplexNoise.new()
	new_noise.seed = 0
	new_noise.octaves = 2
	new_noise.period = 5
	new_noise.persistence = 0.5
	
	while true:
		var next_radius = beginning_value - reduction + new_noise.get_noise_1d(current_step) * 5
		
		if next_radius < 0:
			break
		
		radius_values.append(next_radius)
#		reduction += int(randi() % 2 > 0.5)
		
		current_step += 1
		if current_step > maximum_steps:
			break
	
#	print(radius_values)
	
	var current_point: Vector2 = Vector2(
		randi() % image.get_width(),
		randi() % image.get_height()
	)
	
	var random_direction = Vector2(1, 0).rotated(deg2rad(360 * randf()))
	var carving_points = []
	
	current_step = 0
	var variable_new_direction = deg2rad(90) # degrees
	var direction_hop = 10
	for radius in radius_values:
		random_direction = random_direction.rotated(variable_new_direction * new_noise.get_noise_1d(current_step + maximum_steps))
#		print(random_direction)
		current_point = current_point + random_direction * direction_hop
		carving_points.append(current_point)
		current_step += 1
#	print(carving_points)
	
	var carving_point_objects = []
	for i in range(carving_points.size()):
		carving_point_objects.append(CarvePoint.new(carving_points[i], radius_values[i]))
	
	return carving_point_objects
