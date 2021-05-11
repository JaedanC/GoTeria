using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class CollisionComponent : Node2D
{
    private Terrain terrain;
    private Player player;
    private KinematicBody2D parentRigidbody;
    private CollisionShape2D parentHitbox;

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
        player = GetParent<Player>();
        parentRigidbody = player.GetNode<KinematicBody2D>("Rigidbody");
        parentHitbox = parentRigidbody.GetNode<CollisionShape2D>("Hitbox");

        blockScene = (PackedScene)ResourceLoader.Load("res://scenes/Block/Block.tscn");
        loadedBlocks = new Dictionary<Vector2, StaticBody2D>();
    }

    public override void _PhysicsProcess(float delta)
    {
        UpdateCollisionVisiblityRect(delta);
        DeleteInvisibleBlocksHitboxes();
        CreateVisibleBlocksHitboxes();
        Move(player.GetVelocity());
    }

    private void UpdateCollisionVisiblityRect(float delta)
    {
        RectangleShape2D shape = (RectangleShape2D)parentHitbox.Shape;
        Vector2 hitboxSize = shape.Extents;
        // TODO Hardcoded hitbox size
        Vector2 collision_visibility = parentHitbox.Position - hitboxSize;
        previousParentHitboxRect = new Rect2(collision_visibility + parentRigidbody.Position, 2 * hitboxSize);
        nextParentHitboxRect = previousParentHitboxRect;
        nextParentHitboxRect.Position += player.GetVelocity() * delta;
        mergedParentHitboxRect = nextParentHitboxRect.Merge(previousParentHitboxRect);
    }

    private void CreateVisibleBlocksHitboxes()
    {
        Array<Vector2> visibleBlockPoints = GetHitboxVisibilityPoints(mergedParentHitboxRect);
        foreach (Vector2 visibleBlockPoint in visibleBlockPoints)
        {
            if (loadedBlocks.ContainsKey(visibleBlockPoint))
                continue;
            
            Dictionary<String, object> existingBlock = terrain.GetBlockFromWorldPosition(visibleBlockPoint * terrain.GetBlockPixelSize());
            if (existingBlock != null && (int)existingBlock["id"] != 0)
            {
                // TODO: This could be my memory leak
                StaticBody2D block = (StaticBody2D)blockScene.Instance();

                // The order of these two operations is very important. Flip them, and collision goes out the window
                block.Position = visibleBlockPoint * terrain.GetBlockPixelSize() + terrain.GetBlockPixelSize() / 2;
                AddChild(block);
                loadedBlocks[visibleBlockPoint] = block;
            }
        }
    }

    private void DeleteInvisibleBlocksHitboxes() {
        Dictionary<Vector2, StaticBody2D> visibleBlocks = new Dictionary<Vector2, StaticBody2D>();
        Array<Vector2> visibleBlockPoints = GetHitboxVisibilityPoints(mergedParentHitboxRect);
        foreach (Vector2 visibleBlockPoint in visibleBlockPoints)
        {
            if (loadedBlocks.ContainsKey(visibleBlockPoint))
            {
                Dictionary<String, object> existingBlock = terrain.GetBlockFromWorldPosition(visibleBlockPoint * terrain.GetBlockPixelSize());
                // TODO check for null?
                if ((int)existingBlock["id"] == 0) // Don't add collision for air. TODO: use a future is_solid() method
                    continue;
                
                visibleBlocks[visibleBlockPoint] = loadedBlocks[visibleBlockPoint];
                bool erased = loadedBlocks.Remove(visibleBlockPoint);
			    Debug.Assert(erased == true);
            }
        }

        foreach (Vector2 invisibleBlock in loadedBlocks.Keys)
        {
            loadedBlocks[invisibleBlock].QueueFree();
        }

        loadedBlocks = visibleBlocks;
    }

    private Array<Vector2> GetHitboxVisibilityPoints(Rect2 area)
    {
        Vector2 topLeft = (area.Position / terrain.GetBlockPixelSize()).Floor();
        Vector2 bottomRight = ((area.Position + area.Size) / terrain.GetBlockPixelSize()).Floor();
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
        Vector2 oldParentPosition = parentRigidbody.Position;
        
        // Let's do the collision in two parts:
        Vector2 firstResponse = parentRigidbody.MoveAndSlide(new Vector2(vector.x, 0), Vector2.Up);
        Vector2 secondResponse = parentRigidbody.MoveAndSlide(new Vector2(0, vector.y), Vector2.Up);
        player.SetVelocity(firstResponse + secondResponse);

        // Portal 2 fix pt.2        
        if (parentRigidbody.Position == oldParentPosition)
            player.SetVelocity(Vector2.Zero);
    }

    // public override void _Process(float delta)
    // {
    //     Update();
    // }

    // public override void _Draw()
    // {
    //     if (mergedParentHitboxRect != null)
    //     {
    //         foreach (Vector2 point in GetHitboxVisibilityPoints(mergedParentHitboxRect))
    //         {
    //             Vector2 worldLocation = point * terrain.GetBlockPixelSize();
    //             DrawCircle(worldLocation, 4, new Color(0, 1, 0));
    //         }
    //     }
    // }
}