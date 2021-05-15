extends Node

onready var config_handler = $"../ConfigHandler"

var game_action_bindings = {
	InputEventKey: {
		"binds": {},
	},
	InputEventJoypadButton: {
		"binds": {},
	},
	InputEventMouseButton: {
		"binds": {},
	},
	InputEventJoypadMotion: {
		"binds": {},
	}
}

var input_type_strings = {}

func _ready():
	input_type_strings[InputEventKey] = "Keyboard"
	input_type_strings[InputEventJoypadButton] = "Joy"
	input_type_strings[InputEventMouseButton] = "Mouse"
	input_type_strings[InputEventJoypadMotion] = "Axis"
	
	setup_aliases()
	
	add_action_mapping("jump", " ")
	add_action_mapping("save_config", "i")
	add_action_mapping("load_config", "o")
	add_action_mapping("show_mappings", "p")

	add_action_mapping("move_left", "a")
	add_action_mapping("move_right", "d")
	add_action_mapping("move_up", "w")
	add_action_mapping("move_down", "s")
	add_action_mapping("zoom_reset", "backspace")
	add_action_mapping("zoom_in", "mwheeldown")
	add_action_mapping("zoom_out", "mwheelup")
	add_action_mapping("save_world", "m")
	add_action_mapping("click", "mouse1")
	add_action_mapping("dig", "mouse2")
	add_action_mapping("brake", "x")
	add_action_mapping("quit", "esc")
	add_action_mapping("light", "l")
	add_action_mapping("debug", "b")
	add_action_mapping("toggle_fullscreen", "F11")
	add_action_mapping("place_light", "mouse4")
	add_action_mapping("remove_light", "mouse5")

