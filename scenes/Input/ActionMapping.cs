using System;
using System.Collections.Generic;
using Godot;


public class ActionMapping : Node
{
    private class ClassPair
    {
        public IInputMethod InputMethod;
        public int KeyCode;

        public ClassPair(IInputMethod inputMethod, int keyCode)
        {
            this.InputMethod = inputMethod;
            this.KeyCode = keyCode;
        }
    }

    private Dictionary<IInputMethod, Dictionary<String, int>> actionBindings;
    private KeyboardInput keyboardInput;
    private MouseInput mouseInput;
    private JoypadInput joypadInput;
    private JoypadAxisInput joypadAxisInput;
    private AnalogMapping analogMapping;

    public override void _Ready()
    {
        actionBindings = new Dictionary<IInputMethod, Dictionary<String, int>>();
        keyboardInput = new KeyboardInput();
        mouseInput = new MouseInput();
        joypadInput = new JoypadInput();
        joypadAxisInput = new JoypadAxisInput();
        actionBindings[keyboardInput] = new Dictionary<String, int>();
        actionBindings[mouseInput] = new Dictionary<String, int>();
        actionBindings[joypadInput] = new Dictionary<String, int>();
        actionBindings[joypadAxisInput] = new Dictionary<String, int>();
    }

    public void Initialise(AnalogMapping analogMapping)
    {
        this.analogMapping = analogMapping;
        SetupAliases();
        SetupActionMappings();
    }

    /* Create bindings for game events. "*/
    private void SetupActionMappings()
    {
        AddActionMapping("jump", " ");
        AddActionMapping("move_left", "a");
        AddActionMapping("move_right", "d");
        AddActionMapping("move_up", "w");
        AddActionMapping("move_down", "s");
        AddActionMapping("zoom_reset", "backspace");
        AddActionMapping("zoom_in", "=");
        AddActionMapping("zoom_in", "mwheeldown");
        AddActionMapping("zoom_out", "-");
        AddActionMapping("zoom_out", "mwheelup");

        AddActionMapping("spawn_slime", "mouse3");
        AddActionMapping("shoot_bullet", "mouse1");
        AddActionMapping("teleport", "tab");
        AddActionMapping("place_light", "mouse4");
        AddActionMapping("place_light", ",");
        AddActionMapping("remove_light", "mouse5");
        AddActionMapping("remove_light", ".");

        AddActionMapping("jump", "joy_a");
        AddActionMapping("brake", "joy_b");
        AddActionMappingAxis("move_left", "move_right", 0, 0, true);
        AddActionMappingAxis("move_up", "move_down", 0, 1, true);
        AddActionMapping("quit", "select");
        AddActionMapping("toggle_fullscreen", "start");

        AddActionMapping("save_config", "i");
        AddActionMapping("load_config", "o");
        AddActionMapping("show_mappings", "p");
        AddActionMapping("save_world", "F5");
        AddActionMapping("load_saved_world", "F9");
        AddActionMapping("place_block", "c");
        AddActionMapping("dig", "v");
        AddActionMapping("dig", "mouse2");
        AddActionMapping("brake", "x");
        AddActionMapping("quit", "esc");
        AddActionMapping("light_debug", "l");
        AddActionMapping("remove_light_debug", "k");
        AddActionMapping("remove_light_add_debug", "j");
        AddActionMapping("debug", "\\");
        AddActionMapping("toggle_fullscreen", "F11");
    }

