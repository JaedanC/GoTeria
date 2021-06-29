using System;


public class Developer
{
    public class AssertionException : Exception
    {
        public AssertionException() { }

        public AssertionException(string message)
            : base(message) { }

        public AssertionException(string message, Exception inner)
            : base(message, inner) { }
    }


    private static void PrintStackTrace(String format, params object[] arguments)
    {
        // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.stacktrace?view=net-5.0
        // Thankfully this isn't really required because Godot gives a better trace than StackTrace()
        
        // StackTrace currentStack = new StackTrace();
        Console.WriteLine("Assertion Failed");
        Console.WriteLine(format, arguments);
        // for (int i = 1; i < currentStack.FrameCount; i++)
        // {
        //     StackFrame sf = currentStack.GetFrame(i);
        //     Console.WriteLine(sf.GetMethod());
        // }
    }

    public static void Fail(String message="")
    {
        PrintStackTrace("Fail");
        throw new AssertionException(message);
    }

    public static void AssertEquals(object expected, object actual, String message="")
    {
        if (actual.Equals(expected))
            return;
        
        PrintStackTrace("Expected: {0}\nActual:   {1}", expected, actual);
        throw new AssertionException(message);
    }

    public static void AssertTrue(bool value, String message="")
    {
        if (value)
            return;
        
        PrintStackTrace("Expected: true\nActual: {0}", value);
        throw new AssertionException(message);
    }

    public static void AssertFalse(bool value, String message="")
    {
        if (!value)
            return;
        
        PrintStackTrace("Expected: {0} == false", value);
        throw new AssertionException(message);
    }

    public static void AssertNotNull(object value, String message="")
    {
        if (value != null)
            return;
        
        PrintStackTrace("Expected: {0} != null", value);
        throw new AssertionException(message);
    }

    public static void AssertNull(object value, String message="")
    {
        if (value == null)
            return;
        
        PrintStackTrace("Expected: {0} == null", value);
        throw new AssertionException(message);
    }

    public static void AssertLessThan(float value, float lessThan, String message="")
    {
        if (value < lessThan)
            return;
        
        PrintStackTrace("Expected: {0} < {1}", value, lessThan);
        throw new AssertionException(message);
    }

    public static void AssertLessThanEquals(float value, float lessThanEquals, String message="")
    {
        if (value <= lessThanEquals)
            return;
        
        PrintStackTrace("Expected: {0} <= {1}", value, lessThanEquals);
        throw new AssertionException(message);
    }

    public static void AssertGreaterThan(float value, float greaterThan, String message="")
    {
        if (value > greaterThan)
            return;
        
        PrintStackTrace("Expected: {0} > {1}", value, greaterThan);
        throw new AssertionException(message);
    }

    public static void AssertGreaterThanEquals(float value, float greaterThanEquals, String message="")
    {
        if (value >= greaterThanEquals)
            return;
        
        PrintStackTrace("Expected: {0} >= {1}", value, greaterThanEquals);
        throw new AssertionException(message);
    }
}
