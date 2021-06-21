using Godot;

/* This class represents a single block's data. It is a class not a struct because I intend
for this class to get to a medium size, and I would rather c# return references not copies of
this type. */
public class Block : IBlock
{
    public int Id { get; set; }
    public Color Colour { get; set; }

    public Block() { }

    public Block(int id, Color colour)
    {
        this.Id = id;
        this.Colour = colour;
    }

    public bool IsSolid()
    {
        return Id > 0;
    }

    public float GetTransparency()
    {
        return 0.2f;
    }
}
