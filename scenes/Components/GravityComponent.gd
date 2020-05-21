extends Node2D

export(bool) var enabled = true

func _physics_process(_delta):
	if enabled:
		get_parent().velocity += Vector2(0, 98)
