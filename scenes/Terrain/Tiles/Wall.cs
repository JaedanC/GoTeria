using Godot;

// TODO: Change to be meaningful and useful.
public class Wall : MasterBlock
{
    public Wall(string name, Color colour) : base(name, colour) { }
    
    public override bool IsSolid()
    {
        return false;
    }

    public override float GetTransparency()
    {
        return 0.03f;
    }
}
