using System.Linq;
using System.Collections.Generic;
using Godot;

/* This class is responsible for handling controller inputs that assigned to two actions.
This is called a Dual axis action. A dual axis action lets a binding be created between two
actions across range of analog values. The Input.GetJoyAxis() method returns a number [-1, 1].
This class computes on request the action that could be assigned to the left or right of that
range.

For example:
- DualAxisAction("left", "right", device=0, deadzone=false):
- If Input.GetJoyAxis() returns -0.75:
    "left" = 0.75
    "right" = 0
*/
public class AnalogMapping
{
    private const float DeadZone = 0.2f;
    private readonly Dictionary<string, DualAxisAction> dualAxisActions;

    public AnalogMapping()
    {
        dualAxisActions = new Dictionary<string, DualAxisAction>();
    }

    /* Add a new dual axis action. It is stored in a dictionary with the two actions as keys. This means that actions
    can be overridden with different devices. It also means that actions cannot be assigned more than twice to a DAA
    otherwise an error will be thrown. */
    public void AddDualAxisAction(string firstAction, string secondAction, int device, int joyAxis, bool useDeadZone)
    {
        DualAxisAction dualAxisAction = new DualAxisAction(firstAction, secondAction, device, joyAxis, useDeadZone);
        dualAxisActions.Add(firstAction, dualAxisAction);
        dualAxisActions.Add(secondAction, dualAxisAction);
    }

    /* Check if the DAA exists. */
    public bool HasDualAxisActionMapping(string action)
    {
        return dualAxisActions.ContainsKey(action);
    }

    /* For example, if a dual axis action was: ["move_left", "move_right"], then when we ask
    for "move_left" we will get a analogue answer if GetJoyAxis() is negative. */
    public float GetDualActionStrength(string action)
    {
        if (!HasDualAxisActionMapping(action))
        {
            return 0;
        }

        DualAxisAction dualAxisAction = dualAxisActions[action];

        // Read the axis value [-1, 1]
        // Is made to be zero if the action doesn't match the sign of the axis value
        float axisValue = dualAxisAction.GetAxisValueFromAction(action);

        // Remap to include the deadzone if the DAA was created to do so.
        if (dualAxisAction.UseDeadZone())
        {
            axisValue = ApplyDeadZone(DeadZone, axisValue);
        }
        return axisValue;
    }

    /* Remaps from [deadZone, 1] to [0, 1]. */
    private float ApplyDeadZone(float deadZone, float value)
    {
        float newValue = (value - 0.2f) / (1 - deadZone);
        return Mathf.Clamp(newValue, 0, 1);
    }

    public List<DualAxisAction> GetDualAxisMappings()
    {
        HashSet<DualAxisAction> daaSet = new HashSet<DualAxisAction>();
        foreach (DualAxisAction daa in dualAxisActions.Values)
        {
            daaSet.Add(daa);
        }
        return daaSet.ToList();
    }
}
