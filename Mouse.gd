extends Node

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

func mouse_int_to_string(button_index : int):
	for mouse_string in get_parent().mapping_data[self.name]["binds"].keys():
		if get_parent().mapping_data[self.name]["binds"][mouse_string] == button_index:
			return mouse_string
	return null

func mouse_string_to_int(mouse_string : String):
	if mouse_string in get_parent().mapping_data[self.name]["binds"].keys():
		var a = get_parent().mapping_data[self.name]["binds"][mouse_string]
		return a
	return null

func get_mouse_int(event):
	return event.get_button_index()