func setup_aliases():
	var key_aliases = game_action_bindings[InputEventKey]["binds"]
	key_aliases["a"] = KEY_A; key_aliases["b"] = KEY_B; key_aliases["c"] = KEY_C
	key_aliases["d"] = KEY_D; key_aliases["e"] = KEY_E; key_aliases["f"] = KEY_F
	key_aliases["g"] = KEY_G; key_aliases["h"] = KEY_H; key_aliases["i"] = KEY_I
	key_aliases["j"] = KEY_J; key_aliases["k"] = KEY_K; key_aliases["l"] = KEY_L
	key_aliases["m"] = KEY_M; key_aliases["n"] = KEY_N; key_aliases["o"] = KEY_O
	key_aliases["p"] = KEY_P; key_aliases["q"] = KEY_Q; key_aliases["r"] = KEY_R
	key_aliases["s"] = KEY_S; key_aliases["t"] = KEY_T; key_aliases["u"] = KEY_U
	key_aliases["v"] = KEY_V; key_aliases["w"] = KEY_W; key_aliases["x"] = KEY_X
	key_aliases["y"] = KEY_Y; key_aliases["z"] = KEY_Z
	
	key_aliases["0"] = KEY_0; key_aliases["1"] = KEY_1; key_aliases["2"] = KEY_2
	key_aliases["3"] = KEY_3; key_aliases["4"] = KEY_4; key_aliases["5"] = KEY_5
	key_aliases["6"] = KEY_6; key_aliases["7"] = KEY_7; key_aliases["8"] = KEY_8
	key_aliases["9"] = KEY_9
	
	key_aliases["F1"] = KEY_F1; key_aliases["F2"] = KEY_F2; key_aliases["F3"] = KEY_F3
	key_aliases["F4"] = KEY_F4; key_aliases["F5"] = KEY_F5; key_aliases["F6"] = KEY_F6
	key_aliases["F7"] = KEY_F7; key_aliases["F8"] = KEY_F8; key_aliases["F9"] = KEY_F9
	key_aliases["F10"] = KEY_F10; key_aliases["F11"] = KEY_F11; key_aliases["F12"] = KEY_F12
	
	key_aliases["esc"] = KEY_ESCAPE
	key_aliases["`"] = KEY_QUOTELEFT
	key_aliases["capslock"] = KEY_CAPSLOCK
	key_aliases["tab"] = KEY_TAB
	key_aliases["shift"] = KEY_SHIFT
	key_aliases["ctrl"] = KEY_CONTROL
	key_aliases["alt"] = KEY_ALT
	key_aliases["enter"] = KEY_ENTER
	key_aliases[" "] = KEY_SPACE
	key_aliases[","] = KEY_COMMA
	key_aliases["."] = KEY_PERIOD
	key_aliases["/"] = KEY_SLASH
	key_aliases["\\"] = KEY_BACKSLASH
	key_aliases[";"] = KEY_SEMICOLON
	key_aliases["'"] = KEY_APOSTROPHE
	key_aliases["["] = KEY_BRACELEFT
	key_aliases["]"] = KEY_BRACERIGHT
	key_aliases["-"] = KEY_MINUS
	key_aliases["="] = KEY_EQUAL
	key_aliases["backspace"] = KEY_BACKSPACE
	
	key_aliases["del"] = KEY_DELETE
	key_aliases["ins"] = KEY_INSERT
	key_aliases["home"] = KEY_HOME
	key_aliases["end"] = KEY_END
	key_aliases["pageup"] = KEY_PAGEUP
	key_aliases["pagedown"] = KEY_PAGEDOWN
	
	key_aliases["left"] = KEY_LEFT
	key_aliases["right"] = KEY_RIGHT
	key_aliases["up"] = KEY_UP
	key_aliases["down"] = KEY_DOWN
	
	key_aliases["kp0"] = KEY_KP_0; key_aliases["kp1"] = KEY_KP_1
	key_aliases["kp2"] = KEY_KP_2; key_aliases["kp3"] = KEY_KP_3
	key_aliases["kp4"] = KEY_KP_4; key_aliases["kp5"] = KEY_KP_5
	key_aliases["kp6"] = KEY_KP_6; key_aliases["kp7"] = KEY_KP_7
	key_aliases["kp8"] = KEY_KP_8; key_aliases["kp9"] = KEY_KP_9
	key_aliases["kp."] = KEY_KP_PERIOD
	key_aliases["kpenter"] = KEY_KP_ENTER
	key_aliases["kp+"] = KEY_KP_ADD
	key_aliases["kp-"] = KEY_KP_SUBTRACT
	key_aliases["kp*"] = KEY_KP_MULTIPLY
	key_aliases["kp/"] = KEY_KP_DIVIDE
	
	var joy_aliases = game_action_bindings[InputEventJoypadButton]["binds"]
	joy_aliases["a"] = JOY_XBOX_A
	joy_aliases["b"] = JOY_XBOX_B
	joy_aliases["x"] = JOY_XBOX_X
	joy_aliases["y"] = JOY_XBOX_Y
	joy_aliases["dpad_left"] = JOY_DPAD_LEFT
	joy_aliases["dpad_right"] = JOY_DPAD_RIGHT
	joy_aliases["dpad_up"] = JOY_DPAD_UP
	joy_aliases["dpad_down"] = JOY_DPAD_DOWN
	joy_aliases["l1"] = JOY_L 
	joy_aliases["l2"] = JOY_L2 
	joy_aliases["l3"] = JOY_L3 
	joy_aliases["r1"] = JOY_R
	joy_aliases["r2"] = JOY_R2
	joy_aliases["r3"] = JOY_R3
	joy_aliases["start"] = JOY_START
	joy_aliases["select"] = JOY_SELECT
	
	var mouse_aliases = game_action_bindings[InputEventMouseButton]["binds"]
	mouse_aliases["mouse1"] = BUTTON_LEFT
	mouse_aliases["mouse2"] = BUTTON_RIGHT
	mouse_aliases["mouse3"] = BUTTON_MIDDLE
	mouse_aliases["mouse4"] = BUTTON_XBUTTON1
	mouse_aliases["mouse5"] = BUTTON_XBUTTON2
	mouse_aliases["mwheelup"] = BUTTON_WHEEL_UP
	mouse_aliases["mwheeldown"] = BUTTON_WHEEL_DOWN
	
	var axis_aliases = game_action_bindings[InputEventJoypadMotion]["binds"]
	axis_aliases["axis0"] = JOY_AXIS_0; axis_aliases["axis1"] = JOY_AXIS_1
	axis_aliases["axis2"] = JOY_AXIS_2; axis_aliases["axis3"] = JOY_AXIS_3
	axis_aliases["axis4"] = JOY_AXIS_4; axis_aliases["axis5"] = JOY_AXIS_5
	axis_aliases["axis6"] = JOY_AXIS_6; axis_aliases["axis7"] = JOY_AXIS_7
	axis_aliases["axis8"] = JOY_AXIS_8; axis_aliases["axis9"] = JOY_AXIS_9

