using Godot;
using Godot.Collections;
using System;

public class InputLayering : Node
{
    private Dictionary<String, object> consumedActions;

    public override void _Ready()
    {
        consumedActions = new Dictionary<String, object>();
    }

    public override void _Process(float delta)
    {
        consumedActions.Clear();
    }

    /* Actions are readable until this method pops it. This method checks whether
    the action was triggered and it has not already been popped. This allows for a
    system where nodes can greedily steal the action and prevent other nodes from
    reading it. */
    public bool PopAction(String action)
    {
        if (PollAction(action))
        {
            consumedActions[action] = null;
            return true;
        }
        return false;
    }

    /* Checks whether an action was triggered and it was allowed to be read without
	changing it's readability status. */
    public bool PollAction(String action)
    {
        return Input.IsActionPressed(action) && !consumedActions.ContainsKey(action);
    }

    /* Same as PopAction but will only return true on the first frame this was
    called. */
    public bool PopActionPressed(String action)
    {
        if (PollActionPressed(action))
        {
            consumedActions[action] = null;
            return true;
        }
        return false;
    }

    /* Same as PollAction but will only return true on the first frame this was
    called. */
    public bool PollActionPressed(String action)
    {
        return Input.IsActionJustPressed(action) && !consumedActions.ContainsKey(action);
    }
}
