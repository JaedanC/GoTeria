using Godot;
using System.Collections.Generic;
using Priority_Queue;

/*
ThreadPool
A thread pool to asynchronously execute tasks
By Marcos Zolnowski

(Ported to C# by JaedanC)
*/

// TODO: Change this to user the futures implementation because stop reinventing the wheel.
public class ThreadPool : Node
{
    private bool singleThreaded;
    private bool discardFinishedTasks;

    private SimplePriorityQueue<Task, float> tasks;
    // private Array<Task> __tasks;
    private bool started;
    private bool finished;
    private Mutex tasksLock;
    private Semaphore tasksWait;
    private List<Task> finishedTasks;
    private Mutex finishedTasksLock;
    private List<Thread> pool;

    private enum ParameterType
    {
        None,
        Array,
        Object
    }

    public override void _Ready()
    {
        Name = "ThreadPool";

        // __tasks = new Array<Task>();
        started = false;
        finished = false;
        tasksLock = new Mutex();
        tasksWait = new Semaphore();
        finishedTasks = new List<Task>();
        finishedTasksLock = new Mutex();
    }

    public void Initialise(bool singleThreaded, bool discardFinishedTasks, int numThreads)
    {
        Developer.AssertGreaterThanEquals(numThreads, 1, "Number of threads must be at least one");
        this.singleThreaded = singleThreaded;
        this.discardFinishedTasks = discardFinishedTasks;
        tasks = new SimplePriorityQueue<Task, float>();
        pool = __CreatePool(numThreads);
    }

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationPredelete)
            __WaitForShutdown();
    }

    public new void QueueFree()
    {
        Shutdown();
        base.QueueFree();
    }

    public void SubmitTaskUnparameterized(object instance, string method, float priority, object taskTag = null, object taskTagSpecific = null)
    {
        __EnqueueTask(instance, method, null, null, taskTag, taskTagSpecific, priority, ParameterType.None);
    }

    public void SubmitTask(object instance, string method, object parameter, float priority, object taskTag = null, object taskTagSpecific = null)
    {
        __EnqueueTask(instance, method, parameter, null, taskTag, taskTagSpecific, priority, ParameterType.Object);
    }

    public void SubmitTaskArrayParameterized(object instance, string method, List<object> listParameter, float priority, object taskTag = null, object taskTagSpecific = null)
    {
        __EnqueueTask(instance, method, null, listParameter, taskTag, taskTagSpecific, priority, ParameterType.Array);
    }

    public void Shutdown()
    {
        finished = true;
        foreach (Thread i in pool)
        {
            tasksWait.Post();
        }
        tasksLock.Lock();
        tasks.Clear();
        tasksLock.Unlock();
    }

    public List<Task> FetchFinishedTasks()
    {
        finishedTasksLock.Lock();
        List<Task> result = finishedTasks;
        finishedTasks = new List<Task>();
        finishedTasksLock.Unlock();
        return result;
    }

    public List<Task> FetchFinishedTasksByTag(object tag)
    {
        finishedTasksLock.Lock();
        List<Task> result = new List<Task>();
        List<Task> newFinishedTasks = new List<Task>();
        for (int i = 0; i < finishedTasks.Count; i++)
        {
            Task task = finishedTasks[i];
            if (task.GetTag().Equals(tag))
                result.Add(task);
            else
                newFinishedTasks.Add(task);
        }
        finishedTasks = newFinishedTasks;
        finishedTasksLock.Unlock();
        return result;
    }

    public void DoNothing(object arg)
    {
        // GD.Print("doing nothing");
        OS.DelayMsec(1); // if there is nothing to do, go sleep
    }

    private void __EnqueueTask(object instance, string method, object parameter, List<object> arrayParameter,
                               object taskTag, object taskTagSpecific, float priority, ParameterType parameterType)
    {
        if (finished)
            return;
        tasksLock.Lock();
        switch (parameterType)
        {
            case ParameterType.None:
                tasks.Enqueue(new Task(instance, method, taskTag, taskTagSpecific), priority);
                break;
            case ParameterType.Object:
                tasks.Enqueue(new Task(instance, method, parameter, taskTag, taskTagSpecific), priority);
                break;
            case ParameterType.Array:
                tasks.Enqueue(new Task(instance, method, arrayParameter, taskTag, taskTagSpecific), priority);
                break;
            default:
                Developer.Fail();
                break;
        }
        // GD.Print("Tasks size:" + __tasks.Count);
        tasksWait.Post();
        __Start();
        tasksLock.Unlock();
    }

    private void __WaitForShutdown()
    {
        Shutdown();
        foreach (Thread t in pool)
        {
            if (t.IsActive())
                t.WaitToFinish();
        }
    }

    private List<Thread> __CreatePool(int numThreads)
    {
        GD.Print("ThreadPool(): Spawning " + numThreads + " threads.");
        List<Thread> threads = new List<Thread>();
        for (int i = 0; i < numThreads; i++)
        {
            threads.Add(new Thread());
        }
        return threads;
    }

    private void __Start()
    {
        if (singleThreaded)
        {
            Task task = __DrainTask();
            task.__ExecuteTask();
            if (!(task.TaskTag is Task))
                finishedTasks.Add(task);
        }
        else if (!started)
        {
            foreach (Thread t in pool)
            {
                t.Start(this, "__ExecuteTasks", t);
                started = true;
            }
        }
    }

    /* This method will block until it finds a task in __finishedTasks with a matching
    taskSpecific tag. */
    public void WaitForTaskSpecific(object taskSpecific)
    {
        if (discardFinishedTasks)
            Developer.Fail("Can't WaitForTaskSpecific if tasks are set to be discarded");
        //GD.Print("Force waiting for " + str(tag_specific) + " thread to finish");
        while (true)
        {
            finishedTasksLock.Lock();
            foreach (Task task in finishedTasks)
            {
                if (task.GetTagSpecific().Equals(taskSpecific))
                {
                    //GD.Print("Found");
                    finishedTasksLock.Unlock();
                    return;
                }
            }
            finishedTasksLock.Unlock();
            OS.DelayMsec(2);
        }
    }

    private Task __DrainTask()
    {
        tasksLock.Lock();
        Task result;
        if (tasks.Count == 0)
        {
            result = new Task(this, "DoNothing", null, null, null); // normally, this is not expected, but better safe than sorry
            result.TaskTag = result;
        }
        else
        {
            result = tasks.Dequeue();
        }
        tasksLock.Unlock();
        return result;
    }

    private void __ExecuteTasks(Thread argThread)
    {
        // GD.Print(arg_thread);
        while (!finished)
        {
            tasksWait.Wait();
            if (finished)
                return;
            Task task = __DrainTask();
            // GD.Print(task);
            task.__ExecuteTask();
            if (!(task.TaskTag is Task)) // # tasks tagged this way are considered hidden
            {
                if (discardFinishedTasks)
                {
                    // Signals unused.
                    //CallDeferred("emit_signal", "task_discarded", task);
                }
                else
                {
                    finishedTasksLock.Lock();
                    finishedTasks.Add(task);
                    finishedTasksLock.Unlock();
                    // Signals unused.
                    //CallDeferred("emit_signal", "task_finished", task.tag);
                }
            }
        }
    }
}
