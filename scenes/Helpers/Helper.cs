using Godot;


public class Helper
{
    public static bool IsLight(Color colour)
    {
        return colour.a == Helper.Light.a; // TODO: Note there's a floating point operation here. Use a tolerance.
    }

    private static readonly Color Light = new Color(0, 0, 0, 0);

    public static bool InBounds(Vector2 position, Vector2 bounds)
    {
        return !OutOfBounds(position, bounds);
    }

    public static bool OutOfBounds(Vector2 position, Vector2 bounds)
    {
        return position.x < 0 || position.y < 0 || position.x >= bounds.x || position.y >= bounds.y;
    }

    public static Vector2 StringToVector2(string vector2AsString)
    {
        // eg. (0, 1)
        vector2AsString = vector2AsString.Replace("(", "");
        vector2AsString = vector2AsString.Replace(")", "");
        vector2AsString = vector2AsString.Replace(" ", "");
        string[] values = vector2AsString.Split(",");
        return new Vector2(
            float.Parse(values[0]),
            float.Parse(values[1])
        );
    }
}
