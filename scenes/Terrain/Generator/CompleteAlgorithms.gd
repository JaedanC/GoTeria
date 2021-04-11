tool
extends Node

onready var TerrainGenerator = get_parent()
onready var ImageTools = get_parent().get_node("ImageTools")
onready var DrunkardWalk = get_parent().get_node("DrunkardWalk")

func _ready():
	pass

func generate_cave_sticker(world_seed: int, noise: OpenSimplexNoise, max_size: Vector2, drunkards: int, steps: int) -> Image:
	seed(hash(world_seed))
	
	var drunkard_colour = Color.white
	var flood_fill_colour = Color.red
	
	"""A blank image with alpha=0"""
	var image: Image = ImageTools.blank_image(max_size, Color(0, 0, 0, 0))
	
	"""Create a drunkard and let it move within the confines of the image
	This will return an image that is cropped, only containing the changed pixels
	We start the drunkard in the centre of the image to promote movement"""
	image = DrunkardWalk.drunkard_walk(noise, image, drunkards, steps, image.get_size() / 2, drunkard_colour)
	
	"""Add a single border that we can use to guarantee a successful flood fill"""
	image = ImageTools.add_border(image, 1, Color(0, 0, 0, 0))
	
	"""Flood fill around the formation to isolate any islands in the middle of
	the image"""
	image = ImageTools.flood_fill(image, Vector2.ZERO, flood_fill_colour)
	
	"""Swap the colours around to only keep the drunkard and the filled in
	holes"""
	# Fill holes
	image = ImageTools.change_colour(image, Color(0, 0, 0, 0), drunkard_colour)
	# Transparent Background
	image = ImageTools.change_colour(image, flood_fill_colour, Color(0, 0, 0, 0))
	
	return image

func place_cave_stickers(world_seed: int, image: Image, cave_stickers: Array, how_many: int):
	seed(hash(world_seed))
	
	var valid_rotations = [
		0,
		90,
		180,
		270
	]
	
#	for i in range(how_many):
	for sticker in cave_stickers:
#		var sticker: Image = cave_stickers[randi() % cave_stickers.size()]
		
		# Random location
		var sticker_location = Vector2(
			randi() % image.get_width(),
			randi() % image.get_height()
		)
		
		# Random rotation
		var sticker_rotation = valid_rotations[randi() % valid_rotations.size()]
		sticker = ImageTools.rotate_image(sticker, sticker_rotation)
		
#		TerrainGenerator.draw_image(image)
		image = ImageTools.blend_images(image, sticker, ImageTools.BLEND_TYPE.MERGE, sticker_location)
#		TerrainGenerator.draw_image(image)
	
	return image
