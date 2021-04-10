tool
extends Node

func blank_image(image_size: Vector2) -> Image:
	"""
	This method returns a blank (black) image of the specified size
	"""
	var image: Image = Image.new()
	image.create(image_size.x, image_size.y, false, Image.FORMAT_RGBA8)
	image.fill(Color.black)
	
	return image

func random_image(image_size: Vector2, world_seed: int) -> Image:
	"""
	This method returns an image of the specified size where every pixel's value
	is completely random. Currently this returns a black and white image.
	"""
	var image: Image = blank_image(image_size)
	seed(world_seed)
	
	image.lock()
	# Fill the image with random data
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			var value = round(randf())
			image.set_pixel(i, j, Color(value, value, value))
	image.unlock()
	return image

func invert_image(image: Image) -> Image:
	"""
	This method returns a new image where all the pixels that aren't alpha==0,
	are inverted. The original image should not be affected.
	"""
	var new_image: Image = blank_image(image.get_size())
	
	image.lock()
	new_image.lock()
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			var colour: Color = image.get_pixel(i, j)
			colour = colour.inverted()
			new_image.set_pixel(i, j, colour)
	
	image.unlock()
	new_image.unlock()
	return new_image

enum BLEND_TYPE {
	MERGE = 0,
	ADD = 1,
	SUBTRACT = 2,
	MULTIPLY = 3,
	OVERLAY = 4,
}

func blend_images(image_base: Image, top_layer: Image, type) -> Image:
	"""
	This method merges the top_layer image onto the bottom image like in paint.
	Any pixels in the top layer with an alpha==0 are ignored. The base image
	is changed and also returned.
	"""
	if (image_base.get_width() != top_layer.get_width() or
		image_base.get_height() != top_layer.get_height()):
		return image_base
	
	var new_image: Image = blank_image(image_base.get_size())
	
	new_image.lock()
	image_base.lock()
	top_layer.lock()
	for i in range(image_base.get_width()):
		for j in range(image_base.get_height()):
			var top_colour: Color = top_layer.get_pixel(i, j)
			var bottom_colour: Color = image_base.get_pixel(i, j)
			
			match type:
				BLEND_TYPE.MERGE:
					if top_colour.a != 0:
						new_image.set_pixel(i, j, top_colour)
				BLEND_TYPE.ADD:
					var new_colour: Color = Color(
							top_colour.r + bottom_colour.r,
							top_colour.g + bottom_colour.g,
							top_colour.b + bottom_colour.b
					)
					new_image.set_pixel(i, j, new_colour)
				BLEND_TYPE.SUBTRACT:
					var new_colour: Color = Color(
							top_colour.r - bottom_colour.r,
							top_colour.g - bottom_colour.g,
							top_colour.b - bottom_colour.b
					)
					new_image.set_pixel(i, j, new_colour)
				BLEND_TYPE.MULTIPLY:
					var new_colour: Color = Color(
							top_colour.r * bottom_colour.r,
							top_colour.g * bottom_colour.g,
							top_colour.b * bottom_colour.b
					)
					new_image.set_pixel(i, j, new_colour)
				BLEND_TYPE.OVERLAY:
					# From https://en.wikipedia.org/wiki/Blend_modes#Overlay
					var a = bottom_colour
					var b = top_colour
					var new_colour: Color = Color()
					# Red
					if bottom_colour.r < 0.5:
						new_colour.r = 2 * a.r * b.r
					else:
						new_colour.r = 1 - 2 * (1 - a.r) * (1 - b.r)
					# Green
					if bottom_colour.g < 0.5:
						new_colour.g = 2 * a.g * b.g
					else:
						new_colour.g = 1 - 2 * (1 - a.g) * (1 - b.g)
					# Blue
					if bottom_colour.b < 0.5:
						new_colour.b = 2 * a.b * b.b
					else:
						new_colour.b = 1 - 2 * (1 - a.b) * (1 - b.b)
					new_image.set_pixel(i, j, new_colour)
	image_base.unlock()
	top_layer.unlock()
	new_image.unlock()
	return new_image

func dig_circle(image, location: Vector2, radius: int, colour: Color):
	image.lock()
	
	for i in range(location.x - radius, location.x + radius + 1):
		for j in range(location.y - radius, location.y + radius + 1):
			if (i < 0 or i > image.get_width() - 1 or
					j < 0 or j > image.get_height() - 1):
				continue
			var block_location: Vector2 = Vector2(i, j)
			var distance: float = location.distance_to(block_location)
			if distance <= radius:
				image.set_pixel(i, j, colour)
	image.unlock()
	return image

func gradient_point(world_size: Vector2, point: Vector2, gradient_radius: int, gradient_falloff: int) -> Image:
	assert(gradient_radius != 0)
	var image: Image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
	image.fill(Color.black)
	
	image.lock()
	for i in range(world_size.x):
		for j in range(world_size.y):
			var pixel_location = Vector2(i, j)
			var distance_to_point = pixel_location.distance_to(point) - gradient_falloff
			
			var draw_pixel: bool
			if distance_to_point == 0:
				draw_pixel = true
			else:
				# We need the random number generator to beat this value
				var random_cutoff = distance_to_point / gradient_radius # Value [0, 1]
				var random_value = randf() + 1 / 2 # Between 0 and 1
				draw_pixel = random_value < random_cutoff
				
			if draw_pixel:
				image.set_pixel(i, j, Color.white)
	image.unlock()
	return image

func gradient_down(world_size: Vector2, bottom_threshold: int, top_threshold: int) -> Image:
	var image: Image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBA8)
	image.fill(Color.black)
	
	image.lock()
	var colours := []
	for j in range(image.get_height()):
