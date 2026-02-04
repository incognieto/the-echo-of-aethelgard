using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Global panel manager untuk track panel mana yang sedang active.
/// Ini memastikan hanya satu panel yang bisa handle input pada satu waktu.
/// </summary>
public partial class PanelManager : Node
{
	private static PanelManager _instance;
	private Stack<Node> _activePanels = new Stack<Node>();

	public override void _Notification(int what)
	{
		if (what == NotificationSceneInstantiated)
		{
			if (_instance == null)
			{
				_instance = this;
				GetTree().Root.AddChild(this);
			}
			else if (_instance != this)
			{
				QueueFree();
			}
		}
	}

	public static PanelManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new PanelManager();
			}
			return _instance;
		}
	}

	/// <summary>
	/// Register panel sebagai active
	/// </summary>
	public void RegisterPanel(Node panel)
	{
		_activePanels.Push(panel);
		GD.Print($"Panel registered: {panel.Name}. Active panels: {_activePanels.Count}");
	}

	/// <summary>
	/// Unregister panel dari active
	/// </summary>
	public void UnregisterPanel(Node panel)
	{
		if (_activePanels.Count > 0 && _activePanels.Peek() == panel)
		{
			_activePanels.Pop();
			GD.Print($"Panel unregistered: {panel.Name}. Active panels: {_activePanels.Count}");
		}
	}

	/// <summary>
	/// Cek apakah ada panel yang sedang active
	/// </summary>
	public bool HasActivePanels()
	{
		return _activePanels.Count > 0;
	}

	/// <summary>
	/// Get top-most active panel
	/// </summary>
	public Node GetActivePanelTop()
	{
		if (_activePanels.Count > 0)
		{
			return _activePanels.Peek();
		}
		return null;
	}

	/// <summary>
	/// Cek apakah spesifik panel sedang active
	/// </summary>
	public bool IsPanelActive(Node panel)
	{
		return _activePanels.Count > 0 && _activePanels.Peek() == panel;
	}
}
