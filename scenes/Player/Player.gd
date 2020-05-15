extends Node2D


func _process(delta):
	if InputLayering.pop_action("zoom_reset"):
		$Camera.zoom = Vector2(1, 1)
	
	if InputLayering.pop_action("move_left"):
		self.position.x -= 1000 * delta
	
	if InputLayering.pop_action("move_right"):
		self.position.x += 1000 * delta
	
	if InputLayering.pop_action("move_up"):
		self.position.y -= 1000 * delta
	
	if InputLayering.pop_action("move_down"):
		self.position.y += 1000 * delta

func _input(event):
	if event.is_action_pressed("zoom_in"):
		$Camera.zoom += Vector2(0.51, 0.51)
	
	if event.is_action_pressed("zoom_out"):
		$Camera.zoom -= Vector2(0.51, 0.51)
