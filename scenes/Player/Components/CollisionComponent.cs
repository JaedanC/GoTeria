using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class CollisionComponent : Node2D
{
    private Terrain terrain;
    private CollisionSystem collisionSystem;
    private ICollidable parent;
    private Rect2 previousParentHitboxRect;
    private Rect2 nextParentHitboxRect;
    private Rect2 mergedParentHitboxRect;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Name = "CollisionComponent";

        terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        collisionSystem = GetNode<CollisionSystem>("/root/WorldSpawn/CollisionSystem");
        parent = GetParent<ICollidable>();
    }

    public override void _PhysicsProcess(float delta)
    {
        UpdateCollisionVisiblityRect(delta);
        CreateCollisionVisibleBlockHitboxes();
        Move(parent.GetVelocity(), delta);
    }

    /* This function expands a Rect2 to include the player and where the player would be
    after adding their velocity to the Hitbox. This Rect2 is mergedParentHitboxRect, and
    it is the area that is used to spawn in blocks. */
    private void UpdateCollisionVisiblityRect(float delta)
    {
        RectangleShape2D shape = (RectangleShape2D)parent.GetHitbox().Shape;
        Vector2 hitboxSize = shape.Extents;
        // TODO: In future, take into account that some entities may be
        // defined by more than one Hitbox.
        Vector2 collisionVisibility = parent.GetHitbox().Position - hitboxSize;
        previousParentHitboxRect = new Rect2(collisionVisibility + parent.GetRigidBody().Position, 2 * hitboxSize);
        nextParentHitboxRect = previousParentHitboxRect;
        nextParentHitboxRect.Position += parent.GetVelocity() * delta;
        // Expand the Rect2 to include their nextPosition
        mergedParentHitboxRect = nextParentHitboxRect.Merge(previousParentHitboxRect);
        // TODO: Maybe in future return a value?
    }

    /* This method spawns in the blocks that are inside the player's mergedParentHitboxRect.
    This reduces the number of objects created to only be blocks that can be collided with the player,
    heavily reducing the time complexity of calculating the player's collision. */
    private void CreateCollisionVisibleBlockHitboxes()
    {
        collisionSystem.CreateBlockHitboxesInArea(mergedParentHitboxRect);
    }

    private void Move(Vector2 vector, float delta)
    {
        // Fixes infinite velocity Portal 2 style
        Vector2 oldParentPosition = parent.GetRigidBody().Position;

        // Let's do the collision in two parts. This seems to fix many edge case with movement hitting
        // zero randomly on slopes when sliding.
        Vector2 response = parent.GetRigidBody().MoveAndSlide(vector, Vector2.Up);
        Vector2 newPosition = parent.GetRigidBody().Position;

        // GD.Print("Player moved: " + (newPosition - oldParentPosition));

        parent.SetVelocity(response);
        // Vector2 firstResponse = parent.GetRigidBody().MoveAndSlide(new Vector2(vector.x, 0), Vector2.Up);
        // Vector2 secondResponse = parent.GetRigidBody().MoveAndSlide(new Vector2(0, vector.y), Vector2.Up);
        // parent.SetVelocity(firstResponse + secondResponse);

        // Portal 2 fix pt.2        
        if (parent.GetRigidBody().Position == oldParentPosition)
            parent.SetVelocity(Vector2.Zero);
    }

    // public override void _Process(float delta)
    // {
    //     Update();
    // }

    // public override void _Draw()
    // {
    //     if (mergedParentHitboxRect != null)
    //     {
    //         foreach (Vector2 point in GetHitboxBlockVisibilityPoints(mergedParentHitboxRect))
    //         {
    //             Vector2 worldLocation = point * terrain.BlockPixelSize;
    //             DrawCircle(worldLocation, 4, new Color(0, 1, 0));
    //         }
    //     }
    // }
}
