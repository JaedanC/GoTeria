using Godot;
using System.Collections.Generic;

public class LightUpdateQueueSet
{
    private Queue<LightingEngine.LightUpdate> updateQueue;
    private HashSet<Vector2> updateSet;
    public int Count => updateQueue.Count;

    public LightUpdateQueueSet()
    {
        updateQueue = new Queue<LightingEngine.LightUpdate>();
        updateSet = new HashSet<Vector2>();
    }

    // public void Enqueue(LightingEngine.LightUpdate update)
    // {
    //     updateQueue.Enqueue(update);
    // }

    // public LightingEngine.LightUpdate Dequeue()
    // {
    //     LightingEngine.LightUpdate lightUpdate = updateQueue.Dequeue();
    //     return lightUpdate;
    // }

    public void Enqueue(LightingEngine.LightUpdate update)
    {
        if (updateSet.Contains(update.WorldPosition)) return;
        updateSet.Add(update.WorldPosition);
        updateQueue.Enqueue(update);
    }

    public LightingEngine.LightUpdate Dequeue()
    {
        LightingEngine.LightUpdate lightUpdate = updateQueue.Dequeue();
        updateSet.Remove(lightUpdate.WorldPosition);
        return lightUpdate;
    }
}
