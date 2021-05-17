public class TerrainLayers {
    public BlockList blocks;
    public BlockList walls;
    // public BlockList wires;

    public TerrainLayers()
    {
        blocks = new BlockList();

        blocks.AddBlock(new BlockMapping("Dirt", 151, 107, 75));
        blocks.AddBlock(new BlockMapping("Sand", 255, 218, 56));
        blocks.AddBlock(new BlockMapping("Stone", 128, 128, 128));
    }
}