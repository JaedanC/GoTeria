using Godot;
using System.Collections.Generic;

public class LightUpdateColourQueueSet
{
    private readonly Queue<LightingEngine.LightUpdate> updateQueue;
    private readonly Dictionary<Vector2, Color> updateDictionary;
    public int Count => updateQueue.Count;

    public LightUpdateColourQueueSet()
    {
        updateQueue = new Queue<LightingEngine.LightUpdate>();
        updateDictionary = new Dictionary<Vector2, Color>();
    }

    // public void Enqueue(LightingEngine.LightUpdate lightUpdate)
    // {
    //     updateQueue.Enqueue(lightUpdate);
    // }

    // public LightingEngine.LightUpdate Dequeue()
    // {
    //     LightingEngine.LightUpdate lightUpdate = updateQueue.Dequeue();
    //     return lightUpdate;
    // }

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
