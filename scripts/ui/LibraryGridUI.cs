using Godot;
using System;
using System.Collections.Generic;

public partial class LibraryGridUI : Control
{
	[Signal]
	public delegate void GridCompletedEventHandler();
	
	private const int GridSize = 3; // 3x3 grid
	
	private Panel _gridPanel;
	private Label _titleLabel;
	private Label _feedbackLabel;
	private Label _escInstructionLabel;
	private TextureButton _confirmButton;
	private TextureButton _clearButton;
	private TextureButton _closeButton;
	private InventoryUI _inventoryUI;
	private Player _player;
	
	// State management
	private bool _inventoryWasOpen = false;
	
	// Book selection
	private Control _bookSelectionPanel;
	private VBoxContainer _bookSelectionContainer;
	private List<TextureButton> _bookButtons = new List<TextureButton>();
	private int _selectedSlotIndex = -1; // Slot yang dipilih untuk diisi
	
	// Grid slots (9 slots untuk 3x3)
	private List<GridSlot> _gridSlots = new List<GridSlot>();
	private BookSymbol?[] _placedBooks = new BookSymbol?[9]; // null = empty slot
	
	// Public property untuk sequence yang di-submit
	public BookSymbol[] SubmittedSequence { get; private set; }
	
	// Track buku yang sudah digunakan
	private HashSet<BookSymbol> _usedBooks = new HashSet<BookSymbol>();
	
	// Custom font
	private FontFile _customFont;
	
	// Textures for UI elements
	private Dictionary<BookSymbol, Texture2D> _bookTextures;
	private Texture2D _slotTexture;
	private Texture2D _clearButtonTexture;
	private Texture2D _clearButtonClickedTexture;
	private Texture2D _confirmButtonTexture;
	private Texture2D _confirmButtonClickedTexture;
	private Texture2D _backgroundTexture;
	private TextureRect _backgroundSprite;
	
	// Narasi info (di README atau book terpisah)
	private const string HintText = "Order: Castle → Knight → Princess → Dragon → Sword → Shield → Horse → Skulls → Phoenix";

