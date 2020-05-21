extends Node2D

var rigidbody = null
var camera = null
var smoothing = null
var terrain = null

var velocity = Vector2(0, 0)

func _ready():
	self.camera = find_node("Camera")
	self.rigidbody = find_node("Rigidbody")
	self.smoothing = find_node("Smoothing")
	self.terrain = get_tree().get_root().find_node("Terrain", true, false)

func _process(_delta):
	assert(self.position == Vector2(0, 0))
	if InputLayering.pop_action("zoom_reset"):
		camera.zoom = Vector2(1, 1)
	
	update()

func _physics_process(delta):
	self.rigidbody.position += velocity
	
	if InputLayering.pop_action("move_left"):
		velocity.x -= 1
	
	if InputLayering.pop_action("move_right"):
		velocity.x += 1
	
	if InputLayering.pop_action("move_up"):
		velocity.y -= 1
	
	if InputLayering.pop_action("move_down"):
		velocity.y += 1
	
	if InputLayering.pop_action("jump"):
		velocity.y = -15
	
	if InputLayering.pop_action("brake"):
		velocity = Vector2(0, 0)

func _input(event):
	if event.is_action_pressed("zoom_in"):
		camera.zoom += Vector2(0.25, 0.25)
	
	if event.is_action_pressed("zoom_out"):
		camera.zoom -= Vector2(0.25, 0.25)
	
	if event.is_action_pressed("click"):
		var world_position = screen_to_world_position(get_viewport().get_mouse_position())
		
		var chunk_position = terrain.get_chunk_position_from_world_position(world_position)
		var block_position = terrain.get_block_position_from_world_position(world_position)
		
		var block = terrain.get_block_from_world_position(world_position)
		
		print("Chunk Position: " + str(chunk_position))
		print("Block Position: " + str(block_position))
		print(block)
		
	
	camera.zoom.x = clamp(camera.zoom.x, 0.5, 3)
	camera.zoom.y = clamp(camera.zoom.y, 0.5, 3)

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

func _draw():
#	for point in get_visibility_points():
#		draw_circle(
#			point * get_tree().get_root().find_node("Terrain", true, false).get_chunk_pixel_dimensions(),
#			15,
#			Color(0, 1, 0, 1)
#		)
	
	draw_circle(
		screen_to_world_position(get_viewport().get_mouse_position()),
		5,
		Color(1, 1, 0, 1)
	)

	pass
