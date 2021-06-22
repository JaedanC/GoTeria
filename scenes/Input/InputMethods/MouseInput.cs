using Godot;
using System;

public class MouseInput : IInputMethod
{
    public int GetKeyCode(InputEvent @event)
    {
        InputEventMouseButton inputEventMouseButton = (InputEventMouseButton)@event;
        return inputEventMouseButton.ButtonIndex;
    }
    public InputEvent GetInputEvent(int keyCode)
    {
        InputEventMouseButton inputEventMouseButton = new InputEventMouseButton();
        inputEventMouseButton.ButtonIndex = keyCode;
        return inputEventMouseButton;
    }

    public String GetInputMethodName()
    {
        return "Mouse";
    }

    public bool IsInputEventInstance(InputEvent @event)
    {
        try
        {
            InputEventMouse newEvent = (InputEventMouse)@event;
            return true;
        }
        catch (InvalidCastException)
        {
            //
        }
        return false;
    }
}
