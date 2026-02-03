using Godot;
using System;

public partial class BookUI : Control
{
	private Panel _bookPanel;
	private Label _titleLabel;
	private RichTextLabel _leftPageLabel;
	private RichTextLabel _rightPageLabel;
	private Button _closeButton;
	private string _bookTitle = "Book";
	private string _leftPageContent = "";
	private string _rightPageContent = "";
	private FontFile _customFont;
	private InventoryUI _inventoryUI;
	
	// Poster mode
	private bool _isPosterMode = false;
	private TextureRect _posterImage;
	private Label _posterEscLabel;
	private Container _leftPageContainer;
	private Container _rightPageContainer;
	private VSeparator _pageSeparator;

	public override void _Ready()
	{
		// Get InventoryUI reference for crosshair control (try to find in scene tree)
		var root = GetTree().Root;
		if (root.HasNode("Main/UI/InventoryUI"))
		{
			_inventoryUI = root.GetNode<InventoryUI>("Main/UI/InventoryUI");
		}
		else
		{
			GD.PrintErr("InventoryUI not found - crosshair control will be disabled");
		}
		
		// Load custom font
		_customFont = GD.Load<FontFile>("res://assets/fonts/BLKCHCRY.TTF");
		
		// Setup container
		SetAnchorsPreset(LayoutPreset.FullRect);
		Visible = false;
		MouseFilter = MouseFilterEnum.Stop; // Block input when visible
		
		// Semi-transparent background
		var background = new ColorRect();
		background.Color = new Color(0, 0, 0, 0.7f);
		background.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(background);
		
		// Book panel (seperti buku terbuka dengan 2 halaman)
		_bookPanel = new Panel();
		_bookPanel.SetAnchorsPreset(LayoutPreset.Center);
		_bookPanel.CustomMinimumSize = new Vector2(900, 600);
		_bookPanel.Position = new Vector2(-450, -300);
		AddChild(_bookPanel);
		
		// Add stylebox untuk panel (warna kertas)
		var stylebox = new StyleBoxFlat();
		stylebox.BgColor = new Color(0.95f, 0.9f, 0.8f, 1.0f); // Cream/paper color
		stylebox.BorderWidthLeft = 3;
		stylebox.BorderWidthRight = 3;
		stylebox.BorderWidthTop = 3;
		stylebox.BorderWidthBottom = 3;
		stylebox.BorderColor = new Color(0.4f, 0.3f, 0.2f, 1.0f); // Brown border
		_bookPanel.AddThemeStyleboxOverride("panel", stylebox);
		
		var mainVbox = new VBoxContainer();
		mainVbox.SetAnchorsPreset(LayoutPreset.FullRect);
		mainVbox.AddThemeConstantOverride("separation", 15);
		_bookPanel.AddChild(mainVbox);
		
		// Margin container untuk padding
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 30);
		margin.AddThemeConstantOverride("margin_right", 30);
		margin.AddThemeConstantOverride("margin_top", 20);
		margin.AddThemeConstantOverride("margin_bottom", 20);
		mainVbox.AddChild(margin);
		
		var contentVbox = new VBoxContainer();
		contentVbox.AddThemeConstantOverride("separation", 10);
		margin.AddChild(contentVbox);
		
		// Title
		_titleLabel = new Label();
		_titleLabel.Text = "Book Title";
		_titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		if (_customFont != null) _titleLabel.AddThemeFontOverride("font", _customFont);
		_titleLabel.AddThemeFontSizeOverride("font_size", 28);
		contentVbox.AddChild(_titleLabel);
		
		// Separator line
		var separator = new HSeparator();
		contentVbox.AddChild(separator);
		
		// Container for 2 pages (left and right)
		var pagesContainer = new HBoxContainer();
		pagesContainer.AddThemeConstantOverride("separation", 20);
		pagesContainer.CustomMinimumSize = new Vector2(0, 450);
		contentVbox.AddChild(pagesContainer);
		
		// Left page
		_leftPageContainer = new VBoxContainer();
		_leftPageContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		pagesContainer.AddChild(_leftPageContainer);
		
		var leftTitle = new Label();
		leftTitle.Text = "Left Page";
		leftTitle.HorizontalAlignment = HorizontalAlignment.Center;
		if (_customFont != null) leftTitle.AddThemeFontOverride("font", _customFont);
		leftTitle.AddThemeFontSizeOverride("font_size", 16);
		_leftPageContainer.AddChild(leftTitle);
		
		var leftScroll = new ScrollContainer();
		leftScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_leftPageContainer.AddChild(leftScroll);
		
		_leftPageLabel = new RichTextLabel();
		_leftPageLabel.BbcodeEnabled = true;
		_leftPageLabel.FitContent = false;
		_leftPageLabel.ScrollActive = false;
		_leftPageLabel.CustomMinimumSize = new Vector2(400, 400);
		_leftPageLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_leftPageLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_leftPageLabel.AddThemeFontSizeOverride("normal_font_size", 48);
		_leftPageLabel.AddThemeColorOverride("default_color", new Color(0, 0, 0, 1));
		if (_customFont != null) _leftPageLabel.AddThemeFontOverride("normal_font", _customFont);
		leftScroll.AddChild(_leftPageLabel);
		
		// Vertical separator between pages
		_pageSeparator = new VSeparator();
		pagesContainer.AddChild(_pageSeparator);
		
		// Right page
		_rightPageContainer = new VBoxContainer();
		_rightPageContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		pagesContainer.AddChild(_rightPageContainer);
		
		var rightTitle = new Label();
		rightTitle.Text = "Right Page";
		rightTitle.HorizontalAlignment = HorizontalAlignment.Center;
		if (_customFont != null) rightTitle.AddThemeFontOverride("font", _customFont);
		rightTitle.AddThemeFontSizeOverride("font_size", 16);
		_rightPageContainer.AddChild(rightTitle);
		
