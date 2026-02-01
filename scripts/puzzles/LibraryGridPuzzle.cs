using Godot;
using System;

// Library Grid Puzzle untuk level 4
public partial class LibraryGridPuzzle : StaticBody3D
{
	// Urutan yang benar dari narasi (1-9)
	private readonly BookSymbol[] CorrectSequence = new BookSymbol[]
	{
		BookSymbol.Castle,    // 1
		BookSymbol.Knight,    // 2
		BookSymbol.Princess,  // 3
		BookSymbol.Dragon,    // 4
		BookSymbol.Sword,     // 5
		BookSymbol.Shield,    // 6
		BookSymbol.Horse,     // 7
		BookSymbol.Skulls,    // 8
		BookSymbol.Phoenix    // 9
	};
	
	[Export]
	public float InteractionRange { get; set; } = 3.0f;
	
	[Export]
	public NodePath BookshelfPath { get; set; } // Path ke bookshelf yang akan bergeser
	
	[Export]
	public Vector3 BookshelfOffset { get; set; } = new Vector3(0, 0, 5); // Offset pergeseran
	
	[Export]
	public float BookshelfSpeed { get; set; } = 1.5f;
	
	private Label3D _promptLabel;
	private Area3D _interactionArea;
	private bool _playerNearby = false;
	private bool _isPuzzleSolved = false;
	private bool _isBookshelfMoving = false;
	private Node3D _bookshelf;
	private Vector3 _bookshelfStartPos;
	private Vector3 _bookshelfTargetPos;
	private LibraryGridUI _gridUI;
	private Player _player;

	public override void _Ready()
	{
		// Setup interaction area
		CallDeferred(nameof(SetupInteractionArea));
		
		// Find Grid UI
		CallDeferred(nameof(FindGridUI));
		
		// Find bookshelf
		if (BookshelfPath != null && !BookshelfPath.IsEmpty)
		{
			_bookshelf = GetNode<Node3D>(BookshelfPath);
			if (_bookshelf != null)
			{
				_bookshelfStartPos = _bookshelf.GlobalPosition;
				_bookshelfTargetPos = _bookshelfStartPos + BookshelfOffset;
			}
		}
		
		GD.Print("LibraryGridPuzzle initialized - 3x3 Symbol Grid");
		GD.Print($"Correct sequence: {string.Join(", ", CorrectSequence)}");
	}

	private void SetupInteractionArea()
	{
		// Create interaction area
		_interactionArea = new Area3D();
		AddChild(_interactionArea);
		
		var collisionShape = new CollisionShape3D();
		var sphereShape = new SphereShape3D();
		sphereShape.Radius = InteractionRange;
		collisionShape.Shape = sphereShape;
		_interactionArea.AddChild(collisionShape);
		
		_interactionArea.BodyEntered += OnBodyEntered;
		_interactionArea.BodyExited += OnBodyExited;
		
		// Create prompt label
		_promptLabel = new Label3D();
		_promptLabel.Text = "[E] Circular Stone Table";
		_promptLabel.Modulate = new Color(0.8f, 0.7f, 0.5f); // Golden/stone color
		_promptLabel.OutlineModulate = Colors.Black;
		_promptLabel.OutlineSize = 12;
		_promptLabel.FontSize = 32;
		_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_promptLabel.Position = new Vector3(0, 1.5f, 0);
		_promptLabel.Visible = false;
		AddChild(_promptLabel);
		
		GD.Print("✓ LibraryGridPuzzle interaction area setup complete!");
	}

	private void FindGridUI()
	{
		var canvasLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("/root/Main/UI");
		if (canvasLayer != null)
		{
			_gridUI = canvasLayer.GetNodeOrNull<LibraryGridUI>("LibraryGridUI");
			if (_gridUI != null)
			{
				_gridUI.GridCompleted += OnGridCompleted;
				GD.Print("✓ LibraryGridPuzzle: LibraryGridUI connected!");
			}
			else
			{
				GD.PrintErr("✗ LibraryGridPuzzle: LibraryGridUI not found!");
			}
		}
		else
		{
			GD.PrintErr("✗ LibraryGridPuzzle: CanvasLayer not found!");
		}
	}

	public override void _Process(double delta)
	{
		// Update prompt visibility
		if (_promptLabel != null)
		{
			if (!_isPuzzleSolved && _playerNearby)
			{
				_promptLabel.Visible = true;
				var alpha = Mathf.Abs(Mathf.Sin((float)Time.GetTicksMsec() / 500.0f));
				_promptLabel.Modulate = new Color(0.8f, 0.7f, 0.5f, Mathf.Lerp(0.6f, 1.0f, alpha));
			}
			else
			{
				_promptLabel.Visible = false;
			}
		}
		
		// Handle bookshelf movement
		if (_isBookshelfMoving && _bookshelf != null)
		{
			var currentPos = _bookshelf.GlobalPosition;
			var newPos = currentPos.Lerp(_bookshelfTargetPos, BookshelfSpeed * (float)delta);
			_bookshelf.GlobalPosition = newPos;
			
			// Check if reached target
			if (currentPos.DistanceTo(_bookshelfTargetPos) < 0.1f)
			{
				_bookshelf.GlobalPosition = _bookshelfTargetPos;
				_isBookshelfMoving = false;
				GD.Print("✓ Secret passage revealed!");
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_playerNearby && !_isPuzzleSolved && @event.IsActionPressed("interact"))
		{
			OpenGridUI();
		}
	}

	private void OpenGridUI()
	{
		if (_gridUI == null || _player == null)
		{
			GD.PrintErr("Cannot open grid UI - references missing");
			return;
		}
		
		GD.Print("Opening Library Grid UI...");
		_gridUI.OpenGrid(_player);
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			_playerNearby = true;
			_player = player;
			GD.Print("Player near stone table");
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_playerNearby = false;
			_player = null;
			GD.Print("Player left stone table");
		}
	}

	private void OnGridCompleted()
	{
		// Get sequence from UI
		if (_gridUI == null || _gridUI.SubmittedSequence == null)
		{
			GD.PrintErr("Cannot get sequence from UI!");
			return;
		}
		
		var playerSequence = _gridUI.SubmittedSequence;
		
		GD.Print($"Grid completed! Checking sequence...");
		GD.Print($"Player: {string.Join(", ", playerSequence)}");
		GD.Print($"Correct: {string.Join(", ", CorrectSequence)}");
		
		// Check if sequence is correct
		bool isCorrect = true;
		if (playerSequence.Length != CorrectSequence.Length)
		{
			isCorrect = false;
		}
		else
		{
			for (int i = 0; i < CorrectSequence.Length; i++)
			{
				if (playerSequence[i] != CorrectSequence[i])
				{
					isCorrect = false;
					break;
				}
			}
		}
		
		if (isCorrect)
		{
			GD.Print("✓ PUZZLE SOLVED! Opening secret passage...");
			_isPuzzleSolved = true;
			
			// Start moving bookshelf
			if (_bookshelf != null)
			{
				_isBookshelfMoving = true;
			}
			
			// Show success message
			if (_gridUI != null)
			{
				_gridUI.ShowResult(true, "The ancient mechanism activates...\nA secret passage revealed!");
			}
		}
		else
		{
			GD.Print("✗ Wrong sequence!");
			if (_gridUI != null)
			{
				_gridUI.ShowResult(false, "The books tremble but nothing happens...\nCheck the narrative order.");
			}
		}
	}
}
