extends Node

func save_action_mappings_config():
	"""
	Saves the action mappings and key bindings to a save file that is human
	readable. TODO: The file name is currently hardcoded and needs to be
	parameterised.
	"""
	var config = ConfigFile.new()
	var action_mappings = get_node("../ActionMapping").get_action_mappings_as_saveable_dict()
	
	for input_type in action_mappings.keys():
		for action in action_mappings[input_type].keys():
			config.set_value(input_type, action, action_mappings[input_type][action])
	
	config.save("user://action_mapping_config.ini")

func load_action_mappings_config():
	"""
	Loads and binds action mappings to keys based on a save file. TODO: The file
	name is currently hardcoded and needs to be parameterised.
	"""
	var config = ConfigFile.new()
	config.load("user://action_mapping_config.ini")
	
	for input_type in get_node("../ActionMapping").mapping_data.keys():
		for action in config.get_section_keys(input_type):
			for bind in config.get_value(input_type, action):
				$"../ActionMapping".add_action_mapping(
					action,
					$"../ActionMapping".key_string_to_int(bind, input_type),
					$"../ActionMapping".mapping_data[input_type]["input_event_type"]
				)
