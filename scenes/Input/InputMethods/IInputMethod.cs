using Godot;

/* This interface defines an input method for the ActionMapping system. This
allows for a InputEvent to be used with the different children that work
differently from it.
*/
public interface IInputMethod
{
    // Return the keycode for the event
    int GetKeyCode(InputEvent @event);

    // Return an InputEvent instance with the keyCode inside it
    InputEvent GetInputEvent(int keyCode);

    // Since @event is an instance of a subclass of InputEvent,
    // return true if this InputEvent child is the @event's real
    // type.
    bool IsInputEventInstance(InputEvent @event);

    // Name of the input method for saving and loading the mappings
    // to a file.
    string GetInputMethodName();
}
