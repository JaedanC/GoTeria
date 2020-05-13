extends Node

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

func key_int_to_string(scancode : int):
	for key_string in get_parent().mapping_data["Keyboard"]["binds"].keys():
		if get_parent().mapping_data["Keyboard"]["binds"][key_string] == scancode:
			return key_string
	return null

func key_string_to_int(key_string : String):
	if key_string in get_parent().mapping_data["Keyboard"]["binds"].keys():
		return get_parent().mapping_data["Keyboard"]["binds"][key_string]
	return null

func get_key_int(event):
	return event.get_scancode()
