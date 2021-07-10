#if TOOLS
using System.IO;
using Godot;

namespace ExtraNodes.Nodes
{

    [Tool]
    public class ThrowCast2D : Node2D
    {

        #region Backing Fields
        private bool _enabled = false;
        private bool _excludeParent = true;
        private float _throwAngle = 0.0f;
        private float _throwSpeed = 100.0f;
        private float _gravity = 9.81f;
        private int _segments = 100;
        private float _width = 2.0f;
        #endregion

        #region Other Fields
        private bool _rebuildNecessary = false;
        private Godot.Collections.Array _exclude = new Godot.Collections.Array();
        private MeshInstance2D _debugMesh;
        private int _vertexStride;
        private byte[] _vertexBuffer;
        private BinaryWriter _writer;
        #endregion

        #region Exported Properties
        [Export]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                _rebuildNecessary = true;
            }
        }

        [Export]
        public bool ExcludeParent
        {
            get => _excludeParent;
            set
            {
                _excludeParent = value;
                if (!IsInsideTree())
                    return;
                var parentCollisionObject = GetParentOrNull<CollisionObject2D>();
                if (parentCollisionObject == null)
                    return;
                if (value && !_exclude.Contains(parentCollisionObject.GetRid()))
                {
                    _exclude.Add(parentCollisionObject.GetRid());
                }
                else
                {
                    _exclude.Remove(parentCollisionObject.GetRid());
                }
            }
        }

        [Export(PropertyHint.Range, "-360.0,360.0,0.1,or_greater,or_lesser")]
        public float ThrowAngleDeg
        {
            get => Mathf.Rad2Deg(_throwAngle);
            set
            {
                _throwAngle = Mathf.Deg2Rad(value);
                _rebuildNecessary = true;
            }
        }

        [Export(PropertyHint.Range, "0.0,1000.0,0.1,or_greater")]
        public float ThrowSpeed
        {
            get => _throwSpeed;
            set
            {
                _throwSpeed = value;
                _rebuildNecessary = true;
            }
        }

        [Export(PropertyHint.Range, "-10.00,10.00,0.01,or_greater,or_lesser")]
        public float Gravity
        {
            get => _gravity;
            set
            {
                _gravity = value;
                _rebuildNecessary = true;
            }
        }

        [Export(PropertyHint.Range, "1,500,1,or_greater")]
        public int Segments
        {
            get => _segments;
            set
            {
                _segments = value;
                _rebuildNecessary = true;
                if (_writer != null)
                {
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }

        [Export(PropertyHint.Range, "0.1,10.0,1.0,or_greater")]
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                _rebuildNecessary = true;
            }
        }

        [Export(PropertyHint.Layers2dPhysics)]
        public uint CollisionMask { get; set; } = 1;

        [Export]
        public bool CollideWithAreas { get; set; } = false;

        [Export]
        public bool CollideWithBodies { get; set; } = true;

        [Export]
        public ShaderMaterial ThrowCastLineMaterial { get; set; }
        #endregion

        #region Public Node specific Properties
        public bool IsColliding { get; private set; } = false;
        public Object Collider { get; private set; } = null;
        public Vector2 CollisionPoint { get; private set; }
        public Vector2 CollisionNormal { get; private set; }
        public int CollisionShape { get; private set; }
        #endregion

        #region Other Properties
        public float ThrowAngle
        {
            get => _throwAngle;
            set
            {
                _throwAngle = value;
                _rebuildNecessary = true;
            }
        }

        #endregion

        #region Overwritten Base Methods
        public override void _Ready()
        {
            base._Ready();

            if (ThrowCastLineMaterial == null && !Engine.EditorHint)
            {
                ThrowCastLineMaterial = ResourceLoader.Load<ShaderMaterial>("res://addons/ExtraNodesCS/Nodes/ThrowCast2D.material");
            }

            // Update Exported Properties
            Enabled = Enabled;
            ExcludeParent = ExcludeParent;
            ThrowAngleDeg = ThrowAngleDeg;
            ThrowSpeed = ThrowSpeed;
            Gravity = Gravity;
            Segments = Segments;
            Width = Width;
            CollisionMask = CollisionMask;
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            _debugMesh = new MeshInstance2D();
            _debugMesh.Mesh = new ArrayMesh();
            _debugMesh.Visible = false;
            AddChild(_debugMesh);
        }

        public override void _ExitTree()
        {
            if (_debugMesh != null)
            {
                if (_writer != null)
                {
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                }
                if (_debugMesh.IsInsideTree())
                    _debugMesh.QueueFree();
                else
                    _debugMesh.Free();
                _debugMesh = null;
            }
            base._ExitTree();
        }

        public override void _Process(float delta)
        {
            try
            {

                if (_rebuildNecessary)
                {
                    var mesh = _debugMesh.Mesh as ArrayMesh;
                    if (mesh == null)
                        return;

                    if (mesh.GetSurfaceCount() == 0 || _writer == null)
                    {
                        if (_writer != null)
                        {
                            _writer.Close();
                            _writer.Dispose();
                            _writer = null;
                        }

                        while (mesh.GetSurfaceCount() > 0)
                            mesh.SurfaceRemove(0);

                        var vertices = new Vector3[_segments * 2 + 2];
                        var uvs = new Vector2[_segments * 2 + 2];
                        uvs[_segments * 2 + 0] = new Vector2(0.0f, 1.0f);
                        uvs[_segments * 2 + 1] = new Vector2(1.0f, 1.0f);

                        var indices = new int[_segments * 6];
                        for (var i = 0; i < _segments; ++i)
                        {
                            uvs[i * 2 + 0] = new Vector2(0.0f, i / (float)_segments);
                            uvs[i * 2 + 1] = new Vector2(1.0f, i / (float)_segments);

                            indices[i * 6 + 0] = i * 2 + 2;
                            indices[i * 6 + 1] = i * 2 + 1;
                            indices[i * 6 + 2] = i * 2 + 0;
                            indices[i * 6 + 3] = i * 2 + 2;
                            indices[i * 6 + 4] = i * 2 + 3;
                            indices[i * 6 + 5] = i * 2 + 1;
                        }

                        var a = new Godot.Collections.Array();
                        a.Resize((int)ArrayMesh.ArrayType.Max);
                        a[(int)ArrayMesh.ArrayType.Vertex] = vertices;
                        a[(int)ArrayMesh.ArrayType.TexUv] = uvs;
                        a[(int)ArrayMesh.ArrayType.Index] = indices;
                        mesh.AddSurfaceFromArrays(ArrayMesh.PrimitiveType.Triangles, a, null, 0);
                        var rid = mesh.GetRid();
                        _vertexBuffer = VisualServer.MeshSurfaceGetArray(rid, 0);
                        var format = VisualServer.MeshSurfaceGetFormat(rid, 0);
                        var vertexLen = VisualServer.MeshSurfaceGetArrayLen(rid, 0);
                        var indexLen = VisualServer.MeshSurfaceGetArrayIndexLen(rid, 0);
                        _vertexStride = (int)VisualServer.MeshSurfaceGetFormatStride(format, vertexLen, indexLen);
                        _writer = new BinaryWriter(new MemoryStream(_vertexBuffer, true), System.Text.Encoding.ASCII, false);
                    }

                    var p0 = Vector2.Zero;
                    var p1 = new Vector2(0.0f, _width * -.5f);
                    var p2 = new Vector2(0.0f, _width * 0.5f);
                    var p3 = Vector2.Zero;

                    var vx0 = _throwSpeed * Mathf.Cos(_throwAngle); // Horizontal Start Speed
                    var vy0 = _throwSpeed * Mathf.Sin(_throwAngle); // Vertical Start Speed
                    for (var i = 0; i <= _segments; ++i)
                    {
                        var t = i * 0.5f; // Time
                        var x = t * vx0; // X Position at time t
                        var vy = vy0 - _gravity * t; // Y Speed at time t
                        var y = -(vy0 * t - (_gravity * 0.5f * t * t)); // Y Position at time t (Y-Flipped)
                        var phi = -Mathf.Atan2(vy, vx0); // Angle at time t
                        p0 = new Vector2(x, y);
                        p3 = p1.Rotated(phi) + p0;
                        _writer.Seek(_vertexStride * (i * 2 + 0), SeekOrigin.Begin);
                        _writer.Write(p3.x);
                        _writer.Write(p3.y);
                        p3 = p2.Rotated(phi) + p0;
                        _writer.Seek(_vertexStride * (i * 2 + 1), SeekOrigin.Begin);
                        _writer.Write(p3.x);
                        _writer.Write(p3.y);
                    }
                    _writer.Flush();
                    mesh.SurfaceUpdateRegion(0, 0, _vertexBuffer);

                    if (_debugMesh.Material != ThrowCastLineMaterial)
                        _debugMesh.Material = ThrowCastLineMaterial;
                }

                _debugMesh.Visible = Enabled;

                if (!Enabled)
                    return;

                UpdateThrowCastState();
            }
            finally
            {
                base._Process(delta);
            }
        }
        #endregion

        #region Private Node specific Methods
        private void UpdateThrowCastState()
        {
            var w2d = GetWorld2d();
            var dss = Physics2DServer.SpaceGetDirectState(w2d.Space);
            var gt = GlobalTransform;
            ShaderMaterial m = _debugMesh.Material as ShaderMaterial;
            var p0 = Vector2.Zero;
            var p1 = Vector2.Zero;
            var vx0 = _throwSpeed * Mathf.Cos(_throwAngle); // Horizontal Start Speed
            var vy0 = _throwSpeed * Mathf.Sin(_throwAngle); // Vertical Start Speed
            for (var i = 1; i <= _segments; ++i)
            {
                var t = i * 0.5f; // Time
                var x = t * vx0; // X Position at time t
                var y = -(vy0 * t - (_gravity * 0.5f * t * t)); // Y Position at time t (Y-Flipped)
                p1 = new Vector2(x, y);
                var p2 = gt.Xform(p0);
                var p3 = gt.Xform(p1);
                var rr = dss.IntersectRay(p2, p3, _exclude, CollisionMask, CollideWithBodies, CollideWithAreas);
                if (rr.Count > 0)
                {
                    IsColliding = true;
                    var against = (int)rr["collider_id"];
                    Collider = GD.InstanceFromId((ulong)against);
                    CollisionPoint = (Vector2)rr["position"];
                    CollisionNormal = (Vector2)rr["normal"];
                    CollisionShape = (int)rr["shape"];

                    if (m != null && m.Shader.HasParam("collideAt"))
                    {
                        var l0 = (i - 1) / (float)_segments;
                        var l1 = i / (float)_segments;
                        var l2 = l1 - l0;
                        var l3 = (p3 - p2).Length();
                        var l4 = (CollisionPoint - p2).Length();
                        m.SetShaderParam("collideAt", l0 + ((l4 / l3) * l2));
                    }

                    return;
                }

                p0 = p1;
            }
            IsColliding = false;
            Collider = null;
            CollisionPoint = Vector2.Zero;
            CollisionNormal = Vector2.Zero;
            CollisionShape = 0;

            if (m != null && m.Shader.HasParam("collideAt"))
                m.SetShaderParam("collideAt", 100.0f);
        }
        #endregion

        #region Public Node specific Methods
        public void ForceThrowCastUpdate()
        {
            UpdateThrowCastState();
        }
        #endregion
    }

}
#endif