    /* Remap Godot keys to strings to use in bindings. Ensure the keys are unique even
    across input methods. */
    private void SetupAliases()
    {
        Dictionary<String, int> keys = actionBindings[keyboardInput];
        keys.Add("a", (int)KeyList.A); keys.Add("b", (int)KeyList.B); keys.Add("c", (int)KeyList.C);
        keys.Add("d", (int)KeyList.D); keys.Add("e", (int)KeyList.E); keys.Add("f", (int)KeyList.F);
        keys.Add("g", (int)KeyList.G); keys.Add("h", (int)KeyList.H); keys.Add("i", (int)KeyList.I);
        keys.Add("j", (int)KeyList.J); keys.Add("k", (int)KeyList.K); keys.Add("l", (int)KeyList.L);
        keys.Add("m", (int)KeyList.M); keys.Add("n", (int)KeyList.N); keys.Add("o", (int)KeyList.O);
        keys.Add("p", (int)KeyList.P); keys.Add("q", (int)KeyList.Q); keys.Add("r", (int)KeyList.R);
        keys.Add("s", (int)KeyList.S); keys.Add("t", (int)KeyList.T); keys.Add("u", (int)KeyList.U);
        keys.Add("v", (int)KeyList.V); keys.Add("w", (int)KeyList.W); keys.Add("x", (int)KeyList.X);
        keys.Add("y", (int)KeyList.Y); keys.Add("z", (int)KeyList.Z);

        keys.Add("0", (int)KeyList.Key0); keys.Add("1", (int)KeyList.Key1); keys.Add("2", (int)KeyList.Key2);
        keys.Add("3", (int)KeyList.Key3); keys.Add("4", (int)KeyList.Key4); keys.Add("5", (int)KeyList.Key5);
        keys.Add("6", (int)KeyList.Key6); keys.Add("7", (int)KeyList.Key7); keys.Add("8", (int)KeyList.Key8);
        keys.Add("9", (int)KeyList.Key9);

        keys.Add("F1", (int)KeyList.F1); keys.Add("F2", (int)KeyList.F2); keys.Add("F3", (int)KeyList.F3);
        keys.Add("F4", (int)KeyList.F4); keys.Add("F5", (int)KeyList.F5); keys.Add("F6", (int)KeyList.F6);
        keys.Add("F7", (int)KeyList.F7); keys.Add("F8", (int)KeyList.F8); keys.Add("F9", (int)KeyList.F9);
        keys.Add("F10", (int)KeyList.F10); keys.Add("F11", (int)KeyList.F11); keys.Add("F12", (int)KeyList.F12);

        keys.Add("esc", (int)KeyList.Escape);
        keys.Add("`", (int)KeyList.Quoteleft);
        keys.Add("capslock", (int)KeyList.Capslock);
        keys.Add("tab", (int)KeyList.Tab);
        keys.Add("shift", (int)KeyList.Shift);
        keys.Add("ctrl", (int)KeyList.Control);
        keys.Add("alt", (int)KeyList.Alt);
        keys.Add("enter", (int)KeyList.Enter);
        keys.Add(" ", (int)KeyList.Space);
        keys.Add(",", (int)KeyList.Comma);
        keys.Add(".", (int)KeyList.Period);
        keys.Add("/", (int)KeyList.Slash);
        keys.Add("\\", (int)KeyList.Backslash);
        keys.Add(";", (int)KeyList.Semicolon);
        keys.Add("'", (int)KeyList.Apostrophe);
        keys.Add("[", (int)KeyList.Bracketleft);
        keys.Add("]", (int)KeyList.Bracketright);
        keys.Add("-", (int)KeyList.Minus);
        keys.Add("=", (int)KeyList.Equal);
        keys.Add("backspace", (int)KeyList.Backspace);

        keys.Add("del", (int)KeyList.Delete);
        keys.Add("ins", (int)KeyList.Insert);
        keys.Add("home", (int)KeyList.Home);
        keys.Add("end", (int)KeyList.End);
        keys.Add("pageup", (int)KeyList.Pageup);
        keys.Add("pagedown", (int)KeyList.Pagedown);

        keys.Add("left", (int)KeyList.Left);
        keys.Add("right", (int)KeyList.Right);
        keys.Add("up", (int)KeyList.Up);
        keys.Add("down", (int)KeyList.Down);

        keys.Add("kp0", (int)KeyList.Kp0); keys.Add("kp1", (int)KeyList.Kp1); keys.Add("kp2", (int)KeyList.Kp2);
        keys.Add("kp3", (int)KeyList.Kp3); keys.Add("kp4", (int)KeyList.Kp4); keys.Add("kp5", (int)KeyList.Kp5);
        keys.Add("kp6", (int)KeyList.Kp6); keys.Add("kp7", (int)KeyList.Kp7); keys.Add("kp8", (int)KeyList.Kp8);
        keys.Add("kp9", (int)KeyList.Kp9);
        keys.Add("kp.", (int)KeyList.KpPeriod);
        keys.Add("kpenter", (int)KeyList.KpEnter);
        keys.Add("kp+", (int)KeyList.KpAdd);
        keys.Add("kp-", (int)KeyList.KpSubtract);
        keys.Add("kp*", (int)KeyList.KpMultiply);
        keys.Add("kp/", (int)KeyList.KpDivide);

        Dictionary<String, int> mouse = actionBindings[mouseInput];
        mouse.Add("mouse1", (int)ButtonList.Left);
        mouse.Add("mouse2", (int)ButtonList.Right);
        mouse.Add("mouse3", (int)ButtonList.Middle);
        mouse.Add("mouse4", (int)ButtonList.Xbutton1);
        mouse.Add("mouse5", (int)ButtonList.Xbutton2);
        mouse.Add("mwheelup", (int)ButtonList.WheelUp);
        mouse.Add("mwheeldown", (int)ButtonList.WheelDown);

        Dictionary<String, int> joy = actionBindings[joypadInput];
        joy.Add("joy_a", (int)JoystickList.XboxA);
        joy.Add("joy_b", (int)JoystickList.XboxB);
        joy.Add("joy_x", (int)JoystickList.XboxX);
        joy.Add("Joy_y", (int)JoystickList.XboxY);
        joy.Add("dpad_left", (int)JoystickList.DpadLeft);
        joy.Add("dpad_right", (int)JoystickList.DpadRight);
        joy.Add("dpad_up", (int)JoystickList.DpadUp);
        joy.Add("dpad_down", (int)JoystickList.DpadDown);
        joy.Add("l1", (int)JoystickList.L);
        joy.Add("l2", (int)JoystickList.L2);
        joy.Add("l3", (int)JoystickList.L3);
        joy.Add("r1", (int)JoystickList.R);
        joy.Add("r2", (int)JoystickList.R2);
        joy.Add("r3", (int)JoystickList.R3);
        joy.Add("start", (int)JoystickList.Start);
        joy.Add("select", (int)JoystickList.Select);

        Dictionary<String, int> axis = actionBindings[joypadAxisInput];
        axis.Add("axis0", (int)JoystickList.Axis0); // Left Stick LR [-1, 1]
        axis.Add("axis1", (int)JoystickList.Axis1); // Left Stick UD [-1, 1]
        axis.Add("axis2", (int)JoystickList.Axis2); // Right Stick LR [-1, 1]
        axis.Add("axis3", (int)JoystickList.Axis3); // Right Stick UD [-1, 1]
        axis.Add("axis4", (int)JoystickList.Axis4);
        axis.Add("axis5", (int)JoystickList.Axis5);
        axis.Add("axis6", (int)JoystickList.Axis6); // Left Trigger [0, 1]
        axis.Add("axis7", (int)JoystickList.Axis7); // Right Trigger [0, 1]
        axis.Add("axis8", (int)JoystickList.Axis8);
        axis.Add("axis9", (int)JoystickList.Axis9);
    }