func _input(event: InputEvent):
	if event.is_action_pressed("jump"):
		pass
	
	if event.is_action_pressed("show_mappings"):
		show_mappings()
	
	if event.is_action_pressed("save_config"):
		print("Saving Config")
		config_handler.save_action_mappings_config()
		print("Saved Config")
	
	if event.is_action_pressed("load_config"):
		print("Loading Config")
		config_handler.load_action_mappings_config()
		print("Loaded Config")

func add_action_mapping(game_action: String, key_string: String):
	"""
	Binds a key to an action. You also need to pass it the type of event this
	keycode is so that it can be stored correctly.
	Example usage:
		add_action_mapping("move_down", "s")
		add_action_mapping('zoom_reset', "backspace")
		add_action_mapping('zoom_in', "mwheeldown")
	"""
	var result = get_input_type_class_and_keycode_from_key_string(key_string)
	if (result == null):
		return
	
	var input_type_class = result[0]
	var input_event = input_type_class.new()
	var keycode = result[1]
	set_input_event_key(input_event, keycode)
	
	if not InputMap.has_action(game_action):
		InputMap.add_action(game_action)
	InputMap.action_add_event(game_action, input_event)

func get_input_type_class_and_keycode_from_key_string(key_string: String):
	for input_type_class in game_action_bindings.keys():
		var binds_dict = game_action_bindings[input_type_class]["binds"]
		for bind in binds_dict.keys():
			if (bind == key_string):
				return [input_type_class, binds_dict[bind]]
	return null
	

func show_mappings():
	"""
	Print all the ActionMappings to the console.
	"""
	for action in InputMap.get_actions():
		print("Action: " + str(action) + ", Mappings: " + str(InputMap.get_action_list(action)))

func get_bindings_as_saveable_dict():
	"""
	This function converts the mapping data (which is in the format above) to a disk
	storable version that can be saved to a file. Pseudocode:
	
	dict = {}
	foreach action:   # jump
		foreach input_event tied to the action:   # [InputEventKey: 560]
			foreach style_of_input:   # KeyboardInputClass
				if the input_event is not the style_of_input:
					continue
				
				Get the input_events "integer" represenation so that we can
				finding the binding associated with it.
				
				If the Dictionary or Array doesn't exist yet make it.
					
				Add the binding to the dictionary, with the key being a string
				representation of the input_event class.
	return dict
	"""
	
	var bindings = {}
	for game_action in InputMap.get_actions():
		for input_for_game_action in InputMap.get_action_list(game_action):
			for input_type_class in game_action_bindings.keys():
				if not input_for_game_action is input_type_class:
					continue
				
				# 'is' should work but doesn't for some reason :(
				var key_int;
				if str(input_type_class) == str(InputEventKey):
					key_int = input_for_game_action.get_scancode()
				elif str(input_type_class) == str(InputEventJoypadButton):
					key_int = input_for_game_action.get_button_index()
				elif str(input_type_class) == str(InputEventMouseButton):
					key_int = input_for_game_action.get_button_index()
				elif str(input_type_class) == str(InputEventJoypadMotion):
					key_int = input_for_game_action.get_axis()
				else:
					print(input_type_class)
					assert(false)
				
				# Give the action mappings dict some default values
				var input_type_name = input_type_strings[input_type_class]
				if not bindings.has(input_type_name):
					bindings[input_type_name] = {}
				if not bindings[input_type_name].has(game_action):
					bindings[input_type_name][game_action] = []
				
				bindings[input_type_name][game_action].append(
					key_int_to_string(key_int, input_type_class)
				)
	return bindings

func key_int_to_string(key_int: int, input_type_class):
	"""
	Converts KeyCode Enum values to readable string that can be stored in a file.
	The strings are defined in setup_aliases().
	"""
	for key_string in game_action_bindings[input_type_class]["binds"].keys():
		if game_action_bindings[input_type_class]["binds"][key_string] == key_int:
			return key_string
	return null

func key_string_to_int(key_string: String, input_type_class):
	"""
	Converts readable string representations of keys into the godot KeyCode Enum
	values. The strings are defined in setup_aliases().
	"""
	if key_string in game_action_bindings[input_type_class]["binds"].keys():
		return game_action_bindings[input_type_class]["binds"][key_string]
	return null

func set_input_event_key(event, keycode: int):
	if "scancode" in event:
		event.set_scancode(keycode)
	elif "button_index" in event:
		event.set_button_index(keycode)
	elif "axis" in event:
		event.set_axis(keycode)
	else:
		print("set_event_key: Unknown event type")
		assert(false)
