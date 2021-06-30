using Godot;

/* Dual Axis Action's bundle two actions together to be controlled by a single analog
input. This input is typically called an axis input, but it is just a value in the
range [-1, 1]. See the AnalogMapping class for more information. */
public class DualAxisAction
{
    // Public because this is more of a data class and all fields are accessed
    private readonly string firstAction;
    private readonly string secondAction;
    private readonly int joyAxis;
    private readonly int device;
    private readonly bool useDeadZone;

    public DualAxisAction(string firstAction, string secondAction, int device, int joyAxis, bool useDeadZone)
    {
        this.firstAction = firstAction;
        this.secondAction = secondAction;
        this.device = device;
        this.joyAxis = joyAxis;
        this.useDeadZone = useDeadZone;
    }

    /* Returns the axisValue of the Joy Axis if the action is on the same side
    as the sign. The result is the absolute value. 0 if the action doesn't match.
    This does not apply the deadzone. */
    public float GetAxisValueFromAction(string action)
    {
        float axisValue = Input.GetJoyAxis(device, joyAxis);
        if (action.Equals(firstAction) && axisValue < 0 ||
            action.Equals(secondAction) && axisValue > 0)
        {
            return Mathf.Abs(axisValue);
        }
        return 0;
    }

    public string GetFirstAction()
    {
        return firstAction;
    }

    public string GetSecondAction()
    {
        return secondAction;
    }

    public int GetJoyAxis()
    {
        return joyAxis;
    }

    public int GetDevice()
    {
        return device;
    }

    public bool UseDeadZone()
    {
        return useDeadZone;
    }
}
