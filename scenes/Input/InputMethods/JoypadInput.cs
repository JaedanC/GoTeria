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
        InputEventJoypadButton inputEventJoypadButton = new InputEventJoypadButton();
        inputEventJoypadButton.ButtonIndex = keyCode;
        return inputEventJoypadButton;
    }

    public String GetInputMethodName()
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
