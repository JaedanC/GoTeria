extends Node2D

onready var terrain = get_tree().get_root().find_node("Terrain", true, false)
onready var rigidbody = find_node("Rigidbody")
onready var smoothing = find_node("Smoothing")
onready var camera = find_node("Camera")

var velocity := Vector2(0, 0)

func _ready():
	pass
	
func _process(_delta):
	assert(self.position == Vector2(0, 0))
	if InputLayering.pop_action("zoom_reset"):
		camera.zoom = Vector2(1, 1)
	
	if InputLayering.pop_action("click"):
		var world_position: Vector2 = screen_to_world_position(get_viewport().get_mouse_position())
		var block = terrain.get_block_from_world_position(world_position)
		if block:
			block["id"] = 1
			block["colour"] = Color(randf(), randf(), randf(), 0.6)
			terrain.set_block_at_world_position(world_position, block)
	
	if InputLayering.pop_action("dig"):
		var world_position: Vector2 = screen_to_world_position(get_viewport().get_mouse_position())
		var block = terrain.get_block_from_world_position(world_position)
		if block:
			block["id"] = 0
			terrain.set_block_at_world_position(world_position, block)
	
	update()

func _physics_process(_delta):
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

func _input(event):
	if event.is_action_pressed("zoom_in"):
		camera.zoom += Vector2(0.25, 0.25)
	
	if event.is_action_pressed("zoom_out"):
		camera.zoom -= Vector2(0.25, 0.25)
	
	if event.is_action_pressed("quit"):
		get_tree().quit()
	
	camera.zoom.x = clamp(camera.zoom.x, 0.5, 10)
	camera.zoom.y = clamp(camera.zoom.y, 0.5, 10)

