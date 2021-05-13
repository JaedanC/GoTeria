using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class CollisionComponent : Node2D
{
    private Terrain terrain;
    private ICollidable parent;
    private Rect2 previousParentHitboxRect;
    private Rect2 nextParentHitboxRect;
    private Rect2 mergedParentHitboxRect;
    private PackedScene blockScene;
    private Dictionary<Vector2, StaticBody2D> loadedBlocks;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Name = "CollisionComponent";

        terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        parent = GetParent<ICollidable>();

        // The block scene is used exclusively for collision detection. See Chunk for more
        // information on how blocks are actually stored in memory.
        blockScene = (PackedScene)ResourceLoader.Load("res://scenes/BlockHitbox/BlockHitbox.tscn");

        // These are the blocks locally created by this script. TODO: Maybe in future
        // have these blocks be stored someone else so that blocks are not instanced twice
        // unnecessarily.
        loadedBlocks = new Dictionary<Vector2, StaticBody2D>();
    }

    public override void _PhysicsProcess(float delta)
    {
        UpdateCollisionVisiblityRect(delta);
        DeleteInvisibleBlocksHitboxes();
        CreateVisibleBlocksHitboxes();
        Move(parent.Velocity);
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
        Vector2 collision_visibility = parent.GetHitbox().Position - hitboxSize;
        previousParentHitboxRect = new Rect2(collision_visibility + parent.GetRigidBody().Position, 2 * hitboxSize);
        nextParentHitboxRect = previousParentHitboxRect;
        nextParentHitboxRect.Position += parent.Velocity * delta;
        // Expand the Rect2 to include their nextPosition
        mergedParentHitboxRect = nextParentHitboxRect.Merge(previousParentHitboxRect);
        // TODO: Maybe in future return a value?
    }

    /* This method spawns in the blocks that are inside the player's mergedParentHitboxRect.
    This reduces the number of objects created to only be blocks that can be collided with the player,
    heavily reducing the time complexity of calculating the player's collision. */
    private void CreateVisibleBlocksHitboxes()
    {
        Array<Vector2> visibleBlockPoints = GetHitboxBlockVisibilityPoints(mergedParentHitboxRect);
        foreach (Vector2 visibleBlockPoint in visibleBlockPoints)
        {
            if (loadedBlocks.ContainsKey(visibleBlockPoint))
                continue;
            
            Block existingBlock = terrain.GetBlockFromWorldPosition(visibleBlockPoint * terrain.BlockPixelSize);

            // Don't add collision for air.
            if (existingBlock != null && existingBlock.IsSolid())
            {
                StaticBody2D block = (StaticBody2D)blockScene.Instance(); // Instance the block scene.

                // The order of these two operations is very important. Flip them, and collision goes out the window.
                // I assume this is because the _Ready() method in the block requires this value, but I'll be honest
                // I have no idea why this is the case from looking at the source code.
                block.Position = visibleBlockPoint * terrain.BlockPixelSize + terrain.BlockPixelSize / 2;
                AddChild(block);
                loadedBlocks[visibleBlockPoint] = block;
            }
        }
    }

    /* Free blocks from the SceneTree that are no longer inside the mergedParentHitboxRect. */
    private void DeleteInvisibleBlocksHitboxes() {
        Dictionary<Vector2, StaticBody2D> visibleBlocks = new Dictionary<Vector2, StaticBody2D>();
        Array<Vector2> visibleBlockPoints = GetHitboxBlockVisibilityPoints(mergedParentHitboxRect);
        foreach (Vector2 visibleBlockPoint in visibleBlockPoints)
        {
            if (loadedBlocks.ContainsKey(visibleBlockPoint))
            {
                Block existingBlock = terrain.GetBlockFromWorldPosition(visibleBlockPoint * terrain.BlockPixelSize);
                // Don't need to check for null as it was only added if the block wasn't null.
                Debug.Assert(existingBlock != null);

                // If the block is now air, that means the player mined the block while it was loaded.
                // We continue because we now do not want the block to go back into the visibleBlocks
                // Dictionary.
                if (!existingBlock.IsSolid())
                    continue;
                
                visibleBlocks[visibleBlockPoint] = loadedBlocks[visibleBlockPoint];

                // Make sure we did indeed remove a block.
                bool erased = loadedBlocks.Remove(visibleBlockPoint);
                Debug.Assert(erased);
            }
        }

        // After the loop above, the loadedBlocks Dictionary now only contains blocks
        // that aren't visible. Thus, we remove them from the Scene Tree.
        foreach (Vector2 invisibleBlock in loadedBlocks.Keys)
        {
            loadedBlocks[invisibleBlock].QueueFree();
            // loadedBlocks[invisibleBlock].Free();
        }
        
        // Update the blocks that are loaded.
        // loadedBlocks = new Dictionary<Vector2, StaticBody2D>(visibleBlocks);
        loadedBlocks = visibleBlocks;
    }

    /* Returns an Array of points that lie on the block indices contained by the area Rect2. */
    private Array<Vector2> GetHitboxBlockVisibilityPoints(Rect2 area)
    {
        Vector2 topLeft = (area.Position / terrain.BlockPixelSize).Floor();
        Vector2 bottomRight = ((area.Position + area.Size) / terrain.BlockPixelSize).Floor();
        Array<Vector2> visibilityPoints = new Array<Vector2>();
        for (int i = (int)topLeft.x; i < (int)bottomRight.x + 1; i++)
        for (int j = (int)topLeft.y; j < (int)bottomRight.y + 1; j++)
        {
            visibilityPoints.Add(new Vector2(i, j));
        }
        return visibilityPoints;
    }

    private void Move(Vector2 vector)
    {
        // Fixes infinite velocity Portal 2 style
        Vector2 oldParentPosition = parent.GetRigidBody().Position;
        
        // Let's do the collision in two parts. This seems to fix many edge case with movement hitting
        // zero randomly on slopes when sliding.
        Vector2 firstResponse = parent.GetRigidBody().MoveAndSlide(new Vector2(vector.x, 0), Vector2.Up);
        Vector2 secondResponse = parent.GetRigidBody().MoveAndSlide(new Vector2(0, vector.y), Vector2.Up);
        parent.Velocity = firstResponse + secondResponse;

        // Portal 2 fix pt.2        
        if (parent.GetRigidBody().Position == oldParentPosition)
            parent.Velocity = Vector2.Zero;
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