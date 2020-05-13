extends Node

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

func axis_int_to_string(axis : int):
	for axis_string in get_parent().mapping_data[self.name]["binds"].keys():
		if get_parent().mapping_data[self.name]["binds"][axis_string] == axis:
			return axis_string
	return null

func axis_string_to_int(axis_string : String):
	if axis_string in get_parent().mapping_data[self.name]["binds"].keys():
		return get_parent().mapping_data[self.name]["binds"][axis_string]
	return null

func get_axis_int(event):
	return event.get_axis()
