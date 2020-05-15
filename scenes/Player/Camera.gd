extends Camera2D


func _ready():
	pass

func _process(_delta):
	if InputLayering.pop_action("zoom_reset"):
		print("hello")
