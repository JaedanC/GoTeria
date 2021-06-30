using Godot;


/* This is a wrapper around Godot.File that automatically creates the folder the file needs
to exist in, loads from the res:// or user:// folders, and easily constructs the final file
path for reading and writing a file. */
public class TeriaFile
{
    private readonly string filePathDirectory;
    private readonly string filePath;
    private readonly string filePathPrefix;


    /* If userDirectory is true, then the final path will be prefixed with "user://". Otherwise,
    it will be prefixed with "res://". The filePath parameter should not include the prefix. The
    folder to the filePath does not need to exist yet. */
    public TeriaFile(bool userDirectory, string filePath)
    {
        this.filePath = filePath;
        if (userDirectory)
        {
            filePathPrefix = "user://";
        }
        else
        {
            filePathPrefix = "res://";
            GD.Print("TeriaFile() Open into " + filePathPrefix + ". This is the executable. ");
        }

        int lastSlash = filePath.FindLast("/");
        if (lastSlash != -1)
        {
            filePathDirectory = filePath.Substr(0, lastSlash);
        }
        else
            filePathDirectory = null;
    }

    /* Creates the Folder that this file will live in. */
    public Error CreateDirectoryForFile()
    {
        if (filePathDirectory == null)
        {
            return Error.Ok;
        }
        Directory directory = new Directory();
        Error error = directory.Open(filePathPrefix);
        if (error != Error.Ok)
        {
            GD.Print("TeriaFile.CreateDirectoryForFile() Open directory Error: " + error + ", FilePathPrefix: " + filePathPrefix);
            return error;
        }
        error = directory.MakeDirRecursive(filePathPrefix + filePathDirectory);
        if (error != Error.Ok)
        {
            GD.Print("TeriaFile.CreateDirectoryForFile() Make directory Error: " + error + ", Whole FilePath: " + filePathPrefix + filePathDirectory);
            return error;
        }
        return Error.Ok;
    }

    /* Returns a file open at this object's location. */
    public File GetFile(File.ModeFlags flags)
    {
        File file = new File();
        Error error = file.Open(filePathPrefix + filePath, flags);
        if (error == Error.Ok)
            return file;
        GD.Print("TeriaFile.GetFile() Opening file Error: " + error);
        return null;
    }

    public File ReadFile()
    {
        return GetFile(File.ModeFlags.Read);
    }

    /* Returns the final file path with the prefix so that you can save and load from
    this file. */
    public string GetFinalFilePath()
    {
        return filePathPrefix + filePath;
    }
}
