using Godot;
using System;

public class JoypadInput : IInputMethod
{
    public int GetKeyCode(InputEvent @event)
    {
        InputEventJoypadButton inputEventJoypadButton = (InputEventJoypadButton)@event;
        return inputEventJoypadButton.ButtonIndex;
    }
    public InputEvent GetInputEvent(int keyCode)
    {
        InputEventJoypadButton inputEventJoypadButton = new InputEventJoypadButton {ButtonIndex = keyCode};
        return inputEventJoypadButton;
    }

    public string GetInputMethodName()
    {
        return "Joypad";
    }

    public bool IsInputEventInstance(InputEvent @event)
    {
        try
        {
            InputEventJoypadButton newEvent = (InputEventJoypadButton)@event;
            return true;
        }
        catch (InvalidCastException)
        {
            //
        }
        return false;
    }
}
