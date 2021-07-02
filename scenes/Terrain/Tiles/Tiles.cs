using System.Collections.Generic;
using Godot;


/*
 * TODO: Use an array with indices in the future as it will be much faster than using dictionaries.
 */
public class Tiles
{
    public enum Blocks
    {
        Air,
        Default,
        Dirt,
        Sand,
        Stone,
    }

    public enum Walls
    {
        Air,
        Default,
        Dirt,
        Stone,
        Mud,
    }

    public enum Liquids
    {
        Air,
        Default,
        Water,
        Lava,
        Honey,
    }

    private static Tiles tilesInstance;
    private static Tiles Instance => tilesInstance ?? (tilesInstance = new Tiles());

    private Dictionary<Blocks, Block> blocksCatalogue;
    private Dictionary<Walls, Wall> wallsCatalogue;
    private Dictionary<Liquids, Liquid> liquidsCatalogue;
    
    private Dictionary<Color, Block> blocksColourMap;
    private Dictionary<Color, Wall> wallsColourMap;
    private Dictionary<Color, Liquid> liquidsColourMap;

    private Tiles()
    {
        SetupTiles();
    }

    private void SetupTiles()
    {
        blocksColourMap = new Dictionary<Color, Block>();
        wallsColourMap = new Dictionary<Color, Wall>();
        liquidsColourMap = new Dictionary<Color, Liquid>();
        
        blocksCatalogue = new Dictionary<Blocks, Block>
        {
            [Blocks.Air] = new Block("Air", Helper.EmptyColour, false),
            [Blocks.Default] = new Block("Unknown block", Color.Color8(255, 0, 0), true),
            [Blocks.Dirt] = new Block("Dirt", Color.Color8(151, 107, 75), true),
            [Blocks.Sand] = new Block("Sand", Color.Color8(255, 218, 56), true),
            [Blocks.Stone] = new Block("Stone", Color.Color8(128, 128, 128), true)
        };

        wallsCatalogue = new Dictionary<Walls, Wall>
        {
            [Walls.Air] = new Wall("Air", Helper.EmptyColour),
            [Walls.Default] = new Wall("Unknown wall", Color.Color8(0, 255, 0)),
            [Walls.Dirt] = new Wall("Dirt", Color.Color8(88, 61, 46)),
            [Walls.Stone] = new Wall("Stone", Color.Color8(85, 102, 103)),
            [Walls.Mud] = new Wall("Mud", Color.Color8(52, 43, 45))
        };

        liquidsCatalogue = new Dictionary<Liquids, Liquid>
        {
            [Liquids.Air] = new Liquid("Air", Helper.EmptyColour),
            [Liquids.Default] = new Liquid("Unknown liquid", Color.Color8(0, 0, 255)),
            [Liquids.Water] = new Liquid("Water", Color.Color8(0, 12, 255)),
            [Liquids.Lava] = new Liquid("Lava", Color.Color8(255, 30, 0)),
            [Liquids.Honey] = new Liquid("Honey", Color.Color8(255, 172, 0))
        };
        
        foreach (Blocks blockKey in blocksCatalogue.Keys)
        {
            Block block = blocksCatalogue[blockKey];
            blocksColourMap[block.Colour] = block;
        }
        
        foreach (Walls blockKey in wallsCatalogue.Keys)
        {
            Wall wall = wallsCatalogue[blockKey];
            wallsColourMap[wall.Colour] = wall;
        }
        
        foreach (Liquids blockKey in liquidsCatalogue.Keys)
        {
            Liquid liquid = liquidsCatalogue[blockKey];
            liquidsColourMap[liquid.Colour] = liquid;
        }
    }

    public static Block GetBlockInstance(Blocks block)
    {
        Tiles instance = Instance;
        return instance.blocksCatalogue[block];
    }
    
    public static Wall GetWallInstance(Walls wall)
    {
        Tiles instance = Instance;
        return instance.wallsCatalogue[wall];
    }
    
    public static Liquid GetLiquidInstance(Liquids liquid)
    {
        Tiles instance = Instance;
        return instance.liquidsCatalogue[liquid];
    }
    
    public static Block GetBlock(Color colour)
    {
        Tiles instance = Instance;
        return instance.blocksColourMap.ContainsKey(colour)
            ? instance.blocksColourMap[colour]
            // : null;
            : GetBlockInstance(Tiles.Blocks.Default);
            // : new Block("Temp block", colour, !Helper.EmptyColour.Equals(colour));
    }
    
    public static Wall GetWall(Color colour)
    {
        Tiles instance = Instance;
        return instance.wallsColourMap.ContainsKey(colour)
            ? instance.wallsColourMap[colour]
            // : null;
            : GetWallInstance(Tiles.Walls.Default);
            // : new Wall("Temp wall", colour);
    }
    
    public static Liquid GetLiquid(Color colour)
    {
        Tiles instance = Instance;
        return instance.liquidsColourMap.ContainsKey(colour)
            ? instance.liquidsColourMap[colour]
            // : null;
            : GetLiquidInstance(Tiles.Liquids.Default);
            // : new Liquid("Temp liquid", colour);
    }
}
