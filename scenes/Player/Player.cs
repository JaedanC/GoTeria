using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class Player : Node2D, ICollidable
{   
	private const float _ZOOM_CLAMP = 20f;
    private Terrain _terrain;
	private Godot.Object _smoothing;
	private Camera2D _camera;
	private InputLayering _inputLayering;
    private KinematicBody2D _rigidBody;
	private CollisionShape2D _hitbox;
	
	private Vector2 _velocity;
	public Vector2 Velocity { get {	return _velocity; } set { _velocity = value; } }

	/* Hide the player's position with the smoothing one. This returns the player's position
	(which is actually the smoothing sprite's location) if you want the player's position on
	any frame other than a physics frame. This is an linear interpolated Vector2. */
	new public Vector2 Position { get { return (Vector2)_smoothing.Get("position"); } }
	public Vector2 CameraZoom { get { return _camera.Zoom; } }


    public override void _Ready()
    {
		Name = "Player";

        _terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
        _rigidBody = GetNode<KinematicBody2D>("RigidBody");
        _hitbox = _rigidBody.GetNode<CollisionShape2D>("Hitbox");
        _smoothing = GetNode<Godot.Object>("Smoothing");
        _camera = GetNode<Camera2D>("Smoothing/Camera");
        _inputLayering = GetNode<InputLayering>("/root/InputLayering");
		_velocity = Vector2.Zero;
    }

	public override void _Process(float delta)
	{
		if (_inputLayering.PopAction("zoom_reset"))
			_camera.Zoom = Vector2.Zero;
		
		if (_inputLayering.PopAction("click"))
		{
			Vector2 worldPosition = ScreenToWorldPosition(GetViewport().GetMousePosition());
			Block newBlock = new Block(1, new Color(1, 1, 0, 1));
			_terrain.SetBlockAtWorldPosition(worldPosition, newBlock);
		}

		if (_inputLayering.PopAction("dig"))
		{
			Vector2 worldPosition = ScreenToWorldPosition(GetViewport().GetMousePosition());
			Block newBlock = new Block(0, new Color(0, 0, 0, 0));
			_terrain.SetBlockAtWorldPosition(worldPosition, newBlock);
		}

		

		Update();
	}

    public override void _PhysicsProcess(float delta)
    {
		if (_inputLayering.PopAction("move_left"))
		{
			_velocity.x -= 50;
		}
		if (_inputLayering.PopAction("move_right"))
		{
			_velocity.x += 50;
		}
		if (_inputLayering.PopAction("move_up"))
		{
			_velocity.y -= 50;
		}
		if (_inputLayering.PopAction("move_down"))
		{
			_velocity.y += 50;
		}
		if (_inputLayering.PopAction("jump"))
		{
			_velocity.y = -1000;
		}
		if (_inputLayering.PopAction("brake"))
		{
			_velocity = Vector2.Zero;
		}
		// if (_inputLayering.PollActionPressed("debug"))
		// {
		// 	GD.Print("Printing Stray Nodes:");
		// 	PrintStrayNodes();
		// }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("zoom_in"))
			_camera.Zoom += new Vector2(0.5f, 0.5f);

		if (@event.IsActionPressed("zoom_out"))
			_camera.Zoom -= new Vector2(0.5f, 0.5f);

		if (@event.IsActionPressed("zoom_reset"))
			_camera.Zoom = new Vector2(1, 1);

		if (@event.IsActionPressed("quit"))
			GetTree().Quit();
		
		_camera.Zoom = new Vector2(
			Mathf.Clamp(_camera.Zoom.x, 1, _ZOOM_CLAMP),
			Mathf.Clamp(_camera.Zoom.y, 1, _ZOOM_CLAMP)
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
		// Vector2 smoothedPosition = GetPlayerPosition();
		Vector2 smoothedPosition = Position;
		Rect2 viewportRectangle = GetViewportRect();
		
		// Using the viewport rectangle as a base, centre a copy of it around the player.
		// We used smoothed position because we want to take the interpolated position of
		// the player so visibility still works at high speeds.
		viewportRectangle.Position = smoothedPosition - viewportRectangle.Size / 2;
		
		// Expand this Rectangle to take into account camera zooming by using two
		// Vector2's the point the the corners of the screen and using the Rect2.expand
		// method.
		viewportRectangle = viewportRectangle.Expand(smoothedPosition - (GetViewportRect().Size / 2) * CameraZoom);
		viewportRectangle = viewportRectangle.Expand(smoothedPosition + (GetViewportRect().Size / 2) * CameraZoom);
		
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

		return new Array<Vector2>(topLeft, bottomRight);
	}
	
	/* This function is similar to GetVisibilityWorldPositionCorners() but this
	returns the two vectors divided by the block size. This then returns two
	Vector2's which represent the corners of block indices in the world that can be
	seen by the player. */
	public Array<Vector2> GetVisibilityWorldBlockPositionCorners()
	{
		Array<Vector2> corners = GetVisibilityWorldPositionCorners();
		return new Array<Vector2>(
			(corners[0] / _terrain.BlockPixelSize).Floor(),
			(corners[1] / _terrain.BlockPixelSize).Floor()
		);
	}

	/* This function is similar to GetVisibilityWorldPositionCorners but instead it
	returns the two Vector2's which represent the chunk indices that can be seen by
	the player. */
	public Array<Vector2> GetVisibilityChunkPositionCorners()
	{
		Array<Vector2> worldPositionCorners = GetVisibilityWorldPositionCorners();
		var chunkPositionTopLeft = (worldPositionCorners[0] / _terrain.ChunkPixelDimensions).Floor();
		var chunkPositionBottomRight = (worldPositionCorners[1] / _terrain.ChunkPixelDimensions).Floor();
		return new Array<Vector2>(chunkPositionTopLeft, chunkPositionBottomRight);
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
	
	/* This function converts a screen position to a world position.
		- A screen position is a location inside the window. For example, The location
		of the mouse is supplied to Godot as a screen position. */
	public Vector2 ScreenToWorldPosition(Vector2 screenPosition)
	{
		// Vector2 worldPosition = GetPlayerPosition() + screenPosition * GetCameraZoom();
		Vector2 worldPosition = Position + screenPosition * CameraZoom;
		return worldPosition - CameraZoom * GetViewportRect().Size / 2;
	}

	public KinematicBody2D GetRigidBody()
	{
		return _rigidBody;
	}

	public CollisionShape2D GetHitbox()
	{
		return _hitbox;
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
            ScreenToWorldPosition(GetViewport().Size/2 + Velocity / 10),
            new Color(1, 0, 0)
        );
    }
}