using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class Chunk : Node2D, IResettable
{
    private Terrain terrain;
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
                Debug.Assert(!loadLocked);
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
                Debug.Assert(!lightingLocked);
            }
            lightingLocked = value;
        }
    }
    public bool LoadingDone { get; set; }
    public bool LightingDone { get; set; }
    private ChunkLighting chunkLighting;
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

    public override void _Ready()
    {
        terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        chunkLighting = new ChunkLighting(this, terrain);
    }

    public void Create(Vector2 chunkPosition, Vector2 blockCount, Image worldBlocksImages, Image worldWallsImage)
    {
        if (!memoryAllocated)
            AllocateMemory(blockCount);

        chunkStack.Create(chunkPosition, blockCount, worldBlocksImages, worldWallsImage);
        LoadingDone = true;
    }

    /* This is the method that is called when a chunk is reset before it is reused. */
    public void Reset(object[] parameters)
    {
        worldImage = (Image)parameters[0];
        chunkPosition = (Vector2)parameters[1];
        blockPixelSize = (Vector2)parameters[2];
        blockCount = (Vector2)parameters[3];
        loadLocked = false;
        lightingLocked = false;
        LightingDone = false;
        LoadingDone = false;
        Position = blockPixelSize * chunkPosition * blockCount;
    }


    /* This method will save all the data in a chunk to disk. Currently it is being
    done using compression, however this can be changed below. TODO: Change this
    to take in a parameter as a save destination. Currently it's hardcoded. */
    public void SaveChunk()
    {
        // Create the directory if it does not exist
        Directory directory = new Directory();
        directory.MakeDir("user://chunk_data");

        // Create a file for each chunk. Store chunk specfic data below. File
        // size heavily depends on how varied the data is stored between blocks.
        File chunkFile = new File();
        // chunk.Open("user://chunk_data/%s.dat" % worldPosition, File.Write);
        chunkFile.OpenCompressed(String.Format("user://chunk_data/{0}.dat", chunkPosition), File.ModeFlags.Write, File.CompressionMode.Zstd);

        // Save all chunk data in here
        for (int i = 0; i < blockCount.x; i++)
            for (int j = 0; j < blockCount.y; j++)
            {
                float randomNumber = Mathf.Floor(Convert.ToSingle(GD.RandRange(0, 5)));
                chunkFile.Store16(Convert.ToUInt16(randomNumber));
            }
        chunkFile.Close();
    }

    public override void _Draw()
    {
        // DrawCircle(Vector2.Zero, 2, Color.aquamarine);
        DrawChunk();
    }

    public void ComputeLightingPass()
    {
        GD.Print("Computed Lighting for chunk: " + ChunkPosition);
        chunkLighting.ComputeLightingPass();
        LightingDone = true;
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
