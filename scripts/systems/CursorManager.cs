using Godot;
using System;

public partial class CursorManager : Node
{
	private static CursorManager _instance;
	public static CursorManager Instance => _instance;

	private Texture2D _cursorStandard;
	private Texture2D _cursorHover;
	private Texture2D _cursorGrab;
	private Texture2D _cursorGrabbing;
	private Texture2D _cursorLoading;

	private Vector2 _hotspot = new Vector2(0, 0);

	public override void _Ready()
	{
		if (_instance == null)
		{
			_instance = this;
		}
		else
		{
			QueueFree();
			return;
		}

		LoadCursorTextures();
		SetCursor(CursorType.Standard);
	}

	private void LoadCursorTextures()
	{
		_cursorStandard = LoadAndResizeCursor("res://assets/sprites/cursor/Leather Standar.png");
		_cursorHover = LoadAndResizeCursor("res://assets/sprites/cursor/Leather Hover or Click.png");
		_cursorGrab = LoadAndResizeCursor("res://assets/sprites/cursor/Leather Grab.png");
		_cursorGrabbing = LoadAndResizeCursor("res://assets/sprites/cursor/Leather Grabbing.png");
		_cursorLoading = LoadAndResizeCursor("res://assets/sprites/cursor/Leather Loading.png");
	}

	private Texture2D LoadAndResizeCursor(string path)
	{
		var originalTexture = GD.Load<Texture2D>(path);
		if (originalTexture == null) return null;

		var image = originalTexture.GetImage();
		var originalSize = image.GetSize();
		var newSize = new Vector2I((int)(originalSize.X * 0.5f), (int)(originalSize.Y * 0.5f));
		
		image.Resize(newSize.X, newSize.Y, Image.Interpolation.Lanczos);
		
		return ImageTexture.CreateFromImage(image);
	}

   public void SetCursor(CursorType cursorType)
{
	Texture2D cursorTexture = null;
	Vector2 currentHotspot = Vector2.Zero;

	switch (cursorType)
	{
		case CursorType.Standard:
			cursorTexture = _cursorStandard;
			// (21, 0) / 2 = (10.5, 0)
			currentHotspot = new Vector2(10.5f, 0); 
			break;
		case CursorType.Hover:
			cursorTexture = _cursorHover;
			// (32, 6) / 2 = (16, 3)
			currentHotspot = new Vector2(16, 3);
			break;
		case CursorType.Grab:
			cursorTexture = _cursorGrab;
			// (41, 64) / 2 = (20.5, 32)
			currentHotspot = new Vector2(20.5f, 32);
			break;
		case CursorType.Grabbing:
			cursorTexture = _cursorGrabbing;
			// (56, 69) / 2 = (28, 34.5)
			currentHotspot = new Vector2(28, 34.5f);
			break;
		case CursorType.Loading:
			cursorTexture = _cursorLoading;
			// (46, 66) / 2 = (23, 33)
			currentHotspot = new Vector2(23, 33);
			break;
	}

	if (cursorTexture != null)
	{
		Input.SetCustomMouseCursor(cursorTexture, Input.CursorShape.Arrow, currentHotspot);
	}
}
	public enum CursorType
	{
		Standard,
		Hover,
		Grab,
		Grabbing,
		Loading
	}
}
