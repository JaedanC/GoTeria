using Godot;
using System;


/* A WorldFile allows access to the images that make up the terrain. */
public class WorldFile
{
    private ITerrainStack terrainStack;
    private TeriaFile blockFile;
    private TeriaFile wallFile;
    private static Mutex loadMutex = new Mutex();


    public WorldFile(TeriaFile blockFile, TeriaFile wallFile)
    {
        this.blockFile = blockFile;
        this.wallFile = wallFile;

        Image blockImage = LoadImage(blockFile);
        Image wallImage = LoadImage(wallFile);
        Developer.AssertNotNull(blockImage, "Block image was null");
        Developer.AssertNotNull(wallImage, "Wall image was null");

        this.terrainStack = new TerrainStack(blockImage, wallImage);
    }

    /* Retrieve the images in the ITerrainStack.  */
    public ITerrainStack GetITerrainStack()
    {
        Developer.AssertNotNull(terrainStack);
        return terrainStack;
    }

    /* Save the blocks and walls to the specifed TeriaFiles. If moveToUseTeriaFiles is true
    then the new blockFile and wallFile will overwrite the current ones. */
    public Error SaveWorld(TeriaFile blockFile, TeriaFile wallFile, bool moveToUseTeriaFiles)
    {
        if (moveToUseTeriaFiles)
        {
            this.blockFile = blockFile;
            this.wallFile = wallFile;
        }

        Error error = SaveImage(terrainStack.WorldBlocksImage, blockFile);
        if (error != Error.Ok)
        {
            GD.Print("WorldFile.SaveWorld() Saving block Error: " + error);
            return error;
        }

        error = SaveImage(terrainStack.WorldWallsImage, wallFile);
        if (error != Error.Ok)
        {
            GD.Print("WorldFile.SaveWorld() Saving block Error: " + error);
            return error;
        }

        GD.Print("WorldFile.SaveWorld(): Success");
        return Error.Ok;
    }

    /* Save to the default location (Where the images were originally loaded from). */
    public Error SaveWorld()
    {
        return SaveWorld(blockFile, wallFile, true);
    }

    /* Save an arbritray image to a location. */
    public static Error SaveImage(Image image, TeriaFile saveLocation)
    {
        // Creating the save directory if it doesn't exist
        saveLocation.CreateDirectoryForFile();

        String savePath = saveLocation.GetFinalFilePath();
        GD.Print("WorldFile.SaveImage(): Saving image: " + savePath);
        Error error = image.SavePng(savePath);
        if (error != Error.Ok)
        {
            GD.Print("WorldFile.SaveImage(): Saving PNG Error: " + error);
            return error;
        }
        return Error.Ok;
    }

    /* Load an arbritray image from a location. */
    public static Image LoadImage(TeriaFile teriaFileToImage)
    {
        loadMutex.Lock();
        teriaFileToImage.CreateDirectoryForFile();

        // https://www.reddit.com/r/godot/comments/eojihj/how_to_load_images_without_importer/
        String imagePath = teriaFileToImage.GetFinalFilePath();
        Image image = new Image();
        File imageFile = teriaFileToImage.ReadFile();
        byte[] binaryImageContents = imageFile.GetBuffer((int)imageFile.GetLen());
        imageFile.Close();

        // Get the file's extension
        Error error;
        if (imagePath.Extension().Equals("png"))
        {
            error = image.LoadPngFromBuffer(binaryImageContents);
        }
        else if (imagePath.Extension().Equals("jpg"))
        {
            error = image.LoadJpgFromBuffer(binaryImageContents);
        }
        else
        {
            GD.Print("WorldFile.LoadImage(): Unknown file extension: " + imagePath.Extension());
            Developer.Fail();
            loadMutex.Unlock();
            return null;
        }

        if (error != Error.Ok)
        {
            GD.Print("WorldFile.LoadImage(): Load image binary from buffer Error: " + error);
            Developer.Fail();
            loadMutex.Unlock();
            return null;
        }
        loadMutex.Unlock();

        GD.Print("WorldFile.LoadImage(): Loaded image: " + imagePath);
        return image;
    }
}
