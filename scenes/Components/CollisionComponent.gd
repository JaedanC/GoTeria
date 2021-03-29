extends Node2D

var terrain = null
var parent = null
var parent_hitbox = null
var parent_rigidbody = null

var prev_parent_hitbox_rect = null
var next_parent_hitbox_rect = null
var merged_parent_hitbox_rect = null

var block_scene = preload("res://scenes/Block/Block.tscn")
var loaded_blocks = {}

func _ready():
	terrain = get_tree().get_root().find_node("Terrain", true, false)
	parent = get_parent()
	parent_hitbox = parent.find_node("Hitbox", true, false)
	parent_rigidbody = parent.find_node("Rigidbody", true, false)
#	parent.velocity

func _physics_process(delta):
	update_collision_visiblity_rect(delta)
	delete_invisible_blocks_hitboxes()
	create_visible_blocks_hitboxes()
	move(parent.velocity, delta)

func update_collision_visiblity_rect(delta):
	var hitbox_size = parent_hitbox.get_shape().get_extents()
	# TODO Hardcoded hitbox size
	var collision_visibility = parent_hitbox.position - hitbox_size
	prev_parent_hitbox_rect = Rect2(collision_visibility + parent_rigidbody.position, 2 * hitbox_size)
	next_parent_hitbox_rect = prev_parent_hitbox_rect
	next_parent_hitbox_rect.position += parent.velocity * delta
	merged_parent_hitbox_rect = next_parent_hitbox_rect.merge(prev_parent_hitbox_rect)

func create_visible_blocks_hitboxes():
	var visible_blocks = get_hitbox_visibility_points(merged_parent_hitbox_rect)
	for visible_block_point in visible_blocks:
		if loaded_blocks.has(visible_block_point):
			continue
		
		var existing_block = terrain.get_block_from_world_position(visible_block_point * terrain.block_pixel_size)
		if existing_block and existing_block["id"] != 0:
			var block = block_scene.instance()
			block.position = visible_block_point * terrain.block_pixel_size + terrain.block_pixel_size / 2
			add_child(block)
			loaded_blocks[visible_block_point] = block

func delete_invisible_blocks_hitboxes():
	var visible_blocks = {}
	for visible_block_point in get_hitbox_visibility_points(merged_parent_hitbox_rect):
		if loaded_blocks.has(visible_block_point):
			var existing_block_data = terrain.get_block_from_world_position(visible_block_point * terrain.block_pixel_size)
			if existing_block_data["id"] == 0: # Don't add collision for air. TODO: use a future is_solid() method
				continue
			visible_blocks[visible_block_point] = loaded_blocks[visible_block_point]
			loaded_blocks.erase(visible_block_point)
	
	for invisible_blocks in loaded_blocks.keys():
		loaded_blocks[invisible_blocks].queue_free()
	
	loaded_blocks = visible_blocks

func get_hitbox_visibility_points(area : Rect2) -> Array:
	var visibility_points = []
	var top_left = (area.position / terrain.block_pixel_size).floor()
	var bottom_right = ((area.position + area.size) / terrain.block_pixel_size).floor()
	for i in range(top_left.x, bottom_right.x + 1):
		for j in range(top_left.y, bottom_right.y + 1):
			var visible_point = Vector2(i, j)
			visibility_points.append(visible_point)
	return visibility_points

func move(vector : Vector2, delta):
#	self.parent_rigidbody.move_and_slide_with_snap(vector, Vector2(0, -1))
#	self.parent.velocity = self.parent_rigidbody.move_and_slide(vector, Vector2(0, -1))
	var horizontal_vector = Vector2(vector.x, 0)
	var vertical_vector = Vector2(0, vector.y)
	
	var new_horizontal_vector = self.parent_rigidbody.move_and_slide(horizontal_vector)
	var new_vertical_vector = self.parent_rigidbody.move_and_slide(vertical_vector)
	self.parent.velocity = Vector2(new_horizontal_vector.x, new_vertical_vector.y)
	
	
#	self.parent_rigidbody.move_and_slide
#	var collision = self.parent_rigidbody.move_and_collide(delta * vector)
#	if collision:
#		var normal = collision.get_normal()
#		print(normal)
#		if normal == Vector2(0, -1):
#			self.parent.velocity.y = 0
#		if normal == Vector2(1, 0):
#			self.parent.velocity.x = 0
#		if normal == Vector2(-1, 0):
#			self.parent.velocity.x = 0
		
#	if self.parent_rigidbody.is_on_floor():
#		self.parent.velocity.y = 0
#	if self.parent_rigidbody.is_on_wall():
#		self.parent.velocity.x = 0

func _process(delta):
	update()

func _draw():
	if (merged_parent_hitbox_rect != null):
		for point in get_hitbox_visibility_points(merged_parent_hitbox_rect):
			var world_location = point * terrain.block_pixel_size
			draw_circle(world_location, 1, Color(0, 1, 0))
