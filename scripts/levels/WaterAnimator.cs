using Godot;

public partial class WaterAnimator : Node3D
{
    private AnimationPlayer _animPlayer;

    public override void _Ready()
    {
        // Cari AnimationPlayer child node
        _animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        
        if (_animPlayer != null)
        {
            // Play animation water_flow dengan loop
            _animPlayer.Play("water_flow");
            GD.Print("✓ Water flow animation started");
        }
        else
        {
            GD.PrintErr("❌ AnimationPlayer not found! Make sure AnimationPlayer is a child of water node.");
        }
    }
}
