extends Node

var mapping_data = {
	"Keyboard": {
		"binds": {},
		"get_int" : "get_key_int",
		"input_event_type": InputEventKey 
	},
	"Joystick": {
		"binds": {},
		"get_int" : "get_joy_int",
		"input_event_type": InputEventJoypadButton
	},
	"Mouse": {
		"binds": {},
		"get_int" : "get_mouse_int",
		"input_event_type": InputEventMouseButton
	},
	"Axis": {
		"binds": {},
		"get_int" : "get_axis_int",
		"input_event_type": InputEventJoypadMotion
	}
}

# Called when the node enters the scene tree for the first time.
func _ready():
	setup_aliases()
	
	add_action_mapping("jump", KEY_SPACE, InputEventKey)
	add_action_mapping("save_config", KEY_I, InputEventKey)
	add_action_mapping("load_config", KEY_O, InputEventKey)
	add_action_mapping("show_mappings", KEY_P, InputEventKey)
	
	add_action_mapping("move_left", KEY_A, InputEventKey)
	add_action_mapping("move_right", KEY_D, InputEventKey)
	add_action_mapping("move_up", KEY_W, InputEventKey)
	add_action_mapping("move_down", KEY_S, InputEventKey)
	add_action_mapping("zoom_reset", key_string_to_int("backspace", "Keyboard"), InputEventKey)
	add_action_mapping("zoom_in", key_string_to_int("mwheeldown", "Mouse"), InputEventMouseButton)
	add_action_mapping("zoom_out", key_string_to_int("mwheelup", "Mouse"), InputEventMouseButton)
	add_action_mapping("save_world", key_string_to_int("m", "Keyboard"), InputEventKey)
	add_action_mapping("click", key_string_to_int("mouse1", "Mouse"), InputEventMouseButton)
	add_action_mapping("dig", key_string_to_int("mouse2", "Mouse"), InputEventMouseButton)
	add_action_mapping("brake", key_string_to_int("x", "Keyboard"), InputEventKey)

func setup_aliases():
	# TODO: No Axis Mappings yet. Though is this required?
	var key_aliases = mapping_data["Keyboard"]["binds"]
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
	
	var joy_aliases = mapping_data["Joystick"]["binds"]
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
	
	var mouse_aliases = mapping_data["Mouse"]["binds"]
	mouse_aliases["mouse1"] = BUTTON_LEFT
	mouse_aliases["mouse2"] = BUTTON_RIGHT
	mouse_aliases["mouse3"] = BUTTON_MIDDLE
	mouse_aliases["mouse4"] = BUTTON_XBUTTON1
	mouse_aliases["mouse5"] = BUTTON_XBUTTON2
	mouse_aliases["mwheelup"] = BUTTON_WHEEL_UP
	mouse_aliases["mwheeldown"] = BUTTON_WHEEL_DOWN
	
	var axis_aliases = mapping_data["Axis"]["binds"]
	axis_aliases["axis0"] = JOY_AXIS_0; axis_aliases["axis1"] = JOY_AXIS_1
	axis_aliases["axis2"] = JOY_AXIS_2; axis_aliases["axis3"] = JOY_AXIS_3
	axis_aliases["axis4"] = JOY_AXIS_4; axis_aliases["axis5"] = JOY_AXIS_5
	axis_aliases["axis6"] = JOY_AXIS_6; axis_aliases["axis7"] = JOY_AXIS_7
	axis_aliases["axis8"] = JOY_AXIS_8; axis_aliases["axis9"] = JOY_AXIS_9

func _input(event : InputEvent):
	
#	var result = get_event_key(event)
#	if result:
#		print(result)
	if event.is_action_pressed("jump"):
		print("jumping")
	
	if event.is_action_pressed("show_mappings"):
		show_mappings()
	
	if event.is_action_pressed("save_config"):
		print("Saving Config")
		get_node("../ConfigHandler").save_action_mappings_config()
		print("Saved Config")
	
	if event.is_action_pressed("load_config"):
		get_node("../ConfigHandler").load_action_mappings_config()
		print("Loaded Config")

func add_action_mapping(action : String, keycode : int, type):
	"""
	Binds a key to an action. You also need to pass it the type of event this
	keycode is so that it can be stored correctly.
	Example usage:
		add_action_mapping('move_down', KEY_S, InputEventKey)
		add_action_mapping('zoom_reset', key_string_to_int('backspace', 'Keyboard'), InputEventKey)
		add_action_mapping('zoom_in', key_string_to_int('mwheeldown', 'Mouse'), InputEventMouseButton)
	"""
	var event = type.new()
	set_event_key(event, keycode)
	
	if !(action in InputMap.get_actions()):
		InputMap.add_action(action)
	InputMap.action_add_event(action, event)

func show_mappings():
	"""
	Print all the ActionMappings to the console.
	"""
	for action in InputMap.get_actions():
		print("Action: " + str(action) + ", Mappings: " + str(InputMap.get_action_list(action)))

func get_action_mappings_as_saveable_dict():
	"""
	This function converts the mapping data (which is in the format above) to a disk
	storable version that can be saved to a file. Pseudocode:
	
	dict = {}
	foreach action:   # jump
		foreach input_event tied to the action:   # [InputEventKey: 560]
			foreach style_of_input:   # Keyboard
				if the input_event is the style_of_input:
					If the dict[action] doesn't exist yet make it default []
					
					Add the string rep of the input_event.button_int by calling the
					the corresponding conversion callback functions tied
					to each type.
	return dict
	"""
	
	var action_mappings_dict = {}
	
	for input_type in mapping_data.keys(): # Keyboard
		if !(input_type in action_mappings_dict):
			action_mappings_dict[input_type] = {}
	
	for action in InputMap.get_actions():
		for input_event in InputMap.get_action_list(action):
			for input_type in mapping_data.keys():
				if input_event is mapping_data[input_type]["input_event_type"]:
					if !(action in action_mappings_dict[input_type]):
						action_mappings_dict[input_type][action] = []
					
					var key_int = get_node(input_type).call(
						mapping_data[input_type]["get_int"],
						input_event
					)
					
					action_mappings_dict[input_type][action].append(
						key_int_to_string(key_int, input_type)
					)
	return action_mappings_dict

func key_int_to_string(key_int : int, input_type : String):
	"""
	Converts KeyCode Enum values to readable string that can be stored in a file.
	The strings are defined in setup_aliases().
	"""
	for key_string in mapping_data[input_type]["binds"].keys():
		if mapping_data[input_type]["binds"][key_string] == key_int:
			return key_string
	return null

func key_string_to_int(key_string : String, input_type : String):
	"""
	Converts readable string representations of keys into the godot KeyCode Enum
	values. The strings are defined in setup_aliases().
	"""
	if key_string in mapping_data[input_type]["binds"].keys():
		return mapping_data[input_type]["binds"][key_string]
	return null

func set_event_key(event, keycode : int):
	if "scancode" in event:
		event.set_scancode(keycode)
	elif "button_index" in event:
		event.set_button_index(keycode)
	elif "axis" in event:
		event.set_axis(keycode)
	else:
		print("Unknown event type")

func get_event_key(event):
	if "scancode" in event:
		return event.get_scancode()
	elif "button_index" in event:
		return event.get_button_index()
	elif "axis" in event:
		return event.get_axis()
		
	return null
