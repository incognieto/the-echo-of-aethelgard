using Godot;
using System;
using System.Threading.Tasks;

public partial class Credits : Control
{
	[ExportGroup("UI Components")]
	[Export] public Label DialogueLabel; // For Grimoire dialogue typing
	[Export] public Label DefiedFateLabel; // For "YOU HAVE DEFIED FATE."
	[Export] public Label TitleLabel; // For "The Echo of Aethelgard"
	[Export] public ColorRect BlackCover; // Fade overlay
	[Export] public ScrollContainer ScrollContainer; // For scrolling credits
	[Export] public VBoxContainer CreditsContent; // Content inside scroll
	[Export] public TextureButton BackButton; // Skip button
	
	[ExportGroup("Images")]
	[Export] public Texture2D GameLogo; // Logo The Echo of Aethelgard
	[Export] public Texture2D Member1Photo; // Nieto Salim Maula
	[Export] public Texture2D Member2Photo; // Farras Ahmad Rasyid
	[Export] public Texture2D Member3Photo; // Muhammad Ichsan Rahmat Ramadhan
	[Export] public Texture2D Member4Photo; // Umar Faruq Robbany
	[Export] public Texture2D Member5Photo; // Satria Permata Sejati
	
	[ExportGroup("Tools & Software (2 rows x 4 columns)")]
	[Export] public Texture2D Tool1Image;
	[Export] public Texture2D Tool2Image;
	[Export] public Texture2D Tool3Image;
	[Export] public Texture2D Tool4Image;
	[Export] public Texture2D Tool5Image;
	[Export] public Texture2D Tool6Image;
	[Export] public Texture2D Tool7Image;
	[Export] public Texture2D Tool8Image;
	
	[ExportGroup("Additional Logos (1 row x 2 columns)")]
	[Export] public Texture2D Logo1Image;
	[Export] public Texture2D Logo2Image;
	
	[ExportGroup("Audio")]
	[Export] public AudioStreamPlayer DialogueTypeSFX; // Typewriter sound
	[Export] public AudioStreamPlayer EpicMusicBGM; // Credits music
	
	[ExportGroup("Settings")]
	[Export] public float TypingSpeed = 0.03f; // Seconds per character
	[Export] public float ScrollSpeed = 50f; // Pixels per second
	[Export] public float FadeDuration = 2f; // Fade in/out duration
	
	private bool _isScrolling = false;
	private bool _skipRequested = false;

	public override async void _Ready()
	{
		// Start epic music for credits
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.InstantRestart();
		}
		
		if (EpicMusicBGM != null)
		{
			EpicMusicBGM.Play();
		}
		
		// Setup initial state
		if (DialogueLabel != null) DialogueLabel.Text = "";
		if (DefiedFateLabel != null) DefiedFateLabel.Modulate = new Color(1, 1, 1, 0); // Invisible
		if (TitleLabel != null) TitleLabel.Text = "";
		if (ScrollContainer != null) ScrollContainer.Modulate = new Color(1, 1, 1, 0); // Invisible
		
		// Start with black screen
		if (BlackCover != null) 
		{
			BlackCover.Color = new Color(0, 0, 0, 1);
			BlackCover.Modulate = new Color(1, 1, 1, 1);
		}
		
		if (BackButton != null)
		{
			BackButton.Pressed += OnSkipPressed;
		}
		
		// Setup images if available
		SetupImages();
		
		GD.Print("🎬 Credits Sequence Start");
		
