using Godot;

public interface IBlock
{
    int Id { get; set; }
    Color Colour { get; set; }
    bool IsSolid();
}