	public override void _Ready()
	{
		// Load custom font
		_customFont = GD.Load<FontFile>("res://assets/fonts/BLKCHCRY.TTF");
		
		// Load textures
		LoadTextures();
		
		Visible = false;
		
		// Get nodes from scene
		_gridPanel = GetNode<Panel>("GridPanel");
		_titleLabel = GetNode<Label>("GridPanel/TitleLabel");
		_feedbackLabel = GetNode<Label>("GridPanel/FeedbackLabel");
		_confirmButton = GetNode<TextureButton>("GridPanel/ConfirmButton");
		_clearButton = GetNode<TextureButton>("GridPanel/ClearButton");
		_closeButton = GetNode<TextureButton>("GridPanel/CloseButton");
		
		// Get book selection nodes from scene
		_bookSelectionContainer = GetNode<VBoxContainer>("GridPanel/BookSelectionContainer");
		_bookSelectionPanel = GetNode<Control>("GridPanel/BookSelectionContainer/BookGrid");
		
		// Setup grid slots - get manual nodes instead of creating in GridContainer
		var gridContainer = GetNode<Control>("GridPanel/GridContainer");
		for (int i = 0; i < 9; i++)
		{
			// Get the slot node from scene (Slot1, Slot2, ... Slot9)
			var slotNode = gridContainer.GetNode<Control>($"Slot{i + 1}");
			
			var slot = new GridSlot(i + 1, _slotTexture, _bookTextures);
			slot.BookPlaced += OnBookPlaced;
			slot.BookRemoved += OnBookRemoved;
			slot.SlotClicked += OnSlotClicked;
			
			// Add GridSlot as child of the empty Control node
			slotNode.AddChild(slot);
			_gridSlots.Add(slot);
		}
		
		// Connect buttons
		_confirmButton.Pressed += OnConfirmPressed;
		_clearButton.Pressed += OnClearPressed;
		_closeButton.Pressed += OnClosePressed;
		
		// Create ESC instruction label
		_escInstructionLabel = new Label();
		_escInstructionLabel.Text = "(Esc) to return";
		_escInstructionLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_escInstructionLabel.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f, 0.8f));
		_escInstructionLabel.AddThemeFontSizeOverride("font_size", 18);
		_escInstructionLabel.Position = new Vector2(10, 10);
		_gridPanel.AddChild(_escInstructionLabel);
		
		// Find InventoryUI
		CallDeferred(nameof(FindInventoryUI));
		
		GD.Print("LibraryGridUI ready!");
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			if (Visible)
			{
				OnClosePressed();
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	private void LoadTextures()
	{
		// Load book textures
		_bookTextures = new Dictionary<BookSymbol, Texture2D>();
		_bookTextures[BookSymbol.Castle] = GD.Load<Texture2D>("res://assets/sprites/library/book_of_castle.png");
		_bookTextures[BookSymbol.Knight] = GD.Load<Texture2D>("res://assets/sprites/library/book_of knight.png");
		_bookTextures[BookSymbol.Princess] = GD.Load<Texture2D>("res://assets/sprites/library/book_of_princess.png");
		_bookTextures[BookSymbol.Dragon] = GD.Load<Texture2D>("res://assets/sprites/library/book_of_dragon.png");
		_bookTextures[BookSymbol.Sword] = GD.Load<Texture2D>("res://assets/sprites/library/book_of_sword.png");
		_bookTextures[BookSymbol.Shield] = GD.Load<Texture2D>("res://assets/sprites/library/book_of_shield.png");
		_bookTextures[BookSymbol.Horse] = GD.Load<Texture2D>("res://assets/sprites/library/book_of_horse.png");
		_bookTextures[BookSymbol.Skulls] = GD.Load<Texture2D>("res://assets/sprites/library/book_of_skull.png");
		_bookTextures[BookSymbol.Phoenix] = GD.Load<Texture2D>("res://assets/sprites/library/book_of_phoenix.png");
		
		// Load slot texture
		_slotTexture = GD.Load<Texture2D>("res://assets/sprites/library/slot_1x1_ui_stone_table.png");
		
		// Load button textures
		_clearButtonTexture = GD.Load<Texture2D>("res://assets/sprites/library/button_clear.png");
		_clearButtonClickedTexture = GD.Load<Texture2D>("res://assets/sprites/library/button_clear_clicked.png");
		_confirmButtonTexture = GD.Load<Texture2D>("res://assets/sprites/library/button_confirm.png");
		_confirmButtonClickedTexture = GD.Load<Texture2D>("res://assets/sprites/library/button_confirm_clicked.png");
		
		GD.Print("LibraryGridUI: All textures loaded!");
	}
	
	
	private void FindInventoryUI()
	{
		var canvasLayer = GetParent() as CanvasLayer;
		if (canvasLayer != null)
		{
			_inventoryUI = canvasLayer.GetNodeOrNull<InventoryUI>("InventoryUI");
			if (_inventoryUI == null)
			{
				GD.PrintErr("LibraryGridUI: InventoryUI not found!");
			}
		}
	}
	
	public void OpenGrid(Player player)
	{
		_player = player;
		Visible = true;
		InventoryUI.IsAnyPanelOpen = true; // Set global flag
		_feedbackLabel.Text = "Left-click slot → Left-click book to place | Right-click slot to remove";
		_feedbackLabel.Modulate = Colors.White;
		
		// Show mouse cursor
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// Disable player movement
		if (_player != null)
		{
			_player.SetMeta("inventory_open", true);
		}
		
		// Hide crosshair
		if (_inventoryUI != null)
		{
			_inventoryUI.SetCrosshairVisible(false);
		}
		
		// Save inventory state and close inventory panel if open
		if (_inventoryUI != null)
		{
			// Check if full inventory panel is open (not just hotbar)
			var invPanel = _inventoryUI.GetNodeOrNull<Panel>("InventoryPanel");
			_inventoryWasOpen = invPanel != null && invPanel.Visible;
			
			// Close inventory panel if it's open
			if (_inventoryWasOpen && invPanel != null)
			{
				invPanel.Visible = false;
			}
		}
		
		// Reset grid
		ClearGrid();
		
		// Update book selection list
		UpdateBookSelection();
		
		GD.Print("Grid opened!");
	}
	
	public void CloseGrid()
	{
		Visible = false;
		InventoryUI.IsAnyPanelOpen = false; // Clear global flag
		
		// Hide mouse cursor
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Re-enable player movement
		if (_player != null)
		{
			_player.SetMeta("inventory_open", false);
		}
		
		// Show crosshair
		if (_inventoryUI != null)
		{
			_inventoryUI.SetCrosshairVisible(true);
			
			// Restore inventory panel state if it was open
			if (_inventoryWasOpen)
			{
				var invPanel = _inventoryUI.GetNodeOrNull<Panel>("InventoryPanel");
				if (invPanel != null)
				{
					invPanel.Visible = true;
				}
				_inventoryWasOpen = false;
			}
		}
		
		// Reset selection
		_selectedSlotIndex = -1;
		
		GD.Print("Grid closed!");
	}
	
	private void ClearGrid()
	{
		for (int i = 0; i < 9; i++)
		{
			_placedBooks[i] = null;
			_gridSlots[i].ClearSlot();
		}
		
		// Reset used books (tapi jangan reset _ownedBooksOrder karena itu urutan pengambilan)
		_usedBooks.Clear();
		
		// Update book list
		UpdateBookSelection();
		
		_feedbackLabel.Text = "Grid cleared. Place books from your inventory.";
		_feedbackLabel.Modulate = Colors.White;
	}
	
	private void UpdateBookSelection()
	{
		// Disconnect all previous button connections before clearing list
		foreach (var btn in _bookButtons)
		{
			if (btn != null && !btn.IsQueuedForDeletion())
			{
				try
				{
					btn.Pressed -= OnBookButtonPressed;
				}
				catch { /* Ignore if already disconnected */ }
			}
		}
		
		// Clear existing button references (don't queue free scene nodes)
		_bookButtons.Clear();
		
		if (_player == null || _player._inventory == null)
		{
			_bookSelectionContainer.Visible = false;
			return;
		}
		
		// Get all available books from inventory
		var inventory = _player._inventory;
		var availableBooks = new List<BookSymbol>();
		
		// Cek semua buku di inventory yang belum digunakan
		for (int i = 0; i < 9; i++)
		{
			var symbol = (BookSymbol)i;
			var itemId = $"book_{symbol.ToString().ToLower()}";
			
			// Hanya tampilkan jika ada di inventory dan belum digunakan
			if (inventory.HasItem(itemId) && !_usedBooks.Contains(symbol))
			{
				availableBooks.Add(symbol);
			}
		}
		
		if (availableBooks.Count == 0)
		{
			_bookSelectionContainer.Visible = false;
			_feedbackLabel.Text = "You don't have any story books yet! Explore the library to find them.";
			_feedbackLabel.Modulate = new Color(1, 0.5f, 0);
			return;
		}
		
		_bookSelectionContainer.Visible = true;
		
		// SHUFFLE urutan buku agar tidak berurutan sesuai solusi!
		// Ini membuat puzzle lebih menantang karena pemain tidak bisa mengandalkan urutan tombol
		var random = new Random();
		for (int i = availableBooks.Count - 1; i > 0; i--)
		{
			int j = random.Next(i + 1);
			var temp = availableBooks[i];
			availableBooks[i] = availableBooks[j];
			availableBooks[j] = temp;
		}
		
		GD.Print($"📚 Book buttons shuffled: {string.Join(", ", availableBooks)}");
		
		// Assign textures to book button nodes (BookButton1 to BookButton9)
		for (int i = 0; i < availableBooks.Count && i < 9; i++)
		{
			var symbol = availableBooks[i];
			var btnNode = _bookSelectionPanel.GetNodeOrNull<TextureButton>($"BookButton{i + 1}");
			
			if (btnNode != null)
			{
				btnNode.Visible = true;
				btnNode.IgnoreTextureSize = true;
				btnNode.StretchMode = TextureButton.StretchModeEnum.Scale;
				
				// Set book texture
				if (_bookTextures != null && _bookTextures.ContainsKey(symbol))
				{
					var texture = _bookTextures[symbol];
					btnNode.TextureNormal = texture;
					btnNode.TextureHover = texture;
					btnNode.TexturePressed = texture;
				}
				
				// Store symbol in metadata
				btnNode.SetMeta("book_symbol", (int)symbol);
				btnNode.Pressed += OnBookButtonPressed;
				
				_bookButtons.Add(btnNode);
			}
		}
		
		// Hide unused button nodes
		for (int i = availableBooks.Count; i < 9; i++)
		{
			var btnNode = _bookSelectionPanel.GetNodeOrNull<TextureButton>($"BookButton{i + 1}");
			if (btnNode != null)
			{
				btnNode.Visible = false;
			}
		}
	}
	
	private void OnBookButtonPressed()
	{
		// Get the button that was pressed
		var btn = (TextureButton)GetViewport().GuiGetFocusOwner();
		if (btn != null && btn.HasMeta("book_symbol"))
		{
			var symbol = (BookSymbol)(int)btn.GetMeta("book_symbol");
			OnBookSelected(symbol);
		}
	}
	
	private void OnBookSelected(BookSymbol symbol)
	{
		if (_selectedSlotIndex < 0)
		{
			_feedbackLabel.Text = "Click a slot (I-IX) first, then select a book.";
			_feedbackLabel.Modulate = new Color(1, 0.7f, 0);
			return;
		}
		
		// Check if slot is already filled
		if (_placedBooks[_selectedSlotIndex].HasValue)
		{
			_feedbackLabel.Text = "That slot is already filled! Right-click to remove the book first, or select another slot.";
			_feedbackLabel.Modulate = new Color(1, 0.5f, 0);
			return;
		}
		
		// Place book in selected slot
		_gridSlots[_selectedSlotIndex].PlaceBook(symbol);
		
		// Mark book as used
		_usedBooks.Add(symbol);
		
		// Update placed books tracking
		_placedBooks[_selectedSlotIndex] = symbol;
		
		_selectedSlotIndex = -1;
		
		// Update all slot highlights
		for (int i = 0; i < 9; i++)
		{
			_gridSlots[i].SetSelected(false);
		}
		
		// Refresh book list
		UpdateBookSelection();
		
		_feedbackLabel.Text = $"{symbol} placed. Continue placing books or click Confirm when done.";
		_feedbackLabel.Modulate = Colors.LimeGreen;
	}
	
	private void OnBookPlaced(int slotIndex, BookSymbol symbol)
	{
		if (slotIndex >= 0 && slotIndex < 9)
		{
			_placedBooks[slotIndex] = symbol;
			GD.Print($"Book {symbol} placed in slot {slotIndex + 1}");
		}
	}
	
	private void OnBookRemoved(int slotIndex)
	{
		if (slotIndex >= 0 && slotIndex < 9)
		{
			// Remove book from used list if it was placed
			if (_placedBooks[slotIndex].HasValue)
			{
				_usedBooks.Remove(_placedBooks[slotIndex].Value);
			}
			
			_placedBooks[slotIndex] = null;
			GD.Print($"Book removed from slot {slotIndex + 1}");
			
			// Refresh book list
			UpdateBookSelection();
		}
	}
	
	private void OnSlotClicked(int slotIndex)
	{
		// Deselect all slots
		for (int i = 0; i < 9; i++)
		{
			_gridSlots[i].SetSelected(false);
		}
		
		// Select this slot
		_selectedSlotIndex = slotIndex;
		_gridSlots[slotIndex].SetSelected(true);
		
		var romanNumeral = _gridSlots[slotIndex].GetRomanNumeral();
		_feedbackLabel.Text = $"Slot {romanNumeral} selected. Now click a book from the list.";
		_feedbackLabel.Modulate = new Color(0.5f, 0.8f, 1f);
	}
	
	private void OnConfirmPressed()
	{
		// Check if all slots are filled
		int filledCount = 0;
		for (int i = 0; i < 9; i++)
		{
			if (_placedBooks[i].HasValue)
			{
				filledCount++;
			}
		}
		
		if (filledCount < 9)
		{
			_feedbackLabel.Text = $"Only {filledCount}/9 slots filled. Place all books before confirming.";
			_feedbackLabel.Modulate = new Color(1, 0.7f, 0); // Orange warning
			return;
		}
		
		// Create sequence array
		var sequence = new BookSymbol[9];
		for (int i = 0; i < 9; i++)
		{
			sequence[i] = _placedBooks[i].Value;
		}
		
		// Store sequence and emit signal
		SubmittedSequence = sequence;
		EmitSignal(SignalName.GridCompleted);
		
		GD.Print("Grid confirmed! Sequence submitted.");
	}
	
	private void OnClearPressed()
	{
		ClearGrid();
	}
	
	private void OnClosePressed()
	{
		CloseGrid();
	}
	
	public void ShowResult(bool success, string message)
	{
		_feedbackLabel.Text = message;
		_feedbackLabel.Modulate = success ? Colors.LimeGreen : Colors.Red;
		
		if (success)
		{
			// Disable buttons after success
			_confirmButton.Disabled = true;
			_clearButton.Disabled = true;
			
			// Auto-close after delay
			GetTree().CreateTimer(3.0).Timeout += CloseGrid;
		}
	}
	
	// Helper method untuk menempatkan buku dari inventory
	public void PlaceBookFromInventory(int slotIndex, string bookItemId)
	{
		// Parse book symbol from item ID
		var symbolStr = bookItemId.Replace("book_", "");
		
		if (Enum.TryParse<BookSymbol>(symbolStr, true, out var symbol))
		{
			if (slotIndex >= 0 && slotIndex < 9)
			{
				_gridSlots[slotIndex].PlaceBook(symbol);
			}
		}
	}
}

