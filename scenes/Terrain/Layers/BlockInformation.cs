using Godot;

public class BlockInformation
{
    private readonly string name;
    private readonly Color readableColour;
    private Color saveableColour;
    // public Sprite sprite;

    public BlockInformation(string name, byte red, byte green, byte blue)
    {
        this.name = name;
        this.readableColour = Color.Color8(red, green, blue);
    }

    public void SetSaveableColour(Color saveableColour)
    {
        this.saveableColour = saveableColour;
    }
}