		var rightScroll = new ScrollContainer();
		rightScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_rightPageContainer.AddChild(rightScroll);
		
		_rightPageLabel = new RichTextLabel();
		_rightPageLabel.BbcodeEnabled = true;
		_rightPageLabel.FitContent = false;
		_rightPageLabel.ScrollActive = false;
		_rightPageLabel.CustomMinimumSize = new Vector2(400, 400);
		_rightPageLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_rightPageLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_rightPageLabel.AddThemeFontSizeOverride("normal_font_size", 48);
		_rightPageLabel.AddThemeColorOverride("default_color", new Color(0, 0, 0, 1));
		if (_customFont != null) _rightPageLabel.AddThemeFontOverride("normal_font", _customFont);
		rightScroll.AddChild(_rightPageLabel);
		
		// Close button
		_closeButton = new Button();
		_closeButton.Text = "Close (ESC)";
		_closeButton.CustomMinimumSize = new Vector2(150, 40);
		_closeButton.Pressed += OnClosePressed;
		
		// Setup cursor hover effect
		_closeButton.MouseEntered += () => CursorManager.Instance?.SetCursor(CursorManager.CursorType.Hover);
		_closeButton.MouseExited += () => CursorManager.Instance?.SetCursor(CursorManager.CursorType.Standard);
		
		var buttonContainer = new HBoxContainer();
		buttonContainer.Alignment = BoxContainer.AlignmentMode.Center;
		buttonContainer.AddChild(_closeButton);
		contentVbox.AddChild(buttonContainer);
		
		// TextureRect for poster image (centered, not fullscreen)
		_posterImage = new TextureRect();
		_posterImage.SetAnchorsPreset(LayoutPreset.Center);
		_posterImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_posterImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_posterImage.Size = new Vector2(480, 600); // Fixed size untuk 1280x720 screen (ratio 4:5)
		_posterImage.Position = new Vector2(-240, -300); // Center offset
		_posterImage.Visible = false;
		_posterImage.MouseFilter = MouseFilterEnum.Stop; // Block clicks
		AddChild(_posterImage);
		
		// ESC hint label for poster
		var posterEscLabel = new Label();
		posterEscLabel.Text = "Press ESC to close";
		posterEscLabel.SetAnchorsPreset(LayoutPreset.BottomWide);
		posterEscLabel.Position = new Vector2(0, -50);
		posterEscLabel.HorizontalAlignment = HorizontalAlignment.Center;
		posterEscLabel.AddThemeFontSizeOverride("font_size", 24);
		posterEscLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
		posterEscLabel.Visible = false;
		AddChild(posterEscLabel);
		
		// Store reference for showing/hiding with poster
		_posterEscLabel = posterEscLabel;
	}

	public override void _Input(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public void ShowBook(string title, string leftContent, string rightContent)
	{
		_isPosterMode = false;
		_bookTitle = title;
		_leftPageContent = string.IsNullOrEmpty(leftContent) ? "(Empty page)" : leftContent;
		_rightPageContent = string.IsNullOrEmpty(rightContent) ? "(Empty page)" : rightContent;
		
		_titleLabel.Text = title;
		
		// Show all book elements, hide poster image
		_bookPanel.Visible = true;
		_titleLabel.Visible = true;
		_leftPageContainer.Visible = true;
		_leftPageLabel.Visible = true;
		_posterImage.Visible = false;
		_posterEscLabel.Visible = false;
		_rightPageContainer.Visible = true;
		_pageSeparator.Visible = true;
		
		// Center align and display content with BBCode
		_leftPageLabel.Text = $"[center]{_leftPageContent}[/center]";
		_rightPageLabel.Text = $"[center]{_rightPageContent}[/center]";
		
		Visible = true;
		InventoryUI.IsAnyPanelOpen = true; // Block player movement
		Input.MouseMode = Input.MouseModeEnum.Visible;
		if (_inventoryUI != null) _inventoryUI.SetCrosshairVisible(false);
		GD.Print($"Opening book: {title} | Left: {_leftPageContent} | Right: {_rightPageContent}");
	}
	
	public void ShowPoster(string title, string imagePath)
	{
		_isPosterMode = true;
		_bookTitle = title;
		
		// Hide ALL UI elements - only show poster image
		_bookPanel.Visible = false;
		_titleLabel.Visible = false;
		_leftPageLabel.Visible = false;
		_posterImage.Visible = true;
		_posterEscLabel.Visible = true;
		_rightPageContainer.Visible = false;
		_pageSeparator.Visible = false;
		_leftPageContainer.Visible = false;
		
		// Load and display image
		var texture = GD.Load<Texture2D>(imagePath);
		if (texture != null)
		{
			_posterImage.Texture = texture;
			GD.Print($"Loaded poster image: {imagePath}");
		}
		else
		{
			GD.PrintErr($"Failed to load poster image: {imagePath}");
		}
		
		Visible = true;
		InventoryUI.IsAnyPanelOpen = true; // Block player movement
		Input.MouseMode = Input.MouseModeEnum.Visible;
		if (_inventoryUI != null) _inventoryUI.SetCrosshairVisible(false);
		GD.Print($"Opening poster: {title} | Image: {imagePath}");
	}

	private void OnClosePressed()
	{
		Close();
	}

	public void Close()
	{
		Visible = false;
		InventoryUI.IsAnyPanelOpen = false; // Restore player movement
		Input.MouseMode = Input.MouseModeEnum.Captured;
		if (_inventoryUI != null) _inventoryUI.SetCrosshairVisible(true);
		GD.Print("Closing book");
	}
}
