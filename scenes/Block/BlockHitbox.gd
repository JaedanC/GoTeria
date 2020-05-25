extends CollisionShape2D


func _ready():
	var shape = get_shape()
	var terrain = get_tree().get_root().find_node("Terrain", true, false)
	shape.extents = terrain.block_pixel_size / 2
