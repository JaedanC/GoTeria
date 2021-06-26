using Godot;


/* This class represents a blocks hitbox. This is only spawned when the player get's very close
to save memory. This has nothing to do with a Block's data, which is found in the Block class. */
public class BlockHitbox : StaticBody2D
{
    private CollisionShape2D hitbox;

    /* Set's the size of the hitbox to be the Terrain's BlockPixelSize. */
    public override void _Ready()
    {
        Name = "BlockHitbox";
        hitbox = GetNode<CollisionShape2D>("Hitbox");
    }

    public void Initialise(Terrain terrain)
    {
        RectangleShape2D shape = (RectangleShape2D)hitbox.Shape;
        shape.Extents = terrain.BlockPixelSize / 2;
    }
}