		// Begin credits sequence
		await RunCreditsSequence();
	}

	private async Task RunCreditsSequence()
	{
		// Fade out black cover first so background is visible
		await FadeOutBlackCover();
		
		// === SEQUENCE 1: Grimoire Dialogue 1 ===
		await ShowDialogueTyped("The Grimoire: \"The iron teeth of Ironfang have finally let go. You were meant to rot in the dark, a nameless footnote in Valerius's bloody history. But you did the impossible... you read the spoilers and dared to change the ending.\"");
		await Task.Delay(TimeSpan.FromSeconds(1.5)); // Brief pause to read
		await ClearDialogue(); // Fade out for transition
		if (_skipRequested) { SkipToEnd(); return; }
		
		// === SEQUENCE 2: Grimoire Dialogue 2 ===
		await ShowDialogueTyped("The Grimoire: \"The map in your hand is no longer just paper; it is the blueprint of a revolution. But remember, Weaver... the King has eyes everywhere, and he does not like it when the ink of destiny is rewritten.\"");
		await Task.Delay(TimeSpan.FromSeconds(1.5));
		await ClearDialogue(); // Fade out for transition
		if (_skipRequested) { SkipToEnd(); return; }
		
		// === SEQUENCE 3: Grimoire Dialogue 3 ===
		await ShowDialogueTyped("The Grimoire: \"Final Spoiler: The real war begins now.\"");
		await Task.Delay(TimeSpan.FromSeconds(1.5));
		await ClearDialogue(); // Fade out for transition
		if (_skipRequested) { SkipToEnd(); return; }
		
		// === SEQUENCE 4: YOU HAVE DEFIED FATE ===
		await ShowDefiedFate();
		await Task.Delay(TimeSpan.FromSeconds(2.5)); // Hold longer
		await ClearDefiedFate(); // Fade out for transition
		if (_skipRequested) { SkipToEnd(); return; }
		
		// === SEQUENCE 5: Title + Scrolling Credits ===
		await ShowTitleTyped("The Echo of Aethelgard");
		await Task.Delay(TimeSpan.FromSeconds(2));
		
		// Show scrolling credits (no typing, no SFX)
		await ShowScrollingCredits();
		
		GD.Print("🎬 Credits Sequence Complete");
	}

	private async Task FadeOutBlackCover()
	{
		if (BlackCover == null) return;
		
		Tween tween = CreateTween();
		tween.TweenProperty(BlackCover, "modulate:a", 0.0f, FadeDuration);
		await ToSignal(tween, "finished");
	}

	private async Task ClearDialogue()
	{
		if (DialogueLabel == null) return;
		
		// Fade out dialogue text
		Tween tween = CreateTween();
		tween.TweenProperty(DialogueLabel, "modulate:a", 0.0f, 0.8f);
		await ToSignal(tween, "finished");
		
		DialogueLabel.Text = "";
		
		// Reset alpha for next dialogue
		DialogueLabel.Modulate = new Color(1, 1, 1, 1);
	}

	private async Task ClearDefiedFate()
	{
		if (DefiedFateLabel == null) return;
		
		// Fade out
		Tween tween = CreateTween();
		tween.TweenProperty(DefiedFateLabel, "modulate:a", 0.0f, 1.0f);
		await ToSignal(tween, "finished");
	}

	private async Task ShowDialogueTyped(string text)
	{
		if (DialogueLabel == null) return;
		
		// Fade in dialogue label first
		DialogueLabel.Text = "";
		DialogueLabel.Modulate = new Color(1, 1, 1, 0);
		
		Tween fadeIn = CreateTween();
		fadeIn.TweenProperty(DialogueLabel, "modulate:a", 1.0f, 0.5f);
		await ToSignal(fadeIn, "finished");
		
		GD.Print($"💬 {text}");
		
		// Start typing sound (loop)
		if (DialogueTypeSFX != null)
		{
			DialogueTypeSFX.Play();
		}
		
		// Typewriter effect
		foreach (char c in text)
		{
			if (_skipRequested) break;
			
			DialogueLabel.Text += c;
			await ToSignal(GetTree().CreateTimer(TypingSpeed), "timeout");
		}
		
		// Stop typing sound
		if (DialogueTypeSFX != null)
		{
			DialogueTypeSFX.Stop();
		}
		
		// If skipped, show full text
		if (_skipRequested)
		{
			DialogueLabel.Text = text;
		}
	}

	private async Task ShowDefiedFate()
	{
		if (DefiedFateLabel == null) return;
		
		DefiedFateLabel.Text = "YOU HAVE DEFIED FATE.";
		
		// Simple fade in without blue glow effect
		Tween tween = CreateTween();
		tween.TweenProperty(DefiedFateLabel, "modulate:a", 1.0f, 1.5f);
		await ToSignal(tween, "finished");
		
		GD.Print("✨ YOU HAVE DEFIED FATE.");
	}

	private async Task ShowTitleTyped(string text)
	{
		if (TitleLabel == null) return;
		
		TitleLabel.Text = "";
		TitleLabel.Modulate = new Color(1, 1, 1, 1);
		
		// Start typing sound
		if (DialogueTypeSFX != null)
		{
			DialogueTypeSFX.Play();
		}
		
		// Typewriter effect for title
		foreach (char c in text)
		{
			if (_skipRequested) break;
			
			TitleLabel.Text += c;
			await ToSignal(GetTree().CreateTimer(TypingSpeed), "timeout");
		}
		
		// Stop typing sound
		if (DialogueTypeSFX != null)
		{
			DialogueTypeSFX.Stop();
		}
		
		if (_skipRequested)
		{
			TitleLabel.Text = text;
		}
		
		GD.Print($"📜 {text}");
	}

	private async Task ShowScrollingCredits()
	{
		if (ScrollContainer == null || CreditsContent == null) return;
		
		// Fade out title smoothly
		if (TitleLabel != null)
		{
			Tween titleTween = CreateTween();
			titleTween.TweenProperty(TitleLabel, "modulate:a", 0.0f, 1.0f);
			await ToSignal(titleTween, "finished");
		}
		
		// Fade in credits container (NO typing, NO SFX)
		Tween fadeTween = CreateTween();
		fadeTween.TweenProperty(ScrollContainer, "modulate:a", 1.0f, 1.5f);
		await ToSignal(fadeTween, "finished");
		
		_isScrolling = true;
		GD.Print("🎞️ Credits rolling...");
	}

	public override void _Process(double delta)
	{
		if (_isScrolling && ScrollContainer != null)
		{
			// Auto-scroll credits from bottom to top
			var scrollBar = ScrollContainer.GetVScrollBar();
			double newVScroll = scrollBar.Value + delta * ScrollSpeed;
			
			// Check if reached end
			if (newVScroll >= scrollBar.MaxValue)
			{
				_isScrolling = false;
				GD.Print("🎬 Credits finished scrolling");
				
				// Wait a bit then return to main menu
				GetTree().CreateTimer(3.0f).Timeout += OnCreditsComplete;
			}
			else
			{
				scrollBar.Value = newVScroll;
			}
		}
	}

	private void OnSkipPressed()
	{
		_skipRequested = true;
		SkipToEnd();
	}

	private void SkipToEnd()
	{
		GD.Print("⏩ Credits skipped");
		OnCreditsComplete();
	}

	private void OnCreditsComplete()
	{
		// Return to main menu
		GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
	}

	private void SetupImages()
	{
		// Setup game logo
		if (GameLogo != null && CreditsContent != null)
		{
			var logoNode = CreditsContent.GetNodeOrNull<TextureRect>("GameLogo");
			if (logoNode != null)
			{
				logoNode.Texture = GameLogo;
			}
		}
		
		// Setup member photos
		var memberPhotos = new[] { Member1Photo, Member2Photo, Member3Photo, Member4Photo, Member5Photo };
		
		for (int i = 0; i < memberPhotos.Length; i++)
		{
			if (memberPhotos[i] != null && CreditsContent != null)
			{
				var memberNode = CreditsContent.GetNodeOrNull<VBoxContainer>($"Member{i + 1}");
				if (memberNode != null)
				{
					var photoNode = memberNode.GetNodeOrNull<TextureRect>("Photo");
					if (photoNode != null)
					{
						photoNode.Texture = memberPhotos[i];
						GD.Print($"✓ Loaded photo for Member {i + 1}");
					}
				}
			}
		}
		
		// Setup tools grid (2 rows x 4 columns = 8 images)
		var toolImages = new[] { Tool1Image, Tool2Image, Tool3Image, Tool4Image, Tool5Image, Tool6Image, Tool7Image, Tool8Image };
		var toolsGrid = CreditsContent?.GetNodeOrNull<GridContainer>("ToolsGrid");
		
		if (toolsGrid != null)
		{
			for (int i = 0; i < toolImages.Length; i++)
			{
				if (toolImages[i] != null)
				{
					var toolNode = toolsGrid.GetNodeOrNull<TextureRect>($"Tool{i + 1}");
					if (toolNode != null)
					{
						toolNode.Texture = toolImages[i];
						GD.Print($"✓ Loaded tool image {i + 1}");
					}
				}
			}
		}
		
		// Setup logos grid (1 row x 2 columns = 2 images)
		var logoImages = new[] { Logo1Image, Logo2Image };
		var logosGrid = CreditsContent?.GetNodeOrNull<GridContainer>("LogosGrid");
		
		if (logosGrid != null)
		{
			for (int i = 0; i < logoImages.Length; i++)
			{
				if (logoImages[i] != null)
				{
					var logoNode = logosGrid.GetNodeOrNull<TextureRect>($"Logo{i + 1}");
					if (logoNode != null)
					{
						logoNode.Texture = logoImages[i];
						GD.Print($"✓ Loaded logo image {i + 1}");
					}
				}
			}
		}
		
		GD.Print("✓ Images setup complete");
	}
}
