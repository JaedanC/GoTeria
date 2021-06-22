using Godot;
using System;
using System.Collections.Generic;

public class InputLayering : Node
{
    private HashSet<String> consumedActions;
    private float DEAD_ZONE = 0.2f;

    public override void _Ready()
    {
        consumedActions = new HashSet<String>();
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
            consumedActions.Add(action);
            return true;
        }
        return false;
    }

    /* Checks whether an action was triggered and it was allowed to be read without
    changing it's readability status. */
    public bool PollAction(String action)
    {
        return Input.IsActionPressed(action) && !consumedActions.Contains(action);
    }

    /* Same as PopAction but will only return true on the first frame this was
    called. */
    public bool PopActionPressed(String action)
    {
        if (PollActionPressed(action))
        {
            consumedActions.Add(action);
            return true;
        }
        return false;
    }

    /* Same as PollAction but will only return true on the first frame this was
    called. */
    public bool PollActionPressed(String action)
    {
        InputEventAction blah = new InputEventAction();
        
        return Input.IsActionJustPressed(action) && !consumedActions.Contains(action);
    }

    public float PollActionStrength(String action)
    {
        if (!consumedActions.Contains(action))
        {
            return Input.GetActionStrength(action);
        }
        return 0;
    }

    public float PopActionStrength(String action)
    {
        float actionStrength = PollActionStrength(action);
        consumedActions.Add(action);
        return actionStrength;
    }
}
