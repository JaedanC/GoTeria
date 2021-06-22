using Godot;
using System;

public interface IInputMethod
{
    int GetKeyCode(InputEvent @event);
    InputEvent GetInputEvent(int keyCode);
    String GetInputMethodName();

    bool IsInputEventInstance(InputEvent @event);
}
