extends Node2D

var title = "Teria"

func _process(_delta):
	OS.set_window_title(title + " | FPS: " + str(Engine.get_frames_per_second()))

# Called when the node enters the scene tree for the first time.
func _ready():
	pass
