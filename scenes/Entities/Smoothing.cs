// Copyright (c) 2019 Lawnjelly
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

/*
Smoothing (v1.0.0)
Smoothing nodes for fixed timestep interpolation
By Lawnjelly

(Ported to C# by JaedanC)
*/

using Godot;

public class Smoothing : Node2D
{
    private NodePath target = new NodePath("../RigidBody");
    private int flags = Enabled | TrackTranslate;
    
    [Export]
    public NodePath Target
    {
        get => GetTarget();
        set => SetTarget(value);
    }

    [Export(PropertyHint.Flags, "enabled,translate,rotate,scale,global in,global out")]
    public int Flags {
        get => OnExportGetFlags();
        set => OnExportSetFlags(value);
    }
    private Node2D mTarget;
    private Vector2 mPosCurr = Vector2.Zero;
    private Vector2 mPosPrev = Vector2.Zero;
    private float mAngleCurr;
    private float mAnglePrev;
    private Vector2 mScaleCurr = Vector2.Zero;
    private Vector2 mScalePrev = Vector2.Zero;

    private const int Enabled = 1 << 0;
    private const int TrackTranslate = 1 << 1;
    private const int TrackRotate = 1 << 2;
    private const int TrackScale = 1 << 3;
    private const int GlobalIn = 1 << 4;
    private const int GlobalOut = 1 << 5;
    private const int Dirty = 1 << 6;
    private const int Invisible = 1 << 7;


    // User Functions
    // ##########################################################################################

    /* Call this on e.g. starting a level, AFTER moving the target
    so we can update both the previous and current values. */
    public void Teleport()
    {
        int tempFlags = flags;
        SetFlags(TrackTranslate | TrackRotate | TrackScale);

        RefreshTransform();
        mPosPrev = mPosCurr;
        mAnglePrev = mAngleCurr;
        mScalePrev = mScaleCurr;

        // Call frame update to make sure all components of the node are set
        _Process(0);

        // Get back the old flags
        flags = tempFlags;
    }

    public void SetEnabled(bool enable)
    {
        ChangeFlags(Enabled, enable);
        SetProcessing();
    }

    public bool IsEnabled()
    {
        return TestFlags(Enabled);
    }

    // ##########################################################################################

    public override void _Ready()
    {
        mAngleCurr = 0;
        mAnglePrev = 0;

        Developer.AssertNotNull(mTarget, "A target must be defined for the Smoothing2D node to work.");
    }

    private void SetTarget(NodePath newTarget)
    {
        target = newTarget;
        if (IsInsideTree())
            FindTarget();
    }

    private string GetTarget()
    {
        return target;
    }

    private void OnExportSetFlags(int flags)
    {
        this.flags = flags;
        // We may have enabled or disabled
        SetProcessing();
    }

    private int OnExportGetFlags()
    {
        return flags;
    }

    private void SetProcessing()
    {
        bool enabled = TestFlags(Enabled);
        if (TestFlags(Invisible))
        {
            enabled = false;
        }

        SetProcess(enabled);
        SetPhysicsProcess(enabled);
    }

    public override void _EnterTree()
    {
        // Might have been moved
        FindTarget();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged)
        {
            ChangeFlags(Invisible, IsVisibleInTree() == false);
            SetProcessing();
        }
    }

    private void RefreshTransform()
    {
        ClearFlags(Dirty);

        if (!HasTarget())
            return;

        if (TestFlags(GlobalIn))
        {
            if (TestFlags(TrackTranslate))
            {
                mPosPrev = mPosCurr;
                mPosCurr = mTarget.GlobalPosition;
            }

            if (TestFlags(TrackRotate))
            {
                mAnglePrev = mAngleCurr;
                mAngleCurr = mTarget.GlobalRotation;
            }

            if (TestFlags(TrackScale))
            {
                mScalePrev = mScaleCurr;
                mScaleCurr = mTarget.GlobalScale;
            }
        }
        else
        {
            if (TestFlags(TrackTranslate))
            {
                mPosPrev = mPosCurr;
                mPosCurr = mTarget.Position;
            }

            if (TestFlags(TrackRotate))
            {
                mAnglePrev = mAngleCurr;
                mAngleCurr = mTarget.Rotation;
            }

            if (TestFlags(TrackScale))
            {
                mScalePrev = mScaleCurr;
                mScaleCurr = mTarget.Scale;
            }
        }
    }

    private void FindTarget()
    {
        mTarget = null;
        if (target.IsEmpty())
            return;

        // Not sure about this.
        mTarget = GetNode<Node2D>(target);

        if (mTarget is Node2D)
            return;

        mTarget = null;
    }

    private bool HasTarget()
    {
        if (mTarget == null)
            return false;

        // Has not been deleted?
        if (IsInstanceValid(mTarget))
            return true;

        mTarget = null;
        return false;
    }

    public override void _Process(float delta)
    {
        if (TestFlags(Dirty))
            RefreshTransform();
        
        float f = Engine.GetPhysicsInterpolationFraction();

        if (TestFlags(GlobalOut))
        {
            // Translate
            if (TestFlags(TrackTranslate))
                GlobalPosition = mPosPrev.LinearInterpolate(mPosCurr, f);

            // Rotate
            if (TestFlags(TrackRotate))
                GlobalRotation = LerpAngle(mAnglePrev, mAngleCurr, f);

            // Scale?
            if (TestFlags(TrackScale))
                GlobalScale = mScalePrev.LinearInterpolate(mScaleCurr, f);
        }
        else
        {
            // Translate
            if (TestFlags(TrackTranslate))
                Position = mPosPrev.LinearInterpolate(mPosCurr, f);

            // Rotate
            if (TestFlags(TrackRotate))
                Rotation = LerpAngle(mAnglePrev, mAngleCurr, f);

            // Scale
            if (TestFlags(TrackScale))
                Scale = mScalePrev.LinearInterpolate(mScaleCurr, f);
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        // Take care of the special case where multiple physics ticks
        // occur before a frame .. the data must flow!
        if (TestFlags(Dirty))
            RefreshTransform();

        SetFlags(Dirty);
    }

    private static float LerpAngle(float from, float to, float weight)
    {
        return from + ShortAngleDist(from, to) * weight;
    }

    private static float ShortAngleDist(float from, float to)
    {
        const float maxAngle = 2 * Mathf.Pi;
        // Floating point modulus
        float diff = (to - from) % maxAngle;
        return (2 * diff % maxAngle) - diff;
    }

    private void SetFlags(int f)
    {
        flags |= f;
    }

    private void ClearFlags(int f)
    {
        flags &= ~f;
    }

    private bool TestFlags(int f)
    {
        return (flags & f) == f;
    }

    private void ChangeFlags(int f, bool set)
    {
        if (set)
            SetFlags(f);
        else
            ClearFlags(f);
    }
}
