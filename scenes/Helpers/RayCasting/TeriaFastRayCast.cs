using Godot;
using System;


public class TeriaFastRayCast
{
    private Terrain terrain;
    private Physics2DDirectSpaceState spaceState;
    private Entity owner;
    private Vector2 startPoint;
    private Vector2 direction;
    private float maxDistance;

    public TeriaFastRayCast(Terrain terrain, Entity owner, Physics2DDirectSpaceState spaceState, Vector2 startPoint, Vector2 direction, float maxDistance)
    {
        this.terrain = terrain;
        this.spaceState = spaceState;
        this.owner = owner;
        this.startPoint = startPoint;
        this.direction = direction.Normalized();
        this.maxDistance = maxDistance;
    }

    public static TeriaFastRayCast FromTwoPoints(Terrain terrain, Entity owner, Physics2DDirectSpaceState spaceState, Vector2 startPoint, Vector2 endPoint, float maxDistance)
    {
        return new TeriaFastRayCast(
            terrain,
            owner,
            spaceState,
            startPoint,
            endPoint - startPoint,
            maxDistance
        );
    }

    public static TeriaFastRayCast FromDirection(Terrain terrain, Entity owner, Physics2DDirectSpaceState spaceState, Vector2 startPoint, Vector2 direction, float maxDistance)
    {
        return new TeriaFastRayCast(
            terrain,
            owner,
            spaceState,
            startPoint,
            direction,
            maxDistance
        );
    }

    public TeriaFastRayCastCollision Cast()
    {
        Vector2 voxelSize = terrain.BlockPixelSize;
        // From http://www.cse.chalmers.se/edu/year/2010/course/TDA361/grid.pdf
        // Ray = u + tv for t >= 0
        // Where:
        //  u = starting point
        //  v = direction
        //  t = distance along the ray

        // Paper Test (https://www.geogebra.org/classic/tudvm5hk) (y axis flipped)
        // Voxel Size: (16, 16)
        // Start Point: (-45.5, 16.3)
        // Direction: (4, -1) -> (0.97, -0.24)
        //          : Slight Up ramp going right

        // Initialisation Phase
        // (X, Y): (-3, 1)
        // X and Y are initialized to the starting voxel coordinates.
        int X = (int) Mathf.Floor(startPoint.x / voxelSize.x);
        int Y = (int) Mathf.Floor(startPoint.y / voxelSize.y);

        // (stepX, stepY): (1, -1)
        // In addition, the variables stepX and stepY are initialized to either 1 or -1 indicating
        // whether X and Y are incremented or decremented as the ray crosses voxel boundaries (this
        // is determined by the sign of the x and y components of → v).
        int stepX = Mathf.Sign(direction.x);
        int stepY = Mathf.Sign(direction.y);


        // (tMaxX, tMaxY): (-3 * 16 + 45.5 + 16, 16.3 - 16)
        //               : (-2.5 + 16, 16.3 - 16)
        //               : (13.5, 0.3)                 -> (Note the sign isn't negative for y)
        //               : Looks good as both are less than the Voxel Size
        // Next, we determine the value of t at which the ray crosses the first vertical voxel
        // boundary and store it in variable tMaxX. We perform a similar computation in y and
        // store the result in tMaxY. The minimum of these two values will indicate how much we
        // can travel along the ray and still remain in the current voxel.
        float tMaxX;
        float tMaxY;
        if (stepX > 0)
        {
            // For stepX == 1
            tMaxX = (X * voxelSize.x) - startPoint.x + voxelSize.x;
        }
        else if (stepX < 0)
        {
            // For stepX == -1 
            tMaxX = startPoint.x - (X * voxelSize.x);
        }
        else // stepX == 0
        {
            tMaxX = float.PositiveInfinity;
        }
        if (stepY > 0)
        {
            tMaxY = (Y * voxelSize.y) - startPoint.y + voxelSize.y;
        }
        else if (stepY < 0)
        {
            tMaxY = startPoint.y - (Y * voxelSize.y); 
        }
        else
        {
            tMaxY = float.PositiveInfinity;
        }

        // (tDeltaX, tDeltaY): (16.5, 65.97)
        // Finally, we compute tDeltaX and tDeltaY. TDeltaX indicates how far along the ray we
        // must move (in units of t) for the horizontal component of such a movement to equal
        // the width of a voxel. Similarly, we store in tDeltaY the amount of movement along
        // the ray which has a vertical component equal to the height of a voxel.
        float tDeltaX = direction.x == 0 ? float.PositiveInfinity : Mathf.Abs(voxelSize.x / direction.x);
        float tDeltaY = direction.y == 0 ? float.PositiveInfinity : Mathf.Abs(voxelSize.y / direction.y);


        /*
        t = 0
        1st: Block (-3, 1) -> Empty
            : 13.5 < 0.3 = false
             : Y = 1 - 1 = 0
             : t = 0.3
             : tMaxY = 0.3 + 65.97 = 66.27
        2nd: Block (-3, 0) -> Empty
            : 13.5 < 66.27 = true
             : X = -3 + 1 = -2
             : t = 13.5
             : tMaxX = 13.5 + 16.49 = 29.99
        3rd: Block (-2, 0) -> Empty
            : 29.99 < 66.27 = true
             : X = -2 + 1 = -1
             : t = 29.99
             : tMaxX = 29.99 + 16.49 = 46.48
        4th: Block (-1, 0) -> Block! (continue for testing)
            : tMaxX = 46.48
            : tMaxY = 66.27
            : t = 29.99
        */

        // Find the ray intersection point
        // u + tv = intersection
        // u = (-45.5, 16.3)
        // t = 29.99
        // v = (0.97, -0.24)
        // intersection = (-45.5 + 29.99 * 0.97, 16.3 + 29.99 * -0.24)
        //              = (-45.5 + 29.09, 16.3 - 7.20)
        //              = (-16.41, 9.1)
        float t = 0;

        Vector2[] axis = new Vector2[2];
        axis[0] = Vector2.Right * stepX * -1;
        axis[1] = Vector2.Down * stepY * -1;
        int normalAxis = tMaxX < tMaxY ? 0 : 1;
        
        while (t <= maxDistance) // Incremental Phase
        {
            Block foundBlock = terrain.GetBlockFromWorldPosition(new Vector2(X, Y) * voxelSize);
            if (foundBlock == null)
            {
                return new TeriaFastRayCastCollision(null, null);
            }
            else if (foundBlock.IsSolid())
            {
                Vector2 normal = axis[normalAxis];
                Vector2 intercept = startPoint + t * direction; // u + tv = intersection
                return new TeriaFastRayCastCollision(intercept, normal);
            }

            if (tMaxX < tMaxY)
            {
                X = X + stepX;
                t = tMaxX;
                tMaxX += tDeltaX;
                normalAxis = 0;
            }
            else
            {
                Y = Y + stepY;
                t = tMaxY;
                tMaxY += tDeltaY;
                normalAxis = 1;
            }
        }
        return new TeriaFastRayCastCollision(null, null);
    }
}
