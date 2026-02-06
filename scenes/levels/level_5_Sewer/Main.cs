using Godot;

namespace Level5Sewer
{
	public partial class Main : Node
	{
		private Node3D _player;
		private Vector3 _initialPlayerPosition;
		
		public override void _Ready()
		{
			// Get player reference
			_player = GetNodeOrNull<Node3D>("Player");
			if (_player != null)
			{
				_initialPlayerPosition = _player.GlobalPosition;
			}
			
			// Connect to FailScreen respawn signal (deferred to ensure FailScreen is ready)
			CallDeferred(MethodName.ConnectFailScreen);
			
			if (TimerManager.Instance != null)
			{
				TimerManager.Instance.StartTimer(180f, "Sewer");
				GD.Print("⏱️ Level 5 timer started");
			}
		}
		
		private void ConnectFailScreen()
		{
			var failScreenNode = GetNodeOrNull<Control>("UI/FailScreen");
			if (failScreenNode != null)
			{
				failScreenNode.Connect("RespawnRequested", new Callable(this, MethodName.OnRespawnRequested));
				GD.Print("✅ Level 5 connected to FailScreen.RespawnRequested");
			}
			else
			{
				GD.PrintErr("❌ FailScreen not found in UI/FailScreen");
			}
		}
		
		private void OnRespawnRequested()
		{
			// Reset player position
			if (_player != null)
			{
				_player.GlobalPosition = _initialPlayerPosition;
				GD.Print("🔄 Player position reset");
			}
		}
	}
}
