using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class Chunk : Node2D, IResettable
{
    private Terrain _terrain;
    private Image _worldImage;
    // private readonly ImageTexture _chunkTexture;
    private Vector2 _chunkPosition;
    private Vector2 _blockCount;
    private Vector2 _blockPixelSize;
    private bool _doneLightingPass;
    private bool _loadLocked;
    private bool _lightingLocked;
    private bool _memoryAllocated;
    private ChunkStack _chunkStack;

    public Vector2 ChunkPosition { get { return _chunkPosition; } }
    public bool LoadLocked
    {
        get { return _loadLocked; } 
        set
        {
            if (value)
            {
                Debug.Assert(!_loadLocked);
            }
            _loadLocked = value;
        }
    }
    public bool LightingLocked
    {
        get { return _lightingLocked; } 
        set
        {
            if (value)
            {
                Debug.Assert(!_lightingLocked);
            }
            _lightingLocked = value;
        }
    }

    public bool Loaded { get; set; }
    /* This variable is true only after a chunk has been fully loaded AND then drawn.
    This is so the chunks draw call is cached and the terrain knows not to try
    and draw this chunk to the screen again. */
    public bool LightingDone { get; set; }
    private ChunkLighting _chunkLighting;
    public Block[] Blocks { get { return _chunkStack.Blocks; } }
    public Wall[] Walls { get { return _chunkStack.Walls; } }
    // public Image ChunkImage { get { return _chunkStack.GetRawChunkImage(); } }
    

    public static int BlockPositionToBlockIndex(Vector2 chunkSize, Vector2 blockPosition)
    {
        return (int)(chunkSize.x * blockPosition.y + blockPosition.x);
    }

    public Chunk()
    {
        Name = "Chunk";
        // _chunkTexture = new ImageTexture();
        _chunkStack = new ChunkStack();
    }

    /* This function should only be run once. This initialises a chunk to have the memory it requires
    to be allocated on the heap. */ 
    public void AllocateMemory(params object[] memoryAllocationParameters)
    {
        _blockCount = (Vector2)memoryAllocationParameters[0];
        _chunkStack.AllocateMemory(_blockCount);
        _memoryAllocated = true;
    }

    public override void _Ready()
    {
        _terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        _chunkLighting = new ChunkLighting(this, _terrain);
    }

    public void Create(Vector2 chunkPosition, Vector2 blockCount, Image worldBlocksImages, Image worldWallsImage)
    {
        if (!_memoryAllocated)
            AllocateMemory(blockCount);
        
        _chunkStack.Create(chunkPosition, blockCount, worldBlocksImages, worldWallsImage);
        Loaded = true;
        // _chunkLighting.ComputeLightingPass();
    }

    /* This is the method that is called when a chunk is reset before it is reused. */
    public void Reset(object[] parameters)
    {
        _worldImage = (Image)parameters[0];
        _chunkPosition = (Vector2)parameters[1];
        _blockPixelSize = (Vector2)parameters[2];
        _blockCount = (Vector2)parameters[3];
        _doneLightingPass = false;
        _loadLocked = false;
        _lightingLocked = false;
        LightingDone = false;
        Loaded = false;
        Position = _blockPixelSize * _chunkPosition * _blockCount;
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
        chunkFile.OpenCompressed(String.Format("user://chunk_data/{0}.dat", _chunkPosition), File.ModeFlags.Write, File.CompressionMode.Zstd);
        
        // Save all chunk data in here
        for (int i = 0; i < _blockCount.x; i++)
        for (int j = 0; j < _blockCount.y; j++)
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
        _chunkLighting.ComputeLightingPass();
        LightingDone = true;
    }

    /* Draw the chunk to the screen using my special colour formula. This function
    Is run when a chunk is created however, we only want it to count as being
    run after all the blocks have been loaded.*/
    private void DrawChunk()
    {
        if (!Loaded || !LightingDone)
            return;
        
        DrawSetTransform(Vector2.Zero, 0, _blockPixelSize);
        foreach (ImageTexture texture in _chunkStack.ComputeAndGetTextures())
        {
            DrawTexture(texture, Vector2.Zero);
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private bool IsValidBlockPosition(Vector2 blockPosition)
    {
    	return !(blockPosition.x < 0 || blockPosition.x >= _blockCount.x || 
			    blockPosition.y < 0 || blockPosition.y >= _blockCount.y);
    }

    public int BlockPositionToBlockIndex(Vector2 blockPosition)
    {
	    return (int)(_blockCount.x * blockPosition.y + blockPosition.x);
    }

    public IBlock GetTopIBlockFromBlockPosition(Vector2 blockPosition)
    {
        if (!IsValidBlockPosition(blockPosition))
            return null;
        return _chunkStack.GetTopIBlock(blockPosition);
    }

    private IBlock GetIBlockFromBlockPosition(IBlock[] blocks, Vector2 blockPosition)
    {
        if (!IsValidBlockPosition(blockPosition))
            return null;
        return blocks[BlockPositionToBlockIndex(blockPosition)];
    }

    public Block GetBlockFromBlockPosition(Vector2 blockPosition)
    {
        return (Block)GetIBlockFromBlockPosition(_chunkStack.Blocks, blockPosition);
    }

    public Wall GetWallFromBlockPosition(Vector2 blockPosition)
    {
        return (Wall)GetIBlockFromBlockPosition(_chunkStack.Walls, blockPosition);
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
        SetIBlockFromBlockPosition(_chunkStack.Blocks, _chunkStack.GetBlocksImage(), blockPosition, newBlock);
    }

    public void SetWallFromBlockPosition(Vector2 blockPosition, Wall newWall)
    {
        SetIBlockFromBlockPosition(_chunkStack.Walls, _chunkStack.GetWallsImage(), blockPosition, newWall);
    }
}
