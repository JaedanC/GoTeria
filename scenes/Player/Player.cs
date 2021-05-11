using Godot;
using Godot.Collections;
using System;

public class Player : Node2D
{   
    private Terrain terrain;
    private KinematicBody2D rigidbody;
	private Godot.Object smoothing;
	private Camera2D camera;
	private Godot.Object inputLayering;

	private Vector2 velocity;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
		Name = "Player";

        terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        rigidbody = GetNode<KinematicBody2D>("Rigidbody");
        smoothing = GetNode<Godot.Object>("Smoothing");
        camera = GetNode<Camera2D>("Smoothing/Camera");
		inputLayering = GetNode<Godot.Object>("/root/InputLayering");

		velocity = Vector2.Zero;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(float delta)
	{
		if (!Position.Equals(Vector2.Zero))
		{
			throw new Exception("Player position must stay at (0, 0).");
		}

		if ((bool)inputLayering.Call("pop_action", "zoom_reset"))
			camera.Set("zoom", Vector2.One);
		
		if ((bool)inputLayering.Call("pop_action", "click"))
		{
			Vector2 worldPosition = ScreenToWorldPosition(GetViewport().GetMousePosition());
			Dictionary<String, object> block = terrain.GetBlockFromWorldPosition(worldPosition);
			if (block != null)
			{
				block["id"] = 1;
				// block["colour"] = new Color(GD.Randf(), GD.Randf(), GD.Randf(), 1);
				block["colour"] = new Color(1, 1, 0, 1);
				// TODO: This is likely not required as it is returning a reference.
				terrain.SetBlockAtWorldPosition(worldPosition, block);
			}
		}

		if ((bool)inputLayering.Call("pop_action", "dig"))
		{
			Vector2 worldPosition = ScreenToWorldPosition(GetViewport().GetMousePosition());
			Dictionary<String, object> block = terrain.GetBlockFromWorldPosition(worldPosition);
			if (block != null)
			{
				block["id"] = 0;
				block["colour"] = new Color(0, 0, 0, 0);
				// TODO: This is likely not required as it is returning a reference.
				terrain.SetBlockAtWorldPosition(worldPosition, block);
			}
		}

		Update();
	}

    public override void _PhysicsProcess(float delta)
    {
		if ((bool)inputLayering.Call("pop_action", "move_left"))
		{
			velocity.x -= 50;
		}
		if ((bool)inputLayering.Call("pop_action", "move_right"))
		{
			velocity.x += 50;
		}
		if ((bool)inputLayering.Call("pop_action", "move_up"))
		{
			velocity.y -= 50;
		}
		if ((bool)inputLayering.Call("pop_action", "move_down"))
		{
			velocity.y += 50;
		}
		if ((bool)inputLayering.Call("pop_action", "jump"))
		{
			velocity.y = -1000;
		}
		if ((bool)inputLayering.Call("pop_action", "brake"))
		{
			velocity = Vector2.Zero;
		}
		if ((bool)inputLayering.Call("poll_action", "debug"))
		{
			PrintStrayNodes();
		}
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("zoom_in"))
		{
			camera.Set("zoom", GetCameraZoom() + new Vector2(0.5f, 0.5f));
		}
		if (@event.IsActionPressed("zoom_out"))
		{
			camera.Set("zoom", GetCameraZoom() - new Vector2(0.5f, 0.5f));
		}
		if (@event.IsActionPressed("quit"))
		{
			GetTree().Quit();
		}
		camera.Set("zoom", new Vector2(
			Mathf.Clamp((GetCameraZoom()).x, 1, 10),
			Mathf.Clamp((GetCameraZoom()).y, 1, 10)
		));
    }

	/* This method returns an Array containing two Vector2 representing the world
	position of the player's visibility on the screen. This value is in pixels
	relative to the top-left of the world.

	For example. Window is 1920x1080, and the player is at [10000, 10000] in the
	world. This function would then return something like:
		[
			Vector2(9040, 9040),
			Vector2(10960, 10960
		]
	This is an oversimplification because this function also takes into account the
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
		float viewportModifier = 0.4f;
		//float viewportModifier = 0.2f;
		// float viewportModifier = 0f;
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
	
	/* This function is similar to get_visibility_world_position_corners() but this
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

	/* This function is similar to get_visibility_world_position_corners but instead it
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

	/* This function returns a. Array of chunk positions that are contained by the
	function get_visibility_chunk_position_corners().
		- The margin parameter extends the radius of the visibility by the 'margin'
		chunks. This can be used to include chunks are slightly off screen.
		- The border only parameter is only relevant if the margin parameter is
		used. If border_only is true, then only the chunk indices that the
		(non-zero) margin added are returned in the Array.

	Example Return:
	[
		Vector2(0, 0),
		Vector2(0, 1),
		Vector2(1, 0),
		Vector2(1, 1),
		...
	]*/
	public Array<Vector2> GetVisibilityChunkPositions(int margin=0, bool borderOnly=false, int borderIgnore=0)
	{
		Array<Vector2> chunkCorners = GetVisibilityChunkPositionCorners();
		
		// Now include the margin
		Vector2 topLeftMargin = chunkCorners[0] - Vector2.One * margin;
		Vector2 bottomRightMargin = chunkCorners[1] + Vector2.One * margin;
		
		Array<Vector2> visibilityPoints = new Array<Vector2>();
		if (borderOnly)
		{
			for (int i = (int)topLeftMargin.x; i < (int)bottomRightMargin.x + 1; i++)
			for (int j = (int)topLeftMargin.y; j < (int)bottomRightMargin.y + 1; j++)
			{
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

	public KinematicBody2D GetRigidbody()
	{
		return rigidbody;
	}

	public Vector2 GetVelocity()
	{
		return velocity;
	}

	public void SetVelocity(Vector2 velocity)
	{
		this.velocity = velocity;
	}

	/* This returns the player's position, which is actually the smoothing sprite's
	location if you want the player's position on any frame other than a physics
	call. */
	public Vector2 GetPlayerPosition()
	{
		return (Vector2)smoothing.Get("position");
	}

	/* This function returns the zoom factor of the player's camera. */
	public Vector2 GetCameraZoom()
	{
		return (Vector2)camera.Get("zoom");
	}

	/* This function converts a screen position to a world position.
		- A screen position is a location inside the window. The location of the
		mouse is supplied to Godot as a screen position. */
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