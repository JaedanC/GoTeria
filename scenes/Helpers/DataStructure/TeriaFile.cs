using Godot;
using System;


public class TeriaFile
{
    private bool userDirectory;
    private String filePathDirectory;
    private String filePath;
    private String filePathPrefix;


    public TeriaFile(bool userDirectory, String filePath)
    {
        this.userDirectory = userDirectory;
        this.filePath = filePath;
        if (userDirectory)
        {
            this.filePathPrefix = "user://";
        }
        else
        {
            this.filePathPrefix = "res://";
            GD.Print("TeriaFile() Open into " + filePathPrefix + ". This is the executable. ");
        }

        int lastSlash = filePath.FindLast("/");
        if (lastSlash != -1)
        {
            this.filePathDirectory = filePath.Substr(0, lastSlash);
        }
        else
            this.filePathDirectory = null;
    }

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
            GD.Print("CreateDirectoryForFile() Open directory Error: " + error + ", FilePathPrefix: " + filePathPrefix);
            return error;
        }
        error = directory.MakeDirRecursive(filePathPrefix + filePathDirectory);
        if (error != Error.Ok)
        {
            GD.Print("CreateDirectoryForFile() Make directory Error: " + error + ", Whole FilePath: " + filePathPrefix + filePathDirectory);
            return error;
        }
        return Error.Ok;
    }

    /* Returns a file open to the location. */
    public File GetFile(File.ModeFlags flags)
    {
        File file = new File();
        file.Open(filePathPrefix + filePath, flags);
        return file;
    }

    /* Returns the file path so you can save to the file */
    public String GetFinalFilePath()
    {
        return filePathPrefix + filePath;
    }
}
