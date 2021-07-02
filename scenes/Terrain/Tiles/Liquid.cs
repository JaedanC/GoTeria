using Godot;

/* This class represents a single block's data. It is a class not a struct because I intend
for this class to get to a medium size, and I would rather c# return references not copies of
this type. */
public class Liquid : MasterBlock
{
    public Liquid(string name, Color colour) : base(name, colour) { }
    
    public override bool IsSolid()
    {
        return false;
    }

    public override float GetTransparency()
    {
        return 0.06f;
    }
}
