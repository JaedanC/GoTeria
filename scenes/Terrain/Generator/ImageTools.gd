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

enum MERGE_TYPE {
	MERGE = 0,
	ADD = 1,
	SUBTRACT = 2,
	MULTIPLY = 3,
}

func merge_images(image_base: Image, top_layer: Image, type) -> Image:
	"""
	This method merges the top_layer image onto the bottom image like in paint.
	Any pixels in the top layer with an alpha==0 are ignored. The base image
	is changed and also returned.
	"""
	if (image_base.get_width() != top_layer.get_width() or
		image_base.get_height() != top_layer.get_height()):
		return image_base
	
	image_base.lock()
	top_layer.lock()
	for i in range(image_base.get_width()):
		for j in range(image_base.get_height()):
			var top_colour: Color = top_layer.get_pixel(i, j)
			var bottom_colour: Color = image_base.get_pixel(i, j)
			
			match type:
				MERGE_TYPE.MERGE:
					if top_colour.a != 0:
						image_base.set_pixel(i, j, top_colour)
				MERGE_TYPE.ADD:
					var new_colour: Color = Color(
							top_colour.r + bottom_colour.r,
							top_colour.g + bottom_colour.g,
							top_colour.b + bottom_colour.b
					)
					image_base.set_pixel(i, j, new_colour)
				MERGE_TYPE.SUBTRACT:
					var new_colour: Color = Color(
							top_colour.r - bottom_colour.r,
							top_colour.g - bottom_colour.g,
							top_colour.b - bottom_colour.b
					)
					image_base.set_pixel(i, j, new_colour)
				MERGE_TYPE.MULTIPLY:
					var new_colour: Color = Color(
							top_colour.r * bottom_colour.r,
							top_colour.g * bottom_colour.g,
							top_colour.b * bottom_colour.b
					)
					image_base.set_pixel(i, j, new_colour)
			
	image_base.unlock()
	top_layer.unlock()
	return image_base

func dig_circle(image: Image, location: Vector2, radius: int, colour: Color):
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
