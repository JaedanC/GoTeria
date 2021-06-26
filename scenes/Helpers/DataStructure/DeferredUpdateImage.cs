using Godot;


/* An image that that can have its pixel's read live, but the image only updated when
the updates are commit. Intended for concurrent reading and writing to a Image. */
public class DefferedUpdateImage
{
    private Image image;
    private System.Collections.Generic.Dictionary<Vector2, Color> colourChanges;
    private Mutex imageMutex;

    public DefferedUpdateImage(Image image)
    {
        this.image = image;
        Developer.AssertGreaterThan(image.GetWidth(), 0, "Image size.x is 0");
        Developer.AssertGreaterThan(image.GetHeight(), 0, "Image size.y is 0");
        colourChanges = new System.Collections.Generic.Dictionary<Vector2, Color>();
        imageMutex = new Mutex();
    }

    /* Acts exactly like Image.SetPixelv() */
    public void SetPixelv(Vector2 pixel, Color newColour)
    {
        imageMutex.Lock();
        colourChanges[pixel] = newColour;
        imageMutex.Unlock();
    }

    /* Acts exactly like Image.GetPixelv() */
    public Color GetPixelv(Vector2 pixel)
    {
        imageMutex.Lock();
        Color colour;
        if (colourChanges.ContainsKey(pixel))
            colour = colourChanges[pixel];
        else
            colour = image.GetPixelv(pixel);
        imageMutex.Unlock();
        return colour;
    }

    /* Call this before you are about to call GetImage() so that
    no concurrent updates can work on the image. */
    public void LockImage()
    {
        imageMutex.Lock();
    }

    /* Make sure you this.Lock() and this.Unlock() this resource to ensure the
    image returned is not modified concurrently in the meantime. */
    public Image GetImage()
    {
        return image;
    }

    /* Unlock the image to allow further modification of the image. */
    public void UnlockImage()
    {
        imageMutex.Unlock();
    }

    /* Any calls to SetPixelv() will now be reflected in the image
    retrieved with GetImage() now.*/
    public void CommitColourChangesToImage()
    {
        imageMutex.Lock();
        foreach (Vector2 pixel in colourChanges.Keys)
        {
            image.SetPixelv(pixel, colourChanges[pixel]);
        }
        colourChanges.Clear();
        imageMutex.Unlock();
    }
}
