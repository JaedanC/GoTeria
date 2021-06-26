using Godot;


public class Helper
{
    public static bool IsLight(Color colour)
    {
        return colour.a == Helper.Light.a;
    }

    public readonly static Color Light = new Color(0, 0, 0, 0);

    public static bool InBounds(Vector2 position, Vector2 bounds)
    {
        return !OutOfBounds(position, bounds);
    }

    public static bool OutOfBounds(Vector2 position, Vector2 bounds)
    {
        return position.x < 0 || position.y < 0 || position.x >= bounds.x || position.y >= bounds.y;
    }
}