// Custom control untuk grid slot
public partial class GridSlot : Panel
{
	[Signal]
	public delegate void BookPlacedEventHandler(int slotIndex, BookSymbol symbol);
	
	[Signal]
	public delegate void BookRemovedEventHandler(int slotIndex);
	
	[Signal]
	public delegate void SlotClickedEventHandler(int slotIndex);
	
	private int _slotNumber; // 1-9 untuk Roman numerals
	private BookSymbol? _currentBook;
	private Label _romanLabel;
	private TextureRect _slotBackground;
	private TextureRect _bookIcon;
	private Dictionary<BookSymbol, Texture2D> _bookTextures;
	private Texture2D _slotTexture;
	private bool _isSelected = false;
	private Panel _highlight;
	
	public GridSlot(int slotNumber, Texture2D slotTexture, Dictionary<BookSymbol, Texture2D> bookTextures)
	{
		_slotNumber = slotNumber;
		_slotTexture = slotTexture;
		_bookTextures = bookTextures;
		
		// Match the size set in scene (60x60)
		SetAnchorsPreset(Control.LayoutPreset.FullRect);
		
		// No panel styling - use texture instead
		MouseFilter = MouseFilterEnum.Pass;
	}
	
	public override void _Ready()
	{
		var font = GD.Load<FontFile>("res://assets/fonts/BLKCHCRY.TTF");
		
		// Highlight panel (for selection)
		_highlight = new Panel();
		var highlightStyle = new StyleBoxFlat();
		highlightStyle.BgColor = new Color(1, 0.8f, 0.3f, 0.3f);
		highlightStyle.BorderColor = new Color(1, 0.8f, 0.3f);
		highlightStyle.BorderWidthLeft = 3;
		highlightStyle.BorderWidthRight = 3;
		highlightStyle.BorderWidthTop = 3;
		highlightStyle.BorderWidthBottom = 3;
		_highlight.AddThemeStyleboxOverride("panel", highlightStyle);
		_highlight.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_highlight.MouseFilter = MouseFilterEnum.Ignore;
		_highlight.Visible = false;
		AddChild(_highlight);
		
		// Book icon area
		_bookIcon = new TextureRect();
		_bookIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_bookIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_bookIcon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_bookIcon.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(_bookIcon);
	}
	
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				// Left click - select slot
				EmitSignal(SignalName.SlotClicked, _slotNumber - 1);
				AcceptEvent();
			}
			else if (mouseEvent.ButtonIndex == MouseButton.Right && _currentBook.HasValue)
			{
				// Right click - remove book
				ClearSlot();
				AcceptEvent();
			}
		}
	}
	
	public void SetSelected(bool selected)
	{
		_isSelected = selected;
		
		if (_highlight != null)
		{
			_highlight.Visible = _isSelected;
		}
	}
	
	public void PlaceBook(BookSymbol symbol)
	{
		_currentBook = symbol;
		
		// Update icon with book texture
		if (_bookTextures != null && _bookTextures.ContainsKey(symbol))
		{
			_bookIcon.Texture = _bookTextures[symbol];
		}
		
		EmitSignal(SignalName.BookPlaced, _slotNumber - 1, (int)symbol);
	}
	
	public void ClearSlot()
	{
		_currentBook = null;
		if (_bookIcon != null)
		{
			_bookIcon.Texture = null;
		}
		
		EmitSignal(SignalName.BookRemoved, _slotNumber - 1);
	}
	
	public string GetRomanNumeral()
	{
		return ToRoman(_slotNumber);
	}
	
	private string ToRoman(int number)
	{
		return number switch
		{
			1 => "I",
			2 => "II",
			3 => "III",
			4 => "IV",
			5 => "V",
			6 => "VI",
			7 => "VII",
			8 => "VIII",
			9 => "IX",
			_ => number.ToString()
		};
	}
}
