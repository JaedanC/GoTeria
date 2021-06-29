using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/* This class represents a Task that is run on another thread. It contains all
the data required for this operation. */
// TODO: Have this use Tag, TagSpecific style Getters and Setters.
public class Task
{
    private object targetInstance;
    private String targetMethod;
    private object targetArgument;
    private List<object> targetArrayArgument;
    private String callbackMethod;
    private object result;
    private object tagSpecific;
    private bool noArgument;
    private bool arrayArgument;
    public object TaskTag;

    private Task(object instance, String method, String callbackMethod, object parameter, List<object> arrayParameter, object taskTag, object taskTagSpecific, bool noArgument, bool arrayArgument)
    {
        this.targetInstance = instance;
        this.targetMethod = method;
        this.callbackMethod = callbackMethod;
        this.targetArgument = parameter;
        this.targetArrayArgument = arrayParameter;
        this.tagSpecific = taskTagSpecific;
        this.TaskTag = taskTag;
        this.noArgument = noArgument;
        this.arrayArgument = arrayArgument;
        this.result = null;
    }

    public Task(object instance, String method, String callbackMethod, object taskTag, object taskTagSpecific)
        : this(instance, method, callbackMethod, null,      null,           taskTag, taskTagSpecific, true, false) { }

    public Task(object instance, String method, String callbackMethod, object parameter, object taskTag, object taskTagSpecific)
        : this(instance, method, callbackMethod, parameter, null,           taskTag, taskTagSpecific, false, false) { }

    public Task(object instance, String method, String callbackMethod, List<object> arrayParameter, object taskTag, object taskTagSpecific)
        : this(instance, method, callbackMethod, null,      arrayParameter,  taskTag, taskTagSpecific, false, true) { }

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
        StartTargetMethod();
        if (callbackMethod != null)
        {
            StartCallBackMethod();
        }
    }

    private void StartTargetMethod()
    {
        // The method must be public otherwise it will error.
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

    private void StartCallBackMethod()
    {
        Type type = targetInstance.GetType();
        System.Reflection.MethodInfo method = type.GetMethod(callbackMethod);
        if (method == null)
        {
            GD.Print(String.Format("Callback Method {0} was null. Is the method private or does it not exist?", callbackMethod));
        }
        method.Invoke(targetInstance, new object[] { result });
    }
}