#		if j < top_threshold:
#			colours.append(Color.black)
#			continue
#		if j > bottom_threshold:
#			colours.append(Color.white)
#			continue
		
		var gradient = (j - bottom_threshold) / float(top_threshold - bottom_threshold)
		colours.append(Color(
			gradient,
			gradient,
			gradient
		))
			
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			image.set_pixel(i, j, colours[j])
	image.unlock()
	return image

func generate_voronoi_diagram(world_size : Vector2, num_cells: int) -> Image:
	"""
	From https://www.reddit.com/r/godot/comments/bazs8m/quick_voronoi_diagram/
	
	For very large maps, it may be better to scale the image down to a smaller
	size, create the diagram, and scale it back. The blended pixels may in the
	future be recalculated. Otherwise, a nearest neighbour apporach can cut down
	on the time by a lot, for sacrificing a bit of pixel perfect detail.

	"""
	var image = Image.new()
	image.create(world_size.x, world_size.y, false, Image.FORMAT_RGBH)

	var points = []
	var colors = []
	
	for _i in range(num_cells):
		points.push_back(Vector2(int(randf()*image.get_size().x), int(randf()*image.get_size().y)))
		
#		randomize()
#		var colorPossibilities = [ Color.blue, Color.red, Color.green, Color.purple, Color.yellow, Color.orange]
#		colors.push_back(colorPossibilities[randi()%colorPossibilities.size()])
		colors.push_back(Color(
			randf() + 1 / 2,
			randf() + 1 / 2,
			randf() + 1 / 2
		))
		
	image.lock()
	for y in range(image.get_size().y):
		for x in range(image.get_size().x):
			var dmin = image.get_size().length()
			var j = -1
			for i in range(num_cells):
				var d = (points[i] - Vector2(x, y)).length()
				if d < dmin:
					dmin = d
					j = i
			image.set_pixel(x, y, colors[j])
	image.unlock()
	return image

func black_and_white(image: Image, threshold: float) -> Image:
	var noise := OpenSimplexNoise.new()
	noise.seed = 5
	noise.octaves = 2
	noise.period = 150
	noise.persistence = 0.5
	
	image.lock()
	for i in range(image.get_width()):
		for j in range(image.get_height()):
#			var gray_value = image.get_pixel(i, j).gray()
			var gray_value = image.get_pixel(i, j).v
			
#			var new_threshold = (noise.get_noise_1d(i) + 1) / 2 * threshold
			var new_threshold = threshold
			
			if gray_value < new_threshold:
				image.set_pixel(i, j, Color.black)
			else:
				image.set_pixel(i, j, Color.white)
	image.unlock()
	return image

func flood_fill(image: Image, location: Vector2, desired_colour: Color) -> Image:
	image.lock()
	var base_colour: Color = image.get_pixelv(location)
	var fringe: Array = [location]
	var explored: Dictionary = {}
	
	while fringe.size() > 0:
		var current_node: Vector2 = fringe.pop_back()
		var i = current_node.x
		var j = current_node.y
		
		# Add to explored
		explored[current_node] = null
		
		if (i < 0 or j < 0 or i >= image.get_width() or j >= image.get_height()):
			continue
		
		var current_node_colour: Color = image.get_pixelv(current_node)
		if current_node_colour == base_colour:
			image.set_pixelv(current_node, desired_colour)
			
			var neighbours = [
				Vector2(i - 1, j),
				Vector2(i + 1, j),
				Vector2(i, j - 1),
				Vector2(i, j + 1),
			]
			
			for neighbour in neighbours:
				if explored.has(neighbour):
					continue
				fringe.append(neighbour)
	
	image.unlock()
	return image

func rotate_image(image: Image, degrees: int) -> Image:
	"""Rotate clockwise"""
	degrees %= 360
	if degrees == 0:
		return image
	
	image.lock()
	
	var new_image: Image
	if degrees == 90:
		var new_size = Vector2(
			image.get_height(),
			image.get_width()
		)
		new_image = blank_image(new_size)
		
		new_image.lock()
		for i in range(image.get_width()):
			for j in range(image.get_height()):
				var pixel_colour = image.get_pixel(i, j)
				var x = image.get_height() - 1 - j
				var y = i
				new_image.set_pixel(x, y, pixel_colour)
		new_image.unlock()
	elif degrees == 180:
		new_image = blank_image(image.get_size())
		new_image.lock()
		for i in range(image.get_width()):
			for j in range(image.get_height()):
				var pixel_colour = image.get_pixel(i, j)
				var x = image.get_width() - 1 - i
				var y = image.get_height() - 1 - j
				new_image.set_pixel(x, y, pixel_colour)
		new_image.unlock()
	elif degrees == 270:
		var new_size = Vector2(
			image.get_height(),
			image.get_width()
		)
		new_image = blank_image(new_size)
		
		new_image.lock()
		for i in range(image.get_width()):
			for j in range(image.get_height()):
				var pixel_colour = image.get_pixel(i, j)
				var x = j
				var y = image.get_width() - 1 - i
				new_image.set_pixel(x, y, pixel_colour)
		new_image.unlock()
	else:
		assert(false)
	
	image.unlock()
	return new_image

func add_border(image: Image, border: int, colour=Color.white) -> Image:
	assert(border > 0)
	var new_image: Image = blank_image(Vector2(
		image.get_width() + 2 * border,
		image.get_height() + 2 * border
	))
	new_image.fill(colour)
	
	image.lock()
	new_image.lock()
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			var pixel_colour = image.get_pixel(i , j)
			new_image.set_pixel(i + border, j + border, pixel_colour)
	image.unlock()
	new_image.unlock()
	
	return new_image
