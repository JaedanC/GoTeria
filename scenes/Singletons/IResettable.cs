using Godot;
using Godot.Collections;
using System;

public interface IResettable {
    void Reset(params object[] resetParameters);
}