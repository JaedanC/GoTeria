using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class Chunk : Node2D, IResettable
{
    
    private Terrain terrain;

    private Image worldImage;
    private Image chunkImage;
    private ImageTexture chunkTexture;
    private int volatileStreaming;

    private Vector2 chunkPosition;
    private Vector2 blockCount;
    private Vector2 blockPixelSize;

    private Dictionary<String, object>[] blocks;
    private bool loaded;
    private bool drawn;

    public Chunk()
    {
        chunkTexture = new ImageTexture();
        Name = "Chunk";
    }

    public override void _Ready()
    {
        terrain = GetTree().Root.GetNode<Terrain>("WorldSpawn/Terrain");
    }

    /* This is the method that is called when a chunk is reset before it is reused. */
    // public void Reset(Vector2 chunkPosition)
    public void Reset(object[] parameters)
    {
        worldImage =        (Image)parameters[0];
        chunkPosition =     (Vector2)parameters[1];
        blockCount =        (Vector2)parameters[2];
        blockPixelSize =    (Vector2)parameters[3];

        int blocksInChunk = (int)(blockCount.x * blockCount.y);
        blocks = new Dictionary<String, object>[blocksInChunk];
        Position = blockPixelSize * chunkPosition * blockCount;
        volatileStreaming = 0;
        loaded = false;
        drawn = false;
    }

    // public override void _Process(float delta)
    // {
    //     Update();
    // }

    public void Lock()
    {
        Debug.Assert(volatileStreaming == 0);
        volatileStreaming += 1;
    }

    public bool IsLocked()
    {
        return volatileStreaming > 0;
    }

    public bool IsLoaded()
    {
        return loaded;
    }

    /* This function is true only after a chunk has been fully loaded AND then drawn.
    This is so the chunks draw call is cached and the terrain knows not to try
    and draw this chunk to the screen again. */
    public bool IsDrawn()
    {
        return drawn;
    }

    public void ObtainChunkData(Array<Dictionary<String, object>> blocks, Image chunkImage)
    {
        // this.blocks = new Dictionary<String, object>[(int)blockCount.x * (int)blockCount.y];
        
        for (int i = 0; i < blocks.Count; i++)
        {
            this.blocks[i] = blocks[i];
        }
        this.chunkImage = chunkImage;
        loaded = true;
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

    /* Draw the chunk to the screen using my special colour formula. This function
    Is run when a chunk is created however, we only want it to count as being
    run after all the blocks have been loaded.*/
    private void DrawChunk()
    {
        if (!IsLoaded())
            return;
        drawn = true;

        chunkTexture.CreateFromImage(chunkImage, (int)Texture.FlagsEnum.Mipmaps | (int)Texture.FlagsEnum.AnisotropicFilter);
        DrawSetTransform(Vector2.Zero, 0, blockPixelSize);
        DrawTexture(chunkTexture, Vector2.Zero);
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

    public Dictionary<String, object> GetBlockFromBlockPosition(Vector2 blockPosition)
    {
        if (!IsValidBlockPosition(blockPosition))
            return null;
        return blocks[BlockPositionToBlockIndex(blockPosition)];
    }

    public void SetBlockFromBlockPosition(Vector2 blockPosition, Dictionary<String, object> newBlock)
    {
        if (!IsValidBlockPosition(blockPosition))
            return;

        blocks[BlockPositionToBlockIndex(blockPosition)] = newBlock;
        chunkImage.Lock();
        if ((int)newBlock["id"] == 0)
            chunkImage.SetPixelv(blockPosition, new Color(0, 0, 0, 0));
        else
            chunkImage.SetPixelv(blockPosition, (Color)newBlock["colour"]);
        chunkImage.Unlock();
        Update();
    }

    private void SetBlockColourFromInt(int x, int y, Color colour)
    {
        SetBlockColour(new Vector2(x, y), colour);
    }

    private void SetBlockColour(Vector2 blockPosition, Color colour)
    {
        Dictionary<String, object> block = GetBlockFromBlockPosition(blockPosition);
        if (block == null)
            return;
        
        block.Add("colour", colour);

        chunkImage.Lock();
        chunkImage.SetPixelv(blockPosition, colour);
        chunkImage.Unlock();
        Update();
    }

    public Vector2 GetChunkPosition()
    {
        return chunkPosition;
    }

    public override void _Draw()
    {
        // DrawCircle(Vector2.Zero, 2, Color.aquamarine);
        DrawChunk();
    }
}
