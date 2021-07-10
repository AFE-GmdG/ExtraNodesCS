using System;
using System.Globalization;
using ExtraNodes.Nodes;
using Godot;

namespace ExtraNodes.Demo
{

    public class ThrowCastTest2D : Node2D
    {

        #region Other Fields
        private StaticBody2D _cannonPosition;
        private ThrowCast2D _throwCast;
        private Line2D _laser;
        private Label _mousePositionText;
        private Label _throwAngleText;
        private Label _throwSpeedText;
        private Label _colliderText;
        #endregion

        #region Overwritten Base Methods
        public override void _Ready()
        {
            base._Ready();
            _cannonPosition = GetNode<StaticBody2D>("Cannon");
            _throwCast = GetNode<ThrowCast2D>("Cannon/ThrowCast2D");
            _laser = GetNode<Line2D>("Cannon/Line2D");
            _mousePositionText = GetNode<Label>("MousePositionText");
            _throwAngleText = GetNode<Label>("ThrowAngleText");
            _throwSpeedText = GetNode<Label>("ThrowSpeedText");
            _colliderText = GetNode<Label>("ColliderText");

        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            var mousePosition = GetGlobalMousePosition();
            FormattableString text = $"[ {mousePosition.x,4} {mousePosition.y,4} ]";
            _mousePositionText.Text = text.ToString(CultureInfo.InvariantCulture);

            var cannonPosition = _cannonPosition.GlobalTransform.origin;
            var phi = Mathf.Atan2(cannonPosition.y - mousePosition.y, mousePosition.x - cannonPosition.x);
            var speed = (mousePosition - cannonPosition).Length() * 0.2f;

            text = $" {Mathf.Rad2Deg(phi),4:f1}° ({phi,6:f3}) Sin: {Mathf.Sin(phi),6:f3} Cos: {Mathf.Cos(phi),6:f3}";
            _throwAngleText.Text = text.ToString(CultureInfo.InvariantCulture);

            text = $"{speed,6:f1}";
            _throwSpeedText.Text = text.ToString(CultureInfo.InvariantCulture);

            _laser.ClearPoints();
            _laser.AddPoint(_cannonPosition.ToLocal(cannonPosition));
            _laser.AddPoint(_cannonPosition.ToLocal(mousePosition));

            _throwCast.ThrowAngle = phi;
            _throwCast.ThrowSpeed = speed;

            // The following values are from previous frame. If you really need the current changed values, uncomment the following line.
            // _throwCast.ForceThrowCastUpdate();
            if (_throwCast.IsColliding) {
                text = $"{(_throwCast.Collider as Node).Name} [ {_throwCast.CollisionPoint.x,4} {_throwCast.CollisionPoint.y,4} ]";
                _colliderText.Text = text.ToString(CultureInfo.InvariantCulture);
            } else {
                _colliderText.Text = "(null)";
            }
        }
        #endregion

    }

}
