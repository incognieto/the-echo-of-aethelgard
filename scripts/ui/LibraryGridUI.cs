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
	private Button _confirmButton;
	private Button _clearButton;
	private Button _closeButton;
	private InventoryUI _inventoryUI;
	private Player _player;
	
	// State management
	private bool _inventoryWasOpen = false;
	
	// Book selection
	private GridContainer _bookSelectionPanel;
	private VBoxContainer _bookSelectionContainer;
	private List<Button> _bookButtons = new List<Button>();
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
	
	// Narasi info (di README atau book terpisah)
	private const string HintText = "Order: Castle → Knight → Princess → Dragon → Sword → Shield → Horse → Skulls → Phoenix";

	public override void _Ready()
	{
		// Load custom font
		_customFont = GD.Load<FontFile>("res://assets/fonts/BLKCHCRY.TTF");
		
		Visible = false;
		
		// Get nodes from scene
		_gridPanel = GetNode<Panel>("GridPanel");
		_titleLabel = GetNode<Label>("GridPanel/TitleLabel");
		_feedbackLabel = GetNode<Label>("GridPanel/FeedbackLabel");
		_confirmButton = GetNode<Button>("GridPanel/ConfirmButton");
		_clearButton = GetNode<Button>("GridPanel/ClearButton");
		_closeButton = GetNode<Button>("GridPanel/CloseButton");
		
		// Setup grid slots
		var gridContainer = GetNode<GridContainer>("GridPanel/GridContainer");
		for (int i = 0; i < 9; i++)
		{
			var slot = new GridSlot(i + 1); // Roman numerals 1-9
			slot.BookPlaced += OnBookPlaced;
			slot.BookRemoved += OnBookRemoved;
			slot.SlotClicked += OnSlotClicked;
			gridContainer.AddChild(slot);
			_gridSlots.Add(slot);
		}
		
		// Connect buttons
		_confirmButton.Pressed += OnConfirmPressed;
		_clearButton.Pressed += OnClearPressed;
		_closeButton.Pressed += OnClosePressed;
		
		// Setup book selection panel
		SetupBookSelectionPanel();
		
		// Find InventoryUI
		CallDeferred(nameof(FindInventoryUI));
		
		GD.Print("LibraryGridUI ready!");
	}
	
	private void SetupBookSelectionPanel()
	{
		// Create container untuk book selection area di kanan grid
		_bookSelectionContainer = new VBoxContainer();
		_bookSelectionContainer.Position = new Vector2(420, 80);
		_gridPanel.AddChild(_bookSelectionContainer);
		
		var selectionTitle = new Label();
		selectionTitle.Text = "Your Books:";
		if (_customFont != null) selectionTitle.AddThemeFontOverride("font", _customFont);
		selectionTitle.AddThemeFontSizeOverride("font_size", 18);
		selectionTitle.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.6f));
		_bookSelectionContainer.AddChild(selectionTitle);
		
		var instruction = new Label();
		instruction.Text = "Click slot, then click book";
		if (_customFont != null) instruction.AddThemeFontOverride("font", _customFont);
		instruction.AddThemeFontSizeOverride("font_size", 12);
		instruction.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
		_bookSelectionContainer.AddChild(instruction);
		
		// Create GridContainer 3x3 untuk book buttons
		_bookSelectionPanel = new GridContainer();
		_bookSelectionPanel.Columns = 3;
		_bookSelectionPanel.AddThemeConstantOverride("h_separation", 5);
		_bookSelectionPanel.AddThemeConstantOverride("v_separation", 5);
		_bookSelectionContainer.AddChild(_bookSelectionPanel);
		
		_bookSelectionContainer.Visible = false;
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
		// Clear existing buttons
		foreach (var btn in _bookButtons)
		{
			btn.QueueFree();
		}
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
		
		// Create button untuk setiap buku dalam urutan yang sudah diacak
		foreach (var symbol in availableBooks)
		{
			var btn = new Button();
			btn.Text = symbol.ToString();
			btn.CustomMinimumSize = new Vector2(100, 100); // Sama dengan ukuran grid slot
			btn.AddThemeFontSizeOverride("font_size", 12);
			
			var capturedSymbol = symbol;
			btn.Pressed += () => OnBookSelected(capturedSymbol);
			
			_bookSelectionPanel.AddChild(btn);
			_bookButtons.Add(btn);
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
		
		// Place book in selected slot
		_gridSlots[_selectedSlotIndex].PlaceBook(symbol);
		
		// Mark book as used
		_usedBooks.Add(symbol);
		
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
	private Label _bookLabel;
	private TextureRect _bookIcon;
	private Dictionary<BookSymbol, Texture2D> _bookTextures;
	private bool _isSelected = false;
	private StyleBoxFlat _styleBox;
	
	public GridSlot(int slotNumber)
	{
		_slotNumber = slotNumber;
		CustomMinimumSize = new Vector2(100, 100);
		
		// Panel styling
		_styleBox = new StyleBoxFlat();
		_styleBox.BgColor = new Color(0.2f, 0.2f, 0.25f);
		_styleBox.BorderColor = new Color(0.6f, 0.5f, 0.3f);
		_styleBox.BorderWidthLeft = 2;
		_styleBox.BorderWidthRight = 2;
		_styleBox.BorderWidthTop = 2;
		_styleBox.BorderWidthBottom = 2;
		AddThemeStyleboxOverride("panel", _styleBox);
	}
	
	public override void _Ready()
	{
		var font = GD.Load<FontFile>("res://assets/fonts/BLKCHCRY.TTF");
		
		// Roman numeral label
		_romanLabel = new Label();
		_romanLabel.Text = ToRoman(_slotNumber);
		if (font != null) _romanLabel.AddThemeFontOverride("font", font);
		_romanLabel.AddThemeFontSizeOverride("font_size", 24);
		_romanLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.7f, 0.5f));
		_romanLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_romanLabel.AnchorRight = 1.0f;
		_romanLabel.AnchorBottom = 0.3f;
		AddChild(_romanLabel);
		
		// Book name label
		_bookLabel = new Label();
		if (font != null) _bookLabel.AddThemeFontOverride("font", font);
		_bookLabel.AddThemeFontSizeOverride("font_size", 12);
		_bookLabel.AddThemeColorOverride("font_color", Colors.White);
		_bookLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_bookLabel.VerticalAlignment = VerticalAlignment.Center;
		_bookLabel.AnchorTop = 0.4f;
		_bookLabel.AnchorRight = 1.0f;
		_bookLabel.AnchorBottom = 1.0f;
		_bookLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_bookLabel.Visible = false;
		AddChild(_bookLabel);
		
		// Book icon area
		_bookIcon = new TextureRect();
		_bookIcon.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
		_bookIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_bookIcon.AnchorTop = 0.3f;
		_bookIcon.AnchorRight = 1.0f;
		_bookIcon.AnchorBottom = 1.0f;
		AddChild(_bookIcon);
		
		// Load book textures (placeholder - seharusnya load dari assets)
		_bookTextures = new Dictionary<BookSymbol, Texture2D>();
		// TODO: Load actual book cover textures
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
		
		if (_isSelected)
		{
			_styleBox.BgColor = new Color(0.3f, 0.3f, 0.45f); // Highlight color
			_styleBox.BorderColor = new Color(1f, 0.8f, 0.3f); // Gold border
		}
		else
		{
			if (_currentBook.HasValue)
			{
				_styleBox.BgColor = new Color(0.3f, 0.4f, 0.3f); // Filled color
			}
			else
			{
				_styleBox.BgColor = new Color(0.2f, 0.2f, 0.25f); // Empty color
			}
			_styleBox.BorderColor = new Color(0.6f, 0.5f, 0.3f); // Normal border
		}
	}
	
	public void PlaceBook(BookSymbol symbol)
	{
		_currentBook = symbol;
		
		// Update icon (placeholder)
		// _bookIcon.Texture = _bookTextures.GetValueOrDefault(symbol);
		
		// Visual feedback
		var styleBox = GetThemeStylebox("panel") as StyleBoxFlat;
		if (styleBox != null)
		{
			styleBox.BgColor = new Color(0.3f, 0.4f, 0.3f); // Greenish when filled
		}
		
		EmitSignal(SignalName.BookPlaced, _slotNumber - 1, (int)symbol);
	}
	
	public void ClearSlot()
	{
		_currentBook = null;
		_bookIcon.Texture = null;
		_bookLabel.Visible = false;
		
		// Reset visual
		_styleBox.BgColor = new Color(0.2f, 0.2f, 0.25f);
		
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
