using Godot;
using System;

/* This class represents a blocks hitbox. This is only spawned when the player get's very close
to save memory. This has nothing to do with a Block's data, which is found in the Block class. */
public class BlockHitbox : CollisionShape2D
{
    /* Set's the size of the hitbox to be the Terrain's BlockPixelSize. */
    public override void _Ready()
    {
        Name = "BlockHitbox";
        RectangleShape2D shape = (RectangleShape2D)Shape;
        Terrain terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        shape.Extents = terrain.BlockPixelSize / 2;
    }
}
