using Godot;
using Godot.Collections;
using System.Diagnostics;


/* Using Godot's built-in Raycasting system is not an option because not every hitbox is present in the world for the
blocks. This class ensures that blocks have the their hitboxes created before the Raycasting is done. */
public class TeriaRayCast
{
    private Physics2DDirectSpaceState spaceState;
    private Entity owner;
    private Terrain terrain;
    private CollisionSystem collisionSystem;
    private Vector2 startPoint;
    private Vector2 direction;
    private float maxDistance;
    Array<Vector2> raycastStepPoints;

    // This is private because the constructor is named using the two static methods.
    private TeriaRayCast(Terrain terrain, CollisionSystem collisionSystem, Physics2DDirectSpaceState spaceState, Entity owner, Vector2 startPoint, Vector2 direction, float maxDistance)
    {
        this.terrain = terrain;
        this.collisionSystem = collisionSystem;
        this.spaceState = spaceState;
        this.owner = owner;
        this.startPoint = startPoint;
        this.direction = direction.Normalized();
        this.maxDistance = maxDistance;
    }

    // public override void _Ready()
    // {
    //     terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");
    //     collisionSystem = GetNode<CollisionSystem>("/root/WorldSpawn/CollisionSystem");
    // }

    /* Create a TeriaRayCast using two points. Note: The two points only dictate the starting point and a
    direction. The maxDistance field is what dictates the length of the Ray that is casted. It is called
    maxDistance because only the first collision is returned for a RayCast. TODO: In future we may want
    the ray to continue through something. Maybe we can add a penetration Array too that is included to
    the exclusion Array.*/
    public static TeriaRayCast FromTwoPoints(Terrain terrain, CollisionSystem collisionSystem, Physics2DDirectSpaceState spaceState, Entity owner, Vector2 startPoint, Vector2 endPoint, float maxDistance)
    {
        TeriaRayCast teriaRayCast = new TeriaRayCast(
            terrain,
            collisionSystem,
            spaceState,
            owner,
            startPoint,
            endPoint - startPoint,
            maxDistance
        );
        // The owner now has the RayCast as a child so that we can Grab the Terrain from the SceneTree
        // in _Ready().
        // owner.AddChild(teriaRayCast);
        return teriaRayCast;
    }

    /* Create a TeriaRayCast using a startingPoint and a direction. MaxDistance is the length of the RayCast. */
    public static TeriaRayCast FromDirection(Terrain terrain, CollisionSystem collisionSystem, Physics2DDirectSpaceState spaceState, Entity owner, Vector2 startPoint, Vector2 direction, float maxDistance)
    {
        TeriaRayCast teriaRayCast = new TeriaRayCast(
            terrain,
            collisionSystem,
            spaceState,
            owner,
            startPoint,
            direction,
            maxDistance
        );
        // The owner now has the RayCast as a child so that we can Grab the Terrain from the SceneTree
        // in _Ready().
        // owner.AddChild(teriaRayCast);
        return teriaRayCast;
    }

    /* Calculates the positions to step the ray forward. Since the collisionSystem.CreateBlockHitboxesInArea()
    takes in an area, we *could* just create a rect that contains the start and end point, but that would be
    very inefficient as many blocks that aren't even close to the ray would be loaded. Instead, we step the
    ray forward by an amount dictated by the block size of the world such that the area rectangle is small. */
    private void CreateRayCastSteps(float maxDistance)
    {
        raycastStepPoints = new Array<Vector2>();

        // If the distance is zero then the ray cast will not kick in.
        if (maxDistance == 0)
        {
            return;
        }

        // Get the smallest component of the BlockPixelSize and use that as the ray cast step.
        int stepFactor = 1;
        float raycastStepSize = terrain.BlockPixelSize[(int)terrain.BlockPixelSize.MinAxis()] * stepFactor;

        // Calculate the number of full steps that we can apply inside the max distance.
        int numberOfFullSteps = (int)Mathf.Floor(maxDistance / raycastStepSize);
        // The last step is the remainder.
        float lastStepSize = maxDistance % raycastStepSize;

        Vector2 fullStepVector = Vector2.One * raycastStepSize;
        Vector2 lastStepVector = Vector2.One * lastStepSize;

        // Create the points from the above information.
        raycastStepPoints = new Array<Vector2>();
        raycastStepPoints.Add(startPoint);
        for (int step = 1; step <= numberOfFullSteps; step++)
        {
            raycastStepPoints.Add(startPoint + fullStepVector * step * direction);
        }
        raycastStepPoints.Add(startPoint + (fullStepVector * numberOfFullSteps + lastStepVector) * direction);
    }

    private void CreateRayCastStepsNaive(float maxDistance)
    {
        raycastStepPoints = new Array<Vector2>();

        // If the distance is zero then the ray cast will not kick in.
        if (maxDistance == 0)
        {
            return;
        }

        // Get the smallest component of the BlockPixelSize and use that as the ray cast step.
        raycastStepPoints.Add(startPoint);
        raycastStepPoints.Add(startPoint + startPoint + direction * maxDistance);
    }

    /* Casts a this ray. Returns a TeriaRayCastCollision that contains information about the object that the
    ray hit first. Note: The ray cast does not include the owner of this ray. */
    public TeriaRayCastCollision Cast()
    {
        // CreateRayCastStepsN(maxDistance);
        CreateRayCastSteps(maxDistance);

        // Don't do the ray cast
        if (raycastStepPoints.Count < 2)
        {
            return new TeriaRayCastCollision(new Dictionary());
        }

        // Load all the blocks along the ray's path so that the ray cast function
        // has something to collide with.
        // Physics2DDirectSpaceState spaceState = GetWorld2d().DirectSpaceState;
        for (int i = 0; i < raycastStepPoints.Count - 1; i++)
        {
            Vector2 firstPoint = raycastStepPoints[i];
            Vector2 secondPoint = raycastStepPoints[i + 1];

            Rect2 area = new Rect2(firstPoint, (secondPoint - firstPoint));
            area = area.Abs();
            collisionSystem.CreateBlockHitboxesInArea(area);
        }

        // Exclude the Entity that is the owner from the ray cast. TODO: In future maybe
        // we want to return an array of collisions from a ray?
        Array exclude = new Array(owner.GetRigidBody());
        return new TeriaRayCastCollision(spaceState.IntersectRay(
            raycastStepPoints[0],
            raycastStepPoints[raycastStepPoints.Count - 1],
            exclude
        ));
    }

    // public override void _Draw()
    // {
    //     for (int i = 0; i < raycastStepPoints.Count - 1; i++)
    //     {
    //         Vector2 firstPoint = raycastStepPoints[i];
    //         Vector2 secondPoint = raycastStepPoints[i + 1];

    //         DrawLine(firstPoint, secondPoint, Colors.Red, 2, true);
    //     }
    // }

}
