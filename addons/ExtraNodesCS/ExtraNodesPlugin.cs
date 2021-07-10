#if TOOLS
using Godot;

namespace ExtraNodes
{

    [Tool]
    public class ExtraNodesPlugin : EditorPlugin
    {

        public override void _EnterTree()
        {
            base._EnterTree();

            GD.Print("================");
            GD.Print("Extra Nodes (C#)");
            GD.Print("================");

            var throwCast2DScript = GD.Load<Script>("res://addons/ExtraNodesCS/Nodes/ThrowCast2D.cs");
            var throwCast2DTexture = GD.Load<Texture>("res://addons/ExtraNodesCS/Icons/ThrowCast2D.png");
            AddCustomType("ThrowCast2D", "Node2D", throwCast2DScript, throwCast2DTexture);
        }

        public override void _ExitTree()
        {
            RemoveCustomType("ThrowCast2D");
        }

    }

}
#endif
