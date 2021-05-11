using Godot;
using Godot.Collections;
using System;

public class ThreadPool : Node
{
    [Export]
    private bool discardFinishedTasks = false;

    private Array<Task> __tasks;
    private bool __started;
    private bool __finished;
    private Mutex __tasksLock;
    private Semaphore __tasksWait;
    private Array<Task> __finishedTasks;
    private Mutex __finishedTasksLock;

    private Array<Thread> __pool;

    public override void _Ready()
    {
        __tasks = new Array<Task>();
        __started = false;
        __finished = false;
        __tasksLock = new Mutex();
        __tasksWait = new Semaphore();
        __finishedTasks = new Array<Task>();
        __finishedTasksLock = new Mutex();

        __pool = __CreatePool();
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

    public void SubmitTask(object instance, String method, object parameter, object taskTag=null, object taskTagSpecific=null)
    {
        __EnqueueTask(instance, method, parameter, null, taskTag, taskTagSpecific, false, false);
    }

    public void SubmitTaskUnparameterized(object instance, String method, object taskTag=null, object taskTagSpecific=null)
    {
        __EnqueueTask(instance, method, null, null, taskTag, taskTagSpecific, true, false);
    }

    public void SubmitTaskArrayParameterized(object instance, String method, Array<object> parameter, object taskTag=null, object taskTagSpecific=null)
    {
        __EnqueueTask(instance, method, null, parameter, taskTag, taskTagSpecific, false, true);
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
    
    public Array<Task> FetchFinishedTasks()
    {
        __finishedTasksLock.Lock();
        Array<Task> result = __finishedTasks;
        __finishedTasks = new Array<Task>();
        __finishedTasksLock.Unlock();
    	return result;
    }

    public Array<Task> FetchFinishedTasksByTag(object tag)
    {
        __finishedTasksLock.Lock();
        Array<Task> result = new Array<Task>();
        Array<Task> newFinishedTasks = new Array<Task>();
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

    private void DoNothing(object arg)
    {
	    // GD.Print("doing nothing");
        OS.DelayMsec(1); // if there is nothing to do, go sleep
    }

    private void __EnqueueTask(object instance, String method, object parameter, Array<object> arrayParameter, object taskTag, object taskTagSpecific, bool noArgument=false, bool arrayArgument=false)
    {
	    if (__finished)
            return;
        __tasksLock.Lock();
        // __tasks.push_front(Task.new(instance, method, parameter, task_tag, task_tag_specific, no_argument, array_argument))
        __tasks.Add(new Task(instance, method, parameter, arrayParameter, taskTag, taskTagSpecific, noArgument, arrayArgument));
        // GD.Print("Tasks size:" + str(__tasks.size()));
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

    private Array<Thread> __CreatePool()
    {
        Array<Thread> result = new Array<Thread>();
        for (int i = 0; i < OS.GetProcessorCount(); i++)
        {
		    result.Add(new Thread());
        }
        return result;
    }

    private void __Start()
    {
        if (!__started)
        {
            foreach (Thread t in __pool)
            {
                t.Start(this, "__ExecuteTasks", t);
                __started = true;
            }
        }
    }

    public void WaitForTaskSpecific(object taskSpecific)
    {
        //GD.Print("Force waiting for " + str(tag_specific) + " thread to finish");
        while (true)
        {
            foreach (Task task in __finishedTasks)
            {
                if (task.GetTagSpecific().Equals(taskSpecific))
                {
                    //GD.Print("Found");
                    return;
                }
            }
            OS.DelayMsec(1);
        }
    }
    
    private Task __DrainTask()
    {
        __tasksLock.Lock();
        Task result;
        if (__tasks.Count == 0)
        {
            result = new Task(this, "do_nothing", null, null, null, null, true, false); // normally, this is not expected, but better safe than sorry
            result.tag = result;
        }
        else
        {
            result = __tasks[0];
            __tasks.RemoveAt(0);
        }
        __tasksLock.Unlock();
        return result;
    }

    private void  __ExecuteTasks(Thread argThread)
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
            if (!(task.tag is Task)) // # tasks tagged this way are considered hidden
            {
                if (discardFinishedTasks)
                {
                    //
                    //CallDeferred("emit_signal", "task_discarded", task);
                }
                else
                {
                    __finishedTasksLock.Lock();
                    __finishedTasks.Add(task);
                    __finishedTasksLock.Unlock();
                    //CallDeferred("emit_signal", "task_finished", task.tag);
                }
            }
        }
    }

}
