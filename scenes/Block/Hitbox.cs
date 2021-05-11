using Godot;
using System;

public class Hitbox : CollisionShape2D
{
    public override void _Ready()
    {
        RectangleShape2D shape = (RectangleShape2D)Shape;
        Terrain terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        shape.Extents = terrain.GetBlockPixelSize() / 2;
    }
}
