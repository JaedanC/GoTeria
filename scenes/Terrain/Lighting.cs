using Godot;
using Godot.Collections;
using System;

public class Lighting : Node2D
{
    private InputLayering inputLayering;
    private Terrain terrain;
    private Player player;
	private Image lightLevelsImage;
    private Image lightSourceImage;
    private ImageTexture shaderTexture;

	private System.Threading.Thread lightingThread;
	private volatile bool doLighting = true;

    public override void _Ready()
    {
        inputLayering = GetNode<InputLayering>("/root/InputLayering");
        terrain = GetParent<Terrain>();
        player = GetNode<Player>("/root/WorldSpawn/Player");

        shaderTexture = new ImageTexture();
        lightSourceImage = new Image();
		lightingThread = new System.Threading.Thread(new System.Threading.ThreadStart(SpawnThread));

		
    }

    public void Emit(Image image, Vector2 imagePosition, Color colour)
    {
        if (colour.r <= 0.05)
            return;

        if (imagePosition.x < 0 || imagePosition.y < 0 ||
            imagePosition.x >= image.GetWidth() || imagePosition.y >= image.GetHeight())
            return;
        
        Color existingColour = image.GetPixelv(imagePosition);
        if (colour.r < existingColour.r)
            return;
        
		image.Lock();
        image.SetPixelv(imagePosition, colour);

		float multiplier = 0.7f;
        
        Color newColour = new Color(colour.r * multiplier, colour.g * multiplier, colour.b * multiplier, 1);
        Emit(image, new Vector2(imagePosition.x - 1, imagePosition.y), newColour);
        Emit(image, new Vector2(imagePosition.x, imagePosition.y - 1), newColour);
        Emit(image, new Vector2(imagePosition.x + 1, imagePosition.y), newColour);
        Emit(image, new Vector2(imagePosition.x, imagePosition.y + 1), newColour);
    }

    public void LightEmission()
    {
		lightLevelsImage = new Image();
		lightLevelsImage.CopyFrom(lightSourceImage);
        lightLevelsImage.Lock();
        for (int i = 0; i < lightLevelsImage.GetWidth(); i++)
        for (int j = 0; j < lightLevelsImage.GetHeight(); j++)
        {
            Vector2 imagePosition = new Vector2(i, j);
            Color colour = lightLevelsImage.GetPixelv(imagePosition);
            Emit(lightLevelsImage, imagePosition, colour);
        }
        lightLevelsImage.Unlock();
    }

	public void SpawnThread()
	{
		while (doLighting)
		{
			Array<Vector2> chunkPointCorners = player.GetVisibilityChunkPositionCorners();
			Vector2 chunkPointTopLeftInPixels = chunkPointCorners[0] * terrain.ChunkPixelDimensions;
			Vector2 chunkPointBottomRightInPixels = (chunkPointCorners[1] + Vector2.One) * terrain.ChunkPixelDimensions;
			
			// Calculate where to put the lighting
			Vector2 position = chunkPointTopLeftInPixels;
			Vector2 scale = chunkPointBottomRightInPixels - chunkPointTopLeftInPixels;

			// Calculate the lighting
			Vector2 topLeftBlock = chunkPointCorners[0] * terrain.ChunkBlockCount;
			Vector2 blocksOnScreen = scale / terrain.BlockPixelSize;
			lightSourceImage.Create((int)blocksOnScreen.x, (int)blocksOnScreen.y, false, Image.Format.Rgba8);
			lightSourceImage.BlitRect(terrain.WorldImageLuminance, new Rect2(topLeftBlock, blocksOnScreen), Vector2.Zero);
			LightEmission();

			// Set our position to be the new location before we render it to the screen so that
			// it is in the correct location.
			Position = position;
			Scale = scale;

			shaderTexture.CreateFromImage(lightLevelsImage);
			(Material as ShaderMaterial).SetShaderParam("light_values_size", shaderTexture.GetSize());
			(Material as ShaderMaterial).SetShaderParam("light_values", shaderTexture);
		}
	}

    public override void _PhysicsProcess(float delta)
    {
		if (lightingThread.ThreadState == System.Threading.ThreadState.Unstarted)
			lightingThread.Start();
    }

}

/*

Lighting algorithm:

The terrain has two light images:
1. light_sources = This contains information about light sources. 
2. light_levels = The second contains information about the light levels of the world. This image is computed using the
image above.

(They do not need to be the exact size of the world in blocks
in theory it could be larger to create a more detailed light surface. This should be possible.)

Light channels:
Each block in the world be effected by (4, 6, 8, 16) different lights. Each block stores an array of pointers to light_source's
that have touched this block. If a light source is added, when we compute the Recursive algorithm, attempt to add our
light intensity value to the block alongside the light itself.

class LightBlock {
	LightSource[8] light_affecting_me    # (or 4, 6, 8, 16 etc.)
}

class LightSourceFragment  {
	int intensity     # (Will change depending on the distance from the LightEntity potentially a short int)
	[@LightSource]    # (reference/pointer. Could be a torch or the sky)
}

if the array is full, iterate through the list and insert yourself in a slot if you are more intense. Note: This doesn't
work if all 8 of the light sources are removed. The tile will go dark but there may be sources around!

Dictionary<LightSource, Array<LightBlock>>

This terrain-wide dictionary stores an Array of LightBlocks that have been affected by a light. If the LightBlock is removed, we can
use the values of this Dictionary to find the LightBlocks we need to check if we need to remove ourselves. LightBlock
may opt to use a Dictionary instead. This decision is implementation specific.



When a chunk is loaded, image two is generated if it has not already been generated and copied onto the light_levels image.
If a light update occurs inside the world, push a light_update class instance to worker thread so that it can locally
recompute the light levels and copy it back to the light_levels image.

The light update class looks like the following

struct/class {
	Vector2 blockPosition
	Color lightColour
	int colour radius/intensity.
}

1. If a light is added then you can simply perform the recursive algorithm starting at the source

The worker thread then recomputes the light levels inside a box of size radius x2. This ensures that all the blocks that
may be effected by the light are recomputed. This allows for lights to flicker, without taxing the game heavily. Since this
operation should also be faster, retrieving a blocks transparency should also be possible during the BFS light calculation.
Furthermore, a different lighting algorithm could be applied. As long as you know the maximum radius for which a light could
change the world, you can limit the update rectangle to achieve this speed up.

Finally a blur filter could be applied to the light_source image to smooth out the edges of the lighting further.


*/