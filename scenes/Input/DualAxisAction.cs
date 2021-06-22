using System;
using Godot;

/* Dual Axis Action's bundle two actions together to be controlleed by a single analog
input. This input is typicall called an axis input, but it is just a value in the
range [-1, 1]. See the AnalogMapping class for more information. */
public class DualAxisAction
{
    // Public because this is more of a data class and all fields are accessed
    private String firstAction;
    private String secondAction;
    private int joyAxis;
    private int device;
    public bool UseDeadZone;

    public DualAxisAction(String firstAction, String secondAction, int device, int joyAxis, bool useDeadZone)
    {
        this.firstAction = firstAction;
        this.secondAction = secondAction;
        this.device = device;
        this.joyAxis = joyAxis;
        this.UseDeadZone = useDeadZone;
    }

    /* Returns the axisValue of the Joy Axis if the action is on the same side
    as the sign. The result is the absolute value. 0 if the action doesn't match.
    This does not apply the deadzone. */
    public float GetAxisValueFromAction(String action)
    {
        float axisValue = Input.GetJoyAxis(device, joyAxis);
        if (action.Equals(firstAction) && axisValue < 0 ||
            action.Equals(secondAction) && axisValue > 0)
        {
            return Mathf.Abs(axisValue);
        }
        return 0;
    }
}
