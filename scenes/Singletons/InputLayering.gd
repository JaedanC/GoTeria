extends Node

# See pop_action
var input_consumed_actions = {}

func _process(_delta):
	# Each frame we clear the readability status of every action
	input_consumed_actions.clear()

func pop_action(action : String) -> bool:
	"""
	Actions each frame are marked as readable until this method is called upon
	the action. Checks whether an action was triggered and it was allowed to be
	read while also not makring the action as unreadable for the rest of the frame.
	This allows a system where nodes can greedily steal the action and prevent
	other nodes from reading it.
	"""
	if poll_action(action):
		input_consumed_actions[action] = false
		return true
	return false
	
func poll_action(action : String) -> bool:
	"""
	Checks whether an action was triggered and it was allowed to be read without
	changing it's readability status.
	"""
	return Input.is_action_pressed(action) and input_consumed_actions.get(action, true)
