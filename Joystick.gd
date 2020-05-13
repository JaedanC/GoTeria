extends Node

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

func joy_int_to_string(button_index : int):
	for joy_string in get_parent().mapping_data[self.name]["binds"].keys():
		if get_parent().mapping_data[self.name]["binds"][joy_string] == button_index:
			return joy_string
	return null

func joy_string_to_int(joy_string : String):
	if joy_string in get_parent().mapping_data[self.name]["binds"].keys():
		return get_parent().mapping_data[self.name]["binds"][joy_string]
	return null

func get_joy_int(event):
	return event.get_button_index()
