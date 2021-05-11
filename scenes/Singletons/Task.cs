using Godot;
using Godot.Collections;
using System;
using System.Linq;

public class Task : Node
{
    private object targetInstance;
    private String targetMethod;
    private object targetArgument;
    private Array<object> targetArrayArgument;
    private object result;
    private object tagSpecific;
    public object tag;
    private bool __noArgument;
    private bool __arrayArgument;

    public Task(object instance, String method, object parameter, Array<object> arrayParameter, object taskTag, object taskTagSpecific, bool noArgument, bool arrayArgument)
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

    public override void _Ready()
    {
        
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
            // result = targetInstance.Call(targetMethod);
            result = method.Invoke(targetInstance, new object[]{});
        }
        else if (__arrayArgument)
        {
            // result = targetInstance.Callv(targetMethod, targetArgument);
            // Array Argument as object.
            result = method.Invoke(targetInstance,  targetArrayArgument.ToArray());
        }
        else
        {
            // result = targetInstance.Call(targetMethod, targetArgument);
            result = method.Invoke(targetInstance, new object[]{targetArgument});
        }
    }
}