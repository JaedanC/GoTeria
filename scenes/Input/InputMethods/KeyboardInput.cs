using Godot;
using System;

public class KeyboardInput : IInputMethod
{
    public int GetKeyCode(InputEvent @event)
    {
        InputEventKey inputEventKey = (InputEventKey)@event;
        return (int)inputEventKey.Scancode;
    }
    public InputEvent GetInputEvent(int keyCode)
    {
        InputEventKey inputEventKey = new InputEventKey {Scancode = (uint) keyCode};
        return inputEventKey;
    }

    public string GetInputMethodName()
    {
        return "Keyboard";
    }

    public bool IsInputEventInstance(InputEvent @event)
    {
        try
        {
            InputEventKey newEvent = (InputEventKey)@event;
            return true;
        }
        catch (InvalidCastException)
        {
            //
        }
        return false;
    }
}
