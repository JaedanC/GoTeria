extends Node2D

var rigidbody = null
var camera = null
var smoothing = null
var terrain = null

var velocity = Vector2(0, 0)

var original_player_hitbox = null
var next_physics_player_hitbox = null
var visibility_hitbox = null

var block_scene = preload("res://scenes/Block.tscn")
var blocks = {}

func _ready():
	self.camera = find_node("Camera")
	self.rigidbody = find_node("Rigidbody")
	self.smoothing = find_node("Smoothing")
	self.terrain = get_tree().get_root().find_node("Terrain", true, false)

func _process(_delta):
	assert(self.position == Vector2(0, 0))
	if InputLayering.pop_action("zoom_reset"):
		camera.zoom = Vector2(1, 1)
	
	if InputLayering.pop_action("click"):
		var world_position = screen_to_world_position(get_viewport().get_mouse_position())
		var block = terrain.get_block_from_world_position(world_position)
		if block:
			block["id"] = 1
			block["colour"] = Color(randf(), randf(), randf(), 0.2)
			terrain.set_block_at_world_position(world_position, block)
	
	if InputLayering.pop_action("dig"):
		var world_position = screen_to_world_position(get_viewport().get_mouse_position())
		var block = terrain.get_block_from_world_position(world_position)
		if block:
			block["id"] = 0
			terrain.set_block_at_world_position(world_position, block)
	
	update()

func _physics_process(delta):
	if InputLayering.pop_action("move_left"):
		velocity.x -= 50
	
	if InputLayering.pop_action("move_right"):
		velocity.x += 50
	
	if InputLayering.pop_action("move_up"):
		velocity.y += -50
	
	if InputLayering.pop_action("move_down"):
		velocity.y += 50
	
	if InputLayering.pop_action("jump"):
		velocity.y = -1000
	
	if InputLayering.pop_action("brake"):
		velocity = Vector2(0, 0)
	
	
	update_collision_visiblity_rect(delta)
	delete_invisible_blocks_hitboxes()
	create_visible_blocks_hitboxes()
	move(velocity)
	
func move(vector : Vector2):
	self.rigidbody.move_and_slide(vector, Vector2(0, -1))
	if self.rigidbody.is_on_floor():
		self.velocity.y = 0
	if self.rigidbody.is_on_wall():
		self.velocity.x = 0

func _input(event):
	if event.is_action_pressed("zoom_in"):
		camera.zoom += Vector2(0.25, 0.25)
	
	if event.is_action_pressed("zoom_out"):
		camera.zoom -= Vector2(0.25, 0.25)
	
	camera.zoom.x = clamp(camera.zoom.x, 0.5, 3)
	camera.zoom.y = clamp(camera.zoom.y, 0.5, 3)

func create_visible_blocks_hitboxes():
	var visible_blocks = get_hitbox_visibility_points(visibility_hitbox)
	
	for visible_block_point in visible_blocks:
		if blocks.has(visible_block_point):
			continue
		
		var existing_block = terrain.get_block_from_world_position(visible_block_point * terrain.block_pixel_size)
		if existing_block and existing_block["id"] != 0:
			var block = block_scene.instance()
			block.position = visible_block_point * terrain.block_pixel_size + terrain.block_pixel_size / 2
			add_child(block)
			
			blocks[visible_block_point] = block

func delete_invisible_blocks_hitboxes():
	var visible_blocks = {}
	
	for visible_block_point in get_hitbox_visibility_points(visibility_hitbox):
		if blocks.has(visible_block_point):
			var existing_block_data = terrain.get_block_from_world_position(visible_block_point * terrain.block_pixel_size)
			if existing_block_data["id"] == 0:
				continue
			visible_blocks[visible_block_point] = blocks[visible_block_point]
			blocks.erase(visible_block_point)
	
	for invisible_blocks in blocks.keys():
		blocks[invisible_blocks].queue_free()
	
	blocks = visible_blocks

