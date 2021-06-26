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
using System;

public class Smoothing : Node2D
{
    private NodePath target = new NodePath("../RigidBody");
    private int flags = ENABLED | TRANSLATE;
    
    [Export]
    public NodePath Target
    {
        get
        {
            return GetTarget();
        }
        set
        {
            SetTarget(value);
        }
    }

    [Export(PropertyHint.Flags, "enabled,translate,rotate,scale,global in,global out")]
    public int Flags {
        get
        {
            return OnExportGetFlags();
        }
        set
        {
            OnExportSetFlags(value);
        }
    }
    private Node2D _m_Target;
    private Vector2 m_Pos_curr = Vector2.Zero;
    private Vector2 m_Pos_prev = Vector2.Zero;
    private float m_Angle_curr;
    private float m_Angle_prev;
    private Vector2 m_Scale_curr = Vector2.Zero;
    private Vector2 m_Scale_prev = Vector2.Zero;

    private const int ENABLED = 1 << 0;
    private const int TRANSLATE = 1 << 1;
    private const int ROTATE = 1 << 2;
    private const int SCALE = 1 << 3;
    private const int GLOBAL_IN = 1 << 4;
    private const int GLOBAL_OUT = 1 << 5;
    private const int DIRTY = 1 << 6;
    private const int INVISIBLE = 1 << 7;


    // User Functions
    // ##########################################################################################

    /* Call this on e.g. starting a level, AFTER moving the target
    so we can update both the previous and current values. */
    public void Teleport()
    {
        var tempFlags = flags;
        SetFlags(TRANSLATE | ROTATE | SCALE);

        RefreshTransform();
        m_Pos_prev = m_Pos_curr;
        m_Angle_prev = m_Angle_curr;
        m_Scale_prev = m_Scale_curr;

        // Call frame update to make sure all components of the node are set
        _Process(0);

        // Get back the old flags
        flags = tempFlags;
    }

    public void SetEnabled(bool enable)
    {
        ChangeFlags(ENABLED, enable);
        SetProcessing();
    }

    public bool isEnabled()
    {
        return TestFlags(ENABLED);
    }

    // ##########################################################################################

    public override void _Ready()
    {
        m_Angle_curr = 0;
        m_Angle_prev = 0;

        Developer.AssertNotNull(_m_Target, "A target must be defined for the Smoothing2D node to work.");
    }

    private void SetTarget(NodePath newTarget)
    {
        target = newTarget;
        if (IsInsideTree())
            FindTarget();
    }

    private String GetTarget()
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
        bool enabled = TestFlags(ENABLED);
        if (TestFlags(INVISIBLE))
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
            ChangeFlags(INVISIBLE, IsVisibleInTree() == false);
            SetProcessing();
        }
    }

    private void RefreshTransform()
    {
        ClearFlags(DIRTY);

        if (!HasTarget())
            return;

        if (TestFlags(GLOBAL_IN))
        {
            if (TestFlags(TRANSLATE))
            {
                m_Pos_prev = m_Pos_curr;
                m_Pos_curr = _m_Target.GlobalPosition;
            }

            if (TestFlags(ROTATE))
            {
                m_Angle_prev = m_Angle_curr;
                m_Angle_curr = _m_Target.GlobalRotation;
            }

            if (TestFlags(SCALE))
            {
                m_Scale_prev = m_Scale_curr;
                m_Scale_curr = _m_Target.GlobalScale;
            }
        }
        else
        {
            if (TestFlags(TRANSLATE))
            {
                m_Pos_prev = m_Pos_curr;
                m_Pos_curr = _m_Target.Position;
            }

            if (TestFlags(ROTATE))
            {
                m_Angle_prev = m_Angle_curr;
                m_Angle_curr = _m_Target.Rotation;
            }

            if (TestFlags(SCALE))
            {
                m_Scale_prev = m_Scale_curr;
                m_Scale_curr = _m_Target.Scale;
            }
        }
    }

    private void FindTarget()
    {
        _m_Target = null;
        if (target.IsEmpty())
            return;

        // Not sure about this.
        _m_Target = GetNode<Node2D>(target);

        if (_m_Target is Node2D)
            return;

        _m_Target = null;
    }

    private bool HasTarget()
    {
        if (_m_Target == null)
            return false;

        // Has not been deleted?
        if (IsInstanceValid(_m_Target))
            return true;

        _m_Target = null;
        return false;
    }

    public override void _Process(float delta)
    {
        if (TestFlags(DIRTY))
            RefreshTransform();
        
        float f = Engine.GetPhysicsInterpolationFraction();

        if (TestFlags(GLOBAL_OUT))
        {
            // Translate
            if (TestFlags(TRANSLATE))
                GlobalPosition = m_Pos_prev.LinearInterpolate(m_Pos_curr, f);

            // Rotate
            if (TestFlags(ROTATE))
                GlobalRotation = LerpAngle(m_Angle_prev, m_Angle_curr, f);

            // Scale?
            if (TestFlags(SCALE))
                GlobalScale = m_Scale_prev.LinearInterpolate(m_Scale_curr, f);
        }
        else
        {
            // Translate
            if (TestFlags(TRANSLATE))
                Position = m_Pos_prev.LinearInterpolate(m_Pos_curr, f);

            // Rotate
            if (TestFlags(ROTATE))
                Rotation = LerpAngle(m_Angle_prev, m_Angle_curr, f);

            // Scale
            if (TestFlags(SCALE))
                Scale = m_Scale_prev.LinearInterpolate(m_Scale_curr, f);
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        // Take care of the special case where multiple physics ticks
        // occur before a frame .. the data must flow!
        if (TestFlags(DIRTY))
            RefreshTransform();

        SetFlags(DIRTY);
    }

    private float LerpAngle(float from, float to, float weight)
    {
        return from + ShortAngleDist(from, to) * weight;
    }

    private float ShortAngleDist(float from, float to)
    {
        float maxAngle  = 2 * Mathf.Pi;
        // Floating point modulus
        float diff = (to - from) % maxAngle;
        return (2 * diff % maxAngle) - diff;
    }

    private void SetFlags(int f)
    {
        this.flags |= f;
    }

    private void ClearFlags(int f)
    {
        this.flags &= ~f;
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
