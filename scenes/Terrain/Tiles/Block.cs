using Godot;

/* This class represents a single block's data. It is a class not a struct because I intend
for this class to get to a medium size, and I would rather c# return references not copies of
this type. */
public class Block : MasterBlock
{
    private bool isSolid;

    public Block(string name, Color colour, bool isSolid) : base(name, colour)
    {
        this.isSolid = isSolid;
    }
    
    public override bool IsSolid()
    {
        return isSolid;
    }

    public override float GetTransparency()
    {
        return 0.2f;
    }
}