func get_visibility_points() -> Array:
	"""
	This method returns a list of integer Vector2's that represent the indexes of
	chunks that should be loaded for the player. This takes into account where
	the player is and the camera zoom by performing some neat mathematical
	tricks on the viewport, player and camera, position vectors.
	
	Example Output:
	[
		Vector2(0, 0),
		Vector2(0, 1),
		Vector2(1, 0),
		Vector2(1, 1)
	]
	"""
	# Grab important data
	var terrain = get_tree().get_root().find_node("Terrain", true, false)
	var smoothed_position = find_node("Smoothing").position
	var viewport_rectangle = get_viewport_rect()
	
	# Using the viewport rectangle as a base, centre a copy of it around the player.
	# We used smoothed position because we want to take the interpolated position of
	# the player so visibility still works at high speeds.
	viewport_rectangle.position = smoothed_position - viewport_rectangle.size/2
	
	# Expand this Rectangle to take into account camera zooming by using two
	# Vector2's the point the the corners of the screen and using the Rect2.expand
	# method. TODO: This strategy does not take into account zooming in, only zooming
	# out is considered. Adding this feature would only slightly increase the fps
	# when zoomed in and thus it isn't a high priority
	viewport_rectangle = viewport_rectangle.expand(smoothed_position - (get_viewport_rect().size / 2) * camera.zoom)
	viewport_rectangle = viewport_rectangle.expand(smoothed_position + (get_viewport_rect().size / 2) * camera.zoom)
	
	# Convert the top left and bottom right points of the Rect2 into an integer
	# that we can loop through to get the points for the visible chunks. This
	# takes into account the size of each chunk.
	var chunk_dimensions = terrain.get_chunk_pixel_dimensions()
	var top_left = (viewport_rectangle.position / chunk_dimensions).floor()
	var bottom_right = ((viewport_rectangle.position + viewport_rectangle.size) / chunk_dimensions).floor()
	
	# Loop through these two values and include them in the visibility point
	# list. +1 is there so that the right and bottom edge are forced to be
	# included. Remove it if you don't know what I mean an then you'll see.
	var visibility_points = []
	for i in range(top_left.x, bottom_right.x + 1):
		for j in range(top_left.y, bottom_right.y + 1):
			var visible_point = Vector2(i, j)
			visibility_points.append(visible_point)

	return visibility_points

func get_rigidbody():
	return rigidbody

func screen_to_world_position(screen_position : Vector2) -> Vector2:
	var world_position = self.smoothing.position + screen_position * camera.zoom
	return world_position - camera.zoom * get_viewport_rect().size/2

func update_collision_visiblity_rect(delta):
	# TODO Hardcoded hitbox size
	var collision_visibility = find_node("Hitbox", true, false).position - Vector2(32, 32)
	original_player_hitbox = Rect2(collision_visibility + rigidbody.position, 2 * Vector2(32, 32))
	next_physics_player_hitbox = original_player_hitbox
	next_physics_player_hitbox.position += velocity * delta
	visibility_hitbox = next_physics_player_hitbox.merge(original_player_hitbox)

func get_hitbox_visibility_points(area : Rect2) -> Array:
	var visibility_points = []
	var top_left = (area.position / terrain.block_pixel_size).floor()
	var bottom_right = ((area.position + area.size) / terrain.block_pixel_size).floor()
	for i in range(top_left.x, bottom_right.x + 1):
		for j in range(top_left.y, bottom_right.y + 1):
			var visible_point = Vector2(i, j)
			visibility_points.append(visible_point)
	return visibility_points

func _draw():
#	for point in get_visibility_points():
#		draw_circle(
#			point * terrain.get_chunk_pixel_dimensions(),
#			15,
#			Color(0, 1, 0, 1)
#		)
	
#	if original_player_hitbox:
#		draw_rect(original_player_hitbox, Color(0, 0, 1, 0.1))
#		draw_rect(next_physics_player_hitbox, Color(0, 1, 0, 0.1))
#		draw_rect(visibility_hitbox, Color(1, 0, 0, 0.1))
#		for point in get_hitbox_visibility_points(visibility_hitbox):
#			draw_circle(
#				point * terrain.block_pixel_size,
#				5,
#				Color(0, 1, 0, 1)
#			)
	
	draw_circle(
		screen_to_world_position(get_viewport().get_mouse_position()),
		5,
		Color(1, 1, 0, 1)
	)
	
	draw_line(
		screen_to_world_position(get_viewport().size/2),
		screen_to_world_position(get_viewport().size/2 + velocity / 10),
		Color(1, 0, 0)
	)
	
	pass
