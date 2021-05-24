using Godot;

// TODO: Change to be meaningful and useful.
public class Wall : IBlock
{
    public int Id { get; set; }
    public Color Colour { get; set; }

    public Wall() {}

    public Wall(int id, Color colour)
    {
        this.Id = id;
        this.Colour = colour;
    }

    public bool IsSolid() {
        return false;
    }
}