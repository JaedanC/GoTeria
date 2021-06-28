using Godot;
using System;


public class Chunk : Node2D, IResettable
{
    private Terrain terrain;
    private WorldFile worldFile;
    private ChunkLighting chunkLighting;
    private Image worldImage;
    private Vector2 chunkPosition;
    private Vector2 blockCount;
    private Vector2 blockPixelSize;
    private bool loadLocked;
    private bool lightingLocked;
    private bool memoryAllocated;
    private ChunkStack chunkStack;
    public Vector2 ChunkPosition { get { return chunkPosition; } }
    public bool LoadLocked
    {
        get { return loadLocked; }
        set
        {
            if (value)
            {
                Developer.AssertFalse(loadLocked);
            }
            loadLocked = value;
        }
    }
    public bool LightingLocked
    {
        get { return lightingLocked; }
        set
        {
            if (value)
            {
                Developer.AssertFalse(lightingLocked);
            }
            lightingLocked = value;
        }
    }
    public bool LoadingDone { get; set; }
    public bool LightingDone { get; set; }
    // private ChunkLighting chunkLighting;
    public Block[] Blocks { get { return chunkStack.Blocks; } }
    public Wall[] Walls { get { return chunkStack.Walls; } }


    public static int BlockPositionToBlockIndex(Vector2 chunkSize, Vector2 blockPosition)
    {
        return (int)(chunkSize.x * blockPosition.y + blockPosition.x);
    }

    public Chunk()
    {
        Name = "Chunk";
        chunkStack = new ChunkStack();
    }

    /* This function should only be run once. This initialises a chunk to have the memory it requires
    to be allocated on the heap. */
    public void AllocateMemory(params object[] memoryAllocationParameters)
    {
        blockCount = (Vector2)memoryAllocationParameters[0];
        chunkStack.AllocateMemory(blockCount);
        memoryAllocated = true;
    }

    /* This is the method that is called when a chunk is reset before it is reused. */
    public void Initialise(object[] parameters)
    {
        this.worldImage = (Image)parameters[0];
        this.chunkPosition = (Vector2)parameters[1];
        this.blockPixelSize = (Vector2)parameters[2];
        this.blockCount = (Vector2)parameters[3];
        this.terrain = (Terrain)parameters[4];
        this.worldFile = (WorldFile)parameters[5];
        this.chunkLighting = (ChunkLighting)parameters[6];
        this.loadLocked = false;
        this.lightingLocked = false;
        this.LightingDone = false;
        this.LoadingDone = false;
        this.Position = blockPixelSize * chunkPosition * blockCount;
    }

    public void Create(Vector2 chunkPosition, Vector2 blockCount, Image worldBlocksImages, Image worldWallsImage)
    {
        if (!memoryAllocated)
            AllocateMemory(blockCount);

        chunkStack.Create(chunkPosition, blockCount, worldBlocksImages, worldWallsImage);
        LoadingDone = true;
    }

    public override void _Draw()
    {
        // DrawCircle(Vector2.Zero, 2, Color.aquamarine);
        DrawChunk();
    }

    public void ComputeLightingPass()
    {
        GD.Print("Computed Lighting for chunk: " + ChunkPosition);
        // chunkLighting.ComputeLightingPass();
        // terrain.LightingEngine.LightChunk(this);
        chunkLighting.CalculateLightOrUseCachedLight(this);
        LightingDone = true;
    }

    public void OnDeath()
    {
        // chunkLighting.SaveLight();
    }

    /* Draw the chunk to the screen using my special colour formula. This function
    Is run when a chunk is created however, we only want it to count as being
    run after all the blocks have been loaded.*/
    private void DrawChunk()
    {
        if (!LoadingDone || !LightingDone)
            return;

        DrawSetTransform(Vector2.Zero, 0, blockPixelSize);
        foreach (ImageTexture texture in chunkStack.ComputeAndGetTextures())
        {
            DrawTexture(texture, Vector2.Zero);
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private bool IsValidBlockPosition(Vector2 blockPosition)
    {
        return !(blockPosition.x < 0 || blockPosition.x >= blockCount.x ||
                 blockPosition.y < 0 || blockPosition.y >= blockCount.y);
    }

    public int BlockPositionToBlockIndex(Vector2 blockPosition)
    {
        return (int)(blockCount.x * blockPosition.y + blockPosition.x);
    }

    public IBlock GetTopIBlockFromBlockPosition(Vector2 blockPosition)
    {
        if (!IsValidBlockPosition(blockPosition))
            return null;
        return chunkStack.GetTopIBlock(blockPosition);
    }

    private IBlock GetIBlockFromBlockPosition(IBlock[] blocks, Vector2 blockPosition)
    {
        if (!IsValidBlockPosition(blockPosition))
            return null;
        return blocks[BlockPositionToBlockIndex(blockPosition)];
    }

    public Block GetBlockFromBlockPosition(Vector2 blockPosition)
    {
        return (Block)GetIBlockFromBlockPosition(chunkStack.Blocks, blockPosition);
    }

    public Wall GetWallFromBlockPosition(Vector2 blockPosition)
    {
        return (Wall)GetIBlockFromBlockPosition(chunkStack.Walls, blockPosition);
    }

    private void SetIBlockFromBlockPosition(IBlock[] blocks, Image chunkLayerImage, Vector2 blockPosition, IBlock newBlock)
    {
        if (!IsValidBlockPosition(blockPosition))
            return;

        Color newColour;
        if (newBlock.IsSolid())
            newColour = newBlock.Colour;
        else
            newColour = new Color(0, 0, 0, 0);

        blocks[BlockPositionToBlockIndex(blockPosition)] = newBlock;
        chunkLayerImage.Lock();
        chunkLayerImage.SetPixelv(blockPosition, newColour);
        chunkLayerImage.Unlock();
        Update();
    }

    public void SetBlockFromBlockPosition(Vector2 blockPosition, Block newBlock)
    {
        SetIBlockFromBlockPosition(chunkStack.Blocks, chunkStack.GetBlocksImage(), blockPosition, newBlock);
    }

    public void SetWallFromBlockPosition(Vector2 blockPosition, Wall newWall)
    {
        SetIBlockFromBlockPosition(chunkStack.Walls, chunkStack.GetWallsImage(), blockPosition, newWall);
    }
}