func get_visibility_world_position_corners() -> Array:
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
		Vector2(1, 1),
		...
	]
	"""
	# Grab important data
	var smoothed_position: Vector2 = smoothing.position
	var viewport_rectangle: Rect2 = get_viewport_rect()
	
	# Using the viewport rectangle as a base, centre a copy of it around the player.
	# We used smoothed position because we want to take the interpolated position of
	# the player so visibility still works at high speeds.
	viewport_rectangle.position = smoothed_position - viewport_rectangle.size/2
	
	# Expand this Rectangle to take into account camera zooming by using two
	# Vector2's the point the the corners of the screen and using the Rect2.expand
	# method.
	viewport_rectangle = viewport_rectangle.expand(smoothed_position - (get_viewport_rect().size / 2) * camera.zoom)
	viewport_rectangle = viewport_rectangle.expand(smoothed_position + (get_viewport_rect().size / 2) * camera.zoom)
	
	# Use this to temporarily reduce the size of the viewport loading rectangle
	# to watch the chunks be streamed in. 0 is no effect. 1 is no vision.
	var viewport_modifier := 0.4
#	var viewport_modifier := 0.2
#	var viewport_modifier := 0
	var size: Vector2 = viewport_rectangle.size * Vector2(viewport_modifier, viewport_modifier)
	viewport_rectangle = viewport_rectangle.grow_individual(-size.x, -size.y, -size.x, -size.y)
	
	# Convert the top left and bottom right points of the Rect2 into an integer
	# that we can loop through to get the points for the visible chunks. This
	# takes into account the size of each chunk.
	var top_left: Vector2 = viewport_rectangle.position
	var bottom_right: Vector2 = viewport_rectangle.position + viewport_rectangle.size
	
	
	return [top_left, bottom_right]

func get_visibility_world_block_position_corners() -> Array:
	var corners = get_visibility_world_position_corners()
	var world_block_position_corners = [
		(corners[0] / terrain.get_block_pixel_size()).floor(),
		(corners[1] / terrain.get_block_pixel_size()).floor()
	]
	return world_block_position_corners

func get_visibility_chunk_position_corners() -> Array:
	var world_position_corners = get_visibility_world_position_corners()
	var chunk_position_top_left = (world_position_corners[0] / terrain.get_chunk_pixel_dimensions()).floor()
	var chunk_position_bottom_right = (world_position_corners[1] / terrain.get_chunk_pixel_dimensions()).floor()
	return [
		chunk_position_top_left,
		chunk_position_bottom_right
	]

func get_visibility_chunk_positions(margin=0, border_only=false) -> Array:
	var chunk_corners = get_visibility_chunk_position_corners()
	
	# Now include the margin
	var top_left_margin = chunk_corners[0] - Vector2.ONE * margin
	var bottom_right_margin = chunk_corners[1] + Vector2.ONE * margin
	
	var visibility_points := []
	if border_only:
		for i in range(top_left_margin.x, bottom_right_margin.x + 1):
			for j in range(top_left_margin.y, bottom_right_margin.y + 1):
				if (i < chunk_corners[0].x or j < chunk_corners[0].y or
						i >= chunk_corners[1].x or j >= chunk_corners[1].y):
					visibility_points.append(Vector2(i, j))
	else:
		for i in range(top_left_margin.x, bottom_right_margin.x + 1):
			for j in range(top_left_margin.y, bottom_right_margin.y + 1):
				visibility_points.append(Vector2(i, j))

	return visibility_points
	
#func get_visibility_points(margin=0, border_only=false) -> Array:
#	"""
#	This method returns a list of integer Vector2's that represent the indexes of
#	chunks that should be loaded for the player. This takes into account where
#	the player is and the camera zoom by performing some neat mathematical
#	tricks on the viewport, player and camera, position vectors.
#
#	Example Output:
#	[
#		Vector2(0, 0),
#		Vector2(0, 1),
#		Vector2(1, 0),
#		Vector2(1, 1),
#		...
#	]
#	"""
#	# Grab important data
#	var smoothed_position: Vector2 = smoothing.position
#	var viewport_rectangle: Rect2 = get_viewport_rect()
#
#	# Using the viewport rectangle as a base, centre a copy of it around the player.
#	# We used smoothed position because we want to take the interpolated position of
#	# the player so visibility still works at high speeds.
#	viewport_rectangle.position = smoothed_position - viewport_rectangle.size/2
#
#	# Expand this Rectangle to take into account camera zooming by using two
#	# Vector2's the point the the corners of the screen and using the Rect2.expand
#	# method.
#	viewport_rectangle = viewport_rectangle.expand(smoothed_position - (get_viewport_rect().size / 2) * camera.zoom)
#	viewport_rectangle = viewport_rectangle.expand(smoothed_position + (get_viewport_rect().size / 2) * camera.zoom)
#
#	# Use this to temporarily reduce the size of the viewport loading rectangle
#	# to watch the chunks be streamed in. 0 is no effect. 1 is no vision.
#	var viewport_modifier := 0.4
##	var viewport_modifier := 0.2
##	var viewport_modifier := 0
#	var size: Vector2 = viewport_rectangle.size * Vector2(viewport_modifier, viewport_modifier)
#	viewport_rectangle = viewport_rectangle.grow_individual(-size.x, -size.y, -size.x, -size.y)
#
#	# Convert the top left and bottom right points of the Rect2 into an integer
#	# that we can loop through to get the points for the visible chunks. This
#	# takes into account the size of each chunk.
#	var chunk_dimensions: Vector2 = terrain.get_chunk_pixel_dimensions()
#	var top_left: Vector2 = (viewport_rectangle.position / chunk_dimensions).floor()
#	var bottom_right: Vector2 = ((viewport_rectangle.position + viewport_rectangle.size) / chunk_dimensions).floor()
#
#	# Loop through these two values and include them in the visibility point
#	# list. +1 is there so that the right and bottom edge are forced to be
#	# included. Remove it if you don't know what I mean an then you'll see.
#	var visibility_points := []
#	if border_only:
#		top_left -= Vector2.ONE * 1
#		bottom_right += Vector2.ONE * 2
#		var top_left_margin = top_left - Vector2.ONE * margin
#		var bottom_right_margin = bottom_right + Vector2.ONE * margin
#		for i in range(top_left_margin.x, bottom_right_margin.x):
#			for j in range(top_left_margin.y, bottom_right_margin.y):
#				if i < top_left.x or j < top_left.y or i >= bottom_right.x or j >= bottom_right.y:
#					visibility_points.append(Vector2(i, j))
#	else:
#		top_left -= Vector2.ONE * (1 + margin)
#		bottom_right += Vector2.ONE * (2 + margin)
#		for i in range(top_left.x, bottom_right.x):
#			for j in range(top_left.y, bottom_right.y):
#				visibility_points.append(Vector2(i, j))
#
#	return visibility_points

func get_rigidbody():
	return rigidbody

func get_player_position():
	return smoothing.position

func get_camera_zoom() -> float:
	return camera.zoom

func screen_to_world_position(screen_position : Vector2) -> Vector2:
	var world_position: Vector2 = self.smoothing.position + screen_position * camera.zoom
	return world_position - camera.zoom * get_viewport_rect().size/2

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
