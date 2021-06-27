using Godot;
using System;


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

    public static Vector2 StringToVector2(String vector2AsString)
    {
        // eg. (0, 1)
        vector2AsString = vector2AsString.Replace("(", "");
        vector2AsString = vector2AsString.Replace(")", "");
        vector2AsString = vector2AsString.Replace(" ", "");
        String[] values = vector2AsString.Split(",");
        return new Vector2(
            float.Parse(values[0]),
            float.Parse(values[1])
        );
    }
}
