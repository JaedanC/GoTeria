using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/* This class represents a Task that is run on another thread. It contains all
the data required for this operation. */
public class Task
{
    private object targetInstance;
    private String targetMethod;
    private object targetArgument;
    private List<object> targetArrayArgument;
    private object result;
    private object tagSpecific;
    private bool noArgument;
    private bool arrayArgument;
    public object TaskTag;

    private Task(object instance, String method, object parameter, List<object> arrayParameter, object taskTag, object taskTagSpecific, bool noArgument, bool arrayArgument)
    {
        this.targetInstance = instance;
        this.targetMethod = method;
        this.targetArgument = parameter;
        this.targetArrayArgument = arrayParameter;
        this.tagSpecific = taskTagSpecific;
        this.TaskTag = taskTag;
        this.noArgument = noArgument;
        this.arrayArgument = arrayArgument;
        this.result = null;
    }

    public Task(object instance, String method, object taskTag, object taskTagSpecific)
        : this(instance, method, null,      null,           taskTag, taskTagSpecific, true, false) { }

    public Task(object instance, String method, object parameter, object taskTag, object taskTagSpecific)
        : this(instance, method, parameter, null,           taskTag, taskTagSpecific, false, false) { }

    public Task(object instance, String method, List<object> arrayParameter, object taskTag, object taskTagSpecific)
        : this(instance, method, null,      arrayParameter, taskTag, taskTagSpecific, false, true) { }

    public object GetTag()
    {
        return TaskTag;
    }

    public object GetArgument()
    {
        return targetArgument;
    }

    public object GetResult()
    {
        return result;
    }

    public object GetTagSpecific()
    {
        return tagSpecific;
    }

    public void __ExecuteTask()
    {
        Type type = targetInstance.GetType();
        System.Reflection.MethodInfo method = type.GetMethod(targetMethod);
        if (method == null)
        {
            GD.Print(String.Format("Target Method {0} was null. Is the method private or does it not exist?", targetMethod));
        }

        if (noArgument)
        {
            result = method.Invoke(targetInstance, new object[] { });
        }
        else if (arrayArgument)
        {
            // Array Argument as object.
            result = method.Invoke(targetInstance, targetArrayArgument.ToArray());
        }
        else
        {
            result = method.Invoke(targetInstance, new object[] { targetArgument });
        }
    }
}
