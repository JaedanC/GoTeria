using Godot;


public abstract class MasterBlock
{
    public Color Colour { get; private set; }
    public string Name { get; private set; }

    protected MasterBlock(string name, Color colour)
    {
        this.Name = name;
        this.Colour = colour;
    }
    public abstract bool IsSolid();
    public abstract float GetTransparency();
}
