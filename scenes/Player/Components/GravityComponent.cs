using Godot;

public class GravityComponent : Node2D
{
    private const bool Enabled = false;

    public override void _Ready()
    {
        Name = "GravityComponent";
    }

    /* Adds Gravity to the Node. */
    public override void _PhysicsProcess(float _delta)
    {
        if (!Enabled)
            return;
        
        ICollidable parent = GetParent<ICollidable>();
        parent.SetVelocity(parent.GetVelocity() + new Vector2(0, 98));
    }
}
