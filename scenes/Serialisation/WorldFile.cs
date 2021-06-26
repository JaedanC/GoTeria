using Godot;
using System;

public class WorldFile
{
    private String WORLD_DIRECTORY = "res://saves/worlds/";
    private String BLOCKS_FILE = "blocks.png";
    private String WALLS_FILE = "walls.png";
    private ITerrainStack terrainStack = null;
    public String SaveName;

    private static Mutex loadMutex = new Mutex();


    public WorldFile(String saveName)
    {
        LoadWorld(saveName);
    }

    private String GetSavePathForFile(String saveName, String fileName)
    {
        return WORLD_DIRECTORY + saveName + "/" + fileName;
    }

    public ITerrainStack GetITerrainStack()
    {
        Developer.AssertNotNull(terrainStack);
        return terrainStack;
    }

    public Error SaveWorld(String newSaveName)
    {
        Error blockError = SaveImage(terrainStack.WorldBlocksImage, newSaveName, BLOCKS_FILE);
        Error wallError = SaveImage(terrainStack.WorldWallsImage, newSaveName, WALLS_FILE);

        if (blockError != Error.Ok)
        {
            return blockError;
        }
        if (wallError != Error.Ok)
        {
            return wallError;
        }

        GD.Print("SaveWorld(): Success");
        return Error.Ok;
    }

    public Error SaveWorld()
    {
        return SaveWorld(SaveName);
    }

    public Error SaveImage(Image image, String newSaveName, String fileName)
    {
        String savePath = GetSavePathForFile(newSaveName, fileName);

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

        GD.Print("SaveWorld(): Saving: " + savePath);
        Error saveError = image.SavePng(savePath);
        if (saveError != Error.Ok)
        {
            GD.Print("SaveWorld(): Error: " + saveError);
            return saveError;
        }
        return Error.Ok;
    }

    public void LoadWorld(String saveName)
    {
        this.SaveName = saveName;
        Image worldBlocksImage = LoadImage(saveName, BLOCKS_FILE);
        Image worldWallsImage = LoadImage(saveName, WALLS_FILE);

        this.terrainStack = terrainStack = new TerrainStack(
            worldBlocksImage,
            worldWallsImage
        );

        GD.Print("LoadWorld(): Success");
    }

    public Image LoadImage(String newSaveName, String fileName)
    {
        loadMutex.Lock();

        String savePath = WORLD_DIRECTORY + SaveName;
        Directory loadDirectory = new Directory();
        Error error = loadDirectory.Open(savePath);
        if (error != Error.Ok)
        {
            GD.Print("LoadImage(): Opening directory error: " + error);
            loadMutex.Unlock();
            return null;
        }

        String imagePath = GetSavePathForFile(newSaveName, fileName);

        if (!loadDirectory.FileExists(imagePath))
        {
            GD.Print("LoadImage(): Image path " + imagePath + " does not exist");
            loadMutex.Unlock();
            return null;
        }

        GD.Print("LoadImage(): Loaded image: " + imagePath);
        Texture imageTexture = (Texture)GD.Load(imagePath);
        loadMutex.Unlock();
        return imageTexture.GetData();
    }
}
