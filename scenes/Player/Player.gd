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
		camera.zoom += Vector2(0.5, 0.5)
	
	if event.is_action_pressed("zoom_out"):
		camera.zoom -= Vector2(0.5, 0.5)
	
	if event.is_action_pressed("quit"):
		get_tree().quit()
	
#	camera.zoom.x = clamp(camera.zoom.x, 1, 20)
#	camera.zoom.y = clamp(camera.zoom.y, 1, 20)
	camera.zoom.x = clamp(camera.zoom.x, 1, 10)
	camera.zoom.y = clamp(camera.zoom.y, 1, 10)

"""
This method returns an Array containing two Vector2 representing the world
position of the player's visibility on the screen. This value is in pixels
relative to the top-left of the world.

For example. Window is 1920x1080, and the player is at [10000, 10000] in the
world. This function would then return something like:
	[
		Vector2(9040, 9040),
		Vector2(10960, 10960
	]
This is an oversimplification because this function also takes into account the
camera's zoom. The first Vector2 is the top-left visibility world position. The
second Vector2 is the bottom-right visibility world position. 
"""
func get_visibility_world_position_corners() -> Array:
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
#	var viewport_modifier := 0.4
#	var viewport_modifier := 0.2
	var viewport_modifier := 0
	var size: Vector2 = viewport_rectangle.size * Vector2(viewport_modifier, viewport_modifier)
	viewport_rectangle = viewport_rectangle.grow_individual(-size.x, -size.y, -size.x, -size.y)
	
	# Convert the top left and bottom right points of the Rect2 into an integer
	# that we can loop through to get the points for the visible chunks. This
	# takes into account the size of each chunk.
	var top_left: Vector2 = viewport_rectangle.position
	var bottom_right: Vector2 = viewport_rectangle.position + viewport_rectangle.size
	
	
	return [top_left, bottom_right]

"""
This function is similar to get_visibility_world_position_corners() but this
returns the two vectors divided by the block size. This then returns two
Vector2's which represent the corners of block indices in the world that can be
seen by the player.
"""
func get_visibility_world_block_position_corners() -> Array:
	var corners = get_visibility_world_position_corners()
	var world_block_position_corners = [
		(corners[0] / terrain.get_block_pixel_size()).floor(),
		(corners[1] / terrain.get_block_pixel_size()).floor()
	]
	return world_block_position_corners

"""
This function is similar to get_visibility_world_position_corners but instead it
returns the two Vector2's which represent the chunk indices that can be seen by
the player.
"""
func get_visibility_chunk_position_corners() -> Array:
	var world_position_corners = get_visibility_world_position_corners()
	var chunk_position_top_left = (world_position_corners[0] / terrain.get_chunk_pixel_dimensions()).floor()
	var chunk_position_bottom_right = (world_position_corners[1] / terrain.get_chunk_pixel_dimensions()).floor()
	return [
		chunk_position_top_left,
		chunk_position_bottom_right
	]

"""
This function returns a. Array of chunk positions that are contained by the
function get_visibility_chunk_position_corners().
	- The margin parameter extends the radius of the visibility by the 'margin'
	chunks. This can be used to include chunks are slightly off screen.
	- The border only parameter is only relevant if the margin parameter is
	used. If border_only is true, then only the chunk indices that the
	(non-zero) margin added are returned in the Array.

Example Return:
[
	Vector2(0, 0),
	Vector2(0, 1),
	Vector2(1, 0),
	Vector2(1, 1),
	...
]
"""
func get_visibility_chunk_positions(margin=0, border_only=false, border_ignore=0) -> Array:
	var chunk_corners = get_visibility_chunk_position_corners()
	
	# Now include the margin
	var top_left_margin = chunk_corners[0] - Vector2.ONE * margin
	var bottom_right_margin = chunk_corners[1] + Vector2.ONE * margin
	
	var visibility_points := []
	if border_only:
		for i in range(top_left_margin.x, bottom_right_margin.x + 1):
			for j in range(top_left_margin.y, bottom_right_margin.y + 1):
				var ignore_top_left = chunk_corners[0] - Vector2.ONE * border_ignore
				var ignore_bottom_right = chunk_corners[1] + Vector2.ONE * border_ignore
				if (i < ignore_top_left.x or j < ignore_top_left.y or
						i > ignore_bottom_right.x or j > ignore_bottom_right.y):
					visibility_points.append(Vector2(i, j))
	else:
		for i in range(top_left_margin.x, bottom_right_margin.x + 1):
			for j in range(top_left_margin.y, bottom_right_margin.y + 1):
				visibility_points.append(Vector2(i, j))

	return visibility_points

func get_rigidbody():
	return rigidbody

"""
This returns the player's position, which is actually the smoothing sprite's
location if you want the player's position on any frame other than a physics
call.
"""
func get_player_position():
	return smoothing.position

"""
This function returns the zoom factor of the player's camera.
"""
func get_camera_zoom() -> float:
	return camera.zoom

"""
This function converts a screen position to a world position.
	- A screen position is a location inside the window. The location of the
	mouse is supplied to Godot as a screen position.
"""
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
