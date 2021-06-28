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
    private object result;
    private object tagSpecific;
    public object tag;
    private bool __noArgument;
    private bool __arrayArgument;

    public Task(object instance, String method, object parameter, List<object> arrayParameter, object taskTag, object taskTagSpecific, bool noArgument, bool arrayArgument)
    {
        targetInstance = instance;
        targetMethod = method;
        targetArgument = parameter;
        targetArrayArgument = arrayParameter;
        tagSpecific = taskTagSpecific;
        tag = taskTag;
        __noArgument = noArgument;
        __arrayArgument = arrayArgument;
        result = null;
    }

    public object GetTag()
    {
        return tag;
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
        // The method must be public otherwise it will error.
        Type type = targetInstance.GetType();
        System.Reflection.MethodInfo method = type.GetMethod(targetMethod);

        if (method == null)
        {
            GD.Print(String.Format("Method {0} was null. Is the method private or does it not exist?", targetMethod));
        }

        if (__noArgument)
        {
            result = method.Invoke(targetInstance, new object[] { });
        }
        else if (__arrayArgument)
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
