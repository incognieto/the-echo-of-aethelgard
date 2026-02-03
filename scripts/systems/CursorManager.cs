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
        Texture2D cursorTexture = cursorType switch
        {
            CursorType.Standard => _cursorStandard,
            CursorType.Hover => _cursorHover,
            CursorType.Grab => _cursorGrab,
            CursorType.Grabbing => _cursorGrabbing,
            CursorType.Loading => _cursorLoading,
            _ => _cursorStandard
        };

        if (cursorTexture != null)
        {
            Input.SetCustomMouseCursor(cursorTexture, Input.CursorShape.Arrow, _hotspot);
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
