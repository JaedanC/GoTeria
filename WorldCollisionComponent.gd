extends Node2D

var terrain = null
var parent = null

func _ready():
	terrain = get_tree().get_root().find_node("Terrain", true, false)
	parent = get_parent()

func _physics_process(delta):
	pass
