extends Node

onready var action_mapping = $"../ActionMapping";

func save_action_mappings_config():
	"""
	Saves the action mappings and key bindings to a save file that is human
	readable. TODO: The file name is currently hardcoded and needs to be
	parameterised.
	"""
	var config = ConfigFile.new()
	var action_mappings = action_mapping.get_bindings_as_saveable_dict()
	
	for input_type_string in action_mappings.keys():
		for action in action_mappings[input_type_string].keys():
			config.set_value(input_type_string, action, action_mappings[input_type_string][action])
	
	config.save("user://action_mapping_config.ini")

func load_action_mappings_config():
	"""
	Loads and binds action mappings to keys based on a save file. TODO: The file
	name is currently hardcoded and needs to be parameterised.
	"""
	var config = ConfigFile.new()
	var err = config.load("user://action_mapping_config.ini")
	if err != OK:
		return
	
	for input_type in action_mapping.game_action_bindings.keys():
		var input_type_string = action_mapping.input_type_strings[input_type]
		if not config.has_section(input_type_string):
			continue
		
		for action in config.get_section_keys(input_type_string):
			for bind in config.get_value(input_type_string, action):
				action_mapping.add_action_mapping(action, bind)
