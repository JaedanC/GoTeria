tool
extends Node

func cellular_auto(image: Image, iterations: int) -> Image:
	"""
	This algorithm takes in an image and applies simple rules to each pixel.
	
	Pixels are considered solid when they are (1, 1, 1, 1)
	All other pixel are considered to be non-solid for the rules.
	
	Rules:
	1. If a pixel is solid and there are less than two solid blocks surrounding:
		- Become non-solid (black)
	2. If a pixel is non-solid and there are more than four solid blocks surrounding:
		- Become solid (white)
	3. Otherwise stay the same
	
	The oringal image is changed and returned
	"""
	image.lock()
	var next_image: Image = Image.new()
	# Perform the cellular passes
	for _iteration in range(iterations):
		next_image.create(image.get_width(), image.get_height(), false, Image.FORMAT_RGBA8)
		next_image.copy_from(image)
		
		next_image.lock()
		for i in range(image.get_width()):
			for j in range(image.get_height()):
				if (i == 0 or i == image.get_width() - 1 or
					j == 0 or j == image.get_height() - 1):
					next_image.set_pixel(i, j, image.get_pixel(i, j))
					continue
				
				# Count the neighbouring walls
				var results := 0
				results += int(image.get_pixel(i - 1, j - 1) == Color(1, 1, 1))
				results += int(image.get_pixel(i - 1, j) 	 == Color(1, 1, 1))
				results += int(image.get_pixel(i - 1, j + 1) == Color(1, 1, 1))
				results += int(image.get_pixel(i, j - 1) 	 == Color(1, 1, 1))
				results += int(image.get_pixel(i, j + 1) 	 == Color(1, 1, 1))
				results += int(image.get_pixel(i + 1, j - 1) == Color(1, 1, 1))
				results += int(image.get_pixel(i + 1, j) 	 == Color(1, 1, 1))
				results += int(image.get_pixel(i + 1, j + 1) == Color(1, 1, 1))
				
				var is_solid = image.get_pixel(i, j) == Color.white
				
				var colour: Color
				if is_solid and results < 3:
					colour = Color(0, 0, 0)
				elif !is_solid and results > 4:
					colour = Color(1, 1, 1)
				else:
					colour = image.get_pixel(i, j)
#
#				if results == 0:
#					colour = Color(1, 1, 1)
#				elif results >= 1 and results <= 4:
#					colour = Color(0, 0, 0)
#				elif results >= 5:
#					colour = Color(1, 1, 1)
				
				next_image.set_pixel(i, j, colour)
				
		for i in range(image.get_width()):
			for j in range(image.get_height()):
				image.set_pixel(i, j, next_image.get_pixel(i, j))
		next_image.unlock()
	image.unlock()
	return image
