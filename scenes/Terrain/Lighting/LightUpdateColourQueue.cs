using Godot;
using System.Collections.Generic;

public class LightUpdateColourQueue
{
    private Queue<LightingEngine.LightUpdate> updateQueue;
    private System.Collections.Generic.Dictionary<Vector2, Color> updateDictionary;
    public int Count { get { return updateQueue.Count; } }

    public LightUpdateColourQueue()
    {
        updateQueue = new Queue<LightingEngine.LightUpdate>();
        updateDictionary = new Dictionary<Vector2, Color>();
    }

    public void Enqueue(LightingEngine.LightUpdate lightUpdate)
    {
        Vector2 worldBlockPosition = lightUpdate.WorldPosition;
        if (updateDictionary.ContainsKey(worldBlockPosition) && 
            updateDictionary[worldBlockPosition].r >= lightUpdate.Colour.r)
            return;

        updateDictionary[worldBlockPosition] = lightUpdate.Colour;
        updateQueue.Enqueue(lightUpdate);
    }

    public LightingEngine.LightUpdate Dequeue()
    {
        LightingEngine.LightUpdate lightUpdate = updateQueue.Dequeue();
        updateDictionary.Remove(lightUpdate.WorldPosition);
        return lightUpdate;
    }
}