    /* See AnalogMapping for more information. */
    private void AddActionMappingAxis(String firstAction, String secondAction, int device, int joyAxis, bool useDeadZone)
    {
        analogMapping.AddDualAxisAction(firstAction, secondAction, device, joyAxis, useDeadZone);
    }

    /* Used by InputLaying to gain access to AnalogMapping. I could make AnalogMapping static,
    but that makes making bindings tie to a save really difficult. */
    public AnalogMapping GetAnalogMapping()
    {
        return analogMapping;
    }

    /* Binds a key to an action. */
    private void AddActionMapping(String gameAction, String keyString)
    {
        ClassPair inputClassAndKeyCode = GetClassPairFromKeyString(keyString);
        if (inputClassAndKeyCode == null)
            return;

        IInputMethod inputMethod = inputClassAndKeyCode.InputMethod;
        InputEvent inputEvent = inputMethod.GetInputEvent(inputClassAndKeyCode.KeyCode);

        if (!InputMap.HasAction(gameAction))
            InputMap.AddAction(gameAction);
        InputMap.ActionAddEvent(gameAction, inputEvent);
    }

    /* Finds the input method and Godot integer representing the key for the
    keyString binding. */
    private ClassPair GetClassPairFromKeyString(String keyStringToMatch)
    {
        foreach (IInputMethod inputMethod in actionBindings.Keys)
        {
            Dictionary<String, int> inputClassBindings = actionBindings[inputMethod];
            foreach (String keyString in inputClassBindings.Keys)
            {
                if (keyString.Equals(keyStringToMatch))
                {
                    return new ClassPair(inputMethod, inputClassBindings[keyStringToMatch]);
                }
            }
        }
        return null;
    }

    /* Print all the ActionMappings to the console.*/
    private void ShowMappings()
    {
        foreach (String action in InputMap.GetActions())
        {
            GD.Print("Action: " + action + ", Mappings: " + InputMap.GetActionList(action));
        }
    }

