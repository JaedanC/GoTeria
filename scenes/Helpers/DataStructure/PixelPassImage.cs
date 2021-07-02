using System.Collections.Generic;
using System.Linq;
using Godot;

public class PixelPassImage
{
    private readonly Image image;
    private readonly Dictionary<Vector2, IList<byte>> imageChanges;
    
    public PixelPassImage(Image image)
    {
        this.image = image;
        imageChanges = new Dictionary<Vector2, IList<byte>>();
    }

    public void AddChange(Vector2 pixel, byte value)
    {
        if (!imageChanges.ContainsKey(pixel))
        {
            imageChanges[pixel] = new List<byte>();
        }
        imageChanges[pixel].Add(value);
    }

    public void CommitChanges(byte maximumByteValue)
    {
        foreach (Vector2 pixel in imageChanges.Keys)
        {
            int numberOfChanges = imageChanges[pixel].Count;
            byte nSum = 0;
            foreach (byte value in imageChanges[pixel])
            {
                nSum += value;
            }
            
            // Integer division is okay here
            float alphaValue = (nSum / numberOfChanges) / (float)maximumByteValue;
            Color existingColour = image.GetPixelv(pixel);
            existingColour.a = alphaValue;
            image.SetPixelv(pixel, existingColour);
        }
        imageChanges.Clear();
    }
}
