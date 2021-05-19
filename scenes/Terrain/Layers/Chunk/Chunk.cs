using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class Chunk : Node2D, IResettable
{
    private Terrain _terrain;
    private Image _worldImage;
    private Image _chunkImage;
    private readonly ImageTexture _chunkTexture;
    private Vector2 _chunkPosition;
    private Vector2 _blockCount;
    private Vector2 _blockPixelSize;
    private Block[] _blocks;
    private bool _locked;
    private bool _drawn;

    public Image ChunkImage { get { return _chunkImage; } }
    public Vector2 ChunkPosition { get { return _chunkPosition; } }
    public Block[] Blocks { get { return _blocks; } }
    public bool Locked
    {
        get { return _locked; } 
        set
        {
            if (value)
            {
                Debug.Assert(!_locked);
            }
            _locked = value;
        }
    }
    public bool Loaded { get; set; }
    /* This variable is true only after a chunk has been fully loaded AND then drawn.
    This is so the chunks draw call is cached and the terrain knows not to try
    and draw this chunk to the screen again. */
    public bool Drawn { get { return _drawn; } }
    public bool MemoryAllocated { get; set; }
    public ChunkLighting ChunkLighting;
    

    public Chunk()
    {
        Name = "Chunk";
        _chunkTexture = new ImageTexture();
    }

    public override void _Ready()
    {
        _terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        ChunkLighting = new ChunkLighting(this, _terrain);
    }

    /* This is the method that is called when a chunk is reset before it is reused. */
    public void Reset(object[] parameters)
    {
        _worldImage = (Image)parameters[0];
        _chunkPosition = (Vector2)parameters[1];
        _blockPixelSize = (Vector2)parameters[2];
        _blockCount = (Vector2)parameters[3];
        Loaded = false;
        _locked = false;
        _drawn = false;
        Position = _blockPixelSize * _chunkPosition * _blockCount;
    }

    /* This function should only be run once. This initialises a chunk to have the memory it requires
    to be allocated on the heap. */ 
    public void AllocateMemory(params object[] memoryAllocationParameters)
    {
        _blockCount = (Vector2)memoryAllocationParameters[0];
        int blocksInChunk = (int)(_blockCount.x * _blockCount.y);
        _blocks = new Block[blocksInChunk];
        _chunkImage = new Image();
        _chunkImage.Create((int)_blockCount.x, (int)_blockCount.y, false, Image.Format.Rgba8);
        MemoryAllocated = true;
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

    /* Draw the chunk to the screen using my special colour formula. This function
    Is run when a chunk is created however, we only want it to count as being
    run after all the blocks have been loaded.*/
    private void DrawChunk()
    {
        if (!Loaded)
            return;
        _drawn = true;

        _chunkTexture.CreateFromImage(ChunkImage, (int)Texture.FlagsEnum.Mipmaps | (int)Texture.FlagsEnum.AnisotropicFilter);
        DrawSetTransform(Vector2.Zero, 0, _blockPixelSize);
        DrawTexture(_chunkTexture, Vector2.Zero);
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

    public Block GetBlockFromBlockPosition(Vector2 blockPosition)
    {
        if (!IsValidBlockPosition(blockPosition))
            return null;
        return _blocks[BlockPositionToBlockIndex(blockPosition)];
    }

    public void SetBlockFromBlockPosition(Vector2 blockPosition, Block newBlock)
    {
        if (!IsValidBlockPosition(blockPosition))
            return;

        _blocks[BlockPositionToBlockIndex(blockPosition)] = newBlock;
        _chunkImage.Lock();
        if (newBlock.IsSolid())
            _chunkImage.SetPixelv(blockPosition, newBlock.Colour);
        else
            _chunkImage.SetPixelv(blockPosition, new Color(0, 0, 0, 0));
        _chunkImage.Unlock();
        Update();
    }

    public override void _Draw()
    {
        // DrawCircle(Vector2.Zero, 2, Color.aquamarine);
        DrawChunk();
    }
}
