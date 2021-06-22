using System;
using System.Collections.Generic;
using Godot;

public class ControllerMapping
{
    private class DualAxisAction
    {
        public String FirstAction;
        public String SecondAction;
        public int JoyAxis;
        public int Device;
        public DualAxisAction(String firstAction, String secondAction, int device, int joyAxis)
        {
            this.FirstAction = firstAction;
            this.SecondAction = secondAction;
            this.Device = device;
            this.JoyAxis = joyAxis;
        }
    }

    private Dictionary<String, DualAxisAction> dualAxisActions;

    public ControllerMapping()
    {
        dualAxisActions = new Dictionary<String, DualAxisAction>();
    }

    public void AddDualAxisAction(String firstAction, String secondAction, int device, int joyAxis)
    {
        DualAxisAction dualAxisAction = new DualAxisAction(firstAction, secondAction, device, joyAxis);
        dualAxisActions[firstAction] = dualAxisAction;
        dualAxisActions[secondAction] = dualAxisAction;
    }

    public bool HasDualAxisMapping(String action)
    {
        return dualAxisActions.ContainsKey(action);
    }

    /* For example, if a dual axis action was: ["move_left", "move_right"], then when we ask
    for "move_left" we will get a analogue answer if GetJoyAxis() is negative. */
    public float GetDualAction(String action)
    {
        if (!HasDualAxisMapping(action))
        {
            return 0;
        }

        DualAxisAction dualAxisAction = dualAxisActions[action];
        float axisValue = Input.GetJoyAxis(dualAxisAction.Device, dualAxisAction.JoyAxis);
        if (action.Equals(dualAxisAction.FirstAction) && axisValue < 0)
        {
            return Math.Abs(axisValue);
        }
        else if (action.Equals(dualAxisAction.SecondAction) && axisValue > 0)
        {
            return axisValue;
        }
        return 0;
    }
}