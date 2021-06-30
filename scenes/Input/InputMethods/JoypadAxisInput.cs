using Godot;
using System;

public class JoypadAxisInput : IInputMethod
{
    public int GetKeyCode(InputEvent @event)
    {
        InputEventJoypadMotion inputEventJoypadMotion = (InputEventJoypadMotion)@event;
        return inputEventJoypadMotion.Axis;
    }
    public InputEvent GetInputEvent(int keyCode)
    {
        InputEventJoypadMotion inputEventJoypadMotion = new InputEventJoypadMotion {Axis = keyCode};
        return inputEventJoypadMotion;
    }

    public string GetInputMethodName()
    {
        return "JoypadAxis";
    }

    public bool IsInputEventInstance(InputEvent @event)
    {
        try
        {
            InputEventJoypadMotion newEvent = (InputEventJoypadMotion)@event;
            return true;
        }
        catch (InvalidCastException)
        {
            //
        }
        return false;
    }
}
