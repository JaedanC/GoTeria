using Godot;
using System;
using System.Diagnostics;

public class WorldFile
{
    private String WORLD_DIRECTORY = "res://saves/worlds/";
    private String BLOCKS_FILE = "blocks.png";
    private String WALLS_FILE = "walls.png";
    private String saveName = null;
    private ITerrainStack terrainStack = null;
    public WorldFile(String saveName)
    {
        LoadWorld(saveName);
    }

    private String GetSavePathForFile(String saveName, String filename)
    {
        return WORLD_DIRECTORY + saveName + "/" + filename;
    }

    public ITerrainStack GetITerrainStack()
    {
        Debug.Assert(terrainStack != null);
        return terrainStack;
    }

    public Error SaveWorld(String newSaveName)
    {
        // Creating the save directory if it doesn't exist
        Directory saveDirectory = new Directory();
        if (!saveDirectory.DirExists(WORLD_DIRECTORY + newSaveName))
        {
            Error directoryError = saveDirectory.MakeDir(WORLD_DIRECTORY + newSaveName);
            if (directoryError != Error.Ok)
            {
                GD.Print("SaveWorld(): Directory Error: " + directoryError);
                return directoryError;
            }
        }

        String blocksSavePath = GetSavePathForFile(newSaveName, BLOCKS_FILE);
        String wallsSavePath = GetSavePathForFile(newSaveName, WALLS_FILE);

        // Saving the blocks
        GD.Print("SaveWorld(): Saving: " + blocksSavePath);
        Error blockError = terrainStack.WorldBlocksImage.SavePng(blocksSavePath);
        if (blockError != Error.Ok)
        {
            GD.Print("SaveWorld(): Blocks Error: " + blockError);
            return blockError;
        }

        // Saving the walls
        GD.Print("SaveWorld(): Saving: " + wallsSavePath);
        Error wallError = terrainStack.WorldWallsImage.SavePng(wallsSavePath);
        if (wallError != Error.Ok)
        {
            GD.Print("SaveWorld(): Walls Error: " + wallError);
            return wallError;
        }

        GD.Print("SaveWorld(): Success");

        return Error.Ok;
    }
    public Error SaveWorld()
    {
        return SaveWorld(saveName);
    }

    public Error LoadWorld(String saveName)
    {
        String savePath = WORLD_DIRECTORY + saveName;
        GD.Print("LoadWorld(): Loading from path: " + savePath);

        Directory loadDirectory = new Directory();
        Error error = loadDirectory.Open(savePath);
        if (error != Error.Ok)
        {
            GD.Print("LoadWorld(): Opening directory error: " + error);
            return error;
        }

        String blocksPath = GetSavePathForFile(saveName, BLOCKS_FILE);
        String wallsPath = GetSavePathForFile(saveName, WALLS_FILE);

        if (!loadDirectory.FileExists(blocksPath))
        {
            GD.Print("LoadWorld(): Blocks path " + blocksPath + " does not exist");
            return Error.FileNotFound;
        }
        if (!loadDirectory.FileExists(wallsPath))
        {
            GD.Print("LoadWorld(): Walls path " + wallsPath + " does not exist");
            return Error.FileNotFound;
        }
    
        this.saveName = saveName;
        this.terrainStack = terrainStack = new TerrainStack(
            blocksPath,
            wallsPath
        );

        GD.Print("LoadWorld(): Success");
        return Error.Ok;
    }
}