    /* This function converts the mapping data (which is in the format above) to a disk
    storable version that can be saved to a file. Pseudocode:
    
    dict = {}
    foreach action:   # jump
        foreach input_event tied to the action:   # [InputEventKey: 560]
            foreach input_method:   # KeyboardInput
                if the input_event is not the type of input_method:
                    continue
                
                Get the input_events 'integer' representation so that we can
                finding the binding associated with it.
                
                If the Dictionary or Array doesn't exist yet make it.
                    
                Add the binding to the dictionary, with the key being a string
                representation of the input_event class.
    return dict */
    private Dictionary<string, Dictionary<string, Godot.Collections.Array<String>>> GetBindingsAsSaveableDictionary()
    {
        var bindings = new Dictionary<string, Dictionary<string, Godot.Collections.Array<String>>>();
        foreach (String gameAction in InputMap.GetActions())
        {
            foreach (InputEvent inputForGameAction in InputMap.GetActionList(gameAction))
            {
                foreach (IInputMethod inputMethod in actionBindings.Keys)
                {
                    if (!inputMethod.IsInputEventInstance(inputForGameAction))
                    {
                        continue;
                    }

                    String inputTypeName = inputMethod.GetInputMethodName();
                    int keyCodeValue = inputMethod.GetKeyCode(inputForGameAction);

                    // Give the action mappings dict some default values
                    if (!bindings.ContainsKey(inputTypeName))
                    {
                        bindings[inputTypeName] = new Dictionary<String, Godot.Collections.Array<String>>();
                    }
                    if (!bindings[inputTypeName].ContainsKey(gameAction))
                    {
                        bindings[inputTypeName][gameAction] = new Godot.Collections.Array<String>();
                    }

                    bindings[inputTypeName][gameAction].Add(KeyIntToString(keyCodeValue, inputMethod));
                }
            }
        }
        return bindings;
    }

    /* Converts KeyCode Enum values to readable string that can be stored in a file.
    The strings are defined in setup_aliases(). */
    private String KeyIntToString(int keyCodeValue, IInputMethod inputMethod)
    {
        foreach (String keyString in actionBindings[inputMethod].Keys)
        {
            if (actionBindings[inputMethod][keyString] == keyCodeValue)
            {
                return keyString;
            }
        }
        return null;
    }

    /* Converts readable string representations of keys into the godot KeyCode Enum
    values. The strings are defined in setup_aliases(). */
    private int? KeyStringToInt(String keyString, IInputMethod inputMethod)
    {
        if (actionBindings[inputMethod].ContainsKey(keyString))
        {
            return actionBindings[inputMethod][keyString];
        }
        return null;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("show_mappings"))
        {
            ShowMappings();
        }

        if (@event.IsActionPressed("save_config"))
        {
            GD.Print("Saving Config");
            SaveActionMappingsConfig();
            GD.Print("Saved Config");
        }

        if (@event.IsActionPressed("load_config"))
        {
            GD.Print("Loading Config");
            LoadActionMappingsConfig();
            GD.Print("Loaded Config");
        }
    }

    /* Saves the action mappings and key bindings to a save file that is human
    readable. TODO: The file name is currently hardcoded and needs to be
    parameterised. */
    private void SaveActionMappingsConfig()
    {
        ConfigFile configFile = new ConfigFile();
        var saveableActionMappings = GetBindingsAsSaveableDictionary();

        foreach (String inputMethodName in saveableActionMappings.Keys)
        {
            foreach (String action in saveableActionMappings[inputMethodName].Keys)
            {
                configFile.SetValue(inputMethodName, action, saveableActionMappings[inputMethodName][action]);
            }
        }
        configFile.Save("user://action_mapping_config_v2.ini");
    }

    /* Loads and binds action mappings to keys based on a save file. TODO: The file
    name is currently hardcoded and needs to be parameterised. */
    private void LoadActionMappingsConfig()
    {
        ConfigFile configFile = new ConfigFile();
        Error error = configFile.Load("user://action_mapping_config_v2.ini");

        if (error != Error.Ok)
        {
            GD.Print("Loading action mappings error: " + error);
            return;
        }

        foreach (IInputMethod inputMethod in actionBindings.Keys)
        {
            String inputMethodName = inputMethod.GetInputMethodName();
            if (!configFile.HasSection(inputMethodName))
            {
                continue;
            }

            foreach (String action in configFile.GetSectionKeys(inputMethodName))
            {
                foreach (String binding in (Godot.Collections.Array)configFile.GetValue(inputMethodName, action))
                {
                    AddActionMapping(action, binding);
                }
            }
        }
    }
}
