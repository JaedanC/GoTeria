using Godot;
using System.Collections.Generic;


public class CollisionSystem : Node
{
    private Terrain terrain;
    
    // Blocks not mentioned will be deleted.
    private HashSet<Vector2> mentionedBlocks; 

    // These are the blocks loaded in the world.
    private Dictionary<Vector2, StaticBody2D> loadedBlocks; 

    // The block scene is used exclusively for collision detection. See Chunk for more
    // information on how blocks are actually stored in memory.
    private PackedScene blockScene;

    public override void _Ready()
    {
        terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        loadedBlocks = new Dictionary<Vector2, StaticBody2D>();
        mentionedBlocks = new HashSet<Vector2>();
        blockScene = (PackedScene)ResourceLoader.Load("res://scenes/Terrain/Chunk/Layers/BlockHitbox/BlockHitbox.tscn");
    }

    public override void _PhysicsProcess(float delta)
    {
        // Ensure this is run towards the end of the SceneTree;
        FreeUnmentionedBlockHitboxes();
    }

    /* Instance the block hitboxes of the points in the worldBlockPoints Array. Does not load a block twice. */
    public void CreateBlockHitboxes(Godot.Collections.Array<Vector2> worldBlockPoints)
    {
        foreach (Vector2 visibleBlockPoint in worldBlockPoints)
        {
            mentionedBlocks.Add(visibleBlockPoint);
            Block existingBlock = terrain.GetBlockFromWorldPosition(visibleBlockPoint * terrain.BlockPixelSize);

            // Don't add collision for air.
            // TODO: In future this is where we would put slopes.
            if (existingBlock != null && existingBlock.IsSolid())
            {
                if (loadedBlocks.ContainsKey(visibleBlockPoint))
                    continue;
                
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

    public void CreateBlockHitboxesInArea(Rect2 area)
    {
        Vector2 topLeft = (area.Position / terrain.BlockPixelSize).Floor();
        Vector2 bottomRight = ((area.Position + area.Size) / terrain.BlockPixelSize).Floor();
        Godot.Collections.Array<Vector2> visibilityPoints = new Godot.Collections.Array<Vector2>();
        for (int i = (int)topLeft.x; i < (int)bottomRight.x + 1; i++)
            for (int j = (int)topLeft.y; j < (int)bottomRight.y + 1; j++)
            {
                visibilityPoints.Add(new Vector2(i, j));
            }
        CreateBlockHitboxes(visibilityPoints);
    }

    /* Free blocks from the SceneTree that are no longer being asked for in CreateWorldBlockPointHitboxes. */
    private void FreeUnmentionedBlockHitboxes()
    {
        Dictionary<Vector2, StaticBody2D> newLoadedBlocks = new Dictionary<Vector2, StaticBody2D>();
        foreach (Vector2 mentionedBlock in mentionedBlocks)
        {
            if (!loadedBlocks.ContainsKey(mentionedBlock))
                continue;
            
            Block existingBlock = terrain.GetBlockFromWorldPosition(mentionedBlock * terrain.BlockPixelSize);

            // If the block is now air, that means the player mined the block while it was loaded.
            // We continue because we now do not want the block to go back into the visibleBlocks
            // Dictionary. Could be null if it was unloaded. 
            if (existingBlock == null || !existingBlock.IsSolid())
                continue;

            newLoadedBlocks[mentionedBlock] = loadedBlocks[mentionedBlock];
            bool erased = loadedBlocks.Remove(mentionedBlock);
            Developer.AssertTrue(erased);
        }

        // Reset the mentioned blocks for the next physics frame.
        mentionedBlocks.Clear();

        // After the loop above, the loadedBlocks Dictionary now only contains blocks
        // that aren't visible. Thus, we remove them from the Scene Tree.
        foreach (Vector2 invisibleBlock in loadedBlocks.Keys)
        {
            loadedBlocks[invisibleBlock].QueueFree();
            // loadedBlocks[invisibleBlock].Free();
        }

        // Update the blocks that are loaded.
        loadedBlocks = newLoadedBlocks;
    }
}
