using Godot;
using System;
using System.Collections.Generic;

public class InputLayering : Node
{
    private HashSet<String> consumedActions;
    private AnalogMapping analogMapping;

    public override void _Ready()
    {
        consumedActions = new HashSet<String>();
    }

    public void Initialise(AnalogMapping analogMapping)
    {
        this.analogMapping = analogMapping;
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

    /* Accesses the Dual Axis Actions if one exists. Returns a value in the range [0, 1]
    depending on the strength of the action. */
    public float PollActionStrength(String action)
    {
        if (consumedActions.Contains(action))
        {
            return 0;
        }
        // If another button is pressed then return the maximum action (for use
        // alongside digital inputs).
        return Mathf.Max(
            Input.GetActionStrength(action),
            analogMapping.GetDualActionStrength(action)
        );
    }

    /* Same as PollActionStrength() but pops the action so nothing else can read it
    when done. */
    public float PopActionStrength(String action)
    {
        float actionStrength = PollActionStrength(action);
        consumedActions.Add(action);
        return actionStrength;
    }
}
