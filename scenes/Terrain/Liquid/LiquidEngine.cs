using System;
using Godot;


public class LiquidEngine : Node
{
    private const byte MaxLiquidValue = 16;
    private readonly Terrain terrain;
    private readonly TerrainStack terrainStack;
    private readonly InputLayering inputLayering;
    private readonly PixelPassImage pixelPassImage;
    private readonly Random rng;

    public LiquidEngine(Terrain terrain, TerrainStack terrainStack, InputLayering inputLayering)
    {
        this.terrain = terrain;
        this.terrainStack = terrainStack;
        this.inputLayering = inputLayering;
        rng = new Random();
        pixelPassImage = new PixelPassImage(terrainStack.WorldLiquidsImage);
    }
    
    public void SimulationStep()
    {
        /* From https://gamedev.stackexchange.com/questions/58734/simulating-pressure-in-a-grid-based-liquid-simulation
         * Dwarf fortress (no water pressure though) http://www.bay12forums.com/smf/index.php?topic=32453.0
         *
         * for each liquid block in image do these steps
         *
         * 1. If the tile below has space in it, move as much as possible from the current tile to the bottom one
         * (Flow Down)
         * 2. If the 2 sides aren't the same, aren't zero and both are passable, we get the sum of the 3 tiles
         * (left + current + right) and divide it by 3 leaving the rest on the middle (current) tile
         *      From https://www.reddit.com/r/gamedev/comments/2048wv/help_with_cellular_automata_water/
         *      Case a) Water 2 6 2. Sum is (10), divide by three (3, remainder 1). Replace the water levels with the
         *              average: 3 3 3. The remainder is dealt with later.
         *      Case b) Water 2 7 2 -> 4 3 4. Remainder 2 is shared between the left and right.
         *      Case c) Water 2 6 2 -> 4 3 3 or 3 3 4. Remainder 1 is assigned randomly.
         * 3. If one side is open. Split the current tile in half for the 2 tiles. Water | 5 1 -> | 3 3.
         *      Case a) Water | 5 2 -> | 3 4 or | 4 3. Remainder 1 is assigned randomly.
         *
         * Consider rendering falling water as full
         */
        Image blocksImage = terrainStack.WorldBlocksImage;
        Image liquidImage = terrainStack.WorldLiquidsImage;
        Vector2 worldSize = terrain.GetWorldSize();
        for (int i = 0; i < worldSize.x; i++)
        for (int j = 0; j < worldSize.y; i++)
        {
            Vector2 currentPixelPosition = new Vector2(i, j);
            byte liquidLevel = GetLiquidLevel(liquidImage.GetPixelv(currentPixelPosition));
            if (liquidLevel == 0)
                continue;
            
            Vector2 belowPixelPosition = new Vector2(i, j + 1);
            Vector2 leftPixelPosition = new Vector2(i - 1, j);
            Vector2 rightPixelPosition = new Vector2(i + 1, j);
            bool belowTileExists = Helper.InBounds(belowPixelPosition, worldSize);
            bool leftTileExists = Helper.InBounds(leftPixelPosition, worldSize);
            bool rightTileExists = Helper.InBounds(rightPixelPosition, worldSize);
            bool belowTileIsSolid = false;
            bool leftTileIsSolid = false;
            bool rightTileIsSolid = false;
            byte belowTileLiquidLevel = 0;
            byte leftTileLiquidLevel = 0;
            byte rightTileLiquidLevel = 0;

            if (belowTileExists)
            {
                belowTileLiquidLevel = GetLiquidLevel(liquidImage.GetPixelv(belowPixelPosition));
                belowTileIsSolid = blocksImage.GetPixelv(belowPixelPosition).a != 0;
            }
            if (leftTileExists)
            {
                leftTileLiquidLevel = GetLiquidLevel(liquidImage.GetPixelv(leftPixelPosition));
                leftTileIsSolid = blocksImage.GetPixelv(leftPixelPosition).a != 0;
            }
            if (rightTileExists)
            {
                rightTileLiquidLevel = GetLiquidLevel(liquidImage.GetPixelv(rightPixelPosition));
                rightTileIsSolid = blocksImage.GetPixelv(rightPixelPosition).a != 0;
            }
            
            // 1. If the tile below has space in it, move as much as possible from the current tile to the bottom one
            // (Flow Down)
            // TODO: Map colours to fly-weighted blocks.
            if (belowTileExists && !belowTileIsSolid) // Exists && solid
            {
                byte maximumAmountToAddToBelow = (byte)(MaxLiquidValue - belowTileLiquidLevel);
                byte drainingAmount = (byte)Mathf.Min(liquidLevel, maximumAmountToAddToBelow);
                liquidLevel -= drainingAmount;
                belowTileLiquidLevel -= drainingAmount;
            }
            // 2. If the 2 sides aren't the same, aren't zero and both are passable, we get the sum of the 3 tiles
            // (left + current + right) and divide it by 3 leaving the rest on the middle (current) tile
            //      From https://www.reddit.com/r/gamedev/comments/2048wv/help_with_cellular_automata_water/
            //      Case a) Water 2 6 2. Sum is (10), divide by three (3, remainder 1). Replace the water levels with
            //              the average: 3 3 3. The remainder is dealt with later.
            //      Case b) Water 2 7 2 -> 4 3 4. Remainder 2 is shared between the left and right.
            //      Case c) Water 2 6 2 -> 4 3 3 or 3 3 4. Remainder 1 is assigned randomly.
            if (leftTileExists && !leftTileIsSolid && rightTileExists && !rightTileIsSolid)
            {
                byte tripleSum = (byte)(leftTileLiquidLevel + liquidLevel + rightTileLiquidLevel);
                byte eachBucket = (byte)(tripleSum / 3);
                byte remainder = (byte)(tripleSum % 3);
                liquidLevel = eachBucket;
                leftTileLiquidLevel = eachBucket;
                rightTileLiquidLevel = eachBucket;
                if (remainder == 2)
                {
                    leftTileLiquidLevel += 1;
                    rightTileLiquidLevel += 1;
                }
                if (remainder == 1)
                {
                    if (rng.Next(0, 1) == 0)
                    {
                        leftTileLiquidLevel += 1;
                    }
                    else
                    {
                        rightTileLiquidLevel += 1;
                    }
                }
            }
            // 3. If one side is open. Split the current tile in half for the 2 tiles. Water | 5 1 -> | 3 3.
            //     Case a) Water | 5 2 -> | 3 4 or | 4 3. Remainder 1 is assigned randomly.
            else if (leftTileExists && !leftTileIsSolid)
            {
                byte doubleSum = (byte)(leftTileLiquidLevel + liquidLevel);
                byte eachBucket = (byte)(doubleSum / 2);
                byte remainder = (byte)(doubleSum % 2);
                leftTileLiquidLevel = eachBucket;
                liquidLevel = eachBucket;
                if (remainder == 1)
                {
                    if (rng.Next(0, 1) == 0)
                    {
                        leftTileLiquidLevel += 1;
                    }
                    else
                    {
                        liquidLevel += 1;
                    }
                }
            }
            else if (rightTileExists && !rightTileIsSolid)
            {
                byte doubleSum = (byte)(liquidLevel + rightTileLiquidLevel);
                byte eachBucket = (byte)(doubleSum / 2);
                byte remainder = (byte)(doubleSum % 2);
                liquidLevel = eachBucket;
                rightTileLiquidLevel = eachBucket;
                if (remainder == 1)
                {
                    if (rng.Next(0, 1) == 0)
                    {
                        liquidLevel += 1;
                    }
                    else
                    {
                        rightTileLiquidLevel += 1;
                    }
                }
            }
            
            // Now add these changes to a data structure that can commit the changes later.
            pixelPassImage.AddChange(currentPixelPosition, liquidLevel);
            if (leftTileExists)
                pixelPassImage.AddChange(leftPixelPosition, leftTileLiquidLevel);
            if (rightTileExists)
                pixelPassImage.AddChange(rightPixelPosition, rightTileLiquidLevel);
            if (belowTileExists)
                pixelPassImage.AddChange(belowPixelPosition, belowTileLiquidLevel);
        }
        
        // Commit the changes here
        pixelPassImage.CommitChanges(MaxLiquidValue);
    }

    private byte GetLiquidLevel(Color colour)
    {
        return (byte)((int)colour.a / MaxLiquidValue);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (inputLayering.PollAction("liquid_steps") || inputLayering.PollActionPressed("liquid_step"))
        {
            SimulationStep();
        }
    }
}
