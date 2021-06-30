using Godot;
using System;
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

    private SimplePriorityQueue<Task, float> __tasks;
    // private Array<Task> __tasks;
    private bool __started;
    private bool __finished;
    private Mutex __tasksLock;
    private Semaphore __tasksWait;
    private List<Task> __finishedTasks;
    private Mutex __finishedTasksLock;
    private List<Thread> __pool;

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
        __started = false;
        __finished = false;
        __tasksLock = new Mutex();
        __tasksWait = new Semaphore();
        __finishedTasks = new List<Task>();
        __finishedTasksLock = new Mutex();
    }

    public void Initialise(bool singleThreaded, bool discardFinishedTasks, int numThreads)
    {
        Developer.AssertGreaterThanEquals(numThreads, 1, "Number of threads must be at least one");
        this.singleThreaded = singleThreaded;
        this.discardFinishedTasks = discardFinishedTasks;
        __tasks = new SimplePriorityQueue<Task, float>();
        __pool = __CreatePool(numThreads);
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

    public void SubmitTaskUnparameterized(object instance, String method, float priority, object taskTag = null, object taskTagSpecific = null)
    {
        __EnqueueTask(instance, method, null, null, taskTag, taskTagSpecific, priority, ParameterType.None);
    }

    public void SubmitTask(object instance, String method, object parameter, float priority, object taskTag = null, object taskTagSpecific = null)
    {
        __EnqueueTask(instance, method, parameter, null, taskTag, taskTagSpecific, priority, ParameterType.Object);
    }

    public void SubmitTaskArrayParameterized(object instance, String method, List<object> listParameter, float priority, object taskTag = null, object taskTagSpecific = null)
    {
        __EnqueueTask(instance, method, null, listParameter, taskTag, taskTagSpecific, priority, ParameterType.Array);
    }

    public void Shutdown()
    {
        __finished = true;
        foreach (Thread i in __pool)
        {
            __tasksWait.Post();
        }
        __tasksLock.Lock();
        __tasks.Clear();
        __tasksLock.Unlock();
    }

    public List<Task> FetchFinishedTasks()
    {
        __finishedTasksLock.Lock();
        List<Task> result = __finishedTasks;
        __finishedTasks = new List<Task>();
        __finishedTasksLock.Unlock();
        return result;
    }

    public List<Task> FetchFinishedTasksByTag(object tag)
    {
        __finishedTasksLock.Lock();
        List<Task> result = new List<Task>();
        List<Task> newFinishedTasks = new List<Task>();
        for (int i = 0; i < __finishedTasks.Count; i++)
        {
            Task task = __finishedTasks[i];
            if (task.GetTag().Equals(tag))
                result.Add(task);
            else
                newFinishedTasks.Add(task);
        }
        __finishedTasks = newFinishedTasks;
        __finishedTasksLock.Unlock();
        return result;
    }

    public void DoNothing(object arg)
    {
        // GD.Print("doing nothing");
        OS.DelayMsec(1); // if there is nothing to do, go sleep
    }

    private void __EnqueueTask(object instance, String method, object parameter, List<object> arrayParameter,
                               object taskTag, object taskTagSpecific, float priority, ParameterType parameterType)
    {
        if (__finished)
            return;
        __tasksLock.Lock();
        switch (parameterType)
        {
            case ParameterType.None:
                __tasks.Enqueue(new Task(instance, method, taskTag, taskTagSpecific), priority);
                break;
            case ParameterType.Object:
                __tasks.Enqueue(new Task(instance, method, parameter, taskTag, taskTagSpecific), priority);
                break;
            case ParameterType.Array:
                __tasks.Enqueue(new Task(instance, method, arrayParameter, taskTag, taskTagSpecific), priority);
                break;
            default:
                Developer.Fail();
                break;
        }
        GD.Print("Tasks size:" + __tasks.Count);
        __tasksWait.Post();
        __Start();
        __tasksLock.Unlock();
    }

    private void __WaitForShutdown()
    {
        Shutdown();
        foreach (Thread t in __pool)
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
                __finishedTasks.Add(task);
        }
        else if (!__started)
        {
            foreach (Thread t in __pool)
            {
                t.Start(this, "__ExecuteTasks", t);
                __started = true;
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
            __finishedTasksLock.Lock();
            foreach (Task task in __finishedTasks)
            {
                if (task.GetTagSpecific().Equals(taskSpecific))
                {
                    //GD.Print("Found");
                    __finishedTasksLock.Unlock();
                    return;
                }
            }
            __finishedTasksLock.Unlock();
            OS.DelayMsec(2);
        }
    }

    private Task __DrainTask()
    {
        __tasksLock.Lock();
        Task result;
        if (__tasks.Count == 0)
        {
            result = new Task(this, "DoNothing", null, null, null); // normally, this is not expected, but better safe than sorry
            result.TaskTag = result;
        }
        else
        {
            result = __tasks.Dequeue();
        }
        __tasksLock.Unlock();
        return result;
    }

    private void __ExecuteTasks(Thread argThread)
    {
        // GD.Print(arg_thread);
        while (!__finished)
        {
            __tasksWait.Wait();
            if (__finished)
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
                    __finishedTasksLock.Lock();
                    __finishedTasks.Add(task);
                    __finishedTasksLock.Unlock();
                    // Signals unused.
                    //CallDeferred("emit_signal", "task_finished", task.tag);
                }
            }
        }
    }
}
