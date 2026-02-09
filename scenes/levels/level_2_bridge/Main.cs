using Godot;

namespace Level2Bridge
{
	public partial class Main : Node
	{
		private Node3D _player;
		private Vector3 _initialPlayerPosition;
		private FailScreen _failScreen;
		private BridgePuzzle _bridgePuzzle;
		private const float VOID_THRESHOLD = -20f; // Y position di mana player dianggap jatuh ke void
		private bool _hasTriggeredVoidFail = false;
		
		public override void _Ready()
		{
			// Get player reference
			_player = GetNodeOrNull<Node3D>("Player");
			if (_player != null)
			{
				_initialPlayerPosition = _player.GlobalPosition;
			}
			
			// Get BridgePuzzle reference (it's the BridgeController node)
			_bridgePuzzle = GetNodeOrNull<BridgePuzzle>("Bridge1/BridgeController");
			if (_bridgePuzzle != null)
			{
				GD.Print("✅ BridgePuzzle reference obtained");
			}
			else
			{
				GD.PrintErr("❌ BridgePuzzle not found in level!");
			}
			
			// Connect to FailScreen respawn signal (deferred to ensure FailScreen is ready)
			CallDeferred(MethodName.ConnectFailScreen);
			
			if (TimerManager.Instance != null)
			{
				TimerManager.Instance.StartTimer(600f, "Bridge");
				GD.Print("⏱️ Level 2 timer started");
			}
		}
		
		private void ConnectFailScreen()
		{
			_failScreen = GetNodeOrNull<FailScreen>("UI/FailScreen");
			if (_failScreen != null)
			{
				_failScreen.Connect("RespawnRequested", new Callable(this, MethodName.OnRespawnRequested));
				GD.Print("✅ Level 2 connected to FailScreen.RespawnRequested");
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
			
			// Reset bridge puzzle so player can try again
			if (_bridgePuzzle != null)
			{
				_bridgePuzzle.ResetPuzzle();
				GD.Print("🔄 Bridge puzzle reset");
			}
			
			// Reset void fail flag untuk respawn
			_hasTriggeredVoidFail = false;
		}
		
		public override void _Process(double delta)
		{
			// Check if player fell into void
			if (_player != null && !_hasTriggeredVoidFail && _player.GlobalPosition.Y < VOID_THRESHOLD)
			{
				GD.Print($"💧 Player fell into void! Y position: {_player.GlobalPosition.Y}");
				_hasTriggeredVoidFail = true;
				
				// Trigger fail screen
				if (_failScreen != null)
				{
					_failScreen.OnPlayerFellInVoid();
				}
				else
				{
					GD.PrintErr("❌ FailScreen not available for void fall trigger");
				}
			}
		}
	}
}
