using Godot;
using System;

public interface ITerrainLayer
{
    Image WorldImage { get; }
    BlockMapping BlockList { get; }
    void Lock();
}