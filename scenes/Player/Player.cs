using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class Player : Node2D
{   
    private Terrain terrain;
    private KinematicBody2D rigidbody;
	private Godot.Object smoothing;
	private Camera2D camera;
	private InputLayering inputLayering;

	private Vector2 velocity;

    public override void _Ready()
    {
		Name = "Player";

        terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        rigidbody = GetNode<KinematicBody2D>("Rigidbody");
        smoothing = GetNode<Godot.Object>("Smoothing");
        camera = GetNode<Camera2D>("Smoothing/Camera");
        inputLayering = GetNode<InputLayering>("/root/InputLayering");

		velocity = Vector2.Zero;
    }

	public override void _Process(float delta)
	{
		// The 'player' node does not actually change. Rather the rigidbody and the smoothing
		// sprite move.
		Debug.Assert(Position == Vector2.Zero, "Player position must stay at (0, 0).");

		if (inputLayering.PopAction("zoom_reset"))
			camera.Zoom = Vector2.Zero;
		
		if (inputLayering.PopAction("click"))
		{
			Vector2 worldPosition = ScreenToWorldPosition(GetViewport().GetMousePosition());
			Dictionary<String, object> block = terrain.GetBlockFromWorldPosition(worldPosition);
			if (block != null)
			{
				block["id"] = 1;
				// block["colour"] = new Color(GD.Randf(), GD.Randf(), GD.Randf(), 1);
				block["colour"] = new Color(1, 1, 0, 1);
			}
			terrain.SetBlockAtWorldPosition(worldPosition, block);
		}

		if (inputLayering.PopAction("dig"))
		{
			Vector2 worldPosition = ScreenToWorldPosition(GetViewport().GetMousePosition());
			Dictionary<String, object> block = terrain.GetBlockFromWorldPosition(worldPosition);
			if (block != null)
			{
				block["id"] = 0;
				block["colour"] = new Color(0, 0, 0, 0);
			}
			terrain.SetBlockAtWorldPosition(worldPosition, block);
		}

		Update();
	}

    public override void _PhysicsProcess(float delta)
    {
		if (inputLayering.PopAction("move_left"))
		{
			velocity.x -= 50;
		}
		if (inputLayering.PopAction("move_right"))
		{
			velocity.x += 50;
		}
		if (inputLayering.PopAction("move_up"))
		{
			velocity.y -= 50;
		}
		if (inputLayering.PopAction("move_down"))
		{
			velocity.y += 50;
		}
		if (inputLayering.PopAction("jump"))
		{
			velocity.y = -1000;
		}
		if (inputLayering.PopAction("brake"))
		{
			velocity = Vector2.Zero;
		}
		if (inputLayering.PollActionPressed("debug"))
		{
			GD.Print("Printing Stray Nodes:");
			PrintStrayNodes();
		}
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("zoom_in"))
			camera.Zoom += new Vector2(0.5f, 0.5f);

		if (@event.IsActionPressed("zoom_out"))
			camera.Zoom -= new Vector2(0.5f, 0.5f);

		if (@event.IsActionPressed("quit"))
			GetTree().Quit();
		
		camera.Zoom = new Vector2(
			Mathf.Clamp(camera.Zoom.x, 1, 10),
			Mathf.Clamp(camera.Zoom.y, 1, 10)
		);
    }

	/* This method returns an Array containing two Vector2's representing the world
	position of the player's visibility on the screen. This value is in pixels
	relative to the top-left of the world.

	For example. Window is 1920x1080, and the player is at [10000, 10000] in the
	world. This function would then return something like:
		[
			Vector2(9040, 9040),
			Vector2(10960, 10960
		]
	This is an over simplification because this function also takes into account the
	camera's zoom. The first Vector2 is the top-left visibility world position. The
	second Vector2 is the bottom-right visibility world position. */
	public Array<Vector2> GetVisibilityWorldPositionCorners()
	{
		// Grab important data
		Vector2 smoothedPosition = GetPlayerPosition();
		Rect2 viewportRectangle = GetViewportRect();
		
		// Using the viewport rectangle as a base, centre a copy of it around the player.
		// We used smoothed position because we want to take the interpolated position of
		// the player so visibility still works at high speeds.
		viewportRectangle.Position = smoothedPosition - viewportRectangle.Size / 2;
		
		// Expand this Rectangle to take into account camera zooming by using two
		// Vector2's the point the the corners of the screen and using the Rect2.expand
		// method.
		viewportRectangle = viewportRectangle.Expand(smoothedPosition - (GetViewportRect().Size / 2) * GetCameraZoom());
		viewportRectangle = viewportRectangle.Expand(smoothedPosition + (GetViewportRect().Size / 2) * GetCameraZoom());
		
		// Use this to temporarily reduce the size of the viewport loading rectangle
		// to watch the chunks be streamed in. 0 is no effect. 1 is no vision.
		// float viewportModifier = 0.4f;
		// float viewportModifier = 0.2f;
		float viewportModifier = 0f;
		Vector2 size = viewportRectangle.Size * new Vector2(viewportModifier, viewportModifier);
		viewportRectangle = viewportRectangle.GrowIndividual(-size.x, -size.y, -size.x, -size.y);
		
		// Convert the top left and bottom right points of the Rect2 into an integer
		// that we can loop through to get the points for the visible chunks. This
		// takes into account the size of each chunk.
		Vector2 topLeft = viewportRectangle.Position;
		Vector2 bottomRight = viewportRectangle.Position + viewportRectangle.Size;

		Array<Vector2> corners = new Array<Vector2>(topLeft, bottomRight);
		return corners;
	}
	
	/* This function is similar to GetVisibilityWorldPositionCorners() but this
	returns the two vectors divided by the block size. This then returns two
	Vector2's which represent the corners of block indices in the world that can be
	seen by the player. */
	public Array<Vector2> GetVisibilityWorldBlockPositionCorners()
	{
		Array<Vector2> corners = GetVisibilityWorldPositionCorners();
		Array<Vector2> worldBlockPositionCorners = new Array<Vector2>(
			(corners[0] / terrain.GetBlockPixelSize()).Floor(),
			(corners[1] / terrain.GetBlockPixelSize()).Floor()
		);
		return worldBlockPositionCorners;
	}

	/* This function is similar to GetVisibilityWorldPositionCorners but instead it
	returns the two Vector2's which represent the chunk indices that can be seen by
	the player. */
	public Array<Vector2> GetVisibilityChunkPositionCorners()
	{
		Array<Vector2> worldPositionCorners = GetVisibilityWorldPositionCorners();
		var chunkPositionTopLeft = (worldPositionCorners[0] / terrain.GetChunkPixelDimensions()).Floor();
		var chunkPositionBottomRight = (worldPositionCorners[1] / terrain.GetChunkPixelDimensions()).Floor();
		Array<Vector2> chunkPositionCorners = new Array<Vector2>(chunkPositionTopLeft, chunkPositionBottomRight);
		return chunkPositionCorners;
	}

	/* This function returns an Array of chunk positions that are contained by the
	method GetVisibilityChunkPositionCorners().
		- The margin parameter extends the radius of the visibility by the 'margin'
		chunks. This can be used to include chunks are slightly off screen. I guess the
		name of this parameter can be slightly misleading.
		- The border only parameter is only relevant if the margin parameter is
		used. If borderOnly is true, then only the chunk indices that the
		(non-zero) margin added are returned in the Array.

	Example Return:
	[
		Vector2(0, -1),
		Vector2(0, 0),
		Vector2(1, -1),
		Vector2(1, 0),
		Vector2(2, -1),
		Vector2(2, 0),
		...
	]
	*/
	public Array<Vector2> GetVisibilityChunkPositions(int margin=0, bool borderOnly=false, int borderIgnore=0)
	{
		Array<Vector2> chunkCorners = GetVisibilityChunkPositionCorners();
		
		// Now include the margin
		Vector2 topLeftMargin = chunkCorners[0] - Vector2.One * margin;
		Vector2 bottomRightMargin = chunkCorners[1] + Vector2.One * margin;
		
		Array<Vector2> visibilityPoints = new Array<Vector2>();

		// If borderOnly is true then we only add chunkPositions that are in the margin. If the margin is zero,
		// then this would return an empty Array... I hope.
		if (borderOnly)
		{
			for (int i = (int)topLeftMargin.x; i < (int)bottomRightMargin.x + 1; i++)
			for (int j = (int)topLeftMargin.y; j < (int)bottomRightMargin.y + 1; j++)
			{
				// borderIgnore reduces the radius of the border by borderIgnore ammount, from the inside.
				// It's like eating a donut but from the middle first. Don't do this btw.
				Vector2 ignoreTopLeft = chunkCorners[0] - Vector2.One * borderIgnore;
				Vector2 ignoreBottomRight = chunkCorners[1] + Vector2.One * borderIgnore;
				if (i < ignoreTopLeft.x || j < ignoreTopLeft.y || i > ignoreBottomRight.x || j > ignoreBottomRight.y)
					visibilityPoints.Add(new Vector2(i, j));
			}
		}
		else
		{
			for (int i = (int)topLeftMargin.x; i < (int)bottomRightMargin.x + 1; i++)
			for (int j = (int)topLeftMargin.y; j < (int)bottomRightMargin.y + 1; j++)
				visibilityPoints.Add(new Vector2(i, j));
		}
		return visibilityPoints;
	}

	/* Return the rigidbody position of the Player. This is the position you should use in physics
	calculations of the player. */
	public KinematicBody2D GetRigidbody()
	{
		return rigidbody;
	}

	/* Returns the velocity of the player. This Vector2 is added to the Player's position every
	physics frame. */
	public Vector2 GetVelocity()
	{
		return velocity;
	}

	/* Sets the velocity of the player.This Vector2 is added to the Player's position every
	physics frame. */
	public void SetVelocity(Vector2 velocity)
	{
		this.velocity = velocity;
	}

	/* This returns the player's position (which is actually the smoothing sprite's
	location) if you want the player's position on any frame other than a physics
	frame. This is an linear interpolated Vector2. */
	public Vector2 GetPlayerPosition()
	{
		return (Vector2)smoothing.Get("position");
	}

	/* This function returns the zoom factor of the player's camera. */
	public Vector2 GetCameraZoom()
	{
		return camera.Zoom;
	}

	/* This function converts a screen position to a world position.
		- A screen position is a location inside the window. For example, The location
		of the mouse is supplied to Godot as a screen position. */
	public Vector2 ScreenToWorldPosition(Vector2 screenPosition)
	{
		Vector2 worldPosition = GetPlayerPosition() + screenPosition * GetCameraZoom();
		return worldPosition - GetCameraZoom() * GetViewportRect().Size / 2;
	}

    public override void _Draw()
    {	
		// foreach (Vector2 chunkPoint in GetVisibilityChunkPositions())
		// {
		// 	DrawCircle(
		// 		chunkPoint * terrain.GetChunkPixelDimensions(),
		// 		15,
		// 		new Color(0, 1, 0, 1)
        // 	);
		// }

        DrawCircle(
            ScreenToWorldPosition(GetViewport().GetMousePosition()),
            5,
            new Color(1, 1, 0, 1)
        );

        DrawLine(
            ScreenToWorldPosition(GetViewport().Size/2),
            ScreenToWorldPosition(GetViewport().Size/2 + velocity / 10),
            new Color(1, 0, 0)
        );
    }
}