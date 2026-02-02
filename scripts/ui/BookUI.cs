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

	public override void _Ready()
	{
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
		var leftPageContainer = new VBoxContainer();
		leftPageContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		pagesContainer.AddChild(leftPageContainer);
		
		var leftTitle = new Label();
		leftTitle.Text = "Left Page";
		leftTitle.HorizontalAlignment = HorizontalAlignment.Center;
		if (_customFont != null) leftTitle.AddThemeFontOverride("font", _customFont);
		leftTitle.AddThemeFontSizeOverride("font_size", 16);
		leftPageContainer.AddChild(leftTitle);
		
		var leftScroll = new ScrollContainer();
		leftScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		leftPageContainer.AddChild(leftScroll);
		
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
		var pageSeparator = new VSeparator();
		pagesContainer.AddChild(pageSeparator);
		
		// Right page
		var rightPageContainer = new VBoxContainer();
		rightPageContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		pagesContainer.AddChild(rightPageContainer);
		
		var rightTitle = new Label();
		rightTitle.Text = "Right Page";
		rightTitle.HorizontalAlignment = HorizontalAlignment.Center;
		if (_customFont != null) rightTitle.AddThemeFontOverride("font", _customFont);
		rightTitle.AddThemeFontSizeOverride("font_size", 16);
		rightPageContainer.AddChild(rightTitle);
		
		var rightScroll = new ScrollContainer();
		rightScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		rightPageContainer.AddChild(rightScroll);
		
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
		
		var buttonContainer = new HBoxContainer();
		buttonContainer.Alignment = BoxContainer.AlignmentMode.Center;
		buttonContainer.AddChild(_closeButton);
		contentVbox.AddChild(buttonContainer);
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
		_bookTitle = title;
		_leftPageContent = string.IsNullOrEmpty(leftContent) ? "(Empty page)" : leftContent;
		_rightPageContent = string.IsNullOrEmpty(rightContent) ? "(Empty page)" : rightContent;
		
		_titleLabel.Text = title;
		
		// Center align and display content with BBCode
		_leftPageLabel.Text = $"[center]{_leftPageContent}[/center]";
		_rightPageLabel.Text = $"[center]{_rightPageContent}[/center]";
		
		Visible = true;
		InventoryUI.IsAnyPanelOpen = true; // Block player movement
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GD.Print($"Opening book: {title} | Left: {_leftPageContent} | Right: {_rightPageContent}");
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
		GD.Print("Closing book");
	}
}
