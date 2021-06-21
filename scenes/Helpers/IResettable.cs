using Godot;
using Godot.Collections;
using System;

/* This interface defines the method required for an object to be 'Resettable'
in the ObjectPool class. Parameters required for the Resetting are found inside
resetParameters. */
public interface IResettable {
    void Reset(params object[] resetParameters);
    void AllocateMemory(params object[] memoryAllocationParameters);
}
