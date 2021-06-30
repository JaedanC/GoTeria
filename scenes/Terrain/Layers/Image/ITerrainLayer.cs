using Godot;

public interface ITerrainLayer
{
    Image WorldImage { get; }
    BlockMapping BlockList { get; }
    void Lock();
}
